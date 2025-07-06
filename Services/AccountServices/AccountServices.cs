using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;
using AutoMapper;
using E_Commers.DtoModels;
using E_Commers.DtoModels.AccountDtos;
using E_Commers.DtoModels.Responses;
using E_Commers.DtoModels.TokenDtos;
using E_Commers.Enums;
using E_Commers.ErrorHnadling;
using E_Commers.Interfaces;
using E_Commers.Models;
using E_Commers.Services.AccountServices;
using E_Commers.Services.AccountServices.Shared;
using E_Commers.Services.EmailServices;
using E_Commers.UOW;
using Hangfire;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.JsonPatch.Operations;
using Microsoft.AspNetCore.WebUtilities;

namespace E_Commers.Services.AccountServices
{
    public class AccountServices : IAccountServices
    {
        private const string DefaultRole = "User";
        private readonly ILogger<AccountServices> _logger;
        private readonly UserManager<Customer> _userManager;
        private readonly IRefreshTokenService _refrehtokenService;
        private readonly IImagesServices _imagesService;
        private readonly ITokenService _tokenService;
        private readonly IErrorNotificationService _errorNotificationService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public AccountServices(
            IErrorNotificationService errorNotificationService,
            IAccountEmailService accountEmailService,
            IRefreshTokenService refrehtokenService,
            IMapper mapper,
            IImagesServices imagesService,
            UserManager<Customer> userManager,
            ITokenService tokenService,
            IUnitOfWork unitOfWork,
            ILogger<AccountServices> logger
        )
        {
            _errorNotificationService = errorNotificationService;

            _refrehtokenService = refrehtokenService;
            _imagesService = imagesService;
            _userManager = userManager;
            _tokenService = tokenService;
            _unitOfWork = unitOfWork;
            _logger = logger;
            _mapper = mapper;
        }

        public async Task<Result<string>> DeleteAsync(string id)
        {
            _logger.LogInformation($"Execute :{nameof(DeleteAsync)} in services");
            using var Tran = await _unitOfWork.BeginTransactionAsync();
            try
            {
                Customer? customer = await _userManager.FindByIdAsync(id);
                if (customer is null)
                {
                    _logger.LogError($"Can't find Customer with this id: {id}");
                    return Result<string>.Fail($"Can't find Customer with this id: {id}", 401);
                }
                customer.DeletedAt = DateTime.Now;
                var isupdated = await _userManager.UpdateAsync(customer);
                if (!isupdated.Succeeded)
                {
                    var errors = string.Join(", ", isupdated.Errors.Select(e => e.Description));
                    _logger.LogError($"Can't update Customer: {customer.Id}. Errors: {errors}");
                    return Result<string>.Fail($"Can't delete account now. Errors: {errors}", 500);
                }
                
                // Move operation logging to background
                BackgroundJob.Enqueue<AccountServices>(s => s.AddOperationAsync(customer.Id, "Delete Account", Opreations.DeleteOpreation));
                
                await _unitOfWork.CommitAsync();
                await Tran.CommitAsync();
                _logger.LogInformation("Soft deleted is done");
                return Result<string>.Ok("Deleted", "Deleted", 200);
            }
            catch (Exception ex)
            {
                await Tran.RollbackAsync();
                BackgroundJob.Enqueue<IErrorNotificationService>(e =>
                    e.SendErrorNotificationAsync(ex.Message, ex.StackTrace)
                );
                _logger.LogError($"Error:{ex.Message}");
                return Result<string>.Fail($"Error:{ex.Message}", 500);
            }
        }

        public async Task<Result<string>> LogoutAsync(string userid)
        {
            _logger.LogInformation($"Execute:{nameof(LogoutAsync)} in services");
            
            // Move token removal to background for better performance
            BackgroundJob.Enqueue<AccountServices>(s => s.RemoveUserTokensAsync(userid));
            
            Customer? customer = await _userManager.FindByIdAsync(userid);
            if (customer is null)
            {
                _logger.LogError($"No user with this id:{userid}");
                return Result<string>.Fail("Invalid userid", 401);
            }
            
            var isupdate = await _userManager.UpdateSecurityStampAsync(customer);
            if (!isupdate.Succeeded)
            {
                string errors = string.Join(", ", isupdate.Errors.Select(e => e.Description));
                BackgroundJob.Enqueue<IErrorNotificationService>(e =>
                    e.SendErrorNotificationAsync(
                        errors,
                        $"{nameof(AccountServices)}/{nameof(LogoutAsync)}"
                    )
                );
            }
            return Result<string>.Ok("Logout Secssuced", "Logout Secssuced", 200);
        }

        public async Task<Result<RegisterResponse>> RegisterAsync(RegisterDto model)
        {
            _logger.LogInformation($"Executing {nameof(RegisterAsync)} for email: {model.Email}");

            using var tran = await _unitOfWork.BeginTransactionAsync();

            try
            {
                var existingUser = await _userManager.FindByEmailAsync(model.Email);

                if (existingUser != null)
                {
                    _logger.LogWarning("Registration attempt with existing email.");
                    return Result<RegisterResponse>.Fail("This email already exists.", 409);
                }

                Customer customer = _mapper.Map<Customer>(model);
                customer.EmailConfirmed = false;
                customer.LockoutEnabled = true;

                var result = await _userManager.CreateAsync(customer, model.Password);

                if (!result.Succeeded)
                {
                    var errorMessages = string.Join("; ", result.Errors.Select(e => e.Description));
                    _logger.LogError($"Failed to register user: {errorMessages}");
                    return Result<RegisterResponse>.Fail("Registration failed", 400, result.Errors.Select(e => e.Description).ToList());
                }

                IdentityResult result1 = await _userManager.AddToRoleAsync(customer, DefaultRole);
                if (!result1.Succeeded)
                {
                    await tran.RollbackAsync();
                    _logger.LogError(result1.Errors.ToString());
                    return Result<RegisterResponse>.Fail("Errors:Sory Try Again Later", 500);
                }

                await tran.CommitAsync();
                _logger.LogInformation("User registered successfully.");
                RegisterResponse response = _mapper.Map<RegisterResponse>(customer);

                // Move email sending to background for better user experience
                BackgroundJob.Enqueue<IAccountEmailService>(e =>
                    e.SendValidationEmailAsync(customer)
                );
                BackgroundJob.Enqueue<IAccountEmailService>(e => e.SendWelcomeEmailAsync(customer));

                return Result<RegisterResponse>.Ok(response, "Created", 201);
            }
            catch (Exception ex)
            {
                await tran.RollbackAsync();
                _logger.LogError($"Exception in RegisterAsync: {ex}");
                BackgroundJob.Enqueue<IErrorNotificationService>(e =>
                    e.SendErrorNotificationAsync(ex.Message, ex.StackTrace)
                );
                return Result<RegisterResponse>.Fail("An unexpected error occurred.", 500);
            }
        }

        public async Task<Result<TokensDto>> LoginAsync(string email, string password)
        {
            try
            {
                var user = await _userManager.FindByEmailAsync(email);
                if (user == null)
                {
                    _logger.LogWarning("Login failed: Email not found.");
                    return Result<TokensDto>.Fail("Invalid email or password.", 401);
                }

                if (!user.LockoutEnabled)
                {
                    user.LockoutEnabled = true;
                    await _userManager.UpdateAsync(user);
                }

                if (await _userManager.IsLockedOutAsync(user))
                {
                    return Result<TokensDto>.Fail("Your account is currently locked. Please try again later.", 403);
                }

                var isPasswordValid = await _userManager.CheckPasswordAsync(user, password);
                if (!isPasswordValid)
                {
                    await _userManager.AccessFailedAsync(user);
                    var failedCount = await _userManager.GetAccessFailedCountAsync(user);

                    if (failedCount >= 10)
                    {
                        user.LockoutEnd = DateTime.UtcNow.AddYears(100);
                        var updateResult = await _userManager.UpdateAsync(user);

                        if (!updateResult.Succeeded)
                        {
                            _ = _errorNotificationService.SendErrorNotificationAsync(
                                updateResult.Errors.ToString()
                                    ?? "Error updating lockout state at 10 failed attempts."
                            );
                        }

                        // Move email sending to background
                        BackgroundJob.Enqueue<IAccountEmailService>(e =>
                            e.SendAccountLockedEmailAsync(
                                user,
                                "Multiple failed login attempts (10+ times)"
                            )
                        );
                        BackgroundJob.Enqueue<IAccountEmailService>(e =>
                            e.SendPasswordResetEmailAsync(user)
                        );
                        return Result<TokensDto>.Fail("Your account has been locked due to multiple failed login attempts. Please use the code sent to your email to reset.", 403);
                    }

                    if (failedCount >= 5)
                    {
                        user.LockoutEnd = DateTime.UtcNow.AddMinutes(15);
                        var updateResult = await _userManager.UpdateAsync(user);

                        if (!updateResult.Succeeded)
                        {
                            _ = _errorNotificationService.SendErrorNotificationAsync(
                                updateResult.Errors.ToString()
                                    ?? "Error updating temporary lockout."
                            );
                        }

                        // Move email sending to background
                        BackgroundJob.Enqueue<IAccountEmailService>(e =>
                            e.SendAccountLockedEmailAsync(
                                user,
                                "Multiple failed login attempts (5+ times)"
                            )
                        );
                        return Result<TokensDto>.Fail("Too many failed login attempts. Please try again after 15 minutes.", 403);
                    }

                    return Result<TokensDto>.Fail("Invalid email or password.", 401);
                }

                await _userManager.ResetAccessFailedCountAsync(user);

                var tokens = await _tokenService.GenerateTokenAsync(user);
                var refreshTokenResult = await _refrehtokenService.GenerateRefreshTokenAsync(
                    user.Id
                );
                var response = new TokensDto
                {
                    RefreshToken = refreshTokenResult.Data,
                    Token = tokens.Data,
                    Userid = user.Id,
                };
                return Result<TokensDto>.Ok(response, "Login successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred in LoginAsync.");
                return Result<TokensDto>.Fail("An error occurred during login.", 500);
            }
        }

        private async Task<Result<TokensDto>> CheckEmailConfirmationAsync(Customer user)
        {
            if (!user.EmailConfirmed)
            {
                _logger.LogWarning($"Login failed: Email not confirmed for user {user.Email}");
                return Result<TokensDto>.Fail("Email not confirmed", 403, new List<string>{
                    "Please confirm your email before logging in.",
                    "Check your email for the confirmation link.",
                    "If you haven't received the email, you can request a new one."
                });
            }
            return Result<TokensDto>.Ok(null, "Email confirmed", 200);
        }

        public async Task<Result<string>> ChangePasswordAsync(
            string userid,
            string oldPassword,
            string newPassword
        )
        {
            try
            {
                var user = await _userManager.FindByIdAsync(userid);
                if (user == null)
                {
                    _logger.LogWarning("Change password failed: User not found.");
                    return Result<string>.Fail("User not found.", 401);
                }
                if (oldPassword.Equals(newPassword))
                {
                    _logger.LogWarning($"Use Same Password:{oldPassword}");
                    return Result<string>.Fail("Can't Used Same password", 400);
                }

                var result = await _userManager.ChangePasswordAsync(user, oldPassword, newPassword);
                if (!result.Succeeded)
                {
                    var errorMessages = string.Join(
                        "\nError: ",
                        result.Errors.Select(e => e.Description)
                    );
                    _logger.LogError($"Failed to change password: {errorMessages}");
                    return Result<string>.Fail($"Errors: {errorMessages}", 400);
                }
                
                // Move token removal to background for better performance
                BackgroundJob.Enqueue<AccountServices>(s => s.RemoveUserTokensAsync(userid));

                _logger.LogInformation("Password changed successfully.");
                return Result<string>.Ok("Password changed successfully.", "Password changed successfully.", 200);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Exception in ChangePasswordAsync: {ex}");
                BackgroundJob.Enqueue<IErrorNotificationService>(e =>
                    e.SendErrorNotificationAsync(ex.Message, ex.StackTrace)
                );
                return Result<string>.Fail("An unexpected error occurred.", 500);
            }
        }

        public async Task<Result<ChangeEmailResultDto>> ChangeEmailAsync(
            string newEmail,
            string userid
        )
        {
            _logger.LogInformation($"Executing {nameof(ChangeEmailAsync)} for user ID: {userid}");
            var user = await _userManager.FindByIdAsync(userid);
            if (user == null)
            {
                _logger.LogWarning("Change email failed: User not found.");
                return Result<ChangeEmailResultDto>.Fail("User not found.", 401);
            }
            if (string.IsNullOrWhiteSpace(newEmail))
            {
                _logger.LogWarning(
                    "Change email failed: Email parameters cannot be null or empty."
                );
                return Result<ChangeEmailResultDto>.Fail("Email addresses cannot be empty.", 400);
            }

            if (newEmail.Equals(user.Email))
            {
                _logger.LogWarning("Use same Email");
                return Result<ChangeEmailResultDto>.Fail("Can't Use Same Email Address", 400);
            }
            using var transaction = await _unitOfWork.BeginTransactionAsync();
            try
            {
                var existingEmailUser = await _userManager.FindByEmailAsync(newEmail);
                if (existingEmailUser != null)
                {
                    _logger.LogWarning("Change email failed: New email already exists.");
                    return Result<ChangeEmailResultDto>.Fail("This email already exists and can't be used again.", 409);
                }

                var result = await _userManager.SetEmailAsync(user, newEmail);
                if (!result.Succeeded)
                {
                    var errorMessages = string.Join("; ", result.Errors.Select(e => e.Description));
                    _logger.LogError($"Failed to change email: {errorMessages}");
                    return Result<ChangeEmailResultDto>.Fail("Email update failed", 400, result.Errors.Select(e => e.Description).ToList());
                }

                await transaction.CommitAsync();

                // Move email sending to background
                BackgroundJob.Enqueue<IAccountEmailService>(x => x.SendValidationEmailAsync(user));

                _logger.LogInformation("Email changed successfully.");
                return Result<ChangeEmailResultDto>.Ok(
                    new ChangeEmailResultDto
                    {
                        NewEmail = newEmail,
                        Note = "please go to email confirm",
                    },
                    "Email changed successfully.",
                    200
                );
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError($"Exception in ChangeEmailAsync: {ex}");
                BackgroundJob.Enqueue<IErrorNotificationService>(e =>
                    e.SendErrorNotificationAsync(ex.Message, ex.StackTrace)
                );
                return Result<ChangeEmailResultDto>.Fail("An unexpected error occurred.", 500);
            }
        }

        public async Task<Result<UploadPhotoResponseDto>> UploadPhotoAsync(
            IFormFile image,
            string id
        )
        {
            const string loggerAction = nameof(UploadPhotoAsync);
            _logger.LogInformation("Executing {Action} for user ID: {UserId}", loggerAction, id);
			var customer = await _userManager.FindByIdAsync(id);
			if (customer == null)
			{
				_logger.LogError($"User not found with ID: {id}", id);
				return Result<UploadPhotoResponseDto>.Fail("Can't Found User with this id:" + id, 401);
			}

			try
            {
                if (image == null || image.Length == 0)
                {
                    _logger.LogWarning("No image file provided for user ID: {UserId}", id);
                    return Result<UploadPhotoResponseDto>.Fail("No image file provided.", 400);
                }

                var pathResult = await _imagesService.SaveCustomerImageAsync(image,id);
                if (!pathResult.Success || pathResult.Data == null)
                {
                    _logger.LogError("Failed to save image for user ID: {UserId}", id);
                    return Result<UploadPhotoResponseDto>.Fail("Can't Save Image", 500);
                }
                await using var transaction = await _unitOfWork.BeginTransactionAsync();

               

                // Move image replacement to background for better performance
                BackgroundJob.Enqueue<AccountServices>(s => s.ReplaceCustomerImageAsync(customer, pathResult.Data));

                customer.Image = pathResult.Data;
                var updateResult = await _userManager.UpdateAsync(customer);
                if (!updateResult.Succeeded)
                {
                    _logger.LogError(
                        "Failed to update user profile photo. Errors: {Errors}",
                        string.Join(", ", updateResult.Errors.Select(e => e.Description))
                    );
                    await transaction.RollbackAsync();
                    return Result<UploadPhotoResponseDto>.Fail("Failed to update profile.", 500);
                }
                
                // Move operation logging to background
                BackgroundJob.Enqueue<AccountServices>(s => s.AddOperationAsync(id, "Update Profile photo", Opreations.UpdateOpreation));
                
                await transaction.CommitAsync();

                _logger.LogInformation("Successfully uploaded photo for user ID: {UserId}", id);
                return Result<UploadPhotoResponseDto>.Ok(
                    new UploadPhotoResponseDto { ImageUrl = pathResult.Data.Url },
                    "Photo uploaded successfully.",
                    200
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Unexpected error in {Action} for user ID: {UserId}",
                    loggerAction,
                    id
                );
                BackgroundJob.Enqueue<IErrorNotificationService>(e =>
                    e.SendErrorNotificationAsync(ex.Message, ex.StackTrace)
                );
                return Result<UploadPhotoResponseDto>.Fail("An unexpected error occurred.  Try Again Later", 500);
            }
        }

        // Background method for replacing customer image
        public async Task ReplaceCustomerImageAsync(Customer customer, Image newImage)
        {
            try
            {
                if (customer.Image is not null)
                {
                   _=_imagesService.DeleteImageAsync(newImage);
                }

                customer.Image = newImage;
                await AddOperationAsync(customer.Id, "Change Photo", Opreations.UpdateOpreation);
                await _unitOfWork.CommitAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in ReplaceCustomerImageAsync: {ex.Message}");
                BackgroundJob.Enqueue<IErrorNotificationService>(e =>
                    e.SendErrorNotificationAsync(ex.Message, ex.StackTrace)
                );
            }
        }

        // Background method for removing user tokens
        public async Task RemoveUserTokensAsync(string userid)
        {
            try
            {
                await _refrehtokenService.RemoveRefreshTokenAsync(userid);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in RemoveUserTokensAsync: {ex.Message}");
                BackgroundJob.Enqueue<IErrorNotificationService>(e =>
                    e.SendErrorNotificationAsync(ex.Message, ex.StackTrace)
                );
            }
        }

        public async Task AddOperationAsync(
            string userid,
            string description,
            Opreations opreation
        )
        {
            try
            {
                await _unitOfWork
                    .Repository<UserOperationsLog>()
                    .CreateAsync(
                        new UserOperationsLog
                        {
                            Description = description,
                            OperationType = opreation,
                            UserId = userid,
                            Timestamp = DateTime.UtcNow,
                        }
                    );
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in AddOperationAsync: {ex.Message}");
                BackgroundJob.Enqueue<IErrorNotificationService>(e =>
                    e.SendErrorNotificationAsync(ex.Message, ex.StackTrace)
                );
            }
        }

        public async Task<Result<string>> RefreshTokenAsync(string userid, string refreshtoken)
        {
            Customer? customer = await _userManager.FindByIdAsync(userid);
            if (customer is null)
            {
                _logger.LogWarning($"Can't Find user with this id:{userid}");
                return Result<string>.Fail($"Can't Find user with this id:{userid}", 404);
            }
            var result = await _refrehtokenService.ValidateRefreshTokenAsync(userid, refreshtoken);
            if (!result.Success || !result.Data)
            {
                return Result<string>.Fail("Invalid Refrehtoekn... login again", 400);
            }
            var token = await _refrehtokenService.RefreshTokenAsync(userid, refreshtoken);
            if (!token.Success || token.Data is null)
            {
                BackgroundJob.Enqueue<IErrorNotificationService>(e =>
                    e.SendErrorNotificationAsync(token.Message, null)
                );
                return Result<string>.Fail("Filad Generate Token... try again later", 500);
            }
            return Result<string>.Ok("Token Generate", token.Data, 200);
        }

        public async Task<Result<string>> ConfirmEmailAsync(string userId, string token)
        {
            _logger.LogInformation($"Executing {nameof(ConfirmEmailAsync)} for user ID: {userId}");
            try
            {
                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                {
                    _logger.LogWarning($"User not found with ID: {userId}");
                    return Result<string>.Fail("User not found.", 404);
                }

                if (user.EmailConfirmed)
                {
                    _logger.LogWarning($"Email already confirmed for user ID: {userId}");
                    return Result<string>.Fail("Email is already confirmed.", 400);
                }
                var decodedToken = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(token));
                var result = await _userManager.ConfirmEmailAsync(user, decodedToken);
                if (!result.Succeeded)
                {
                    var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                    _logger.LogError(
                        $"Failed to confirm email for user ID: {userId}. Errors: {errors}"
                    );
                    return Result<string>.Fail($"Failed to confirm email: {errors}", 400);
                }

                // Move operation logging to background
                BackgroundJob.Enqueue<AccountServices>(s => s.AddOperationAsync(userId, "Email Confirmation", Opreations.UpdateOpreation));
                
                _logger.LogInformation($"Email confirmed successfully for user ID: {userId}");
                return Result<string>.Ok("Email confirmed successfully.", "Email confirmed successfully.", 200);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in {nameof(ConfirmEmailAsync)}: {ex.Message}");
                BackgroundJob.Enqueue<IErrorNotificationService>(e =>
                    e.SendErrorNotificationAsync(ex.Message, ex.StackTrace)
                );
                return Result<string>.Fail("An unexpected error occurred.", 500);
            }
        }

        public async Task<Result<string>> ResendConfirmationEmailAsync(string email)
        {
            _logger.LogInformation(
                $"Executing {nameof(ResendConfirmationEmailAsync)} for email: {email}"
            );
            try
            {
                var user = await _userManager.FindByEmailAsync(email);
                if (user == null)
                {
                    _logger.LogWarning($"User not found with email: {email}");
                    return Result<string>.Fail("User not found.", 404);
                }

                if (user.EmailConfirmed)
                {
                    _logger.LogWarning($"Email already confirmed for user: {email}");
                    return Result<string>.Fail("Email is already confirmed.", 400);
                }

                // Move email sending to background
                BackgroundJob.Enqueue<IAccountEmailService>(e => e.SendValidationEmailAsync(user));
                
                _logger.LogInformation($"Confirmation email resent successfully to: {email}");
                return Result<string>.Ok("Confirmation email has been resent. Please check your inbox.", "Confirmation email has been resent. Please check your inbox.", 200);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in {nameof(ResendConfirmationEmailAsync)}: {ex.Message}");
                BackgroundJob.Enqueue<IErrorNotificationService>(e =>
                    e.SendErrorNotificationAsync(ex.Message, ex.StackTrace)
                );
                return Result<string>.Fail("An unexpected error occurred.", 500);
            }
        }

        public async Task<Result<string>> RequestPasswordResetAsync(string email)
        {
            try
            {
                var user = await _userManager.FindByEmailAsync(email);
                if (user == null)
                {
                    _logger.LogWarning($"Password reset requested for non-existent email: {email}");
                    return Result<string>.Ok("If the email exists, a reset link has been sent.");
                }

                // Move email sending to background
                BackgroundJob.Enqueue<IAccountEmailService>(e =>
                    e.SendPasswordResetEmailAsync(user)
                );
                return Result<string>.Ok("If the email exists, a reset link has been sent.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in RequestPasswordResetAsync");
                BackgroundJob.Enqueue<IErrorNotificationService>(e =>
                    e.SendErrorNotificationAsync(ex.Message, ex.StackTrace)
                );
                return Result<string>.Fail("An error occurred while requesting password reset.", 500);
            }
        }

        public async Task<Result<string>> ResetPasswordAsync(
            string email,
            string token,
            string newPassword
        )
        {
            try
            {
                var user = await _userManager.FindByEmailAsync(email);
                if (user == null)
                {
                    _logger.LogWarning($"Password reset attempted for non-existent email: {email}");
                    return Result<string>.Fail("Invalid token or email.", 400);
                }
                token = System.Net.WebUtility.UrlDecode(token);

				var result = await _userManager.ResetPasswordAsync(user, token, newPassword);
                if (!result.Succeeded)
                {
                    var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                    _logger.LogWarning($"Password reset failed for {email}: {errors}");
                    return Result<string>.Fail("Password reset failed", 400, result.Errors.Select(e => e.Description).ToList());
                }

                // Move email sending and token cleanup to background
                BackgroundJob.Enqueue<IAccountEmailService>(e =>
                    e.SendPasswordResetSuccessEmailAsync(email)
                );
                BackgroundJob.Enqueue<AccountServices>(s => s.RemoveUserTokensAsync(user.Id));
                
                return Result<string>.Ok("Password has been reset successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in ResetPasswordAsync");
                BackgroundJob.Enqueue<IErrorNotificationService>(e =>
                    e.SendErrorNotificationAsync(ex.Message, ex.StackTrace)
                );
                return Result<string>.Fail("An error occurred while resetting password.", 500);
            }
        }
    }
}
