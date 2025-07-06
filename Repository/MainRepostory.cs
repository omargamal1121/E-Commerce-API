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

	public async Task<T?> CreateAsync(T model)
	{
		_logger.LogInformation($"Executing {nameof(CreateAsync)} for entity {typeof(T).Name}");

		if (model == null)
		{
			_logger.LogWarning("CreateAsync called with null model");
			return null;
		}

		 await _entities.AddAsync(model);
		_logger.LogInformation($"{typeof(T).Name} added successfully (pending save)");
		return model;
	}



	public IQueryable<T> GetAll()
	{
		_logger.LogInformation($"Execute {nameof(GetAll)} for entity {typeof(T).Name}");
		return _entities.AsNoTracking();
	}

	public bool Remove(T model)
	{
		_logger.LogInformation($"Execute {nameof(Remove)} for entity {typeof(T).Name}");

		if (model == null)
		{
			_logger.LogWarning("RemoveAsync called with null model");
			return false;
		}

		
		_entities.Remove(model);

		_logger.LogInformation($"{typeof(T).Name} marked for deletion (pending save)");
		return true;
	}

	public async Task<T?> GetByIdAsync(int id)
	{
		_logger.LogInformation($"Executing {nameof(GetByIdAsync)} for entity {typeof(T).Name} with ID: {id}");
		var entity = await _entities.AsNoTracking()
			.FirstOrDefaultAsync(e => e.Id == id);

		if (entity == null)
			_logger.LogWarning($"{typeof(T).Name} with ID {id} not found or deleted");

		return entity;
	}

	public bool Update(T model)
	{
		_logger.LogInformation($"Execute {nameof(Update)} for entity {typeof(T).Name}");
		
		if (model == null)
		{
			_logger.LogWarning($"UpdateAsync called with null {typeof(T).Name}");
			return false;
		}

		if (!_context.ChangeTracker.HasChanges())
		{
			_logger.LogWarning($"No changes detected for the {typeof(T).Name}");
			return false;
		}

		_logger.LogInformation($"{typeof(T).Name} marked for update (pending save)");
		return true;
	}

	public async Task<T?> GetByQuery(Expression<Func<T, bool>> predicate)
	{
		try
		{
			var entity = await _entities.FirstOrDefaultAsync(predicate);
			return entity;
		}
		catch (Exception ex)
		{
			_logger.LogError($"Error retrieving {typeof(T).Name}: {ex.Message}");
			return null;
		}
	}

	public async Task<bool> SoftDeleteAsync(int id)
	{
		_logger.LogInformation($"Execute {nameof(SoftDeleteAsync)} for entity {typeof(T).Name} with ID: {id}");

		var entity = await _entities.FirstOrDefaultAsync(e => e.Id == id && e.DeletedAt == null);
		if (entity == null)
		{
			_logger.LogWarning($"{typeof(T).Name} with ID: {id} not found or already deleted.");
			return false;
		}

		entity.DeletedAt = DateTime.UtcNow;
		_entities.Update(entity);

		_logger.LogInformation($"{typeof(T).Name} with ID: {id} soft deleted.");
		return true;
	}

	public async Task<bool> IsExsistAsync(int id)
	{
		return await _entities.AnyAsync(e => e.Id == id && e.DeletedAt == null);
	}

	public async Task<bool> IsDeletedAsync(int id)
	{
		return await _entities.AnyAsync(e => e.Id == id && e.DeletedAt != null);
	}

	public async Task<List<T>> GetAllDeletedAsync()
	{
		_logger.LogInformation($"Execute {nameof(GetAllDeletedAsync)} for entity {typeof(T).Name}");
		return await _entities.AsNoTracking().Where(e => e.DeletedAt != null).ToListAsync();
	}

	public async Task<bool> RestoreAsync(int id)
	{
		_logger.LogInformation($"Execute {nameof(RestoreAsync)} for entity {typeof(T).Name} with ID: {id}");
		var entity = await _entities.FirstOrDefaultAsync(e => e.Id == id && e.DeletedAt != null);
		if (entity == null)
		{
			_logger.LogWarning($"{typeof(T).Name} with ID: {id} not found or not deleted.");
			return false;
		}
		entity.DeletedAt = null;
		_entities.Update(entity);
		_logger.LogInformation($"{typeof(T).Name} with ID: {id} restored.");
		return true;
	}
}
