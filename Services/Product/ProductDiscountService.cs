using E_Commers.DtoModels.DiscoutDtos;
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
	public interface IProductDiscountService
	{
		Task<Result<DiscountDto>> GetProductDiscountAsync(int productId);
		Task<Result<DiscountDto>> AddDiscountToProductAsync(int productId, CreateDiscountDto dto, string userId);
		Task<Result<DiscountDto>> UpdateProductDiscountAsync(int productId, UpdateDiscountDto dto, string userId);
		Task<Result<string>> RemoveDiscountFromProductAsync(int productId, string userId);
		Task<Result<string>> ActivateDiscountAsync(int productId, string userId);
		Task<Result<string>> DeactivateDiscountAsync(int productId, string userId);
		Task<Result<List<ProductDto>>> GetProductsWithActiveDiscountsAsync();
		Task<Result<decimal>> CalculateDiscountedPriceAsync(int productId, int variantId);
	}

	public class ProductDiscountService : IProductDiscountService
	{
		private readonly IUnitOfWork _unitOfWork;
		private readonly ILogger<ProductDiscountService> _logger;
		private readonly IAdminOpreationServices _adminOpreationServices;
		private readonly IErrorNotificationService _errorNotificationService;

		public ProductDiscountService(
			IUnitOfWork unitOfWork,
			ILogger<ProductDiscountService> logger,
			IAdminOpreationServices adminOpreationServices,
			IErrorNotificationService errorNotificationService)
		{
			_unitOfWork = unitOfWork;
			_logger = logger;
			_adminOpreationServices = adminOpreationServices;
			_errorNotificationService = errorNotificationService;
		}

		public async Task<Result<DiscountDto>> GetProductDiscountAsync(int productId)
		{
			try
			{
				var discount = await _unitOfWork.Product.GetAll()
					.Where(p => p.Id == productId)
					.Select(p => p.Discount)
					.Where(d => d != null && d.DeletedAt == null)
					.Select(d => new DiscountDto
					{
						Id = d.Id,
						DiscountPercent = d.DiscountPercent,
						IsActive = d.IsActive,
						CreatedAt = d.CreatedAt,
						DeletedAt = d.DeletedAt,
						Description = d.Description
					})
					.FirstOrDefaultAsync();

				if (discount == null)
					return Result<DiscountDto>.Fail("No discount found for this product", 404);

				return Result<DiscountDto>.Ok(discount, "Product discount retrieved successfully", 200);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, $"Error in GetProductDiscountAsync for productId: {productId}");
				await _errorNotificationService.SendErrorNotificationAsync(ex.Message, ex.StackTrace);
				return Result<DiscountDto>.Fail("Error retrieving product discount", 500);
			}
		}

		public async Task<Result<DiscountDto>> AddDiscountToProductAsync(int productId, CreateDiscountDto dto, string userId)
		{
			_logger.LogInformation($"Adding discount to product: {productId}");
			try
			{
				// Validate product exists
				var product = await _unitOfWork.Product.GetByIdAsync(productId);
				if (product == null)
					return Result<DiscountDto>.Fail("Product not found", 404);

				// Check if product already has a discount
				if (product.DiscountId.HasValue)
					return Result<DiscountDto>.Fail("Product already has a discount", 400);

				// Validate discount data
				if (dto.DiscountPercent <= 0 || dto.DiscountPercent > 100)
					return Result<DiscountDto>.Fail("Discount percentage must be between 1 and 100", 400);

				if (dto.StartDate >= dto.EndDate)
					return Result<DiscountDto>.Fail("Start date must be before end date", 400);

				using var transaction = await _unitOfWork.BeginTransactionAsync();

				// Fix for CS0118: 'Discount' is a namespace but is used like a type
				// The issue occurs because the 'Discount' identifier is ambiguous between a namespace and a type.
				// To resolve this, ensure the correct type is referenced explicitly using its namespace.

				var discount = new E_Commers.Models.Discount
				{
					DiscountPercent = dto.DiscountPercent,
					IsActive = dto.IsActive ?? true,
					StartDate = dto.StartDate,
					EndDate = dto.EndDate,
					Description = dto.Description
				};

				var discountResult = await _unitOfWork.Repository<E_Commers.Models.Discount>().CreateAsync(discount);
				if (discountResult == null)
				{
					await transaction.RollbackAsync();
					return Result<DiscountDto>.Fail("Failed to create discount", 400);
				}

				// Assign discount to product
				product.DiscountId = discount.Id;
				var productResult = _unitOfWork.Product.Update(product);
				if (!productResult)
				{
					await transaction.RollbackAsync();
					return Result<DiscountDto>.Fail("Failed to assign discount to product", 400);
				}

				// Log admin operation
				await _adminOpreationServices.AddAdminOpreationAsync(
					$"Add Discount to Product {productId}",
					Opreations.AddOpreation,
					userId,
					productId
				);

				await _unitOfWork.CommitAsync();

				var discountDto = new DiscountDto
				{
					Id = discount.Id,
					DiscountPercent = discount.DiscountPercent,
					IsActive = discount.IsActive,
					StartDate = discount.StartDate,
					EndDate = discount.EndDate,
					Description = discount.Description
				};

				return Result<DiscountDto>.Ok(discountDto, "Discount added successfully", 201);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, $"Error in AddDiscountToProductAsync for productId: {productId}");
				await _errorNotificationService.SendErrorNotificationAsync(ex.Message, ex.StackTrace);
				return Result<DiscountDto>.Fail("Error adding discount", 500);
			}
		}

		public async Task<Result<DiscountDto>> UpdateProductDiscountAsync(int productId, UpdateDiscountDto dto, string userId)
		{
			_logger.LogInformation($"Updating discount for product: {productId}");
			try
			{
				// Validate product and discount exist
				var product = await _unitOfWork.Product.GetAll()
					.Where(p => p.Id == productId)
					.Include(p => p.Discount)
					.FirstOrDefaultAsync();

				if (product == null)
					return Result<DiscountDto>.Fail("Product not found", 404);

				if (product.Discount == null)
					return Result<DiscountDto>.Fail("Product has no discount to update", 404);

				using var transaction = await _unitOfWork.BeginTransactionAsync();

				// Update discount fields
				if (dto.DiscountPercent.HasValue)
				{
					if (dto.DiscountPercent.Value <= 0 || dto.DiscountPercent.Value > 100)
						return Result<DiscountDto>.Fail("Discount percentage must be between 1 and 100", 400);
					product.Discount.DiscountPercent = dto.DiscountPercent.Value;
				}

				if (dto.IsActive.HasValue)
					product.Discount.IsActive = dto.IsActive.Value;

				if (dto.StartDate.HasValue)
					product.Discount.StartDate = dto.StartDate.Value;

				if (dto.EndDate.HasValue)
					product.Discount.EndDate = dto.EndDate.Value;

				if (!string.IsNullOrEmpty(dto.Description))
					product.Discount.Description = dto.Description;

				// Validate dates
				if (product.Discount.StartDate >= product.Discount.EndDate)
					return Result<DiscountDto>.Fail("Start date must be before end date", 400);

				var result = _unitOfWork.Repository<E_Commers.Models.Discount>().Update(product.Discount);
				if (!result)
				{
					await transaction.RollbackAsync();
					return Result<DiscountDto>.Fail("Failed to update discount", 400);
				}

				// Log admin operation
				await _adminOpreationServices.AddAdminOpreationAsync(
					$"Update Discount for Product {productId}",
					Opreations.UpdateOpreation,
					userId,
					productId
				);

				await _unitOfWork.CommitAsync();

				var discountDto = new DiscountDto
				{
					Id = product.Discount.Id,
					DiscountPercent = product.Discount.DiscountPercent,
					IsActive = product.Discount.IsActive,
					StartDate = product.Discount.StartDate,
					EndDate = product.Discount.EndDate,
					Description = product.Discount.Description
				};

				return Result<DiscountDto>.Ok(discountDto, "Discount updated successfully", 200);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, $"Error in UpdateProductDiscountAsync for productId: {productId}");
				await _errorNotificationService.SendErrorNotificationAsync(ex.Message, ex.StackTrace);
				return Result<DiscountDto>.Fail("Error updating discount", 500);
			}
		}

		public async Task<Result<string>> RemoveDiscountFromProductAsync(int productId, string userId)
		{
			_logger.LogInformation($"Removing discount from product: {productId}");
			try
			{
				var product = await _unitOfWork.Product.GetAll()
					.Where(p => p.Id == productId)
					.Include(p => p.Discount)
					.FirstOrDefaultAsync();

				if (product == null)
					return Result<string>.Fail("Product not found", 404);

				if (product.Discount == null)
					return Result<string>.Fail("Product has no discount to remove", 404);

				using var transaction = await _unitOfWork.BeginTransactionAsync();

				// Remove discount from product
				product.DiscountId = null;
				var productResult = _unitOfWork.Product.Update(product);
				if (!productResult)
				{
					await transaction.RollbackAsync();
					return Result<string>.Fail("Failed to remove discount from product", 400);
				}

				// Soft delete the discount
				var discountResult = await _unitOfWork.Repository<E_Commers.Models.Discount>().SoftDeleteAsync(product.Discount.Id);
				if (!discountResult)
				{
					await transaction.RollbackAsync();
					return Result<string>.Fail("Failed to delete discount", 400);
				}

				// Log admin operation
				await _adminOpreationServices.AddAdminOpreationAsync(
					$"Remove Discount from Product {productId}",
					Opreations.DeleteOpreation,
					userId,
					productId
				);

				await _unitOfWork.CommitAsync();
				return Result<string>.Ok("Discount removed successfully", "Discount removed", 200);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, $"Error in RemoveDiscountFromProductAsync for productId: {productId}");
				await _errorNotificationService.SendErrorNotificationAsync(ex.Message, ex.StackTrace);
				return Result<string>.Fail("Error removing discount", 500);
			}
		}

		public async Task<Result<string>> ActivateDiscountAsync(int productId, string userId)
		{
			_logger.LogInformation($"Activating discount for product: {productId}");
			try
			{
				var product = await _unitOfWork.Product.GetAll()
					.Where(p => p.Id == productId)
					.Include(p => p.Discount)
					.FirstOrDefaultAsync();

				if (product == null)
					return Result<string>.Fail("Product not found", 404);

				if (product.Discount == null)
					return Result<string>.Fail("Product has no discount", 404);

				if (product.Discount.IsActive)
					return Result<string>.Fail("Discount is already active", 400);

				using var transaction = await _unitOfWork.BeginTransactionAsync();

				product.Discount.IsActive = true;
				var result = _unitOfWork.Repository<E_Commers.Models.Discount>().Update(product.Discount);
				if (!result)
				{
					await transaction.RollbackAsync();
					return Result<string>.Fail("Failed to activate discount", 400);
				}

				// Log admin operation
				await _adminOpreationServices.AddAdminOpreationAsync(
					$"Activate Discount for Product {productId}",
					Opreations.UpdateOpreation,
					userId,
					productId
				);

				await _unitOfWork.CommitAsync();
				return Result<string>.Ok("Discount activated successfully", "Discount activated", 200);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, $"Error in ActivateDiscountAsync for productId: {productId}");
				await _errorNotificationService.SendErrorNotificationAsync(ex.Message, ex.StackTrace);
				return Result<string>.Fail("Error activating discount", 500);
			}
		}

		public async Task<Result<string>> DeactivateDiscountAsync(int productId, string userId)
		{
			_logger.LogInformation($"Deactivating discount for product: {productId}");
			try
			{
				var product = await _unitOfWork.Product.GetAll()
					.Where(p => p.Id == productId)
					.Include(p => p.Discount)
					.FirstOrDefaultAsync();

				if (product == null)
					return Result<string>.Fail("Product not found", 404);

				if (product.Discount == null)
					return Result<string>.Fail("Product has no discount", 404);

				if (!product.Discount.IsActive)
					return Result<string>.Fail("Discount is already inactive", 400);

				using var transaction = await _unitOfWork.BeginTransactionAsync();

				product.Discount.IsActive = false;
				var result = _unitOfWork.Repository<E_Commers.Models.Discount>().Update(product.Discount);
				if (!result)
				{
					await transaction.RollbackAsync();
					return Result<string>.Fail("Failed to deactivate discount", 400);
				}

				// Log admin operation
				await _adminOpreationServices.AddAdminOpreationAsync(
					$"Deactivate Discount for Product {productId}",
					Opreations.UpdateOpreation,
					userId,
					productId
				);

				await _unitOfWork.CommitAsync();
				return Result<string>.Ok("Discount deactivated successfully", "Discount deactivated", 200);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, $"Error in DeactivateDiscountAsync for productId: {productId}");
				await _errorNotificationService.SendErrorNotificationAsync(ex.Message, ex.StackTrace);
				return Result<string>.Fail("Error deactivating discount", 500);
			}
		}

		public async Task<Result<List<ProductDto>>> GetProductsWithActiveDiscountsAsync()
		{
			try
			{
				var products = await _unitOfWork.Product.GetAll()
					.Where(p => p.Discount != null && p.Discount.IsActive && p.Discount.DeletedAt == null)
					.Select(p => new ProductDto
					{
						Id = p.Id,
						Name = p.Name,
						Description = p.Description,
						AvailableQuantity = p.Quantity,
						Gender = p.Gender,
						SubCategoryId = p.SubCategoryId,
						Discount = new DiscountDto
						{
							Id = p.Discount.Id,
							DiscountPercent = p.Discount.DiscountPercent,
							IsActive = p.Discount.IsActive,
							StartDate = p.Discount.StartDate,
							EndDate = p.Discount.EndDate,
							Description = p.Discount.Description
						},
						Images = p.Images.Where(i => i.DeletedAt == null).Select(i => new ImageDto { Id = i.Id, Url = i.Url }).ToList(),
						Variants = p.ProductVariants.Where(v => v.DeletedAt == null).Select(v => new ProductVariantDto 
						{ 
							Id = v.Id, 
							Color = v.Color, 
							Size = v.Size, 
							Price = v.Price, 
							Quantity = v.Quantity 
						}).ToList()
					})
					.ToListAsync();

				if (!products.Any())
					return Result<List<ProductDto>>.Fail("No products with active discounts found", 404);

				return Result<List<ProductDto>>.Ok(products, "Products with active discounts retrieved successfully", 200);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error in GetProductsWithActiveDiscountsAsync");
				await _errorNotificationService.SendErrorNotificationAsync(ex.Message, ex.StackTrace);
				return Result<List<ProductDto>>.Fail("Error retrieving products with active discounts", 500);
			}
		}

		public async Task<Result<decimal>> CalculateDiscountedPriceAsync(int productId, int variantId)
		{
			try
			{
				var product = await _unitOfWork.Product.GetAll()
					.Where(p => p.Id == productId)
					.Include(p => p.Discount)
					.Include(p => p.ProductVariants.Where(v => v.Id == variantId))
					.FirstOrDefaultAsync();

				if (product == null)
					return Result<decimal>.Fail("Product not found", 404);

				var variant = product.ProductVariants.FirstOrDefault(v => v.Id == variantId);
				if (variant == null)
					return Result<decimal>.Fail("Variant not found", 404);

				var originalPrice = variant.Price;
				var discountedPrice = originalPrice;

				// Check if discount is active and valid
				if (product.Discount != null && 
					product.Discount.IsActive && 
					product.Discount.DeletedAt == null &&
					product.Discount.StartDate <= DateTime.UtcNow &&
					product.Discount.EndDate >= DateTime.UtcNow)
				{
					var discountAmount = originalPrice * (product.Discount.DiscountPercent / 100m);
					discountedPrice = originalPrice - discountAmount;
				}

				return Result<decimal>.Ok(discountedPrice, "Discounted price calculated successfully", 200);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, $"Error in CalculateDiscountedPriceAsync for productId: {productId}, variantId: {variantId}");
				await _errorNotificationService.SendErrorNotificationAsync(ex.Message, ex.StackTrace);
				return Result<decimal>.Fail("Error calculating discounted price", 500);
			}
		}
	}
} 