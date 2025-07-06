using E_Commers.DtoModels;
using E_Commers.DtoModels.CategoryDtos;
using E_Commers.DtoModels.DiscoutDtos;
using E_Commers.DtoModels.ProductDtos;
using E_Commers.Enums;
using E_Commers.Services;
using E_Commers.Models;
using E_Commers.UOW;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.OpenApi.Models;
using System.Security.Claims;
using System.Transactions;
using System.IdentityModel.Tokens.Jwt;
using E_Commers.Services.Product;
using E_Commers.DtoModels.Responses;
using E_Commers.Interfaces;
using E_Commers.ErrorHnadling;
using E_Commers.DtoModels.InventoryDtos;

namespace E_Commers.Controllers
{
	[Route("api/[controller]")]
	[ApiController]
	[Authorize(Roles = "Admin")]
	public class ProductController : ControllerBase
	{
		private readonly IUnitOfWork _unitOfWork;
		private readonly IProductLinkBuilder _productLinkBuilder;
		private readonly IProductsServices _productsServices;
		private readonly ILogger<ProductController> _logger;
		public ProductController(IProductLinkBuilder productLinkBuilder, IUnitOfWork unitOfWork, IProductsServices productsServices, ILogger<ProductController> logger)
		{
			_productLinkBuilder = productLinkBuilder;
			_productsServices = productsServices;
			_logger = logger;
			_unitOfWork = unitOfWork;
		}

		private ActionResult<ApiResponse<T>> HandleResult<T>(Result<T> result, string actionName = null, int? id = null) where T : class
		{
			var apiResponse = result.Success
				? ApiResponse<T>.CreateSuccessResponse(result.Message, result.Data, result.StatusCode, warnings: result.Warnings)
				: ApiResponse<T>.CreateErrorResponse(result.Message, new ErrorResponse("Error", result.Message), result.StatusCode, warnings: result.Warnings);

			switch (result.StatusCode)
			{
				case 200:
					return Ok(apiResponse);
				case 201:
					return actionName != null && id.HasValue ? CreatedAtAction(actionName, new { id }, apiResponse) : StatusCode(201, apiResponse);
				case 400:
					return BadRequest(apiResponse);
				case 401:
					return Unauthorized(apiResponse);
				case 404:
					return NotFound(apiResponse);
				case 409:
					return Conflict(apiResponse);
				default:
					return StatusCode(result.StatusCode, apiResponse);
			}
		}

		[HttpGet]
		[ResponseCache(Duration = 120)]
		public async Task<ActionResult<ApiResponse<List<ProductDto>>>> GetAllProducts()
		{
			_logger.LogInformation($"Executing {nameof(GetAllProducts)}");
			var response = await _productsServices.GetAllAsync();
			return HandleResult<List<ProductDto>>(response, nameof(GetAllProducts));
		}
		[HttpGet("{id}")]
		[ResponseCache(Duration = 60, VaryByQueryKeys = new string[] { "id" })]
		public async Task<ActionResult<ApiResponse<ProductDto>>> GetProduct(int id)
		{
			_logger.LogInformation($"Executing {nameof(GetProduct)} for ID: {id}");
			var response = await _productsServices.GetProductByIdAsync(id);
			return HandleResult<ProductDto>(response, nameof(GetProduct), id);
		}

		[HttpPost]
		public async Task<ActionResult<ApiResponse<ProductDto>>> CreateProduct(CreateProductDto model)
		{
			_logger.LogInformation($"Executing {nameof(CreateProduct)}");
			if (!ModelState.IsValid)
			{
				var errors = string.Join(", ", ModelState.Values
					.SelectMany(v => v.Errors)
					.Select(e => e.ErrorMessage)
					.ToList());
				_logger.LogError($"Validation Errors: {errors}");
				return BadRequest(ApiResponse<ProductDto>.CreateErrorResponse("Check on data", new ErrorResponse("Invalid data", errors)));
			}
			var userId = HttpContext.Items["UserId"]?.ToString();
			var response = await _productsServices.CreateProductAsync(model, userId);
			return HandleResult<ProductDto>(response, nameof(GetProduct), response.Data?.Id);
		}
		[HttpPut("{id}")]
		public async Task<ActionResult<ApiResponse<ProductDto>>> UpdateProduct(int id, UpdateProductDto model)
		{
			_logger.LogInformation($"Executing {nameof(UpdateProduct)} for ID: {id}");
			if (!ModelState.IsValid)
			{
				var errors = string.Join(", ", ModelState.Values
					.SelectMany(v => v.Errors)
					.Select(e => e.ErrorMessage)
					.ToList());
				_logger.LogError($"Validation Errors: {errors}");
				return BadRequest(ApiResponse<ProductDto>.CreateErrorResponse("", new ErrorResponse("Invalid data", errors)));
			}
			var userId = HttpContext.Items["UserId"]?.ToString();
			var response = await _productsServices.UpdateProductAsync(id, model, userId);
			return HandleResult<ProductDto>(response, nameof(GetProduct), id);
		}

		[HttpDelete("{id}")]
		public async Task<ActionResult<ApiResponse<string>>> DeleteProduct(int id)
		{
			_logger.LogInformation($"Executing {nameof(DeleteProduct)} for ID: {id}");
			var userId = HttpContext.Items["UserId"]?.ToString();
			var response = await _productsServices.DeleteProductAsync(id, userId);
			return HandleResult<string>(response, nameof(GetProduct), id);
		}

		[HttpGet("category/{categoryId}")]
		public async Task<ActionResult<ApiResponse<List<ProductDto>>>> GetProductsByCategory(int categoryId)
		{
			_logger.LogInformation($"Executing {nameof(GetProductsByCategory)} for category ID: {categoryId}");
			var response = await _productsServices.GetProductsByCategoryId(categoryId);
			return HandleResult<List<ProductDto>>(response, nameof(GetProductsByCategory), categoryId);
		}

		//[HttpGet("{productId}/inventory")]
		//public async Task<ActionResult<ApiResponse<List<InventoryDto>>>> GetProductInventory(int productId)
		//{
		//	_logger.LogInformation($"Executing {nameof(GetProductInventory)} for product ID: {productId}");
		//	var response = await _productsServices.GetProductInventoryAsync(productId);
		//	return HandleResult<List<InventoryDto>>(response, nameof(GetProductInventory), productId);
		//}

		//[HttpPatch("{productId}/quantity")]
		//public async Task<ActionResult<ApiResponse<ProductDto>>> UpdateProductQuantity(int productId, [FromBody] int quantity)
		//{
		//	_logger.LogInformation($"Executing {nameof(UpdateProductQuantity)} for product ID: {productId}");
		//	var userId = HttpContext.Items["UserId"]?.ToString();
		////	var response = await _productsServices.UpdateProductQuantityAsync(productId, quantity, userId);
		//	return HandleResult<ProductDto>(response, nameof(GetProduct), productId);
		//}

		[HttpGet("deleted-Products")]
		[ResponseCache(Duration = 60)]
		public async Task<ActionResult<ResponseDto>> GetDeletedProductsAsync()
		{
			_logger.LogInformation($"Executing {nameof(GetDeletedProductsAsync)}");
			var resultlist = _unitOfWork.Product.GetAll();
			if (resultlist == null || !resultlist.Any())
			{
				_logger.LogInformation("No Deleted Products found");
				return Ok(new ResponseDto { Message = "No Deleted Products found" });
			}
			var ProductsDtos = resultlist.Select(c => new ProductDto
			{
				Name = c.Name,
				CreatedAt = c.CreatedAt,
				DeletedAt = c.DeletedAt,
				Description = c.Description,
				Id = c.Id,
				ModifiedAt = c.ModifiedAt,
				
			}).ToList();
			_logger.LogInformation($"Deleted Prducts found: {ProductsDtos.Count()}");
			return Ok(new ResponseDto { Message = "Success", Data = ProductsDtos });
		}
		[HttpPatch("return-Deleted-Product")]
		[ResponseCache(Duration = 120, VaryByQueryKeys = new string[] { "id" })]
		public async Task<ActionResult<ResponseDto>> ReturnRemovedCategoryAsync(int id)
		{
			_logger.LogInformation($"Executing {nameof(ReturnRemovedCategoryAsync)}");
			string userid = User.FindFirst(ClaimTypes.NameIdentifier).Value;
			var resultProduct = await _unitOfWork.Product.GetByIdAsync(id);
			if (resultProduct == null)
			{
				_logger.LogWarning($"Product not found with ID: {id}");
				return BadRequest(new ResponseDto { Message = $"Product not found with ID: {id}" });
			}
			using var tran = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled);
			resultProduct.DeletedAt = null;
			var updateResult = _unitOfWork.Product.Update(resultProduct);
			if (!updateResult)
			{
				_logger.LogError("Failed to update product");
				return StatusCode(500, new ResponseDto { Message = "Failed to update product" });
			}
			var logResult = await _unitOfWork.Repository<AdminOperationsLog>().CreateAsync(new AdminOperationsLog
			{
				AdminId = userid,
				ItemId = resultProduct.Id,
				OperationType = Opreations.UndoDeleteOpreation,
				Description = $"Undo Delete of Product: {resultProduct.Id}"
			});
			if (logResult == null)
			{
				_logger.LogError("Failed to log admin operation");
				return StatusCode(500, new ResponseDto { Message = "Failed to log admin operation" });
			}
			int saveResult = await _unitOfWork.CommitAsync();
			if (saveResult == 0)
			{
				_logger.LogError("Database update failed, no changes were committed.");
				return StatusCode(500, new ResponseDto { Message = "Database update failed, no changes were committed." });
			}
			tran.Complete();
			return Ok(new ResponseDto { Message = $"Product restored: {resultProduct.Id}" });
		}

		[HttpDelete()]
		[ResponseCache(Duration = 120, VaryByQueryKeys = new string[] { "id" })]
		public async Task<ActionResult<ResponseDto>> DeleteProductAsync(int id)
		{
			_logger.LogInformation($"Executing {nameof(DeleteProductAsync)}");
			using var transaction = await _unitOfWork.BeginTransactionAsync();
			try
			{
				var resultProduct = await _unitOfWork.Product.GetByIdAsync(id);
				if (resultProduct == null || resultProduct.DeletedAt.HasValue)
				{
					_logger.LogWarning($"No Category with this id: {id}");
					return BadRequest(new ResponseDto { Message = $"No Category with this id: {id}" });
				}
				string adminId = User.FindFirst(ClaimTypes.NameIdentifier).Value;
				resultProduct.DeletedAt = DateTime.UtcNow;
				var result = _unitOfWork.Product.Update(resultProduct);
				if (!result)
				{
					_logger.LogError("Failed to update product");
					return StatusCode(500, new ResponseDto { Message = "Failed to update product" });
				}
				if (await _unitOfWork.CommitAsync() == 0)
				{
					_logger.LogError("Can't delete category.");
					return StatusCode(500, new ResponseDto { Message = "Can't delete category." });
				}
				_logger.LogInformation($"Category Deleted successfully, ID: {resultProduct.Id}");
				AdminOperationsLog adminOperations = new()
				{
					OperationType = Opreations.DeleteOpreation,
					AdminId = adminId,
					Description = $"Deleted category: {resultProduct.Id}",
					Timestamp = DateTime.UtcNow
				};
				var logResult = await _unitOfWork.Repository<AdminOperationsLog>().CreateAsync(adminOperations);
				if (logResult == null)
				{
					await transaction.RollbackAsync();
					return StatusCode(500, new ResponseDto { Message = "Failed to log admin operation" });
				}
				await _unitOfWork.CommitAsync();
				await transaction.CommitAsync();
				return Ok(new ResponseDto { Message = $"Deleted successfully, ID: {resultProduct.Id}", });
			}
			catch (Exception ex)
			{
				_logger.LogError($"Transaction failed: {ex.Message}");
				await transaction.RollbackAsync();
				return StatusCode(500, new ResponseDto { Message = "An error occurred while deleting the category." });
			}
		}
		[HttpGet("Category")]
		public async Task<ActionResult<ResponseDto>> ProductsByCategoryId(int id)
		{
			_logger.LogInformation($"Execute {nameof(ProductsByCategoryId)} ");
			var category = await _unitOfWork.Category.GetByIdAsync(id);
			if (category == null)
			{
				return NotFound(new ResponseDto { Message = "Category not found" });
			}
			return Ok(new ResponseDto { Message = "Success", Data = new object() });
		}

		[HttpGet("wareHouse")]
		public async Task<ActionResult<ResponseDto>> ProductsByWareHouse(int id)
		{
			_logger.LogInformation($"Execute {nameof(ProductsByWareHouse)} ");
			var category = await _unitOfWork.WareHouse.GetByIdAsync(id);
			if (category == null)
			{
				return NotFound(new ResponseDto { Message = "Warehouse not found" });
			}
			if (category.ProductInventories.Count == 0)
			{
				_logger.LogWarning("No Products in this WareHouse");
				return NotFound(new ResponseDto { Message = "No Products in this WareHouse" });
			}
			var productsdto = category.ProductInventories.Select(p => new ProductDto
			{
				Id = p.Product.Id,
				Name = p.Product.Name,
			
				Description = p.Product.Description,
				Discount = p.Product.Discount == null ? null : new DiscountDto(p.Product.Discount.Id, p.Product.Discount.Name, p.Product.Discount.DiscountPercent, p.Product.Discount.Description, p.Product.Discount.IsActive),
				CreatedAt = p.CreatedAt,
			});
			return Ok(new ResponseDto { Message = "Success", Data = productsdto });
		}

		[HttpPatch("{id}/restore")]
		public async Task<ActionResult<ApiResponse<ProductDto>>> RestoreProductAsync(int id)
		{
			var userId = HttpContext.Items["UserId"]?.ToString();
			var result = await _productsServices.RestoreProductAsync(id, userId);
			return HandleResult(result, nameof(RestoreProductAsync), id);
		}
	}
}