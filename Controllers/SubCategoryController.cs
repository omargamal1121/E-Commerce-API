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
using System.ComponentModel.DataAnnotations;

namespace E_Commers.Controllers
{
	[Route("api/[controller]")]
	[ApiController]
	[Authorize(Roles = "Admin")]
	public class SubCategoryController : ControllerBase
	{
		private readonly ISubCategoryServices _subCategoryServices;
		private readonly ILogger<SubCategoryController> _logger;
		private readonly ISubCategoryLinkBuilder _linkBuilder;

		public SubCategoryController(
			ISubCategoryLinkBuilder linkBuilder,
			ISubCategoryServices subCategoryServices,
			ILogger<SubCategoryController> logger)
		{
			_linkBuilder = linkBuilder;
			_subCategoryServices = subCategoryServices;
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

		[HttpGet("{id}", Name = "GetSubCategoryById")]
		[ActionName(nameof(GetByIdAsync))]
		public async Task<ActionResult<ApiResponse<SubCategoryDto>>> GetByIdAsync(
			int id,
			[FromQuery] bool isActive = true,
			[FromQuery] bool includeDeleted = false)
		{
			_logger.LogInformation($"Executing {nameof(GetByIdAsync)} for id: {id}, isActive: {isActive}, includeDeleted: {includeDeleted}");
			var result = await _subCategoryServices.GetSubCategoryByIdAsync(id, isActive, includeDeleted);
			return HandleResult(result, nameof(GetByIdAsync), id);
		}

		[HttpGet]
		[ActionName(nameof(GetAllAsync))]
		public async Task<ActionResult<ApiResponse<List<SubCategoryDto>>>> GetAllAsync(
			[FromQuery] string? search,
			[FromQuery] bool? isActive,
			[FromQuery] bool includeDeleted = false,
			[FromQuery] int page = 1,
			[FromQuery] int pageSize = 10)
		{
			_logger.LogInformation($"Executing {nameof(GetAllAsync)} with filters");

			if (page <= 0 || pageSize <= 0)
			{
				return BadRequest(ApiResponse<List<SubCategoryDto>>.CreateErrorResponse(
					"Invalid Pagination",
					new ErrorResponse("Validation", new List<string> { "Page and PageSize must be greater than 0" }),
					400
				));
			}

			var role = User.FindFirst(ClaimTypes.Role)?.Value ?? "User";
			var result = await _subCategoryServices.FilterAsync(search, isActive, includeDeleted, page, pageSize, role);
			return HandleResult(result, nameof(GetAllAsync));
		}

		[HttpPost]
		[ActionName(nameof(CreateAsync))]
		public async Task<ActionResult<ApiResponse<SubCategoryDto>>> CreateAsync([FromForm] CreateSubCategoryDto subCategoryDto)
		{
			_logger.LogInformation($"Executing {nameof(CreateAsync)}");
			if (!ModelState.IsValid)
			{
				var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
				return BadRequest(ApiResponse<SubCategoryDto>.CreateErrorResponse("Check On Data", new ErrorResponse("Invalid Data", errors)));
			}
			var userId = HttpContext.Items["UserId"]?.ToString();
			var result = await _subCategoryServices.CreateAsync(subCategoryDto, userId);
			return HandleResult(result, nameof(CreateAsync));
		}

		[HttpDelete("{id}")]
		[ActionName(nameof(DeleteAsync))]
		public async Task<ActionResult<ApiResponse<string>>> DeleteAsync(int id)
		{
			_logger.LogInformation($"Executing {nameof(DeleteAsync)} for id: {id}");
			var userId = HttpContext.Items["UserId"]?.ToString();
			var result = await _subCategoryServices.DeleteAsync(id, userId);
			return HandleResult(result, nameof(DeleteAsync));
		}

		[HttpPatch("{id}/Restore")]
		[ActionName(nameof(ReturnRemovedSubCategoryAsync))]
		public async Task<ActionResult<ApiResponse<SubCategoryDto>>> ReturnRemovedSubCategoryAsync(int id)
		{
			_logger.LogInformation($"Executing {nameof(ReturnRemovedSubCategoryAsync)} for id: {id}");
			var userId = HttpContext.Items["UserId"]?.ToString();
			var result = await _subCategoryServices.ReturnRemovedSubCategoryAsync(id, userId);
			return HandleResult(result, nameof(ReturnRemovedSubCategoryAsync), id);
		}

		[HttpPut("{id}")]
		[ActionName(nameof(UpdateAsync))]
		public async Task<ActionResult<ApiResponse<SubCategoryDto>>> UpdateAsync(int id, [FromForm] UpdateSubCategoryDto subCategoryDto)
		{
			_logger.LogInformation($"Executing {nameof(UpdateAsync)} for id: {id}");
			if (!ModelState.IsValid)
			{
				var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
				return BadRequest(ApiResponse<SubCategoryDto>.CreateErrorResponse("Check on data", new ErrorResponse("Invalid Data", errors)));
			}
			var userId = HttpContext.Items["UserId"]?.ToString();
			var result = await _subCategoryServices.UpdateAsync(id, subCategoryDto, userId);
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
			var result = await _subCategoryServices.AddMainImageToSubCategoryAsync(id, mainImage.Image, userId);
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
			var result = await _subCategoryServices.AddImagesToSubCategoryAsync(id, images.Images, userId);
			return HandleResult(result, nameof(AddExtraImagesAsync), id);
		}

		[HttpDelete("{subCategoryId}/RemoveImage/{imageId}")]
		[ActionName(nameof(RemoveImageAsync))]
		public async Task<ActionResult<ApiResponse<SubCategoryDto>>> RemoveImageAsync(int subCategoryId, int imageId)
		{
			var userId = HttpContext.Items["UserId"]?.ToString();
			var result = await _subCategoryServices.RemoveImageFromSubCategoryAsync(subCategoryId, imageId, userId);
			return HandleResult(result, nameof(RemoveImageAsync), subCategoryId);
		}

		[HttpPatch("{subCategoryId}/ChangeActiveStatus")]
		[ActionName(nameof(ChangeActiveStatus))]
		public async Task<ActionResult<ApiResponse<bool>>> ChangeActiveStatus(int subCategoryId)
		{
			_logger.LogInformation($"Executing {nameof(ChangeActiveStatus)} for subCategoryId: {subCategoryId}");
			var userId = HttpContext.Items["UserId"]?.ToString();
			var result = await _subCategoryServices.ChangeActiveStatus(subCategoryId, userId);
			return HandleResult(result, nameof(ChangeActiveStatus), subCategoryId);
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