using E_Commers.Context;
using E_Commers.Services;
using E_Commers.Interfaces;
using E_Commers.Models;
using Microsoft.EntityFrameworkCore;

namespace E_Commers.Repository
{
	public class ProductVariantRepository : MainRepository<ProductVariant>, IProductVariantRepository
	{
		private readonly DbSet<ProductVariant> _entity;
		private readonly ILogger<ProductVariantRepository> _logger;

		public ProductVariantRepository(AppDbContext context, ILogger<ProductVariantRepository> logger) : base(context, logger)
		{
			_logger = logger;
			_entity = context.ProductVariants;
		}

		// Basic Operations
		public async Task<bool> VariantExistsAsync(int id)
		{
			_logger.LogInformation($"Checking if variant exists: {id}");
			return await _entity.AnyAsync(v => v.Id == id && v.DeletedAt == null);
		}

		public async Task<ProductVariant?> GetVariantById(int id)
		{
			_logger.LogInformation($"Getting variant by ID: {id}");
			return await _entity
				.Where(v => v.Id == id && v.DeletedAt == null)
				.Include(v => v.Product)
				.FirstOrDefaultAsync();
		}

		public async Task<List<ProductVariant>> GetVariantsByProductId(int productId)
		{
			_logger.LogInformation($"Getting variants by product ID: {productId}");
			return await _entity
				.Where(v => v.ProductId == productId && v.DeletedAt == null)
				.Include(v => v.Product)
				.AsNoTracking()
				.ToListAsync();
		}

		// Price Management
		public async Task<bool> UpdateVariantPriceAsync(int variantId, decimal newPrice)
		{
			_logger.LogInformation($"Updating price for variant {variantId} to {newPrice}");
			var variant = await _entity.FindAsync(variantId);
			if (variant is null)
			{
				_logger.LogWarning($"Variant ID {variantId} not found.");
				return false;
			}

			if (newPrice <= 0)
			{
				_logger.LogWarning($"Invalid price: {newPrice}. Must be greater than zero.");
				return false;
			}

			variant.Price = newPrice;
			variant.ModifiedAt = DateTime.UtcNow;
			_logger.LogInformation($"Price updated for variant ID {variantId}");
			return true;
		}

		public async Task<bool> UpdateVariantQuantityAsync(int variantId, int newQuantity)
		{
			_logger.LogInformation($"Updating quantity for variant {variantId} to {newQuantity}");
			var variant = await _entity.FindAsync(variantId);
			if (variant is null)
			{
				_logger.LogWarning($"Variant ID {variantId} not found.");
				return false;
			}

			if (newQuantity < 0)
			{
				_logger.LogWarning($"Invalid quantity: {newQuantity}. Must be non-negative.");
				return false;
			}

			variant.Quantity = newQuantity;
			variant.ModifiedAt = DateTime.UtcNow;
			_logger.LogInformation($"Quantity updated for variant ID {variantId}");
			return true;
		}

		// Search and Filter
		public async Task<List<ProductVariant>> GetVariantsByColorAsync(string color)
		{
			_logger.LogInformation($"Getting variants by color: {color}");
			return await _entity
				.Where(v => v.Color == color && v.DeletedAt == null)
				.Include(v => v.Product)
				.AsNoTracking()
				.ToListAsync();
		}

		public async Task<List<ProductVariant>> GetVariantsBySizeAsync(string size)
		{
			_logger.LogInformation($"Getting variants by size: {size}");
			return await _entity
				.Where(v => v.Size == size && v.DeletedAt == null)
				.Include(v => v.Product)
				.AsNoTracking()
				.ToListAsync();
		}

		public async Task<List<ProductVariant>> GetVariantsByPriceRangeAsync(decimal minPrice, decimal maxPrice)
		{
			_logger.LogInformation($"Getting variants by price range: {minPrice} - {maxPrice}");
			return await _entity
				.Where(v => v.Price >= minPrice && v.Price <= maxPrice && v.DeletedAt == null)
				.Include(v => v.Product)
				.AsNoTracking()
				.ToListAsync();
		}

		public async Task<List<ProductVariant>> GetVariantsInStockAsync()
		{
			_logger.LogInformation("Getting variants in stock");
			return await _entity
				.Where(v => v.Quantity > 0 && v.DeletedAt == null)
				.Include(v => v.Product)
				.AsNoTracking()
				.ToListAsync();
		}

		// Statistics
		public async Task<decimal> GetAverageVariantPriceAsync()
		{
			return await _entity
				.Where(v => v.DeletedAt == null)
				.AverageAsync(v => v.Price);
		}

		public async Task<decimal> GetMinVariantPriceAsync()
		{
			return await _entity
				.Where(v => v.DeletedAt == null)
				.MinAsync(v => v.Price);
		}

		public async Task<decimal> GetMaxVariantPriceAsync()
		{
			return await _entity
				.Where(v => v.DeletedAt == null)
				.MaxAsync(v => v.Price);
		}
	}
} 