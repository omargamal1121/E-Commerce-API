using E_Commers.Helper;
using E_Commers.Models;

namespace E_Commers.Interfaces
{
	public interface ICategoryRepository:IRepository<Category>
	{
		public Task<ResultDto<List<Product>>> GetProductsByCategoryIdAsync(int categoryId);
		public Task<ResultDto<Category?>> GetByNameAsync(string Name);
		public Task<ResultDto<Category?>> GetCategoryByProductIdAsync(int productid);
		public Task<ResultDto<bool>> CategoryExistsAsync(int id);
	}
}
