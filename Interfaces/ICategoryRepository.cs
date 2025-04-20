using E_Commers.Services;
using E_Commers.Models;

namespace E_Commers.Interfaces
{
	public interface ICategoryRepository:IRepository<Category>
	{
		public Task<Result<List<Product>>> GetProductsByCategoryIdAsync(int categoryId);
		public Task<Result<Category?>> GetByNameAsync(string Name);
		public Task<Result<Category?>> GetCategoryByProductIdAsync(int productid);
		public Task<Result<bool>> CategoryExistsAsync(int id);
	}
}
