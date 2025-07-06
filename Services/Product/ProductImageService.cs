using E_Commers.DtoModels.ImagesDtos;
using E_Commers.DtoModels.ProductDtos;
using E_Commers.DtoModels.Responses;
using E_Commers.Enums;
using E_Commers.ErrorHnadling;
using E_Commers.Interfaces;
using E_Commers.Models;
using E_Commers.Services.AdminOpreationServices;
using E_Commers.Services.EmailServices;
using E_Commers.UOW;
using Microsoft.EntityFrameworkCore;

namespace E_Commers.Services.Product
{
	public interface IProductImageService
	{
		Task<Result<List<ImageDto>>> GetProductImagesAsync(int productId);
		Task<Result<ImageDto>> AddProductImageAsync(int productId, CreateImageDto dto, string userId);
		Task<Result<string>> RemoveProductImageAsync(int productId, int imageId, string userId);
		Task<Result<string>> SetMainImageAsync(int productId, int imageId, string userId);
		Task<Result<string>> UpdateImageOrderAsync(int productId, List<int> imageIds, string userId);
	}

	public class ProductImageService : IProductImageService
	{
		private readonly IUnitOfWork _unitOfWork;
		private readonly ILogger<ProductImageService> _logger;
		private readonly IAdminOpreationServices _adminOpreationServices;
		private readonly IErrorNotificationService _errorNotificationService;

		public ProductImageService(
			IUnitOfWork unitOfWork,
			ILogger<ProductImageService> logger,
			IAdminOpreationServices adminOpreationServices,
			IErrorNotificationService errorNotificationService)
		{
			_unitOfWork = unitOfWork;
			_logger = logger;
			_adminOpreationServices = adminOpreationServices;
			_errorNotificationService = errorNotificationService;
		}

		public async Task<Result<List<ImageDto>>> GetProductImagesAsync(int productId)
		{
			try
			{
				var images = await _unitOfWork.Product.GetAll()
					.Where(p => p.Id == productId)
					.SelectMany(p => p.Images.Where(i => i.DeletedAt == null))
					.Select(i => new ImageDto
					{
						Id = i.Id,
						Url = i.Url,
					
					})
					.ToListAsync();

				if (!images.Any())
					return Result<List<ImageDto>>.Fail("No images found for this product", 404);

				return Result<List<ImageDto>>.Ok(images, "Product images retrieved successfully", 200);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, $"Error in GetProductImagesAsync for productId: {productId}");
				await _errorNotificationService.SendErrorNotificationAsync(ex.Message, ex.StackTrace);
				return Result<List<ImageDto>>.Fail("Error retrieving product images", 500);
			}
		}

		public async Task<Result<ImageDto>> AddProductImageAsync(int productId, CreateImageDto dto, string userId)
		{
			_logger.LogInformation($"Adding image to product: {productId}");
			try
			{
				// Validate product exists
				var product = await _unitOfWork.Product.GetByIdAsync(productId);
				if (product == null)
					return Result<ImageDto>.Fail("Product not found", 404);

				// Validate image URL
				if (string.IsNullOrEmpty(dto.Url))
					return Result<ImageDto>.Fail("Image URL is required", 400);

				using var transaction = await _unitOfWork.BeginTransactionAsync();


				// Create new image
				var image = new Image
				{
					Url = dto.Url,
					ProductId = productId,
					IsMain = dto.IsMain ?? false,
				
				};

				// If this is main image, unset other main images
				if (image.IsMain)
				{
					var existingMainImages = await _unitOfWork.Product.GetAll()
						.Where(p => p.Id == productId)
						.SelectMany(p => p.Images.Where(i => i.DeletedAt == null && i.IsMain))
						.ToListAsync();

					foreach (var existingImage in existingMainImages)
					{
						existingImage.IsMain = false;
						_unitOfWork.Image.Update(existingImage);
					}
				}

				var result = await _unitOfWork.Image.CreateAsync(image);
				if (result == null)
				{
					await transaction.RollbackAsync();
					return Result<ImageDto>.Fail("Failed to add image", 400);
				}

				// Log admin operation
				await _adminOpreationServices.AddAdminOpreationAsync(
					$"Add Image to Product {productId}",
					Opreations.AddOpreation,
					userId,
					productId
				);

				await _unitOfWork.CommitAsync();

				var imageDto = new ImageDto
				{
					Id = image.Id,
					Url = image.Url,
					
				};

				return Result<ImageDto>.Ok(imageDto, "Image added successfully", 201);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, $"Error in AddProductImageAsync for productId: {productId}");
				await _errorNotificationService.SendErrorNotificationAsync(ex.Message, ex.StackTrace);
				return Result<ImageDto>.Fail("Error adding image", 500);
			}
		}

		public async Task<Result<string>> RemoveProductImageAsync(int productId, int imageId, string userId)
		{
			_logger.LogInformation($"Removing image {imageId} from product: {productId}");
			try
			{
				// Validate product and image exist
				var image = await _unitOfWork.Product.GetAll()
					.Where(p => p.Id == productId)
					.SelectMany(p => p.Images.Where(i => i.Id == imageId && i.DeletedAt == null))
					.FirstOrDefaultAsync();

				if (image == null)
					return Result<string>.Fail("Image not found", 404);

				using var transaction = await _unitOfWork.BeginTransactionAsync();

				// Soft delete the image
				var result = await _unitOfWork.Image.SoftDeleteAsync(imageId);
				if (!result)
				{
					await transaction.RollbackAsync();
					return Result<string>.Fail("Failed to remove image", 400);
				}

				// If this was the main image, set another image as main
				if (image.IsMain)
				{
					var nextMainImage = await _unitOfWork.Product.GetAll()
						.Where(p => p.Id == productId)
						.SelectMany(p => p.Images.Where(i => i.DeletedAt == null && i.Id != imageId))
					
						.FirstOrDefaultAsync();

					if (nextMainImage != null)
					{
						nextMainImage.IsMain = true;
						_unitOfWork.Image.Update(nextMainImage);
					}
				}

				// Log admin operation
				await _adminOpreationServices.AddAdminOpreationAsync(
					$"Remove Image {imageId} from Product {productId}",
					Opreations.DeleteOpreation,
					userId,
					productId
				);

				await _unitOfWork.CommitAsync();
				return Result<string>.Ok("Image removed successfully", "Image removed", 200);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, $"Error in RemoveProductImageAsync for productId: {productId}, imageId: {imageId}");
				await _errorNotificationService.SendErrorNotificationAsync(ex.Message, ex.StackTrace);
				return Result<string>.Fail("Error removing image", 500);
			}
		}

		public async Task<Result<string>> SetMainImageAsync(int productId, int imageId, string userId)
		{
			_logger.LogInformation($"Setting main image {imageId} for product: {productId}");
			try
			{
				// Validate product and image exist
				var image = await _unitOfWork.Product.GetAll()
					.Where(p => p.Id == productId)
					.SelectMany(p => p.Images.Where(i => i.Id == imageId && i.DeletedAt == null))
					.FirstOrDefaultAsync();

				if (image == null)
					return Result<string>.Fail("Image not found", 404);

				using var transaction = await _unitOfWork.BeginTransactionAsync();

				// Unset all other main images for this product
				var existingMainImages = await _unitOfWork.Product.GetAll()
					.Where(p => p.Id == productId)
					.SelectMany(p => p.Images.Where(i => i.DeletedAt == null && i.IsMain))
					.ToListAsync();

				foreach (var existingImage in existingMainImages)
				{
					existingImage.IsMain = false;
					_unitOfWork.Image.Update(existingImage);
				}

				// Set the new main image
				image.IsMain = true;
				var result = _unitOfWork.Image.Update(image);
				if (!result)
				{
					await transaction.RollbackAsync();
					return Result<string>.Fail("Failed to set main image", 400);
				}

				// Log admin operation
				await _adminOpreationServices.AddAdminOpreationAsync(
					$"Set Main Image {imageId} for Product {productId}",
					Opreations.UpdateOpreation,
					userId,
					productId
				);

				await _unitOfWork.CommitAsync();
				return Result<string>.Ok("Main image set successfully", "Main image updated", 200);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, $"Error in SetMainImageAsync for productId: {productId}, imageId: {imageId}");
				await _errorNotificationService.SendErrorNotificationAsync(ex.Message, ex.StackTrace);
				return Result<string>.Fail("Error setting main image", 500);
			}
		}

		public async Task<Result<string>> UpdateImageOrderAsync(int productId, List<int> imageIds, string userId)
		{
			_logger.LogInformation($"Updating image order for product: {productId}");
			try
			{
				// Validate product exists
				var product = await _unitOfWork.Product.GetByIdAsync(productId);
				if (product == null)
					return Result<string>.Fail("Product not found", 404);

				// Validate all images belong to this product
				var productImages = await _unitOfWork.Product.GetAll()
					.Where(p => p.Id == productId)
					.SelectMany(p => p.Images.Where(i => i.DeletedAt == null))
					.ToListAsync();

				var imageIdsSet = imageIds.ToHashSet();
				var productImageIds = productImages.Select(i => i.Id).ToHashSet();

				if (!imageIdsSet.SetEquals(productImageIds))
					return Result<string>.Fail("Invalid image IDs provided", 400);

				using var transaction = await _unitOfWork.BeginTransactionAsync();

				// Update order for each image
				for (int i = 0; i < imageIds.Count; i++)
				{
					var image = productImages.First(img => img.Id == imageIds[i]);
			
					_unitOfWork.Image.Update(image);
				}

				// Log admin operation
				await _adminOpreationServices.AddAdminOpreationAsync(
					$"Update Image Order for Product {productId}",
					Opreations.UpdateOpreation,
					userId,
					productId
				);

				await _unitOfWork.CommitAsync();
				return Result<string>.Ok("Image order updated successfully", "Image order updated", 200);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, $"Error in UpdateImageOrderAsync for productId: {productId}");
				await _errorNotificationService.SendErrorNotificationAsync(ex.Message, ex.StackTrace);
				return Result<string>.Fail("Error updating image order", 500);
			}
		}
	}
} 