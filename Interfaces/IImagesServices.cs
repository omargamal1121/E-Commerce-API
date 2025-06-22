using E_Commers.Models;
using E_Commers.Services;

namespace E_Commers.Interfaces
{
	public interface IImagesServices
	{
		bool IsValidExtension(string extension);
		string GetFolderPath(params string[] folders);
		Task<Result<Image>> SaveImageAsync(IFormFile image, string folderName);
		Result<string> DeleteImage(string folderName,string imagename);
		Task<Result<List<Image>>> SaveImagesAsync(IFormFileCollection images, string folderName);
	}
}
