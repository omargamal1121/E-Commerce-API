using E_Commers.Context;
using E_Commers.Interfaces;
using E_Commers.Models;
using E_Commers.Repository;
using E_Commers.UOW;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

public class UnitOfWork : IUnitOfWork
{
	private readonly AppDbContext _context;
	private readonly Dictionary<Type, object> _repositories = new();
	private readonly ILoggerFactory _loggerFactory;
	private readonly IConnectionMultiplexer _redis;
	public ICategoryRepository Category { get; }
	public IWareHouseRepository  WareHouse { get; }

	public IProductRepository Product { get; }
	public UnitOfWork(IProductRepository product,IWareHouseRepository wareHouse,IConnectionMultiplexer redis, AppDbContext context, ICategoryRepository category, ILoggerFactory loggerFactory)
	{
		Product = product;
		WareHouse = wareHouse;
		_redis = redis;
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

	public IRepository<T> Repository<T>() where T : BaseEntity
	{
		if (!_repositories.ContainsKey(typeof(T)))
		{
		
			var logger = _loggerFactory.CreateLogger<MainRepository<T>>();

		
			var repository = new MainRepository<T>(_redis,_context, logger);
			_repositories.Add(typeof(T), repository);
		}

		return (IRepository<T>)_repositories[typeof(T)];
	}
	public async Task<IDbContextTransaction> BeginTransactionAsync()
	{
		return await _context.Database.BeginTransactionAsync();
	}
}
