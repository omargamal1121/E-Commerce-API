using AutoMapper;
using E_Commers.DtoModels;
using E_Commers.DtoModels.CategoryDtos;
using E_Commers.DtoModels.ImagesDtos;
using E_Commers.DtoModels.Responses;
using E_Commers.DtoModels.Shared;
using E_Commers.ErrorHnadling;
using E_Commers.Interfaces;
using E_Commers.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace E_Commers.Controllers
{
	[Route("api/[controller]")]
	[ApiController]
	[Authorize(Roles = "Admin")]
	public class CategoriesController : ControllerBase
	{
		private readonly ICategoryServices _categoryServices;
		private readonly ILogger<CategoriesController> _logger;
		private readonly ICategoryLinkBuilder _linkBuilder;

		public CategoriesController(
			ICategoryLinkBuilder linkBuilder,
			ICategoryServices categoryServices,
			ILogger<CategoriesController> logger)
		{
			_linkBuilder = linkBuilder;
			_categoryServices = categoryServices;
			_logger = logger;
		}

		private ActionResult<ApiResponse<T>> HandleResult<T>(Result<T> result, string apiname, int? id = null)
		{
			var links = _linkBuilder.MakeRelSelf(_linkBuilder.GenerateLinks(id), apiname);
			ApiResponse<T> apiResponse;
			if (result.Success)
			{
				apiResponse = ApiResponse<T>.CreateSuccessResponse(result.Message, result.Data, result.StatusCode, warnings: result.Warnings, links: links);
			}
			else
			{
				var errorResponse = (result.Warnings != null && result.Warnings.Count > 0)
					? new ErrorResponse("Error", result.Warnings)
					: new ErrorResponse("Error", result.Message);
				apiResponse = ApiResponse<T>.CreateErrorResponse(result.Message, errorResponse, result.StatusCode, warnings: result.Warnings, links: links);
			}

			switch (result.StatusCode)
			{
				case 200: return Ok(apiResponse);
				case 201: return StatusCode(201, apiResponse);
				case 400: return BadRequest(apiResponse);
				case 401: return Unauthorized(apiResponse);
				case 409: return Conflict(apiResponse);
				default: return StatusCode(result.StatusCode, apiResponse);
			}
		}

		[HttpGet("{id}", Name = "GetCategoryById")]
		[ActionName(nameof(GetByIdAsync))]
		public async Task<ActionResult<ApiResponse<CategoryDto>>> GetByIdAsync(
			int id,
			[FromQuery] bool? isActive = null,
			[FromQuery] bool includeDeleted = false)
		{
			_logger.LogInformation($"Executing {nameof(GetByIdAsync)} for id: {id}, isActive: {isActive}, includeDeleted: {includeDeleted}");
			var result = await _categoryServices.GetCategoryByIdAsync(id, isActive, includeDeleted);
			return HandleResult(result, nameof(GetByIdAsync), id);
		}

		[HttpGet]
		[ActionName(nameof(GetAllAsync))]
		public async Task<ActionResult<ApiResponse<List<CategoryDto>>>> GetAllAsync(
			[FromQuery] string? search,
			[FromQuery] bool? isActive,
			[FromQuery] bool includeDeleted = false,
			[FromQuery] int page = 1,
			[FromQuery] int pageSize = 10)
		{
			_logger.LogInformation($"Executing {nameof(GetAllAsync)} with filters");

			if (page <= 0 || pageSize <= 0)
			{
				return BadRequest(ApiResponse<List<CategoryDto>>.CreateErrorResponse(
					"Invalid Pagination",
					new ErrorResponse("Validation", new List<string> { "Page and PageSize must be greater than 0" }),
					400
				));
			}

			var role = User.FindFirst(ClaimTypes.Role)?.Value ?? "User";
			var result = await _categoryServices.FilterAsync(search, isActive, includeDeleted, page, pageSize, role);
			return HandleResult(result, nameof(GetAllAsync));
		}

		[HttpPost]
		[ActionName(nameof(CreateAsync))]
		public async Task<ActionResult<ApiResponse<CategoryDto>>> CreateAsync([FromForm] CreateCategotyDto categoryDto)
		{
			_logger.LogInformation($"Executing {nameof(CreateAsync)}");
			if (!ModelState.IsValid)
			{
				var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
				return BadRequest(ApiResponse<CategoryDto>.CreateErrorResponse("Check On Data", new ErrorResponse("Invalid Data", errors)));
			}
			var userId = HttpContext.Items["UserId"]?.ToString();
			var result = await _categoryServices.CreateAsync(categoryDto, userId);
			return HandleResult(result, nameof(CreateAsync));
		}

		[HttpDelete("{id}")]
		[ActionName(nameof(DeleteAsync))]
		public async Task<ActionResult<ApiResponse<string>>> DeleteAsync(int id)
		{
			_logger.LogInformation($"Executing {nameof(DeleteAsync)} for id: {id}");
			var userId = HttpContext.Items["UserId"]?.ToString();
			var result = await _categoryServices.DeleteAsync(id, userId);
			return HandleResult(result, nameof(DeleteAsync));
		}

		[HttpPatch("{id}/Restore")]
		[ActionName(nameof(ReturnRemovedCategoryAsync))]
		public async Task<ActionResult<ApiResponse<CategoryDto>>> ReturnRemovedCategoryAsync(int id)
		{
			_logger.LogInformation($"Executing {nameof(ReturnRemovedCategoryAsync)} for id: {id}");
			var userId = HttpContext.Items["UserId"]?.ToString();
			var result = await _categoryServices.ReturnRemovedCategoryAsync(id, userId);
			return HandleResult(result, nameof(ReturnRemovedCategoryAsync), id);
		}

		[HttpPut("{id}")]
		[ActionName(nameof(UpdateAsync))]
		public async Task<ActionResult<ApiResponse<CategoryDto>>> UpdateAsync(int id, [FromForm] UpdateCategoryDto categoryDto)
		{
			_logger.LogInformation($"Executing {nameof(UpdateAsync)} for id: {id}");
			if (!ModelState.IsValid)
			{
				var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
				return BadRequest(ApiResponse<CategoryDto>.CreateErrorResponse("Check on data", new ErrorResponse("Invalid Data", errors)));
			}
			var userId = HttpContext.Items["UserId"]?.ToString();
			var result = await _categoryServices.UpdateAsync(id, categoryDto, userId);
			return HandleResult(result, nameof(UpdateAsync), id);
		}

		[HttpPost("{id}/AddMainImage")]
		[ActionName(nameof(AddMainImageAsync))]
		public async Task<ActionResult<ApiResponse<ImageDto>>> AddMainImageAsync(int id, [FromForm] AddMainImageDto mainImage)
		{
			_logger.LogInformation($"Executing {nameof(AddMainImageAsync)} for id: {id}");
			if (mainImage.Image == null || mainImage.Image.Length == 0)
			{
				return BadRequest(ApiResponse<ImageDto>.CreateErrorResponse("Image Can't Empty", new ErrorResponse("Validation", new List<string> { "Main image is required." }), 400));
			}
			var userId = HttpContext.Items["UserId"]?.ToString();
			var result = await _categoryServices.AddMainImageToCategoryAsync(id, mainImage.Image, userId);
			return HandleResult(result, nameof(AddMainImageAsync), id);
		}

		[HttpPost("{id}/AddExtraImages")]
		[ActionName(nameof(AddExtraImagesAsync))]
		public async Task<ActionResult<ApiResponse<List<ImageDto>>>> AddExtraImagesAsync(int id, [FromForm] AddImagesDto images)
		{
			_logger.LogInformation($"Executing {nameof(AddExtraImagesAsync)} for id: {id}");
			if (images.Images == null || !images.Images.Any())
			{
				return BadRequest(ApiResponse<List<ImageDto>>.CreateErrorResponse("Image Can't Empty", new ErrorResponse("Validation", new List<string> { "At least one image is required." }), 400));
			}
			var userId = HttpContext.Items["UserId"]?.ToString();
			var result = await _categoryServices.AddImagesToCategoryAsync(id, images.Images, userId);
			return HandleResult(result, nameof(AddExtraImagesAsync), id);
		}

		[HttpPatch("{categoryId}/ChangeActiveStatus")]
		[ActionName(nameof(ChangeActiveStatus))]
		public async Task<ActionResult<ApiResponse<bool>>> ChangeActiveStatus(int categoryId)
		{
			_logger.LogInformation($"Executing {nameof(ChangeActiveStatus)} for categoryId: {categoryId}");
			var userId = HttpContext.Items["UserId"]?.ToString();
			var result = await _categoryServices.ChangeActiveStatus(categoryId, userId);
			return HandleResult(result, nameof(ChangeActiveStatus), categoryId);
		}

		[HttpDelete("{categoryId}/RemoveImage/{imageId}")]
		[ActionName(nameof(RemoveImageAsync))]
		public async Task<ActionResult<ApiResponse<CategoryDto>>> RemoveImageAsync(int categoryId, int imageId)
		{
			var userId = HttpContext.Items["UserId"]?.ToString();
			var result = await _categoryServices.RemoveImageFromCategoryAsync(categoryId, imageId, userId);
			return HandleResult(result, nameof(RemoveImageAsync), categoryId);
		}

		[HttpGet("test-links")]
		[ActionName(nameof(TestLinks))]
		public ActionResult<ApiResponse<List<LinkDto>>> TestLinks()
		{
			var links = _linkBuilder.GenerateLinks(1);
			return Ok(ApiResponse<List<LinkDto>>.CreateSuccessResponse("Test Links", links, 200, links: links));
		}
	}
}
