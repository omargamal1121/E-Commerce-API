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
		public ProductController(IProductLinkBuilder productLinkBuilder, IUnitOfWork unitOfWork,IProductsServices productsServices,ILogger<ProductController> logger )
		{
			_productLinkBuilder = productLinkBuilder;
			_productsServices = productsServices;
			_logger = logger;
			_unitOfWork = unitOfWork;	
		}

		[HttpGet]
		[ResponseCache(Duration =120)]
		public async Task<ActionResult<ApiResponse<List<ProductDto>>>> GetAllProducts()
		{
			_logger.LogInformation($"Executing {nameof(GetAllProducts)}");
			var response = await _productsServices.GetAllAsync();
			return HandleResponse(response, nameof(GetAllProducts));
		}
		[HttpGet("{id}")]
		[ResponseCache(Duration =60,VaryByQueryKeys =new string[] {"id"})]
		public async Task<ActionResult<ApiResponse<ProductDto>>> GetProduct(int id)
		{
			_logger.LogInformation($"Executing {nameof(GetProduct)} for ID: {id}");
			var response = await _productsServices.GetProductByIdAsync(id);
			return HandleResponse(response, nameof(GetProduct), id);
		}
		private ActionResult<ApiResponse<T>> HandleResponse<T>(
	 ApiResponse<T> response,
	 string actionName,
	 int? id = null)
			where T : class
		{
			
			
				response.ResponseBody.Links = _productLinkBuilder.MakeRelSelf(
					_productLinkBuilder.GenerateLinks(id),
					actionName
				);
			
			return response.Statuscode switch
			{
				200 => Ok(response),
				201 => CreatedAtAction(actionName, new { id }, response),
				400 => BadRequest(response),
				401 => Unauthorized(response),
				404 => NotFound(response),
				409 => Conflict(response),
				_ => StatusCode(response.Statuscode, response),
			};
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
				_logger.LogError($"Validation Errors: { errors}");
				return BadRequest(ApiResponse<Product>.CreateErrorResponse(new ErrorResponse("Invalid data",errors)));
			}

			var userId = HttpContext.Items["UserId"]?.ToString();
			var response = await _productsServices.CreateProductAsync(model, userId);
			return HandleResponse(response, nameof(GetProduct),response.ResponseBody?.Data?.Id);
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
				return BadRequest(ApiResponse<Product>.CreateErrorResponse(new ErrorResponse("Invalid data", errors)));
			}

			var userId = HttpContext.Items["UserId"]?.ToString();
			var response = await _productsServices.UpdateProductAsync(id, model, userId);
			return HandleResponse(response, nameof(GetProduct), id);
		}

		[HttpDelete("{id}")]
		public async Task<ActionResult<ApiResponse<string>>> DeleteProduct(int id)
		{
			_logger.LogInformation($"Executing {nameof(DeleteProduct)} for ID: {id}");
			var userId = HttpContext.Items["UserId"]?.ToString();
			var response = await _productsServices.DeleteProductAsync(id, userId);
			return HandleResponse(response, nameof(GetProduct), id);
		}

		[HttpGet("category/{categoryId}")]
		public async Task<ActionResult<ApiResponse<List<ProductDto>>>> GetProductsByCategory(int categoryId)
		{
			_logger.LogInformation($"Executing {nameof(GetProductsByCategory)} for category ID: {categoryId}");
			var response = await _productsServices.GetProductsByCategoryId(categoryId);
			return HandleResponse(response, nameof(GetProductsByCategory), categoryId);
		}

		[HttpGet("{productId}/inventory")]
		public async Task<ActionResult<ApiResponse<List<InventoryDto>>>> GetProductInventory(int productId)
		{
			_logger.LogInformation($"Executing {nameof(GetProductInventory)} for product ID: {productId}");
			var response = await _productsServices.GetProductInventoryAsync(productId);
			return HandleResponse(response, nameof(GetProductInventory), productId);
		}

		[HttpPatch("{productId}/quantity")]
		public async Task<ActionResult<ApiResponse<ProductDto>>> UpdateProductQuantity(int productId, [FromBody] int quantity)
		{
			_logger.LogInformation($"Executing {nameof(UpdateProductQuantity)} for product ID: {productId}");
			var userId = HttpContext.Items["UserId"]?.ToString();
			var response = await _productsServices.UpdateProductQuantityAsync(productId, quantity, userId);
			return HandleResponse(response, nameof(GetProduct), productId);
		}

		[HttpGet("deleted-Products")]
		[ResponseCache(Duration =60)]
		public async Task<ActionResult<ResponseDto>> GetDeletedProductsAsync()
		{
			_logger.LogInformation($"Executing {nameof(GetDeletedProductsAsync)}");

			var resultlist = await _unitOfWork.Product.GetAllAsync(p=>p.Include(c=>c.SubCategory).Include(c => c.Discount), filter: c => c.DeletedAt.HasValue);

			if (!resultlist.Success|| !resultlist.Data.Any())
			{
				_logger.LogInformation("No Deleted Products found");
				return Ok(new ResponseDto { Message = "No Deleted Products found" });
			}

			var ProductsDtos = resultlist.Data.Select(c => new ProductDto
			{
				Name = c.Name,
				CreatedAt = c.CreatedAt,
				DeletedAt = c.DeletedAt,
				Description = c.Description,
				Id = c.Id,
				ModifiedAt = c.ModifiedAt,
				AvailabeQuantity=c.Quantity,
			
			
			}).ToList();

			_logger.LogInformation($"Deleted Prducts found: {ProductsDtos.Count()}");
			return Ok(new ResponseDto { Message = resultlist.Message,Data= ProductsDtos });
		}
		[HttpPatch("return-Deleted-Product")]
		[ResponseCache(Duration =120,VaryByQueryKeys =new string[] { "id"})]
		public async Task<ActionResult<ResponseDto>> ReturnRemovedCategoryAsync(int id)
		{
			_logger.LogInformation($"Executing {nameof(ReturnRemovedCategoryAsync)}");

			string userid = User.FindFirst(ClaimTypes.NameIdentifier).Value;


			Result<Product> resultProduct = await _unitOfWork.Product.GetByIdAsync(id);
			if (!resultProduct.Success)
			{
				_logger.LogWarning($"Product not found with ID: {id}");
				return BadRequest(new ResponseDto {Message = $"Product not found with ID: {id}" });
			}

			using var tran = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled);

			resultProduct.Data.DeletedAt = null;
			Result<Product> updateResult = await _unitOfWork.Product.UpdateAsync(resultProduct.Data);
			if (!updateResult.Success)
			{
				_logger.LogError(updateResult.Message);
				return StatusCode(500, new ResponseDto { Message = updateResult.Message });
			}

			Result<AdminOperationsLog> logResult = await _unitOfWork.Repository<AdminOperationsLog>().CreateAsync(new AdminOperationsLog
			{
				AdminId = userid,
				ItemId = resultProduct.Data.Id,
				OperationType = Opreations.UndoDeleteOpreation,
				Description = $"Undo Delete of Product: {resultProduct.Data.Id}"
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

			tran.Complete();
			return Ok(new ResponseDto { Message = $"Product restored: {resultProduct.Data.Id}" });
		}

		[HttpDelete()]
		[ResponseCache(Duration = 120, VaryByQueryKeys = new string[] { "id" })]
		public async Task<ActionResult<ResponseDto>> DeleteProductAsync(int id)
		{
			_logger.LogInformation($"Executing {nameof(DeleteProductAsync)}");

			using var transaction = await _unitOfWork.BeginTransactionAsync();

			try
			{
				Result<Product> resultProduct = await _unitOfWork.Product.GetByIdAsync(id);
				if (resultProduct.Data is null || resultProduct.Data.DeletedAt.HasValue)
				{
					_logger.LogWarning($"No Category with this id: {id}");
					return BadRequest(new ResponseDto {Message = $"No Category with this id: {id}" });
				}

				string adminId = User.FindFirst(ClaimTypes.NameIdentifier).Value;

				
				resultProduct.Data.DeletedAt = DateTime.UtcNow;

				Result<Product> result = await _unitOfWork.Product.UpdateAsync(resultProduct.Data);
				if (!result.Success)
				{
					_logger.LogError(result.Message);
					return StatusCode(500, new ResponseDto { Message = result.Message });
				}

				if (await _unitOfWork.CommitAsync() == 0)
				{
					_logger.LogError("Can't delete category.");
					return StatusCode(500, new ResponseDto { Message = "Can't delete category." });
				}

				_logger.LogInformation($"Category Deleted successfully, ID: {resultProduct.Data.Id}");

				AdminOperationsLog adminOperations = new()
				{
					OperationType = Opreations.DeleteOpreation,
					AdminId = adminId,
					Description = $"Deleted category: {resultProduct.Data.Id}",
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

				return Ok(new ResponseDto { Message = $"Deleted successfully, ID: {resultProduct.Data.Id}", });
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
			  Result< Category > category = await _unitOfWork.Category.GetByIdAsync(id);
			if(!category.Success|| category.Data is null)
			{
				return NotFound(new ResponseDto { Message = category.Message });

			}
			//if(category.Data.products.Count==0)
			//{
			//	_logger.LogWarning("No Products in this Category");
			//	return NotFound(new ResponseDto { Message = "No Products in this Category" });
			//}
			//var productsdto = category.Data.subCategories.Select(p => new ProductDto
			//{
			//	Id = p.Id,
			//	Name = p.Name,
			//	AvailabeQuantity = p.Quantity,
			//	Description = p.Description,
			//	Discount = p.Discount == null ? null : new DiscountDto(p.Discount.Id, p.Discount.Name, p.Discount.DiscountPercent, p.Discount.Description, p.Discount.IsActive),
			//	FinalPrice = p.Discount == null || !p.Discount.IsActive ? p.Price : p.Price - p.Discount.DiscountPercent * p.Price,
			//	Category = new CategoryDto(p.Category.Id, p.Category.Name, p.Category.Description, p.Category.CreatedAt),
			//	CreatedAt = p.CreatedAt,
			//});
			return Ok(new ResponseDto { Message = category.Message, Data = new object() });
		}

		[HttpGet("wareHouse")]
		public async Task<ActionResult<ResponseDto>> ProductsByWareHouse(int id)
		{
			_logger.LogInformation($"Execute {nameof(ProductsByWareHouse)} ");
			Result<Warehouse> category = await _unitOfWork.WareHouse.GetByIdAsync(id);
			if (!category.Success || category.Data is null)
			{
				return NotFound(new ResponseDto { Message = category.Message });

			}
			if (category.Data.ProductInventories.Count == 0)
			{
				_logger.LogWarning("No Products in this WareHouse");
				return NotFound(new ResponseDto { Message = "No Products in this WareHouse" });
			}
			var productsdto = category.Data.ProductInventories.Select(p => new ProductDto
			{
				Id = p.Product.Id,
				Name = p.Product.Name,
				AvailabeQuantity = p.Quantity,
				Description = p.Product.Description,
				Discount = p.Product.Discount == null ? null : new DiscountDto(p.Product.Discount.Id, p.Product.Discount.Name, p.Product.Discount.DiscountPercent, p.Product.Discount.Description, p.Product.Discount.IsActive),
				//FinalPrice = p.Product.Discount == null || !p.Product.Discount.IsActive ? p.Product.Price : p.Product.Price - p.Product.Discount.DiscountPercent * p.Product.Price,
				//Category = new CategoryDto(p.Product.Category.Id, p.Product.Category.Name, p.Product.Category.Description, p.Product.Category.CreatedAt),
				CreatedAt = p.CreatedAt,
			});
			return Ok(new ResponseDto { Message = category.Message , Data= productsdto });
		}
	}
}
