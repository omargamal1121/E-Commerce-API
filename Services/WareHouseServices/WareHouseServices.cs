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

		public async Task<ApiResponse<WareHouseDto>> CreateWareHouseAsync(string userid, WareHouseDto wareHouse)
		{
			_logger.LogInformation($"Execute:{nameof(CreateWareHouseAsync)}");
			using var transction=await _unitOfWork.BeginTransactionAsync();
			var warehouse=_mapper.Map<Warehouse>(wareHouse);
			if(warehouse == null)
			{
				_logger.LogError("Mapping Filed");

				return ApiResponse<WareHouseDto>.CreateErrorResponse(new ErrorResponse("Server Error", "Try Again later"), 500);

			}
			var nameCheck = await _unitOfWork.WareHouse.GetByNameAsync(wareHouse.Name);
			if (nameCheck.Success)
			{
				_logger.LogWarning($"Warehouse name {wareHouse.Name} is already in use");
				return ApiResponse<WareHouseDto>.CreateErrorResponse(new ErrorResponse("Name", "Warehouse name is already in use"), 409);
			}
			var iscreated=await _unitOfWork.WareHouse.CreateAsync(warehouse);
			if (!iscreated.Success||iscreated.Data == null) 
			{
				_logger.LogError("Error While Createing Warehouse");
				await transction.RollbackAsync();
				return ApiResponse<WareHouseDto>.CreateErrorResponse(new ErrorResponse("Server Error", "Try Again later"), 500);
			}
			_logger.LogInformation("Created ");
			await _cacheManager.RemoveByTagAsync(CACH_TAGE);
			await _unitOfWork.CommitAsync();
			var isadded = await _adminOpreationServices.AddAdminOpreationAsync("Add WareHouse", Opreations.AddOpreation, userid, iscreated.Data.Id);
			if(!isadded.Success||isadded.Data is null)
			{
				_logger.LogError(isadded.Message);
				await transction.RollbackAsync();
				return ApiResponse<WareHouseDto>.CreateErrorResponse(new ErrorResponse("Server Error", "Try Again later"), 500);

			}
			_logger.LogInformation($"Admin Opreation added with id:{isadded.Data.Id}");
			await _unitOfWork.CommitAsync();
			await transction.CommitAsync();
			var createdwarehouse = _mapper.Map<WareHouseDto>(iscreated.Data);
			
			return ApiResponse<WareHouseDto>.CreateSuccessResponse("Created", createdwarehouse,201);

		}

		public async Task<ApiResponse<List<WareHouseDto>>> GetAllWareHousesAsync()
		{
			_logger.LogInformation($"Execute:{nameof(GetAllWareHousesAsync)}");
			string cach_key = "GetAllWareHouse";
			var cached_data= await _cacheManager.GetAsync<List<WareHouseDto>>(cach_key);
			if(cached_data != null)
			{
				_logger.LogInformation("From Cach");
				return ApiResponse<List<WareHouseDto>>.CreateSuccessResponse("Get WareHouse", cached_data);


			}
			var warehousereult= await _unitOfWork.WareHouse.GetAllAsync();
			if(!warehousereult.Success||warehousereult.Data is null)
			{
				_logger.LogError(warehousereult.Message);
				return ApiResponse<List<WareHouseDto>>.CreateErrorResponse(new ErrorResponse("Server Error", "Try Again later"), 500);
			}
			var warehousesdto= await warehousereult.Data.Where(w=>w.DeletedAt==null).Select(w=>_mapper.Map<WareHouseDto>(w)).ToListAsync();
			if (warehousesdto is null)
			{
				_logger.LogError("Can't Mapping");
				return ApiResponse<List<WareHouseDto>>.CreateErrorResponse(new ErrorResponse("Server Error", "Try Again later"), 500);
			}
			await _cacheManager.SetAsync(cach_key,warehousesdto,tags:new string[] { CACH_TAGE });
			return ApiResponse<List<WareHouseDto>>.CreateSuccessResponse("Get WareHouse", warehousesdto);
		}

		public async Task<ApiResponse<WareHouseDto>> GetWareHouseByIdAsync(int id)
		{
			_logger.LogInformation($"Execute:{nameof(GetWareHouseByIdAsync)} with  id:{id}");
			var warehouse= await _unitOfWork.WareHouse.GetByIdAsync(id);
			if (!warehouse.Success|| warehouse.Data is null)
			{
				_logger.LogWarning(warehouse.Message);
				return ApiResponse<WareHouseDto>.CreateErrorResponse(new ErrorResponse("WareHouse Id",$"Can't found warehouse with this id {id}"),404);
			}
			string key = $"WareHouse with id:{id}";
			var warehousedto = _mapper.Map<WareHouseDto>(warehouse.Data);
			if(warehousedto is null)
			{
				_logger.LogError("Can't mapping");
				return ApiResponse<WareHouseDto>.CreateErrorResponse(new ErrorResponse("Server Error", "Try Again later"), 500);
			}
			await _cacheManager.SetAsync(key, warehousedto,tags: new string []{CACH_TAGE});
			_logger.LogInformation("WareHouse found");

			return ApiResponse<WareHouseDto>.CreateSuccessResponse("Warehouse Found", warehousedto);
		}

		public async Task<ApiResponse<string>> RemoveWareHouseAsync(int id, string userid)
		{
			_logger.LogInformation($"Execute:{nameof(RemoveWareHouseAsync)}");
			using var transaction = await _unitOfWork.BeginTransactionAsync();
			
			var isexsist = await IsExsistAsync(id);
			if(isexsist.Statuscode == 404)
			{
				return ApiResponse<string>.CreateErrorResponse(new ErrorResponse("Warehouse Id", $"Can't found warehouse with this id {id}"), 404);
			}

			var warehouse = await _unitOfWork.WareHouse.GetByIdAsync(id);
			if (!warehouse.Success || warehouse.Data == null)
			{
				_logger.LogError(warehouse.Message);
				await transaction.RollbackAsync();
				return ApiResponse<string>.CreateErrorResponse(new ErrorResponse("Server Error", "Try Again later"), 500);
			}

			// Check if warehouse has any products
			if (warehouse.Data.ProductInventories.Count > 0)
			{
				_logger.LogError("Can't delete warehouse that contains products");
				await transaction.RollbackAsync();
				return ApiResponse<string>.CreateErrorResponse(new ErrorResponse("Warehouse", "Cannot delete warehouse that contains products"), 400);
			}
			warehouse.Data.DeletedAt = DateTime.UtcNow;
			var isRemoved = await _unitOfWork.WareHouse.UpdateAsync(warehouse.Data);
			if (!isRemoved.Success)
			{
				_logger.LogError(isRemoved.Message);
				await transaction.RollbackAsync();
				return ApiResponse<string>.CreateErrorResponse(new ErrorResponse("Server Error", "Try Again later"), 500);
			}

			await _cacheManager.RemoveByTagAsync(CACH_TAGE);
			await _unitOfWork.CommitAsync();

			var isAdded = await _adminOpreationServices.AddAdminOpreationAsync("soft delete to warehouse ", Opreations.DeleteOpreation, userid, id);
			if (!isAdded.Success || isAdded.Data is null)
			{
				_logger.LogError(isAdded.Message);
				await transaction.RollbackAsync();
				return ApiResponse<string>.CreateErrorResponse(new ErrorResponse("Server Error", "Try Again later"), 500);
			}

			_logger.LogInformation($"Admin Operation added with id:{isAdded.Data.Id}");
			await _unitOfWork.CommitAsync();
			await transaction.CommitAsync();

			return ApiResponse<string>.CreateSuccessResponse("Warehouse removed successfully", "Warehouse removed", 200);
		}

		public async Task<ApiResponse<string>> TransferProductsAsync(int from_warehouse_id, int to_warehouse_id, string userid, int Inventoryid)
		{
			_logger.LogInformation($"Execute:{nameof(TransferProductsAsync)}");
			using var transaction = await _unitOfWork.BeginTransactionAsync();

			// Validate source warehouse
			var sourceWarehouse = await _unitOfWork.WareHouse.GetByIdAsync(from_warehouse_id);
			if (!sourceWarehouse.Success || sourceWarehouse.Data == null)
			{
				_logger.LogWarning($"Source warehouse not found with id: {from_warehouse_id}");
				return ApiResponse<string>.CreateErrorResponse(new ErrorResponse("Source Warehouse", $"Warehouse with id {from_warehouse_id} not found"), 404);
			}

			// Validate target warehouse
			var targetWarehouse = await _unitOfWork.WareHouse.GetByIdAsync(to_warehouse_id);
			if (!targetWarehouse.Success || targetWarehouse.Data == null)
			{
				_logger.LogWarning($"Target warehouse not found with id: {to_warehouse_id}");
				return ApiResponse<string>.CreateErrorResponse(new ErrorResponse("Target Warehouse", $"Warehouse with id {to_warehouse_id} not found"), 404);
			}

			// Validate inventory exists in source warehouse
			var sourceInventory = sourceWarehouse.Data.ProductInventories.FirstOrDefault(pi => pi.Id == Inventoryid);
			if (sourceInventory == null)
			{
				_logger.LogWarning($"Inventory {Inventoryid} not found in source warehouse {from_warehouse_id}");
				return ApiResponse<string>.CreateErrorResponse(new ErrorResponse("Inventory", $"Inventory {Inventoryid} not found in source warehouse"), 404);
			}

			try
			{
				// Check if target warehouse already has this product
				var existingInventory = targetWarehouse.Data.ProductInventories.FirstOrDefault(pi => pi.ProductId == sourceInventory.ProductId);
				
				if (existingInventory != null)
				{
					// Update quantity in target warehouse
					existingInventory.Quantity += sourceInventory.Quantity;
					existingInventory.ModifiedAt = DateTime.UtcNow;
					
					var updateResult = await _unitOfWork.Repository<ProductInventory>().UpdateAsync(existingInventory);
					if (!updateResult.Success)
					{
						_logger.LogError($"Failed to update inventory in target warehouse: {updateResult.Message}");
						await transaction.RollbackAsync();
						return ApiResponse<string>.CreateErrorResponse(new ErrorResponse("Server Error", "Failed to update target warehouse inventory"), 500);
					}
				}
				else
				{
					// Create new inventory in target warehouse
					var newInventory = new ProductInventory
					{
						ProductId = sourceInventory.ProductId,
						WarehouseId = to_warehouse_id,
						Quantity = sourceInventory.Quantity,
						CreatedAt = DateTime.UtcNow,
						ModifiedAt = DateTime.UtcNow
					};

					var createResult = await _unitOfWork.Repository<ProductInventory>().CreateAsync(newInventory);
					if (!createResult.Success)
					{
						_logger.LogError($"Failed to create inventory in target warehouse: {createResult.Message}");
						await transaction.RollbackAsync();
						return ApiResponse<string>.CreateErrorResponse(new ErrorResponse("Server Error", "Failed to create inventory in target warehouse"), 500);
					}
				}

				// Remove inventory from source warehouse
				sourceInventory.DeletedAt = DateTime.UtcNow;
				var removeResult = await _unitOfWork.Repository<ProductInventory>().UpdateAsync(sourceInventory);
				if (!removeResult.Success)
				{
					_logger.LogError($"Failed to remove inventory from source warehouse: {removeResult.Message}");
					await transaction.RollbackAsync();
					return ApiResponse<string>.CreateErrorResponse(new ErrorResponse("Server Error", "Failed to remove inventory from source warehouse"), 500);
				}

				// Log admin operation
				var adminLog = await _adminOpreationServices.AddAdminOpreationAsync(
					$"Transferred inventory {Inventoryid} from warehouse {from_warehouse_id} to warehouse {to_warehouse_id}",
					Opreations.UpdateOpreation,
					userid,
					Inventoryid
				);

				if (!adminLog.Success)
				{
					_logger.LogError($"Failed to log admin operation: {adminLog.Message}");
					await transaction.RollbackAsync();
					return ApiResponse<string>.CreateErrorResponse(new ErrorResponse("Server Error", "Failed to log operation"), 500);
				}

				await _cacheManager.RemoveByTagAsync(CACH_TAGE);
				await _unitOfWork.CommitAsync();
				await transaction.CommitAsync();

				return ApiResponse<string>.CreateSuccessResponse(
					"Products transferred successfully",
					$"Transferred {sourceInventory.Quantity} units of product {sourceInventory.ProductId} from warehouse {from_warehouse_id} to warehouse {to_warehouse_id}",
					200
				);
			}
			catch (Exception ex)
			{
				_logger.LogError($"Error in {nameof(TransferProductsAsync)}: {ex.Message}");
				await transaction.RollbackAsync();
				return ApiResponse<string>.CreateErrorResponse(new ErrorResponse("Server Error", new List<string> { "An error occurred while transferring products" }), 500);
			}
		}

		public async Task<ApiResponse<WareHouseDto>> UpdateWareHouseAsync(int id, string userid, WareHouseDto wareHouse)
		{
			_logger.LogInformation($"Execute:{nameof(UpdateWareHouseAsync)}");
			using var transaction = await _unitOfWork.BeginTransactionAsync();

			// Validate warehouse exists
			var existingWarehouse = await _unitOfWork.WareHouse.GetByIdAsync(id);
			if (!existingWarehouse.Success || existingWarehouse.Data == null)
			{
				_logger.LogWarning($"Warehouse not found with id: {id}");
				return ApiResponse<WareHouseDto>.CreateErrorResponse(new ErrorResponse("Warehouse", new List<string> { $"Warehouse with id {id} not found" }), 404);
			}

			try
			{
				// Validate and update name
				if (!string.IsNullOrEmpty(wareHouse.Name))
				{
					// Check if name is the same
					if (existingWarehouse.Data.Name.Equals(wareHouse.Name, StringComparison.OrdinalIgnoreCase))
					{
						_logger.LogWarning($"Same Name ID: {id}");
						return ApiResponse<WareHouseDto>.CreateErrorResponse(new ErrorResponse("Name", new List<string> { "Can't Use Same Name" }), 409);
					}

					// Check if name is already in use
					var nameCheck = await _unitOfWork.WareHouse.GetByNameAsync(wareHouse.Name);
					if (nameCheck.Success)
					{
						_logger.LogWarning($"Warehouse name {wareHouse.Name} is already in use");
						return ApiResponse<WareHouseDto>.CreateErrorResponse(new ErrorResponse("Name", new List<string> { "Warehouse name is already in use" }), 400);
					}

					existingWarehouse.Data.Name = wareHouse.Name;
				}

				// Validate and update address
				if (!string.IsNullOrEmpty(wareHouse.Address))
				{

					existingWarehouse.Data.Address = wareHouse.Address;
				}

				// Update phone if provided
				if (!string.IsNullOrEmpty(wareHouse.Phone))
				{
					existingWarehouse.Data.Phone = wareHouse.Phone;
				}

				existingWarehouse.Data.ModifiedAt = DateTime.UtcNow;

				// Update warehouse
				var updateResult = await _unitOfWork.WareHouse.UpdateAsync(existingWarehouse.Data);
				if (!updateResult.Success)
				{
					_logger.LogError($"Failed to update warehouse: {updateResult.Message}");
					await transaction.RollbackAsync();
					return ApiResponse<WareHouseDto>.CreateErrorResponse(new ErrorResponse("Server Error", "Failed to update warehouse"), 500);
				}

				// Log admin operation
				var adminLog = await _adminOpreationServices.AddAdminOpreationAsync(
					$"Updated warehouse {id}",
					Opreations.UpdateOpreation,
					userid,
					id
				);

				if (!adminLog.Success)
				{
					_logger.LogError($"Failed to log admin operation: {adminLog.Message}");
					await transaction.RollbackAsync();
					return ApiResponse<WareHouseDto>.CreateErrorResponse(new ErrorResponse("Server Error", "Failed to log operation"), 500);
				}

				await _cacheManager.RemoveByTagAsync(CACH_TAGE);
				await _unitOfWork.CommitAsync();
				await transaction.CommitAsync();

				// Map updated warehouse to DTO
				var updatedWarehouseDto = _mapper.Map<WareHouseDto>(existingWarehouse.Data);
				if (updatedWarehouseDto == null)
				{
					_logger.LogError("Failed to map updated warehouse to DTO");
					return ApiResponse<WareHouseDto>.CreateErrorResponse(new ErrorResponse("Server Error", "Failed to map warehouse data"), 500);
				}

				return ApiResponse<WareHouseDto>.CreateSuccessResponse(
					"Warehouse updated successfully",
					updatedWarehouseDto,
					200
				);
			}
			catch (Exception ex)
			{
				_logger.LogError($"Error in {nameof(UpdateWareHouseAsync)}: {ex.Message}");
				await transaction.RollbackAsync();
				return ApiResponse<WareHouseDto>.CreateErrorResponse(new ErrorResponse("Server Error", "An error occurred while updating warehouse"), 500);
			}
		}

		public async Task<ApiResponse<string>> IsExsistAsync(int id)
		{
			var isexsist= await _unitOfWork.WareHouse.IsExsistAsync(id);
			return isexsist.Success ? ApiResponse<string>.CreateSuccessResponse("Found"):ApiResponse<string>.CreateErrorResponse(new ErrorResponse("Warehouse id", $"No Warehouse with this id:{id}"),404);
		}

		public async Task<ApiResponse<WareHouseDto>> ReturnRemovedWareHouseAsync(int id, string userid)
		{
			_logger.LogInformation($"Execute:{nameof(ReturnRemovedWareHouseAsync)}");
			var isdeleted= await _unitOfWork.WareHouse.IsDeletedAsync(id);
			if (!isdeleted.Success) 
			{
				_logger.LogWarning($"No WareHouse Deleted with this id:{id}");
				return ApiResponse<WareHouseDto>.CreateErrorResponse(new ErrorResponse("WareHouse id", $"No WareHouse Deleted with this id:{id}"),404);
			}
			var deletdwarehouse=await _unitOfWork.WareHouse.GetByIdAsync(id);
			if(!deletdwarehouse.Success||deletdwarehouse.Data is null)
			{
				return ApiResponse<WareHouseDto>.CreateErrorResponse(new ErrorResponse("WareHouse id", $"No WareHouse Deleted with this id:{id}"), 404);

			}
			using var transaction= await _unitOfWork.BeginTransactionAsync();
			try
			{

				deletdwarehouse.Data.DeletedAt = null;
				var isupdated = await _unitOfWork.WareHouse.UpdateAsync(deletdwarehouse.Data);
				if (!isupdated.Success || isupdated.Data is null)
				{
					_logger.LogError(isupdated.Message);
					await transaction.RollbackAsync();
					return ApiResponse<WareHouseDto>.CreateErrorResponse(new ErrorResponse("Server Error ", "Try Again Later"), 500);


				}

				var isadded = await _adminOpreationServices.AddAdminOpreationAsync("Return WareHouse From Deleted", Opreations.UndoDeleteOpreation, userid, id);
				if(!isadded .Success)
				{
					_logger.LogError(isadded.Message);
					await transaction.RollbackAsync();
					return ApiResponse<WareHouseDto>.CreateErrorResponse(new ErrorResponse("Server Error ", "Try Again Later"), 500);


				}

				await _unitOfWork.CommitAsync();
				await transaction.RollbackAsync();
				var warehousedto = _mapper.Map<WareHouseDto>(deletdwarehouse.Data);
				if(warehousedto==null)
				{
					return ApiResponse<WareHouseDto>.CreateErrorResponse(new ErrorResponse("Server Error ", "Try Again Later"), 500);

				}
				return ApiResponse<WareHouseDto>.CreateSuccessResponse("Done", warehousedto);

			}
			catch (Exception ex)
			{

				_logger.LogError(ex.Message);
			 await	transaction.RollbackAsync();
					return ApiResponse<WareHouseDto>.CreateErrorResponse(new ErrorResponse("Server Error ", "Try Again Later"), 500);

			}

		}
	}
}
