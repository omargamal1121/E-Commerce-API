using E_Commers.DtoModels.CategoryDtos;
using E_Commers.DtoModels.ImagesDtos;
using E_Commers.DtoModels.Responses;
using E_Commers.Services;
using Microsoft.AspNetCore.Http;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace E_Commers.Interfaces
{
    public interface ISubCategoryServices
    {
        Task<Result<string>> IsExsistAsync(int id);

        public Task<Result<List<SubCategoryDto>>> FilterAsync(
    string? search,
    bool? isActive,
    bool includeDeleted,
    int page,
    int pageSize,
    string userRole);

		Task<Result<List<SubCategoryDto>>> SearchAsync(string key, bool isactivefilter);
        Task<Result<SubCategoryDto>> CreateAsync(CreateSubCategoryDto subCategory, string userid);
        Task<Result<SubCategoryDto>> GetSubCategoryByIdAsync(int id, bool isActive = true, bool includeDeleted = false);
        Task<Result<string>> DeleteAsync(int id, string userid);
        Task<Result<SubCategoryDto>> UpdateAsync(int subCategoryId, UpdateSubCategoryDto subCategory, string userid);
        Task<Result<List<SubCategoryDto>>> GetAllDeletedAsync();
        Task<Result<SubCategoryDto>> ReturnRemovedSubCategoryAsync(int id, string userid);
        Task<Result<List<ImageDto>>> AddImagesToSubCategoryAsync(int subCategoryId, List<IFormFile> images, string userId);
        Task<Result<ImageDto>> AddMainImageToSubCategoryAsync(int subCategoryId, IFormFile mainImage, string userId);
        Task<Result<SubCategoryDto>> RemoveImageFromSubCategoryAsync(int subCategoryId, int imageId, string userId);
        Task<Result<bool>> ChangeActiveStatus(int subCategoryId, string userId);
    }
} 