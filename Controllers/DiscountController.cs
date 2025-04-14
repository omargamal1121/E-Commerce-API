using E_Commers.DtoModels.AccountDtos;
using E_Commers.DtoModels.CategoryDtos;
using E_Commers.DtoModels.DiscoutDtos;
using E_Commers.DtoModels.ProductDtos;
using E_Commers.Helper;
using E_Commers.Models;
using E_Commers.UOW;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace E_Commers.Controllers
{
	[Route("api/[controller]")]
	[ApiController]
	public class DiscountController : ControllerBase
	{
		private readonly IUnitOfWork _unitOfWork;
		private readonly ILogger<DiscountController> _logger;

		public DiscountController(IUnitOfWork unitOfWork, ILogger<DiscountController> logger)
		{
			_unitOfWork = unitOfWork;
			_logger = logger;

		}
		[HttpGet("{id}")]

		public async Task<ActionResult<ResponseDto>> GetDiscount([FromRoute] int id)
		{
			_logger.LogInformation($"Executing {nameof(GetDiscount)} in DiscountController");


			ResultDto<Discount> discountresult = await _unitOfWork.Repository<Discount>().GetByIdAsync(id,include:d=>d.Include(d=>d.products).ThenInclude(p=>p.Category));
			if (!discountresult.Success || discountresult.Data is null)
			{

				return Ok(new ResponseDto { StatusCode = 200, Message = discountresult.Message });
			}
			DiscountDto discountDto = new DiscountDto
			{
				Id = discountresult.Data.Id,
				Name = discountresult.Data.Name,
				DiscountPercent = discountresult.Data.DiscountPercent,
				Description = discountresult.Data.Description, IsActive = discountresult.Data.IsActive,
				products = discountresult.Data.products.Select(p=>new ProductDto 
				{
					Id = p.Id,
					Name = p.Name,
					AvailabeQuantity = p.Quantity,
					Description = p.Description,
					FinalPrice =discountresult.Data.IsActive ? p.Price : p.Price - discountresult.Data.DiscountPercent * p.Price,
					Category = new CategoryDto(p.Category.Id, p.Category.Name, p.Category.Description, p.Category.CreatedAt),
					CreatedAt = p.CreatedAt,
				}
				).ToList()
			};



			return Ok(new ResponseDto { Message = discountresult.Message, Data = discountDto, StatusCode = 200 });
		}

		[HttpGet("GetAll")]
		[ResponseCache(Duration = 120, VaryByQueryKeys = new string[] { "includeDates" })]
		public async Task<ActionResult<ResponseDto>> GetAll([FromQuery] bool includeDates = false)
		{
			_logger.LogInformation($"Executing {nameof(GetAll)} in DiscountController");


			var discountresult = await _unitOfWork.Repository<Discount>().GetAllAsync(filter: c => c.DeletedAt == null, include: d => d.Include(d => d.products).ThenInclude(p => p.Category));
			if (!discountresult.Success||  discountresult.Data is null)
			{
				return Ok(new ResponseDto { StatusCode = 200, Message = discountresult.Message, });
			}

			List<DiscountDto> discountDtos = discountresult.Data.Select(c =>
			new DiscountDto
			{
				Id = c.Id,
				Name = c.Name,
				DiscountPercent = c.DiscountPercent,
				Description = c.Description,
				IsActive = c.IsActive,
				products = c.products.Select(p => new ProductDto
				{
					Id = p.Id,
					Name = p.Name,
					AvailabeQuantity = p.Quantity,
					Description = p.Description,
					FinalPrice = c.IsActive ? p.Price : p.Price - c.DiscountPercent * p.Price,
					Category = new CategoryDto(p.Category.Id, p.Category.Name, p.Category.Description, p.Category.CreatedAt),
					CreatedAt = p.CreatedAt,
				}).ToList()
			}
			).ToList();



			return Ok(new ResponseDto { Message = discountresult.Message, Data = discountDtos, StatusCode = 200 });
		}

		[HttpPost]
		public async Task<ActionResult> CreateDiscountAsnc(CreateDiscountDto model)
		{
			_logger.LogInformation($"Executing {nameof(CreateDiscountAsnc)}");

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

			string userid = User.FindFirst(ClaimTypes.NameIdentifier).Value;

			var checkname = await _unitOfWork.Repository<Discount>().GetByQuery(d=>d.Name== model.Name);
			if (checkname.Success)
			{
				return BadRequest(new ResponseDto
				{
					StatusCode = 400,
					Message = $"Their's Discount with this Name:{model.Name}"
				});
			}

			using var transaction = await _unitOfWork.BeginTransactionAsync();
			try
			{
				Discount discount = new Discount { DiscountPercent= model.DiscountPercent,IsActive=model.IsActive, Description = model.Description, Name = model.Name };
				ResultDto<bool> result = await _unitOfWork.Repository<Discount>().CreateAsync(discount);

				if (!result.Success)
				{
					_logger.LogWarning(result.Message);
					return BadRequest(new ResponseDto { Message = result.Message, StatusCode = 400 });
				}

				_logger.LogInformation($"discount added successfully, ID: {discount.Id}");

				AdminOperationsLog adminOperations = new()
				{
					AdminId = userid,
					Description = $"Added discount: {discount.Id}",
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

				return Ok(new ResponseDto { Message = $"Added successfully, ID: {discount.Id}", StatusCode = 200 });
			}
			catch (Exception ex)
			{
				_logger.LogError($"Exception: {ex.Message}");
				await transaction.RollbackAsync();
				return StatusCode(500, new ResponseDto { Message = "An error occurred while saving data.", StatusCode = 500 });
			}
		}

	}
}
