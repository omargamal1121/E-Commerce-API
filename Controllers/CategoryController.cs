using AutoMapper;
using E_Commers.DtoModels.AccountDtos;
using E_Commers.DtoModels.CategoryDtos;
using E_Commers.Enums;
using E_Commers.Helper;
using E_Commers.Models;
using E_Commers.UOW;
using Microsoft.AspNetCore.Authorization;

using Microsoft.AspNetCore.Mvc;

using System.Security.Claims;
using System.Transactions;

namespace E_Commers.Controllers
{
	[Route("api/[controller]")]
	[ApiController]
	[Authorize(Roles = "Admin")]
	public class CategoryController : ControllerBase
	{
		private readonly IUnitOfWork _unitOfWork;
		private readonly ILogger<CategoryController> _logger;

		public CategoryController(IUnitOfWork unitOfWork, ILogger<CategoryController> logger)
		{
			_unitOfWork = unitOfWork;
			_logger = logger;

		}
		[HttpGet("{id}")]
	
		public async Task<ActionResult<ResponseDto>> GetCategory([FromRoute]int id)
		{
			_logger.LogInformation($"Executing {nameof(GetCategory)} in CategoryController");


			ResultDto<Category> resultcategory = await _unitOfWork.Category.GetByIdAsync(id);
			if (!resultcategory.Success)
			{
				_logger.LogWarning(resultcategory.Message);
				return Ok(new ResponseDto { Message = $"category.Message", StatusCode = 200 });
			}
			CategoryDto categoryDto = new CategoryDto(resultcategory.Data.Id, resultcategory.Data.Name, resultcategory.Data.Description, resultcategory.Data.CreatedAt, resultcategory.Data.DeletedAt, resultcategory.Data.DeletedAt);



			return Ok(new ResponseDto { Message = categoryDto, StatusCode = 200 });
		}

		[HttpGet("GetAll")]
		[ResponseCache(Duration =120,VaryByQueryKeys = new string [] { "includeDates" })]
		public async Task<ActionResult<ResponseDto>> GetAll([FromQuery] bool includeDates = false)
		{
			_logger.LogInformation($"Executing {nameof(GetAll)} in CategoryController");


			var Resultcategories = await _unitOfWork.Category.GetAllAsync();
			if (!Resultcategories.Data.Any())
			{
				_logger.LogWarning("No categories found");
				return Ok(new ResponseDto { Message = "No categories found", StatusCode = 200 });
			}

			List<CategoryDto> categoryDtos = Resultcategories.Data.Select(c =>
			new CategoryDto
			{
				CreatedAt = includeDates ? c.CreatedAt : null,
				ModifiedAt = includeDates ? c.ModifiedAt : null,
				Description = c.Description,
				Id = c.Id,
				Name = c.Name
			}
			).ToList();
			
		

			return Ok(new ResponseDto { Message = categoryDtos, StatusCode = 200 });
		}

		[HttpPost]
		public async Task<ActionResult> CreateCategoryAsnc(CreateCategotyDto model)
		{
			_logger.LogInformation($"Executing {nameof(CreateCategoryAsnc)}");

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

			if (await _unitOfWork.Category.GetByNameAsync(model.Name) != null) 
			{
				return BadRequest(new ResponseDto
				{
					StatusCode = 400,
					Message = "A category with this name already exists."
				});
			}

			using var transaction = await _unitOfWork.BeginTransactionAsync();
			try
			{
				Category category = new Category {Description=model.Description,Name=model.Name};
				ResultDto<bool> result = await _unitOfWork.Category.CreateAsync(category);

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

				_logger.LogInformation($"Category added successfully, ID: {category.Id}");

				AdminOperationsLog adminOperations = new()
				{
					AdminId = userid,
					Description = $"Added category: {category.Id}",
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

				return Ok(new ResponseDto { Message = $"Added successfully, ID: {category.Id}", StatusCode = 200 });
			}
			catch (Exception ex)
			{
				_logger.LogError($"Exception: {ex.Message}");
				await transaction.RollbackAsync();
				return StatusCode(500, new ResponseDto { Message = "An error occurred while saving data.", StatusCode = 500 });
			}
		}

		[HttpDelete]
		[ResponseCache(Duration =120,VaryByQueryKeys =new string[] {"id"})]
		public async Task<ActionResult<ResponseDto>> DeleteCategoryAsync(int id)
		{
			_logger.LogInformation($"Executing {nameof(DeleteCategoryAsync)}");

			using var transaction = await _unitOfWork.BeginTransactionAsync();

			try
			{
				ResultDto<Category> resultcategory = await _unitOfWork.Category.GetByIdAsync(id);
				if (resultcategory.Data is null|| resultcategory.Data.DeletedAt.HasValue)
				{
					_logger.LogWarning($"No Category with this id: {id}");
					return BadRequest(new ResponseDto { StatusCode = 400, Message = $"No Category with this id: {id}" });
				}

				string? adminId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
				if (string.IsNullOrEmpty(adminId))
				{
					_logger.LogError("Admin ID not found, canceling delete operation.");
					return Unauthorized(new ResponseDto { StatusCode = 401, Message = "Invalid Admin ID." });
				}
				resultcategory.Data.DeletedAt = DateTime.UtcNow;

				ResultDto<bool> result = await _unitOfWork.Category.UpdateAsync(resultcategory.Data);
				if (!result.Success)
				{
					_logger.LogError(result.Message);
					return StatusCode(500, new ResponseDto { StatusCode = 500, Message = result.Message });
				}

				if (await _unitOfWork.CommitAsync() == 0)
				{
					_logger.LogError("Can't delete category.");
					return StatusCode(500, new ResponseDto { StatusCode = 500, Message = "Can't delete category." });
				}

				_logger.LogInformation($"Category Deleted successfully, ID: {resultcategory.Data.Id}");

				AdminOperationsLog adminOperations = new()
				{
					OperationType = Opreations.DeleteOpreation,
					AdminId = adminId,
					Description = $"Deleted category: {resultcategory.Data.Id}",
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

				return Ok(new ResponseDto { Message = $"Deleted successfully, ID: {resultcategory.Data.Id}", StatusCode = 200 });
			}
			catch (Exception ex)
			{
				_logger.LogError($"Transaction failed: {ex.Message}");
				await transaction.RollbackAsync();
				return StatusCode(500, new ResponseDto { StatusCode = 500, Message = "An error occurred while deleting the category." });
			}
		}
		[HttpGet("deleted-categories")]
		public async Task<ActionResult<ResponseDto>> GetDeletedCategoriesAsync()
		{
			_logger.LogInformation($"Executing {nameof(GetDeletedCategoriesAsync)}");

			var resultlist = await _unitOfWork.Category.GetAllDeletedAsync();

			if (!resultlist.Data.Any())
			{
				_logger.LogInformation("No Deleted Categories found");
				return Ok(new ResponseDto { StatusCode = 200, Message = "No Deleted Categories found" });
			}

			var categoryDtos = resultlist.Data.Select(c => new CategoryDto
			{
				Name = c.Name,
				CreatedAt = c.CreatedAt,
				DeletedAt = c.DeletedAt,
				Description = c.Description,
				Id = c.Id,
				ModifiedAt = c.ModifiedAt
			}).ToList();

			_logger.LogInformation($"Deleted Categories found: {categoryDtos.Count()}");
			return Ok(new ResponseDto { StatusCode = 200, Message = categoryDtos });
		}
		[HttpPatch("return-Deleted-category")]
		public async Task<ActionResult<ResponseDto>> ReturnRemovedCategoryAsync(int id)
		{
			_logger.LogInformation($"Executing {nameof(ReturnRemovedCategoryAsync)}");

			string? userid = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
			if (userid is null)
			{
				_logger.LogError("Invalid token or user not authenticated");
				return Unauthorized(new ResponseDto { StatusCode = 401, Message = "Invalid token or user not authenticated" });
			}

			ResultDto<Category> resultcategory = await _unitOfWork.Category.GetByIdAsync(id);
			if (resultcategory.Success)
			{
				_logger.LogWarning($"Category not found with ID: {id}");
				return BadRequest(new ResponseDto { StatusCode = 400, Message = $"Category not found with ID: {id}" });
			}

			if (!resultcategory.Data.DeletedAt.HasValue)
			{
				_logger.LogWarning($"Category with ID {id} is not deleted.");
				return BadRequest(new ResponseDto { StatusCode = 400, Message = $"Category with ID {id} is not deleted." });
			}

			using var tran = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled);

			resultcategory.Data.DeletedAt = null;
			ResultDto<bool> updateResult = await _unitOfWork.Category.UpdateAsync(resultcategory.Data);
			if (!updateResult.Success)
			{
				_logger.LogError(updateResult.Message);
				return StatusCode(500, new ResponseDto { StatusCode = 500, Message = updateResult.Message });
			}

			ResultDto<bool> logResult = await _unitOfWork.Repository<AdminOperationsLog>().CreateAsync(new AdminOperationsLog
			{
				AdminId = userid,
				ItemId = resultcategory.Data.Id,
				OperationType = Opreations.UndoDeleteOpreation,
				Description = $"Undo Delete of Category: {resultcategory.Data.Id}"
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
			return Ok(new ResponseDto { StatusCode = 200, Message = $"Category restored: {resultcategory.Data.Id}" });
		}

		[HttpPatch("{id}")]
		public async Task<ActionResult<ResponseDto>> UpdateCategory(
		[FromRoute]	int id,
			[FromBody] CategoryUpdateDto updateDto)
		{
			_logger.LogInformation($"Executing {nameof(UpdateCategory)}");

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

			ResultDto<Category>? resultcategory = await _unitOfWork.Category.GetByIdAsync(id);
			if (!resultcategory.Success)
			{
				_logger.LogWarning($"No Category With this ID: {id}");
				return BadRequest(new ResponseDto { StatusCode = 400, Message = $"No Category With this ID: {id}" });
			} 
			if (!string.IsNullOrWhiteSpace(updateDto.NewName))
			{
				if (resultcategory.Data.Name.Equals(updateDto.NewName, StringComparison.OrdinalIgnoreCase))
				{
					_logger.LogWarning($"Same Name ID: {id}");
					return BadRequest(new ResponseDto { StatusCode = 400, Message = $"Can't Use Same Name" });
				}
				if(updateDto.NewName.Length>20||updateDto.NewName.Length<5)
				{

					_logger.LogWarning($"Invalid Name ID: {id}");
					return BadRequest(new ResponseDto { StatusCode = 400, Message = $"Name Must be from 5 charc to 20" });
				}
				resultcategory.Data.Name = updateDto.NewName;
			}

			if (!string.IsNullOrWhiteSpace(updateDto.Description))
			{
				if (updateDto.Description.Length > 50 || updateDto.Description.Length < 10)
				{

					_logger.LogWarning($"Invalid Description ID: {id}");
					return BadRequest(new ResponseDto { StatusCode = 400, Message = $"Description Must be from 10 charc to 50" });
				}
				resultcategory.Data.Description = updateDto.Description;
			}
			using var transaction = await _unitOfWork.BeginTransactionAsync();

			ResultDto<bool> result = await _unitOfWork.Category.UpdateAsync(resultcategory.Data);
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
				Description = $"Updated category: {resultcategory.Data.Id}",
				Timestamp = DateTime.UtcNow
			};

			ResultDto<bool> logResult = await _unitOfWork.Repository<AdminOperationsLog>().CreateAsync(adminOperations);
			if (!logResult.Success)
			{
				await transaction.RollbackAsync();
				return StatusCode(500, new ResponseDto { StatusCode = 500, Message = logResult.Message });
			}
			await transaction.CommitAsync();
			_logger.LogInformation("Category Updated");
			return Ok(new ResponseDto { StatusCode = 200, Message = "Category Updated Successfully" });
		}
		[HttpGet("Produects/{id}")]
		public async Task<ActionResult<ResponseDto>> GetProduectsByCategoryId(int id)
		{
			_logger.LogInformation($"Execute:{nameof(GetProduectsByCategoryId)} with Id:{id}");
			ResultDto< List<Product>>resultproducts= await _unitOfWork.Category.GetProductsByCategoryIdAsync(id);
			if(!resultproducts.Success)
			{

				return BadRequest(new ResponseDto { StatusCode = 400 , Message=resultproducts.Message});
			}

			if (!resultproducts.Data.Any())
				return Ok(new ResponseDto { StatusCode = 200, Message = resultproducts.Message });

			return Ok(new ResponseDto { StatusCode = 200, Message = resultproducts.Data });



		}
	}
}
