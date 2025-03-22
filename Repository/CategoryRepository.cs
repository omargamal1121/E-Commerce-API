using E_Commers.Context;
using E_Commers.Interfaces;
using E_Commers.Models;
using Microsoft.EntityFrameworkCore;

namespace E_Commers.Repository
{
	public class CategoryRepository : MainRepository<Category>, ICategoryRepository
	{

		private readonly DbSet<Category> _categories;
		private readonly ILogger<CategoryRepository> _logger;

		public CategoryRepository(AppDbContext context, ILogger<CategoryRepository> logger) : base(context, logger)
		{

			_categories = context.Categories; 
			_logger = logger;
		}

		public async Task<bool> CategoryExistsAsync(int id)
		{
			_logger.LogInformation($"Executing {nameof(CategoryExistsAsync)} Id: {id}");
			return await _categories.SingleOrDefaultAsync(c => c.Id == id) != null; 
		}

		public async Task<IEnumerable<Category>> GetAllCategoriesAsync()
		{
			_logger.LogInformation($"Executing {nameof(GetAllCategoriesAsync)}");
			return await _categories.AsNoTracking().Where(c=>c.DeletedAt == null).ToListAsync();
		}

		public async Task<Category?> GetByNameAsync(string Name)
		{
			_logger.LogInformation($"Executing {nameof(GetByNameAsync)}");
			return await _categories.SingleOrDefaultAsync(c => c.Name.Equals(Name,StringComparison.OrdinalIgnoreCase));
		}

		public async Task<Category?> GetCategoryByIdAsync(int id)
		{
			_logger.LogInformation($"Executing {nameof(GetCategoryByIdAsync)} Id: {id}");
			return await _categories.SingleOrDefaultAsync(c => c.Id == id);
		}

		public async Task<Category?> GetCategoryWithProductsAsync(int productId)
		{
			_logger.LogInformation($"Executing {nameof(GetCategoryWithProductsAsync)} for ProductId: {productId}");

			return await _categories
				.Include(c => c.products) 
				.FirstOrDefaultAsync(c => c.products.Any(p => p.Id == productId));
		}

		public async Task<IEnumerable<Category>> GetDeletedCategoriesAsync()
		{
			_logger.LogInformation($"Executing {nameof(GetAllCategoriesAsync)}");
			return await _categories.AsNoTracking().Where(c => c.DeletedAt != null).ToListAsync();
		}
	}
}
