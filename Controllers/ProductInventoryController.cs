using E_Commers.DtoModels;
using E_Commers.DtoModels.CategoryDtos;
using E_Commers.DtoModels.DiscoutDtos;
using E_Commers.DtoModels.InventoryDtos;
using E_Commers.DtoModels.ProductDtos;
using E_Commers.DtoModels.WareHouseDtos;
using E_Commers.Enums;
using E_Commers.Services;
using E_Commers.Models;
using E_Commers.UOW;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Transactions;
using System.IdentityModel.Tokens.Jwt;

namespace E_Commers.Controllers
{
	[Route("api/[controller]")]
	[ApiController]
	[Authorize(Roles ="Admin")]
	public class ProductInventoryController : ControllerBase
	{
		private readonly IUnitOfWork _unitOfWork;
		private readonly ILogger<ProductInventoryController> _logger;
		public ProductInventoryController(IUnitOfWork unitOfWork, ILogger<ProductInventoryController> logger)
		{
			_logger = logger;
			_unitOfWork = unitOfWork;
		}

		[HttpGet("GetAll")]
		[ResponseCache(Duration = 120, VaryByQueryKeys = new string[] { "includeDates" })]
		public async Task<ActionResult<ResponseDto>> GetAll([FromQuery] bool includeDates = false)
		{
			_logger.LogInformation($"Executing {nameof(GetAll)} in ProductInventoryController");


			var Resultcategories = await _unitOfWork.Repository<ProductInventory>().GetAllAsync(filter: c => c.DeletedAt == null,include:i=>i.Include(i=>i.Product).ThenInclude(p=>p.Category));
			if (!Resultcategories.Data.Any())
			{
				return Ok(new ResponseDto { Message = "No Inventory found", });
			}

			List<InventoryDto> inventorydtos = Resultcategories.Data.Select(c =>
			new InventoryDto
			{
				CreatedAt = includeDates ? c.CreatedAt : null,
				ModifiedAt = includeDates ? c.ModifiedAt : null,
				Id = c.Id,
				Quantityinsidewarehouse=c.Quantity,
				WareHousid=c.WarehouseId,
				Product=new ProductDto {
					Id = c.Product.Id,
					Name = c.Product.Name,
					AvailabeQuantity = c.Product.Quantity,
					Description = c.Product.Description,
				//	Discount = c.Product.Discount == null ? null : new DiscountDto(c.Product.Discount.Id, c.Product.Discount.Name, c.Product.Discount.DiscountPercent, c.Product.Discount.Description, c.Product.Discount.IsActive),
					FinalPrice = c.Product.Discount == null || !c.Product.Discount.IsActive ? c.Product.Price : c.Product.Price - c.Product.Discount.DiscountPercent * c.Product.Price,
					//Category = new CategoryDto(c.Product.Category.Id, c.Product.Category.Name, c.Product.Category.Description, c.Product.Category.CreatedAt),
					CreatedAt = c.Product.CreatedAt,
				}
			}
			).ToList();



			return Ok(new ResponseDto { Message = Resultcategories.Message , Data= inventorydtos, });
		}

		[HttpPost]
		public async Task<ActionResult<ResponseDto>> AddProductToWarehouse(CreateInvetoryDto productDto)
		{
			_logger.LogInformation($"Execute {nameof(AddProductToWarehouse)}");
			if(!ModelState.IsValid)
			{
				var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
				_logger.LogError($"Validation Errors: {string.Join(", ", errors)}");

				return BadRequest(new ResponseDto
				{
					
					Message = "Invalid data: " + string.Join(", ", errors)
				});
			}
			var checkfrominventory=  await _unitOfWork.Product.GetByIdAsync(productDto.ProductId);
			if(!checkfrominventory.Success||checkfrominventory==null)
				return NotFound(new ResponseDto {Message= checkfrominventory.Message});

			if(checkfrominventory.Data.Quantity<productDto.Quantity){
				_logger.LogError($"Must Quantity Of Product U Want To Add it <= {checkfrominventory.Data.Quantity} ");
				return BadRequest(new ResponseDto {  Message = $"Must Quantity Of Product U Want To Add it <= {checkfrominventory.Data.Quantity} " });
			}
			var checkwarehouse = await _unitOfWork.WareHouse.GetByIdAsync(productDto.WareHouseId);
			if(!checkwarehouse.Success|| checkwarehouse.Data==null)
				return NotFound(new ResponseDto { Message= checkwarehouse.Message});

			 var isexsist=  await _unitOfWork.Repository<ProductInventory>().GetByQuery(i=>i.ProductId==productDto.ProductId&&i.WarehouseId==productDto.WareHouseId);
			if(isexsist.Success)
			{
				_logger.LogError("Inventory is already exsist");
				return Conflict(new ResponseDto {Message = "Inventory is already exsist"});
			}

		    var inventory=new ProductInventory {ProductId=productDto.ProductId,WarehouseId=productDto.WareHouseId,Quantity=productDto.Quantity};

				var transaction = await _unitOfWork.BeginTransactionAsync();
			try
			{
			    string userid=	User.FindFirst(ClaimTypes.NameIdentifier).Value;
				var iscreated=await _unitOfWork.Repository<ProductInventory>().CreateAsync(inventory);
				if(!iscreated.Success)
				return BadRequest(new ResponseDto { Message= iscreated.Message});

				int changes = await _unitOfWork.CommitAsync();
				if (changes == 0)
				{
					return BadRequest(new ResponseDto { Message = "Nothing added"});
				}

				_logger.LogInformation($"inventory added successfully, ID: {inventory.Id}");

				AdminOperationsLog adminOperations = new()
				{
					AdminId = userid,
					Description = $"Added inventory: {inventory.Id}",
					Timestamp = DateTime.UtcNow
				};

				Result<AdminOperationsLog> logResult = await _unitOfWork.Repository<AdminOperationsLog>().CreateAsync(adminOperations);
				if (!logResult.Success)
				{
					await transaction.RollbackAsync();
					return StatusCode(500, new ResponseDto { Message = logResult.Message });
				}

				await _unitOfWork.CommitAsync();
				await transaction.CommitAsync();

				return CreatedAtAction(nameof(AddProductToWarehouse), new { id = inventory.Id }, new ResponseDto { Message = $"Added successfully, ID: {inventory.Id}", });
			}

			catch (Exception ex)
			{
				_logger.LogError($"Exception: {ex.Message}");
				await transaction.RollbackAsync();
				return StatusCode(500, new ResponseDto { Message = "An error occurred while saving data." });
			}

		}
		[HttpPatch("IncreaseQuantity")]
		public async Task<ActionResult<ResponseDto>> IncreaseQuantityofProductToWarehouse(AddQuantityInvetoryDto productDto)
		{
			_logger.LogInformation($"Execute {nameof(IncreaseQuantityofProductToWarehouse)}");
			if (!ModelState.IsValid)
			{
				var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
				_logger.LogError($"Validation Errors: {string.Join(", ", errors)}");

				return BadRequest(new ResponseDto
				{
					Message = "Invalid data: " + string.Join(", ", errors)
				});
			}
			var checkfrominventory = await _unitOfWork.Repository<ProductInventory>().GetByIdAsync(productDto.Id);
			if (!checkfrominventory.Success || checkfrominventory.Data == null)
				return NotFound(new ResponseDto {  Message = checkfrominventory.Message });



			var transaction = await _unitOfWork.BeginTransactionAsync();
			try
			{
				checkfrominventory.Data.Quantity += productDto.Quantity;
				checkfrominventory.Data.Product.Quantity += productDto.Quantity;
				var productUpdate = await _unitOfWork.Product.UpdateAsync(checkfrominventory.Data.Product);
				var frominventoryUpdate = await _unitOfWork.Repository<ProductInventory>().UpdateAsync(checkfrominventory.Data);

				if (!productUpdate.Success || !frominventoryUpdate.Success)
				{
					await transaction.RollbackAsync();
					return StatusCode(500, new ResponseDto
					{
						
						Message = productUpdate.Success ? frominventoryUpdate.Message : productUpdate.Message
					});
				}
				string userid = User.FindFirst(ClaimTypes.NameIdentifier).Value;
				AdminOperationsLog adminOperations = new()
				{
					AdminId = userid,
					Description = $"Add Quantity:{productDto.Quantity} To Product {checkfrominventory.Data.Product.Id}",
					Timestamp = DateTime.UtcNow,
					ItemId = checkfrominventory.Data.Product.Id,
					OperationType = Enums.Opreations.UpdateOpreation
				};

				var logResult = await _unitOfWork.Repository<AdminOperationsLog>().CreateAsync(adminOperations);
				if (!logResult.Success)
				{
					await transaction.RollbackAsync();
					return StatusCode(500, new ResponseDto { Message = logResult.Message });
				}

				// Single commit point
				await _unitOfWork.CommitAsync();
				await transaction.CommitAsync();

				return Ok(new ResponseDto
				{
					Message = $"Updated successfully, ID: {productDto.Id}",
					
				});
			}

			catch (Exception ex)
			{
				_logger.LogError($"Exception: {ex.Message}");
				await transaction.RollbackAsync();
				return StatusCode(500, new ResponseDto { Message = "An error occurred while saving data.",  });
			}

		}
		[HttpGet("WareHouse{warehouseId}")]
		public async Task<ActionResult<ResponseDto>> GetInventoryByWarehouse(int warehouseId)
		{
			_logger.LogInformation($"Execute {nameof(GetInventoryByWarehouse)} ");
			 var isfound=  await _unitOfWork.WareHouse.GetByIdAsync(warehouseId);
			if (!isfound.Success || isfound.Data == null || isfound.Data.ProductInventories.Count==0)
				return NotFound(new ResponseDto {Message=isfound.Message});


			List<InventoryDto> invantoriesDtos = isfound.Data.ProductInventories.Select(c =>
			new InventoryDto
			{
				Id = c.Id,
				CreatedAt =  c.CreatedAt ,
				ModifiedAt =  c.ModifiedAt ,
				Quantityinsidewarehouse = c.Quantity,
				WareHousid = c.WarehouseId,
				Product = new ProductDto
				{
					Id = c.Product.Id,
					Name = c.Product.Name,
					AvailabeQuantity = c.Product.Quantity,
					Description = c.Product.Description,
					Discount = c.Product.Discount == null ? null : new DiscountDto(c.Product.Discount.Id, c.Product.Discount.Name, c.Product.Discount.DiscountPercent, c.Product.Discount.Description, c.Product.Discount.IsActive),
					FinalPrice = c.Product.Discount == null || !c.Product.Discount.IsActive ? c.Product.Price : c.Product.Price - c.Product.Discount.DiscountPercent * c.Product.Price,
					Category = new CategoryDto(c.Product.Category.Id, c.Product.Category.Name, c.Product.Category.Description, c.Product.Category.CreatedAt),
					CreatedAt = c.Product.CreatedAt,
				}
			}
			).ToList();
			return Ok(new ResponseDto { Message = isfound.Message, Data = invantoriesDtos });
		}

		[HttpPatch("TransferQuantity")]
		public async Task<ActionResult<ResponseDto>> TransferQuantityOfProductToWarehouse(TransfereQuantityInvetoryDto productDto)
		{
			const string methodName = nameof(TransferQuantityOfProductToWarehouse);
			_logger.LogInformation($"Executing {methodName}");

			// Validation
			if (!ModelState.IsValid)
			{
				var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
				_logger.LogError($"Validation Errors: {string.Join(", ", errors)}");
				return BadRequest(new ResponseDto( "Invalid data: " + string.Join(", ", errors)));
			}

			// Check source inventory
			var sourceInventory = await _unitOfWork.Repository<ProductInventory>()
				.GetByIdAsync(productDto.FromInventoryId);

			if (!sourceInventory.Success || sourceInventory.Data == null)
			{
				return NotFound(new ResponseDto( sourceInventory.Message));
			}

			// Check target inventory
			var targetInventory = await _unitOfWork.Repository<ProductInventory>()
				.GetByIdAsync(productDto.ToInventoryId);

			if (!targetInventory.Success || targetInventory.Data == null)
			{
				return NotFound(new ResponseDto( targetInventory.Message));
			}

			// Business validations
			if (sourceInventory.Data.ProductId != productDto.ProductId)
			{
				_logger.LogError("Product ID doesn't match source inventory");
				return BadRequest(new ResponseDto( "Product ID doesn't match source inventory"));
			}

			if (targetInventory.Data.ProductId != productDto.ProductId)
			{
				_logger.LogError("Product ID doesn't match target inventory");
				return BadRequest(new ResponseDto( "Product ID doesn't match target inventory"));
			}

			if (sourceInventory.Data.Id == targetInventory.Data.Id)
			{
				_logger.LogError("Cannot transfer between the same inventory");
				return BadRequest(new ResponseDto( "Cannot transfer between the same inventory"));
			}

			if (sourceInventory.Data.WarehouseId == targetInventory.Data.WarehouseId)
			{
				_logger.LogError("Cannot transfer within the same warehouse");
				return BadRequest(new ResponseDto( "Cannot transfer within the same warehouse"));
			}

			if (sourceInventory.Data.Quantity < productDto.Quantity)
			{
				_logger.LogError("Insufficient quantity in source inventory");
				return BadRequest(new ResponseDto( "Insufficient quantity in source inventory"));
			}

			// Begin transaction
			var transaction = await _unitOfWork.BeginTransactionAsync();
			try
			{
				// Update quantities
				sourceInventory.Data.Quantity -= productDto.Quantity;
				targetInventory.Data.Quantity += productDto.Quantity;

				// Save changes
				var sourceUpdate = await _unitOfWork.Repository<ProductInventory>().UpdateAsync(sourceInventory.Data);
				var targetUpdate = await _unitOfWork.Repository<ProductInventory>().UpdateAsync(targetInventory.Data);

				if (!sourceUpdate.Success || !targetUpdate.Success)
				{
					await transaction.RollbackAsync();
					var errorMessage = !sourceUpdate.Success ? sourceUpdate.Message : targetUpdate.Message;
					_logger.LogError($"Update failed: {errorMessage}");
					return StatusCode(500, new ResponseDto( errorMessage));
				}

				// Log the operation
				var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
				var adminLog = new AdminOperationsLog
				{
					AdminId = userId,
					Description = $"Transferred {productDto.Quantity} units from inventory {sourceInventory.Data.Id} to {targetInventory.Data.Id} for product {productDto.ProductId}",
					Timestamp = DateTime.UtcNow,
					ItemId = sourceInventory.Data.Product.Id,
					OperationType = Enums.Opreations.UpdateOpreation
				};

				var logResult = await _unitOfWork.Repository<AdminOperationsLog>().CreateAsync(adminLog);
				if (!logResult.Success)
				{
					await transaction.RollbackAsync();
					_logger.LogError($"Log creation failed: {logResult.Message}");
					return StatusCode(500, new ResponseDto( logResult.Message));
				}

				// Commit transaction
				await _unitOfWork.CommitAsync();
				await transaction.CommitAsync();

				_logger.LogInformation($"Successfully transferred {productDto.Quantity} units of product {productDto.ProductId}");

				return Ok(new ResponseDto(
					
					"Transfer completed successfully",
					new 
					{
						SourceInventoryId = sourceInventory.Data.Id,
						SourceNewQuantity = sourceInventory.Data.Quantity,
						TargetInventoryId = targetInventory.Data.Id,
						TargetNewQuantity = targetInventory.Data.Quantity,
						ProductId = productDto.ProductId,
						TransferredQuantity = productDto.Quantity
					}));
			}
			catch (Exception ex)
			{
				await transaction.RollbackAsync();
				_logger.LogError(ex, $"Error in {methodName}: {ex.Message}");
				return StatusCode(500, new ResponseDto( "An error occurred during transfer"));
			}
		}

		

		[HttpGet("{id}")]
		public async Task<ActionResult<ResponseDto>> GetInventory([FromRoute] int id)
		{
			_logger.LogInformation($"Executing {nameof(GetInventory)} in InventoryController");


			Result<ProductInventory> result = await _unitOfWork.Repository<ProductInventory>().GetByIdAsync(id);
			if (!result.Success||result.Data==null)
			{

				return NotFound(new ResponseDto {  Message = result.Message });
			}
			InventoryDto inventoryDto = new InventoryDto 
			{
				Id=result.Data.Id,
				CreatedAt=result.Data.CreatedAt,
				Product=new ProductDto 
				{
					Id = result.Data.Product.Id,
					Name = result.Data.Product.Name,
					AvailabeQuantity = result.Data.Product.Quantity,
					Description = result.Data.Product.Description,
					Discount = result.Data.Product.Discount == null ? null : new DiscountDto(result.Data.Product.Discount.Id, result.Data.Product.Discount.Name, result.Data.Product.Discount.DiscountPercent, result.Data.Product.Discount.Description, result.Data.Product.Discount.IsActive),
					FinalPrice = result.Data.Product.Discount == null || !result.Data.Product.Discount.IsActive ? result.Data.Product.Price : result.Data.Product.Price - result.Data.Product.Discount.DiscountPercent * result.Data.Product.Price,
					Category = new CategoryDto(result.Data.Product.Category.Id, result.Data.Product.Category.Name, result.Data.Product.Category.Description, result.Data.Product.Category.CreatedAt),
					CreatedAt = result.Data.Product.CreatedAt,
				}
			};



			return Ok(new ResponseDto { Message = result.Message, Data = result, });
		}

		[HttpDelete]
		[ResponseCache(Duration = 120, VaryByQueryKeys = new string[] { "id" })]
		public async Task<ActionResult<ResponseDto>> DeleteInventoryAsync(int id)
		{
			_logger.LogInformation($"Executing {nameof(DeleteInventoryAsync)}");

			using var transaction = await _unitOfWork.BeginTransactionAsync();

			try
			{
				Result<ProductInventory> resultinventory = await _unitOfWork.Repository<ProductInventory>().GetByIdAsync(id);
				if (!resultinventory.Success||resultinventory.Data is null )
				{
					
					return BadRequest(new ResponseDto { Message = resultinventory.Message });
				}

				string adminId = User.FindFirst(ClaimTypes.NameIdentifier).Value;

				if (resultinventory.Data.Quantity != 0)
				{
					_logger.LogError("Can't delete Inventory Contain Products.");
					return StatusCode( 400,new ResponseDto {  Message = "Can't delete Inventory Contain Products." });
				}
				resultinventory.Data.DeletedAt = DateTime.UtcNow;

				Result<ProductInventory> result = await _unitOfWork.Repository<ProductInventory>().UpdateAsync(resultinventory.Data);
				if (!result.Success)
				{
					_logger.LogError(result.Message);
					return StatusCode(500, new ResponseDto { Message = result.Message });
				}

				if (await _unitOfWork.CommitAsync() == 0)
				{
					_logger.LogError("Can't delete Inventory.");
					return StatusCode(500, new ResponseDto { Message = "Can't delete Inventory." });
				}

				_logger.LogInformation($"Inventort Deleted successfully, ID: {resultinventory.Data.Id}");

				AdminOperationsLog adminOperations = new()
				{
					OperationType = Opreations.DeleteOpreation,
					AdminId = adminId,
					Description = $"Deleted Inventory: {resultinventory.Data.Id}",
					Timestamp = DateTime.UtcNow
				};

				Result<AdminOperationsLog> logResult = await _unitOfWork.Repository<AdminOperationsLog>().CreateAsync(adminOperations);
				if (!logResult.Success)
				{
					await transaction.RollbackAsync();
					return StatusCode(500, new ResponseDto { Message = logResult.Message });
				}

				await _unitOfWork.CommitAsync();
				await transaction.CommitAsync();

				return Ok(new ResponseDto { Message = $"Deleted successfully, ID: {resultinventory.Data.Id}", });
			}
			catch (Exception ex)
			{
				_logger.LogError($"Transaction failed: {ex.Message}");
				await transaction.RollbackAsync();
				return StatusCode(500, new ResponseDto { Message = "An error occurred while deleting the Inventort." });
			}
		}


		[HttpGet("deleted-Invetories")]
		public async Task<ActionResult<ResponseDto>> GetDeletedInvetoriesAsync()
		{
			_logger.LogInformation($"Executing {nameof(GetDeletedInvetoriesAsync)}");

			var resultlist = await _unitOfWork.Repository<ProductInventory>().GetAllAsync(filter: c => c.DeletedAt.HasValue);

			if (!resultlist.Success||!resultlist.Data.Any())
			{
				return Ok(new ResponseDto { Message = resultlist.Message });
			}

			var invetoryDtos = resultlist.Data.Select(c =>
				new InventoryDto
				{
					CreatedAt =  c.CreatedAt ,
					ModifiedAt =  c.ModifiedAt ,
					Id = c.Id,
					Quantityinsidewarehouse = c.Quantity,
					WareHousid = c.WarehouseId,
					Product = new ProductDto
					{
						Id = c.Product.Id,
						Name = c.Product.Name,
						AvailabeQuantity = c.Product.Quantity,
						Description = c.Product.Description,
						FinalPrice = c.Product.Discount == null || !c.Product.Discount.IsActive ? c.Product.Price : c.Product.Price - c.Product.Discount.DiscountPercent * c.Product.Price,
						CreatedAt = c.Product.CreatedAt,
					}
				}
			).ToList();

			_logger.LogInformation($"Deleted Invetories found: {invetoryDtos.Count()}");
			return Ok(new ResponseDto { Message = resultlist.Message, Data = invetoryDtos });
		}
		[HttpPatch("return_Deleted_Invetories")]
		public async Task<ActionResult<ResponseDto>> ReturnRemovedInventoryAsync(int id)
		{
			_logger.LogInformation($"Executing {nameof(ReturnRemovedInventoryAsync)}");

			string userid = User.FindFirst(ClaimTypes.NameIdentifier).Value;


			var resultInvetory = await _unitOfWork.Repository<ProductInventory>().GetByIdAsync(id);
			if (!resultInvetory.Success)
			{
			
				return BadRequest(new ResponseDto { Message = resultInvetory.Message });
			}

			using var tran = await _unitOfWork.BeginTransactionAsync();

			resultInvetory.Data.DeletedAt = null;
			Result<ProductInventory> updateResult = await _unitOfWork.Repository<ProductInventory>().UpdateAsync(resultInvetory.Data);
			if (!updateResult.Success)
			{
				_logger.LogError(updateResult.Message);
				return StatusCode(500, new ResponseDto { Message = updateResult.Message });
			}

			Result<AdminOperationsLog> logResult = await _unitOfWork.Repository<AdminOperationsLog>().CreateAsync(new AdminOperationsLog
			{
				AdminId = userid,
				ItemId = resultInvetory.Data.Id,
				OperationType = Opreations.UndoDeleteOpreation,
				Description = $"Undo Delete of Inventory: {resultInvetory.Data.Id}"
			});

			if (!logResult.Success)
			{
				_logger.LogError(logResult.Message);
				return StatusCode(500, new ResponseDto { Message = logResult.Message });
			}

			int saveResult = await _unitOfWork.CommitAsync();
			if (saveResult == 0)
			{
				_logger.LogError("Database update failed, no changes were committed.");
				return StatusCode(500, new ResponseDto { Message = "Database update failed, no changes were committed." });
			}
			await tran.CommitAsync();
			return Ok(new ResponseDto { Message = $"Inventory restored: {resultInvetory.Data.Id}" });
		}
	}
}