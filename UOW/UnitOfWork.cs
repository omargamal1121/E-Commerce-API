using E_Commers.Context;
using E_Commers.Interfaces;
using E_Commers.Repository;
using E_Commers.UOW;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;

public class UnitOfWork : IUnitOfWork
{
	private readonly AppDbContext _context;
	private readonly Dictionary<Type, object> _repositories = new();
	private readonly ILoggerFactory _loggerFactory;

	public ICategoryRepository Category { get; }

	public UnitOfWork(AppDbContext context, ICategoryRepository category, ILoggerFactory loggerFactory)
	{
		_context = context;
		Category = category;
		_loggerFactory = loggerFactory;
	}

	public async Task<int> CommitAsync()
	{
	
		return await _context.SaveChangesAsync();
	}

	public void Dispose()
	{
		_context.Dispose();
	}

	public IRepository<T> Repository<T>() where T : class
	{
		if (!_repositories.ContainsKey(typeof(T)))
		{
		
			var logger = _loggerFactory.CreateLogger<MainRepository<T>>();

		
			var repository = new MainRepository<T>(_context, logger);
			_repositories.Add(typeof(T), repository);
		}

		return (IRepository<T>)_repositories[typeof(T)];
	}
	public async Task<IDbContextTransaction> BeginTransactionAsync()
	{
		return await _context.Database.BeginTransactionAsync();
	}
}
