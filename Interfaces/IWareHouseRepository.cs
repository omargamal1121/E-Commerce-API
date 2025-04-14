using E_Commers.Helper;
using E_Commers.Models;
using Newtonsoft.Json;

namespace E_Commers.Interfaces
{
	public interface IWareHouseRepository:IRepository<Warehouse>
	{
		public  Task<ResultDto<Warehouse?>> GetByNameAsync(string Name);

	}
}
