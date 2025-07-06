using E_Commers.Services;
using E_Commers.Models;
using E_Commers.Enums;

namespace E_Commers.Interfaces
{
	public interface IProductRepository:IRepository<Product>
	{
		// Basic Operations
		public Task<bool> ProductExistsAsync(int id);
		public Task<Product?> GetProductById(int id, bool isActiveFilter = false);
		public Task<bool> UpdateQuantityAsync(int productId, int quantity);
		public Task<bool> SoftDeleteAsync(int productId);

		// Filtering and Search
		public IQueryable<Product> GetProductsByCategory(int categoryId);
		public IQueryable<Product> GetProductsByInventory(int InventoryId);
		public IQueryable<Product> GetProductsByPriceRange(decimal minPrice, decimal maxPrice);
		public IQueryable<Product> GetProductsByGender(Gender gender);
		public IQueryable<Product> GetProductsInStock();
		public IQueryable<Product> GetDiscountedProducts();
		public IQueryable<Product> GetProductsBySearch(string searchTerm);
		public IQueryable<Product> GetProductsByStatus(bool isActive, bool includeDeleted = false);

		// Pagination
		public Task<(IQueryable<Product> Products, int TotalCount)> GetProductsWithPagination(
			string? search, bool? isActive, bool includeDeleted, int page, int pageSize);

		// Statistics
		public Task<int> GetTotalProductCountAsync();
		public Task<int> GetActiveProductCountAsync();
		public Task<decimal> GetAverageProductPriceAsync();
		public IQueryable<Product> GetTopSellingProducts(int count);
		public IQueryable<Product> GetLowStockProducts(int threshold);

		// Related Data
		public IQueryable<Product> GetProductsWithVariants();
		public IQueryable<Product> GetProductsWithReviews();
		public IQueryable<Product> GetProductsWithImages();
	}
}
