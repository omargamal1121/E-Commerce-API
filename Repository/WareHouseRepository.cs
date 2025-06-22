using E_Commers.Context;
using E_Commers.Services;
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
	

		public WareHouseRepository(AppDbContext context, ILogger<MainRepository<Warehouse>> logger) : base( context, logger)
		{
			_context = context;
			_logger = logger;
			_warehouses = context.warehouses;
		}

		public async Task<Result<Warehouse?>> GetByNameAsync(string Name)
		{
			_logger.LogInformation($"Executing {nameof(GetByNameAsync)} for Name: {Name}");

		

			Warehouse? warehouse = await _warehouses.SingleOrDefaultAsync(c => c.Name.Equals(Name));

			if (warehouse is null)
			{
				_logger.LogWarning($"No Category with this Name:{Name}");
				return Result<Warehouse?>.Fail($"No Category with this Name:{Name}");
			}

			_logger.LogWarning("category found in database");
			return Result<Warehouse?>.Ok(warehouse, "From database");
		}


	}
}
