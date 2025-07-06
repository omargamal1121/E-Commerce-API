using E_Commers.Services;
using E_Commers.Models;
using StackExchange.Redis;
using System.Linq.Expressions;

namespace E_Commers.Interfaces
{
	public interface IRepository<T> where T : BaseEntity
	{
		Task<T?> CreateAsync(T model);
		Task<bool> SoftDeleteAsync(int id);
		Task<bool> IsDeletedAsync(int id);
		Task<T?> GetByQuery(Expression<Func<T, bool>> predicate);
		bool Update(T model);
		bool Remove(T model);
		Task<bool> IsExsistAsync(int id);
		Task<T?> GetByIdAsync(int id);
		IQueryable<T> GetAll();
		Task<List<T>> GetAllDeletedAsync();
		Task<bool> RestoreAsync(int id);
	}
}
