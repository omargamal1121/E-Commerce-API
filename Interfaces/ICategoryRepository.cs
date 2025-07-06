using E_Commers.Services;
using E_Commers.Models;
using System.Linq;
using System.Threading.Tasks;

namespace E_Commers.Interfaces
{
	public interface ICategoryRepository:IRepository<Category>
	{
		//public  Task<bool> IsHasProductAsync(int id);
		//public Task<Result<List<Product>>> GetProductsByCategoryIdAsync(int categoryId);
		//public Task<Result<Category?>> GetByArNameAsync(string Name);
		public Task<bool> CategoryExistsAsync(int id);
		public Task<Category?> GetCategoryById(int id, bool isActiveFilter = false);
		Task<bool> HasSubCategoriesAsync(int categoryId);
		Task<Category?> FindByNameAsync(string name);
		IQueryable<Category> FindByNameContains(string partialName);

	}
}
