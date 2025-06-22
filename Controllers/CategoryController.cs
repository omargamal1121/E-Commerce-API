using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Transactions;
using AutoMapper;
using E_Commers.DtoModels;
using E_Commers.DtoModels.CategoryDtos;
using E_Commers.DtoModels.DiscoutDtos;
using E_Commers.DtoModels.ProductDtos;
using E_Commers.DtoModels.Responses;
using E_Commers.DtoModels.Shared;
using E_Commers.Enums;
using E_Commers.ErrorHnadling;
using E_Commers.Interfaces;
using E_Commers.Models;
using E_Commers.Services;
using E_Commers.Services.Category;
using E_Commers.Services.Product;
using E_Commers.UOW;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace E_Commers.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Admin")]
    public class categoriesController : ControllerBase
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICategoryServices _categoryServices;
        private readonly IProductsServices _productsServices;
        private readonly ILogger<categoriesController> _logger;
        private readonly ICategoryLinkBuilder _linkBuilder;

        public categoriesController(
			IProductsServices productsServices,
			ICategoryLinkBuilder linkBuilder,
            IUnitOfWork unitOfWork,
            ICategoryServices categoryServices,
            ILogger<categoriesController> logger
        )
            
        {
            _productsServices = productsServices;
            _linkBuilder = linkBuilder;
            _categoryServices = categoryServices;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        [HttpGet("{id}")]
        [ActionName(nameof(GetByIdAsync))]
        public async Task<ActionResult<ApiResponse<CategoryDto>>> GetByIdAsync(int id)
        {
			if (!ModelState.IsValid)
			{
				var errors = string.Join(
					"; ",
					ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)
				);
				_logger.LogWarning($"ModelState errors: {errors}");
				return BadRequest(
					ApiResponse<string>.CreateErrorResponse(
						new ErrorResponse("Invalid Data", new List<string> { $"errors:{errors}" })
					)
				);
			}
			_logger.LogInformation(
                $"Executing {nameof(GetByIdAsync)} in CategoryController for id: {id}"
            );

            var response = await _categoryServices.GetCategoryByIdAsync(id);
            return HandleResponse(response, nameof(GetByIdAsync),id);
        }

        [HttpGet]
   
        [ActionName(nameof(GetAllAsync))]
        public async Task<ActionResult<ApiResponse<List<CategoryDto>>>> GetAllAsync()
        {
            _logger.LogInformation($"Executing {nameof(GetAllAsync)} in CategoryController");

            var response = await _categoryServices.GetAllAsync();
            return HandleResponse(response, nameof(GetAllAsync));
        }

        [HttpPost]
        [ActionName(nameof(CreateAsync))]
        public async Task<ActionResult<ApiResponse<CategoryDto>>> CreateAsync(
            [FromBody] CreateCategotyDto categoryDto
        )
        {
            _logger.LogInformation($"Executing {nameof(CreateAsync)} in CategoryController");

            if (!ModelState.IsValid)
            {
                var errors = string.Join(
                    "; ",
                    ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)
                );
                _logger.LogWarning($"ModelState errors: {errors}");
                return BadRequest(
                    ApiResponse<CategoryDto>.CreateErrorResponse(
                        new ErrorResponse("Invalid Data", new List<string> { $"errors:{errors}" })
                    )
                );
            }
			string? userid = GetIdFromToken();
			if (userid == null)
			{
				_logger.LogError("Can't Get Userid From token");
				return ApiResponse<CategoryDto>.CreateErrorResponse(new ErrorResponse("Auth", new List<string> { "UnAuthorized" }), 401);
			}
			var response = await _categoryServices.CreateAsync(categoryDto, userid);
            return HandleResponse(response, nameof(CreateAsync),response.ResponseBody?.Data?.Id);
        }

        [HttpDelete("{id}")]
        [ActionName(nameof(DeleteAsync))]
        public async Task<ActionResult<ApiResponse<string>>> DeleteAsync(int id)
        {
            if (!ModelState.IsValid)
            {
                var errors = string.Join(
                    "; ",
                    ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)
                );
                _logger.LogWarning($"ModelState errors: {errors}");
                return BadRequest(
                    ApiResponse<string>.CreateErrorResponse(
                        new ErrorResponse("Invalid Data", new List<string> { $"errors:{errors}" })
                    )
                );
            }
            _logger.LogInformation(
                $"Executing {nameof(DeleteAsync)} in CategoryController for id: {id}"
            );
            string? userid = GetIdFromToken();
            if (userid == null)
            {
                _logger.LogError("Can't Get Userid From token");
                return ApiResponse<string>.CreateErrorResponse(new ErrorResponse("Auth", new List<string> { "UnAuthorized" }), 401);
            }
            var response = await _categoryServices.DeleteAsync(id,userid);
            return HandleResponse(response, nameof(DeleteAsync));
        }

        [HttpPatch("{id}/Restore")]
        [ActionName(nameof(ReturnRemovedCategoryAsync))]
        public async Task<ActionResult<ApiResponse<CategoryDto>>> ReturnRemovedCategoryAsync(int id)
        {
			if (!ModelState.IsValid)
			{
				var errors = string.Join(
					"; ",
					ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)
				);
				_logger.LogWarning($"ModelState errors: {errors}");
				return BadRequest(
					ApiResponse<string>.CreateErrorResponse(
						new ErrorResponse("Invalid Data", new List<string> { $"errors:{errors}" })
					)
				);
			}
			_logger.LogInformation($"Executing {nameof(ReturnRemovedCategoryAsync)}");

            string? userid =  GetIdFromToken();
			if (userid == null)
			{
				_logger.LogError("Can't Get Userid From token");
				return ApiResponse<CategoryDto>.CreateErrorResponse(new ErrorResponse("Auth", new List<string> { "UnAuthorized" }), 401);
			}

			var response=  await _categoryServices.ReturnRemovedCategoryAsync(id, userid);
            return HandleResponse(response,nameof(ReturnRemovedCategoryAsync));
           
            
         
        }

        [HttpPut("{id}")]
        [ActionName(nameof(UpdateAsync))]
        public async Task<ActionResult<ApiResponse<CategoryDto>>> UpdateAsync(
            int id,
            [FromBody] UpdateCategoryDto categoryDto
        )
        {
            _logger.LogInformation(
                $"Executing {nameof(UpdateAsync)} in CategoryController for id: {id}"
            );

          
            if (!ModelState.IsValid)
            {
                return BadRequest(new ResponseDto { Message = "Invalid model state" });
            }
			string? userid = GetIdFromToken();
			if (userid == null)
			{
				_logger.LogError("Can't Get Userid From token");
				return ApiResponse<CategoryDto>.CreateErrorResponse(new ErrorResponse("Auth", new List<string> { "UnAuthorized" }), 401);
			}
			var response = await _categoryServices.UpdateAsync(id, categoryDto,userid);
            return HandleResponse(response,nameof(UpdateAsync),id);
        }

        [HttpGet("{id}/Produects")]
        [ActionName(nameof(GetProduectsByCategoryIdAsync))]
        [ResponseCache(Duration = 60, VaryByQueryKeys = new string[] { "id" })]
        public async Task<ActionResult<ApiResponse<List<ProductDto>>>> GetProduectsByCategoryIdAsync(int id)
        {
			if (!ModelState.IsValid)
			{
				var errors = string.Join(
					"; ",
					ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)
				);
				_logger.LogWarning($"ModelState errors: {errors}");
				return BadRequest(
					ApiResponse<string>.CreateErrorResponse(
						new ErrorResponse("Invalid Data", new List<string> { $"errors:{errors}" })
					)
				);
			}
			_logger.LogInformation($"Execute:{nameof(GetProduectsByCategoryIdAsync)} with Id:{id}");
            var responce = await _productsServices.GetProductsByCategoryId(id);
            return HandleResponse(responce, nameof(GetAllAsync), id);

        }

        private ActionResult<ApiResponse<T>> HandleResponse<T>(
            ApiResponse<T> response,
            string actionName,
            int? id = null)
            where T : class
        {
          
            response.ResponseBody.Links = _linkBuilder.MakeRelSelf(
                _linkBuilder.GenerateLinks(id),
                actionName
            );
            return response.Statuscode switch
            {
                200 => Ok(response),
                201 => CreatedAtAction(actionName, response),
                400 => BadRequest(response),
                401 => Unauthorized(response),
                409 => Conflict(response),
                _ => StatusCode(response.Statuscode, response),
            };
        }
		private string? GetIdFromToken()
		{
			return HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);

		}
	}
}
