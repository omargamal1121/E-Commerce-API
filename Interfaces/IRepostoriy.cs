using E_Commers.Helper;
using E_Commers.Models;
using StackExchange.Redis;

namespace E_Commers.Interfaces
{
	public interface IRepository<T> where T :BaseEntity
	{
		public IDatabase redisdb { get;  }
		Task<ResultDto<bool>> CreateAsync(T model);
		Task<ResultDto<bool>> UpdateAsync(T model);
		Task<ResultDto<bool>> RemoveAsync(T model);
		Task<ResultDto<T>> GetByIdAsync(int id); 
		Task<ResultDto< IEnumerable<T>>> GetAllAsync();
		Task<ResultDto< IEnumerable<T>>> GetAllDeletedAsync();
	}
}
