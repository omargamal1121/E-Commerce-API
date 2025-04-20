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
namespace E_Commers.Controllers
{
	[Route("api/[controller]")]
	[ApiController]
	public class AccountController : ControllerBase
	{
		private readonly ITokenService _tokenHelper;
		private readonly ILogger<AccountController> _logger;
		private readonly UserManager<Customer> _userManager;
		private readonly IImagesServices _imagesHelper;
		private readonly IUnitOfWork _unitOfWork;
		private readonly IRefreshTokenService _RefreshTokenService;	

		public AccountController(IRefreshTokenService RefreshTokenService,IUnitOfWork unitOfWork, IImagesServices imagesHelper, ITokenService tokenHelper, UserManager<Customer> userManager, ILogger<AccountController> logger)
		{
			_unitOfWork = unitOfWork;
			_imagesHelper = imagesHelper;
			_tokenHelper = tokenHelper;
			_logger = logger;
			_userManager = userManager; 
			_RefreshTokenService = RefreshTokenService;
		}

		[HttpPost(nameof(Login))]
		public async Task<ActionResult<ResponseDto>> Login([FromBody] LoginDTo login)
		{
			_logger.LogInformation($"In {nameof(Login)} Method ");
			if (!ModelState.IsValid)
			{
				var errors = ModelState.Values
									   .SelectMany(v => v.Errors)
									   .Select(e => e.ErrorMessage)
									   .ToList();

				return BadRequest(new ResponseDto
				{
					
					Message = "Invalid data: " + string.Join(", ", errors)

				});
			}

			Customer? customer = await _userManager.FindByEmailAsync(login.Email);
			if (customer is null)
			{
				_logger.LogWarning($"Login failed: Email '{login.Email}' not found.");
				return Unauthorized(new ResponseDto {  Message = "Invalid Email." });
			}

			if (await _userManager.IsLockedOutAsync(customer))
			{
				_logger.LogWarning($"Login failed: Email '{login.Email}' is locked out.");
				return Unauthorized(new ResponseDto { Message = "Account is locked. Try again later." });
			}

			bool checkpass = await _userManager.CheckPasswordAsync(customer, login.Password);
			if (!checkpass)
			{
				 await _userManager.AccessFailedAsync(customer);
				_logger.LogWarning($"Login failed: Incorrect password for '{login.Email}'.");
				await _userManager.AccessFailedAsync(customer);
				return Unauthorized(new ResponseDto { Message = "Incorrect Email or Password." });
			}

			var token = await _tokenHelper.GenerateTokenAsync(customer.Id);
			if (!token.Success||token.Data.IsNullOrEmpty())
			{
				_logger.LogError("Failed to generate token.");
				return BadRequest(new ResponseDto
				{
					
					Message = token.Message
				});
			}

			_logger.LogInformation("Token generated.");
			var refreshToken = await _RefreshTokenService.GenerateRefreshToken(customer.Id);
			if (!refreshToken.Success||refreshToken.Data.IsNullOrEmpty())
			{
				_logger.LogError("Failed to generate refresh token.");
				return BadRequest(new ResponseDto
				{
	
					Message = refreshToken.Message
				});
			}

			customer.LastVisit = DateTime.Now;
			 var result= await _userManager.UpdateAsync(customer);
			if(!result.Succeeded)
			{
				return StatusCode(500, new ResponseDto { Message=string.Join(",", result.Errors.Select(x => x.Description))  });

			}
			return Ok(new ResponseDto
			{
	
				Message = "Token Generated",
				Data = new 
				{
					userid=customer.Id,
					Token = token.Data, 
					RefreshToken = refreshToken.Data 
				}
			});
		}

		[HttpPost(nameof(Register))]
		public async Task<ActionResult<ResponseDto>> Register([FromForm] RegisterDto usermodel)
		{
			_logger.LogInformation($"In {nameof(Register)} Method ");
			if (!ModelState.IsValid)
			{
				var errors = string.Join("; ", ModelState.Values
												  .SelectMany(v => v.Errors)
												  .Select(e => e.ErrorMessage));

				_logger.LogWarning($"ModelState errors: {errors}");
				return BadRequest(new ResponseDto
				{
					
					Message = errors

				});
			}
			if(await _userManager.FindByEmailAsync(usermodel.Email) is not null)
			{
				_logger.LogError($"Invalid Email Address:{usermodel.Email}");
				return BadRequest(new ResponseDto
				{
				
					Message = $"Invalid Email Address:{usermodel.Email}"

				});
			}
			Customer customer = new Customer
			{
				UserName=usermodel.UserName,
				Name=usermodel.Name,
				Age=usermodel.Age,
				Email=usermodel.Email,
				PhoneNumber= usermodel.PhoneNumber,
			
			};
			customer.SecurityStamp = Guid.NewGuid().ToString();
			customer.ConcurrencyStamp = Guid.NewGuid().ToString();
			using var tran = await _unitOfWork.BeginTransactionAsync();

			IdentityResult result = await _userManager.CreateAsync(customer, usermodel.Password);
			if (!result.Succeeded)
			{
				string errors = string.Join("\n", result.Errors.Select(e => e.Description));
				_logger.LogError("User creation failed: {errors}", errors);
				return BadRequest(new ResponseDto
				{
					
					Message = errors
				});
			}
			
			IdentityResult result1= await _userManager.AddToRoleAsync(customer,"User");
			if(!result1.Succeeded)
			{
			 await	tran.RollbackAsync();
				_logger.LogError(result1.Errors.ToString());
				return StatusCode(500, new ResponseDto { Message = "Error While Add Role to User" });
			}
			 await	tran.CommitAsync();
			return Ok(new ResponseDto {  Message = $"User created successfully Id:{customer.Id}." });
		}

		[HttpPost(nameof(RefreshTokenAsync))]
		public async Task<ActionResult<ResponseDto>> RefreshTokenAsync([FromBody]RefreshTokenDto refreshTokenDto)
		{
			_logger.LogInformation($"In {nameof(RefreshTokenAsync)} Method");
			if (!ModelState.IsValid)
			{
				var errors = string.Join("; ", ModelState.Values
												  .SelectMany(v => v.Errors)
												  .Select(e => e.ErrorMessage));

				_logger.LogWarning($"ModelState errors: {errors}");
				return BadRequest(new ResponseDto
				{
				
					Message = errors

				});
			}
			Customer? customer = await _userManager.FindByIdAsync(refreshTokenDto.UserId.ToString("D"));
			if (customer is null)
			{
				_logger.LogWarning("Invalid userid");
				return Unauthorized( new ResponseDto { Message = "Invalid userid",  });
			}
			var result=await	_RefreshTokenService.ValidateRefreshTokenAsync(refreshTokenDto.UserId.ToString("D"), refreshTokenDto.RefreshToken);
			if(!result.Success||!result.Data)
			{
				 return Unauthorized(new ResponseDto { Message = result.Message,  });
			}
			 var token= await _RefreshTokenService.RefreshToken(refreshTokenDto.UserId.ToString("D"),refreshTokenDto.RefreshToken);

			return Ok(new ResponseDto { Message = "Token Generated" ,Data= new { Token = token }, });

		}
		[HttpPost(nameof(ChangePassword))]
		[Authorize]
		public async Task<ActionResult<ResponseDto>> ChangePassword([FromBody] ChangePasswordDto model)
		{

			_logger.LogInformation($"In {nameof(ChangePassword)} Method");
			if (!ModelState.IsValid)
			{
				var errors = string.Join("\n ", ModelState.Values
													  .SelectMany(v => v.Errors)
													  .Select(e => e.ErrorMessage));

				_logger.LogWarning($"ModelState errors: {errors}");
				return BadRequest(new ResponseDto
				{
				
					Message = errors
				});
			}

			string userid = User.FindFirst(ClaimTypes.NameIdentifier).Value;

			Customer? customer = await _userManager.FindByIdAsync(userid);
			if (customer is null)
			{
				_logger.LogError("Invalid userid");
				return Unauthorized(new ResponseDto { Message = "Invalid userid" });
			}

			if (!await _userManager.CheckPasswordAsync(customer, model.CurrentPass))
			{
				_logger.LogWarning("Current password is incorrect");
				return BadRequest(new ResponseDto { Message = "Current password is incorrect",  });
			}

			if (!model.NewPass.Equals(model.ConfirmNewPass))
			{
				_logger.LogWarning("New password and confirmation password do not match");
				return BadRequest(new ResponseDto { Message = "New password and confirmation password do not match",  });
			}

			IdentityResult result = await _userManager.ChangePasswordAsync(customer, model.CurrentPass, model.NewPass);
			if (!result.Succeeded)
			{
				string errorMessages = string.Join("; ", result.Errors.Select(e => e.Description));
				_logger.LogError($"Failed to change password: {errorMessages}");
				return BadRequest(new ResponseDto { Message = errorMessages,  });
			}

			_logger.LogInformation("Password changed successfully");
			await _userManager.UpdateSecurityStampAsync(customer);
			return Ok(new ResponseDto { Message = "Password changed successfully", });
		}



		[Authorize]
		[HttpPost(nameof(ChangeEmail))]
		public async Task<ActionResult<ResponseDto>> ChangeEmail(string NewEmail)
		{
			if (!ModelState.IsValid)
			{
				var errors = string.Join("; ", ModelState.Values
											.SelectMany(v => v.Errors)
											.Select(e => e.ErrorMessage));
				_logger.LogWarning($"ModelState errors: {errors}");
				return BadRequest(new ResponseDto
				{
				
					Message = errors
				});
			}
			if(!NewEmail.Contains("@")||!NewEmail.EndsWith(".com"))
			{
				_logger.LogWarning("Invalid Email Adress");
				return BadRequest(new ResponseDto { Message = "Invalid Email Adress" });
			}
			_logger.LogInformation($"In {nameof(ChangeEmail)} Method");

			string? oldemail = User.FindFirst(ClaimTypes.Email)?.Value;
			if (string.IsNullOrEmpty(oldemail))
			{
				_logger.LogWarning("Email Adress not found in token");
				return Unauthorized(new ResponseDto {  Message = "Unauthorized" });
			}

			Customer? customer = await _userManager.FindByEmailAsync(oldemail);
			if (customer is null)
			{
				_logger.LogWarning("User not found. invalid Email Address");
				return Unauthorized(new ResponseDto {  Message = "invalid Email Address" });
			}

			
			IdentityResult result = await _userManager.SetEmailAsync(customer, NewEmail);
			if (!result.Succeeded)
			{
				string errorMessages = string.Join("; ", result.Errors.Select(e => e.Description));
				_logger.LogError($"Failed to change email: {errorMessages}");
				return BadRequest(new ResponseDto {Message = errorMessages });
			}

			
			await _userManager.UpdateSecurityStampAsync(customer);

			_logger.LogInformation("Email changed successfully");
			return Ok(new ResponseDto { Message = "Email changed successfully" });
		}


		[Authorize]
		[HttpPost(nameof(Logout))]
		public async Task<ActionResult<ResponseDto>> Logout()
		{
			_logger.LogInformation($"In {nameof(Logout)} Method");

			string? userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

			var result = await _RefreshTokenService.RemoveRefreshTokenAsync(userId);
			if (!result.Success||!result.Data)
			{
				return BadRequest(new ResponseDto { Message =result.Message});
			}

			_logger.LogInformation("✅ Logout successful for user {UserId}", userId);
			return Ok(new ResponseDto { Message = "Logout successfully", });
		}

		[Authorize]
		[HttpDelete(nameof(Delete))]
		public async Task<ActionResult<ResponseDto>> Delete()
		{
			_logger.LogInformation($"In {nameof(Delete)} Method");

			string userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
	
			var result = await _RefreshTokenService.RemoveRefreshTokenAsync(userId);
			if (!result.Success || !result.Data)
			{
				
				return BadRequest(new ResponseDto { Message = result.Message });
			}
			Customer? customer = await _userManager.FindByIdAsync(userId);
			if(customer is null)
			{
				_logger.LogError("❌ Can't Find user {UserId}", userId);
				return BadRequest(new ResponseDto { Message = "User doesn't exsist" });
			}
			IdentityResult isdeleted= 	await _userManager.DeleteAsync(customer);
			if(!isdeleted.Succeeded)
			{
				_logger.LogError("❌ Can't Deleted user {UserId}", userId);
				return BadRequest(new ResponseDto {  Message = "❌ Can't Deleted user" });

			}

			_logger.LogInformation("✅ Deleted successful for user {UserId}", userId);
			return Ok(new ResponseDto { Message = "Deleted successfully", });
		}
		[Authorize]
		[HttpPost("UploadPhoto")]
		public async Task<ActionResult<ResponseDto>> UploadPhoto([FromForm] IFormFile image)
		{
			_logger.LogInformation($"Executing {nameof(UploadPhoto)}");
			Result<string>path =  await _imagesHelper.SaveImageAsync(image, "CustomerPhotos");

			if(!path.Success||path.Data is null)
				return new ResponseDto { Message=path.Message};
			string userid =User.FindFirst(ClaimTypes.NameIdentifier).Value;
			Customer customer = await _userManager.FindByIdAsync(userid);
			customer.ImageUrl = path.Data;
			return NoContent();

		}


	}
}
