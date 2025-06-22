using Dapper;
using E_Commers.Context;

using E_Commers.Interfaces;
using E_Commers.Models;
using E_Commers.Services;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using StackExchange.Redis;
using System.Linq.Expressions;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

public class MainRepository<T> : IRepository<T> where T : BaseEntity
{
	private readonly AppDbContext _context;
	private readonly DbSet<T> _entities;
	private readonly ILogger<MainRepository<T>> _logger;

	public MainRepository( AppDbContext context, ILogger<MainRepository<T>> logger)
	{
		
		_context = context ?? throw new ArgumentNullException(nameof(context));
		_logger = logger ?? throw new ArgumentNullException(nameof(logger));
		_entities = _context.Set<T>();
	}

	public async Task<Result<T>> CreateAsync(T model)
	{
		_logger.LogInformation($"Executing {nameof(CreateAsync)} for entity {typeof(T).Name}");

		if (model == null)
		{
			_logger.LogWarning("CreateAsync called with null model");
			return Result<T>.Fail("Model sent is null");
		}

		 await _entities.AddAsync(model);
		_logger.LogInformation($"{typeof(T).Name} added successfully (pending save)");
		return Result<T>.Ok(model, $"{typeof(T).Name} added successfully.");
	}

	public  Task<Result<IQueryable<T>>> GetAllAsync(Func<IQueryable<T>, IQueryable<T>>?include = null,Expression<Func<T,bool>>? filter=null)
	{
		_logger.LogInformation($"Execute {nameof(GetAllAsync)} for entity {typeof(T).Name}");
		IQueryable<T> list = _entities.AsNoTracking();
		_logger.LogInformation("Data retrieved from Database");
		return Task.FromResult(Result<IQueryable<T>>.Ok(list, "Data retrieved successfully."));
	}

	public  async Task<Result<T>> GetByIdAsync(int id)
	{
		_logger.LogInformation($"Execute {nameof(GetByIdAsync)} for entity {typeof(T).Name} with ID: {id}");


		Result<T> result = new Result<T>();
		
		T? obj = await  _entities.FirstOrDefaultAsync(x=>x.Id==id);
		if (obj != null)
		{

			return Result<T>.Ok(obj);

		}
		_logger.LogWarning($"No {typeof(T).Name} with this Id:{id}");
		return Result<T>.Fail($"No {typeof(T).Name} with this Id:{id}") ;
	}

	public  Task<Result<bool>> RemoveAsync(T model)
	{
		_logger.LogInformation($"Execute {nameof(RemoveAsync)} for entity {typeof(T).Name}");

		if (model == null)
		{
			_logger.LogWarning("RemoveAsync called with null model");
			return Task.FromResult(Result<bool>.Fail($"{typeof(T).Name} sent is null"));
		}

		
		_entities.Remove(model);

		_logger.LogInformation($"{typeof(T).Name} marked for deletion (pending save)");
		return  Task.FromResult( Result<bool>.Ok(true, $"{typeof(T).Name} removed successfully."));
	}

	public  Task<Result<T>> UpdateAsync(T model)
	{
		_logger.LogInformation($"Execute {nameof(UpdateAsync)} for entity {typeof(T).Name}");
		
		if (model == null)
		{
			_logger.LogWarning($"UpdateAsync called with null {typeof(T).Name}");
			return Task.FromResult( Result<T>.Fail($"{typeof(T).Name} sent is null"));
		}

		if (!_context.ChangeTracker.HasChanges())
		{
			_logger.LogWarning($"No changes detected for the {typeof(T).Name}");
			return Task.FromResult(Result<T>.Fail("No modifications were made."));
		}

		_logger.LogInformation($"{typeof(T).Name} marked for update (pending save)");
		return Task.FromResult(Result<T>.Ok(model, $"{typeof(T).Name} updated successfully."));

	}

	public async Task<Result<T>> GetByQuery(Expression<Func<T, bool>> predicate)
	{
		try
		{
		

			var entity = await _entities
				.FirstOrDefaultAsync(predicate);

			return entity != null
				? Result<T>.Ok(entity)
				: Result<T>.Fail($"{typeof(T).Name} not found");
		}
		catch (Exception ex)
		{
			
			return Result<T>.Fail($"Error retrieving {typeof(T).Name}: {ex.Message}");
		}
	}

	public async Task<Result<bool>> IsExsistAsync(int id)
	{
		return  await _entities.AnyAsync(e => e.Id == id&&e.DeletedAt == null) ==true? Result<bool>.Ok(true) : Result<bool>.Fail("Doesn't Exsist") ;
	}	public async Task<Result<bool>> IsDeletedAsync(int id)
	{
		return  await _entities.AnyAsync(e => e.Id == id&&e.DeletedAt != null) ==true? Result<bool>.Ok(true) : Result<bool>.Fail("Doesn't Exsist") ;
	}
}
