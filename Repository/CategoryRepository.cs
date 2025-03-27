using E_Commers.Context;
using E_Commers.Helper;
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
		private readonly ILogger<CategoryRepository> _logger;

		public CategoryRepository(IConnectionMultiplexer redis, AppDbContext context, ILogger<CategoryRepository> logger) : base(redis,context, logger)
		{
			_categories = context.Categories; 
			_logger = logger;
		}

		public async Task<ResultDto<bool>> CategoryExistsAsync(int id)
		{
			_logger.LogInformation($"Executing {nameof(CategoryExistsAsync)} Id: {id}");
			if(await GetByIdAsync(id) is null)
			{
				_logger.LogWarning($"No Category With this id:{id}");
				return ResultDto<bool>.Fail($"No Category With this id:{id}");
			}
			_logger.LogInformation("Cateogry found");
			return ResultDto<bool>.Ok(true); 
		}


		public async Task<ResultDto<Category?>> GetByNameAsync(string Name)
		{
			_logger.LogInformation($"Executing {nameof(GetByNameAsync)} for Name: {Name}");

			string cacheKey = $"Category:Name:{Name}"; 
			string? cachedData = await redisdb.StringGetAsync(cacheKey);

			if (!string.IsNullOrEmpty(cachedData))
			{
				_logger.LogInformation("Category Found in cache");
				 return ResultDto<Category?>.Ok(JsonConvert.DeserializeObject<Category>(cachedData));
				
			}

			Category? category = await _categories.SingleOrDefaultAsync(c => c.Name.Equals(Name));

			if (category is null)
			{
				_logger.LogWarning($"No Category with this Name:{Name}");
				return ResultDto<Category?>.Fail($"No Category with this Name:{Name}");
			}
			await redisdb.StringSetAsync(cacheKey, JsonConvert.SerializeObject(category), TimeSpan.FromMinutes(5));
			await redisdb.SetAddAsync(typeof(Category).Name, cacheKey);

			_logger.LogWarning("category found in database");
			return ResultDto<Category?>.Ok(category,"From database");
		}



		public async Task<ResultDto<Category?>> GetCategoryByProductIdAsync(int productId)
		{
			_logger.LogInformation($"Executing {nameof(GetCategoryByProductIdAsync)} for ProductId: {productId}");
			string? serlizecategory = await redisdb.StringGetAsync($"Categories:ProductId:{productId}");
			if (string.IsNullOrEmpty(serlizecategory))
			{
				Category? category = await _categories.Include(c => c.products).FirstOrDefaultAsync(c => c.products.Any(p => p.Id == productId));
				if (category is null)
				{
					_logger.LogInformation($"No category Has this ProductId:{productId}");
					return ResultDto<Category?>.Fail($"No category Has this ProductId:{productId}");
				}
				await redisdb.StringSetAsync($"Category:ProductId:{productId}", JsonConvert.SerializeObject(category));
				await redisdb.SetAddAsync($"{typeof(Category).Name}", $"Categories:ProductId:{productId}");
				return ResultDto<Category?>.Ok(category, "Category found in database");
			}
			_logger.LogInformation("Category found in cache");
			return ResultDto<Category?>.Ok(JsonConvert.DeserializeObject< Category >(serlizecategory));
		}
		public async Task<ResultDto<List<Product>>> GetProductsByCategoryIdAsync(int categoryId)
		{
			_logger.LogInformation($"Executing {nameof(GetProductsByCategoryIdAsync)} for CategoryId: {categoryId}");

			string? serializedProducts = await redisdb.StringGetAsync($"Products:CategoryId:{categoryId}");

			if (!string.IsNullOrEmpty(serializedProducts))
			{
				_logger.LogInformation("Found In cache");
				return ResultDto<List<Product>>.Ok( JsonConvert.DeserializeObject<List<Product>>(serializedProducts) ?? new List<Product>(),"Products found");
			}


			ResultDto<Category> category = await GetByIdAsync(categoryId);
			if (!category.Success)
			{
				_logger.LogWarning(category.Message);
				return ResultDto<List<Product>>.Fail(category.Message);
			}

			List<Product> products = category.Data?.products ?? new List<Product>();
			if (!products.Any()){
				_logger.LogWarning("No Products found");
				return ResultDto<List<Product>>.Ok(products,"No Products found");
			}

			await redisdb.StringSetAsync($"Products:CategoryId:{categoryId}", JsonConvert.SerializeObject(products));

			await redisdb.SetAddAsync($"{typeof(Category).Name}", $"Products:CategoryId:{categoryId}");

			return ResultDto<List<Product>>.Ok(products);
		}


	}
}
