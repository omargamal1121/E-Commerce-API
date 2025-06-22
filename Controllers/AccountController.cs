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
					return BadRequest(ApiResponse<string>.CreateErrorResponse(new ErrorResponse("Invalid Data", errors)));
				}

				_logger.LogInformation($"In {nameof(LoginAsync)} Method ");
				var response = await _accountServices.LoginAsync(login.Email, login.Password);
				response.ResponseBody.Links = _linkBuilder.GenerateLinks();
				response.ResponseBody.Links = _linkBuilder.MakeRelSelf(response.ResponseBody.Links, "login");
				return HandleResponse(response, nameof(LoginAsync));
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, $"Error in {nameof(LoginAsync)}");
				await _errorNotificationService.SendErrorNotificationAsync(ex.Message, ex.StackTrace);
				return StatusCode(500, ApiResponse<TokensDto>.CreateErrorResponse(
					new ErrorResponse("Server Error", "An unexpected error occurred during login.")));
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
					return BadRequest(ApiResponse<RegisterResponse>.CreateErrorResponse(new ErrorResponse($"Invalid Data", errors), 400));
				}

				var response = await _accountServices.RegisterAsync(usermodel);
				return HandleResponse(response, actionName: nameof(RegisterAsync), "register");
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, $"Error in {nameof(RegisterAsync)}");
				await _errorNotificationService.SendErrorNotificationAsync(ex.Message, ex.StackTrace);
				return StatusCode(500, ApiResponse<RegisterResponse>.CreateErrorResponse(
					new ErrorResponse("Server Error", "An unexpected error occurred during registration.")));
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
					return BadRequest(ApiResponse<string>.CreateErrorResponse(new ErrorResponse("Invalid Data", errors)));
				}

				var response = await _accountServices.RefreshTokenAsync(refreshTokenDto.UserId.ToString(), refreshTokenDto.RefreshToken);
				response.ResponseBody.Links = _linkBuilder.GenerateLinks();
				response.ResponseBody.Links = _linkBuilder.MakeRelSelf(response.ResponseBody.Links, "refresh-token");
				return HandleResponse(response, targetrel: "refresh-token");
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, $"Error in {nameof(RefreshTokenAsync)}");
				await _errorNotificationService.SendErrorNotificationAsync(ex.Message, ex.StackTrace);
				return StatusCode(500, ApiResponse<string>.CreateErrorResponse(
					new ErrorResponse("Server Error", "An unexpected error occurred during token refresh.")));
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
					return BadRequest(ApiResponse<string>.CreateErrorResponse(new ErrorResponse("Invalid Data", errors)));
				}

				string? userid = GetIdFromToken();
				if (userid.IsNullOrEmpty())
				{
					_logger.LogError("Can't find userid in token");
					return Unauthorized(ApiResponse<string>.CreateErrorResponse(new ErrorResponse("Authorization", "Can't find userid in token")));
				}

				var response = await _accountServices.ChangePasswordAsync(userid, model.CurrentPass, model.NewPass);
				return HandleResponse(response, targetrel: "change-password");
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, $"Error in {nameof(ChangePasswordAsync)}");
				await _errorNotificationService.SendErrorNotificationAsync(ex.Message, ex.StackTrace);
				return StatusCode(500, ApiResponse<string>.CreateErrorResponse(
					new ErrorResponse("Server Error", "An unexpected error occurred during password change.")));
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
		public async Task<ActionResult<ApiResponse<ChangeEmailResultDto>>> ChangeEmailAsync([FromBody] ChangeEmailDto newemail)
		{
			try
			{
				if (!ModelState.IsValid)
				{
					var errors = GetModelErrors();
					_logger.LogWarning($"ModelState errors: {string.Join(", ", errors)}");
					return BadRequest(ApiResponse<string>.CreateErrorResponse(new ErrorResponse("Invalid Data", $"errors:{string.Join(", ", errors)}")));
				}

				_logger.LogInformation($"In {nameof(ChangeEmailAsync)} Method");
				string? email = GetEmailFromToken();
				if (email.IsNullOrEmpty())
				{
					_logger.LogError("Can't find email in token");
					return Unauthorized(ApiResponse<string>.CreateErrorResponse(new ErrorResponse("Authorization", "Can't find email in token")));
				}

				var response = await _accountServices.ChangeEmailAsync(newemail.Email, email);
				return HandleResponse(response, targetrel: "change-email");
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, $"Error in {nameof(ChangeEmailAsync)}");
				await _errorNotificationService.SendErrorNotificationAsync(ex.Message, ex.StackTrace);
				return StatusCode(500, ApiResponse<ChangeEmailResultDto>.CreateErrorResponse(
					new ErrorResponse("Server Error", "An unexpected error occurred during email change.")));
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
					return Unauthorized(ApiResponse<string>.CreateErrorResponse(new ErrorResponse("Authorization", "Can't find userid in token")));
				}

				var response = await _accountServices.LogoutAsync(userid);
				return HandleResponse(response, targetrel: "logout");
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, $"Error in {nameof(LogoutAsync)}");
				await _errorNotificationService.SendErrorNotificationAsync(ex.Message, ex.StackTrace);
				return StatusCode(500, ApiResponse<string>.CreateErrorResponse(
					new ErrorResponse("Server Error", "An unexpected error occurred during logout.")));
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
					return Unauthorized(ApiResponse<string>.CreateErrorResponse(new ErrorResponse("Authorization", "Can't found userid in token")));
				}

				var response = await _accountServices.DeleteAsync(userid);
				response.ResponseBody.Links = _linkBuilder.MakeRelSelf(_linkBuilder.GenerateLinks(), "Delete");
				return HandleResponse(response, "delete", userid);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, $"Error in {nameof(DeleteAsync)}");
				await _errorNotificationService.SendErrorNotificationAsync(ex.Message, ex.StackTrace);
				return StatusCode(500, ApiResponse<string>.CreateErrorResponse(
					new ErrorResponse("Server Error", "An unexpected error occurred during account deletion.")));
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
					return BadRequest(ApiResponse<string>.CreateErrorResponse(new ErrorResponse("Invalid Data", $"errors:{string.Join(", ", errors)}")));
				}

				_logger.LogInformation($"Executing {nameof(UploadPhotoAsync)}");
				string? id = GetIdFromToken();
				if (id.IsNullOrEmpty())
				{
					_logger.LogError("Can't find userid in token");
					return Unauthorized(ApiResponse<string>.CreateErrorResponse(new ErrorResponse("Authorization", "Can't find userid in token")));
				}

				var response = await _accountServices.UploadPhotoAsync(image.image, id);
				return HandleResponse(response, targetrel: "upload-photo");
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, $"Error in {nameof(UploadPhotoAsync)}");
				await _errorNotificationService.SendErrorNotificationAsync(ex.Message, ex.StackTrace);
				return StatusCode(500, ApiResponse<UploadPhotoResponseDto>.CreateErrorResponse(
					new ErrorResponse("Server Error", "An unexpected error occurred during photo upload.")));
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
		public async Task<ActionResult<ApiResponse<string>>> ConfirmEmailAsync([FromBody] ConfirmEmailDto model)
		{
			try
			{
				_logger.LogInformation($"Executing {nameof(ConfirmEmailAsync)}");
				if (!ModelState.IsValid)
				{
					var errors = GetModelErrors();
					_logger.LogWarning($"ModelState errors: {string.Join(", ", errors)}");
					return BadRequest(ApiResponse<string>.CreateErrorResponse(
						new ErrorResponse("Invalid Data", $"errors:{string.Join(", ", errors)}")));
				}

				var response = await _accountServices.ConfirmEmailAsync(model.UserId, model.Token);
				return HandleResponse(response, nameof(ConfirmEmailAsync), "confirm-email");
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, $"Error in {nameof(ConfirmEmailAsync)}");
				await _errorNotificationService.SendErrorNotificationAsync(ex.Message, ex.StackTrace);
				return StatusCode(500, ApiResponse<string>.CreateErrorResponse(
					new ErrorResponse("Server Error", "An unexpected error occurred during email confirmation.")));
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
					return BadRequest(ApiResponse<string>.CreateErrorResponse(
						new ErrorResponse("Invalid Data", $"errors:{string.Join(", ", errors)}")));
				}

				var response = await _accountServices.ResendConfirmationEmailAsync(model.Email);
				return HandleResponse(response, nameof(ResendConfirmationEmailAsync), "resend-confirmation-email");
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, $"Error in {nameof(ResendConfirmationEmailAsync)}");
				await _errorNotificationService.SendErrorNotificationAsync(ex.Message, ex.StackTrace);
				return StatusCode(500, ApiResponse<string>.CreateErrorResponse(
					new ErrorResponse("Server Error", "An unexpected error occurred while resending confirmation email.")));
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
					return BadRequest(ApiResponse<string>.CreateErrorResponse(new ErrorResponse("Invalid Data", errors)));
				}
				var response = await _accountServices.RequestPasswordResetAsync(dto.Email);
				return HandleResponse(response, nameof(RequestPasswordReset));
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, $"Error in {nameof(RequestPasswordReset)}");
				await _errorNotificationService.SendErrorNotificationAsync(ex.Message, ex.StackTrace);
				return StatusCode(500, ApiResponse<string>.CreateErrorResponse(
					new ErrorResponse("Server Error", "An unexpected error occurred while requesting password reset.")));
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
					return BadRequest(ApiResponse<string>.CreateErrorResponse(new ErrorResponse("Invalid Data", errors)));
				}
				var response = await _accountServices.ResetPasswordAsync(dto.Email, dto.Token, dto.NewPassword);
				return HandleResponse(response, nameof(ResetPassword));
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, $"Error in {nameof(ResetPassword)}");
				await _errorNotificationService.SendErrorNotificationAsync(ex.Message, ex.StackTrace);
				return StatusCode(500, ApiResponse<string>.CreateErrorResponse(
					new ErrorResponse("Server Error", "An unexpected error occurred while resetting password.")));
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

		private ActionResult<ApiResponse<T>> HandleResponse<T>(ApiResponse<T> response, string? actionName = null, string? targetrel = null) where T : class
		{
			response.ResponseBody.Links = _linkBuilder.MakeRelSelf(_linkBuilder.GenerateLinks(), actionName);
			return response.Statuscode switch
			{
				200 => Ok(response),
				201 => CreatedAtAction(actionName, response),
				400 => BadRequest(response),
				401 => Unauthorized(response),
				404 => NotFound(response),
				409 => Conflict(response),
				_ => StatusCode(response.Statuscode, response)
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
