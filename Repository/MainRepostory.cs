using E_Commers.Context;
using E_Commers.Helper;
using E_Commers.Interfaces;
using Microsoft.EntityFrameworkCore;

public class MainRepository<T> : IRepository<T> where T : class
{
	private readonly AppDbContext _context;
	private readonly DbSet<T> _entities;
	private readonly ILogger<MainRepository<T>> _logger;

	public MainRepository(AppDbContext context, ILogger<MainRepository<T>> logger)
	{
		_context = context ?? throw new ArgumentNullException(nameof(context));
		_logger = logger ?? throw new ArgumentNullException(nameof(logger));
		_entities = _context.Set<T>();
	}

	public async Task<Result<bool>> CreateAsync(T model)
	{
		_logger.LogInformation($"Executing {nameof(CreateAsync)} for entity {typeof(T).Name}");

		if (model == null)
		{
			_logger.LogWarning("CreateAsync called with null model");
			return Result<bool>.Fail("Model sent is null");
		}

		await _entities.AddAsync(model);
		_logger.LogInformation("Model added successfully (pending save)");
		return Result<bool>.Ok(true, "Model added successfully.");
	}

	public async Task<IEnumerable<T>> GetAllAsync()
	{
		_logger.LogInformation($"Fetching all entities of type {typeof(T).Name}");
		return await _entities.AsNoTracking().ToListAsync();
	}

	public async Task<T?> GetByIdAsync(int id)
	{
		_logger.LogInformation($"Fetching entity {typeof(T).Name} with ID: {id}");
		return await _entities.FindAsync(id);
	}

	public async Task<Result<bool>> RemoveAsync(T model)
	{
		_logger.LogInformation($"Executing {nameof(RemoveAsync)} for entity {typeof(T).Name}");

		if (model == null)
		{
			_logger.LogWarning("RemoveAsync called with null model");
			return Result<bool>.Fail("Model sent is null");
		}

		_entities.Attach(model);
		_entities.Remove(model);

		_logger.LogInformation("Model marked for deletion (pending save)");
		return Result<bool>.Ok(true, "Model removed successfully.");
	}

	public async Task<Result<bool>> UpdateAsync(T model)
	{
		_logger.LogInformation($"Executing {nameof(UpdateAsync)} for entity {typeof(T).Name}");

		if (model == null)
		{
			_logger.LogWarning("UpdateAsync called with null model");
			return Result<bool>.Fail("Model sent is null");
		}

		_entities.Attach(model);

		if (!_context.ChangeTracker.HasChanges())
		{
			_logger.LogWarning("No changes detected for the model");
			return Result<bool>.Fail("No modifications were made.");
		}

		_logger.LogInformation("Model marked for update (pending save)");
		return Result<bool>.Ok(true, "Model updated successfully.");
	}
}
