using E_Commers.Services;
using E_Commers.Models;
using Newtonsoft.Json;

namespace E_Commers.Interfaces
{
	public interface IWareHouseRepository:IRepository<Warehouse>
	{
		public  Task<Result<Warehouse?>> GetByNameAsync(string Name);

	}
}
