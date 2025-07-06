using AutoMapper;
using E_Commers.DtoModels.CategoryDtos;
using E_Commers.DtoModels.CollectionDtos;
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
	public interface IProductCatalogService
	{
		Task<Result<List<ProductDto>>> GetAllAsync();
		Task<Result<ProductDto>> GetProductByIdAsync(int id);
		Task<Result<ProductDto>> CreateProductAsync(CreateProductDto dto, string userId);
		Task<Result<ProductDto>> UpdateProductAsync(int id, UpdateProductDto dto, string userId);
		Task<Result<string>> DeleteProductAsync(int id, string userId);
		Task<Result<ProductDto>> RestoreProductAsync(int id, string userId);
		Task<Result<List<ProductDto>>> GetProductsByCategoryId(int categoryId);
		Task<Result<List<ProductDto>>> FilterAsync(string? search, bool? isActive, bool includeDeleted, int page, int pageSize, string role);
	}

	public class ProductCatalogService : IProductCatalogService
	{
		private readonly IUnitOfWork _unitOfWork;
		private readonly ISubCategoryServices _subCategoryServices;
		private readonly ILogger<ProductCatalogService> _logger;
		private readonly IAdminOpreationServices _adminOpreationServices;
		private readonly IErrorNotificationService _errorNotificationService;

		public ProductCatalogService(
			IUnitOfWork unitOfWork,
			 ISubCategoryServices subCategoryServices,
			ILogger<ProductCatalogService> logger,
			IAdminOpreationServices adminOpreationServices,
			IErrorNotificationService errorNotificationService)
		{
			_unitOfWork = unitOfWork;
			_subCategoryServices = subCategoryServices;
			_logger = logger;
			_adminOpreationServices = adminOpreationServices;
			_errorNotificationService = errorNotificationService;
		}

		public async Task<Result<List<ProductDto>>> GetAllAsync()
		{
			try
			{
				var productsQuery = _unitOfWork.Product.GetAll();
				if (productsQuery == null)
					return Result<List<ProductDto>>.Fail("No Products Found", 404);

				var products = await productsQuery
					.Select(p => new ProductDto
					{
						Id = p.Id,
						Name = p.Name,
						Description = p.Description,
						AvailableQuantity = p.Quantity,
						Gender = p.Gender,
						SubCategoryId = p.SubCategoryId,
						SubCategory = p.SubCategory != null ? new SubCategoryDto { Id = p.SubCategory.Id, Name = p.SubCategory.Name } : null,
						Discount = p.Discount != null ? new DiscountDto { Id = p.Discount.Id, DiscountPercent = p.Discount.DiscountPercent, IsActive = p.Discount.IsActive, StartDate = p.Discount.StartDate, EndDate = p.Discount.EndDate, Name = p.Discount.Name, Description = p.Discount.Description } : null,
						Images = p.Images.Where(i => i.DeletedAt == null).Select(i => new ImageDto { Id = i.Id, Url = i.Url }).ToList(),
						Variants = p.ProductVariants.Where(v => v.DeletedAt == null).Select(v => new ProductVariantDto 
						{ 
							Id = v.Id, 
							Color = v.Color, 
							Size = v.Size, 
							Price = v.Price, 
							Quantity = v.Quantity 
						}).ToList(),
						Collections = p.ProductCollections.Select(pc => new CollectionDto 
						{ 
							Id = pc.Collection.Id, 
							Name = pc.Collection.Name,
							Description = pc.Collection.Description,
							DisplayOrder = pc.Collection.DisplayOrder,
							IsActive = pc.Collection.IsActive
						}).ToList()
					})
					.ToListAsync();

				return Result<List<ProductDto>>.Ok(products, "All Products", 200);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error in GetAllAsync");
				await _errorNotificationService.SendErrorNotificationAsync(ex.Message, ex.StackTrace);
				return Result<List<ProductDto>>.Fail("Error retrieving products", 500);
			}
		}

		public async Task<Result<ProductDto>> GetProductByIdAsync(int id)
		{
			try
			{
				var product = await _unitOfWork.Product.GetAll()
					.Where(p => p.Id == id)
					.Select(p => new ProductDto
					{
						Id = p.Id,
						Name = p.Name,
						Description = p.Description,
						AvailableQuantity = p.Quantity,
						Gender = p.Gender,
						SubCategoryId = p.SubCategoryId,
						SubCategory = p.SubCategory != null ? new SubCategoryDto 
						{ 
							Id = p.SubCategory.Id, 
							Name = p.SubCategory.Name,
							Description = p.SubCategory.Description
						} : null,
						Discount = p.Discount != null ? new DiscountDto 
						{ 
							Id = p.Discount.Id, 
							DiscountPercent = p.Discount.DiscountPercent, 
							IsActive = p.Discount.IsActive,
							StartDate = p.Discount.StartDate,
							EndDate = p.Discount.EndDate,
							Name = p.Discount.Name,
							Description = p.Discount.Description
						} : null,
						Images = p.Images.Where(i => i.DeletedAt == null).Select(i => new ImageDto 
						{ 
							Id = i.Id, 
							Url = i.Url
						}).ToList(),
						Variants = p.ProductVariants.Where(v => v.DeletedAt == null).Select(v => new ProductVariantDto 
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
						}).ToList(),
						Collections = p.ProductCollections.Select(pc => new CollectionDto 
						{ 
							Id = pc.Collection.Id, 
							Name = pc.Collection.Name,
							Description = pc.Collection.Description,
							DisplayOrder = pc.Collection.DisplayOrder,
							IsActive = pc.Collection.IsActive
						}).ToList()
					})
					.FirstOrDefaultAsync();

				if (product == null)
					return Result<ProductDto>.Fail("Product not found", 404);

				return Result<ProductDto>.Ok(product, "Product retrieved successfully", 200);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, $"Error in GetProductByIdAsync for id: {id}");
				await _errorNotificationService.SendErrorNotificationAsync(ex.Message, ex.StackTrace);
				return Result<ProductDto>.Fail("Error retrieving product", 500);
			}
		}

		public async Task<Result<ProductDto>> CreateProductAsync(CreateProductDto dto, string userId)
		{
			_logger.LogInformation($"Creating new product: {dto.Name}");
			try
			{
				// Validate category exists
				var categoryExists = await _subCategoryServices.IsExsistAsync(dto.Subcategoryid);
				if (!categoryExists.Success)
					return Result<ProductDto>.Fail("Category not found", 404);

				// Validate variants have prices
				if (dto.Variants == null || !dto.Variants.Any())
					return Result<ProductDto>.Fail("At least one variant with price is required", 400);

				using var transaction = await _unitOfWork.BeginTransactionAsync();
				
				// Create product
				var product = new Models.Product
				{
					Name = dto.Name,
					Description = dto.Description,
					SubCategoryId = dto.Subcategoryid,
					Quantity = dto.Quantity,
					Gender = dto.Gender
				};

				// Handle variants
				if (dto.Variants != null && dto.Variants.Count > 0)
				{
					product.ProductVariants = dto.Variants.Select(v => new ProductVariant
					{
						Color = v.Color,
						Size = v.Size,
						Waist = v.Waist,
						Length = v.Length,
			
						Quantity = v.Quantity,
						Price = v.Price
					}).ToList();
				}

				// Handle collections
				if (dto.CollectionIds != null && dto.CollectionIds.Count > 0)
				{
					product.ProductCollections = dto.CollectionIds.Select(cid => new ProductCollection
					{
						CollectionId = cid
					}).ToList();
				}

				// Save product
				var result = await _unitOfWork.Product.CreateAsync(product);
				if (result == null)
				{
					await transaction.RollbackAsync();
					return Result<ProductDto>.Fail("Failed to create product", 400);
				}

				// Log admin operation
				await _adminOpreationServices.AddAdminOpreationAsync(
					$"Create Product {product.Id}",
					E_Commers.Enums.Opreations.AddOpreation,
					userId,
					product.Id
				);

				await _unitOfWork.CommitAsync();

				// Return created product with full details
				var productDto = await GetProductByIdAsync(product.Id);
				return Result<ProductDto>.Ok(productDto.Data, "Product created successfully", 201);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, $"Unexpected error in CreateProductAsync for product {dto.Name}");
				await _errorNotificationService.SendErrorNotificationAsync(ex.Message, ex.StackTrace);
				return Result<ProductDto>.Fail("Unexpected error occurred while creating product", 500);
			}
		}

		public async Task<Result<ProductDto>> UpdateProductAsync(int id, UpdateProductDto dto, string userId)
		{
			_logger.LogInformation($"Updating product: {id}");
			try
			{
				var product = await _unitOfWork.Product.GetAll()
					.Where(p => p.Id == id)
					.Include(p => p.ProductVariants)
					.Include(p => p.ProductCollections)
					.Include(p => p.Images)
					.FirstOrDefaultAsync();

				if (product == null)
					return Result<ProductDto>.Fail("Product not found", 404);

				// Update basic properties
				if (!string.IsNullOrEmpty(dto.Name))
					product.Name = dto.Name;
				if (!string.IsNullOrEmpty(dto.Description))
					product.Description = dto.Description;
				if (dto.CategoryId.HasValue)
					product.SubCategoryId = dto.CategoryId.Value;
				if (dto.Quantity.HasValue)
					product.Quantity = dto.Quantity.Value;

				// Handle collections (replace all for simplicity)
				if (dto.CollectionIds != null)
				{
					product.ProductCollections.Clear();
					product.ProductCollections = dto.CollectionIds.Select(cid => new ProductCollection
					{
						CollectionId = cid
					}).ToList();
				}

				var result = _unitOfWork.Product.Update(product);
				if (!result)
					return Result<ProductDto>.Fail("Failed to update product", 400);

				// Log admin operation
				await _adminOpreationServices.AddAdminOpreationAsync(
					$"Update Product {id}",
					E_Commers.Enums.Opreations.UpdateOpreation,
					userId,
					id
				);

				await _unitOfWork.CommitAsync();

				var productDto = await GetProductByIdAsync(id);
				return Result<ProductDto>.Ok(productDto.Data, "Product updated successfully", 200);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, $"Error in UpdateProductAsync for id: {id}");
				await _errorNotificationService.SendErrorNotificationAsync(ex.Message, ex.StackTrace);
				return Result<ProductDto>.Fail("Error updating product", 500);
			}
		}

		public async Task<Result<string>> DeleteProductAsync(int id, string userId)
		{
			_logger.LogInformation($"Deleting product: {id}");
			try
			{
				var product = await _unitOfWork.Product.GetByIdAsync(id);
				if (product == null)
					return Result<string>.Fail("Product not found", 404);

				var result = await _unitOfWork.Product.SoftDeleteAsync(id);
				if (!result)
					return Result<string>.Fail("Failed to delete product", 400);

				// Log admin operation
				await _adminOpreationServices.AddAdminOpreationAsync(
					$"Delete Product {id}",
					E_Commers.Enums.Opreations.DeleteOpreation,
					userId,
					id
				);

				await _unitOfWork.CommitAsync();
				return Result<string>.Ok("Product deleted successfully", "Product deleted", 200);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, $"Error in DeleteProductAsync for id: {id}");
				await _errorNotificationService.SendErrorNotificationAsync(ex.Message, ex.StackTrace);
				return Result<string>.Fail("Error deleting product", 500);
			}
		}

		public async Task<Result<ProductDto>> RestoreProductAsync(int id, string userId)
		{
			_logger.LogInformation($"Restoring product: {id}");
			try
			{
				var product = await _unitOfWork.Product.GetByIdAsync(id);
				if (product == null)
					return Result<ProductDto>.Fail("Product not found", 404);

				product.DeletedAt = null;
				var result = _unitOfWork.Product.Update(product);
				if (!result)
					return Result<ProductDto>.Fail("Failed to restore product", 400);

				// Log admin operation
				await _adminOpreationServices.AddAdminOpreationAsync(
					$"Restore Product {id}",
					E_Commers.Enums.Opreations.UpdateOpreation,
					userId,
					id
				);

				await _unitOfWork.CommitAsync();

				var productDto = await GetProductByIdAsync(id);
				return Result<ProductDto>.Ok(productDto.Data, "Product restored successfully", 200);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, $"Error in RestoreProductAsync for id: {id}");
				await _errorNotificationService.SendErrorNotificationAsync(ex.Message, ex.StackTrace);
				return Result<ProductDto>.Fail("Error restoring product", 500);
			}
		}

		public async Task<Result<List<ProductDto>>> GetProductsByCategoryId(int categoryId)
		{
			try
			{
				var isfound = await _subCategoryServices.IsExsistAsync(categoryId);
				if (!isfound.Success)
					return Result<List<ProductDto>>.Fail($"No Category with this id:{categoryId}", 404);

				var productsQuery = _unitOfWork.Product.GetProductsByCategory(categoryId);
				if (productsQuery == null)
					return Result<List<ProductDto>>.Fail("No Products Found", 404);

				var products = await productsQuery
					.Select(p => new ProductDto
					{
						Id = p.Id,
						Name = p.Name,
						Description = p.Description,
						AvailableQuantity = p.Quantity,
						Gender = p.Gender,
						SubCategoryId = p.SubCategoryId,
						SubCategory = p.SubCategory != null ? new SubCategoryDto { Id = p.SubCategory.Id, Name = p.SubCategory.Name } : null,
						Discount = p.Discount != null ? new DiscountDto { Id = p.Discount.Id, DiscountPercent = p.Discount.DiscountPercent, IsActive = p.Discount.IsActive, StartDate = p.Discount.StartDate, EndDate = p.Discount.EndDate, Name = p.Discount.Name, Description = p.Discount.Description } : null,
						Images = p.Images.Where(i => i.DeletedAt == null).Select(i => new ImageDto { Id = i.Id, Url = i.Url }).ToList(),
						Variants = p.ProductVariants.Where(v => v.DeletedAt == null).Select(v => new ProductVariantDto 
						{ 
							Id = v.Id, 
							Color = v.Color, 
							Size = v.Size, 
							Price = v.Price, 
							Quantity = v.Quantity 
						}).ToList(),
						Collections = p.ProductCollections.Select(pc => new CollectionDto 
						{ 
							Id = pc.Collection.Id, 
							Name = pc.Collection.Name,
							Description = pc.Collection.Description,
							DisplayOrder = pc.Collection.DisplayOrder,
							IsActive = pc.Collection.IsActive
						}).ToList()
					})
					.ToListAsync();

				return Result<List<ProductDto>>.Ok(products, "Products by Category", 200);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, $"Error in GetProductsByCategoryId for categoryId: {categoryId}");
				await _errorNotificationService.SendErrorNotificationAsync(ex.Message, ex.StackTrace);
				return Result<List<ProductDto>>.Fail("Error retrieving products by category", 500);
			}
		}

		public async Task<Result<List<ProductDto>>> FilterAsync(string? search, bool? isActive, bool includeDeleted, int page, int pageSize, string role)
		{
			try
			{
				var (productsQuery, totalCount) = await _unitOfWork.Product.GetProductsWithPagination(search, isActive, includeDeleted, page, pageSize);
				
				if (productsQuery == null)
					return Result<List<ProductDto>>.Fail("No Products Found", 404);

				var products = await productsQuery
					.Skip((page - 1) * pageSize)
					.Take(pageSize)
					.Select(p => new ProductDto
					{
						Id = p.Id,
						Name = p.Name,
						Description = p.Description,
						AvailableQuantity = p.Quantity,
						Gender = p.Gender,
						SubCategoryId = p.SubCategoryId,
						SubCategory = p.SubCategory != null ? new SubCategoryDto { Id = p.SubCategory.Id, Name = p.SubCategory.Name } : null,
						Discount = p.Discount != null ? new DiscountDto { Id = p.Discount.Id, DiscountPercent = p.Discount.DiscountPercent, IsActive = p.Discount.IsActive, StartDate = p.Discount.StartDate, EndDate = p.Discount.EndDate, Name = p.Discount.Name, Description = p.Discount.Description } : null,
						Images = p.Images.Where(i => i.DeletedAt == null).Select(i => new ImageDto { Id = i.Id, Url = i.Url }).ToList(),
						Variants = p.ProductVariants.Where(v => v.DeletedAt == null).Select(v => new ProductVariantDto 
						{ 
							Id = v.Id, 
							Color = v.Color, 
							Size = v.Size, 
							Price = v.Price, 
							Quantity = v.Quantity 
						}).ToList(),
						Collections = p.ProductCollections.Select(pc => new CollectionDto 
						{ 
							Id = pc.Collection.Id, 
							Name = pc.Collection.Name,
							Description = pc.Collection.Description,
							DisplayOrder = pc.Collection.DisplayOrder,
							IsActive = pc.Collection.IsActive
						}).ToList()
					})
					.ToListAsync();

				return Result<List<ProductDto>>.Ok(products, $"Found {products.Count} products out of {totalCount}", 200);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error in FilterAsync");
				await _errorNotificationService.SendErrorNotificationAsync(ex.Message, ex.StackTrace);
				return Result<List<ProductDto>>.Fail("Error filtering products", 500);
			}
		}
	}
} 