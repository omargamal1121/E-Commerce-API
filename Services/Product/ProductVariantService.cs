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
	public interface IProductVariantService
	{
		Task<Result<List<ProductVariantDto>>> GetProductVariantsAsync(int productId);
		Task<Result<ProductVariantDto>> GetVariantByIdAsync(int variantId);
		Task<Result<ProductVariantDto>> AddVariantAsync(int productId, CreateProductVariantDto dto, string userId);
		Task<Result<ProductVariantDto>> UpdateVariantAsync(int variantId, UpdateProductVariantDto dto, string userId);
		Task<Result<string>> DeleteVariantAsync(int variantId, string userId);
		Task<Result<string>> UpdateVariantPriceAsync(int variantId, decimal newPrice, string userId);
		Task<Result<string>> UpdateVariantQuantityAsync(int variantId, int newQuantity, string userId);
		Task<Result<List<ProductVariantDto>>> GetVariantsByColorAsync(int productId, string color);
		Task<Result<List<ProductVariantDto>>> GetVariantsBySizeAsync(int productId, string size);
	}

	public class ProductVariantService : IProductVariantService
	{
		private readonly IUnitOfWork _unitOfWork;
		private readonly ILogger<ProductVariantService> _logger;
		private readonly IAdminOpreationServices _adminOpreationServices;
		private readonly IErrorNotificationService _errorNotificationService;

		public ProductVariantService(
			IUnitOfWork unitOfWork,
			ILogger<ProductVariantService> logger,
			IAdminOpreationServices adminOpreationServices,
			IErrorNotificationService errorNotificationService)
		{
			_unitOfWork = unitOfWork;
			_logger = logger;
			_adminOpreationServices = adminOpreationServices;
			_errorNotificationService = errorNotificationService;
		}

		public async Task<Result<List<ProductVariantDto>>> GetProductVariantsAsync(int productId)
		{
			try
			{
				var variants = await _unitOfWork.Product.GetAll()
					.Where(p => p.Id == productId)
					.SelectMany(p => p.ProductVariants.Where(v => v.DeletedAt == null))
					.Select(v => new ProductVariantDto
					{
						Id = v.Id,
						Color = v.Color,
						Size = v.Size,
						Waist = v.Waist,
						Length = v.Length,
						FitType = v.FitType,
						Price = v.Price,
						Quantity = v.Quantity,
						ProductId = v.ProductId
					})
					.ToListAsync();

				if (!variants.Any())
					return Result<List<ProductVariantDto>>.Fail("No variants found for this product", 404);

				return Result<List<ProductVariantDto>>.Ok(variants, "Product variants retrieved successfully", 200);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, $"Error in GetProductVariantsAsync for productId: {productId}");
				await _errorNotificationService.SendErrorNotificationAsync(ex.Message, ex.StackTrace);
				return Result<List<ProductVariantDto>>.Fail("Error retrieving product variants", 500);
			}
		}

		public async Task<Result<ProductVariantDto>> GetVariantByIdAsync(int variantId)
		{
			try
			{
				var variant = await _unitOfWork.Repository<ProductVariant>().GetAll()
					.Where(v => v.Id == variantId && v.DeletedAt == null)
					.Select(v => new ProductVariantDto
					{
						Id = v.Id,
						Color = v.Color,
						Size = v.Size,
						Waist = v.Waist,
						Length = v.Length,
						FitType = v.FitType,
						Price = v.Price,
						Quantity = v.Quantity,
						ProductId = v.ProductId
					})
					.FirstOrDefaultAsync();

				if (variant == null)
					return Result<ProductVariantDto>.Fail("Variant not found", 404);

				return Result<ProductVariantDto>.Ok(variant, "Variant retrieved successfully", 200);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, $"Error in GetVariantByIdAsync for variantId: {variantId}");
				await _errorNotificationService.SendErrorNotificationAsync(ex.Message, ex.StackTrace);
				return Result<ProductVariantDto>.Fail("Error retrieving variant", 500);
			}
		}

		public async Task<Result<ProductVariantDto>> AddVariantAsync(int productId, CreateProductVariantDto dto, string userId)
		{
			_logger.LogInformation($"Adding variant to product: {productId}");
			try
			{
				// Validate product exists
				var product = await _unitOfWork.Product.GetByIdAsync(productId);
				if (product == null)
					return Result<ProductVariantDto>.Fail("Product not found", 404);

				//// Validate required fields
				//if (string.IsNullOrEmpty(dto.Color) || string.IsNullOrEmpty(dto.Size))
				//	return Result<ProductVariantDto>.Fail("Color and Size are required", 400);

				if (dto.Price <= 0)
					return Result<ProductVariantDto>.Fail("Price must be greater than zero", 400);

				if (dto.Quantity < 0)
					return Result<ProductVariantDto>.Fail("Quantity cannot be negative", 400);

				// Check for duplicate variant
				var existingVariant = await _unitOfWork.Product.GetAll()
					.Where(p => p.Id == productId)
					.SelectMany(p => p.ProductVariants.Where(v => v.DeletedAt == null))
					.FirstOrDefaultAsync(v => v.Color == dto.Color && v.Size == dto.Size);

				if (existingVariant != null)
					return Result<ProductVariantDto>.Fail("Variant with this color and size already exists", 400);

				using var transaction = await _unitOfWork.BeginTransactionAsync();

				var variant = new ProductVariant
				{
					ProductId = productId,
					Color = dto.Color,
					Size = dto.Size,
					
					Price = dto.Price,
					Quantity = dto.Quantity
				};

				var result = await _unitOfWork.Repository<ProductVariant>().CreateAsync(variant);
				if (result == null)
				{
					await transaction.RollbackAsync();
					return Result<ProductVariantDto>.Fail("Failed to add variant", 400);
				}

				// Log admin operation
				await _adminOpreationServices.AddAdminOpreationAsync(
					$"Add Variant to Product {productId}",
					Opreations.AddOpreation,
					userId,
					productId
				);

				await _unitOfWork.CommitAsync();

				var variantDto = new ProductVariantDto
				{
					Id = variant.Id,
					Color = variant.Color,
					Size = variant.Size,
					Waist = variant.Waist,
					Length = variant.Length,
					FitType = variant.FitType,
					Price = variant.Price,
					Quantity = variant.Quantity,
					ProductId = variant.ProductId
				};

				return Result<ProductVariantDto>.Ok(variantDto, "Variant added successfully", 201);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, $"Error in AddVariantAsync for productId: {productId}");
				await _errorNotificationService.SendErrorNotificationAsync(ex.Message, ex.StackTrace);
				return Result<ProductVariantDto>.Fail("Error adding variant", 500);
			}
		}

		public async Task<Result<ProductVariantDto>> UpdateVariantAsync(int variantId, UpdateProductVariantDto dto, string userId)
		{
			_logger.LogInformation($"Updating variant: {variantId}");
			try
			{
				var variant = await _unitOfWork.Repository<ProductVariant>().GetByIdAsync(variantId);
				if (variant == null)
					return Result<ProductVariantDto>.Fail("Variant not found", 404);

				// Check for duplicate variant (excluding current variant)
				if (!string.IsNullOrEmpty(dto.Color) && !string.IsNullOrEmpty(dto.Size))
				{
					var existingVariant = await _unitOfWork.Repository<ProductVariant>().GetAll()
						.Where(v => v.ProductId == variant.ProductId && v.Id != variantId && v.DeletedAt == null)
						.FirstOrDefaultAsync(v => v.Color == dto.Color && v.Size == dto.Size);

					if (existingVariant != null)
						return Result<ProductVariantDto>.Fail("Variant with this color and size already exists", 400);
				}

				using var transaction = await _unitOfWork.BeginTransactionAsync();

				// Update fields
				if (!string.IsNullOrEmpty(dto.Color))
					variant.Color = dto.Color;
				//if (!string.IsNullOrEmpty(dto.Size))
				//	variant.Size = dto.Size;
				
				if (dto.Price.HasValue && dto.Price.Value > 0)
					variant.Price = dto.Price.Value;
				if (dto.Quantity.HasValue && dto.Quantity.Value >= 0)
					variant.Quantity = dto.Quantity.Value;

				var result = _unitOfWork.Repository<ProductVariant>().Update(variant);
				if (!result)
				{
					await transaction.RollbackAsync();
					return Result<ProductVariantDto>.Fail("Failed to update variant", 400);
				}

				// Log admin operation
				await _adminOpreationServices.AddAdminOpreationAsync(
					$"Update Variant {variantId}",
					Opreations.UpdateOpreation,
					userId,
					variant.ProductId
				);

				await _unitOfWork.CommitAsync();

				var variantDto = new ProductVariantDto
				{
					Id = variant.Id,
					Color = variant.Color,
					Size = variant.Size,
					Waist = variant.Waist,
					Length = variant.Length,
					FitType = variant.FitType,
					Price = variant.Price,
					Quantity = variant.Quantity,
					ProductId = variant.ProductId
				};

				return Result<ProductVariantDto>.Ok(variantDto, "Variant updated successfully", 200);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, $"Error in UpdateVariantAsync for variantId: {variantId}");
				await _errorNotificationService.SendErrorNotificationAsync(ex.Message, ex.StackTrace);
				return Result<ProductVariantDto>.Fail("Error updating variant", 500);
			}
		}

		public async Task<Result<string>> DeleteVariantAsync(int variantId, string userId)
		{
			_logger.LogInformation($"Deleting variant: {variantId}");
			try
			{
				var variant = await _unitOfWork.Repository<ProductVariant>().GetByIdAsync(variantId);
				if (variant == null)
					return Result<string>.Fail("Variant not found", 404);

				using var transaction = await _unitOfWork.BeginTransactionAsync();

				var result = await _unitOfWork.Repository<ProductVariant>().SoftDeleteAsync(variantId);
				if (!result)
				{
					await transaction.RollbackAsync();
					return Result<string>.Fail("Failed to delete variant", 400);
				}

				// Log admin operation
				await _adminOpreationServices.AddAdminOpreationAsync(
					$"Delete Variant {variantId}",
					Opreations.DeleteOpreation,
					userId,
					variant.ProductId
				);

				await _unitOfWork.CommitAsync();
				return Result<string>.Ok("Variant deleted successfully", "Variant deleted", 200);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, $"Error in DeleteVariantAsync for variantId: {variantId}");
				await _errorNotificationService.SendErrorNotificationAsync(ex.Message, ex.StackTrace);
				return Result<string>.Fail("Error deleting variant", 500);
			}
		}

		public async Task<Result<string>> UpdateVariantPriceAsync(int variantId, decimal newPrice, string userId)
		{
			_logger.LogInformation($"Updating price for variant: {variantId}");
			try
			{
				if (newPrice <= 0)
					return Result<string>.Fail("Price must be greater than zero", 400);

				var variant = await _unitOfWork.Repository<ProductVariant>().GetByIdAsync(variantId);
				if (variant == null)
					return Result<string>.Fail("Variant not found", 404);

				using var transaction = await _unitOfWork.BeginTransactionAsync();

				variant.Price = newPrice;
				var result = _unitOfWork.Repository<ProductVariant>().Update(variant);
				if (!result)
				{
					await transaction.RollbackAsync();
					return Result<string>.Fail("Failed to update price", 400);
				}

				// Log admin operation
				await _adminOpreationServices.AddAdminOpreationAsync(
					$"Update Price for Variant {variantId}",
					Opreations.UpdateOpreation,
					userId,
					variant.ProductId
				);

				await _unitOfWork.CommitAsync();
				return Result<string>.Ok("Price updated successfully", "Price updated", 200);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, $"Error in UpdateVariantPriceAsync for variantId: {variantId}");
				await _errorNotificationService.SendErrorNotificationAsync(ex.Message, ex.StackTrace);
				return Result<string>.Fail("Error updating price", 500);
			}
		}

		public async Task<Result<string>> UpdateVariantQuantityAsync(int variantId, int newQuantity, string userId)
		{
			_logger.LogInformation($"Updating quantity for variant: {variantId}");
			try
			{
				if (newQuantity < 0)
					return Result<string>.Fail("Quantity cannot be negative", 400);

				var variant = await _unitOfWork.Repository<ProductVariant>().GetByIdAsync(variantId);
				if (variant == null)
					return Result<string>.Fail("Variant not found", 404);

				using var transaction = await _unitOfWork.BeginTransactionAsync();

				variant.Quantity = newQuantity;
				var result = _unitOfWork.Repository<ProductVariant>().Update(variant);
				if (!result)
				{
					await transaction.RollbackAsync();
					return Result<string>.Fail("Failed to update quantity", 400);
				}

				// Log admin operation
				await _adminOpreationServices.AddAdminOpreationAsync(
					$"Update Quantity for Variant {variantId}",
					Opreations.UpdateOpreation,
					userId,
					variant.ProductId
				);

				await _unitOfWork.CommitAsync();
				return Result<string>.Ok("Quantity updated successfully", "Quantity updated", 200);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, $"Error in UpdateVariantQuantityAsync for variantId: {variantId}");
				await _errorNotificationService.SendErrorNotificationAsync(ex.Message, ex.StackTrace);
				return Result<string>.Fail("Error updating quantity", 500);
			}
		}

		public async Task<Result<List<ProductVariantDto>>> GetVariantsByColorAsync(int productId, string color)
		{
			try
			{
				var variants = await _unitOfWork.Product.GetAll()
					.Where(p => p.Id == productId)
					.SelectMany(p => p.ProductVariants.Where(v => v.DeletedAt == null && v.Color == color))
					.Select(v => new ProductVariantDto
					{
						Id = v.Id,
						Color = v.Color,
						Size = v.Size,
						Waist = v.Waist,
						Length = v.Length,
						FitType = v.FitType,
						Price = v.Price,
						Quantity = v.Quantity,
						ProductId = v.ProductId
					})
					.ToListAsync();

				if (!variants.Any())
					return Result<List<ProductVariantDto>>.Fail($"No variants found with color: {color}", 404);

				return Result<List<ProductVariantDto>>.Ok(variants, $"Variants with color {color} retrieved successfully", 200);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, $"Error in GetVariantsByColorAsync for productId: {productId}, color: {color}");
				await _errorNotificationService.SendErrorNotificationAsync(ex.Message, ex.StackTrace);
				return Result<List<ProductVariantDto>>.Fail("Error retrieving variants by color", 500);
			}
		}

		public async Task<Result<List<ProductVariantDto>>> GetVariantsBySizeAsync(int productId, string size)
		{
			try
			{
				var variants = await _unitOfWork.Product.GetAll()
					.Where(p => p.Id == productId)
					.SelectMany(p => p.ProductVariants.Where(v => v.DeletedAt == null && v.Size == size))
					.Select(v => new ProductVariantDto
					{
						Id = v.Id,
						Color = v.Color,
						Size = v.Size,
						Waist = v.Waist,
						Length = v.Length,
						FitType = v.FitType,
						Price = v.Price,
						Quantity = v.Quantity,
						ProductId = v.ProductId
					})
					.ToListAsync();

				if (!variants.Any())
					return Result<List<ProductVariantDto>>.Fail($"No variants found with size: {size}", 404);

				return Result<List<ProductVariantDto>>.Ok(variants, $"Variants with size {size} retrieved successfully", 200);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, $"Error in GetVariantsBySizeAsync for productId: {productId}, size: {size}");
				await _errorNotificationService.SendErrorNotificationAsync(ex.Message, ex.StackTrace);
				return Result<List<ProductVariantDto>>.Fail("Error retrieving variants by size", 500);
			}
		}
	}
} 