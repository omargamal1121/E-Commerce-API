using AutoMapper;
using E_Commers.DtoModels;
using E_Commers.DtoModels.AccountDtos;
using E_Commers.Services;
using E_Commers.Models;
using E_Commers.UOW;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using E_Commers.Interfaces;
using E_Commers.Services.AccountServices;
using Microsoft.AspNetCore.Http.HttpResults;
using E_Commers.DtoModels.Responses;
using E_Commers.DtoModels.Shared;
using E_Commers.DtoModels.TokenDtos;
using E_Commers.ErrorHnadling;
namespace E_Commers.Controllers
{
	[Route("api/[controller]")]
	[ApiController]
	public class AccountController : ControllerBase
	{
		private readonly ILogger<AccountController> _logger;

		private readonly IAccountServices _accountServices;
		private readonly IAccountLinkBuilder _linkBuilder;

		public AccountController(IAccountLinkBuilder linkBuilder, IAccountServices accountServices ,ILogger<AccountController> logger)
		{
			_linkBuilder = linkBuilder;
			_accountServices = accountServices;
			_logger = logger;

		}

		[HttpPost("login")]
		[ActionName(nameof(LoginAsync))]
		public async Task<ActionResult<ApiResponse<TokensDto>>> LoginAsync([FromBody] LoginDTo login)
		{

			if (!ModelState.IsValid)
			{
				var errors = string.Join("; ", ModelState.Values
											.SelectMany(v => v.Errors)
											.Select(e => e.ErrorMessage));
				_logger.LogWarning($"ModelState errors: {errors}");
				return BadRequest(ApiResponse<string>.CreateErrorResponse(new ErrorResponse("Invalid Data", $"errors:{errors}")));
			}

			_logger.LogInformation($"In {nameof(LoginAsync)} Method ");
			ApiResponse<TokensDto> response = await _accountServices.LoginAsync(login.Email, login.Password);

			response.ResponseBody.Links = _linkBuilder.GenerateLinks();
			response.ResponseBody.Links = _linkBuilder.MakeRelSelf(response.ResponseBody.Links, "login");

			return HandleResponse(response, nameof(LoginAsync));
		}

		[HttpPost("register")]
		[ActionName(nameof(RegisterAsync))]
		public async Task<ActionResult<ApiResponse<RegisterResponse>>> RegisterAsync([FromBody] RegisterDto usermodel)
		{
			_logger.LogInformation($"In {nameof(RegisterAsync)} Method ");
			if (!ModelState.IsValid)
			{
				var errors = string.Join("; ", ModelState.Values
												  .SelectMany(v => v.Errors)
												  .Select(e => e.ErrorMessage));

				_logger.LogWarning($"ModelState errors: {errors}");
				return BadRequest(ApiResponse<RegisterResponse>.CreateErrorResponse(new ErrorResponse($"Invalid Data", $"Errors : {errors}"), 400));
			}
			var response = await _accountServices.RegisterAsync(usermodel);

			return HandleResponse(response, actionName:nameof(RegisterAsync),targetrel:"register");
		}

		[HttpPost("refresh-token")]
		[ActionName(nameof(RefreshTokenAsync))]
		public async Task<ActionResult<ApiResponse<string>>> RefreshTokenAsync([FromBody]RefreshTokenDto refreshTokenDto)
		{
			_logger.LogInformation($"In {nameof(RefreshTokenAsync)} Method");
			if (!ModelState.IsValid)
			{
				var errors = string.Join("; ", ModelState.Values
											.SelectMany(v => v.Errors)
											.Select(e => e.ErrorMessage));
				_logger.LogWarning($"ModelState errors: {errors}");
				return BadRequest(ApiResponse<string>.CreateErrorResponse(new ErrorResponse("Invalid Data", $"errors:{errors}")));
			}

			var response= await	_accountServices.RefreshTokenAsync(refreshTokenDto.UserId.ToString(), refreshTokenDto.RefreshToken);
			response.ResponseBody.Links = _linkBuilder.GenerateLinks();
			response.ResponseBody.Links = _linkBuilder.MakeRelSelf(response.ResponseBody.Links, "refresh-token");
			return HandleResponse(response,targetrel:"refresh-token");

		}
		[HttpPatch("change-password")]
		[ActionName(nameof(ChangePasswordAsync))]
		[Authorize]
		public async Task<ActionResult<ApiResponse<string>>> ChangePasswordAsync([FromBody] ChangePasswordDto model)
		{

			_logger.LogInformation($"In {nameof(ChangePasswordAsync)} Method");
			if (!ModelState.IsValid)
			{
				var errors = string.Join("; ", ModelState.Values
											.SelectMany(v => v.Errors)
											.Select(e => e.ErrorMessage));
				_logger.LogWarning($"ModelState errors: {errors}");
				return BadRequest(ApiResponse<string>.CreateErrorResponse(new ErrorResponse("Invalid Data", $"errors:{errors}")));
			}

			string? userid =GetIdFromToken();
			if (userid.IsNullOrEmpty())
			{
				_logger.LogError("Can't find userid in token");
				//send email
				return Unauthorized(ApiResponse<string>.CreateErrorResponse(new ErrorResponse("Authoriztion", "Can't find userid in token")));
			}

			var response = await _accountServices.ChangePasswordAsync(userid,model.CurrentPass,model.NewPass);
			return HandleResponse(response,targetrel: "change-password");
		}



		[Authorize]
		[HttpPatch("change-email")]
		[ActionName(nameof(ChangeEmailAsync))]
		public async Task<ActionResult<ApiResponse<ChangeEmailResultDto>>> ChangeEmailAsync(ChangeEmailDto newemail)
		{
			if (!ModelState.IsValid)
			{
				var errors = string.Join("; ", ModelState.Values
											.SelectMany(v => v.Errors)
											.Select(e => e.ErrorMessage));
				_logger.LogWarning($"ModelState errors: {errors}");
				return BadRequest(ApiResponse<string>.CreateErrorResponse(new ErrorResponse("Invalid Data",$"errors:{errors}")));
			}
			
			_logger.LogInformation($"In {nameof(ChangeEmailAsync)} Method");
			string? email = GetEmailFromToken();
			if (email.IsNullOrEmpty())
				return Unauthorized();
			var response= await _accountServices.ChangeEmailAsync(newemail.Email, email);

			return HandleResponse(response,targetrel: "change-email");
		}
		private string? GetIdFromToken()
		{
			return HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);

		}
			private string? GetEmailFromToken()
		{
			return HttpContext.User.FindFirstValue(ClaimTypes.Email);

		}

		[Authorize]
		[HttpPost("Logout")]
		[ActionName(nameof(LogoutAsync))]
		public async Task<ActionResult<ApiResponse<string>>> LogoutAsync()
		{
			_logger.LogInformation($"In {nameof(LogoutAsync)} Method");

			string? userid =GetIdFromToken();
			if (userid.IsNullOrEmpty())
			{
				_logger.LogError("Can't find userid in token");
				//send email
				return Unauthorized(ApiResponse<string>.CreateErrorResponse(new ErrorResponse("Authoriztion", "Can't find userid in token")));
			}
			var response = await _accountServices.LogoutAsync(userid);

			return HandleResponse(response,targetrel: "logout");

		}

		[Authorize]
		[HttpDelete("delete-account")]
		[ActionName(nameof(DeleteAsync))]
		public async Task<ActionResult<ApiResponse<string>>> DeleteAsync()
		{
			_logger.LogInformation($"In {nameof(DeleteAsync)} Method");
			string? userid = GetIdFromToken();
			if (userid.IsNullOrEmpty())
			{
				_logger.LogError("Can't Get Userid from token");
				// send email
				return Unauthorized(ApiResponse<string>.CreateErrorResponse(new ErrorResponse("Authoriztion", "Can't found userid in token")));
			}
			var	response= await _accountServices.DeleteAsync(userid);
			response.ResponseBody.Links =_linkBuilder.MakeRelSelf( _linkBuilder.GenerateLinks(),"Delete");
			return HandleResponse(response, "delete",userid);
		}
		[Authorize]
		[HttpPatch("upload-photo")]
		[ActionName(nameof(UploadPhotoAsync))]
		[ProducesResponseType(StatusCodes.Status200OK)]
		[ProducesResponseType(StatusCodes.Status400BadRequest)]
		[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
		[ProducesResponseType(StatusCodes.Status500InternalServerError)]
		public async Task<ActionResult<ApiResponse<UploadPhotoResponseDto>>> UploadPhotoAsync([FromForm] UploadPhotoDto image)
		{
			if (!ModelState.IsValid)
			{
				var errors = string.Join("; ", ModelState.Values
											.SelectMany(v => v.Errors)
											.Select(e => e.ErrorMessage));
				_logger.LogWarning($"ModelState errors: {errors}");
				return BadRequest(ApiResponse<string>.CreateErrorResponse(new ErrorResponse("Invalid Data", $"errors:{errors}")));
			}

			_logger.LogInformation($"Executing {nameof(UploadPhotoAsync)}");
			string? id = GetIdFromToken();
			if(id.IsNullOrEmpty())
				return Unauthorized();
			  var response= await _accountServices.UploadPhotoAsync(image.image, id);
			return HandleResponse(response,targetrel: "upload - photo");
	
		}
		private ActionResult<ApiResponse<T>> HandleResponse<T>(ApiResponse<T> response, string ?actionName=null,string ?targetrel=null)where T : class
		{
			response.ResponseBody.Links = _linkBuilder.MakeRelSelf(_linkBuilder.GenerateLinks(), actionName);
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


	}
}
