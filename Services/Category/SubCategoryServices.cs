using AutoMapper;
using E_Commers.DtoModels.CategoryDtos;
using E_Commers.DtoModels.DiscoutDtos;
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
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace E_Commers.Services.Category
{
    public class SubCategoryServices : ISubCategoryServices
    {
        private readonly ILogger<SubCategoryServices> _logger;
        private readonly IMapper _mapping;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IAdminOpreationServices _adminopreationservices;
        private readonly ICacheManager _cacheManager;
        private readonly IImagesServices _imagesServices;
        private const string CACHE_TAG_SUBCATEGORY = "subcategory";

        public SubCategoryServices(
            IImagesServices imagesServices,
            IAdminOpreationServices adminopreationservices,
            ICacheManager cacheManager,
            IMapper mapping,
            IUnitOfWork unitOfWork,
            ILogger<SubCategoryServices> logger
        )
        {
            _imagesServices = imagesServices;
            _adminopreationservices = adminopreationservices;
            _cacheManager = cacheManager;
            _mapping = mapping;
            _logger = logger;
            _unitOfWork = unitOfWork;
        }

        private void NotifyAdminOfError(string message, string? stackTrace = null)
        {
            BackgroundJob.Enqueue<IErrorNotificationService>(_ => _.SendErrorNotificationAsync(message, stackTrace));
        }

        public async Task<Result<List<SubCategoryDto>>> SearchAsync(string key, bool isactivefilter)
        {
            _logger.LogInformation($"Executing {nameof(SearchAsync)} in SubCategoryService with keyword: {key}, isActiveFilter: {isactivefilter}");
            if (string.IsNullOrWhiteSpace(key))
            {
                return Result<List<SubCategoryDto>>.Fail("Keyword must not be empty", 400);
            }
            var query = _unitOfWork.SubCategory.FindByNameContains(key)
                .Where(sc => sc.DeletedAt == null);
            if (isactivefilter)
            {
                query = query.Where(sc => sc.IsActive);
            }
            var subCategories = await query
                .Select(sc => new
                {
                    sc.Id,
                    sc.Name,
                    sc.Description,
                    sc.IsActive,
                    sc.CreatedAt,
                    sc.ModifiedAt,
                    sc.DeletedAt,
                    Images = sc.Images.Where(i => i.DeletedAt == null).Select(i => new
                    {
                        i.Id,
                        i.Url,
                        i.IsMain,
             
            
                        i.DeletedAt
                    }).ToList(),
                    Products = sc.Products.Where(p => p.DeletedAt == null).Select(p => new
                    {
                        p.Id,
                        p.Name,
                        p.Description,
                        p.Quantity,
                        p.Gender,
                        p.SubCategoryId,
                   
                        Discount = p.Discount != null ? new
                        {
                            p.Discount.Id,
                            p.Discount.Name,
                            p.Discount.Description,
                            p.Discount.DiscountPercent,
                            p.Discount.StartDate,
                            p.Discount.EndDate,
                            p.Discount.IsActive,
                  
                        } : null,
                        ProductVariants = p.ProductVariants.Where(v => v.DeletedAt == null).Select(v => new
                        {
                            v.Id,
                            v.Color,
                            v.Size,
                            v.Price,
                            v.Quantity,
                            v.CreatedAt,
                            v.ModifiedAt,
                            v.DeletedAt
                        }).ToList()
                    }).ToList()
                })
                .ToListAsync();
            var result = subCategories.Select(sc => new SubCategoryDto
            {
                Id = sc.Id,
                Name = sc.Name,
                Description = sc.Description,
                IsActive = sc.IsActive,
                CreatedAt = sc.CreatedAt,
                ModifiedAt = sc.ModifiedAt,
                DeletedAt = sc.DeletedAt,
                MainImage = sc.Images.FirstOrDefault(i => i.IsMain) != null ? new ImageDto
                {
                    Id = sc.Images.FirstOrDefault(i => i.IsMain).Id,
                    Url = sc.Images.FirstOrDefault(i => i.IsMain).Url
                } : null,
                Images = sc.Images.Where(i => !i.IsMain).Select(i => new ImageDto
                {
                    Id = i.Id,
                    Url = i.Url
                }).ToList(),
                Products = sc.Products.Select(p => new ProductDto
                {
                    Id = p.Id,
                    Name = p.Name,
                    Description = p.Description,
                    AvailableQuantity = p.Quantity,
                    Gender = p.Gender,
                    SubCategoryId = p.SubCategoryId,
                    MinPrice = p.ProductVariants?.Any() == true ? p.ProductVariants.Min(v => v.Price) : 0,
                    MaxPrice = p.ProductVariants?.Any() == true ? p.ProductVariants.Max(v => v.Price) : 0,
                    FinalPrice = p.ProductVariants?.Any() == true ? 
                        (p.Discount != null && p.Discount.IsActive ? 
                            p.ProductVariants.Min(v => v.Price) * (1 - p.Discount.DiscountPercent / 100) : 
                            p.ProductVariants.Min(v => v.Price)) : 0,
                    HasVariants = p.ProductVariants?.Any() == true,
                    TotalVariants = p.ProductVariants?.Count ?? 0,
                    Discount = p.Discount != null ? new DiscountDto
                    {
                        Id = p.Discount.Id,
                        Name = p.Discount.Name,
                        Description = p.Discount.Description,
                        DiscountPercent = p.Discount.DiscountPercent,
                        StartDate = p.Discount.StartDate,
                        EndDate = p.Discount.EndDate,
                        IsActive = p.Discount.IsActive
                    } : null,
                    Images = new List<ImageDto>(), // Products don't have images in this projection
                    Variants = p.ProductVariants?.Select(v => new ProductVariantDto
                    {
                        Id = v.Id,
                        Color = v.Color,
                        Size = v.Size,
                        Price = v.Price,
                        Quantity = v.Quantity
                    }).ToList() ?? new List<ProductVariantDto>()
                }).ToList()
            }).ToList();
            if (!result.Any())
            {
                return Result<List<SubCategoryDto>>.Fail("No subcategories found.", 404);
            }
            return Result<List<SubCategoryDto>>.Ok(result, "Result of Search", 200);
        }

        public async Task<Result<string>> IsExsistAsync(int id)
        {
                _logger.LogInformation($"Execute:{nameof(IsExsistAsync)} in SubCategory Services");
                var exists = await _unitOfWork.SubCategory.GetAll()
                    .Where(sc => sc.Id == id && sc.DeletedAt == null)
                    .Select(sc => sc.Id)
                    .AnyAsync();
                return exists ?
                Result<string>.Ok(null, "subcategory Exsist", 200)
                :
                Result<string>.Fail($"No SubCategory with this id:{id}", 404);
        }

        public async Task<Result<SubCategoryDto>> GetSubCategoryByIdAsync(int id, bool isActive = true, bool includeDeleted = false)
        {
            _logger.LogInformation($"Execute:{nameof(GetSubCategoryByIdAsync)} in services for id: {id}, isActive: {isActive}, includeDeleted: {includeDeleted}");
       
            var cacheKey = $"{CACHE_TAG_SUBCATEGORY}id:{id}_active:{isActive}_deleted:{includeDeleted}";
            var cached = await _cacheManager.GetAsync<SubCategoryDto>(cacheKey);
            if (cached != null)
            {
                _logger.LogInformation($"Cache hit for subcategory {id} with filters");
                return Result<SubCategoryDto>.Ok(cached, "SubCategory found in cache", 200);
            }
            
            var subCategory = await _unitOfWork.SubCategory.GetSubCategoryById(id, isActive);
            
            if (!includeDeleted && subCategory?.DeletedAt != null)
            {
                _logger.LogWarning($"SubCategory with id: {id} is deleted and includeDeleted is false");
                return Result<SubCategoryDto>.Fail($"SubCategory with id: {id} not found", 404);
            }
            
            if (subCategory == null)
            {
                _logger.LogWarning($"SubCategory with id: {id} not found");
                return Result<SubCategoryDto>.Fail($"SubCategory with id: {id} not found", 404);
            }
            
            // Additional filtering for active status if specified
            if (isActive!=subCategory.IsActive)
            {
                _logger.LogWarning($"SubCategory with id: {id} active status ({subCategory.IsActive}) doesn't match requested ({isActive})");
                return Result<SubCategoryDto>.Fail($"SubCategory with id: {id} not found", 404);
            }
            
            var dto = _mapping.Map<SubCategoryDto>(subCategory);
            await _cacheManager.SetAsync(cacheKey, dto, tags: new[] { CACHE_TAG_SUBCATEGORY });
            return Result<SubCategoryDto>.Ok(dto, "SubCategory found", 200);
        }

        public async Task<Result<SubCategoryDto>> CreateAsync(CreateSubCategoryDto subCategory, string userid)
        {
            _logger.LogInformation($"Execute {nameof(CreateAsync)}");
            if (string.IsNullOrWhiteSpace(subCategory.Name))
            {
                return Result<SubCategoryDto>.Fail("SubCategory name cannot be empty", 400);
            }
            
           
            var category = await _unitOfWork.Category.GetByIdAsync(subCategory.CategoryId);
            if (category == null)
            {
                return Result<SubCategoryDto>.Fail($"Category with id {subCategory.CategoryId} not found", 404);
            }
            
            var isexsist = await _unitOfWork.SubCategory.FindByNameAsync(subCategory.Name);
            if (isexsist != null)
            {
                return Result<SubCategoryDto>.Fail($"there's subcategory with this name:{subCategory.Name}", 409);
            }
            using var transaction = await _unitOfWork.BeginTransactionAsync();
            try
            {
                SubCategory subCategoryEntity = _mapping.Map<SubCategory>(subCategory);
                var creationResult = await _unitOfWork.SubCategory.CreateAsync(subCategoryEntity);
                if (creationResult == null)
                {
                    _logger.LogWarning("Failed to create subcategory");
                    NotifyAdminOfError($"Failed to create subcategory '{subCategory.Name}'");
                    await transaction.RollbackAsync();
                    return Result<SubCategoryDto>.Fail("Can't create subcategory now... try again later", 500);
                }
                await _unitOfWork.CommitAsync();
                List<Image> images = new List<Image>();
                List<string>? warnings = new List<string>();
                if (subCategory.Images != null && subCategory.Images.Count > 0)
                {
                    var imageResult = await _imagesServices.SaveSubCategoryImagesAsync(subCategory.Images, userid);
                    if (imageResult == null || imageResult.Data == null)
                    {
                        _logger.LogWarning("Failed to save subcategory images");
                        NotifyAdminOfError($"Failed to save subcategory images for '{subCategory.Name}'");
                      
                    }
                    else{
                        images = imageResult.Data;
}                    warnings = imageResult.Warnings;
                }
                if (subCategory.MainImage != null)
                {
                    var mainImageResult = await _imagesServices.SaveMainSubCategoryImageAsync(subCategory.MainImage, userid);
                    if (mainImageResult == null || mainImageResult.Data == null)
                    {
                        _logger.LogWarning($"Failed to save main image");
                        NotifyAdminOfError($"Failed to save main image for subcategory '{subCategory.Name}'");
                        await transaction.RollbackAsync();
                        return Result<SubCategoryDto>.Fail($"Failed to save main image", 400);
                    }
                    images.Add(mainImageResult.Data);
                }
                if (images.Any())
                {
                    subCategoryEntity.Images = images;
                    var updateResult = _unitOfWork.SubCategory.Update(subCategoryEntity);
                    if (!updateResult)
                    {
                        _logger.LogError($"Failed to update subcategory with images");
                        NotifyAdminOfError($"Failed to update subcategory '{subCategory.Name}' with images");
                        await transaction.RollbackAsync();
                        return Result<SubCategoryDto>.Fail("Failed to associate images with subcategory", 500);
                    }
                }
                var adminLog = await _adminopreationservices.AddAdminOpreationAsync(
                    "Add SubCategory",
                    Opreations.AddOpreation,
                    userid,
                    subCategoryEntity.Id
                );
                if (!adminLog.Success)
                {
                    _logger.LogError(adminLog.Message);
                    NotifyAdminOfError($"Failed to log admin operation for subcategory '{subCategory.Name}' (ID: {subCategoryEntity.Id})");
                    await transaction.RollbackAsync();
                    return Result<SubCategoryDto>.Fail("Try Again later", 500);
                }
                await _cacheManager.RemoveByTagAsync(CACHE_TAG_SUBCATEGORY);
                await _unitOfWork.CommitAsync();
                await transaction.CommitAsync();
                _logger.LogInformation($"Retrieving subcategory with ID {subCategoryEntity.Id} after creation");
                var subCategoryWithImages = await _unitOfWork.SubCategory.GetSubCategoryById(subCategoryEntity.Id);
                if (subCategoryWithImages == null)
                {
                    _logger.LogError("Failed to retrieve created subcategory with images");
                    NotifyAdminOfError($"Failed to retrieve created subcategory with ID {subCategoryEntity.Id} after creation");
                    return Result<SubCategoryDto>.Fail("SubCategory created but failed to retrieve details", 500);
                }
                _logger.LogInformation($"Successfully retrieved subcategory: Id={subCategoryWithImages.Id}, Name={subCategoryWithImages.Name}");
                _logger.LogInformation($"SubCategory has {subCategoryWithImages.Images?.Count ?? 0} images");
                try
                {
                    _logger.LogInformation($"Mapping subcategory with {subCategoryWithImages.Images?.Count ?? 0} images");
                    var subCategoryDto = _mapping.Map<SubCategoryDto>(subCategoryWithImages);
                    _logger.LogInformation($"Successfully mapped subcategory to DTO");
                 
                    return Result<SubCategoryDto>.Ok(subCategoryDto, "Created", 201, warnings: warnings);
                }
                catch (Exception mappingEx)
                {
                    _logger.LogError($"Mapping error: {mappingEx.Message}");
                    _logger.LogError($"SubCategory data: Id={subCategoryWithImages.Id}, Name={subCategoryWithImages.Name}, Images count={subCategoryWithImages.Images?.Count ?? 0}");
                    NotifyAdminOfError($"Mapping error in CreateAsync for subcategory '{subCategory.Name}': {mappingEx.Message}", mappingEx.StackTrace);
                    return Result<SubCategoryDto>.Fail("SubCategory created but failed to map to response", 500);
                }
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError($"❌ Exception in CreateAsync: {ex.Message}");
                NotifyAdminOfError($"Exception in CreateAsync for subcategory '{subCategory.Name}': {ex.Message}", ex.StackTrace);
                return Result<SubCategoryDto>.Fail("Can't create subcategory now... try again later", 500);
            }
        }

		public async Task<Result<List<ImageDto>>> AddImagesToSubCategoryAsync(int subCategoryId, List<IFormFile> images, string userId)
		{
			_logger.LogInformation($"Executing {nameof(AddImagesToSubCategoryAsync)} for subCategoryId: {subCategoryId}");

			if (images == null || !images.Any())
			{
				return Result<List<ImageDto>>.Fail("At least one image is required.", 400);
			}

			var subCategory = await _unitOfWork.SubCategory.GetAll().AsTracking()
				.Where(sc => sc.Id == subCategoryId && sc.DeletedAt == null)
				.Include(sc => sc.Images.Where(i => i.DeletedAt == null))
				.FirstOrDefaultAsync();
			if (subCategory == null)
			{
				return Result<List<ImageDto>>.Fail($"SubCategory with id {subCategoryId} not found", 404);
			}

			using var transaction = await _unitOfWork.BeginTransactionAsync();

			try
			{
				var imageResult = await _imagesServices.SaveSubCategoryImagesAsync(images, userId);
				if (imageResult == null || imageResult.Data == null)
				{
					await transaction.RollbackAsync();
					return Result<List<ImageDto>>.Fail(imageResult?.Message ?? "Failed to save images", imageResult?.StatusCode ?? 500, imageResult?.Warnings);
				}

				foreach (var img in imageResult.Data)
				{
					subCategory.Images.Add(img);
				}

				var updateResult = _unitOfWork.SubCategory.Update(subCategory);
				if (!updateResult)
				{
					await transaction.RollbackAsync();
					return Result<List<ImageDto>>.Fail("Failed to update subcategory with new images", 500);
				}

				var adminLog = await _adminopreationservices.AddAdminOpreationAsync(
					$"Added {imageResult.Data.Count} images to SubCategory {subCategoryId}",
					Opreations.UpdateOpreation,
					userId,
					subCategoryId
				);
				
				if (!adminLog.Success)
				{
					_logger.LogWarning($"Failed to log admin operation: {adminLog.Message}");
				}

				await _unitOfWork.CommitAsync();
				await transaction.CommitAsync();
				await _cacheManager.RemoveByTagAsync(CACHE_TAG_SUBCATEGORY);

				var mapped = _mapping.Map<List<ImageDto>>(subCategory.Images);
				return Result<List<ImageDto>>.Ok(mapped, $"Added {imageResult.Data.Count} images to subcategory", 200, warnings: imageResult.Warnings);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, $"Exception in {nameof(AddImagesToSubCategoryAsync)} for subCategoryId: {subCategoryId}");
				await transaction.RollbackAsync();
				 NotifyAdminOfError(ex.Message, ex.StackTrace);
				return Result<List<ImageDto>>.Fail("Unexpected error occurred while adding images", 500);
			}
		}

		public async Task<Result<string>> DeleteAsync(int id, string userid)
        {
            _logger.LogInformation($"Executing {nameof(DeleteAsync)} for subCategoryId: {id}");
            
            using var transaction = await _unitOfWork.BeginTransactionAsync();
            try
            {
                var subCategory = await _unitOfWork.SubCategory.GetAll()
                    .Where(sc => sc.Id == id&&sc.DeletedAt==null)
                    .Select(sc => new { sc.Id, sc.DeletedAt })
                    .FirstOrDefaultAsync();
                if (subCategory == null)
                {
                    await transaction.RollbackAsync();
                    return Result<string>.Fail($"SubCategory with id {id} not found", 404);
                }
                
                if(subCategory.DeletedAt != null)
                {
                    _logger.LogWarning($"SubCategory {id} is already deleted");
                    return Result<string>.Fail($"SubCategory with id {id} is already deleted", 400);
				}
                
                var hasProducts = await _unitOfWork.SubCategory.HasProductsAsync(id);
                if (hasProducts)
                {
                    await transaction.RollbackAsync();
                    _logger.LogWarning($"SubCategory {id} contains products");
                    return Result<string>.Fail("Can't delete subcategory because it has products", 400);
                }
                var deleteResult = await _unitOfWork.SubCategory.SoftDeleteAsync(id);
                if (!deleteResult)
                {
                    await transaction.RollbackAsync();
                    return Result<string>.Fail($"Failed to delete subcategory", 500);
                }
                
                var adminLog = await _adminopreationservices.AddAdminOpreationAsync(
                    $"Deleted SubCategory {id}",
                    Opreations.DeleteOpreation,
                    userid,
                    id
                );
                
                if (!adminLog.Success)
                {
                    _logger.LogWarning($"Failed to log admin operation: {adminLog.Message}");
                }
                
                await _unitOfWork.CommitAsync();
                await transaction.CommitAsync();
                await _cacheManager.RemoveByTagAsync(CACHE_TAG_SUBCATEGORY);
                
                return Result<string>.Ok(null, $"SubCategory with ID {id} deleted successfully", 200);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError($"Exception in DeleteAsync: {ex.Message}");
                NotifyAdminOfError($"Exception in DeleteAsync: {ex.Message}", ex.StackTrace);
                return Result<string>.Fail("An error occurred while deleting subcategory", 500);
            }
        }

        public async Task<Result<List<SubCategoryDto>>> FilterAsync(
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
                return Result<List<SubCategoryDto>>.Fail("Unauthorized access", 403);
            }

            var query = _unitOfWork.SubCategory.GetAll();

            if (!string.IsNullOrWhiteSpace(search))
                query = query.Where(sc => sc.Name.Contains(search));

            if (isActive.HasValue)
                query = query.Where(sc => sc.IsActive == isActive.Value);

            if (includeDeleted && userRole == "Admin")
                query = query.Where(sc => sc.DeletedAt != null);

            bool canCache = string.IsNullOrWhiteSpace(search)
                && page == 1
                && pageSize == 10
                && (isActive ?? true)
                && !includeDeleted;

            string cacheKey = $"{CACHE_TAG_SUBCATEGORY}_filtered_{isActive}_{includeDeleted}_p{page}_ps{pageSize}_{search}";

            if (canCache)
            {
                var cachedData = await _cacheManager.GetAsync<List<SubCategoryDto>>(cacheKey);
                if (cachedData != null)
                    return Result<List<SubCategoryDto>>.Ok(cachedData, "Subcategories from cache", 200);
            }

            var subCategories = await query
                .OrderBy(sc => sc.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(sc => new
                {
                    sc.Id,
                    sc.Name,
                    sc.Description,
                    sc.IsActive,
                    sc.CreatedAt,
                    sc.ModifiedAt,
                    sc.DeletedAt,
                    Images = sc.Images.Where(i => i.DeletedAt == null).Select(i => new
                    {
                        i.Id,
                        i.Url,
                        i.IsMain,
                        i.AltText,
                        i.Title,
                        i.Width,
                        i.Height,
                        i.FileSize,
                        i.FileType,
               
                    }).ToList(),
                    Products = sc.Products.Where(p => p.DeletedAt == null).Select(p => new
                    {
                        p.Id,
                        p.Name,
                        p.Description,
                        p.Quantity,
                        p.Gender,
                        p.SubCategoryId,
                 
                        Discount = p.Discount != null ? new
                        {
                            p.Discount.Id,
                            p.Discount.Name,
                            p.Discount.Description,
                            p.Discount.DiscountPercent,
                            p.Discount.StartDate,
                            p.Discount.EndDate,
                            p.Discount.IsActive,
               
                        } : null,
                        ProductVariants = p.ProductVariants.Where(v => v.DeletedAt == null).Select(v => new
                        {
                            v.Id,
                            v.Color,
                            v.Size,
                            v.Price,
                            v.Quantity,
                            v.CreatedAt,
                            v.ModifiedAt,
                            v.DeletedAt
                        }).ToList()
                    }).ToList()
                })
                .ToListAsync();
            var result = subCategories.Select(sc => new SubCategoryDto
            {
                Id = sc.Id,
                Name = sc.Name,
                Description = sc.Description,
                IsActive = sc.IsActive,
                CreatedAt = sc.CreatedAt,
                ModifiedAt = sc.ModifiedAt,
                DeletedAt = sc.DeletedAt,
                MainImage = sc.Images.FirstOrDefault(i => i.IsMain) != null ? new ImageDto
                {
                    Id = sc.Images.FirstOrDefault(i => i.IsMain).Id,
                    Url = sc.Images.FirstOrDefault(i => i.IsMain).Url
                } : null,
                Images = sc.Images.Where(i => !i.IsMain).Select(i => new ImageDto
                {
                    Id = i.Id,
                    Url = i.Url
                }).ToList(),
                Products = sc.Products.Select(p => new ProductDto
                {
                    Id = p.Id,
                    Name = p.Name,
                    Description = p.Description,
                    AvailableQuantity = p.Quantity,
                    Gender = p.Gender,
                    SubCategoryId = p.SubCategoryId,
                    MinPrice = p.ProductVariants?.Any() == true ? p.ProductVariants.Min(v => v.Price) : 0,
                    MaxPrice = p.ProductVariants?.Any() == true ? p.ProductVariants.Max(v => v.Price) : 0,
                    FinalPrice = p.ProductVariants?.Any() == true ? 
                        (p.Discount != null && p.Discount.IsActive ? 
                            p.ProductVariants.Min(v => v.Price) * (1 - p.Discount.DiscountPercent / 100) : 
                            p.ProductVariants.Min(v => v.Price)) : 0,
                    HasVariants = p.ProductVariants?.Any() == true,
                    TotalVariants = p.ProductVariants?.Count ?? 0,
                    Discount = p.Discount != null ? new DiscountDto
                    {
                        Id = p.Discount.Id,
                        Name = p.Discount.Name,
                        Description = p.Discount.Description,
                        DiscountPercent = p.Discount.DiscountPercent,
                        StartDate = p.Discount.StartDate,
                        EndDate = p.Discount.EndDate,
                        IsActive = p.Discount.IsActive
                    } : null,
                    Images = new List<ImageDto>(), // Products don't have images in this projection
                    Variants = p.ProductVariants?.Select(v => new ProductVariantDto
                    {
                        Id = v.Id,
                        Color = v.Color,
                        Size = v.Size,
                        Price = v.Price,
                        Quantity = v.Quantity
                    }).ToList() ?? new List<ProductVariantDto>()
                }).ToList()
            }).ToList();

            if (!result.Any())
                return Result<List<SubCategoryDto>>.Fail("No subcategories found", 404);

            if (canCache)
            {
                await _cacheManager.SetAsync(cacheKey, result, tags: new[] { CACHE_TAG_SUBCATEGORY });
            }

            return Result<List<SubCategoryDto>>.Ok(result, "Filtered subcategories retrieved", 200);
        }


        public async Task<Result<SubCategoryDto>> ReturnRemovedSubCategoryAsync(int id, string userid)
        {
            _logger.LogInformation($"Executing {nameof(ReturnRemovedSubCategoryAsync)} for id: {id}");
            
            using var transaction = await _unitOfWork.BeginTransactionAsync();
            try
            {
                var subCategory = await _unitOfWork.SubCategory.GetAll()
                    .Where(sc => sc.Id == id)
                    .Select(sc => new { sc.Id, sc.DeletedAt })
                    .FirstOrDefaultAsync();
                if (subCategory == null || subCategory.DeletedAt == null)
                {
                    await transaction.RollbackAsync();
                    _logger.LogWarning($"Can't Found SubCategory with this id:{id}");
                    return Result<SubCategoryDto>.Fail($"Can't Found SubCategory with this id:{id}", 404);
                }
                
                var restoreResult = await _unitOfWork.SubCategory.RestoreAsync(id);
                if (!restoreResult)
                {
                    await transaction.RollbackAsync();
                    return Result<SubCategoryDto>.Fail("Try Again later", 500);
                }
                
                var adminLog = await _adminopreationservices.AddAdminOpreationAsync(
                    $"Restored SubCategory {id}",
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
                await _cacheManager.RemoveByTagAsync(CACHE_TAG_SUBCATEGORY);
                
                var dto = new SubCategoryDto
                {
                    Id = subCategory.Id,
                    DeletedAt = subCategory.DeletedAt
                };
                return Result<SubCategoryDto>.Ok(dto, "SubCategory restored successfully", 200);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError($"Exception in ReturnRemovedSubCategoryAsync: {ex.Message}");
                NotifyAdminOfError($"Exception in ReturnRemovedSubCategoryAsync: {ex.Message}", ex.StackTrace);
                return Result<SubCategoryDto>.Fail("An error occurred while restoring subcategory", 500);
            }
        }

        public async Task<Result<List<SubCategoryDto>>> GetAllDeletedAsync()
        {
            var cacheKey = $"{CACHE_TAG_SUBCATEGORY}deleted";
            var cached = await _cacheManager.GetAsync<List<SubCategoryDto>>(cacheKey);
            if (cached != null)
            {
                _logger.LogInformation("Cache hit for deleted subcategories");
                return Result<List<SubCategoryDto>>.Ok(cached, "All deleted subcategories from cache", 200);
            }
            var subCategories = await _unitOfWork.SubCategory.GetAll()
                .Where(sc => sc.DeletedAt != null)
                .Include(sc => sc.Images.Where(i => i.DeletedAt == null))
                .Include(sc => sc.Products.Where(p => p.DeletedAt == null))
                .ThenInclude(p => p.Discount)
                .Include(sc => sc.Products.Where(p => p.DeletedAt == null))
                .ThenInclude(p => p.ProductVariants.Where(v => v.DeletedAt == null))
                .Select(sc => new
                {
                    sc.Id,
                    sc.Name,
                    sc.Description,
                    sc.IsActive,
                    sc.CreatedAt,
                    sc.ModifiedAt,
                    sc.DeletedAt,
                    Images = sc.Images.Where(i => i.DeletedAt == null).Select(i => new
                    {
                        i.Id,
                        i.Url,
                        i.IsMain,
                        i.AltText,
                        i.Title,
                        i.Width,
                        i.Height,
                        i.FileSize,
                        i.FileType,
                        i.Folder,
                  
                  
                  
                  
                    }).ToList(),
                    Products = sc.Products.Where(p => p.DeletedAt == null).Select(p => new
                    {
                        p.Id,
                        p.Name,
                        p.Description,
                        p.Quantity,
                        p.Gender,
                        p.SubCategoryId,
                        p.CreatedAt,
                        p.ModifiedAt,
                        p.DeletedAt,
                        Discount = p.Discount != null ? new
                        {
                            p.Discount.Id,
                            p.Discount.Name,
                            p.Discount.Description,
                            p.Discount.DiscountPercent,
                            p.Discount.StartDate,
                            p.Discount.EndDate,
                            p.Discount.IsActive,
                            p.Discount.CreatedAt,
                            p.Discount.ModifiedAt,
                            p.Discount.DeletedAt
                        } : null,
                        ProductVariants = p.ProductVariants.Where(v => v.DeletedAt == null).Select(v => new
                        {
                            v.Id,
                            v.Color,
                            v.Size,
                            v.Price,
                            v.Quantity,
                            v.CreatedAt,
                            v.ModifiedAt,
                            v.DeletedAt
                        }).ToList()
                    }).ToList()
                })
                .ToListAsync();
            if (subCategories == null || !subCategories.Any())
                return Result<List<SubCategoryDto>>.Fail("No deleted subcategories found", 404);
            var dtos = subCategories.Select(sc => new SubCategoryDto
            {
                Id = sc.Id,
                Name = sc.Name,
                Description = sc.Description,
                IsActive = sc.IsActive,
                CreatedAt = sc.CreatedAt,
                ModifiedAt = sc.ModifiedAt,
                DeletedAt = sc.DeletedAt,
                MainImage = sc.Images.FirstOrDefault(i => i.IsMain) != null ? new ImageDto
                {
                    Id = sc.Images.FirstOrDefault(i => i.IsMain).Id,
                    Url = sc.Images.FirstOrDefault(i => i.IsMain).Url
                } : null,
                Images = sc.Images.Where(i => !i.IsMain).Select(i => new ImageDto
                {
                    Id = i.Id,
                    Url = i.Url
                }).ToList(),
                Products = sc.Products.Select(p => new ProductDto
                {
                    Id = p.Id,
                    Name = p.Name,
                    Description = p.Description,
                    AvailableQuantity = p.Quantity,
                    Gender = p.Gender,
                    SubCategoryId = p.SubCategoryId,
                    MinPrice = p.ProductVariants?.Any() == true ? p.ProductVariants.Min(v => v.Price) : 0,
                    MaxPrice = p.ProductVariants?.Any() == true ? p.ProductVariants.Max(v => v.Price) : 0,
                    FinalPrice = p.ProductVariants?.Any() == true ? 
                        (p.Discount != null && p.Discount.IsActive ? 
                            p.ProductVariants.Min(v => v.Price) * (1 - p.Discount.DiscountPercent / 100) : 
                            p.ProductVariants.Min(v => v.Price)) : 0,
                    HasVariants = p.ProductVariants?.Any() == true,
                    TotalVariants = p.ProductVariants?.Count ?? 0,
                    Discount = p.Discount != null ? new DtoModels.DiscoutDtos.DiscountDto
                    {
                        Id = p.Discount.Id,
                        Name = p.Discount.Name,
                        Description = p.Discount.Description,
                        DiscountPercent = p.Discount.DiscountPercent,
                        StartDate = p.Discount.StartDate,
                        EndDate = p.Discount.EndDate,
                        IsActive = p.Discount.IsActive
                    } : null,
                    Images = new List<ImageDto>(), // Products don't have images in this projection
                    Variants = p.ProductVariants?.Select(v => new ProductVariantDto
                    {
                        Id = v.Id,
                        Color = v.Color,
                        Size = v.Size,
                        Price = v.Price,
                        Quantity = v.Quantity
                    }).ToList() ?? new List<ProductVariantDto>()
                }).ToList()
            }).ToList();
            await _cacheManager.SetAsync(cacheKey, dtos, tags: new[] { CACHE_TAG_SUBCATEGORY });
            return Result<List<SubCategoryDto>>.Ok(dtos, "All deleted subcategories", 200);
        }

        public async Task<Result<SubCategoryDto>> UpdateAsync(int subCategoryId, UpdateSubCategoryDto subCategory, string userid)
        {
            _logger.LogInformation($"Executing {nameof(UpdateAsync)} for subCategoryId: {subCategoryId}");


            var existingSubCategory = await _unitOfWork.SubCategory.GetAll()
                .Where(sc => sc.Id == subCategoryId && sc.DeletedAt == null)
                .Include(sc => sc.Images.Where(i => i.DeletedAt == null)).AsTracking()
                .FirstOrDefaultAsync();
                
            if (existingSubCategory == null)
            {
                return Result<SubCategoryDto>.Fail($"SubCategory with id {subCategoryId} not found", 404);
            }
            
            using var transaction = await _unitOfWork.BeginTransactionAsync();
            try
            {
                List<string> warnings = new List<string>();
                bool hasChanges = false;

                if (subCategory == null)
                {
                    return Result<SubCategoryDto>.Fail("Update data is required", 400);
                }

                _logger.LogInformation($"Update data received - Name: '{subCategory.Name}', Description: '{subCategory.Description}', CategoryId: {subCategory.CategoryId}, IsActive: {subCategory.IsActive}");
                _logger.LogInformation($"Image data - NewMainImage: {subCategory.NewMainImage != null}, NewImages count: {subCategory.newImages?.Count ?? 0}, KeptImages count: {subCategory.KeptImages?.Count ?? 0}");

          
                // Handle image removal based on KeptImages
                if (subCategory.KeptImages != null && subCategory.KeptImages.Count > 0)
                {
                    var imagesToRemove = existingSubCategory.Images
                        .Where(img => !subCategory.KeptImages.Contains(img.Id) && !img.IsMain)
                        .ToList();
                    
                    foreach (var img in imagesToRemove)
                    {
                        _logger.LogInformation($"Removing image {img.Id} from subcategory {subCategoryId}");
                        var deleteResult = await _imagesServices.DeleteImageAsync(img);
                        if (!deleteResult.Success)
                        {
                            _logger.LogWarning($"Failed to delete image {img.Id}: {deleteResult.Message}");
                            warnings.Add($"Failed to delete image {img.Id}: {deleteResult.Message}");
                        }
                        existingSubCategory.Images.Remove(img);
                    }
                    
                    if (imagesToRemove.Any())
                    {
                        hasChanges = true;
                    }
                }

                // Handle new images
                if (subCategory.newImages != null && subCategory.newImages.Count != 0)
                {
                    var imageResult = await _imagesServices.SaveSubCategoryImagesAsync(subCategory.newImages, userid);
                    if (imageResult == null || imageResult.Data == null || imageResult.Data.Count == 0)
                    {
                        _logger.LogWarning("All new images failed to save");
                        warnings.Add("All new images failed to save.");
                    }
                    else
                    {
                        if (imageResult.Warnings != null && imageResult.Warnings.Any())
                        {
                            warnings.AddRange(imageResult.Warnings);
                            _logger.LogWarning($"Some new images had issues: {string.Join(", ", imageResult.Warnings)}");
                        }

                        foreach (var img in imageResult.Data)
                        {
                            existingSubCategory.Images.Add(img);
                        }
                        hasChanges = true;
                    }
                }

                if (subCategory.NewMainImage != null)
                {
                    var mainimage = existingSubCategory.Images.FirstOrDefault(i => i.IsMain && i.DeletedAt == null);
                    var issaved = await _imagesServices.SaveMainSubCategoryImageAsync(subCategory.NewMainImage, userid);
                    if (issaved == null || !issaved.Success || issaved.Data == null)
                    {
                        _logger.LogWarning("Failed to save new main image");
                        warnings.Add("Failed to save new main image");
                    }
                    else
                    {
                        existingSubCategory.Images.Add(issaved.Data);
                        if (mainimage != null)
                        {
                            _ = _imagesServices.DeleteImageAsync(mainimage);
                        }
                        hasChanges = true;
                    }
                }

                if (!string.IsNullOrWhiteSpace(subCategory.Name?.Trim()) && subCategory.Name.Trim() != existingSubCategory.Name)
                {
                    var trimmedName = subCategory.Name.Trim();
                    
                    // Validate name format
                    var nameRegex = new System.Text.RegularExpressions.Regex(@"^[a-zA-Z0-9][a-zA-Z0-9\s\-,]*[a-zA-Z0-9]$");
                    if (!nameRegex.IsMatch(trimmedName))
                    {
                        warnings.Add($"Name '{trimmedName}' does not match the required format. Name will not be changed.");
                        _logger.LogWarning($"Name update skipped - invalid format '{trimmedName}'");
                    }
                    else if (trimmedName.Length < 5 || trimmedName.Length > 20)
                    {
                        warnings.Add($"Name '{trimmedName}' must be between 5 and 20 characters. Name will not be changed.");
                        _logger.LogWarning($"Name update skipped - invalid length '{trimmedName}'");
                    }
                    else
                    {
                        _logger.LogInformation($"Updating name from '{existingSubCategory.Name}' to '{trimmedName}'");
                        var isexist = await _unitOfWork.SubCategory.GetAll()
                            .Where(sc => sc.Name == trimmedName && sc.Id != subCategoryId && sc.DeletedAt == null)
                            .AnyAsync();
                        
                        if (isexist)
                        {
                            warnings.Add($"SubCategory with name '{trimmedName}' already exists. Name will not be changed.");
                            _logger.LogWarning($"Name update skipped - duplicate name '{trimmedName}'");
                        }
                        else
                        {
                            existingSubCategory.Name = trimmedName;
                            hasChanges = true;
                            _logger.LogInformation($"Name updated successfully to '{trimmedName}'");
                        }
                    }
                }

               
                if (subCategory.CategoryId.HasValue && subCategory.CategoryId.Value != existingSubCategory.CategoryId)
                {
                    _logger.LogInformation($"Updating CategoryId from {existingSubCategory.CategoryId} to {subCategory.CategoryId.Value}");
                    var category = await _unitOfWork.Category.GetByIdAsync(subCategory.CategoryId.Value);
                    if (category == null)
                    {
                        warnings.Add($"Category with id {subCategory.CategoryId.Value} not found. Category will not be changed.");
                        _logger.LogWarning($"Category update skipped - category {subCategory.CategoryId.Value} not found");
                    }
                    else
                    {
                        existingSubCategory.CategoryId = subCategory.CategoryId.Value;
                        hasChanges = true;
                        _logger.LogInformation($"CategoryId updated successfully to {subCategory.CategoryId.Value}");
                    }
                }

                if (!string.IsNullOrWhiteSpace(subCategory.Description?.Trim()) && subCategory.Description.Trim() != existingSubCategory.Description)
                {
                    var trimmedDescription = subCategory.Description.Trim();
                    
                    // Validate description format
                    var descRegex = new System.Text.RegularExpressions.Regex(@"^[\w\s.,\-()'\""]{0,500}$");
                    if (!descRegex.IsMatch(trimmedDescription))
                    {
                        warnings.Add($"Description '{trimmedDescription}' does not match the required format. Description will not be changed.");
                        _logger.LogWarning($"Description update skipped - invalid format '{trimmedDescription}'");
                    }
                    else if (trimmedDescription.Length < 10 || trimmedDescription.Length > 50)
                    {
                        warnings.Add($"Description '{trimmedDescription}' must be between 10 and 50 characters. Description will not be changed.");
                        _logger.LogWarning($"Description update skipped - invalid length '{trimmedDescription}'");
                    }
                    else
                    {
                        _logger.LogInformation($"Updating description from '{existingSubCategory.Description}' to '{trimmedDescription}'");
                        existingSubCategory.Description = trimmedDescription;
                        hasChanges = true;
                        _logger.LogInformation("Description updated successfully");
                    }
                }

              
                if (subCategory.IsActive != existingSubCategory.IsActive)
                {
                    _logger.LogInformation($"Updating IsActive from {existingSubCategory.IsActive} to {subCategory.IsActive}");
                    existingSubCategory.IsActive = subCategory.IsActive;
                    hasChanges = true;
                    _logger.LogInformation("IsActive updated successfully");
                }

              
                if (hasChanges)
                {
                    existingSubCategory.ModifiedAt = DateTime.UtcNow;
                    _logger.LogInformation($"SubCategory {subCategoryId} has changes, updating ModifiedAt timestamp");
                    _logger.LogInformation($"Final entity state - Name: '{existingSubCategory.Name}', Description: '{existingSubCategory.Description}', CategoryId: {existingSubCategory.CategoryId}, IsActive: {existingSubCategory.IsActive}, ModifiedAt: {existingSubCategory.ModifiedAt}");
                    
                    // Log that changes will be committed
                    _logger.LogInformation($"Changes will be committed to database for SubCategory {subCategoryId}");
                }
                else
                {
                    _logger.LogInformation($"No changes detected for SubCategory {subCategoryId}");
                }

                _unitOfWork.SubCategory.Update(existingSubCategory);
                var adminLog = await _adminopreationservices.AddAdminOpreationAsync(
                    $"Updated SubCategory {subCategoryId}",
                    Opreations.UpdateOpreation,
                    userid,
                    subCategoryId
                );

                if (!adminLog.Success)
                {
                    _logger.LogWarning($"Failed to log admin operation: {adminLog.Message}");
                }
                await _unitOfWork.CommitAsync();
				await transaction.CommitAsync();

                await _cacheManager.RemoveByTagAsync(CACHE_TAG_SUBCATEGORY);

                _logger.LogInformation($"Successfully updated SubCategory {subCategoryId}");
                var dto = _mapping.Map<SubCategoryDto>(existingSubCategory);
                return Result<SubCategoryDto>.Ok(dto, "Updated", 200, warnings: warnings);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError($"Exception in UpdateAsync: {ex.Message}");
                NotifyAdminOfError($"Exception in UpdateAsync for subcategory {subCategoryId}: {ex.Message}", ex.StackTrace);
                return Result<SubCategoryDto>.Fail("An error occurred during update", 500);
            }
        }

		public async Task<Result<ImageDto>> AddMainImageToSubCategoryAsync(int subCategoryId, IFormFile mainImage, string userId)
		{
			_logger.LogInformation($"Executing {nameof(AddMainImageToSubCategoryAsync)} for subCategoryId: {subCategoryId}");
			if (mainImage == null || mainImage.Length == 0)
			{
				return Result<ImageDto>.Fail("Main image is required.", 400);
			}
			var subCategory = await _unitOfWork.SubCategory.GetAll().AsTracking()
				.Where(sc => sc.Id == subCategoryId && sc.DeletedAt == null)
				.Include(sc => sc.Images.Where(i => i.DeletedAt == null))
				.FirstOrDefaultAsync();
			if (subCategory == null)
			{
				return Result<ImageDto>.Fail($"SubCategory with id {subCategoryId} not found", 404);
			}
			
			using var transaction = await _unitOfWork.BeginTransactionAsync();
			try
			{
				var existingMainImage = subCategory.Images.FirstOrDefault(i => i.IsMain && i.DeletedAt == null);
				if (existingMainImage != null)
				{
					_logger.LogInformation($"Removing existing main image with ID {existingMainImage.Id} from subcategory {subCategoryId}");
					var deleteResult = await _imagesServices.DeleteImageAsync(existingMainImage);
					if (!deleteResult.Success)
					{
						_logger.LogError($"Failed to delete existing main image: {deleteResult.Message}");
						await transaction.RollbackAsync();
						return Result<ImageDto>.Fail(deleteResult.Message, deleteResult.StatusCode, deleteResult.Warnings);
					}
					subCategory.Images.Remove(existingMainImage);
				}
				
				var mainImageResult = await _imagesServices.SaveMainSubCategoryImageAsync(mainImage, userId);
				if (mainImageResult == null || !mainImageResult.Success || mainImageResult.Data == null)
				{
					await transaction.RollbackAsync();
					return Result<ImageDto>.Fail(mainImageResult?.Message ?? "Failed to save main image", mainImageResult?.StatusCode ?? 500, mainImageResult?.Warnings);
				}
				
				subCategory.Images.Add(mainImageResult.Data);
                _unitOfWork.SubCategory.Update(subCategory);
				
				var adminLog = await _adminopreationservices.AddAdminOpreationAsync(
					$"Added main image to SubCategory {subCategoryId}",
					Opreations.UpdateOpreation,
					userId,
					subCategoryId
				);
				
				if (!adminLog.Success)
				{
					_logger.LogWarning($"Failed to log admin operation: {adminLog.Message}");
				}
				
				await _unitOfWork.CommitAsync();
				await transaction.CommitAsync();
				
				var mapped = _mapping.Map<ImageDto>(mainImageResult.Data);
                await _cacheManager.RemoveByTagAsync(CACHE_TAG_SUBCATEGORY);
				return Result<ImageDto>.Ok(mapped, "Main image added to subcategory", 200);
			}
			catch (Exception ex)
			{
				await transaction.RollbackAsync();
				_logger.LogError($"Exception in AddMainImageToSubCategoryAsync: {ex.Message}");
				NotifyAdminOfError($"Exception in AddMainImageToSubCategoryAsync: {ex.Message}", ex.StackTrace);
				return Result<ImageDto>.Fail("An error occurred while adding main image", 500);
			}
		}

		public async Task<Result<SubCategoryDto>> RemoveImageFromSubCategoryAsync(int subCategoryId, int imageId, string userId)
        {
            _logger.LogInformation($"Removing image {imageId} from subcategory: {subCategoryId}");
            
            using var transaction = await _unitOfWork.BeginTransactionAsync();
            try
            {
                var subCategory = await _unitOfWork.SubCategory.GetAll().AsTracking()
                    .Where(sc => sc.Id == subCategoryId && sc.DeletedAt == null)
                    .Include(sc => sc.Images.Where(i => i.DeletedAt == null))
                    .FirstOrDefaultAsync();
                if (subCategory == null)
                {
                    await transaction.RollbackAsync();
                    return Result<SubCategoryDto>.Fail($"SubCategory with id {subCategoryId} not found", 404);
                }
                
                var image = subCategory.Images.FirstOrDefault(i => i.Id == imageId);
                if (image == null)
                {
                    await transaction.RollbackAsync();
                    return Result<SubCategoryDto>.Fail("Image not found", 404);
                }
                
                // Delete the image file first
                var deleteResult = await _imagesServices.DeleteImageAsync(image);
                if (!deleteResult.Success)
                {
                    _logger.LogError($"Failed to delete image file: {deleteResult.Message}");
                    await transaction.RollbackAsync();
                    return Result<SubCategoryDto>.Fail(deleteResult.Message, deleteResult.StatusCode, deleteResult.Warnings);
                }
                
                subCategory.Images.Remove(image);
                _unitOfWork.SubCategory.Update(subCategory);
                
                var adminLog = await _adminopreationservices.AddAdminOpreationAsync(
                    $"Remove Image {imageId} from SubCategory {subCategoryId}",
                    Opreations.UpdateOpreation,
                    userId,
                    subCategoryId
                );
                
                if (!adminLog.Success)
                {
                    _logger.LogWarning($"Failed to log admin operation: {adminLog.Message}");
                }
                
                await _unitOfWork.CommitAsync();
                await transaction.CommitAsync();
                await _cacheManager.RemoveByTagAsync(CACHE_TAG_SUBCATEGORY);
                
                var subCategoryDto = _mapping.Map<SubCategoryDto>(subCategory);
                return Result<SubCategoryDto>.Ok(subCategoryDto, "Image removed successfully", 200);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, $"Unexpected error in RemoveImageFromSubCategoryAsync for subcategory {subCategoryId}");
                NotifyAdminOfError(ex.Message, ex.StackTrace);
                return Result<SubCategoryDto>.Fail("Unexpected error occurred while removing image", 500);
            }
        }

        public async Task<Result<bool>> ChangeActiveStatus(int subCategoryId, string userId)
        {
            _logger.LogInformation($"Executing {nameof(ChangeActiveStatus)} for subCategoryId: {subCategoryId}");
            
            using var transaction = await _unitOfWork.BeginTransactionAsync();
            try
            {
                var subCategory = await _unitOfWork.SubCategory.GetAll()
                    .Where(sc => sc.Id == subCategoryId && sc.DeletedAt == null)
                    .Select(sc => new { sc.Id, sc.IsActive })
                    .FirstOrDefaultAsync();
                    
                if (subCategory == null)
                {
                    await transaction.RollbackAsync();
                    return Result<bool>.Fail($"SubCategory with id {subCategoryId} not found", 404);
                }

                var newActiveStatus = !subCategory.IsActive;
                
           
                var updateResult = await _unitOfWork.SubCategory.GetAll()
                    .Where(sc => sc.Id == subCategoryId)
                    .ExecuteUpdateAsync(s => s.SetProperty(sc => sc.IsActive, newActiveStatus));
                
                if (updateResult == 0)
                {
                    await transaction.RollbackAsync();
                    return Result<bool>.Fail($"Failed to update SubCategory {subCategoryId}", 500);
                }
                
                var adminLog = await _adminopreationservices.AddAdminOpreationAsync(
                    $"Changed active status of SubCategory {subCategoryId} to {newActiveStatus}",
                    Opreations.UpdateOpreation,
                    userId,
                    subCategoryId
                );
                
                if (!adminLog.Success)
                {
                    _logger.LogWarning($"Failed to log admin operation: {adminLog.Message}");
                }

                await _unitOfWork.CommitAsync();
                await transaction.CommitAsync();
                await _cacheManager.RemoveByTagAsync(CACHE_TAG_SUBCATEGORY);

                return Result<bool>.Ok(newActiveStatus, $"SubCategory active status changed to {newActiveStatus}", 200);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError($"Exception in ChangeActiveStatus: {ex.Message}");
                NotifyAdminOfError($"Exception in ChangeActiveStatus: {ex.Message}", ex.StackTrace);
                return Result<bool>.Fail("An error occurred while changing active status", 500);
            }
        }
    }
} 