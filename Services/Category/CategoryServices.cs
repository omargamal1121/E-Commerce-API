using AutoMapper;
using E_Commers.DtoModels;
using E_Commers.DtoModels.CategoryDtos;
using E_Commers.DtoModels.ImagesDtos;
using E_Commers.DtoModels.ProductDtos;
using E_Commers.DtoModels.Responses;
using E_Commers.Enums;
using E_Commers.ErrorHnadling;
using E_Commers.Interfaces;
using E_Commers.Models;
using E_Commers.Services.AdminOpreationServices;
using E_Commers.Services.Cache;
using E_Commers.Services.EmailServices;
using E_Commers.UOW;
using Hangfire;
using Microsoft.EntityFrameworkCore;

namespace E_Commers.Services.Category
{
	public class CategoryServices : ICategoryServices
	{
		private readonly ILogger<CategoryServices> _logger;
		private readonly IMapper _mapping;
		private readonly IUnitOfWork _unitOfWork;
		private readonly IAdminOpreationServices _adminopreationservices;
		private readonly ICacheManager _cacheManager;
		private readonly IImagesServices _imagesServices;
		private const string CACHE_TAG_CATEGORY = "category";

		public CategoryServices(
			 IImagesServices imagesServices,
			IAdminOpreationServices adminopreationservices,
			ICacheManager cacheManager,
			IMapper mapping,
			IUnitOfWork unitOfWork,
			ILogger<CategoryServices> logger
		)
		{
			_imagesServices = imagesServices;
			_adminopreationservices = adminopreationservices;
			_cacheManager = cacheManager;
			_mapping = mapping;
			_logger = logger;
			_unitOfWork = unitOfWork;
		}


		public async Task<Result<List<CategoryDto>>> SearchAsync(string keyword, bool isActiveFilter)
		{
			_logger.LogInformation($"Executing {nameof(SearchAsync)} in CategoryService with keyword: {keyword}, isActiveFilter: {isActiveFilter}");

			if (string.IsNullOrWhiteSpace(keyword))
			{
				return Result<List<CategoryDto>>.Fail("Keyword must not be empty", 400);
			}

			var query = _unitOfWork.Category.FindByNameContains(keyword)
				.Where(c => c.DeletedAt == null);

			if (isActiveFilter)
			{
				query = query.Where(c => c.IsActive);
			}

			// Load only required fields directly to reduce DB overhead
			var result = await query
				.Select(c => new CategoryDto
				{
					Id = c.Id,
					Name = c.Name,
					Description = c.Description,
					DisplayOrder = c.DisplayOrder,
					IsActive = c.IsActive,
					MainImageUrl = c.Images
						.Where(i => i.IsMain && i.DeletedAt == null)
						.Select(i => new ImageDto
						{
							Id = i.Id,
							Url = i.Url
						})
						.FirstOrDefault(),
					images = c.Images
						.Where(i => !i.IsMain && i.DeletedAt == null)
						.Select(i => new ImageDto
						{
							Id = i.Id,
							Url = i.Url
						})
						.ToList()
				})
				.ToListAsync();

			if (!result.Any())
			{
				return Result<List<CategoryDto>>.Fail("No categories found.", 404);
			}

			return Result<List<CategoryDto>>.Ok(result, "Result of Search", 200);
		}


		public async Task<Result<string>> IsExsistAsync(int id)
		{
			_logger.LogInformation($"Execute:{nameof(IsExsistAsync)} in Category Services");
			return await _unitOfWork.Category.GetByIdAsync(id) is null ?
				Result<string>.Fail($"No Categoty with this id:{id}", 404)
				:
				Result<string>.Ok(null, "category Exsist", 200);
		}

		public async Task<Result<CategoryDto>> GetCategoryByIdAsync(int id, bool? isActive = null, bool includeDeleted = false)
		{
			_logger.LogInformation($"Execute:{nameof(GetCategoryByIdAsync)} in services for id: {id}, isActive: {isActive}, includeDeleted: {includeDeleted}");
			
			// Create cache key that includes the filter parameters
			var cacheKey = $"{CACHE_TAG_CATEGORY}id:{id}_active:{isActive}_deleted:{includeDeleted}";
			var cachedCategory = await _cacheManager.GetAsync<CategoryDto>(cacheKey);
			if (cachedCategory != null)
			{
				_logger.LogInformation($"Cache hit for category {id} with filters");
				return Result<CategoryDto>.Ok(cachedCategory, "Category found in cache", 200);
			}
			
			var category = await _unitOfWork.Category.GetCategoryById(id, isActive ?? true);
			
			// Additional filtering for deleted items
			if (!includeDeleted && category?.DeletedAt != null)
			{
				_logger.LogWarning($"Category with id: {id} is deleted and includeDeleted is false");
				return Result<CategoryDto>.Fail($"Category with id: {id} not found", 404);
			}
			
			if (category == null)
			{
				_logger.LogWarning($"Category with id: {id} not found");
				return Result<CategoryDto>.Fail($"Category with id: {id} not found", 404);
			}
			
			// Additional filtering for active status if specified
			if (isActive.HasValue && category.IsActive != isActive.Value)
			{
				_logger.LogWarning($"Category with id: {id} active status ({category.IsActive}) doesn't match requested ({isActive.Value})");
				return Result<CategoryDto>.Fail($"Category with id: {id} not found", 404);
			}
			
			var categoryDto = _mapping.Map<CategoryDto>(category);
			await _cacheManager.SetAsync(cacheKey, categoryDto, tags: new[] { CACHE_TAG_CATEGORY });

			return Result<CategoryDto>.Ok(categoryDto, "Category found", 200);
		}

		private void NotifyAdminOfError(string message, string? stackTrace = null)
		{
			BackgroundJob.Enqueue<IErrorNotificationService>(_ => _.SendErrorNotificationAsync(message, stackTrace));
		}

		public async Task<Result<CategoryDto>> CreateAsync(CreateCategotyDto model, string userId)
		{
			_logger.LogInformation($"Execute {nameof(CreateAsync)}");
			if (string.IsNullOrWhiteSpace(model.Name))
			{
				return Result<CategoryDto>.Fail("Category name cannot be empty", 400);
			}
			var isexsist = await _unitOfWork.Category.FindByNameAsync(model.Name);
			if (isexsist != null)
			{
				return Result<CategoryDto>.Fail($"thier's category with this name:{model.Name}", 409);
			}


			using var transaction = await _unitOfWork.BeginTransactionAsync();
			try
			{
				var category = _mapping.Map<Models.Category>(model);

				var creationResult = await _unitOfWork.Category.CreateAsync(category);
				if (creationResult == null)
				{
					_logger.LogWarning("Failed to create category");
					NotifyAdminOfError($"Failed to create category '{model.Name}'");
					await transaction.RollbackAsync();
					return Result<CategoryDto>.Fail("Can't create category now... try again later", 500);
				}

				await _unitOfWork.CommitAsync();

				List<Image> images = new List<Image>();
				List<string>? warings = new List<string>();


				if (model.Images != null && model.Images.Count > 0)
				{
					var imageResult = await _imagesServices.SaveCategoryImagesAsync(model.Images, userId);
					if (!imageResult.Success || imageResult.Data == null)
					{
						_logger.LogWarning(imageResult.Message);
						NotifyAdminOfError($"Failed to save category images for '{model.Name}': {imageResult.Message}");
						await transaction.RollbackAsync();
						return Result<CategoryDto>.Fail($"Failed to save category images: {imageResult.Message}", 400);
					}
					foreach (var img in imageResult.Data)
					{
						images.Add(img);
					}
					warings = imageResult.Warnings;
				}
				if (model.MainImage != null)
				{
					var mainImageResult = await _imagesServices.SaveMainCategoryImageAsync(model.MainImage, userId);
					if (!mainImageResult.Success || mainImageResult.Data == null)
					{
						_logger.LogWarning($"Failed to save main image: {mainImageResult.Message}");
						NotifyAdminOfError($"Failed to save main image for category '{model.Name}': {mainImageResult.Message}");
						await transaction.RollbackAsync();
						return Result<CategoryDto>.Fail($"Failed to save main image: {mainImageResult.Message}", 400);
					}
					images.Add(mainImageResult.Data);
				}

				if (images.Any())
				{
					category.Images = images;
					var updateResult = _unitOfWork.Category.Update(category);
					if (!updateResult)
					{
						_logger.LogError($"Failed to update category with images");
						NotifyAdminOfError($"Failed to update category '{model.Name}' with images");
						await transaction.RollbackAsync();
						return Result<CategoryDto>.Fail("Failed to associate images with category", 500);
					}
				}

				var adminLog = await _adminopreationservices.AddAdminOpreationAsync(
					"Add Category",
					Opreations.AddOpreation,
					userId,
					category.Id
				);

				if (!adminLog.Success)
				{
					_logger.LogError(adminLog.Message);
					NotifyAdminOfError($"Failed to log admin operation for category '{model.Name}' (ID: {category.Id})");
					await transaction.RollbackAsync();
					return Result<CategoryDto>.Fail("Try Again later", 500);
				}


				await _cacheManager.RemoveByTagAsync(CACHE_TAG_CATEGORY);


				await _unitOfWork.CommitAsync();
				await transaction.CommitAsync();


				_logger.LogInformation($"Retrieving category with ID {category.Id} after creation");
				var categoryWithImages = await _unitOfWork.Category.GetCategoryById(category.Id);
				if (categoryWithImages == null)
				{
					_logger.LogError("Failed to retrieve created category with images");
					NotifyAdminOfError($"Failed to retrieve created category with ID {category.Id} after creation");
					return Result<CategoryDto>.Fail("Category created but failed to retrieve details", 500);
				}

				_logger.LogInformation($"Successfully retrieved category: Id={categoryWithImages.Id}, Name={categoryWithImages.Name}");
				_logger.LogInformation($"Category has {categoryWithImages.Images?.Count ?? 0} images");

				try
				{
					_logger.LogInformation($"Mapping category with {categoryWithImages.Images?.Count ?? 0} images");
					var categoryDto = _mapping.Map<CategoryDto>(categoryWithImages);
					_logger.LogInformation($"Successfully mapped category to DTO");
					return Result<CategoryDto>.Ok(categoryDto, "Created", 201, warnings: warings);
				}
				catch (Exception ex)
				{
					_logger.LogError($"Error mapping category to DTO: {ex.Message}");
					NotifyAdminOfError($"Error mapping category to DTO: {ex.Message}");
					return Result<CategoryDto>.Fail("Category created but failed to map details", 500);
				}
			}
			catch (Exception ex)
			{
				await transaction.RollbackAsync();
				_logger.LogError($"Exception in CreateAsync: {ex.Message}");
				NotifyAdminOfError($"Exception in CreateAsync: {ex.Message}");
				return Result<CategoryDto>.Fail("Server error occurred while creating category", 500);
			}
		}

		public async Task<Result<List<ImageDto>>> AddImagesToCategoryAsync(int categoryId, List<IFormFile> images, string userId)
		{
			_logger.LogInformation($"Executing {nameof(AddImagesToCategoryAsync)} for categoryId: {categoryId}");
			if (images == null || !images.Any())
			{
				return Result<List<ImageDto>>.Fail("At least one image is required.", 400);
			}
			
			using var transaction = await _unitOfWork.BeginTransactionAsync();
			try
			{
				var category = await _unitOfWork.Category.GetCategoryById(categoryId);
				if (category == null)
				{
					await transaction.RollbackAsync();
					return Result<List<ImageDto>>.Fail($"Category with id {categoryId} not found", 404);
				}
				
				var imageResult = await _imagesServices.SaveCategoryImagesAsync(images, userId);
				if (!imageResult.Success || imageResult.Data == null)
				{
					await transaction.RollbackAsync();
					return Result<List<ImageDto>>.Fail($"Failed to save images: {imageResult.Message}", 400);
				}
				
				foreach (var img in imageResult.Data)
				{
					category.Images.Add(img);
				}
				
				var updateResult = _unitOfWork.Category.Update(category);
				if (!updateResult)
				{
					await transaction.RollbackAsync();
					return Result<List<ImageDto>>.Fail($"Failed to update category with new images", 500);
				}
				
				await _unitOfWork.CommitAsync();
				await transaction.CommitAsync();
				
				var mapped = _mapping.Map<List<ImageDto>>(category.Images);
				return Result<List<ImageDto>>.Ok(mapped, $"Added {imageResult.Data.Count} images to category", 200, warnings: imageResult.Warnings);
			}
			catch (Exception ex)
			{
				await transaction.RollbackAsync();
				_logger.LogError($"Exception in AddImagesToCategoryAsync: {ex.Message}");
				NotifyAdminOfError($"Exception in AddImagesToCategoryAsync: {ex.Message}", ex.StackTrace);
				return Result<List<ImageDto>>.Fail("An error occurred while adding images", 500);
			}
		}

		public async Task<Result<string>> DeleteAsync(int categoryId, string userid)
		{
			_logger.LogInformation($"Executing {nameof(DeleteAsync)} for categoryId: {categoryId}");
			
			using var transaction = await _unitOfWork.BeginTransactionAsync();
			try
			{
				var category = await _unitOfWork.Category.GetCategoryById(categoryId);
				if (category == null)
				{
					await transaction.RollbackAsync();
					return Result<string>.Fail($"Category with id {categoryId} not found", 404);
				}
				
				// Check if category has subcategories
				var hasSubCategories = await _unitOfWork.Category.HasSubCategoriesAsync(categoryId);
				if (hasSubCategories)
				{
					await transaction.RollbackAsync();
					_logger.LogWarning($"Category {categoryId} contains subcategories");
					return Result<string>.Fail("Can't delete category because it has subcategories", 400);
				}
				
				var deleteResult = await _unitOfWork.Category.SoftDeleteAsync(categoryId);
				if (!deleteResult)
				{
					await transaction.RollbackAsync();
					return Result<string>.Fail($"Failed to delete category", 500);
				}
				
				var adminLog = await _adminopreationservices.AddAdminOpreationAsync(
					$"Deleted Category {categoryId}",
					Opreations.DeleteOpreation,
					userid,
					categoryId
				);
				
				if (!adminLog.Success)
				{
					_logger.LogWarning($"Failed to log admin operation: {adminLog.Message}");
				}
				
				await _unitOfWork.CommitAsync();
				await transaction.CommitAsync();
				await _cacheManager.RemoveByTagAsync(CACHE_TAG_CATEGORY);
				
				return Result<string>.Ok(null, $"Category with ID {categoryId} deleted successfully", 200);
			}
			catch (Exception ex)
			{
				await transaction.RollbackAsync();
				_logger.LogError($"Exception in DeleteAsync: {ex.Message}");
				NotifyAdminOfError($"Exception in DeleteAsync: {ex.Message}", ex.StackTrace);
				return Result<string>.Fail("An error occurred while deleting category", 500);
			}
		}

		public async Task<Result<List<CategoryDto>>> FilterAsync(
	string? search,
	bool? isActive,
	bool includeDeleted,
	int page,
	int pageSize,
	string userRole)
		{
			_logger.LogInformation($"Executing {nameof(FilterAsync)} with filters");

			if ((includeDeleted || isActive == false) && userRole != "Admin")
			{
				return Result<List<CategoryDto>>.Fail("Unauthorized access", 403);
			}

			var query = _unitOfWork.Category.GetAll();

			
			if (!string.IsNullOrWhiteSpace(search))
				query = query.Where(c => c.Name.Contains(search));

			if (isActive.HasValue)
				query = query.Where(c => c.IsActive == isActive.Value);

			if (!includeDeleted)
				query = query.Where(c => c.DeletedAt == null);

		
			bool canCache = string.IsNullOrWhiteSpace(search)
				&& page == 1
				&& pageSize == 10
				&& (isActive ?? true)
				&& !includeDeleted;

			string cacheKey = $"{CACHE_TAG_CATEGORY}_filtered_{isActive}_{includeDeleted}_p{page}_ps{pageSize}_{search}";

			if (canCache)
			{
				var cachedData = await _cacheManager.GetAsync<List<CategoryDto>>(cacheKey);
				if (cachedData != null)
					return Result<List<CategoryDto>>.Ok(cachedData, "Categories from cache", 200);
			}

			var result = await query
				.OrderBy(c => c.DisplayOrder)
				.Skip((page - 1) * pageSize)
				.Take(pageSize)
				.Select(c => new CategoryDto
				{
					Id = c.Id,
					Name = c.Name,
					Description = c.Description,
					DisplayOrder = c.DisplayOrder,
					IsActive = c.IsActive,
					MainImageUrl = c.Images
						.Where(i => i.IsMain && i.DeletedAt == null)
						.Select(i => new ImageDto { Id = i.Id, Url = i.Url })
						.FirstOrDefault(),
					images = c.Images
						.Where(i => !i.IsMain && i.DeletedAt == null)
						.Select(i => new ImageDto { Id = i.Id, Url = i.Url })
						.ToList()
				})
				.ToListAsync();

			if (!result.Any())
				return Result<List<CategoryDto>>.Fail("No categories found", 404);

			if (canCache)
			{
				await _cacheManager.SetAsync(cacheKey, result, tags: new[] { CACHE_TAG_CATEGORY });
			}

			return Result<List<CategoryDto>>.Ok(result, "Filtered categories retrieved", 200);
		}


		public async Task<Result<CategoryDto>> ReturnRemovedCategoryAsync(int id, string userid)
		{
			_logger.LogInformation($"Executing {nameof(ReturnRemovedCategoryAsync)} for id: {id}");
			
			using var transaction = await _unitOfWork.BeginTransactionAsync();
			try
			{
				var category = await _unitOfWork.Category.GetCategoryById(id);
				if (category == null || category.DeletedAt == null)
				{
					await transaction.RollbackAsync();
					_logger.LogWarning($"Can't Found Category with this id:{id}");
					return Result<CategoryDto>.Fail($"Can't Found Category with this id:{id}", 404);
				}
				
				var restoreResult = await _unitOfWork.Category.RestoreAsync(id);
				if (!restoreResult)
				{
					await transaction.RollbackAsync();
					return Result<CategoryDto>.Fail("Try Again later", 500);
				}
				
				var adminLog = await _adminopreationservices.AddAdminOpreationAsync(
					$"Restored Category {id}",
					Opreations.UpdateOpreation,
					userid,
					id
				);
				
				if (!adminLog.Success)
				{
					_logger.LogWarning($"Failed to log admin operation: {adminLog.Message}");
				}
				
				await _unitOfWork.CommitAsync();
				await transaction.CommitAsync();
				await _cacheManager.RemoveByTagAsync(CACHE_TAG_CATEGORY);
				
				var categorydto = _mapping.Map<CategoryDto>(category);
				return Result<CategoryDto>.Ok(categorydto, "Category restored successfully", 200);
			}
			catch (Exception ex)
			{
				await transaction.RollbackAsync();
				_logger.LogError($"Exception in ReturnRemovedCategoryAsync: {ex.Message}");
				NotifyAdminOfError($"Exception in ReturnRemovedCategoryAsync: {ex.Message}", ex.StackTrace);
				return Result<CategoryDto>.Fail("An error occurred while restoring category", 500);
			}
		}



	
		public async Task<Result<bool>> ChangeActiveStatus(int categoryId, string userId)
		{
			_logger.LogInformation($"Executing {nameof(ChangeActiveStatus)} for categoryId: {categoryId}");
			
			using var transaction = await _unitOfWork.BeginTransactionAsync();
			try
			{
				var category = await _unitOfWork.Category.GetCategoryById(categoryId);
				if (category == null)
				{
					await transaction.RollbackAsync();
					return Result<bool>.Fail($"Category with id {categoryId} not found", 404);
				}

				category.IsActive = !category.IsActive;

				var adminOpResult = await _adminopreationservices.AddAdminOpreationAsync(
					category.IsActive ? "Activate Category" : "Deactivate Category",
					 Opreations.UpdateOpreation,
					userId,
					category.Id
				);
				
				if (!adminOpResult.Success)
				{
					_logger.LogWarning($"Failed to log admin operation: {adminOpResult.Message}");
				}
				
				await _unitOfWork.CommitAsync();
				await transaction.CommitAsync();
				await _cacheManager.RemoveByTagAsync(CACHE_TAG_CATEGORY);

				return Result<bool>.Ok(category.IsActive, $"Category with ID {categoryId} has been {(category.IsActive ? "activated" : "deactivated")}", 200);
			}
			catch (Exception ex)
			{
				await transaction.RollbackAsync();
				_logger.LogError($"Exception in ChangeActiveStatus: {ex.Message}");
				NotifyAdminOfError($"Exception in ChangeActiveStatus: {ex.Message}", ex.StackTrace);
				return Result<bool>.Fail("An error occurred while changing category status", 500);
			}
		}
		public async Task<Result<CategoryDto>> UpdateAsync(int categoryId, UpdateCategoryDto category, string userid)
		{
			_logger.LogInformation($"Executing {nameof(UpdateAsync)} for categoryId: {categoryId}");

			var existingCategory = await _unitOfWork.Category.GetCategoryById(categoryId);
			if (existingCategory == null)
			{
				return Result<CategoryDto>.Fail($"Category with id {categoryId} not found", 404);
			}
		
				using var transaction = await _unitOfWork.BeginTransactionAsync();

			try
			{
				List<string> warings = new List<string>();
				if (category.newImages != null && category.newImages.Count != 0)
				{
					var imageResult = await _imagesServices.SaveCategoryImagesAsync(category.newImages, userid);
					if (!imageResult.Success || imageResult.Warnings.Count!=0)
					{
						_logger.LogWarning($"Failed to save new images: {imageResult.Message}");
						warings = imageResult?.Warnings;
					}
					foreach (var img in imageResult?.Data)
					{
						existingCategory.Images.Add(img);
					}
				}
				if (category.NewMainImage != null)
				{
					var mainimage = existingCategory.Images.FirstOrDefault(i => i.IsMain && i.DeletedAt == null);
					var issaved = await _imagesServices.SaveMainCategoryImageAsync(category.NewMainImage, userid);
					if (!issaved.Success || issaved.Data == null)
					{
						_logger.LogWarning("No main image found after update");
						warings.Add("No main image found after update");
					}
					else
					{
						existingCategory.Images.Add(issaved.Data);
						if (mainimage != null)
						{
							_ = _imagesServices.DeleteImageAsync(mainimage);
						}
					}
				}
				if (!string.IsNullOrWhiteSpace(category.Name))
					existingCategory.Name = category.Name;

				if (!string.IsNullOrWhiteSpace(category.Description))
					existingCategory.Description = category.Description;

				if (category.DisplayOrder.HasValue)
					existingCategory.DisplayOrder = category.DisplayOrder.Value;

				existingCategory.IsActive = category.IsActive;


				var adminOpResult = await _adminopreationservices.AddAdminOpreationAsync(
					"Update Category",
					Opreations.UpdateOpreation,
					userid,
					existingCategory.Id
				);

				if (!adminOpResult.Success)
				{
					_logger.LogWarning($"Failed to log admin operation: {adminOpResult.Message}");
				}

				await _unitOfWork.CommitAsync();
				await transaction.CommitAsync();

				await _cacheManager.RemoveByTagAsync(CACHE_TAG_CATEGORY);

				var dto = _mapping.Map<CategoryDto>(existingCategory);
				return Result<CategoryDto>.Ok(dto, "Updated", 200, warnings: warings);
			}
			catch (Exception ex)
			{
				await transaction.RollbackAsync();
				_logger.LogError($"Exception in UpdateAsync: {ex.Message}");
				return Result<CategoryDto>.Fail("An error occurred during update", 500);
			}
		}



		//public async Task<ApiResponse<List<SubCategoryDto>>> GetSubCategoriesAsync(int categoryId)
		//      {
		//          var subcategoriesQuery = _unitOfWork.Category.GetSubCategories(categoryId);
		//          var dtos = await subcategoriesQuery.Select(sc => new SubCategoryDto
		//          {
		//              Id = sc.Id,
		//              Name = sc.Name,
		//              Description = sc.Description,
		//          }).ToListAsync();
		//          if (!dtos.Any())
		//              return ApiResponse<List<SubCategoryDto>>.CreateErrorResponse(
		//                  new ErrorResponse("SubCategory", $"No subcategories found for category {categoryId}"),
		//                  404
		//              );
		//          return ApiResponse<List<SubCategoryDto>>.CreateSuccessResponse("Subcategories found", dtos);
		//      }


		public async Task<Result<ImageDto>> AddMainImageToCategoryAsync(int categoryId, IFormFile mainImage, string userId)
		{
			_logger.LogInformation($"Executing {nameof(AddMainImageToCategoryAsync)} for categoryId: {categoryId}");
			if (mainImage == null || mainImage.Length == 0)
			{
				return Result<ImageDto>.Fail("Main image is required.", 400);
			}
			
			using var transaction = await _unitOfWork.BeginTransactionAsync();
			try
			{
				var category = await _unitOfWork.Category.GetCategoryById(categoryId);
				if (category == null)
				{
					await transaction.RollbackAsync();
					return Result<ImageDto>.Fail($"Category with id {categoryId} not found", 404);
				}
				
				// Remove existing main image if exists
				var existingMainImage = category.Images.FirstOrDefault(i => i.IsMain && i.DeletedAt == null);
				if (existingMainImage != null)
				{
					_logger.LogInformation($"Removing existing main image with ID {existingMainImage.Id} from category {categoryId}");
					var deleteResult = await _imagesServices.DeleteImageAsync(existingMainImage);
					if (!deleteResult.Success)
					{
						_logger.LogError($"Failed to delete existing main image: {deleteResult.Message}");
						await transaction.RollbackAsync();
						return Result<ImageDto>.Fail(deleteResult.Message, deleteResult.StatusCode, deleteResult.Warnings);
					}
					category.Images.Remove(existingMainImage);
				}
				
				var mainImageResult = await _imagesServices.SaveMainCategoryImageAsync(mainImage, userId);
				if (!mainImageResult.Success || mainImageResult.Data == null)
				{
					await transaction.RollbackAsync();
					return Result<ImageDto>.Fail($"Failed to save main image: {mainImageResult.Message}", 500);
				}
				
				category.Images.Add(mainImageResult.Data);
				var updateResult = _unitOfWork.Category.Update(category);
				if (!updateResult)
				{
					await transaction.RollbackAsync();
					return Result<ImageDto>.Fail($"Failed to update category with main image", 500);
				}
				
				await _unitOfWork.CommitAsync();
				await transaction.CommitAsync();
				
				var mapped = _mapping.Map<ImageDto>(mainImageResult.Data);
				return Result<ImageDto>.Ok(mapped, "Main image added to category", 200);
			}
			catch (Exception ex)
			{
				await transaction.RollbackAsync();
				_logger.LogError($"Exception in AddMainImageToCategoryAsync: {ex.Message}");
				NotifyAdminOfError($"Exception in AddMainImageToCategoryAsync: {ex.Message}", ex.StackTrace);
				return Result<ImageDto>.Fail("An error occurred while adding main image", 500);
			}
		}

		public async Task<Result<CategoryDto>> RemoveImageFromCategoryAsync(int categoryId, int imageId, string userId)
		{
			_logger.LogInformation($"Removing image {imageId} from category: {categoryId}");
			
			using var transaction = await _unitOfWork.BeginTransactionAsync();
			try
			{
				var category = await _unitOfWork.Category.GetCategoryById(categoryId);
				if (category == null)
				{
					await transaction.RollbackAsync();
					return Result<CategoryDto>.Fail($"Category with id {categoryId} not found", 404);
				}
				
				var image = category.Images.FirstOrDefault(i => i.Id == imageId);
				if (image == null)
				{
					await transaction.RollbackAsync();
					return Result<CategoryDto>.Fail("Image not found", 404);
				}
				
				category.Images.Remove(image);
				
				// Optionally: delete file from disk
				var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", image.Url.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
				if (File.Exists(filePath))
				{
					File.Delete(filePath);
				}
				
				var updateResult = _unitOfWork.Category.Update(category);
				if (!updateResult)
				{
					await transaction.RollbackAsync();
					return Result<CategoryDto>.Fail("Failed to remove image", 400);
				}
				
				var adminLog = await _adminopreationservices.AddAdminOpreationAsync(
					$"Remove Image {imageId} from Category {categoryId}",
					Opreations.UpdateOpreation,
					userId,
					categoryId
				);
				
				if (!adminLog.Success)
				{
					_logger.LogWarning($"Failed to log admin operation: {adminLog.Message}");
				}
				
				await _unitOfWork.CommitAsync();
				await transaction.CommitAsync();
				
				var categoryDto = _mapping.Map<CategoryDto>(category);
				return Result<CategoryDto>.Ok(categoryDto, "Image removed successfully", 200);
			}
			catch (Exception ex)
			{
				await transaction.RollbackAsync();
				_logger.LogError(ex, $"Unexpected error in RemoveImageFromCategoryAsync for category {categoryId}");
				NotifyAdminOfError(ex.Message, ex.StackTrace);
				return Result<CategoryDto>.Fail("Unexpected error occurred while removing image", 500);
			}
		}

	}
}
