using E_Commers.DtoModels;
using E_Commers.DtoModels.Responses;
using E_Commers.ErrorHnadling;
using E_Commers.Interfaces;
using E_Commers.Services.EmailServices;
using Microsoft.AspNetCore.Mvc;

namespace E_Commers.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public abstract class BaseController : ControllerBase
    {
        protected readonly ILogger<BaseController> _logger;
        protected readonly ErrorNotificationService _errorNotificationService;

        protected BaseController(
            ILogger<BaseController> logger,
            ErrorNotificationService errorNotificationService)
        {
            _logger = logger;
            _errorNotificationService = errorNotificationService;
        }

  //      protected async Task<ActionResult<T>> HandleResponse<T>(ApiResponse<T> response, string operationName, object? id = null)where T : class
		//{
  //          if (response.Statuscode==200)
  //          {
  //              return Ok(response);
  //          }

  //          _logger.LogError($"Error in {operationName}: {string.Join(", ", response.ResponseBody?.Errors?.Messages ?? new List<string>())}");
            
  //          // Send error notification
  //          await _errorNotificationService.SendErrorNotificationAsync(
  //              $"Error in {operationName}: {string.Join(", ", response.ResponseBody?.Errors?.Messages ?? new List<string>())}"
  //                      );

  //          return response.Statuscode switch
  //          {
  //              400 => BadRequest(response),
  //              401 => Unauthorized(response),
  //              403 => Forbid(),
  //              404 => NotFound(response),
  //              _ => StatusCode(response.Statuscode, response)
  //          };
  //      }

		protected ActionResult<ApiResponse<T>> HandleResponse<T>(ApiResponse<T> response, string? actionName = null, string? targetrel = null) where T : class
		{
			
			return response.Statuscode switch
			{
				200 => Ok(response),
				201 => CreatedAtAction(actionName, response),
				400 => BadRequest(response),
				401 => Unauthorized(response),
				409 => Conflict(response),
				_ => StatusCode(response.Statuscode, response)
			};
		}

		protected async Task<ActionResult<T>> HandleException<T>(Exception ex, string operationName)where T : class
		{
            _logger.LogError(ex, $"Exception in {operationName}");
            
            // Send error notification
            await _errorNotificationService.SendErrorNotificationAsync(
                $"Exception in {operationName}: {ex.Message}",
                ex.StackTrace
            );

            var errorResponse = new ErrorResponse("Internal Server Error", new List<string> { ex.Message });
            return StatusCode(500, ApiResponse<T>.CreateErrorResponse(errorResponse, 500));
        }
    }
} 