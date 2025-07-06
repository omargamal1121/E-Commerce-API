using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using E_Commers.Interfaces;
using E_Commers.Models;
using Hangfire;
using E_Commers.Services.EmailServices;
using E_Commers.Services.AdminOpreationServices;
using E_Commers.Enums;
using E_Commers.UOW;

namespace E_Commers.Services
{
	public class ImagesServices : IImagesServices
	{
		private readonly ILogger<ImagesServices> _logger;
		private readonly IConfiguration _configuration;
		private readonly IUnitOfWork _unitOfWork;
		private readonly IAdminOpreationServices _adminOpreationServices;
		private readonly IHttpContextAccessor _httpContextAccessor;	

		private int MaxFileSize => _configuration.GetValue<int>("Security:FileUpload:MaxFileSizeMB", 5) * 1024 * 1024;
		private string[] AllowedContentTypes => _configuration.GetSection("Security:FileUpload:AllowedContentTypes").Get<string[]>() ?? new[] { "image/jpeg", "image/jpg", "image/png", "image/gif", "image/webp" };
		private string[] AllowedExtensions => _configuration.GetSection("Security:FileUpload:AllowedExtensions").Get<string[]>() ?? new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
		private readonly byte[][] _fileSignatures = {
			new byte[] { 0xFF, 0xD8, 0xFF },
			new byte[] { 0x89, 0x50, 0x4E, 0x47 },
			new byte[] { 0x47, 0x49, 0x46, 0x38 },
			new byte[] { 0x52, 0x49, 0x46, 0x46 },
		};

		public ImagesServices(IHttpContextAccessor httpContextAccessor,ILogger<ImagesServices> logger, IConfiguration configuration, IUnitOfWork unitOfWork, IAdminOpreationServices adminOpreationServices)
		{
			_httpContextAccessor= httpContextAccessor;
			_logger = logger;
			_configuration = configuration;
			_unitOfWork = unitOfWork;
			_adminOpreationServices = adminOpreationServices;
		}

		public bool IsValidExtension(string extension) => AllowedExtensions.Contains(extension.ToLower());
		public bool IsValidContentType(string contentType) => AllowedContentTypes.Contains(contentType.ToLower());

		public bool IsValidFileSignature(Stream fileStream)
		{
			try
			{
				using var reader = new BinaryReader(fileStream, System.Text.Encoding.UTF8, true);
				var headerBytes = reader.ReadBytes(8);
			
				return _fileSignatures.Any(sig => headerBytes.Take(sig.Length).SequenceEqual(sig));

			}
			
			catch { return false; }
		}

		public bool IsValidFileSize(long fileSize) => fileSize > 0 && fileSize <= MaxFileSize;

		public string GetFolderPath(params string[] folders)
		{
			string basePath = Directory.GetCurrentDirectory();
			string folderPath = folders.Aggregate(basePath, Path.Combine);
			if (!Directory.Exists(folderPath)) Directory.CreateDirectory(folderPath);
			return folderPath;
		}

		private async Task LogAdminOperationAsync(string userId, string description, int itemId)
		{
			await _adminOpreationServices.AddAdminOpreationAsync(description, Opreations.AddOpreation, userId, itemId);
		}

		private void NotifyAdminOfError(string message, string? stackTrace = null)
		{
			BackgroundJob.Enqueue<IErrorNotificationService>(_ => _.SendErrorNotificationAsync(message, stackTrace));
		}

		private async Task<Result<Image>> SaveImageAsync(IFormFile image, string folderName, string? userId = null)
		{
			_logger.LogInformation($"📥 Saving image to {folderName}");
			if (image is null) return Result<Image>.Fail("Image is null");

			if (!IsValidFileSize(image.Length))
				return Result<Image>.Fail($"File size must be between 1 and {MaxFileSize / (1024 * 1024)}MB");
			if (!IsValidContentType(image.ContentType))
				return Result<Image>.Fail($"Invalid content type: {image.ContentType}");
			string extension = Path.GetExtension(image.FileName);
			if (!IsValidExtension(extension))
				return Result<Image>.Fail($"Invalid extension. Allowed: {string.Join(", ", AllowedExtensions)}");

			try
			{
				using var stream = image.OpenReadStream();
				if (!IsValidFileSignature(stream))
					return Result<Image>.Fail("Invalid file format detected");
				stream.Position = 0;

				string folderPath = GetFolderPath("wwwroot", folderName);
				string uniqueName = $"{Guid.NewGuid()}{extension}";
				string filePath = Path.Combine(folderPath, uniqueName);

				using var fileStream = new FileStream(filePath, FileMode.Create);
				await stream.CopyToAsync(fileStream);
				var request = _httpContextAccessor.HttpContext?.Request;

				string relativePath = $"/{folderName}/{uniqueName}";
				string baseUrl = $"{request?.Scheme}://{request?.Host}";
				string fullUrl = $"{baseUrl}/{folderName}/{uniqueName}";

				var savedImage = new Image
				{
					UploadDate = DateTime.Now,
					Folder = folderName,
					Url = fullUrl, 
					FileSize = image.Length,
					FileType = image.ContentType
				};

				var imageRepo = _unitOfWork.Repository<Image>();
				await imageRepo.CreateAsync(savedImage);
				await _unitOfWork.CommitAsync();
			
				if (!string.IsNullOrEmpty(userId)) await LogAdminOperationAsync(userId, $"Uploaded image to {folderName}", savedImage.Id);
				_logger.LogInformation($"✅ Image saved: {relativePath}");
				return Result<Image>.Ok(savedImage);
			}
			catch (Exception ex)
			{
				
				_logger.LogError($"❌ Error saving image: {ex.Message}");
				NotifyAdminOfError(ex.Message, ex.StackTrace);
				return Result<Image>.Fail($"Error saving image: {ex.Message}");
			}
		}
		
		private async Task<Result<Image>> SaveMainImageAsync(IFormFile image, string folderName, string? userId = null)
		{
			_logger.LogInformation($"📥 Saving main image to {folderName}");
			if (image is null) return Result<Image>.Fail("Image is null");

			if (!IsValidFileSize(image.Length))
				return Result<Image>.Fail($"File size must be between 1 and {MaxFileSize / (1024 * 1024)}MB");
			if (!IsValidContentType(image.ContentType))
				return Result<Image>.Fail($"Invalid content type: {image.ContentType}");
			string extension = Path.GetExtension(image.FileName);
			if (!IsValidExtension(extension))
				return Result<Image>.Fail($"Invalid extension. Allowed: {string.Join(", ", AllowedExtensions)}");

			try
			{
				using var stream = image.OpenReadStream();
				if (!IsValidFileSignature(stream))
					return Result<Image>.Fail("Invalid file format detected");
				stream.Position = 0;

				string folderPath = GetFolderPath("wwwroot", folderName);
				string uniqueName = $"{Guid.NewGuid()}{extension}";
				string filePath = Path.Combine(folderPath, uniqueName);

				using var fileStream = new FileStream(filePath, FileMode.Create);
				await stream.CopyToAsync(fileStream);
				var request = _httpContextAccessor.HttpContext?.Request;
				string relativePath = $"/{folderName}/{uniqueName}";
				string baseUrl = $"{request?.Scheme}://{request?.Host}";
				string fullUrl = $"{baseUrl}/{folderName}/{uniqueName}";

				var savedImage = new Image
				{
					UploadDate = DateTime.Now,
					Folder = folderName,
					Url = fullUrl, 
					FileSize = image.Length,
					FileType = image.ContentType,
					IsMain = true
				};

				var imageRepo = _unitOfWork.Repository<Image>();
				await imageRepo.CreateAsync(savedImage);
				await _unitOfWork.CommitAsync();
			
				if (!string.IsNullOrEmpty(userId)) await LogAdminOperationAsync(userId, $"Uploaded main image to {folderName}", savedImage.Id);
				_logger.LogInformation($"✅ Main image saved: {relativePath}");
				return Result<Image>.Ok(savedImage);
			}
			catch (Exception ex)
			{
				
				_logger.LogError($"❌ Error saving main image: {ex.Message}");
				NotifyAdminOfError(ex.Message, ex.StackTrace);
				return Result<Image>.Fail($"Error saving main image: {ex.Message}");
			}
		}
		
		private async Task<Result<List<Image>>> SaveImagesAsync(List<IFormFile> images, string folderName, string userId)
		{
			_logger.LogInformation($"📥 Saving {images?.Count} images to {folderName}");
			if (images == null || images.Count == 0) return Result<List<Image>>.Fail("Images are null or empty");

			var savedImages = new List<Image>();
			var errors = new List<string>();
			int counter = 1;

			foreach (var image in images)
			{
				var result = await SaveImageAsync(image, folderName, userId);
				if (!result.Success || result.Data == null)
				{
					errors.Add($"Image #{counter}: {result.Message}");
					_logger.LogError($"❌ Failed to save image #{counter}: {result.Message}");
				}
				else
				{
					savedImages.Add(result.Data);
				}
				counter++;
			}

			// Return success with warnings if some images failed
			if (errors.Any())
			{
				var warningMessage = $"Some images failed to save: {string.Join(" | ", errors)}";
				_logger.LogWarning(warningMessage);
				
				return new Result<List<Image>>
				{
					Message="Error",
					Success = true, 
					Warnings = errors,
					Data = savedImages
				};
			}
			return savedImages.Count==0?Result<List<Image>>.Fail("No Images Saved",400,errors):Result<List<Image>>.Ok(savedImages);
		}

		// Public Wrappers
		public Task<Result<Image>> SaveCustomerImageAsync(IFormFile image, string userId) => SaveImageAsync(image, "CustomerPhotos", userId);
		
		// Single image methods
		public Task<Result<Image>> SaveCategoryImageAsync(IFormFile image, string userId) => SaveImageAsync(image, "CategoryPhotos", userId);
		public Task<Result<Image>> SaveProductImageAsync(IFormFile image, string userId) => SaveImageAsync(image, "ProductPhotos", userId);
		public Task<Result<Image>> SaveSubCategoryImageAsync(IFormFile image, string userId) => SaveImageAsync(image, "SubCategoryPhotos", userId);
		
		// Multiple images methods
		public Task<Result<List<Image>>> SaveCategoryImagesAsync(List<IFormFile> images, string userId) => SaveImagesAsync(images, "CategoryPhotos", userId);
		public Task<Result<List<Image>>> SaveProductImagesAsync(List<IFormFile> images, string userId) => SaveImagesAsync(images, "ProductPhotos", userId);
		public Task<Result<List<Image>>> SaveSubCategoryImagesAsync(List<IFormFile> images, string userId) => SaveImagesAsync(images, "SubCategoryPhotos", userId);
		
		public Task<Result<Image>> SaveMainCategoryImageAsync(IFormFile image, string userId) => SaveMainImageAsync(image, "CategoryPhotos", userId);
		public Task<Result<Image>> SaveMainProductImageAsync(IFormFile image, string userId) => SaveMainImageAsync(image, "ProductPhotos", userId);
		public Task<Result<Image>> SaveMainSubCategoryImageAsync(IFormFile image, string userId) => SaveMainImageAsync(image, "SubCategoryPhotos", userId);
		public async Task<Result<string>> DeleteImageAsync(Image image)
		{
			_logger.LogInformation($"✅ Execute {nameof(DeleteImageAsync)} for image ID: {image.Id}");

			try
			{
		
				var imageFileName = Path.GetFileName(new Uri(image.Url).AbsolutePath);

				string fullPath = Path.Combine("wwwroot", image.Folder ?? "", imageFileName);

				if (!File.Exists(fullPath))
				{
					_logger.LogWarning("❌ Image file not found on disk: " + fullPath);
					return Result<string>.Fail("Image does not exist on disk");
				}

			
				await _unitOfWork.Image.SoftDeleteAsync(image.Id);
				await _unitOfWork.CommitAsync();

		
				// File.Delete(fullPath);

				_logger.LogInformation("🗑️ Image marked as deleted: " + fullPath);
				return Result<string>.Ok("Image marked as deleted successfully");
			}
			catch (Exception ex)
			{
				_logger.LogError("❌ Error deleting image: " + ex.Message);
				NotifyAdminOfError(ex.Message, ex.StackTrace);
				return Result<string>.Fail("An error occurred while deleting the image");
			}
		}
		public async Task<Result<List<string>>> DeleteImagesAsync(List<Image> images)
		{
			List<string> errorMessages = new List<string>();

			foreach (var image in images)
			{
				var result = await DeleteImageAsync(image);
				if (!result.Success)
				{
					_logger.LogError($"❌ Failed to delete image ID {image.Id}: {result.Message}");
					errorMessages.Add($"Image ID {image.Id}: {result.Message}");
				}
			}

			if (errorMessages.Count == 0)
			{
				return Result<List<string>>.Ok(new List<string> { "✅ All images deleted successfully" });
			}
			else
			{
				return Result<List<string>>.Fail("some images can't deleted ",errorMessages);
			}
		}



		public async Task<Result<List<Image>>> SaveCategoryImagesWithCategoryIdAsync(List<IFormFile> images, int categoryId, string userId)
		{
			var result = await SaveImagesAsync(images, "CategoryPhotos", userId);
			if (!result.Success || result.Data == null)
				return result;

			var imageRepo = _unitOfWork.Repository<Image>();
			foreach (var img in result.Data)
			{
				img.CategoryId = categoryId;
				 imageRepo.Update(img);
			}
			await _unitOfWork.CommitAsync();
			return result;
		}
	}
}
