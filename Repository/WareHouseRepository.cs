using E_Commers.Context;
using E_Commers.Helper;
using E_Commers.Interfaces;
using E_Commers.Models;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using StackExchange.Redis;

namespace E_Commers.Repository
{
	public class WareHouseRepository:MainRepository<Warehouse> ,IWareHouseRepository
	{
		private readonly AppDbContext _context;
		private readonly DbSet<Warehouse> _warehouses;
		private readonly ILogger<MainRepository<Warehouse>> _logger;
	

		public WareHouseRepository(IConnectionMultiplexer redis, AppDbContext context, ILogger<MainRepository<Warehouse>> logger) : base(redis, context, logger)
		{
			_context = context;
			_logger = logger;
			_warehouses = context.warehouses;
		}

		public async Task<ResultDto<Warehouse?>> GetByNameAsync(string Name)
		{
			_logger.LogInformation($"Executing {nameof(GetByNameAsync)} for Name: {Name}");

			string cacheKey = $"Category:Name:{Name}";
			string? cachedData = await redisdb.StringGetAsync(cacheKey);

			if (!string.IsNullOrEmpty(cachedData))
			{
				_logger.LogInformation("Category Found in cache");
				return ResultDto<Warehouse?>.Ok(JsonConvert.DeserializeObject<Warehouse>(cachedData));

			}

			Warehouse? warehouse = await _warehouses.SingleOrDefaultAsync(c => c.Name.Equals(Name));

			if (warehouse is null)
			{
				_logger.LogWarning($"No Category with this Name:{Name}");
				return ResultDto<Warehouse?>.Fail($"No Category with this Name:{Name}");
			}
			await redisdb.StringSetAsync(cacheKey, JsonConvert.SerializeObject(warehouse), TimeSpan.FromMinutes(5));
			await redisdb.SetAddAsync(typeof(Category).Name, cacheKey);

			_logger.LogWarning("category found in database");
			return ResultDto<Warehouse?>.Ok(warehouse, "From database");
		}


	}
}
