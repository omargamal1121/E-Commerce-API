using E_Commers.DtoModels.AccountDtos;

using E_Commers.DtoModels.DiscoutDtos;
using E_Commers.DtoModels.InventoryDtos;
using E_Commers.DtoModels.ProductDtos;
using E_Commers.DtoModels.WareHouseDtos;
using E_Commers.Helper;
using E_Commers.Models;
using E_Commers.UOW;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using E_Commers.Interfaces;
using E_Commers.Enums;
using E_Commers.DtoModels.CategoryDtos;
using Microsoft.AspNetCore.Authorization;
using System.Transactions;
using System.Linq;

namespace E_Commers.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
	[Authorize]
    public class WareHouseController : ControllerBase
    {
		private readonly IUnitOfWork _unitOfWork;
		private readonly ILogger<WareHouseController> _logger;

		public WareHouseController(IUnitOfWork unitOfWork, ILogger<WareHouseController> logger)
		{
			_unitOfWork = unitOfWork;
			_logger = logger;

		}
		[HttpGet("GetAll")]
	//	[ResponseCache(Duration = 30)]
		public async Task<ActionResult<ResponseDto>> GetAll()
		{
			_logger.LogInformation($"Executing {nameof(GetAll)} in WareHouseController");


			var ResultWarehouses = await _unitOfWork.Repository<Warehouse>().GetAllAsync(
				q => q.Include(w => w.ProductInventories)
		  .ThenInclude(pi => pi.Product)
		  .ThenInclude(p => p.Category)
		  .Include(w => w.ProductInventories)
		  .ThenInclude(pi => pi.Product)
		  .ThenInclude(p => p.Discount),w=>w.DeletedAt==null

						);
			if (!ResultWarehouses.Success)
			{
				_logger.LogWarning("No WareHouses found");
				return Ok(new ResponseDto { Message = "No WareHouses found", StatusCode = 200 });
			}

			var inventoryData = await _unitOfWork.Repository<Warehouse>()
		 .GetAllAsync();

			
			var warehouseDtos = ResultWarehouses.Data
			
				.Select(g => new WareHouseDto
				{
					Id = g.Id,

					Address=g.Address,
					Phone=g.Phone,
					Name = g.Name,
					CreatedAt = g.CreatedAt,
					ModifiedAt = g.ModifiedAt ,
					Inventory = g.ProductInventories.Select(i => new InventoryDto
					{
					
						Id = i.Id,
						Quantityinsidewarehouse=i.Quantity,
						Product = new ProductDto
						{
							Id = i.Product.Id,
							Name = i.Product.Name,
							AvailabeQuantity = i.Product.Quantity,
							Category = 
							new CategoryDto(i.Product.CategoryId,i.Product.Category.Name,i.Product.Category.Description,i.Product.Category.CreatedAt),
							Discount = i.Product.Discount != null ? new DiscountDto
							{
								Id = i.Product.Discount.Id,
								Name=i.Product.Discount.Name,
								Description = i.Product.Discount.Description,
								 CreatedAt = i.Product.CreatedAt 
							} : null
						}
					}).ToList()
				})
				.ToList();

			return Ok(new ResponseDto { Data=warehouseDtos,Message = ResultWarehouses.Message, StatusCode = 200 });
		}

		[HttpPost]
		public async Task<ActionResult<ResponseDto>> CreateWareHouseAsnc(CreateWareHouseDto model)
		{
			_logger.LogInformation($"Executing {nameof(CreateWareHouseAsnc)}");

			if (!ModelState.IsValid)
			{
				var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
				_logger.LogError($"Validation Errors: {string.Join(", ", errors)}");

				return BadRequest(new ResponseDto
				{
					StatusCode = 400,
					Message = "Invalid data: " + string.Join(", ", errors)
				});
			}

			string? userid = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
			if (string.IsNullOrEmpty(userid))
			{
				_logger.LogError("Admin ID not found, canceling create operation.");
				return Unauthorized(new ResponseDto { StatusCode = 401, Message = "Invalid Admin ID." });
			}

			ResultDto<Warehouse?> checkname=await _unitOfWork.WareHouse.GetByNameAsync(model.Name);
			if(checkname.Success)
			{
				_logger.LogWarning("Can't used this Name Becouse it Exsist");
				return BadRequest(new ResponseDto { StatusCode = 400, Message = "Can't used this Name Becouse it Exsist" });
			}

			
			using var transaction = await _unitOfWork.BeginTransactionAsync();
			try
			{
				Warehouse warehouse = new Warehouse { Address=model.Address,  Phone=model.Phone , Name = model.Name };
				ResultDto<bool> result = await _unitOfWork.Repository<Warehouse>().CreateAsync(warehouse);

				if (!result.Success)
				{
					_logger.LogWarning(result.Message);
					return BadRequest(new ResponseDto { Message = result.Message, StatusCode = 400 });
				}

				int changes = await _unitOfWork.CommitAsync();
				if (changes == 0)
				{
					_logger.LogWarning("Nothing added");
					return BadRequest(new ResponseDto { Message = "Nothing added", StatusCode = 400 });
				}

				_logger.LogInformation($"warehouse added successfully, ID: {warehouse.Id}");

				AdminOperationsLog adminOperations = new()
				{
					AdminId = userid,
					Description = $"Added warehouse: {warehouse.Id}",
					Timestamp = DateTime.UtcNow
				};

				ResultDto<bool> logResult = await _unitOfWork.Repository<AdminOperationsLog>().CreateAsync(adminOperations);
				if (!logResult.Success)
				{
					await transaction.RollbackAsync();
					return StatusCode(500, new ResponseDto { StatusCode = 500, Message = logResult.Message });
				}

				await _unitOfWork.CommitAsync();
				await transaction.CommitAsync();

				return Ok(new ResponseDto { Message = $"Added successfully, ID: {warehouse.Id}", StatusCode = 200 });
			}
			catch (Exception ex)
			{
				_logger.LogError($"Exception: {ex.Message}");
				await transaction.RollbackAsync();
				return StatusCode(500, new ResponseDto { Message = "An error occurred while saving data.", StatusCode = 500 });
			}
		}
		[HttpPatch("{id}")]
		public async Task<ActionResult<ResponseDto>> UpdateWareHouse(
	[FromRoute] int id,
		[FromBody] UpdateWareHouseDto updateDto)
		{
			_logger.LogInformation($"Executing {nameof(UpdateWareHouse)}");

			if (!ModelState.IsValid)
			{
				var errors = string.Join("; ", ModelState.Values
														  .SelectMany(v => v.Errors)
														  .Select(e => e.ErrorMessage));
				_logger.LogError(errors);

				return BadRequest(new ResponseDto { StatusCode = 400, Message = errors });

			}
			string? adminId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
			if (string.IsNullOrEmpty(adminId))
			{
				_logger.LogError("Admin ID not found, canceling delete operation.");
				return Unauthorized(new ResponseDto { StatusCode = 401, Message = "Invalid Admin ID." });
			}

			ResultDto<Warehouse>? resultwarehouse = await _unitOfWork.Repository<Warehouse>().GetByIdAsync(id);
			if (!resultwarehouse.Success)
			{
				_logger.LogWarning($"No Category With this ID: {id}");
				return BadRequest(new ResponseDto { StatusCode = 400, Message = $"No Category With this ID: {id}" });
			}
			if (!string.IsNullOrWhiteSpace(updateDto.NewName))
			{
				if (resultwarehouse.Data.Name.Equals(updateDto.NewName, StringComparison.OrdinalIgnoreCase))
				{
					_logger.LogWarning($"Same Name ID: {id}");
					return BadRequest(new ResponseDto { StatusCode = 400, Message = $"Can't Use Same Name" });
				}
				if (updateDto.NewName.Length > 20 || updateDto.NewName.Length < 5)
				{

					_logger.LogWarning($"Invalid Name ID: {id}");
					return BadRequest(new ResponseDto { StatusCode = 400, Message = $"Name Must be from 5 charc to 20" });
				}
				resultwarehouse.Data.Name = updateDto.NewName;
			}

			if (!string.IsNullOrWhiteSpace(updateDto.NewAddress))
			{
				if (updateDto.NewAddress.Length > 50 || updateDto.NewAddress.Length < 10)
				{

					_logger.LogWarning($"Invalid Description ID: {id}");
					return BadRequest(new ResponseDto { StatusCode = 400, Message = $"Description Must be from 10 charc to 50" });
				}
				resultwarehouse.Data.Address = updateDto.NewAddress;
			}
			using var transaction = await _unitOfWork.BeginTransactionAsync();

			ResultDto<bool> result = await _unitOfWork.Repository<Warehouse>().UpdateAsync(resultwarehouse.Data);
			if (!result.Success)
			{
				_logger.LogError(result.Message);
				await transaction.RollbackAsync();
				return BadRequest(new ResponseDto { StatusCode = 400, Message = result.Message });
			}

			AdminOperationsLog adminOperations = new()
			{
				OperationType = Opreations.UpdateOpreation,
				AdminId = adminId,
				Description = $"Updated WareHouse: {resultwarehouse.Data.Id}",
				Timestamp = DateTime.UtcNow
			};

			ResultDto<bool> logResult = await _unitOfWork.Repository<AdminOperationsLog>().CreateAsync(adminOperations);
			if (!logResult.Success)
			{
				await transaction.RollbackAsync();
				return StatusCode(500, new ResponseDto { StatusCode = 500, Message = logResult.Message });
			}
			await transaction.CommitAsync();
			_logger.LogInformation("WareHouse Updated");
			return Ok(new ResponseDto { StatusCode = 200, Message = "WareHouse Updated Successfully" });
		}
		[HttpDelete]
		[ResponseCache(Duration = 120, VaryByQueryKeys = new string[] { "id" })]
		public async Task<ActionResult<ResponseDto>> DeleteWareHouseAsync(int id)
		{
			_logger.LogInformation($"Executing {nameof(DeleteWareHouseAsync)}");

			using var transaction = await _unitOfWork.BeginTransactionAsync();

			try
			{
				ResultDto<Warehouse> resultwarehouse = await _unitOfWork.Repository<Warehouse>().GetByIdAsync(id, x => x.Include(c => c.ProductInventories));
				if (!resultwarehouse.Success || resultwarehouse.Data is null || resultwarehouse.Data.DeletedAt.HasValue)
				{
					_logger.LogWarning($"No warehouse with this id: {id}");
					return BadRequest(new ResponseDto { StatusCode = 400, Message = $"No warehouse with this id: {id}" });
				}

				string? adminId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
				if (string.IsNullOrEmpty(adminId))
				{
					_logger.LogError("Admin ID not found, canceling delete operation.");
					return Unauthorized(new ResponseDto { StatusCode = 401, Message = "Invalid Admin ID." });
				}
				if (resultwarehouse.Data.ProductInventories.Count != 0)
				{
					_logger.LogError("Can't delete warehouse Contain Products.");
					return StatusCode(400, new ResponseDto { StatusCode = 400, Message = "Can't delete warehouse Contain Products." });
				}
				resultwarehouse.Data.DeletedAt = DateTime.UtcNow;

				ResultDto<bool> result = await _unitOfWork.Repository<Warehouse>().UpdateAsync(resultwarehouse.Data);
				if (!result.Success)
				{
					_logger.LogError(result.Message);
					return StatusCode(500, new ResponseDto { StatusCode = 500, Message = result.Message });
				}

				if (await _unitOfWork.CommitAsync() == 0)
				{
					_logger.LogError("Can't delete warehouse.");
					return StatusCode(500, new ResponseDto { StatusCode = 500, Message = "Can't delete warehouse." });
				}

				_logger.LogInformation($"warehouse Deleted successfully, ID: {resultwarehouse.Data.Id}");

				AdminOperationsLog adminOperations = new()
				{
					OperationType = Opreations.DeleteOpreation,
					AdminId = adminId,
					Description = $"Deleted warehouse: {resultwarehouse.Data.Id}",
					Timestamp = DateTime.UtcNow
				};

				ResultDto<bool> logResult = await _unitOfWork.Repository<AdminOperationsLog>().CreateAsync(adminOperations);
				if (!logResult.Success)
				{
					await transaction.RollbackAsync();
					return StatusCode(500, new ResponseDto { StatusCode = 500, Message = logResult.Message });
				}

				await _unitOfWork.CommitAsync();
				await transaction.CommitAsync();

				return Ok(new ResponseDto { Message = $"Deleted successfully, ID: {resultwarehouse.Data.Id}", StatusCode = 200 });
			}
			catch (Exception ex)
			{
				_logger.LogError($"Transaction failed: {ex.Message}");
				await transaction.RollbackAsync();
				return StatusCode(500, new ResponseDto { StatusCode = 500, Message = "An error occurred while deleting the warehouse." });
			}
		}


		[HttpPatch("Return-Deleted-WareHouse/{id}")]
		public async Task<ActionResult<ResponseDto>> ReturnRemovedWareHouseAsync(int id)
		{
			_logger.LogInformation($"Executing {nameof(ReturnRemovedWareHouseAsync)}");

			string? userid = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
			if (userid is null)
			{
				_logger.LogError("Invalid token or user not authenticated");
				return Unauthorized(new ResponseDto { StatusCode = 401, Message = "Invalid token or user not authenticated" });
			}

			ResultDto<Warehouse> resultwarehouse = await _unitOfWork.WareHouse.GetByIdAsync(id);
			if (!resultwarehouse.Success)
			{
				_logger.LogWarning($"WareHouse not found with ID: {id}");
				return BadRequest(new ResponseDto { StatusCode = 400, Message = $"Category not found with ID: {id}" });
			}

			using var tran = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled);

			resultwarehouse.Data.DeletedAt = null;
			ResultDto<bool> updateResult = await _unitOfWork.WareHouse.UpdateAsync(resultwarehouse.Data);
			if (!updateResult.Success)
			{
				_logger.LogError(updateResult.Message);
				return StatusCode(500, new ResponseDto { StatusCode = 500, Message = updateResult.Message });
			}

			ResultDto<bool> logResult = await _unitOfWork.Repository<AdminOperationsLog>().CreateAsync(new AdminOperationsLog
			{
				AdminId = userid,
				ItemId = resultwarehouse.Data.Id,
				OperationType = Opreations.UndoDeleteOpreation,
				Description = $"Undo Delete of Warehouse: {resultwarehouse.Data.Id}"
			});

			if (!logResult.Success)
			{
				_logger.LogError(logResult.Message);
				return StatusCode(500, new ResponseDto { StatusCode = 500, Message = logResult.Message });
			}

			int saveResult = await _unitOfWork.CommitAsync();
			if (saveResult == 0)
			{
				_logger.LogError("Database update failed, no changes were committed.");
				return StatusCode(500, new ResponseDto { StatusCode = 500, Message = "Database update failed, no changes were committed." });
			}

			tran.Complete();
			return Ok(new ResponseDto { StatusCode = 200, Message = $"warehouse restored: {resultwarehouse.Data.Id}" });
		}
		[HttpGet("{id}/Producets")]
		public async Task<ActionResult<ResponseDto>> GetProductsByWareHouseId([FromRoute] int id)
		{
			_logger.LogInformation($"Execute {nameof(GetProductsByWareHouseId)} in WareHouseController");
			 ResultDto<Warehouse> result= await _unitOfWork.WareHouse.GetByIdAsync(id, w => w.Include(we => we.ProductInventories).ThenInclude(p => p.Product).ThenInclude(c => c.Category));
			if(!result.Success)
			{
				_logger.LogError(result.Message);
				return BadRequest(new ResponseDto { StatusCode = 400, Message = result.Message });

			}
			List<ProductDto> products = result.Data.ProductInventories.Select(pr => pr.Product).Select(p => new ProductDto
			{
				AvailabeQuantity = p.Quantity,
				Category = new CategoryDto(p.Category.Id, p.Category.Name, p.Category.Description, p.Category.CreatedAt),
				CreatedAt = p.CreatedAt,
				Description = p.Description, Name = p.Name, Id = p.Id,
				Discount = p.Discount == null ? null : new DiscountDto(p.Discount.Id, p.Discount.Name, p.Discount.DiscountPercent, p.Discount.Description, p.Discount.IsActive),
				FinalPrice = p.Discount == null || !p.Discount.IsActive ? p.Price : p.Price - p.Discount.DiscountPercent * p.Price,
				ModifiedAt = p.ModifiedAt,

			}
			).ToList();
			if (products.Count == 0){
				_logger.LogWarning("No Products Found");
				return Ok(new ResponseDto { StatusCode = 200, Message = "No Products Found" });
}
			_logger.LogInformation("Return Products");
			return Ok(new ResponseDto { StatusCode = 200, Message = result.Message ,Data=products });
		}
		[HttpPatch("Transfere-All_Products")]
		public async Task<ActionResult<ResponseDto>> TransfereAllProducts([FromRoute] int CurrentWarehouse, int newwarehouse)
		{
			_logger.LogInformation($"Executing {nameof(TransfereAllProducts)} in WareHouseController");
			string? userid = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
			if (string.IsNullOrEmpty(userid))
			{
				_logger.LogError("Admin ID not found, canceling transfer operation.");
				return Unauthorized(new ResponseDto { StatusCode = 401, Message = "Invalid Admin ID." });
			}

			ResultDto<Warehouse> from = await _unitOfWork.WareHouse.GetByIdAsync(CurrentWarehouse, x => x.Include(w => w.ProductInventories));
			if (!from.Success || from.Data.ProductInventories.Count == 0)
			{
				_logger.LogWarning("Source warehouse not found or empty.");
				return BadRequest(new ResponseDto { StatusCode = 400, Message = "Source warehouse is empty or doesn't exist." });
			}

			ResultDto<Warehouse> to = await _unitOfWork.WareHouse.GetByIdAsync(newwarehouse);
			if (!to.Success)
			{
				_logger.LogWarning("Target warehouse not found.");
				return BadRequest(new ResponseDto { StatusCode = 400, Message = "Target warehouse doesn't exist." });
			}

			using var transaction = await _unitOfWork.BeginTransactionAsync();
			try
			{
				
				var transferredProducts = from.Data.ProductInventories
					.Select(item => new ProductInventory
					{
						ProductId = item.ProductId,
						WarehouseId = to.Data.Id,  
						Quantity = item.Quantity,
						CreatedAt = DateTime.UtcNow,
						ModifiedAt = DateTime.UtcNow
					}).ToList();

				foreach (var item in from.Data.ProductInventories)
				{
					to.Data.ProductInventories.Add(item);
					from.Data.ProductInventories.Remove(item);

				}
				ResultDto<bool> updatefrom = await _unitOfWork.WareHouse.UpdateAsync(from.Data); 
				ResultDto<bool> updateto = await _unitOfWork.WareHouse.UpdateAsync(from.Data); 

				int changes = await _unitOfWork.CommitAsync();
				if (changes == 0)
				{
					_logger.LogWarning("Nothing transferred.");
					return BadRequest(new ResponseDto { Message = "Transfer failed.", StatusCode = 400 });
				}

				_logger.LogInformation("Warehouse transfer successful.");

				AdminOperationsLog adminOperations = new()
				{
					AdminId = userid,
					Description = $"Transferred all products from Warehouse ID: {from.Data.Id} to Warehouse ID: {to.Data.Id}",
					Timestamp = DateTime.UtcNow
				};

	
				await _unitOfWork.Repository<AdminOperationsLog>().CreateAsync(adminOperations);
				await _unitOfWork.CommitAsync();

				await transaction.CommitAsync();

				return Ok(new ResponseDto { Message = $"Transferred all products from Warehouse ID: {from.Data.Id} to Warehouse ID: {to.Data.Id}", StatusCode = 200 });
			}
			catch (Exception ex)
			{
				_logger.LogError($"Exception: {ex.Message}");
				await transaction.RollbackAsync();
				return StatusCode(500, new ResponseDto { Message = "An error occurred while processing the transfer.", StatusCode = 500 });
			}
		}




	}
}
