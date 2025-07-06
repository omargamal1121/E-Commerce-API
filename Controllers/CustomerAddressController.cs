using E_Commers.DtoModels.CustomerAddressDtos;
using E_Commers.DtoModels.Responses;
using E_Commers.ErrorHnadling;
using E_Commers.Interfaces;
using E_Commers.Services;
using E_Commers.Services.EmailServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace E_Commers.Controllers
{
	[ApiController]
	[Route("api/[controller]")]
	[Authorize]
	public class CustomerAddressController : ControllerBase
	{
		private readonly ILogger<CustomerAddressController> _logger;
		private readonly ICustomerAddressServices _addressServices;
		private readonly IErrorNotificationService _errorNotificationService;

		public CustomerAddressController(
			ILogger<CustomerAddressController> logger,
			ICustomerAddressServices addressServices,
			IErrorNotificationService errorNotificationService)
		{
			_logger = logger;
			_addressServices = addressServices;
			_errorNotificationService = errorNotificationService;
		}

		private string GetUserId()
		{
			return HttpContext.Items["UserId"]?.ToString() ?? 
				   User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? 
				   throw new UnauthorizedAccessException("User ID not found");
		}

		private string GetUserRole()
		{
			return User.FindFirst(ClaimTypes.Role)?.Value ?? "User";
		}

		private List<string> GetModelErrors()
		{
			return ModelState.Values
				.SelectMany(v => v.Errors)
				.Select(e => e.ErrorMessage)
				.ToList();
		}

		private ActionResult<ApiResponse<T>> HandleResult<T>(Result<T> result, string? actionName = null, int? id = null)
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

		/// <summary>
		/// Get all addresses for the current customer
		/// </summary>
		[HttpGet]
		[ActionName(nameof(GetCustomerAddresses))]
		[ProducesResponseType(typeof(ApiResponse<List<CustomerAddressDto>>), StatusCodes.Status200OK)]
		[ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status401Unauthorized)]
		[ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status500InternalServerError)]
		public async Task<ActionResult<ApiResponse<List<CustomerAddressDto>>>> GetCustomerAddresses()
		{
			try
			{
				_logger.LogInformation("Executing GetCustomerAddresses");
				var userId = GetUserId();
				var result = await _addressServices.GetCustomerAddressesAsync(userId);
				return HandleResult(result, nameof(GetCustomerAddresses));
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error in GetCustomerAddresses");
				await _errorNotificationService.SendErrorNotificationAsync(ex.Message, ex.StackTrace);
				return StatusCode(500, ApiResponse<List<CustomerAddressDto>>.CreateErrorResponse("Server Error", new ErrorResponse("Server Error", "An error occurred while retrieving addresses"), 500));
			}
		}

		/// <summary>
		/// Get a specific address by ID
		/// </summary>
		[HttpGet("{addressId}")]
		[ActionName(nameof(GetAddressById))]
		[ProducesResponseType(typeof(ApiResponse<CustomerAddressDto>), StatusCodes.Status200OK)]
		[ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status404NotFound)]
		[ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status401Unauthorized)]
		[ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status403Forbidden)]
		[ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status500InternalServerError)]
		public async Task<ActionResult<ApiResponse<CustomerAddressDto>>> GetAddressById(int addressId)
		{
			try
			{
				_logger.LogInformation($"Executing GetAddressById for address ID: {addressId}");
				var userId = GetUserId();
				var result = await _addressServices.GetAddressByIdAsync(addressId, userId);
				return HandleResult(result, nameof(GetAddressById), addressId);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, $"Error in GetAddressById for address ID: {addressId}");
				await _errorNotificationService.SendErrorNotificationAsync(ex.Message, ex.StackTrace);
				return StatusCode(500, ApiResponse<CustomerAddressDto>.CreateErrorResponse("Server Error", new ErrorResponse("Server Error", "An error occurred while retrieving address"), 500));
			}
		}

		/// <summary>
		/// Get the default address for the current customer
		/// </summary>
		[HttpGet("default")]
		[ActionName(nameof(GetDefaultAddress))]
		[ProducesResponseType(typeof(ApiResponse<CustomerAddressDto>), StatusCodes.Status200OK)]
		[ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status404NotFound)]
		[ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status401Unauthorized)]
		[ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status500InternalServerError)]
		public async Task<ActionResult<ApiResponse<CustomerAddressDto>>> GetDefaultAddress()
		{
			try
			{
				_logger.LogInformation("Executing GetDefaultAddress");
				var userId = GetUserId();
				var result = await _addressServices.GetDefaultAddressAsync(userId);
				return HandleResult(result, nameof(GetDefaultAddress));
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error in GetDefaultAddress");
				await _errorNotificationService.SendErrorNotificationAsync(ex.Message, ex.StackTrace);
				return StatusCode(500, ApiResponse<CustomerAddressDto>.CreateErrorResponse("Server Error", new ErrorResponse("Server Error", "An error occurred while retrieving default address"), 500));
			}
		}

		/// <summary>
		/// Create a new address for the current customer
		/// </summary>
		[HttpPost]
		[ActionName(nameof(CreateAddress))]
		[ProducesResponseType(typeof(ApiResponse<CustomerAddressDto>), StatusCodes.Status201Created)]
		[ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status400BadRequest)]
		[ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status401Unauthorized)]
		[ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status500InternalServerError)]
		public async Task<ActionResult<ApiResponse<CustomerAddressDto>>> CreateAddress([FromBody] CreateCustomerAddressDto addressDto)
		{
			try
			{
				_logger.LogInformation("Executing CreateAddress");
				
				if (!ModelState.IsValid)
				{
					var errors = GetModelErrors();
					_logger.LogWarning($"ModelState errors: {string.Join(", ", errors)}");
					return BadRequest(ApiResponse<CustomerAddressDto>.CreateErrorResponse("Invalid Data", new ErrorResponse("Invalid Data", errors), 400));
				}

				var userId = GetUserId();
				var result = await _addressServices.CreateAddressAsync(addressDto, userId);
				return HandleResult(result, nameof(CreateAddress));
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error in CreateAddress");
				await _errorNotificationService.SendErrorNotificationAsync(ex.Message, ex.StackTrace);
				return StatusCode(500, ApiResponse<CustomerAddressDto>.CreateErrorResponse("Server Error", new ErrorResponse("Server Error", "An error occurred while creating address"), 500));
			}
		}

		/// <summary>
		/// Update an existing address
		/// </summary>
		[HttpPut("{addressId}")]
		[ActionName(nameof(UpdateAddress))]
		[ProducesResponseType(typeof(ApiResponse<CustomerAddressDto>), StatusCodes.Status200OK)]
		[ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status400BadRequest)]
		[ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status404NotFound)]
		[ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status401Unauthorized)]
		[ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status403Forbidden)]
		[ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status500InternalServerError)]
		public async Task<ActionResult<ApiResponse<CustomerAddressDto>>> UpdateAddress(int addressId, [FromBody] UpdateCustomerAddressDto addressDto)
		{
			try
			{
				_logger.LogInformation($"Executing UpdateAddress for address ID: {addressId}");
				
				if (!ModelState.IsValid)
				{
					var errors = GetModelErrors();
					_logger.LogWarning($"ModelState errors: {string.Join(", ", errors)}");
					return BadRequest(ApiResponse<CustomerAddressDto>.CreateErrorResponse("Invalid Data", new ErrorResponse("Invalid Data", errors), 400));
				}

				var userId = GetUserId();
				var result = await _addressServices.UpdateAddressAsync(addressId, addressDto, userId);
				return HandleResult(result, nameof(UpdateAddress), addressId);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, $"Error in UpdateAddress for address ID: {addressId}");
				await _errorNotificationService.SendErrorNotificationAsync(ex.Message, ex.StackTrace);
				return StatusCode(500, ApiResponse<CustomerAddressDto>.CreateErrorResponse("Server Error", new ErrorResponse("Server Error", "An error occurred while updating address"), 500));
			}
		}

		/// <summary>
		/// Delete an address
		/// </summary>
		[HttpDelete("{addressId}")]
		[ActionName(nameof(DeleteAddress))]
		[ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status200OK)]
		[ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status404NotFound)]
		[ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status401Unauthorized)]
		[ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status500InternalServerError)]
		public async Task<ActionResult<ApiResponse<string>>> DeleteAddress(int addressId)
		{
			try
			{
				_logger.LogInformation($"Executing DeleteAddress for address ID: {addressId}");
				var userId = GetUserId();
				var result = await _addressServices.DeleteAddressAsync(addressId, userId);
				return HandleResult(result, nameof(DeleteAddress), addressId);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, $"Error in DeleteAddress for address ID: {addressId}");
				await _errorNotificationService.SendErrorNotificationAsync(ex.Message, ex.StackTrace);
				return StatusCode(500, ApiResponse<string>.CreateErrorResponse("Server Error", new ErrorResponse("Server Error", "An error occurred while deleting address"), 500));
			}
		}

		/// <summary>
		/// Set an address as default
		/// </summary>
		[HttpPost("{addressId}/set-default")]
		[ActionName(nameof(SetDefaultAddress))]
		[ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status200OK)]
		[ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status404NotFound)]
		[ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status401Unauthorized)]
		[ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status500InternalServerError)]
		public async Task<ActionResult<ApiResponse<string>>> SetDefaultAddress(int addressId)
		{
			try
			{
				_logger.LogInformation($"Executing SetDefaultAddress for address ID: {addressId}");
				var userId = GetUserId();
				var result = await _addressServices.SetDefaultAddressAsync(addressId, userId);
				return HandleResult(result, nameof(SetDefaultAddress), addressId);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, $"Error in SetDefaultAddress for address ID: {addressId}");
				await _errorNotificationService.SendErrorNotificationAsync(ex.Message, ex.StackTrace);
				return StatusCode(500, ApiResponse<string>.CreateErrorResponse("Server Error", new ErrorResponse("Server Error", "An error occurred while setting default address"), 500));
			}
		}

		/// <summary>
		/// Get addresses by type (Home, Work, Other)
		/// </summary>
		[HttpGet("type/{addressType}")]
		[ActionName(nameof(GetAddressesByType))]
		[ProducesResponseType(typeof(ApiResponse<List<CustomerAddressDto>>), StatusCodes.Status200OK)]
		[ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status401Unauthorized)]
		[ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status500InternalServerError)]
		public async Task<ActionResult<ApiResponse<List<CustomerAddressDto>>>> GetAddressesByType(string addressType)
		{
			try
			{
				_logger.LogInformation($"Executing GetAddressesByType for type: {addressType}");
				var userId = GetUserId();
				var result = await _addressServices.GetAddressesByTypeAsync(addressType, userId);
				return HandleResult(result, nameof(GetAddressesByType));
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, $"Error in GetAddressesByType for type: {addressType}");
				await _errorNotificationService.SendErrorNotificationAsync(ex.Message, ex.StackTrace);
				return StatusCode(500, ApiResponse<List<CustomerAddressDto>>.CreateErrorResponse("Server Error", new ErrorResponse("Server Error", "An error occurred while retrieving addresses by type"), 500));
			}
		}

		/// <summary>
		/// Search addresses by term
		/// </summary>
		[HttpGet("search")]
		[ActionName(nameof(SearchAddresses))]
		[ProducesResponseType(typeof(ApiResponse<List<CustomerAddressDto>>), StatusCodes.Status200OK)]
		[ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status401Unauthorized)]
		[ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status500InternalServerError)]
		public async Task<ActionResult<ApiResponse<List<CustomerAddressDto>>>> SearchAddresses([FromQuery] string searchTerm)
		{
			try
			{
				_logger.LogInformation($"Executing SearchAddresses with term: {searchTerm}");
				var userId = GetUserId();
				var result = await _addressServices.SearchAddressesAsync(searchTerm, userId);
				return HandleResult(result, nameof(SearchAddresses));
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, $"Error in SearchAddresses with term: {searchTerm}");
				await _errorNotificationService.SendErrorNotificationAsync(ex.Message, ex.StackTrace);
				return StatusCode(500, ApiResponse<List<CustomerAddressDto>>.CreateErrorResponse("Server Error", new ErrorResponse("Server Error", "An error occurred while searching addresses"), 500));
			}
		}

		/// <summary>
		/// Get address count for the current customer
		/// </summary>
		[HttpGet("count")]
		[ActionName(nameof(GetAddressCount))]
		[ProducesResponseType(typeof(ApiResponse<int>), StatusCodes.Status200OK)]
		[ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status401Unauthorized)]
		[ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status500InternalServerError)]
		public async Task<ActionResult<ApiResponse<int?>>> GetAddressCount()
		{
			try
			{
				_logger.LogInformation("Executing GetAddressCount");
				var userId = GetUserId();
				var result = await _addressServices.GetAddressCountAsync(userId);
				return HandleResult(result, nameof(GetAddressCount));
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error in GetAddressCount");
				await _errorNotificationService.SendErrorNotificationAsync(ex.Message, ex.StackTrace);
				return StatusCode(500, ApiResponse<int>.CreateErrorResponse("Server Error", new ErrorResponse("Server Error", "An error occurred while getting address count"), 500));
			}
		}

		/// <summary>
		/// Get address with customer details (Admin only)
		/// </summary>
		[HttpGet("{addressId}/with-customer")]
		[Authorize(Roles = "Admin")]
		[ActionName(nameof(GetAddressWithCustomer))]
		[ProducesResponseType(typeof(ApiResponse<CustomerAddressDto>), StatusCodes.Status200OK)]
		[ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status404NotFound)]
		[ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status401Unauthorized)]
		[ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status403Forbidden)]
		[ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status500InternalServerError)]
		public async Task<ActionResult<ApiResponse<CustomerAddressDto>>> GetAddressWithCustomer(int addressId)
		{
			try
			{
				_logger.LogInformation($"Executing GetAddressWithCustomer for address ID: {addressId}");
				var userRole = GetUserRole();
				var result = await _addressServices.GetAddressWithCustomerAsync(addressId, userRole);
				return HandleResult(result, nameof(GetAddressWithCustomer), addressId);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, $"Error in GetAddressWithCustomer for address ID: {addressId}");
				await _errorNotificationService.SendErrorNotificationAsync(ex.Message, ex.StackTrace);
				return StatusCode(500, ApiResponse<CustomerAddressDto>.CreateErrorResponse("Server Error", new ErrorResponse("Server Error", "An error occurred while retrieving address with customer details"), 500));
			}
		}
	}
} 