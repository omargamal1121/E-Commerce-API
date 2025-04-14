using E_Commers.Helper;
using E_Commers.Models;
using StackExchange.Redis;
using System.Linq.Expressions;

namespace E_Commers.Interfaces
{
	public interface IRepository<T> where T :BaseEntity
	{
		public IDatabase redisdb { get;  }
		Task<ResultDto<bool>> CreateAsync(T model);
		public Task<ResultDto<T>> GetByQuery(Expression<Func<T, bool>> predicate);
		Task<ResultDto<bool>> UpdateAsync(T model);
		Task<ResultDto<bool>> RemoveAsync(T model);
		Task<ResultDto<T>> GetByIdAsync(int id, Func<IQueryable<T>, IQueryable<T>>? include = null); 
		Task<ResultDto< IEnumerable<T>>> GetAllAsync(Func<IQueryable<T>, IQueryable<T>>? include = null, Expression<Func<T, bool>>? filter = null);
	}
}
