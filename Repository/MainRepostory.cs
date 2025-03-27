using E_Commers.Context;
using E_Commers.Helper;
using E_Commers.Interfaces;
using E_Commers.Models;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using StackExchange.Redis;

public class MainRepository<T> : IRepository<T> where T : BaseEntity
{
	private readonly AppDbContext _context;
	private readonly DbSet<T> _entities;
	private readonly ILogger<MainRepository<T>> _logger;
	public IDatabase redisdb { get; }

	public MainRepository(IConnectionMultiplexer redis, AppDbContext context, ILogger<MainRepository<T>> logger)
	{
		redisdb = redis.GetDatabase() ?? throw new ArgumentNullException(nameof(redis));
		_context = context ?? throw new ArgumentNullException(nameof(context));
		_logger = logger ?? throw new ArgumentNullException(nameof(logger));
		_entities = _context.Set<T>();
	}

	public async Task<ResultDto<bool>> CreateAsync(T model)
	{
		_logger.LogInformation($"Executing {nameof(CreateAsync)} for entity {typeof(T).Name}");

		if (model == null)
		{
			_logger.LogWarning("CreateAsync called with null model");
			return ResultDto<bool>.Fail("Model sent is null");
		}

		await _entities.AddAsync(model);
		await CacheRemoverAsync(typeof(T).Name);
		_logger.LogInformation("Model added successfully (pending save)");
		return ResultDto<bool>.Ok(true, "Model added successfully.");
	}

	public async Task<ResultDto<IEnumerable<T>>> GetAllAsync()
	{
		_logger.LogInformation($"Execute {nameof(GetAllAsync)} for entity {typeof(T).Name}");

		string cacheKey = $"GetAllAsync:{typeof(T).Name}";
		string? cachedData = redisdb.StringGet(cacheKey);
		if (!string.IsNullOrEmpty(cachedData))
		{
			_logger.LogInformation("Data retrieved from cache");
			var cachedList = JsonConvert.DeserializeObject<List<T>>(cachedData);
			return ResultDto<IEnumerable<T>>.Ok(cachedList, "Data retrieved from cache");
			
		}

		var list = await _entities.AsNoTracking().Where(x => x.DeletedAt == null).ToListAsync();

		if (list is null || !list.Any())
		{
			return ResultDto<IEnumerable<T>>.Fail("No Records Found");
		}

		redisdb.StringSet(cacheKey, JsonConvert.SerializeObject(list), TimeSpan.FromMinutes(1));
		await redisdb.SetAddAsync(typeof(T).Name, cacheKey);
		_logger.LogInformation("Data retrieved from Database");
		return ResultDto<IEnumerable<T>>.Ok(list, "Data retrieved successfully.");
	}

	public async Task<ResultDto<T>> GetByIdAsync(int id)
	{
		_logger.LogInformation($"Execute {nameof(GetByIdAsync)} for entity {typeof(T).Name} with ID: {id}");

		string cachedKey = $"GetByIdAsync:{typeof(T).Name}:{id}";
		string? cachedData = redisdb.StringGet(cachedKey);

		ResultDto<T> result = new ResultDto<T>();
		if (!string.IsNullOrEmpty(cachedData))
		{
			_logger.LogInformation("From cache");
			return ResultDto<T>.Ok(JsonConvert.DeserializeObject<T>(cachedData), "Data retrieved from cache");  
		}

		T? obj = await _entities.FindAsync(id);
		if (obj != null)
		{
			redisdb.StringSet(cachedKey, JsonConvert.SerializeObject(obj), TimeSpan.FromMinutes(1));
			await redisdb.SetAddAsync(typeof(T).Name, cachedKey);
			return ResultDto<T>.Ok(obj);

		}
		_logger.LogWarning($"No Recorde with this Id:{id}");
		return ResultDto<T>.Fail("No Recorde with this Id:{id}") ;
	}

	public async Task<ResultDto<bool>> RemoveAsync(T model)
	{
		_logger.LogInformation($"Execute {nameof(RemoveAsync)} for entity {typeof(T).Name}");

		if (model == null)
		{
			_logger.LogWarning("RemoveAsync called with null model");
			return ResultDto<bool>.Fail("Model sent is null");
		}
		await CacheRemoverAsync(typeof(T).Name);

		_entities.Attach(model);
		_entities.Remove(model);

		_logger.LogInformation("Model marked for deletion (pending save)");
		return ResultDto<bool>.Ok(true, "Model removed successfully.");
	}

	public async Task<ResultDto<bool>> UpdateAsync(T model)
	{
		_logger.LogInformation($"Execute {nameof(UpdateAsync)} for entity {typeof(T).Name}");

		if (model == null)
		{
			_logger.LogWarning("UpdateAsync called with null model");
			return ResultDto<bool>.Fail("Model sent is null");
		}

		await CacheRemoverAsync(typeof(T).Name);
		_entities.Attach(model);

		if (!_context.ChangeTracker.HasChanges())
		{
			_logger.LogWarning("No changes detected for the model");
			return ResultDto<bool>.Fail("No modifications were made.");
		}

		_logger.LogInformation("Model marked for update (pending save)");
		return ResultDto<bool>.Ok(true, "Model updated successfully.");
	}

	public async Task<ResultDto<IEnumerable<T>>> GetAllDeletedAsync()
	{
		_logger.LogInformation($"Execute {nameof(GetAllDeletedAsync)} for entity {typeof(T).Name}");

		string cacheKey = $"GetAllDeleted:{typeof(T).Name}";
		string? cachedData = redisdb.StringGet(cacheKey);
		if (!string.IsNullOrEmpty(cachedData))
		{

			_logger.LogInformation("Data retrieved from cache");
			return ResultDto<IEnumerable<T>>.Ok(JsonConvert.DeserializeObject<List<T>>(cachedData), "Data retrieved from cache");
		}

		var list = await _entities.AsNoTracking().Where(x => x.DeletedAt.HasValue).ToListAsync();
		if(list.Count==0)
		{
			_logger.LogWarning("No Recods Found");
			return ResultDto<IEnumerable<T>>.Fail("No Recods Found");
		}

		redisdb.StringSet(cacheKey, JsonConvert.SerializeObject(list), TimeSpan.FromMinutes(1));
		 await redisdb.SetAddAsync(typeof(T).Name, cacheKey);

		return ResultDto<IEnumerable<T>>.Ok(list,"Recods Found");
	}

	public async Task CacheRemoverAsync(string tagename)
	{
		var keys = await redisdb.SetMembersAsync(tagename);

		if (keys.Length > 0)
		{
			var keysArray = keys.Select(k => (RedisKey)k.ToString()).ToArray();
			await redisdb.KeyDeleteAsync(keysArray);
		}

		await redisdb.KeyDeleteAsync(tagename);
	}
}
