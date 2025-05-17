using System;
using System.IO;
using System.Threading.Tasks;
using E_Commers.Interfaces;
using E_Commers.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;

namespace E_Commers.Services
{

	public class ImagesServices : IImagesServices
	{
		private readonly ILogger<ImagesServices> _logger;

		public ImagesServices(ILogger<ImagesServices> logger)
		{
			_logger = logger;
		}

		public bool IsValidExtension(string extension)
		{
			string[] allowedExtensions = { ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".webp" };
			return allowedExtensions.Contains(extension.ToLower());
		}

		public string GetFolderPath(params string[] folders)
		{
			string basePath = Directory.GetCurrentDirectory();
			string folderPath = folders.Aggregate(basePath, Path.Combine);

			if (!Directory.Exists(folderPath))
				return string.Empty;

			return folderPath;
		}

		public async Task<Result<string>> SaveImageAsync(IFormFile image, string folderName)
		{
			_logger.LogInformation($"📥 Saving image to {folderName}");

			if (image is null)
				return Result<string>.Fail("Image is null");

			string extension = Path.GetExtension(image.FileName);
			if (!IsValidExtension(extension))
				return Result<string>.Fail($"Invalid file extension: {extension}");

			try
			{
				string folderPath = GetFolderPath("wwwroot", folderName);
				if (folderPath.IsNullOrEmpty())
				{
					_logger.LogError("Path doesn't exsist");
					return Result<string>.Fail("Path doesn't exsist");
				}
				string uniqueName = Guid.NewGuid().ToString() + extension;
				string filePath = Path.Combine(folderPath, uniqueName);

				using (var stream = new FileStream(filePath, FileMode.Create))
				{
					await image.CopyToAsync(stream);
				}

				string relativePath = $"/{folderName}/{uniqueName}";
				_logger.LogInformation($"✅ Image saved: {relativePath}");
				return Result<string>.Ok(relativePath);
			}
			catch (Exception ex)
			{
				_logger.LogError($"❌ Error saving image: {ex.Message}");
				return Result<string>.Fail($"Error saving image: {ex.Message}");
			}
		}

		public async Task<Result<List<string>>> SaveImagesAsync(IFormFileCollection images, string folderName)
		{
			_logger.LogInformation($"📥 Saving {images?.Count} images to {folderName}");

			if (images == null || images.Count == 0)
				return Result<List<string>>.Fail("Images are null or empty");

			var pathsResult = new Result<List<string>>();

			int counter = 1;
			foreach (var image in images)
			{
				var result = await SaveImageAsync(image, folderName);
				if (!result.Success || string.IsNullOrEmpty(result.Data))
				{
					_logger.LogError($"❌ Error with image #{counter}: {result.Message}");
					return Result<List<string>>.Fail($"Error with image #{counter}: {result.Message}");
				}

				pathsResult.Data.Add(result.Data);
				counter++;
			}

			return pathsResult;
		}

		public  Result<string> DeleteImage(string folderName, string imagename)
		{
			_logger.LogInformation($"Execute {nameof(DeleteImage)}");
			string fullpath= GetFolderPath("wwwroot", folderName, imagename);
			if (fullpath.IsNullOrEmpty())
			{
				_logger.LogError("Path doesn't exsist");
				return Result<string>.Fail("Path doesn't exsist");
			}
			try
			{
				File.Delete(fullpath);
				_logger.LogInformation("Deleted");
				return Result<string>.Ok("Deleted");

			}
			catch (Exception ex)
			{

				_logger.LogError(ex.Message);
				return Result<string>.Fail(ex.Message);
			}
		
		}
	}
}
