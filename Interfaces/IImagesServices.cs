using E_Commers.Services;

namespace E_Commers.Interfaces
{
	public interface IImagesServices
	{
		bool IsValidExtension(string extension);
		string GetFolderPath(params string[] folders);
		Task<Result<string>> SaveImageAsync(IFormFile image, string folderName);
		Result<string> DeleteImage(string folderName,string imagename);
		Task<Result<List<string>>> SaveImagesAsync(IFormFileCollection images, string folderName);
	}
}
