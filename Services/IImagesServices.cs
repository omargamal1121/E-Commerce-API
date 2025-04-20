namespace E_Commers.Services
{
	public interface IImagesServices
	{
		bool IsValidExtension(string extension);
		string GetFolderPath(params string[] folders);
		Task<Result<string>> SaveImageAsync(IFormFile image, string folderName);
		Task<Result<List<string>>> SaveImagesAsync(IFormFileCollection images, string folderName);
	}
}
