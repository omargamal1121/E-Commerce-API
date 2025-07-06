using AutoMapper;
using E_Commers.DtoModels.InventoryDtos;
using E_Commers.DtoModels.Responses;
using E_Commers.DtoModels.WareHouseDtos;
using E_Commers.Enums;
using E_Commers.ErrorHnadling;
using E_Commers.Interfaces;
using E_Commers.Models;
using E_Commers.Services.AdminOpreationServices;
using E_Commers.Services.Cache;
using E_Commers.UOW;
using Microsoft.EntityFrameworkCore;

namespace E_Commers.Services.WareHouseServices
{
	public class WareHouseServices : IWareHouseServices
	{
		private ILogger<WareHouseServices> _logger;
		private IUnitOfWork _unitOfWork;
		private IMapper _mapper;
		private IAdminOpreationServices _adminOpreationServices ;
		private ICacheManager _cacheManager ;
		private const string CACH_TAGE = "WareHouse";


		public WareHouseServices(ICacheManager cacheManager,IAdminOpreationServices adminOpreationServices,IMapper mapper,ILogger<WareHouseServices> logger, IUnitOfWork unitOfWork)
		{
			_cacheManager = cacheManager;
			_adminOpreationServices= adminOpreationServices;
			_mapper = mapper;
			_logger = logger;
			_unitOfWork = unitOfWork;
		}
		public Task<ApiResponse<InventoryDto>> AddInventoryToWareHouseAsync(int id, string userid, int Inventoryid)
		{
			throw new NotImplementedException();
		}

		public async Task<Result<WareHouseDto>> CreateWareHouseAsync(string userid, WareHouseDto wareHouse)
		{
			_logger.LogInformation($"Execute:{nameof(CreateWareHouseAsync)}");
			using var transction=await _unitOfWork.BeginTransactionAsync();
			var warehouse=_mapper.Map<Warehouse>(wareHouse);
			if(warehouse == null)
			{
				_logger.LogError("Mapping Filed");
				return Result<WareHouseDto>.Fail("Try Again later", 500);
			}
			var nameCheck = await _unitOfWork.WareHouse.GetByNameAsync(wareHouse.Name);
			if (nameCheck != null)
			{
				_logger.LogWarning($"Warehouse name {wareHouse.Name} is already in use");
				return Result<WareHouseDto>.Fail("Warehouse name is already in use", 409);
			}
			var iscreated=await _unitOfWork.WareHouse.CreateAsync(warehouse);
			if (iscreated == null) 
			{
				_logger.LogError("Error While Createing Warehouse");
				await transction.RollbackAsync();
				return Result<WareHouseDto>.Fail("Try Again later", 500);
			}
			_logger.LogInformation("Created ");
			await _cacheManager.RemoveByTagAsync(CACH_TAGE);
			await _unitOfWork.CommitAsync();
			var isadded = await _adminOpreationServices.AddAdminOpreationAsync("Add WareHouse", Opreations.AddOpreation, userid, iscreated.Id);
			if(isadded == null)
			{
				_logger.LogError("Failed to log admin operation");
				await transction.RollbackAsync();
				return Result<WareHouseDto>.Fail("Try Again later", 500);
			}
			_logger.LogInformation($"Admin Opreation added with id:{isadded.Data.Id}");
			await _unitOfWork.CommitAsync();
			await transction.CommitAsync();
			var createdwarehouse = _mapper.Map<WareHouseDto>(iscreated);
			return Result<WareHouseDto>.Ok(createdwarehouse, "Created", 201);
		}

		public async Task<Result<List<WareHouseDto>>> GetAllWareHousesAsync()
		{
			_logger.LogInformation($"Execute:{nameof(GetAllWareHousesAsync)}");
			string cach_key = "GetAllWareHouse";
			var cached_data= await _cacheManager.GetAsync<List<WareHouseDto>>(cach_key);
			if(cached_data != null)
			{
				_logger.LogInformation("From Cache");
				return Result<List<WareHouseDto>>.Ok(cached_data, "Get WareHouse", 200);
			}
			
			try
			{
				var warehouses = await _unitOfWork.WareHouse.GetAll()
					.Where(w => w.DeletedAt == null)
					.Include(w => w.ProductInventories.Where(pi => pi.DeletedAt == null))
					.ThenInclude(pi => pi.Product)
					.ToListAsync();
				
				if (warehouses == null)
				{
					_logger.LogError("Error fetching warehouses");
					return Result<List<WareHouseDto>>.Fail("Try Again later", 500);
				}
				
				var warehousesdto = warehouses.Select(w => _mapper.Map<WareHouseDto>(w)).ToList();
				if (warehousesdto == null)
				{
					_logger.LogError("Can't Mapping");
					return Result<List<WareHouseDto>>.Fail("Try Again later", 500);
				}
				
				await _cacheManager.SetAsync(cach_key, warehousesdto, tags: new string[] { CACH_TAGE });
				return Result<List<WareHouseDto>>.Ok(warehousesdto, "Get WareHouse", 200);
			}
			catch (Exception ex)
			{
				_logger.LogError($"Error in {nameof(GetAllWareHousesAsync)}: {ex.Message}");
				return Result<List<WareHouseDto>>.Fail("An error occurred while fetching warehouses", 500);
			}
		}

		public async Task<Result<WareHouseDto>> GetWareHouseByIdAsync(int id)
		{
			_logger.LogInformation($"Execute:{nameof(GetWareHouseByIdAsync)} with  id:{id}");
			
			try
			{
				var warehouse = await _unitOfWork.WareHouse.GetAll()
					.Where(w => w.Id == id && w.DeletedAt == null)
					.Include(w => w.ProductInventories.Where(pi => pi.DeletedAt == null))
					.ThenInclude(pi => pi.Product)
					.SingleOrDefaultAsync();
					
				if (warehouse == null)
				{
					_logger.LogWarning($"Can't found warehouse with this id {id}");
					return Result<WareHouseDto>.Fail($"Can't found warehouse with this id {id}", 404);
				}
				
				string key = $"WareHouse with id:{id}";
				var warehousedto = _mapper.Map<WareHouseDto>(warehouse);
				if(warehousedto is null)
				{
					_logger.LogError("Can't mapping");
					return Result<WareHouseDto>.Fail("Try Again later", 500);
				}
				
				await _cacheManager.SetAsync(key, warehousedto, tags: new string[] { CACH_TAGE });
				_logger.LogInformation("WareHouse found");
				return Result<WareHouseDto>.Ok(warehousedto, "Warehouse Found", 200);
			}
			catch (Exception ex)
			{
				_logger.LogError($"Error in {nameof(GetWareHouseByIdAsync)}: {ex.Message}");
				return Result<WareHouseDto>.Fail("An error occurred while fetching warehouse", 500);
			}
		}

		public async Task<Result<string>> RemoveWareHouseAsync(int id, string userid)
		{
			_logger.LogInformation($"Execute:{nameof(RemoveWareHouseAsync)}");
			using var transaction = await _unitOfWork.BeginTransactionAsync();
			
			try
			{
				var warehouse = await _unitOfWork.WareHouse.GetAll()
					.Where(w => w.Id == id && w.DeletedAt == null)
					.Include(w => w.ProductInventories.Where(pi => pi.DeletedAt == null))
					.AsTracking()
					.SingleOrDefaultAsync();
					
				if (warehouse == null)
				{
					_logger.LogError($"Can't found warehouse with this id {id}");
					await transaction.RollbackAsync();
					return Result<string>.Fail($"Can't found warehouse with this id {id}", 404);
				}
				
				// Check if warehouse has any products
				if (warehouse.ProductInventories.Count > 0)
				{
					_logger.LogError("Can't delete warehouse that contains products");
					await transaction.RollbackAsync();
					return Result<string>.Fail("Cannot delete warehouse that contains products", 400);
				}
				
				warehouse.DeletedAt = DateTime.UtcNow;
				warehouse.ModifiedAt = DateTime.UtcNow;
				var isRemoved = _unitOfWork.WareHouse.Update(warehouse);
				if (!isRemoved)
				{
					_logger.LogError("Failed to update warehouse");
					await transaction.RollbackAsync();
					return Result<string>.Fail("Failed to update warehouse", 500);
				}
				
				await _cacheManager.RemoveByTagAsync(CACH_TAGE);
				await _unitOfWork.CommitAsync();
				
				var isAdded = await _adminOpreationServices.AddAdminOpreationAsync("soft delete to warehouse ", Opreations.DeleteOpreation, userid, id);
				if (isAdded == null)
				{
					_logger.LogError("Failed to log admin operation");
					await transaction.RollbackAsync();
					return Result<string>.Fail("Failed to log operation", 500);
				}
				
				_logger.LogInformation($"Admin Operation added with id:{isAdded.Data.Id}");
				await _unitOfWork.CommitAsync();
				await transaction.CommitAsync();
				return Result<string>.Ok("Warehouse removed", "Warehouse removed successfully", 200);
			}
			catch (Exception ex)
			{
				_logger.LogError($"Error in {nameof(RemoveWareHouseAsync)}: {ex.Message}");
				await transaction.RollbackAsync();
				return Result<string>.Fail("An error occurred while removing warehouse", 500);
			}
		}

		public async Task<Result<string>> TransferProductsAsync(int from_warehouse_id, int to_warehouse_id, string userid, int Inventoryid)
		{
			_logger.LogInformation($"Execute:{nameof(TransferProductsAsync)}");
			using var transaction = await _unitOfWork.BeginTransactionAsync();
			
			try
			{
				var sourceWarehouse = await _unitOfWork.WareHouse.GetAll()
					.Where(w => w.Id == from_warehouse_id && w.DeletedAt == null)
					.Include(w => w.ProductInventories.Where(pi => pi.DeletedAt == null))
					.AsTracking()
					.SingleOrDefaultAsync();
					
				if (sourceWarehouse == null)
				{
					_logger.LogWarning($"Source warehouse not found with id: {from_warehouse_id}");
					return Result<string>.Fail($"Warehouse with id {from_warehouse_id} not found", 404);
				}
				
				var targetWarehouse = await _unitOfWork.WareHouse.GetAll()
					.Where(w => w.Id == to_warehouse_id && w.DeletedAt == null)
					.Include(w => w.ProductInventories.Where(pi => pi.DeletedAt == null))
					.AsTracking()
					.SingleOrDefaultAsync();
					
				if (targetWarehouse == null)
				{
					_logger.LogWarning($"Target warehouse not found with id: {to_warehouse_id}");
					return Result<string>.Fail($"Warehouse with id {to_warehouse_id} not found", 404);
				}
				
				var sourceInventory = sourceWarehouse.ProductInventories.FirstOrDefault(pi => pi.Id == Inventoryid);
				if (sourceInventory == null)
				{
					_logger.LogWarning($"Inventory {Inventoryid} not found in source warehouse {from_warehouse_id}");
					return Result<string>.Fail($"Inventory {Inventoryid} not found in source warehouse", 404);
				}
			try
			{
				var existingInventory = targetWarehouse.ProductInventories.FirstOrDefault(pi => pi.ProductId == sourceInventory.ProductId);
				if (existingInventory != null)
				{
					existingInventory.Quantity += sourceInventory.Quantity;
					existingInventory.ModifiedAt = DateTime.UtcNow;
					var updateResult = _unitOfWork.Repository<ProductInventory>().Update(existingInventory);
					if (!updateResult)
					{
						_logger.LogError("Failed to update inventory in target warehouse");
						await transaction.RollbackAsync();
						return Result<string>.Fail("Failed to update target warehouse inventory", 500);
					}
				}
				else
				{
					var newInventory = new ProductInventory
					{
						ProductId = sourceInventory.ProductId,
						WarehouseId = to_warehouse_id,
						Quantity = sourceInventory.Quantity,
						CreatedAt = DateTime.UtcNow,
						ModifiedAt = DateTime.UtcNow
					};
					var createResult = await _unitOfWork.Repository<ProductInventory>().CreateAsync(newInventory);
					if (createResult == null)
					{
						_logger.LogError("Failed to create inventory in target warehouse");
						await transaction.RollbackAsync();
						return Result<string>.Fail("Failed to create inventory in target warehouse", 500);
					}
				}
				sourceInventory.DeletedAt = DateTime.UtcNow;
				var removeResult = _unitOfWork.Repository<ProductInventory>().Update(sourceInventory);
				if (!removeResult)
				{
					_logger.LogError("Failed to remove inventory from source warehouse");
					await transaction.RollbackAsync();
					return Result<string>.Fail("Failed to remove inventory from source warehouse", 500);
				}
				var adminLog = await _adminOpreationServices.AddAdminOpreationAsync(
					$"Transferred inventory {Inventoryid} from warehouse {from_warehouse_id} to warehouse {to_warehouse_id}",
					Opreations.UpdateOpreation,
					userid,
					Inventoryid
				);
				if (adminLog == null)
				{
					_logger.LogError("Failed to log admin operation");
					await transaction.RollbackAsync();
					return Result<string>.Fail("Failed to log operation", 500);
				}
				await _cacheManager.RemoveByTagAsync(CACH_TAGE);
				await _unitOfWork.CommitAsync();
				await transaction.CommitAsync();
				return Result<string>.Ok($"Transferred {sourceInventory.Quantity} units of product {sourceInventory.ProductId} from warehouse {from_warehouse_id} to warehouse {to_warehouse_id}", "Products transferred successfully", 200);
			}
			catch (Exception ex)
			{
				_logger.LogError($"Error in {nameof(TransferProductsAsync)}: {ex.Message}");
				await transaction.RollbackAsync();
				return Result<string>.Fail("An error occurred while transferring products", 500);
			}
		}

		public async Task<Result<WareHouseDto>> UpdateWareHouseAsync(int id, string userid, WareHouseDto wareHouse)
		{
			_logger.LogInformation($"Execute:{nameof(UpdateWareHouseAsync)}");
			using var transaction = await _unitOfWork.BeginTransactionAsync();
			
			try
			{
				var existingWarehouse = await _unitOfWork.WareHouse.GetAll()
					.Where(w => w.Id == id && w.DeletedAt == null)
					.AsTracking()
					.SingleOrDefaultAsync();
					
				if (existingWarehouse == null)
				{
					_logger.LogWarning($"Warehouse not found with id: {id}");
					return Result<WareHouseDto>.Fail($"Warehouse with id {id} not found", 404);
				}
				
				if (!string.IsNullOrEmpty(wareHouse.Name))
				{
					if (existingWarehouse.Name.Equals(wareHouse.Name, StringComparison.OrdinalIgnoreCase))
					{
						_logger.LogWarning($"Same Name ID: {id}");
						return Result<WareHouseDto>.Fail("Can't Use Same Name", 409);
					}
					var nameCheck = await _unitOfWork.WareHouse.GetByNameAsync(wareHouse.Name);
					if (nameCheck != null)
					{
						_logger.LogWarning($"Warehouse name {wareHouse.Name} is already in use");
						return Result<WareHouseDto>.Fail("Warehouse name is already in use", 400);
					}
					existingWarehouse.Name = wareHouse.Name;
				}
				
				if (!string.IsNullOrEmpty(wareHouse.Address))
				{
					existingWarehouse.Address = wareHouse.Address;
				}
				
				if (!string.IsNullOrEmpty(wareHouse.Phone))
				{
					existingWarehouse.Phone = wareHouse.Phone;
				}
				
				if (!string.IsNullOrEmpty(wareHouse.Email))
				{
					existingWarehouse.Email = wareHouse.Email;
				}
				
				if (!string.IsNullOrEmpty(wareHouse.Description))
				{
					existingWarehouse.Description = wareHouse.Description;
				}
				
				if (!string.IsNullOrEmpty(wareHouse.ManagerName))
				{
					existingWarehouse.ManagerName = wareHouse.ManagerName;
				}
				
				if (!string.IsNullOrEmpty(wareHouse.ManagerPhone))
				{
					existingWarehouse.ManagerPhone = wareHouse.ManagerPhone;
				}
				
				existingWarehouse.IsActive = wareHouse.IsActive;
				existingWarehouse.ModifiedAt = DateTime.UtcNow;
				
				var updateResult = _unitOfWork.WareHouse.Update(existingWarehouse);
				if (!updateResult)
				{
					_logger.LogError("Failed to update warehouse");
					await transaction.RollbackAsync();
					return Result<WareHouseDto>.Fail("Failed to update warehouse", 500);
				}
				
				var adminLog = await _adminOpreationServices.AddAdminOpreationAsync(
					$"Updated warehouse {id}",
					Opreations.UpdateOpreation,
					userid,
					id
				);
				if (adminLog == null)
				{
					_logger.LogError("Failed to log admin operation");
					await transaction.RollbackAsync();
					return Result<WareHouseDto>.Fail("Failed to log operation", 500);
				}
				
				await _cacheManager.RemoveByTagAsync(CACH_TAGE);
				await _unitOfWork.CommitAsync();
				await transaction.CommitAsync();
				
				var updatedWarehouseDto = _mapper.Map<WareHouseDto>(existingWarehouse);
				if (updatedWarehouseDto == null)
				{
					_logger.LogError("Failed to map updated warehouse to DTO");
					return Result<WareHouseDto>.Fail("Failed to map warehouse data", 500);
				}
				
				return Result<WareHouseDto>.Ok(updatedWarehouseDto, "Warehouse updated successfully", 200);
			}
			catch (Exception ex)
			{
				_logger.LogError($"Error in {nameof(UpdateWareHouseAsync)}: {ex.Message}");
				await transaction.RollbackAsync();
				return Result<WareHouseDto>.Fail("An error occurred while updating warehouse", 500);
			}
		}

		public async Task<Result<string>> IsExsistAsync(int id)
		{
			var isexsist= await _unitOfWork.WareHouse.IsExsistAsync(id);
			return isexsist ? Result<string>.Ok("Found", "Found", 200) : Result<string>.Fail($"No Warehouse with this id:{id}", 404);
		}

		public async Task<Result<WareHouseDto>> ReturnRemovedWareHouseAsync(int id, string userid)
		{
			_logger.LogInformation($"Execute:{nameof(ReturnRemovedWareHouseAsync)}");
			
			try
			{
				var isdeleted = await _unitOfWork.WareHouse.IsDeletedAsync(id);
				if (!isdeleted) 
				{
					_logger.LogWarning($"No WareHouse Deleted with this id:{id}");
					return Result<WareHouseDto>.Fail($"No WareHouse Deleted with this id:{id}", 404);
				}
				
				var deletedwarehouse = await _unitOfWork.WareHouse.GetAll()
					.Where(w => w.Id == id && w.DeletedAt != null)
					.AsTracking()
					.SingleOrDefaultAsync();
					
				if(deletedwarehouse == null)
				{
					return Result<WareHouseDto>.Fail($"No WareHouse Deleted with this id:{id}", 404);
				}
				
				using var transaction = await _unitOfWork.BeginTransactionAsync();
				try
				{
					deletedwarehouse.DeletedAt = null;
					deletedwarehouse.ModifiedAt = DateTime.UtcNow;
					var isupdated = _unitOfWork.WareHouse.Update(deletedwarehouse);
					if (!isupdated)
					{
						_logger.LogError("Failed to update warehouse");
						await transaction.RollbackAsync();
						return Result<WareHouseDto>.Fail("Try Again Later", 500);
					}
					
					var isadded = await _adminOpreationServices.AddAdminOpreationAsync("Return WareHouse From Deleted", Opreations.UndoDeleteOpreation, userid, id);
					if(isadded == null)
					{
						_logger.LogError("Failed to log admin operation");
						await transaction.RollbackAsync();
						return Result<WareHouseDto>.Fail("Try Again Later", 500);
					}
					
					await _cacheManager.RemoveByTagAsync(CACH_TAGE);
					await _unitOfWork.CommitAsync();
					await transaction.CommitAsync();
					
					var warehousedto = _mapper.Map<WareHouseDto>(deletedwarehouse);
					if(warehousedto == null)
					{
						return Result<WareHouseDto>.Fail("Try Again Later", 500);
					}
					
					return Result<WareHouseDto>.Ok(warehousedto, "Warehouse restored successfully", 200);
				}
				catch (Exception ex)
				{
					_logger.LogError($"Error in {nameof(ReturnRemovedWareHouseAsync)}: {ex.Message}");
					await transaction.RollbackAsync();
					return Result<WareHouseDto>.Fail("Try Again Later", 500);
				}
			}
			catch (Exception ex)
			{
				_logger.LogError($"Error in {nameof(ReturnRemovedWareHouseAsync)}: {ex.Message}");
				return Result<WareHouseDto>.Fail("An error occurred while restoring warehouse", 500);
			}
		}

		Task<Result<InventoryDto>> IWareHouseServices.AddInventoryToWareHouseAsync(int id, string userid, int Inventoryid)
		{
			throw new NotImplementedException();
		}
	}
}
