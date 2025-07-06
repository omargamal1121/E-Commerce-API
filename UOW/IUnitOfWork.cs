using E_Commers.Interfaces;
using E_Commers.Models;
using Microsoft.EntityFrameworkCore.Storage;

namespace E_Commers.UOW
{
	public interface IUnitOfWork:IDisposable 
	{
		ICategoryRepository Category { get;  }
		IProductRepository Product { get;  }
		ISubCategoryRepository SubCategory { get; }
		ICartRepository Cart { get; }
		IOrderRepository Order { get; }
		ICollectionRepository Collection { get; }
		IWareHouseRepository WareHouse { get; }
		IProductInventoryRepository ProductInventory { get; }
		IImageRepository Image { get; }
		ICustomerAddressRepository CustomerAddress { get; }
		public Task<IDbContextTransaction> BeginTransactionAsync();
		IRepository<T> Repository<T>() where T : BaseEntity;
		public Task<int> CommitAsync();
	}
}
