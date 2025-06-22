using E_Commers.Context;
using E_Commers.Services;
using E_Commers.Interfaces;
using E_Commers.Models;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using StackExchange.Redis;

namespace E_Commers.Repository
{
	public class CategoryRepository : MainRepository<Category>, ICategoryRepository
	{

		private readonly DbSet<Category> _categories;
		private readonly DbSet<Product> _products;
		private readonly ILogger<CategoryRepository> _logger;

		public CategoryRepository( AppDbContext context, ILogger<CategoryRepository> logger) : base(context, logger)
		{
			_categories = context.Categories; 
			_products = context.Products;
			_logger = logger;
		
		}

		public async Task<Result<bool>> CategoryExistsAsync(int id)
		{
			_logger.LogInformation($"Executing {nameof(CategoryExistsAsync)} Id: {id}");
			if(await GetByIdAsync(id) is null)
			{
				_logger.LogWarning($"No Category With this id:{id}");
				return Result<bool>.Fail($"No Category With this id:{id}");
			}
			_logger.LogInformation("Cateogry found");
			return Result<bool>.Ok(true); 
		}


		public async Task<Result<Category?>> GetByNameAsync(string Name)
		{
			_logger.LogInformation($"Executing {nameof(GetByNameAsync)} for Name: {Name}");

			Category? category = await _categories.SingleOrDefaultAsync(c => c.Name.Equals(Name));

			if (category is null)
			{
				_logger.LogWarning($"No Category with this Name:{Name}");
				return Result<Category?>.Fail($"No Category with this Name:{Name}");
			}
			

			_logger.LogWarning("category found in database");
			return Result<Category?>.Ok(category,"From database");
		}



		public async Task<Result<Category?>> GetCategoryByProductIdAsync(int productId)
		{
			_logger.LogInformation($"Executing {nameof(GetCategoryByProductIdAsync)} for ProductId: {productId}");
			
			
				Category? category = await _categories.Include(c => c.subCategories).FirstOrDefaultAsync(c => c.subCategories.Any(p => p.Id == productId));
				if (category is null)
				{
					_logger.LogInformation($"No category Has this ProductId:{productId}");
					return Result<Category?>.Fail($"No category Has this ProductId:{productId}");
				}

			return Result<Category?>.Ok(category, "Category found in database");
		}
		//public async Task<bool>IsHasProductAsync(int id)
		//{
		//	return await _products.AnyAsync(p => p.CategoryId == id);
		//}
		public async Task<Result<List<SubCategory>>> GetProductsByCategoryIdAsync(int categoryId)
		{
			_logger.LogInformation($"Executing {nameof(GetProductsByCategoryIdAsync)} for CategoryId: {categoryId}");

			

			

			Result<Category> category = await GetByIdAsync(categoryId);
			if (!category.Success)
			{
				_logger.LogWarning(category.Message);
				return Result<List<SubCategory>>.Fail(category.Message);
			}

			//List<SubCategory> products = category.Data?.subCategories ;
			//if (!products.Any()){
			//	_logger.LogWarning("No Products found");
			//	return Result<List<SubCategory>>.Ok(products,"No Products found");
			//}

		

			return Result<List<SubCategory>>.Ok(new List<SubCategory>());
		}


	}
}
