using E_Commers.Models;

namespace E_Commers.Interfaces
{
	public interface ICategoryRepository:IRepository<Category>
	{
		public Task<IEnumerable<Category>> GetAllCategoriesAsync();
		public Task<IEnumerable<Category>> GetDeletedCategoriesAsync();
		public Task<Category?> GetCategoryByIdAsync(int id);
		public Task<Category?> GetByNameAsync(string Name);
		public Task<Category?> GetCategoryWithProductsAsync(int productid);
		public Task<bool> CategoryExistsAsync(int id);
	}
}
