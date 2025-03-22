using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace E_Commers.Helper
{
	public class ImagesHelper
	{
		private readonly ILogger<ImagesHelper> _logger;

		public ImagesHelper(ILogger<ImagesHelper> logger)
		{
			_logger = logger;
		}

		public async Task<string> SaveImageForCustomerAsync(IFormFile? image)
		{
			_logger.LogInformation($"In {nameof(SaveImageForCustomerAsync)} Method");

			if (image is null)
			{
				_logger.LogWarning("Image is null");
				return string.Empty;
			}

			try
			{
				string[] allowedExtensions = { ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".webp" };
				string extension = Path.GetExtension(image.FileName).ToLower();

				if (!allowedExtensions.Contains(extension))
				{
					_logger.LogWarning($"Invalid file extension: {extension}");
					return string.Empty;
				}


				string folderPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "CustomerPhotos");

				if (!Directory.Exists(folderPath))
				{
					_logger.LogWarning($"Folder not found, creating: {folderPath}");
					Directory.CreateDirectory(folderPath);
				}


				string uniqueName = Guid.NewGuid().ToString() + extension;
				string filePath = Path.Combine(folderPath, uniqueName);

				using (var stream = new FileStream(filePath, FileMode.Create))
				{
					await image.CopyToAsync(stream);
				}

				_logger.LogInformation($"Photo saved successfully: {filePath}");
				return $"/CustomerPhotos/{uniqueName}";
			}
			catch (Exception ex)
			{
				_logger.LogError($"Error saving image: {ex.Message}");
				return string.Empty;
			}
		}
	}
}
