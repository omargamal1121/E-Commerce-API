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
		private const int MaxFileSize = 5 * 1024 * 1024; // 5MB
		public ImagesServices(ILogger<ImagesServices> logger)
		{
			_logger = logger;
		}

		public bool IsValidExtension(string extension)
		{
			string[] allowedExtensions = { ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".webp" };
			return allowedExtensions.Contains(extension.ToLower());
		}

		

		public bool IsValidFileSize(long fileSize)
		{
			return fileSize > 0 && fileSize <= MaxFileSize;
		}

		public string GetFolderPath(params string[] folders)
		{
			string basePath = Directory.GetCurrentDirectory();
			string folderPath = folders.Aggregate(basePath, Path.Combine);

			if (!Directory.Exists(folderPath))
				return string.Empty;

			return folderPath;
		}

		public async Task<Result<Image>> SaveImageAsync(IFormFile image, string folderName)
		{
			_logger.LogInformation($"📥 Saving image to {folderName}");

			if (image is null)
				return Result<Image>.Fail("Image is null");

			// Validate file size
			if (!IsValidFileSize(image.Length))
			{
				_logger.LogWarning($"File size exceeds limit: {image.Length} bytes");
				return Result<Image>.Fail($"File size must be between 1 and {MaxFileSize / (1024 * 1024)}MB");
			}

			

			string extension = Path.GetExtension(image.FileName);
			if (!IsValidExtension(extension))
			{
				_logger.LogWarning($"Invalid file extension: {extension}");
				return Result<Image>.Fail($"Invalid file extension. Allowed extensions: .jpg, .jpeg, .png, .gif, .webp");
			}

			try
			{
				string folderPath = GetFolderPath("wwwroot", folderName);
				if (folderPath.IsNullOrEmpty())
				{
					_logger.LogError("Path doesn't exist");
					return Result<Image>.Fail("Path doesn't exist");
				}

				string uniqueName = $"{Guid.NewGuid()}{extension}";
				string filePath = Path.Combine(folderPath, uniqueName);

				using (var stream = new FileStream(filePath, FileMode.Create))
				{
					await image.CopyToAsync(stream);
				}

				string relativePath = $"/{folderName}/{uniqueName}";
				_logger.LogInformation($"✅ Image saved: {relativePath}");

				Image savedImage = new Image 
				{ 
					UploadDate = DateTime.Now,
					Folder = folderName,
					Url = relativePath,
					FileSize = image.Length,
					FileType = image.ContentType
				};

				return Result<Image>.Ok(savedImage);
			}
			catch (Exception ex)
			{
				_logger.LogError($"❌ Error saving image: {ex.Message}");
				return Result<Image>.Fail($"Error saving image: {ex.Message}");
			}
		}

		public async Task<Result<List<Image>>> SaveImagesAsync(IFormFileCollection images, string folderName)
		{
			_logger.LogInformation($"📥 Saving {images?.Count} images to {folderName}");

			if (images == null || images.Count == 0)
				return Result<List<Image>>.Fail("Images are null or empty");

			var pathsResult = new Result<List<Image>>();
			pathsResult.Data = new List<Image>();

			int counter = 1;
			foreach (var image in images)
			{
				var result = await SaveImageAsync(image, folderName);
				if (!result.Success || (result.Data) == null)
				{
					_logger.LogError($"❌ Error with image #{counter}: {result.Message}");
					return Result<List<Image>>.Fail($"Error with image #{counter}: {result.Message}");
				}

				pathsResult.Data.Add(result.Data);
				counter++;
			}

			return pathsResult;
		}

		public Result<string> DeleteImage(string folderName, string imagename)
		{
			_logger.LogInformation($"Execute {nameof(DeleteImage)}");
			string fullpath = GetFolderPath("wwwroot", folderName, imagename);
			if (fullpath.IsNullOrEmpty())
			{
				_logger.LogError("Path doesn't exist");
				return Result<string>.Fail("Path doesn't exist");
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
