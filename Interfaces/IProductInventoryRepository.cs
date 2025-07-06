using E_Commers.Services;
using E_Commers.Models;

namespace E_Commers.Interfaces
{
    public interface IProductInventoryRepository : IRepository<ProductInventory>
    {
		// Add any specific methods for ProductInventory here

		public Task<ProductInventory?> GetByInvetoryIdWithProductAsync(int id);

	}
} 