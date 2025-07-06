using E_Commers.Context;
using E_Commers.Services;
using E_Commers.Interfaces;
using E_Commers.Models;
using E_Commers.Enums;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;
using System.Collections;

namespace E_Commers.Repository
{
	public class ProductRepository : MainRepository<Product>, IProductRepository
	{
		private readonly DbSet<Product> _entity;
		private readonly ILogger<ProductRepository> _logger;

		public ProductRepository( AppDbContext context, ILogger<ProductRepository> logger) : base(context, logger)
		{
			_logger = logger;
			_entity = context.Products;
		}

		// Basic Operations
		public async Task<bool> ProductExistsAsync(int id)
		{
			_logger.LogInformation($"Checking if product exists: {id}");
			return await _entity.AnyAsync(p => p.Id == id && p.DeletedAt == null);
		}

		public async Task<Product?> GetProductById(int id, bool isActiveFilter = false)
		{
			_logger.LogInformation($"Getting product by ID: {id}, isActiveFilter: {isActiveFilter}");
			var query = _entity.Where(p => p.Id == id);
			
			if (isActiveFilter)
				query = query.Where(p => p.DeletedAt == null);
			
			return await query.FirstOrDefaultAsync();
		}

		public async Task<bool> UpdatePriceAsync(int productId, decimal newPrice)
		{
			_logger.LogInformation($"Updating price for product {productId} to {newPrice}");
			// Price is now managed through variants, so this method should be deprecated
			// or updated to work with variants
			_logger.LogWarning("UpdatePriceAsync is deprecated. Use variant-specific price updates instead.");
			return false;
		}

		public async Task<bool> UpdateQuantityAsync(int productId, int quantity)
		{
			_logger.LogInformation($"Updating quantity for product {productId} to {quantity}");
			var product = await _entity.FindAsync(productId);
			if (product is null)
			{
				_logger.LogWarning($"Product ID {productId} not found.");
				return false;
			}

			if (quantity < 0)
			{
				_logger.LogWarning($"Invalid quantity: {quantity}. Must be non-negative.");
				return false;
			}

			product.Quantity = quantity;
			product.ModifiedAt = DateTime.UtcNow;
			_logger.LogInformation($"Quantity updated for product ID {productId}");
			return true;
		}

		public async Task<bool> SoftDeleteAsync(int productId)
		{
			var product = await _entity.FindAsync(productId);
			if (product == null)
			{
				_logger.LogWarning($"Product ID {productId} not found for soft delete.");
				return false;
			}
			product.DeletedAt = DateTime.UtcNow;
			_logger.LogInformation($"Soft deleted product ID {productId}.");
			return true;
		}

		// Filtering and Search
		public IQueryable<Product> GetProductsByCategory(int categoryId)
		{
			_logger.LogInformation($"Getting products by category: {categoryId}");
			return _entity
				.Where(p => p.SubCategoryId == categoryId && p.DeletedAt == null)
				.AsNoTracking();
		}

		public IQueryable<Product> GetProductsByInventory(int inventoryId)
		{
			_logger.LogInformation($"Getting products by inventory: {inventoryId}");
			return _entity
				.Where(p => p.InventoryEntries.Any(i => i.WarehouseId == inventoryId) && p.DeletedAt == null)
				.AsNoTracking();
		}

		public IQueryable<Product> GetProductsByPriceRange(decimal minPrice, decimal maxPrice)
		{
			_logger.LogInformation($"Getting products by price range: {minPrice} - {maxPrice}");
			return _entity
				.Where(p => p.ProductVariants.Any(v => v.Price >= minPrice && v.Price <= maxPrice) && p.DeletedAt == null)
				.AsNoTracking();
		}

		public IQueryable<Product> GetProductsByGender(Gender gender)
		{
			_logger.LogInformation($"Getting products by gender: {gender}");
			return _entity
				.Where(p => p.Gender == gender && p.DeletedAt == null)
				.AsNoTracking();
		}

		public IQueryable<Product> GetProductsInStock()
		{
			_logger.LogInformation("Getting products in stock");
			return _entity
				.Where(p => p.Quantity > 0 && p.DeletedAt == null)
				.AsNoTracking();
		}

		public IQueryable<Product> GetDiscountedProducts()
		{
			_logger.LogInformation("Getting discounted products");
			return _entity
				.Where(p => p.DiscountId != null && p.DeletedAt == null)
				.AsNoTracking();
		}

		public IQueryable<Product> GetProductsBySearch(string searchTerm)
		{
			_logger.LogInformation($"Searching products with term: {searchTerm}");
			return _entity
				.Where(p => (p.Name.Contains(searchTerm) || p.Description.Contains(searchTerm)) && p.DeletedAt == null)
				.AsNoTracking();
		}

		public IQueryable<Product> GetProductsByStatus(bool isActive, bool includeDeleted = false)
		{
			_logger.LogInformation($"Getting products by status: isActive={isActive}, includeDeleted={includeDeleted}");
			var query = _entity.AsQueryable();
			
			if (!includeDeleted)
				query = query.Where(p => p.DeletedAt == null);
			
			return query.AsNoTracking();
		}

		// Pagination
		public async Task<(IQueryable<Product> Products, int TotalCount)> GetProductsWithPagination(
			string? search, bool? isActive, bool includeDeleted, int page, int pageSize)
		{
			_logger.LogInformation($"Getting products with pagination: page={page}, pageSize={pageSize}");
			
			var query = _entity.AsQueryable();

			// Apply filters
			if (!string.IsNullOrEmpty(search))
				query = query.Where(p => p.Name.Contains(search) || p.Description.Contains(search));

			if (!includeDeleted)
				query = query.Where(p => p.DeletedAt == null);

			// Get total count
			var totalCount = await query.CountAsync();

			// Return queryable for further processing
			return (query, totalCount);
		}

		// Statistics
		public async Task<int> GetTotalProductCountAsync()
		{
			return await _entity.CountAsync();
		}

		public async Task<int> GetActiveProductCountAsync()
		{
			return await _entity.CountAsync(p => p.DeletedAt == null);
		}

		public async Task<decimal> GetAverageProductPriceAsync()
		{
			return await _entity
				.Where(p => p.DeletedAt == null && p.ProductVariants.Any())
				.SelectMany(p => p.ProductVariants)
				.AverageAsync(v => v.Price);
		}

		public IQueryable<Product> GetTopSellingProducts(int count)
		{
			// This would need to be implemented based on actual sales data
			// For now, returning products with highest quantity
			return _entity
				.Where(p => p.DeletedAt == null)
				.OrderByDescending(p => p.Quantity)
				.Take(count)
				.AsNoTracking();
		}

		public IQueryable<Product> GetLowStockProducts(int threshold)
		{
			return _entity
				.Where(p => p.Quantity <= threshold && p.DeletedAt == null)
				.AsNoTracking();
		}

		// Related Data
		public IQueryable<Product> GetProductsWithVariants()
		{
			return _entity
				.Where(p => p.DeletedAt == null)
				.AsNoTracking();
		}

		public IQueryable<Product> GetProductsWithReviews()
		{
			return _entity
				.Where(p => p.DeletedAt == null)
				.AsNoTracking();
		}

		public IQueryable<Product> GetProductsWithImages()
		{
			return _entity
				.Where(p => p.DeletedAt == null)
				.AsNoTracking();
		}
	}
}
