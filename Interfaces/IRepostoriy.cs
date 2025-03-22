using E_Commers.Helper;

namespace E_Commers.Interfaces
{
	public interface IRepository<T> where T :class
	{
		Task<Result<bool>> CreateAsync(T model);
		Task<Result<bool>> UpdateAsync(T model);
		Task<Result<bool>> RemoveAsync(T model);
		Task<T?> GetByIdAsync(int id); 
		Task<IEnumerable<T>> GetAllAsync();
	}
}
