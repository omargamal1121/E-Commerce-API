using AutoMapper;
using E_Commers.DtoModels;
using E_Commers.DtoModels.CategoryDtos;
using E_Commers.DtoModels.ProductDtos;
using E_Commers.DtoModels.Responses;
using E_Commers.Enums;
using E_Commers.ErrorHnadling;
using E_Commers.Interfaces;
using E_Commers.Mappings;
using E_Commers.Models;
using E_Commers.Services.AdminOpreationServices;
using E_Commers.Services.Cache;
using E_Commers.UOW;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;

namespace E_Commers.Services.Category
{
    public class CategoryServices : ICategoryServices
    {
        private readonly ILogger<CategoryServices> _logger;
        private readonly IMapper _mapping;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IAdminOpreationServices  _adminopreationservices;
        private readonly ICacheManager _cacheManager;
        private const string CACHE_TAG_CATEGORY = "category";

        public CategoryServices(
			IAdminOpreationServices adminopreationservices,
			ICacheManager cacheManager,
            IMapper mapping,
            IUnitOfWork unitOfWork,
            ILogger<CategoryServices> logger
        )
        {
            _adminopreationservices = adminopreationservices;
            _cacheManager = cacheManager;
            _mapping = mapping;
            _logger = logger;
            _unitOfWork = unitOfWork;
        }
        public async Task<ApiResponse<string>> IsExsistAsync(int id) 
        {
            _logger.LogInformation($"Execute:{nameof(IsExsistAsync)} in Category Services");
            return await _unitOfWork.Category.GetByIdAsync(id) is null ?
                ApiResponse<string>.CreateErrorResponse(new ErrorResponse("Id", $"No Categoty with this id:{id}"), 404)
                :
                ApiResponse<string>.CreateSuccessResponse("category Exsist");
                ;

        }

        public async Task<ApiResponse<CategoryDto>> GetCategoryByIdAsync(int id)
        {
            _logger.LogInformation($"Execute:{nameof(GetCategoryByIdAsync)} in services");
            
            var cacheKey = $"{CACHE_TAG_CATEGORY}id:{id}";
            var cachedCategory = await _cacheManager.GetAsync<CategoryDto>(cacheKey);
            
            if (cachedCategory != null)
            {
                _logger.LogInformation($"Cache hit for category {id}");
                return ApiResponse<CategoryDto>.CreateSuccessResponse("Category found in cache", cachedCategory);
            }

            var isfound = await _unitOfWork.Category.GetByIdAsync(id);
            if (!isfound.Success || isfound.Data is null)
            {
                _logger.LogWarning(isfound.Message);
                return ApiResponse<CategoryDto>.CreateErrorResponse(
                    new ErrorResponse("Category id", isfound.Message),
                    409
                );
            }

            var categoryDto = _mapping.Map<CategoryDto>(isfound.Data);
            await _cacheManager.SetAsync(cacheKey, categoryDto, tags: new[] { CACHE_TAG_CATEGORY });
            
            return ApiResponse<CategoryDto>.CreateSuccessResponse("Category found", categoryDto);
        }

        public async Task<ApiResponse<CategoryDto>> CreateAsync(CreateCategotyDto model, string userid)
        {
            _logger.LogInformation($"Execute {nameof(CreateAsync)}");

            // Validate input
            if (string.IsNullOrWhiteSpace(model.Name))
            {
                return ApiResponse<CategoryDto>.CreateErrorResponse(
                    new ErrorResponse("Category Name", "Category name cannot be empty"),
                    400
                );
            }

            // Check for existing category
            var checkname = await _unitOfWork.Category.GetByNameAsync(model.Name);
            if (checkname.Success)
            {
                return ApiResponse<CategoryDto>.CreateErrorResponse(
                    new ErrorResponse(
                        "Category Name",
                        $"There's already a category with the name: {model.Name}"
                    ),
                    409
                );
            }

            using var transaction = await _unitOfWork.BeginTransactionAsync();
            try
            {
                // Create category
                var category = new Models.Category
                {
                    Name = model.Name.Trim(),
                    Description = model.Description?.Trim() ?? string.Empty,
                };

                var iscreated = await _unitOfWork.Category.CreateAsync(category);
                if (!iscreated.Success)
                {
                    _logger.LogWarning(iscreated.Message);
                    await transaction.RollbackAsync();
                    return ApiResponse<CategoryDto>.CreateErrorResponse(
                        new ErrorResponse(
                            "Server Error",
                            "Can't Create Category now... try again later"
                        ),
                        500
                    );
                }
				await _unitOfWork.CommitAsync();

				// Record admin operation
				var adminopreation = await _adminopreationservices.AddAdminOpreationAsync(
                    "Add Category", 
                    Opreations.AddOpreation, 
                    userid,
                    category.Id
                );

                if (!adminopreation.Success)
                {
                    _logger.LogError("Failed to add admin operation");
                    await transaction.RollbackAsync();
                    return ApiResponse<CategoryDto>.CreateErrorResponse(
                        new ErrorResponse("Server Error", "Try Again later"), 
                        500
                    );
                }

                // Commit changes
                await _unitOfWork.CommitAsync();
                await transaction.CommitAsync();
                await _cacheManager.RemoveByTagAsync(CACHE_TAG_CATEGORY);
                
                var categoryDto = _mapping.Map<CategoryDto>(category);
                return ApiResponse<CategoryDto>.CreateSuccessResponse("Created", categoryDto, 201);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError($"Exception in CreateAsync: {ex.Message}");
                return ApiResponse<CategoryDto>.CreateErrorResponse(
                    new ErrorResponse(
                        "Server Error",
                        "Can't Create Category now... try again later"
                    ),
                    500
                );
            }
        }

        public async Task<ApiResponse<string>> DeleteAsync(int categoryId, string userid)
        {
            _logger.LogInformation($"Execute:{nameof(DeleteAsync)} in services");

            var isfound = await _unitOfWork.Category.GetByIdAsync(categoryId);
            if (!isfound.Success || isfound.Data is null)
            {
                return ApiResponse<string>.CreateErrorResponse(
                    new ErrorResponse("Category Id", isfound.Message),
                    404
                );
            }

         //   var products = await _unitOfWork.Category.IsHasProductAsync(categoryId);
            if (true)
            {
                _logger.LogWarning("Category Contain Products");
                return ApiResponse<string>.CreateErrorResponse(
                    new ErrorResponse(
                        "Category Products",
                        "Can't delete category bec it has products"
                    ),
                    400
                );
            }
            var transaction= await _unitOfWork.BeginTransactionAsync();
            isfound.Data.DeletedAt = DateTime.Now;

            var isdeleted = await _unitOfWork.Category.UpdateAsync(isfound.Data);
            if (!isdeleted.Success)
            {
                _logger.LogError(isdeleted.Message);
				//send mail to me
				await transaction.RollbackAsync();
				return ApiResponse<string>.CreateErrorResponse(
                    new ErrorResponse(
                        "Server Error",
                        "Can't delete Category now... try again later"
                    ),
                    500
                );
            }

            var isadded = await _adminopreationservices.AddAdminOpreationAsync($"Soft Delete for category", Opreations.DeleteOpreation, userid, categoryId);
            if(!isadded.Success)
            {
                _logger.LogError(isadded.Message);
                await transaction.RollbackAsync();
                return ApiResponse<string>.CreateErrorResponse(new ErrorResponse("Server Error", "Try Again later"), 500);
            }

            await _unitOfWork.CommitAsync();
			await transaction.CommitAsync();
			await _cacheManager.RemoveByTagAsync(CACHE_TAG_CATEGORY);
            
            return ApiResponse<string>.CreateSuccessResponse("Deleted");
        }

        public async Task<ApiResponse<List<CategoryDto>>> GetAllAsync()
        {
            var cacheKey = $"{CACHE_TAG_CATEGORY}all";
            var cachedCategories = await _cacheManager.GetAsync<List<CategoryDto>>(cacheKey);
            
            if (cachedCategories != null)
            {
                _logger.LogInformation("Cache hit for all categories");
                return ApiResponse<List<CategoryDto>>.CreateSuccessResponse("All categories from cache", cachedCategories);
            }

            var categories = await _unitOfWork.Category.GetAllAsync();
            if (!categories.Success || categories.Data is null)
                return ApiResponse<List<CategoryDto>>.CreateErrorResponse(
                    new ErrorResponse("Category", categories.Message),
                    404
                );

            var categoriesDto = await categories.Data.Where(c => c.DeletedAt == null).Select(c => _mapping.Map<CategoryDto>(c)).ToListAsync();

            await _cacheManager.SetAsync(cacheKey, categoriesDto, tags: new[] { CACHE_TAG_CATEGORY });
            
            return ApiResponse<List<CategoryDto>>.CreateSuccessResponse("All categories", categoriesDto);
        }
        public async Task<ApiResponse<CategoryDto>> ReturnRemovedCategoryAsync(int id, string userid)
        {
			
			var isfound = await _unitOfWork.Category.GetByIdAsync(id);

            if (!isfound.Success||isfound.Data is null||isfound.Data.DeletedAt ==null){
                _logger.LogWarning( $"Can't Found Category with this id:{id}");
                return ApiResponse<CategoryDto>.CreateErrorResponse(new ErrorResponse("Category Id", $"Can't Found Category with this id:{id}"), 404);
            }
            try
            {
               var transaction =  await  _unitOfWork.BeginTransactionAsync();
                isfound.Data.DeletedAt = null;
				var isupdate = await _unitOfWork.Category.UpdateAsync(isfound.Data);
				if (!isupdate.Success)
				{
                    _logger.LogError(isupdate.Message);
                    // send email to me
                    await transaction.RollbackAsync();
					return ApiResponse<CategoryDto>.CreateErrorResponse(new ErrorResponse("Server Error", $"Try Again Later"), 500);
				}
				var isadded = await _adminopreationservices.AddAdminOpreationAsync($"Return category form deleted", Opreations.DeleteOpreation, userid, id);
				if (isadded.Success)
				{
					_logger.LogError(isadded.Message);
					await transaction.RollbackAsync();
					return ApiResponse<CategoryDto>.CreateErrorResponse(new ErrorResponse("Server Error", "Try Again later"), 500);
				}

                 await _unitOfWork.CommitAsync();
				await transaction.CommitAsync();

				var categorydto = _mapping.Map<CategoryDto>(isfound.Data);
              await   _cacheManager.RemoveByTagAsync(CACHE_TAG_CATEGORY);
				return ApiResponse<CategoryDto>.CreateSuccessResponse("Updated", categorydto);

			}
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
				return ApiResponse<CategoryDto>.CreateErrorResponse(new ErrorResponse("Server Error", $"Try Again Later"), 500);
			}
         
		}

		public async Task<ApiResponse<List<CategoryDto>>> GetRemovedCategoriesAsync()
		{
			var cacheKey = $"{CACHE_TAG_CATEGORY}GetRemovedCategories";
			var cachedCategories = await _cacheManager.GetAsync<List<CategoryDto>>(cacheKey);

			if (cachedCategories != null)
			{
				_logger.LogInformation("Cache hit for all categories");
				return ApiResponse<List<CategoryDto>>.CreateSuccessResponse("All categories from cache", cachedCategories);
			}

			var categories = await _unitOfWork.Category.GetAllAsync();
			if (!categories.Success || categories.Data is null)
				return ApiResponse<List<CategoryDto>>.CreateErrorResponse(
					new ErrorResponse("Category", categories.Message),
					404
				);

			var categoriesDto = await categories.Data.Where(c => c.DeletedAt != null).Select(c => _mapping.Map<CategoryDto>(c)).ToListAsync();

			await _cacheManager.SetAsync(cacheKey, categoriesDto, tags: new[] { CACHE_TAG_CATEGORY });

			return ApiResponse<List<CategoryDto>>.CreateSuccessResponse("All categories", categoriesDto);
		}


		public async Task<ApiResponse<List<CategoryDto>>> GetAllDeleted()
        {
            var cacheKey = $"{CACHE_TAG_CATEGORY}deleted"; 
            var cachedCategories = await _cacheManager.GetAsync<List<CategoryDto>>(cacheKey);
            
            if (cachedCategories != null)
            {
                _logger.LogInformation("Cache hit for deleted categories");
                return ApiResponse<List<CategoryDto>>.CreateSuccessResponse("All deleted categories from cache", cachedCategories);
            }

            var categories = await _unitOfWork.Category.GetAllAsync();
            if (!categories.Success || categories.Data is null)
                return ApiResponse<List<CategoryDto>>.CreateErrorResponse(
                    new ErrorResponse("Category", categories.Message),
                    404
                );

            var categoriesDto = await categories.Data
                .Select(c => _mapping.Map<CategoryDto>(c))
                .Where(c => c.DeletedAt != null)
                .ToListAsync();

            await _cacheManager.SetAsync(cacheKey, categoriesDto, tags: new[] { CACHE_TAG_CATEGORY });
            
            return ApiResponse<List<CategoryDto>>.CreateSuccessResponse("All deleted categories", categoriesDto);
        }

        public async Task<ApiResponse<CategoryDto>> UpdateAsync(int categoryId, UpdateCategoryDto category, string userid)
        {
            _logger.LogInformation($"Execute {nameof(UpdateAsync)}");
            var isfound = await _unitOfWork.Category.GetByIdAsync(categoryId);
            if (!isfound.Success || isfound.Data is null)
                return ApiResponse<CategoryDto>.CreateErrorResponse(
                    new ErrorResponse("category id", isfound.Message), 
                    404
                );
            try
            {
				var transaction = await _unitOfWork.BeginTransactionAsync();

                string oldname = isfound.Data.Name;
                string olddes = isfound.Data.Description;
				isfound.Data.Description = category.Description ?? string.Empty;
				isfound.Data.Name = category.Name;

				var isupdated = await _unitOfWork.Category.UpdateAsync(isfound.Data);
				if (!isupdated.Success)
				{
					_logger.LogError(isupdated.Message);
					await transaction.RollbackAsync();
					return ApiResponse<CategoryDto>.CreateErrorResponse(
						new ErrorResponse("server Error", "Try again later"),
						500
					);
				}

				await _unitOfWork.CommitAsync();

				var isadded = await _adminopreationservices.AddAdminOpreationAsync($"Update Category Name From {oldname} to {category.Name}", Opreations.UpdateOpreation, userid, categoryId);
				if (isadded.Success)
				{
					_logger.LogError(isadded.Message);
					await transaction.RollbackAsync();
					return ApiResponse<CategoryDto>.CreateErrorResponse(new ErrorResponse("Server Error", "Try Again later"), 500);
				}

				await _unitOfWork.CommitAsync();
				await transaction.CommitAsync();
				await _cacheManager.RemoveByTagAsync(CACHE_TAG_CATEGORY);

				var afterupdate = _mapping.Map<CategoryDto>(isfound.Data);
				return ApiResponse<CategoryDto>.CreateSuccessResponse("Updated", afterupdate);
			}
            catch (Exception ex)
            {
                _logger.LogError($"{ex.Message}");
				return ApiResponse<CategoryDto>.CreateErrorResponse(new ErrorResponse("Server Error", "Try Again later"), 500);
			}
          
        }
  
    }
}
