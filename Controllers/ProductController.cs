
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

namespace E_Commers.Controllers
{
	[Route("api/[controller]")]
	[ApiController]
	[Authorize(Roles = "Admin")]
	public class ProductController : ControllerBase
	{
		private readonly IUnitOfWork _unitOfWork;
		private readonly ILogger<ProductController> _logger;
		public ProductController(IUnitOfWork unitOfWork,ILogger<ProductController> logger )
		{
			_logger = logger;
			_unitOfWork = unitOfWork;	
		}

		[HttpGet]
		[ResponseCache(Duration =120)]
		public async Task<ActionResult<ResponseDto>> GetAllProducts()
		{
			_logger.LogInformation($"Execute {nameof(GetAllProducts)}");
			Result<IQueryable<Product>> products   =   await _unitOfWork.Repository<Product>().GetAllAsync(include:p=>p.Include(p=>p.Category).Include(p=>p.Discount),p=>p.DeletedAt==null);

			if (!products.Success)
				return BadRequest(new ResponseDto {  Message = products.Message });

			IEnumerable<ProductDto> productsdto = products.Data.Select(p => new ProductDto
			{
				Id= p.Id,
				Name= p.Name,
				AvailabeQuantity= p.Quantity,
				Description= p.Description,
			//	Discount = p.Discount == null ? null : new DiscountDto(p.Discount.Id,p.Discount.Name,p.Discount.DiscountPercent,p.Discount.Description,p.Discount.IsActive),
				FinalPrice=p.Discount==null||!p.Discount.IsActive?p.Price:p.Price-p.Discount.DiscountPercent*p.Price,
			//	Category= new CategoryDto(p.Category.Id, p.Category.Name, p.Category.Description, p.Category.CreatedAt),
				CreatedAt=p.CreatedAt,
				
			});

			return Ok(new ResponseDto { Message= products.Message ,Data=productsdto });


		}
		[HttpGet("id")]
		[ResponseCache(Duration =60,VaryByQueryKeys =new string[] {"id"})]
		public async Task<ActionResult<ResponseDto>> GetProduct(int id)
		{
			_logger.LogInformation($"Execute {nameof(GetProduct)}");
			Result<Product> product = await _unitOfWork.Repository<Product>().GetByIdAsync(id);

			if (!product.Success)
				return BadRequest(new ResponseDto { Message = product.Message });

			var productdto= new ProductDto {
				Id = product.Data.Id,
				Name = product.Data.Name,
				AvailabeQuantity = product.Data.Quantity,
				Description = product.Data.Description,
				Discount = product.Data.Discount == null ? null : new DiscountDto(product.Data.Discount.Id, product.Data.Discount.Name, product.Data.Discount.DiscountPercent, product.Data.Discount.Description, product.Data.Discount.IsActive),
				FinalPrice = product.Data.Discount == null || !product.Data.Discount.IsActive ? product.Data.Price : product.Data.Price - product.Data.Discount.DiscountPercent * product.Data.Price,
				Category = new CategoryDto(product.Data.Category.Id, product.Data.Category.Name, product.Data.Category.Description, product.Data.Category.CreatedAt),
				CreatedAt = product.Data.CreatedAt,
			};

			return Ok(new ResponseDto { Message = product.Message, Data = productdto });
		}

		[HttpPost]
		public async Task<ActionResult> CreateProduct(CreateProductDto model)
		{
			_logger.LogInformation($"Executing {nameof(CreateProduct)}");

			if (!ModelState.IsValid)
			{
				var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
				_logger.LogError($"Validation Errors: {string.Join(", ", errors)}");

				return BadRequest(new ResponseDto
				{
					
					Message = "Invalid data: " + string.Join(", ", errors)
				});
			}

			string userid = User.FindFirst(ClaimTypes.NameIdentifier).Value;

			var checkname = await _unitOfWork.Product.GetByQuery(p=>p.Name==model.Name);
			if (checkname.Success)
			{
				return Conflict(new ResponseDto
				{
				
					Message = $"A product with the name '{model.Name}' already exists"
				});
			}
			var checkcategory= await _unitOfWork.Category.GetByIdAsync(model.CategoryId);
			if (!checkcategory.Success)
				return NotFound(new ResponseDto { Message = checkcategory.Message });
			using var transaction = await _unitOfWork.BeginTransactionAsync();
			try
			{
				Product product = new Product { Price=model.Price,CategoryId=model.CategoryId ,Quantity=model.Quantity, Description = model.Description, Name = model.Name };
				Result<Product> result = await _unitOfWork.Product.CreateAsync(product);

				if (!result.Success)
				{
					return BadRequest(new ResponseDto {  Message = result.Message });
				}

				int changes = await _unitOfWork.CommitAsync();
				if (changes == 0)
				{
					return BadRequest(new ResponseDto { Message = "Nothing added"});
				}

				_logger.LogInformation($"product added successfully, ID: {product.Id}");

				AdminOperationsLog adminOperations = new()
				{
					AdminId = userid,
					Description = $"Added product: {product.Id}",
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

				return CreatedAtAction(nameof(GetProduct), new { id = product.Id }, new ResponseDto { Message = $"Added successfully, ID: {product.Id}", });
			}
			catch (Exception ex)
			{
				_logger.LogError($"Exception: {ex.Message}");
				await transaction.RollbackAsync();
				return StatusCode(500, new ResponseDto { Message = "An error occurred while saving data." });
			}
		}
		[HttpPatch("{id}")]
		
		public async Task<ActionResult<ResponseDto>> UpdateProduct(
			[FromRoute] int id,
			[FromBody] UpdateProductDto updateDto)
		{
			_logger.LogInformation("Executing {MethodName} for Product ID: {ProductId}", nameof(UpdateProduct), id);

			// 1. Model Validation
			if (!ModelState.IsValid)
			{
				var errors = ModelState.Values
					.SelectMany(v => v.Errors)
					.Select(e => e.ErrorMessage)
					.ToList();

				_logger.LogWarning("Validation failed for Product ID {ProductId}: {Errors}", id, string.Join(", ", errors));

				return BadRequest(new ResponseDto
				{
					
					Message = $"Validation failed\n Errors:{errors}",
					
				});
			}

			// 2. Get Admin ID (with null check)
			var adminId = User.FindFirstValue(ClaimTypes.NameIdentifier);
			

			// 3. Fetch Product
			var productResult = await _unitOfWork.Product.GetByIdAsync(id);
			if (!productResult.Success || productResult.Data == null)
			{
				_logger.LogWarning("Product ID {ProductId} not found", id);
				return NotFound(new ResponseDto
				{
					
					Message = productResult.Message ?? "Product not found"
				});
			}

			var product = productResult.Data;

			// 4. Check for Duplicate Name (if name is being updated)
			if (!string.IsNullOrWhiteSpace(updateDto.Name) &&
				!product.Name.Equals(updateDto.Name, StringComparison.OrdinalIgnoreCase))
			{
				var nameCheck = await _unitOfWork.Product.GetByQuery(p=>p.Name == updateDto.Name);
				if (!nameCheck.Success && nameCheck.Data != null)
				{
					_logger.LogWarning("Duplicate product name: {ProductName}", updateDto.Name);
					return Conflict(new ResponseDto
					{
						
						Message = $"A product with name '{updateDto.Name}' already exists"
					});
				}
			
				product.Name = updateDto.Name;
			}

			// 5. Update Fields (if provided)
			if (!string.IsNullOrWhiteSpace(updateDto.Description))
				product.Description = updateDto.Description;

			if (updateDto.Quantity.HasValue && updateDto.Quantity != product.Quantity)
			{
				_logger.LogInformation("Updating quantity for Product ID {ProductId}", id);
				product.Quantity = updateDto.Quantity.Value;
			}

			if (updateDto.CategoryId.HasValue && updateDto.CategoryId != product.CategoryId)
			{
				var categoryCheck = await _unitOfWork.Category.GetByIdAsync(updateDto.CategoryId.Value);
				if (!categoryCheck.Success || categoryCheck.Data == null)
				{
					return NotFound(new ResponseDto
					{
						
						Message = "Specified category does not exist"
					});
				}
				product.CategoryId = updateDto.CategoryId.Value;
			}

			if (updateDto.Price.HasValue && updateDto.Price != product.Price)
				product.Price = updateDto.Price.Value;

			// 6. Save Changes (Transaction)
			await using var transaction = await _unitOfWork.BeginTransactionAsync();

			try
			{
				var updateResult = await _unitOfWork.Product.UpdateAsync(product);
				if (!updateResult.Success)
				{
					_logger.LogError("Failed to update Product ID {ProductId}: {Error}", id, updateResult.Message);
					await transaction.RollbackAsync();
					return StatusCode(StatusCodes.Status500InternalServerError, new ResponseDto
					{
					
						Message = "Failed to update product"
					});
				}

				// 7. Log Admin Action
				var adminLog = new AdminOperationsLog
				{
					OperationType = Opreations.UpdateOpreation,
					AdminId = adminId,
					Description = $"Updated Product: {product.Id}",
					Timestamp = DateTime.UtcNow
				};

				var logResult = await _unitOfWork.Repository<AdminOperationsLog>().CreateAsync(adminLog);
				if (!logResult.Success)
				{
					await transaction.RollbackAsync();
					_logger.LogError("Failed to log admin operation for Product ID {ProductId}", id);
					return StatusCode(StatusCodes.Status500InternalServerError, new ResponseDto
					{
					
						Message = "Failed to log operation"
					});
				}

				await _unitOfWork.CommitAsync();
				await transaction.CommitAsync();

				_logger.LogInformation("Successfully updated Product ID {ProductId}", id);
				return Ok(new ResponseDto
				{
					
					Message = $"Product updated successfully ID: {product.Id}",
					
				});
			}
			catch (Exception ex)
			{
				await transaction.RollbackAsync();
				_logger.LogError(ex, "Error updating Product ID {ProductId}", id);
				return StatusCode(StatusCodes.Status500InternalServerError, new ResponseDto
				{
					Message = "An unexpected error occurred"
				});
			}
		}

		[HttpGet("deleted-Products")]
		[ResponseCache(Duration =60)]
		public async Task<ActionResult<ResponseDto>> GetDeletedProductsAsync()
		{
			_logger.LogInformation($"Executing {nameof(GetDeletedProductsAsync)}");

			var resultlist = await _unitOfWork.Product.GetAllAsync(p=>p.Include(c=>c.Category).Include(c => c.Discount), filter: c => c.DeletedAt.HasValue);

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
			
				FinalPrice= c.Price
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
			if(category.Data.products.Count==0)
			{
				_logger.LogWarning("No Products in this Category");
				return NotFound(new ResponseDto { Message = "No Products in this Category" });
			}
			var productsdto = category.Data.products.Select(p => new ProductDto
			{
				Id = p.Id,
				Name = p.Name,
				AvailabeQuantity = p.Quantity,
				Description = p.Description,
				Discount = p.Discount == null ? null : new DiscountDto(p.Discount.Id, p.Discount.Name, p.Discount.DiscountPercent, p.Discount.Description, p.Discount.IsActive),
				FinalPrice = p.Discount == null || !p.Discount.IsActive ? p.Price : p.Price - p.Discount.DiscountPercent * p.Price,
				Category = new CategoryDto(p.Category.Id, p.Category.Name, p.Category.Description, p.Category.CreatedAt),
				CreatedAt = p.CreatedAt,
			});
			return Ok(new ResponseDto { Message = category.Message , Data= productsdto });
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
				FinalPrice = p.Product.Discount == null || !p.Product.Discount.IsActive ? p.Product.Price : p.Product.Price - p.Product.Discount.DiscountPercent * p.Product.Price,
				Category = new CategoryDto(p.Product.Category.Id, p.Product.Category.Name, p.Product.Category.Description, p.Product.Category.CreatedAt),
				CreatedAt = p.CreatedAt,
			});
			return Ok(new ResponseDto { Message = category.Message , Data= productsdto });
		}
	}
}
