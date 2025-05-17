using E_Commers.DtoModels.CategoryDtos;
using E_Commers.DtoModels.Responses;
using E_Commers.Models;
using E_Commers.Services;

namespace E_Commers.Interfaces
{
	public interface ICategoryServices
	{
		public Task<ApiResponse<string>> IsExsistAsync(int id);
		public Task<ApiResponse<CategoryDto>> CreateAsync(CreateCategotyDto categoty, string userid);
		public Task<ApiResponse<CategoryDto>> GetCategoryByIdAsync(int id);
		public  Task<ApiResponse<string>>DeleteAsync(int id, string userid);
		public Task<ApiResponse<CategoryDto>>UpdateAsync(int categoryid,UpdateCategoryDto category, string userid);
		public  Task<ApiResponse<List<CategoryDto>>>GetAllAsync();
		public  Task<ApiResponse<List<CategoryDto>>>GetAllDeleted();
		public  Task<ApiResponse<CategoryDto>> ReturnRemovedCategoryAsync(int id, string userid);

	}
}
