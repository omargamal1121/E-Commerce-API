using E_Commers.DtoModels;
using E_Commers.DtoModels.AccountDtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using E_Commers.Interfaces;
using E_Commers.Services.AccountServices;
using E_Commers.DtoModels.Responses;
using E_Commers.DtoModels.TokenDtos;
using E_Commers.ErrorHnadling;
using Microsoft.AspNetCore.Identity.UI.Services;
using System.ComponentModel.DataAnnotations;
using E_Commers.Services.EmailServices;
using Microsoft.AspNetCore.RateLimiting;
using E_Commers.Services.AccountServices.Shared;
using E_Commers.Services;

namespace E_Commers.Controllers
{
	/// <summary>
	/// Controller for handling user account operations
	/// </summary>
	[Route("api/[controller]")]
	[ApiController]
	public class AccountController : ControllerBase
	{
		private readonly ILogger<AccountController> _logger;
		private readonly IAccountServices _accountServices;
	
		private readonly IAccountLinkBuilder _linkBuilder;
		private readonly IErrorNotificationService _errorNotificationService;

		public AccountController(
	
			IAccountLinkBuilder linkBuilder, 
			IAccountServices accountServices,
			ILogger<AccountController> logger,
			IErrorNotificationService errorNotificationService)
		{
			
			_linkBuilder = linkBuilder ?? throw new ArgumentNullException(nameof(linkBuilder));
			_accountServices = accountServices ?? throw new ArgumentNullException(nameof(accountServices));
			_logger = logger ?? throw new ArgumentNullException(nameof(logger));
			_errorNotificationService = errorNotificationService ?? throw new ArgumentNullException(nameof(errorNotificationService));
		}

		/// <summary>
		/// Authenticates a user and returns JWT tokens
		/// </summary>
		[EnableRateLimiting("login")]
		[HttpPost("login")]
		[ActionName(nameof(LoginAsync))]
		[ProducesResponseType(typeof(ApiResponse<TokensDto>), StatusCodes.Status200OK)]
		[ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status400BadRequest)]
		[ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status401Unauthorized)]
		[ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status500InternalServerError)]
		public async Task<ActionResult<ApiResponse<TokensDto>>> LoginAsync([FromBody] LoginDTo login)
		{
			try
			{
				if (!ModelState.IsValid)
				{
					var errors = GetModelErrors();
					_logger.LogWarning($"ModelState errors: {string.Join(", ", errors)}");
					return BadRequest(ApiResponse<string>.CreateErrorResponse("Invalid Data", new ErrorResponse("Invalid Data", errors), 400));
				}

				_logger.LogInformation($"In {nameof(LoginAsync)} Method ");
				var result = await _accountServices.LoginAsync(login.Email, login.Password);
				return HandleResult<TokensDto>(result, nameof(LoginAsync));
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, $"Error in {nameof(LoginAsync)}");
				await _errorNotificationService.SendErrorNotificationAsync(ex.Message, ex.StackTrace);
				return StatusCode(500, ApiResponse<TokensDto>.CreateErrorResponse("Server Error", new ErrorResponse("Server Error", "An unexpected error occurred during login."), 500));
			}
		}

		/// <summary>
		/// Registers a new user account
		/// </summary>
		[EnableRateLimiting("register")]
		[HttpPost("register")]
		[ActionName(nameof(RegisterAsync))]
		[ProducesResponseType(typeof(ApiResponse<RegisterResponse>), StatusCodes.Status201Created)]
		[ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status400BadRequest)]
		[ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status409Conflict)]
		[ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status500InternalServerError)]
		public async Task<ActionResult<ApiResponse<RegisterResponse>>> RegisterAsync([FromBody] RegisterDto usermodel)
		{
			try
			{
				_logger.LogInformation($"In {nameof(RegisterAsync)} Method ");
				if (!ModelState.IsValid)
				{
					var errors = GetModelErrors();
					_logger.LogWarning($"ModelState errors: {string.Join(", ", errors)}");
					return BadRequest(ApiResponse<RegisterResponse>.CreateErrorResponse("Invalid Data", new ErrorResponse($"Invalid Data", errors), 400));
				}

				var result = await _accountServices.RegisterAsync(usermodel);
				return HandleResult<RegisterResponse>(result, actionName: nameof(RegisterAsync));
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, $"Error in {nameof(RegisterAsync)}");
				await _errorNotificationService.SendErrorNotificationAsync(ex.Message, ex.StackTrace);
				return StatusCode(500, ApiResponse<RegisterResponse>.CreateErrorResponse("Server Error", new ErrorResponse("Server Error", "An unexpected error occurred during registration."), 500));
			}
		}

		/// <summary>
		/// Refreshes the JWT token using a refresh token
		/// </summary>
		[HttpPost("refresh-token")]
		[ActionName(nameof(RefreshTokenAsync))]
		[ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status200OK)]
		[ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status400BadRequest)]
		[ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status401Unauthorized)]
		[ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status500InternalServerError)]
		public async Task<ActionResult<ApiResponse<string>>> RefreshTokenAsync([FromBody] RefreshTokenDto refreshTokenDto)
		{
			try
			{
				_logger.LogInformation($"In {nameof(RefreshTokenAsync)} Method");
				if (!ModelState.IsValid)
				{
					var errors = GetModelErrors();
					_logger.LogWarning($"ModelState errors: {string.Join(", ", errors)}");
					return BadRequest(ApiResponse<string>.CreateErrorResponse("Invalid Data", new ErrorResponse("Invalid Data", errors), 400));
				}

				var result = await _accountServices.RefreshTokenAsync(refreshTokenDto.UserId.ToString(), refreshTokenDto.RefreshToken);
				return HandleResult<string>(result, nameof(RefreshTokenAsync));
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, $"Error in {nameof(RefreshTokenAsync)}");
				await _errorNotificationService.SendErrorNotificationAsync(ex.Message, ex.StackTrace);
				return StatusCode(500, ApiResponse<string>.CreateErrorResponse("Server Error", new ErrorResponse("Server Error", "An unexpected error occurred during token refresh."), 500));
			}
		}

		/// <summary>
		/// Changes the user's password
		/// </summary>
		[HttpPatch("change-password")]
		[ActionName(nameof(ChangePasswordAsync))]
		[Authorize]
		[ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status200OK)]
		[ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status400BadRequest)]
		[ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status401Unauthorized)]
		[ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status500InternalServerError)]
		public async Task<ActionResult<ApiResponse<string>>> ChangePasswordAsync([FromBody] ChangePasswordDto model)
		{
			try
			{
				_logger.LogInformation($"In {nameof(ChangePasswordAsync)} Method");
				if (!ModelState.IsValid)
				{
					var errors = GetModelErrors();
					_logger.LogWarning($"ModelState errors: {string.Join(", ", errors)}");
					return BadRequest(ApiResponse<string>.CreateErrorResponse("Invalid Data", new ErrorResponse("Invalid Data", errors), 400));
				}

				string? userid = GetIdFromToken();
				if (userid.IsNullOrEmpty())
				{
					_logger.LogError("Can't find userid in token");
					return Unauthorized(ApiResponse<string>.CreateErrorResponse("Authorization", new ErrorResponse("Authorization", "Can't find userid in token"), 401));
				}

				var result = await _accountServices.ChangePasswordAsync(userid, model.CurrentPass, model.NewPass);
				return HandleResult<string>(result, nameof(ChangePasswordAsync));
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, $"Error in {nameof(ChangePasswordAsync)}");
				await _errorNotificationService.SendErrorNotificationAsync(ex.Message, ex.StackTrace);
				return StatusCode(500, ApiResponse<string>.CreateErrorResponse("Server Error", new ErrorResponse("Server Error", "An unexpected error occurred during password change."), 500));
			}
		}

		/// <summary>
		/// Changes the user's email address
		/// </summary>
		[Authorize]
		[HttpPatch("change-email")]
		[ActionName(nameof(ChangeEmailAsync))]
		[ProducesResponseType(typeof(ApiResponse<ChangeEmailResultDto>), StatusCodes.Status200OK)]
		[ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status400BadRequest)]
		[ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status401Unauthorized)]
		[ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status409Conflict)]
		[ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status500InternalServerError)]
		private async Task<ActionResult<ApiResponse<ChangeEmailResultDto>>> ChangeEmailAsync([FromBody] ChangeEmailDto newemail)
		{
			try
			{
				if (!ModelState.IsValid)
				{
					var errors = GetModelErrors();
					_logger.LogWarning($"ModelState errors: {string.Join(", ", errors)}");
					return BadRequest(ApiResponse<string>.CreateErrorResponse("Invalid Data", new ErrorResponse("Invalid Data", $"errors:{string.Join(", ", errors)}"), 400));
				}
				string userid=HttpContext
					.Items["UserId"]?.ToString();
				_logger.LogInformation($"In {nameof(ChangeEmailAsync)} Method");
				var result = await _accountServices.ChangeEmailAsync(newemail.Email,userid);
				return HandleResult<ChangeEmailResultDto>(result, nameof(ChangeEmailAsync)) ;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, $"Error in {nameof(ChangeEmailAsync)}");
				await _errorNotificationService.SendErrorNotificationAsync(ex.Message, ex.StackTrace);
				return StatusCode(500, ApiResponse<ChangeEmailResultDto>.CreateErrorResponse("Server Error", new ErrorResponse("Server Error", "An unexpected error occurred during email change."), 500));
			}
		}

		/// <summary>
		/// Logs out the current user
		/// </summary>
		[Authorize]
		[HttpPost("Logout")]
		[ActionName(nameof(LogoutAsync))]
		[ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status200OK)]
		[ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status401Unauthorized)]
		[ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status500InternalServerError)]
		public async Task<ActionResult<ApiResponse<string>>> LogoutAsync()
		{
			try
			{
				_logger.LogInformation($"In {nameof(LogoutAsync)} Method");

				string? userid = GetIdFromToken();
				if (userid.IsNullOrEmpty())
				{
					_logger.LogError("Can't find userid in token");
					return Unauthorized(ApiResponse<string>.CreateErrorResponse("Authorization", new ErrorResponse("Authorization", "Can't find userid in token"), 401));
				}

				var result = await _accountServices.LogoutAsync(userid);
				return HandleResult<string>(result, nameof(LogoutAsync));
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, $"Error in {nameof(LogoutAsync)}");
				await _errorNotificationService.SendErrorNotificationAsync(ex.Message, ex.StackTrace);
				return StatusCode(500, ApiResponse<string>.CreateErrorResponse("Server Error", new ErrorResponse("Server Error", "An unexpected error occurred during logout."), 500));
			}
		}

		/// <summary>
		/// Deletes the current user's account
		/// </summary>
		[Authorize]
		[HttpDelete("delete-account")]
		[ActionName(nameof(DeleteAsync))]
		[ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status200OK)]
		[ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status401Unauthorized)]
		[ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status500InternalServerError)]
		public async Task<ActionResult<ApiResponse<string>>> DeleteAsync()
		{
			try
			{
				_logger.LogInformation($"In {nameof(DeleteAsync)} Method");
				string? userid = GetIdFromToken();
				if (userid.IsNullOrEmpty())
				{
					_logger.LogError("Can't Get Userid from token");
					return Unauthorized(ApiResponse<string>.CreateErrorResponse("Authorization", new ErrorResponse("Authorization", "Can't found userid in token"), 401));
				}
				var result = await _accountServices.DeleteAsync(userid);
				return HandleResult<string>(result, nameof(DeleteAsync));
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, $"Error in {nameof(DeleteAsync)}");
				await _errorNotificationService.SendErrorNotificationAsync(ex.Message, ex.StackTrace);
				return StatusCode(500, ApiResponse<string>.CreateErrorResponse("Server Error", new ErrorResponse("Server Error", "An unexpected error occurred during account deletion."), 500));
			}
		}

		/// <summary>
		/// Uploads a profile photo for the current user
		/// </summary>
		[Authorize]
		[HttpPatch("upload-photo")]
		[ActionName(nameof(UploadPhotoAsync))]
		[ProducesResponseType(typeof(ApiResponse<UploadPhotoResponseDto>), StatusCodes.Status200OK)]
		[ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status400BadRequest)]
		[ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status401Unauthorized)]
		[ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status500InternalServerError)]
		public async Task<ActionResult<ApiResponse<UploadPhotoResponseDto>>> UploadPhotoAsync([FromForm] UploadPhotoDto image)
		{
			try
			{
				if (!ModelState.IsValid)
				{
					var errors = GetModelErrors();
					_logger.LogWarning($"ModelState errors: {string.Join(", ", errors)}");
					return BadRequest(ApiResponse<string>.CreateErrorResponse("Invalid Data", new ErrorResponse("Invalid Data", $"errors:{string.Join(", ", errors)}"), 400));
				}

				_logger.LogInformation($"Executing {nameof(UploadPhotoAsync)}");
				string? id = GetIdFromToken();
				if (id.IsNullOrEmpty())
				{
					_logger.LogError("Can't find userid in token");
					return Unauthorized(ApiResponse<string>.CreateErrorResponse("Authorization", new ErrorResponse("Authorization", "Can't find userid in token"), 401));
				}

				var result = await _accountServices.UploadPhotoAsync(image.image, id);
				return HandleResult<UploadPhotoResponseDto>(result, nameof(UploadPhotoAsync));
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, $"Error in {nameof(UploadPhotoAsync)}");
				await _errorNotificationService.SendErrorNotificationAsync(ex.Message, ex.StackTrace);
				return StatusCode(500, ApiResponse<UploadPhotoResponseDto>.CreateErrorResponse("Server Error", new ErrorResponse("Server Error", "An unexpected error occurred during photo upload."), 500));
			}
		}

		/// <summary>
		/// Confirms a user's email address
		/// </summary>
		[HttpPost("confirm-email")]
		[ActionName(nameof(ConfirmEmailAsync))]
		[ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status200OK)]
		[ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status400BadRequest)]
		[ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status404NotFound)]
		[ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status500InternalServerError)]
		[Authorize]
		public async Task<ActionResult<ApiResponse<string>>> ConfirmEmailAsync([FromBody] ConfirmEmailDto model)
		{
			try
			{
				_logger.LogInformation($"Executing {nameof(ConfirmEmailAsync)}");
				if (!ModelState.IsValid)
				{
					var errors = GetModelErrors();
					_logger.LogWarning($"ModelState errors: {string.Join(", ", errors)}");
					return BadRequest(ApiResponse<string>.CreateErrorResponse("Invalid Data", new ErrorResponse("Invalid Data", $"errors:{string.Join(", ", errors)}"), 400));
				}
				

				var result = await _accountServices.ConfirmEmailAsync(HttpContext.Items["UserId"].ToString(), model.Token);
				return HandleResult<string>(result, nameof(ConfirmEmailAsync));
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, $"Error in {nameof(ConfirmEmailAsync)}");
				await _errorNotificationService.SendErrorNotificationAsync(ex.Message, ex.StackTrace);
				return StatusCode(500, ApiResponse<string>.CreateErrorResponse("Server Error", new ErrorResponse("Server Error", "An unexpected error occurred during email confirmation."), 500));
			}
		}

		/// <summary>
		/// Resends the email confirmation link
		/// </summary>
		[HttpPost("resend-confirmation-email")]
		[ActionName(nameof(ResendConfirmationEmailAsync))]
		[ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status200OK)]
		[ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status400BadRequest)]
		[ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status404NotFound)]
		[ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status500InternalServerError)]
		public async Task<ActionResult<ApiResponse<string>>> ResendConfirmationEmailAsync([FromBody] ResendConfirmationEmailDto model)
		{
			try
			{
				_logger.LogInformation($"Executing {nameof(ResendConfirmationEmailAsync)}");
				if (!ModelState.IsValid)
				{
					var errors = GetModelErrors();
					_logger.LogWarning($"ModelState errors: {string.Join(", ", errors)}");
					return BadRequest(ApiResponse<string>.CreateErrorResponse("Invalid Data", new ErrorResponse("Invalid Data", $"errors:{string.Join(", ", errors)}"), 400));
				}

				var result = await _accountServices.ResendConfirmationEmailAsync(model.Email);
				return HandleResult<string>(result, nameof(ResendConfirmationEmailAsync));
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, $"Error in {nameof(ResendConfirmationEmailAsync)}");
				await _errorNotificationService.SendErrorNotificationAsync(ex.Message, ex.StackTrace);
				return StatusCode(500, ApiResponse<string>.CreateErrorResponse("Server Error", new ErrorResponse("Server Error", "An unexpected error occurred while resending confirmation email."), 500));
			}
		}

		/// <summary>
		/// Requests a password reset by sending a reset token to the user's email
		/// </summary>
		[EnableRateLimiting("reset")]
		[HttpPost("request-password-reset")]
		[ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status200OK)]
		[ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status400BadRequest)]
		public async Task<ActionResult<ApiResponse<string>>> RequestPasswordReset([FromBody] RequestPasswordResetDto dto)
		{
			try
			{
				if (!ModelState.IsValid)
				{
					var errors = GetModelErrors();
					return BadRequest(ApiResponse<string>.CreateErrorResponse("Invalid Data", new ErrorResponse("Invalid Data", errors), 400));
				}
				var result = await _accountServices.RequestPasswordResetAsync(dto.Email);
				return HandleResult<string>(result, nameof(RequestPasswordReset));
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, $"Error in {nameof(RequestPasswordReset)}");
				await _errorNotificationService.SendErrorNotificationAsync(ex.Message, ex.StackTrace);
				return StatusCode(500, ApiResponse<string>.CreateErrorResponse("Server Error", new ErrorResponse("Server Error", "An unexpected error occurred while requesting password reset."), 500));
			}
		}

		/// <summary>
		/// Resets the user's password using a reset token
		/// </summary>
		[EnableRateLimiting("reset")]
		[HttpPost("reset-password")]
		[ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status200OK)]
		[ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status400BadRequest)]
		public async Task<ActionResult<ApiResponse<string>>> ResetPassword([FromBody] ResetPasswordDto dto)
		{
			try
			{
				if (!ModelState.IsValid)
				{
					var errors = GetModelErrors();
					return BadRequest(ApiResponse<string>.CreateErrorResponse("Invalid Data", new ErrorResponse("Invalid Data", errors), 400));
				}
				var result = await _accountServices.ResetPasswordAsync(dto.Email, dto.Token, dto.NewPassword);
				return HandleResult<string>(result, nameof(ResetPassword));
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, $"Error in {nameof(ResetPassword)}");
				await _errorNotificationService.SendErrorNotificationAsync(ex.Message, ex.StackTrace);
				return StatusCode(500, ApiResponse<string>.CreateErrorResponse("Server Error", new ErrorResponse("Server Error", "An unexpected error occurred while resetting password."), 500));
			}
		}

		private string? GetIdFromToken()
		{
			return HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
		}

		private string? GetEmailFromToken()
		{
			return HttpContext.User.FindFirstValue(ClaimTypes.Email);
		}

		private ActionResult<ApiResponse<T>> HandleResult<T>(Result<T> result, string? actionName = null, int? id = null) where T : class
		{
			var links = _linkBuilder.MakeRelSelf(_linkBuilder.GenerateLinks(id), actionName);
			ApiResponse<T> apiResponse;
			if (result.Success)
			{
				apiResponse = ApiResponse<T>.CreateSuccessResponse(result.Message, result.Data, result.StatusCode, warnings: result.Warnings, links: links);
			}
			else
			{
				ErrorResponse errorResponse = (result.Warnings != null && result.Warnings.Count > 0)
					? new ErrorResponse("Error", result.Warnings)
					: new ErrorResponse("Error", result.Message);
				apiResponse = ApiResponse<T>.CreateErrorResponse("Error", errorResponse, result.StatusCode, links);
			}
			if (apiResponse.ResponseBody != null && _linkBuilder != null && id.HasValue && actionName != null)
			{
				apiResponse.ResponseBody.Links = links;
			}
			return result.StatusCode switch
			{
				200 => Ok(apiResponse),
				201 => Created(string.Empty, apiResponse),
				400 => BadRequest(apiResponse),
				401 => Unauthorized(apiResponse),
				409 => Conflict(apiResponse),
				_ => StatusCode(result.StatusCode, apiResponse),
			};
		}

		private List<string> GetModelErrors()
		{
			return ModelState.Values
				.SelectMany(v => v.Errors)
				.Select(e => e.ErrorMessage)
				.ToList();
		}
	}
}
