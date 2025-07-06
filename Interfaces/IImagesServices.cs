using E_Commers.Models;
using E_Commers.Services;

namespace E_Commers.Interfaces
{
	public interface IImagesServices
	{
		bool IsValidExtension(string extension);
		public Task<Result<List<string>>> DeleteImagesAsync(List<Image> images);
		string GetFolderPath(params string[] folders);
		Task<Result<Image>> SaveCustomerImageAsync(IFormFile image, string userId);
		
		// Single image methods
		Task<Result<Image>> SaveCategoryImageAsync(IFormFile image, string userId);
		Task<Result<Image>> SaveProductImageAsync(IFormFile image, string userId);
		Task<Result<Image>> SaveSubCategoryImageAsync(IFormFile image, string userId);
		
		// Multiple images methods
		Task<Result<List<Image>>> SaveCategoryImagesAsync(List<IFormFile> images, string userId);
		Task<Result<List<Image>>> SaveProductImagesAsync(List<IFormFile> images, string userId);
		Task<Result<List<Image>>> SaveSubCategoryImagesAsync(List<IFormFile> images, string userId);
		
		// Main image methods
		Task<Result<Image>> SaveMainCategoryImageAsync(IFormFile image, string userId);
		Task<Result<Image>> SaveMainProductImageAsync(IFormFile image, string userId);
		Task<Result<Image>> SaveMainSubCategoryImageAsync(IFormFile image, string userId);
		
		Task<Result<string>> DeleteImageAsync(Image image);
	}
}
