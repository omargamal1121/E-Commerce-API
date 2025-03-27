using E_Commers.Context;
using E_Commers.Helper;
using E_Commers.Interfaces;
using E_Commers.Models;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;
using System.Collections;

namespace E_Commers.Repository
{
	public class ProductRepository : MainRepository<Product>, IProductRepository
	{
		private readonly DbSet<Product> _entity;
		private readonly ILogger<ProductRepository> _logger;
		private readonly IConnectionMultiplexer _redis;

		public ProductRepository(IConnectionMultiplexer redis, AppDbContext context, ILogger<ProductRepository> logger) : base(redis,context, logger)
		{
			_redis = redis;
			_logger = logger;
			_entity = context.Products;
		}

		public async Task<IEnumerable<Product>> GetProductsByCategoryAsync(int categoryId)
		{
			_logger.LogInformation($"Executing {nameof(GetProductsByCategoryAsync)} categoryId:{categoryId}");

			return await _entity.Where(p => p.CategoryId == categoryId)
								.AsNoTracking()
								.ToListAsync();
		}

		public async Task<ResultDto<bool>> UpdatePriceAsync(int productId, decimal newPrice)
		{
			_logger.LogInformation($"Executing {nameof(UpdatePriceAsync)} productId:{productId} NewPrice:{newPrice}");

			var product = await _entity.FindAsync(productId);
			if (product is null)
			{
				_logger.LogWarning($"Product ID {productId} not found.");
				return ResultDto<bool>.Fail($"No Product Found with ID: {productId}");
			}

			if (newPrice <= 0)
			{
				_logger.LogWarning($"Invalid price: {newPrice}. Must be greater than zero.");
				return ResultDto<bool>.Fail($"Invalid price: {newPrice}. Must be greater than zero.");
			}

			if (product.Price == newPrice)
			{
				_logger.LogWarning($"Product ID {productId} already has this price.");
				return ResultDto<bool>.Fail($"Product ID {productId} already has this price.");
			}

			product.Price = newPrice;

			_logger.LogInformation($"Price updated for product ID {productId}, awaiting commit.");
			return ResultDto<bool>.Ok(true, "Price updated successfully.");
		}

		public async Task<ResultDto<bool>> UpdateQuantityAsync(int productId, int quantity)
		{
			_logger.LogInformation($"Executing {nameof(UpdateQuantityAsync)} productId:{productId} Quantity:{quantity} ");

			var product = await _entity.FindAsync(productId);
			if (product is null)
			{
				_logger.LogWarning($"Product ID {productId} not found.");
				return ResultDto<bool>.Fail($"No Product Found with ID: {productId}");
			}

			if (quantity <= 0)
			{
				_logger.LogWarning($"Invalid quantity: {quantity}. Must be greater than zero.");
				return ResultDto<bool>.Fail($"Invalid quantity: {quantity}. Must be greater than zero.");
			}

			if (product.Quantity == quantity)
			{
				_logger.LogWarning($"Product ID {productId} already has this quantity.");
				return ResultDto<bool>.Fail($"Product ID {productId} already has this quantity.");
			}

			product.Quantity = quantity;

			_logger.LogInformation($"Quantity updated for product ID {productId}, awaiting commit.");
			return ResultDto<bool>.Ok(true, "Quantity updated successfully.");
		}

		public async Task<List<Product>> GetProductsByInventoryAsync(int inventoryId)
		{
			_logger.LogInformation($"Executing {nameof(GetProductsByInventoryAsync)} inventoryId:{inventoryId}");

			return await _entity.Where(p => p.InventoryId == inventoryId)
								.AsNoTracking()
								.ToListAsync();
		}
	}
}
