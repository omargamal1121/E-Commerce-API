using E_Commers.DtoModels.CategoryDtos;
using E_Commers.DtoModels.CollectionDtos;
using E_Commers.DtoModels.DiscoutDtos;
using E_Commers.DtoModels.ImagesDtos;
using E_Commers.DtoModels.ProductDtos;
using E_Commers.DtoModels.Responses;
using E_Commers.Enums;
using E_Commers.ErrorHnadling;
using E_Commers.Interfaces;
using E_Commers.Services.EmailServices;
using E_Commers.UOW;
using Microsoft.EntityFrameworkCore;

namespace E_Commers.Services.Product
{
	public interface IProductSearchService
	{
		Task<Result<List<ProductDto>>> SearchProductsAsync(string searchTerm, int page, int pageSize);
		Task<Result<List<ProductDto>>> FilterByPriceRangeAsync(decimal minPrice, decimal maxPrice, int page, int pageSize);
		Task<Result<List<ProductDto>>> FilterByCategoryAsync(int categoryId, int page, int pageSize);
		Task<Result<List<ProductDto>>> FilterByGenderAsync(Gender gender, int page, int pageSize);
		Task<Result<List<ProductDto>>> FilterByAvailabilityAsync(bool inStock, int page, int pageSize);
		Task<Result<List<ProductDto>>> GetProductsOnSaleAsync(int page, int pageSize);
		Task<Result<List<ProductDto>>> GetNewArrivalsAsync(int page, int pageSize);
		Task<Result<List<ProductDto>>> GetBestSellersAsync(int page, int pageSize);
		Task<Result<List<ProductDto>>> AdvancedSearchAsync(AdvancedSearchDto searchCriteria, int page, int pageSize);
	}

	public class ProductSearchService : IProductSearchService
	{
		private readonly IUnitOfWork _unitOfWork;
		private readonly ILogger<ProductSearchService> _logger;
		private readonly IErrorNotificationService _errorNotificationService;

		public ProductSearchService(
			IUnitOfWork unitOfWork,
			ILogger<ProductSearchService> logger,
			IErrorNotificationService errorNotificationService)
		{
			_unitOfWork = unitOfWork;
			_logger = logger;
			_errorNotificationService = errorNotificationService;
		}

		public async Task<Result<List<ProductDto>>> SearchProductsAsync(string searchTerm, int page, int pageSize)
		{
			try
			{
				if (string.IsNullOrWhiteSpace(searchTerm))
					return Result<List<ProductDto>>.Fail("Search term is required", 400);

				var products = await _unitOfWork.Product.GetAll()
					.Where(p => p.DeletedAt == null && 
						(p.Name.Contains(searchTerm) || p.Description.Contains(searchTerm)))
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

				if (!products.Any())
					return Result<List<ProductDto>>.Fail($"No products found matching '{searchTerm}'", 404);

				return Result<List<ProductDto>>.Ok(products, $"Found {products.Count} products matching '{searchTerm}'", 200);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, $"Error in SearchProductsAsync for searchTerm: {searchTerm}");
				await _errorNotificationService.SendErrorNotificationAsync(ex.Message, ex.StackTrace);
				return Result<List<ProductDto>>.Fail("Error searching products", 500);
			}
		}

		public async Task<Result<List<ProductDto>>> FilterByPriceRangeAsync(decimal minPrice, decimal maxPrice, int page, int pageSize)
		{
			try
			{
				if (minPrice < 0 || maxPrice < 0 || minPrice > maxPrice)
					return Result<List<ProductDto>>.Fail("Invalid price range", 400);

				var products = await _unitOfWork.Product.GetAll()
					.Where(p => p.DeletedAt == null && 
						p.ProductVariants.Any(v => v.DeletedAt == null && v.Price >= minPrice && v.Price <= maxPrice))
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
						Images = p.Images.Where(i => i.DeletedAt == null).Select(i => new ImageDto { Id = i.Id, Url = i.Url }).ToList(),
						Variants = p.ProductVariants.Where(v => v.DeletedAt == null && v.Price >= minPrice && v.Price <= maxPrice)
							.Select(v => new ProductVariantDto 
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

				if (!products.Any())
					return Result<List<ProductDto>>.Fail($"No products found in price range ${minPrice} - ${maxPrice}", 404);

				return Result<List<ProductDto>>.Ok(products, $"Found {products.Count} products in price range ${minPrice} - ${maxPrice}", 200);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, $"Error in FilterByPriceRangeAsync for range: {minPrice}-{maxPrice}");
				await _errorNotificationService.SendErrorNotificationAsync(ex.Message, ex.StackTrace);
				return Result<List<ProductDto>>.Fail("Error filtering by price range", 500);
			}
		}

		public async Task<Result<List<ProductDto>>> FilterByCategoryAsync(int categoryId, int page, int pageSize)
		{
			try
			{
				var products = await _unitOfWork.Product.GetAll()
					.Where(p => p.DeletedAt == null && p.SubCategoryId == categoryId)
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

				if (!products.Any())
					return Result<List<ProductDto>>.Fail($"No products found in category {categoryId}", 404);

				return Result<List<ProductDto>>.Ok(products, $"Found {products.Count} products in category", 200);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, $"Error in FilterByCategoryAsync for categoryId: {categoryId}");
				await _errorNotificationService.SendErrorNotificationAsync(ex.Message, ex.StackTrace);
				return Result<List<ProductDto>>.Fail("Error filtering by category", 500);
			}
		}

		public async Task<Result<List<ProductDto>>> FilterByGenderAsync(Gender gender, int page, int pageSize)
		{
			try
			{
				var products = await _unitOfWork.Product.GetAll()
					.Where(p => p.DeletedAt == null && p.Gender == gender)
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

				if (!products.Any())
					return Result<List<ProductDto>>.Fail($"No products found for gender: {gender}", 404);

				return Result<List<ProductDto>>.Ok(products, $"Found {products.Count} products for {gender}", 200);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, $"Error in FilterByGenderAsync for gender: {gender}");
				await _errorNotificationService.SendErrorNotificationAsync(ex.Message, ex.StackTrace);
				return Result<List<ProductDto>>.Fail("Error filtering by gender", 500);
			}
		}

		public async Task<Result<List<ProductDto>>> FilterByAvailabilityAsync(bool inStock, int page, int pageSize)
		{
			try
			{
				var products = await _unitOfWork.Product.GetAll()
					.Where(p => p.DeletedAt == null && 
						(inStock ? p.ProductVariants.Any(v => v.DeletedAt == null && v.Quantity > 0) : 
							p.ProductVariants.All(v => v.DeletedAt == null && v.Quantity == 0)))
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

				var status = inStock ? "in stock" : "out of stock";
				if (!products.Any())
					return Result<List<ProductDto>>.Fail($"No products found {status}", 404);

				return Result<List<ProductDto>>.Ok(products, $"Found {products.Count} products {status}", 200);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, $"Error in FilterByAvailabilityAsync for inStock: {inStock}");
				await _errorNotificationService.SendErrorNotificationAsync(ex.Message, ex.StackTrace);
				return Result<List<ProductDto>>.Fail("Error filtering by availability", 500);
			}
		}

		public async Task<Result<List<ProductDto>>> GetProductsOnSaleAsync(int page, int pageSize)
		{
			try
			{
				var products = await _unitOfWork.Product.GetAll()
					.Where(p => p.DeletedAt == null && 
						p.Discount != null && 
						p.Discount.IsActive && 
						p.Discount.DeletedAt == null &&
						p.Discount.StartDate <= DateTime.UtcNow &&
						p.Discount.EndDate >= DateTime.UtcNow)
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
						Discount = new DiscountDto 
						{ 
							Id = p.Discount.Id, 
							DiscountPercent = p.Discount.DiscountPercent, 
							IsActive = p.Discount.IsActive,
							StartDate = p.Discount.StartDate,
							EndDate = p.Discount.EndDate
						},
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

				if (!products.Any())
					return Result<List<ProductDto>>.Fail("No products currently on sale", 404);

				return Result<List<ProductDto>>.Ok(products, $"Found {products.Count} products on sale", 200);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error in GetProductsOnSaleAsync");
				await _errorNotificationService.SendErrorNotificationAsync(ex.Message, ex.StackTrace);
				return Result<List<ProductDto>>.Fail("Error retrieving products on sale", 500);
			}
		}

		public async Task<Result<List<ProductDto>>> GetNewArrivalsAsync(int page, int pageSize)
		{
			try
			{
				var thirtyDaysAgo = DateTime.UtcNow.AddDays(-30);
				var products = await _unitOfWork.Product.GetAll()
					.Where(p => p.DeletedAt == null && p.CreatedAt >= thirtyDaysAgo)
					.OrderByDescending(p => p.CreatedAt)
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

				if (!products.Any())
					return Result<List<ProductDto>>.Fail("No new arrivals found", 404);

				return Result<List<ProductDto>>.Ok(products, $"Found {products.Count} new arrivals", 200);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error in GetNewArrivalsAsync");
				await _errorNotificationService.SendErrorNotificationAsync(ex.Message, ex.StackTrace);
				return Result<List<ProductDto>>.Fail("Error retrieving new arrivals", 500);
			}
		}

		public async Task<Result<List<ProductDto>>> GetBestSellersAsync(int page, int pageSize)
		{
			try
			{
				// This would typically involve order history analysis
				// For now, we'll return products with highest quantities sold (placeholder logic)
				var products = await _unitOfWork.Product.GetAll()
					.Where(p => p.DeletedAt == null)
					.OrderByDescending(p => p.Quantity) // Placeholder: should be based on actual sales
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

				if (!products.Any())
					return Result<List<ProductDto>>.Fail("No best sellers found", 404);

				return Result<List<ProductDto>>.Ok(products, $"Found {products.Count} best sellers", 200);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error in GetBestSellersAsync");
				await _errorNotificationService.SendErrorNotificationAsync(ex.Message, ex.StackTrace);
				return Result<List<ProductDto>>.Fail("Error retrieving best sellers", 500);
			}
		}

		public async Task<Result<List<ProductDto>>> AdvancedSearchAsync(AdvancedSearchDto searchCriteria, int page, int pageSize)
		{
			try
			{
				var query = _unitOfWork.Product.GetAll().Where(p => p.DeletedAt == null);

				// Apply search criteria
				if (!string.IsNullOrWhiteSpace(searchCriteria.SearchTerm))
				{
					query = query.Where(p => p.Name.Contains(searchCriteria.SearchTerm) || 
						p.Description.Contains(searchCriteria.SearchTerm));
				}

				if (searchCriteria.CategoryId.HasValue)
				{
					query = query.Where(p => p.SubCategoryId == searchCriteria.CategoryId.Value);
				}

				if (searchCriteria.Gender.HasValue)
				{
					query = query.Where(p => p.Gender == searchCriteria.Gender.Value);
				}

				if (searchCriteria.MinPrice.HasValue || searchCriteria.MaxPrice.HasValue)
				{
					if (searchCriteria.MinPrice.HasValue && searchCriteria.MaxPrice.HasValue)
					{
						query = query.Where(p => p.ProductVariants.Any(v => v.DeletedAt == null && 
							v.Price >= searchCriteria.MinPrice.Value && v.Price <= searchCriteria.MaxPrice.Value));
					}
					else if (searchCriteria.MinPrice.HasValue)
					{
						query = query.Where(p => p.ProductVariants.Any(v => v.DeletedAt == null && 
							v.Price >= searchCriteria.MinPrice.Value));
					}
					else
					{
						query = query.Where(p => p.ProductVariants.Any(v => v.DeletedAt == null && 
							v.Price <= searchCriteria.MaxPrice.Value));
					}
				}

				if (searchCriteria.InStock.HasValue)
				{
					if (searchCriteria.InStock.Value)
					{
						query = query.Where(p => p.ProductVariants.Any(v => v.DeletedAt == null && v.Quantity > 0));
					}
					else
					{
						query = query.Where(p => p.ProductVariants.All(v => v.DeletedAt == null && v.Quantity == 0));
					}
				}

				if (searchCriteria.OnSale.HasValue && searchCriteria.OnSale.Value)
				{
					query = query.Where(p => p.Discount != null && 
						p.Discount.IsActive && 
						p.Discount.DeletedAt == null &&
						p.Discount.StartDate <= DateTime.UtcNow &&
						p.Discount.EndDate >= DateTime.UtcNow);
				}

				// Apply sorting
				switch (searchCriteria.SortBy?.ToLower())
				{
					case "name":
						query = searchCriteria.SortDescending ? query.OrderByDescending(p => p.Name) : query.OrderBy(p => p.Name);
						break;
					case "price":
						query = searchCriteria.SortDescending ? 
							query.OrderByDescending(p => p.ProductVariants.Max(v => v.Price)) : 
							query.OrderBy(p => p.ProductVariants.Min(v => v.Price));
						break;
					case "newest":
						query = searchCriteria.SortDescending ? query.OrderByDescending(p => p.CreatedAt) : query.OrderBy(p => p.CreatedAt);
						break;
					default:
						query = query.OrderBy(p => p.Name);
						break;
				}

				var products = await query
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

				if (!products.Any())
					return Result<List<ProductDto>>.Fail("No products found matching the search criteria", 404);

				return Result<List<ProductDto>>.Ok(products, $"Found {products.Count} products matching search criteria", 200);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error in AdvancedSearchAsync");
				await _errorNotificationService.SendErrorNotificationAsync(ex.Message, ex.StackTrace);
				return Result<List<ProductDto>>.Fail("Error performing advanced search", 500);
			}
		}
	}

	// DTO for advanced search criteria
	public class AdvancedSearchDto
	{
		public string? SearchTerm { get; set; }
		public int? CategoryId { get; set; }
		public Gender? Gender { get; set; }
		public decimal? MinPrice { get; set; }
		public decimal? MaxPrice { get; set; }
		public bool? InStock { get; set; }
		public bool? OnSale { get; set; }
		public string? SortBy { get; set; }
		public bool SortDescending { get; set; } = false;
	}
} 