using E_Commers.Services;
using E_Commers.Models;
using StackExchange.Redis;
using System.Linq.Expressions;

namespace E_Commers.Interfaces
{
	public interface IRepository<T> where T :BaseEntity
	{
	
		Task<Result<T>> CreateAsync(T model);
		public Task<Result<T>> GetByQuery(Expression<Func<T, bool>> predicate);
		Task<Result<T>> UpdateAsync(T model);
		Task<Result<bool>> RemoveAsync(T model);
		Task<Result<T>> GetByIdAsync(int id); 
		Task<Result< IQueryable<T>>> GetAllAsync(Func<IQueryable<T>, IQueryable<T>>? include = null, Expression<Func<T, bool>>? filter = null);
	}
}
