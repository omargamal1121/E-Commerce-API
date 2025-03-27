using E_Commers.Helper;
using E_Commers.Models;

namespace E_Commers.Interfaces
{
	public interface IProductRepository:IRepository<Product>
	{
		public  Task<IEnumerable<Product>> GetProductsByCategoryAsync(int categoryId);
		public  Task<List<Product>> GetProductsByInventoryAsync(int InventoryId);
		public Task<ResultDto<bool>> UpdatePriceAsync(int productId, decimal Price);
		public Task<ResultDto<bool>> UpdateQuantityAsync(int productId, int quantity);

	}
}
