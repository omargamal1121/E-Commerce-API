using E_Commers.Interfaces;
using E_Commers.Models;
using E_Commers.Repository;
using Microsoft.EntityFrameworkCore.Storage;

namespace E_Commers.UOW
{
	public interface IUnitOfWork:IDisposable 
	{
		ICategoryRepository Category { get;  }
		public Task<IDbContextTransaction> BeginTransactionAsync();
		IRepository<T> Repository<T>() where T : BaseEntity;
		public Task<int> CommitAsync();
	}
}
