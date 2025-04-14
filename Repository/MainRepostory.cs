using E_Commers.Context;
using E_Commers.Helper;
using E_Commers.Interfaces;
using E_Commers.Models;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using StackExchange.Redis;
using System.Linq.Expressions;

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
		_logger.LogInformation($"{typeof(T).Name} added successfully (pending save)");
		return ResultDto<bool>.Ok(true, $"{typeof(T).Name} added successfully.");
	}

	public async Task<ResultDto<IEnumerable<T>>> GetAllAsync(Func<IQueryable<T>, IQueryable<T>>?include = null,Expression<Func<T,bool>>? filter=null)
	{
		_logger.LogInformation($"Execute {nameof(GetAllAsync)} for entity {typeof(T).Name}");

		string cacheKey = $"GetAllAsync:{typeof(T).Name}:include:{include}:Filter:{filter}";
		string? cachedData = redisdb.StringGet(cacheKey);
		if (!string.IsNullOrEmpty(cachedData))
		{
			_logger.LogInformation("Data retrieved from cache");
			var cachedList = JsonConvert.DeserializeObject<List<T>>(cachedData);
			return ResultDto<IEnumerable<T>>.Ok(cachedList, "Data retrieved from cache");
			
		}
		IQueryable<T> list = _entities.AsNoTracking();
		if (filter != null)
		{
			list=list.Where(filter);
		}
		if (include != null)
		{

			list= include(list);
		}
			

		if (list is null || !list.Any())
		{
			return ResultDto<IEnumerable<T>>.Fail("No Records Found");
		}

		redisdb.StringSet(cacheKey, JsonConvert.SerializeObject(list,new JsonSerializerSettings { ReferenceLoopHandling=ReferenceLoopHandling.Ignore} ), TimeSpan.FromMinutes(10));
		await redisdb.SetAddAsync(typeof(T).Name, cacheKey);
		_logger.LogInformation("Data retrieved from Database");
		return ResultDto<IEnumerable<T>>.Ok(await list.ToListAsync(), "Data retrieved successfully.");
	}

	public async Task<ResultDto<T>> GetByIdAsync(int id, Func<IQueryable<T>, IQueryable<T>>? include = null)
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
	
		IQueryable<T> list =  _entities;
		
		if (include != null)
		{

			list=include(list);
		}
		if (!list.Any())
		{
			_logger.LogWarning($"No {typeof(T).Name}s with this Id:{id}");
			return ResultDto<T>.Fail($"No {typeof(T).Name} with this Id:{id}");
		}

		T? obj = list.FirstOrDefault(o => o.Id == id);
		if (obj != null)
		{
			redisdb.StringSet(cachedKey, JsonConvert.SerializeObject(obj,new JsonSerializerSettings { ReferenceLoopHandling=ReferenceLoopHandling.Ignore}), TimeSpan.FromSeconds(30));
			await redisdb.SetAddAsync(typeof(T).Name, cachedKey);
			return ResultDto<T>.Ok(obj);

		}
		_logger.LogWarning($"No {typeof(T).Name} with this Id:{id}");
		return ResultDto<T>.Fail($"No {typeof(T).Name} with this Id:{id}") ;
	}

	public async Task<ResultDto<bool>> RemoveAsync(T model)
	{
		_logger.LogInformation($"Execute {nameof(RemoveAsync)} for entity {typeof(T).Name}");

		if (model == null)
		{
			_logger.LogWarning("RemoveAsync called with null model");
			return ResultDto<bool>.Fail($"{typeof(T).Name} sent is null");
		}
		await CacheRemoverAsync(typeof(T).Name);

		_entities.Attach(model);
		_entities.Remove(model);

		_logger.LogInformation($"{typeof(T).Name} marked for deletion (pending save)");
		return ResultDto<bool>.Ok(true, $"{typeof(T).Name} removed successfully.");
	}

	public async Task<ResultDto<bool>> UpdateAsync(T model)
	{
		_logger.LogInformation($"Execute {nameof(UpdateAsync)} for entity {typeof(T).Name}");

		if (model == null)
		{
			_logger.LogWarning($"UpdateAsync called with null {typeof(T).Name}");
			return ResultDto<bool>.Fail($"{typeof(T).Name} sent is null");
		}

		await CacheRemoverAsync(typeof(T).Name);

		if (!_context.ChangeTracker.HasChanges())
		{
			_logger.LogWarning($"No changes detected for the {typeof(T).Name}");
			return ResultDto<bool>.Fail("No modifications were made.");
		}

		_logger.LogInformation($"{typeof(T).Name} marked for update (pending save)");
		return ResultDto<bool>.Ok(true, $"{typeof(T).Name} updated successfully.");
	}


	public async Task<ResultDto<T>> GetByQuery(Expression<Func<T, bool>> predicate)
	{
		try
		{
		

			var entity = await _entities
				.FirstOrDefaultAsync(predicate);

			return entity != null
				? ResultDto<T>.Ok(entity)
				: ResultDto<T>.Fail($"{typeof(T).Name} not found");
		}
		catch (Exception ex)
		{
			// Log the exception (consider injecting ILogger)
			return ResultDto<T>.Fail($"Error retrieving {typeof(T).Name}: {ex.Message}");
		}
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
