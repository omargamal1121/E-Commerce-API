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
using E_Commers.UOW;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.JsonPatch.Operations;

namespace E_Commers.Services.AccountServices
{
    public class AccountServices : IAccountServices
    {
        private readonly ILogger<AccountServices> _logger;
        private readonly UserManager<Customer> _userManager;
        private readonly IRefreshTokenService _refrehtokenService;
        private readonly IImagesServices _imagesService;
        private readonly ITokenService _tokenService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public AccountServices(
            IRefreshTokenService refrehtokenService,
            IMapper mapper,
            IImagesServices imagesService,
            UserManager<Customer> userManager,
            ITokenService tokenService,
            IUnitOfWork unitOfWork,
            ILogger<AccountServices> logger
        )
        {
            _refrehtokenService = refrehtokenService;
            _imagesService = imagesService;
            _userManager = userManager;
            _tokenService = tokenService;
            _unitOfWork = unitOfWork;
            _logger = logger;
            _mapper = mapper;
        }

        public async Task<ApiResponse<string>> DeleteAsync(string id)
        {
            _logger.LogInformation($"Execute :{nameof(DeleteAsync)} in services");
            using var Tran = await _unitOfWork.BeginTransactionAsync();
            try
            {
                Customer? customer = await _userManager.FindByIdAsync(id);
                if (customer is null)
                {
                    _logger.LogError($"Can't find Customer with this id: {id}");
                    return ApiResponse<string>.CreateErrorResponse(
                        new ErrorResponse("User", $"Can't find Customer with this id: {id}"),
                        401
                    );
                }
                customer.DeletedAt = DateTime.Now;
                var isupdated = await _userManager.UpdateAsync(customer);
                if (!isupdated.Succeeded)
                {
                    var errors = string.Join(", ", isupdated.Errors.Select(e => e.Description));
                    _logger.LogError($"Can't update Customer: {customer.Id}. Errors: {errors}");
                    return ApiResponse<string>.CreateErrorResponse(
                        new ErrorResponse(
                            "Server Error",
                            $"Can't delete account now. Errors: {errors}"
                        ),
                        500
                    );
                }
                await AddOperationAsync(customer.Id, "Delete Account", Opreations.DeleteOpreation);
                await _unitOfWork.CommitAsync();
                await Tran.CommitAsync();
                _logger.LogInformation("Soft deleted is done");
                return ApiResponse<string>.CreateSuccessResponse(
                    message: "Deleted",
                    statusCode: 200
                );
            }
            catch (Exception ex)
            {
                await Tran.RollbackAsync();
                _logger.LogError($"Error:{ex.Message}");
                return ApiResponse<string>.CreateErrorResponse(
                    new ErrorResponse("Server Error", $"Error:{ex.Message}"),
                    500
                );
            }
        }

        public async Task<ApiResponse<string>> LogoutAsync(string userid)
        {
            _logger.LogInformation($"Execute:{nameof(LogoutAsync)} in services");
            var result = await _refrehtokenService.RemoveRefreshTokenAsync(userid);
            //if(!result.Success)
            //{
            //	//Must send email to me
            //}

            Customer? customer = await _userManager.FindByIdAsync(userid);
            if (customer is null)
            {
                _logger.LogError($"No user with this id:{userid}");
                return ApiResponse<string>.CreateErrorResponse(
                    new ErrorResponse("User", "Invalid userid"),
                    401
                );
            }
            var isupdate = await _userManager.UpdateSecurityStampAsync(customer);
            await _refrehtokenService.RemoveRefreshTokenAsync(userid);
            //if (!isupdate.Succeeded)
            //{
            //		//Must send email to me
            //}
            return ApiResponse<string>.CreateSuccessResponse("Logout Secssuced", statusCode: 200);
        }

        public async Task<ApiResponse<RegisterResponse>> RegisterAsync(RegisterDto model)
        {
            using var tran = await _unitOfWork.BeginTransactionAsync();
            try
            {
                var existingUser = await _userManager.FindByEmailAsync(model.Email);
                if (existingUser != null)
                {
                    _logger.LogWarning("Registration attempt with existing email.");
                    return ApiResponse<RegisterResponse>.CreateErrorResponse(
                        new ErrorResponse("Invalid Email", "This email already exists."),
                        409
                    );
                }

                Customer customer = _mapper.Map<Customer>(model);
                customer.SecurityStamp = Guid.NewGuid().ToString();
                customer.ConcurrencyStamp = Guid.NewGuid().ToString();

                var result = await _userManager.CreateAsync(customer, model.Password);

                if (!result.Succeeded)
                {
                    var errorMessages = string.Join("; ", result.Errors.Select(e => e.Description));
                    _logger.LogError($"Failed to register user: {errorMessages}");
                    return ApiResponse<RegisterResponse>.CreateErrorResponse(
                        new ErrorResponse($"Registration failed", $"Errors : {errorMessages}"),
                        400
                    );
                }

                IdentityResult result1 = await _userManager.AddToRoleAsync(customer, "User");
                if (!result1.Succeeded)
                {
                    await tran.RollbackAsync();
                    _logger.LogError(result1.Errors.ToString());
                    return ApiResponse<RegisterResponse>.CreateErrorResponse(
                        new ErrorResponse("Server Error", $"Errors:Sory Try Again Later"),
                        500
                    );
                }

                await tran.CommitAsync();
                _logger.LogInformation("User registered successfully.");
                RegisterResponse response = _mapper.Map<RegisterResponse>(model);
                return ApiResponse<RegisterResponse>.CreateSuccessResponse(
                    "Created",
                    response,
                    201
                );
            }
            catch (Exception ex)
            {
                await tran.RollbackAsync();
                _logger.LogError($"Exception in RegisterAsync: {ex}");
                return ApiResponse<RegisterResponse>.CreateErrorResponse(
                    new ErrorResponse("Server Error", "An unexpected error occurred."),
                    500
                );
            }
        }

        public async Task<ApiResponse<TokensDto>> LoginAsync(string email, string password)
        {
            try
            {
                var user = await _userManager.FindByEmailAsync(email);
                if (user == null)
                {
                    _logger.LogWarning("Login failed: Email not found.");
                    return ApiResponse<TokensDto>.CreateErrorResponse(
                        new ErrorResponse("Login Failed", "Invalid email or password."),
                        401
                    );
                }

                var isPasswordValid = await _userManager.CheckPasswordAsync(user, password);
                if (!isPasswordValid)
                {
                    _logger.LogWarning("Login failed: Incorrect password.");
                    return ApiResponse<TokensDto>.CreateErrorResponse(
                        new ErrorResponse("Login Failed", "Invalid email or password."),
                        401
                    );
                }

                var token = await _tokenService.GenerateTokenAsync(user.Id);
                if (!token.Success || token.Data is null)
                {
                    _logger.LogError("Token generation failed during login.");
                    return ApiResponse<TokensDto>.CreateErrorResponse(
                        new ErrorResponse("Login Failed", "Try Again later."),
                        500
                    );
                }
                var refreshtoken = await _refrehtokenService.GenerateRefreshTokenAsync(user.Id);
                if (!refreshtoken.Success || refreshtoken.Data is null)
                {
                    _logger.LogError("refreshtoken generation failed during login.");
                    return ApiResponse<TokensDto>.CreateErrorResponse(
                        new ErrorResponse("Login Failed", "Try Again later."),
                        500
                    );
                }

                _logger.LogInformation("User logged in successfully.");
                return ApiResponse<TokensDto>.CreateSuccessResponse(
                    "Login successful.",
                    new TokensDto(user.Id, token.Data, refreshtoken.Data),
                    200
                );
            }
            catch (Exception ex)
            {
                _logger.LogError($"Exception in LoginAsync: {ex}");
                return ApiResponse<TokensDto>.CreateErrorResponse(
                    new ErrorResponse("Login Failed", "An unexpected error occurred."),
                    500
                );
            }
        }

        public async Task<ApiResponse<string>> ChangePasswordAsync(
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
                    return ApiResponse<string>.CreateErrorResponse(
                        new ErrorResponse("User", "User not found."),
                        401
                    );
                }
                if (oldPassword.Equals(newPassword))
                {
                    _logger.LogWarning($"Use Same Password:{oldPassword}");
                    return ApiResponse<string>.CreateErrorResponse(
                        new ErrorResponse("New Password", "Can't Used Same password"),
                        400
                    );
                }

                var result = await _userManager.ChangePasswordAsync(user, oldPassword, newPassword);
                if (!result.Succeeded)
                {
                    var errorMessages = string.Join(
                        "\nError: ",
                        result.Errors.Select(e => e.Description)
                    );
                    _logger.LogError($"Failed to change password: {errorMessages}");
                    return ApiResponse<string>.CreateErrorResponse(
                        new ErrorResponse("Password", $"Errors: {errorMessages}"),
                        400
                    );
                }
                var isreomved = await _refrehtokenService.RemoveRefreshTokenAsync(userid);
                if (!isreomved.Success)
                {
                    _logger.LogError(isreomved.Message);
                    //send email
                }
                _logger.LogInformation("Password changed successfully.");
                return ApiResponse<string>.CreateSuccessResponse(
                    "Password changed successfully.",
                    statusCode: 200
                );
            }
            catch (Exception ex)
            {
                _logger.LogError($"Exception in ChangePasswordAsync: {ex}");
                return ApiResponse<string>.CreateErrorResponse(
                    new ErrorResponse("Server Error", "An unexpected error occurred."),
                    500
                );
            }
        }

        public async Task<ApiResponse<ChangeEmailResultDto>> ChangeEmailAsync(
            string newEmail,
            string oldEmail
        )
        {
            if (string.IsNullOrWhiteSpace(newEmail) || string.IsNullOrWhiteSpace(oldEmail))
            {
                _logger.LogWarning(
                    "Change email failed: Email parameters cannot be null or empty."
                );
                return ApiResponse<ChangeEmailResultDto>.CreateErrorResponse(
                    new ErrorResponse("New Email", "Email addresses cannot be empty."),
                    400
                );
            }

            if (newEmail.Equals(oldEmail))
            {
                _logger.LogWarning("Use same Email");
                return ApiResponse<ChangeEmailResultDto>.CreateErrorResponse(
                    new ErrorResponse("New Email", "Can't Use Same Email Address"),
                    400
                );
            }
            using var transaction = await _unitOfWork.BeginTransactionAsync();
            try
            {
                var existingEmailUser = await _userManager.FindByEmailAsync(newEmail);
                if (existingEmailUser != null)
                {
                    _logger.LogWarning("Change email failed: New email already exists.");
                    return ApiResponse<ChangeEmailResultDto>.CreateErrorResponse(
                        new ErrorResponse(
                            "New Email",
                            "This email already exists and can't be used again."
                        ),
                        409
                    ); // 409 Conflict
                }

                var user = await _userManager.FindByEmailAsync(oldEmail);
                if (user == null)
                {
                    _logger.LogWarning("Change email failed: Old email not found.");
                    return ApiResponse<ChangeEmailResultDto>.CreateErrorResponse(
                        new ErrorResponse("User", "User not found."),
                        401
                    );
                }

                var result = await _userManager.SetEmailAsync(user, newEmail);
                if (!result.Succeeded)
                {
                    var errorMessages = string.Join("; ", result.Errors.Select(e => e.Description));
                    _logger.LogError($"Failed to change email: {errorMessages}");
                    return ApiResponse<ChangeEmailResultDto>.CreateErrorResponse(
                        new ErrorResponse(
                            "Email Updateing",
                            $"Email change failed: {errorMessages}"
                        ),
                        400
                    );
                }
                await _userManager.SetUserNameAsync(user, newEmail);
                await transaction.CommitAsync();

                _logger.LogInformation("Email changed successfully.");
                return ApiResponse<ChangeEmailResultDto>.CreateSuccessResponse(
                    "Email changed successfully.",
                    new ChangeEmailResultDto { OldEmail = oldEmail, NewEmail = newEmail },
                    statusCode: 200
                );
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError($"Exception in ChangeEmailAsync: {ex}");
                return ApiResponse<ChangeEmailResultDto>.CreateErrorResponse(
                    new ErrorResponse("Error", "An unexpected error occurred."),
                    500
                );
            }
        }

        public async Task<ApiResponse<UploadPhotoResponseDto>> UploadPhotoAsync(
            IFormFile image,
            string id
        )
        {
            const string loggerAction = nameof(UploadPhotoAsync);
            _logger.LogInformation("Executing {Action} for user ID: {UserId}", loggerAction, id);

            try
            {
                // Validate input
                if (image == null || image.Length == 0)
                {
                    _logger.LogWarning("No image file provided for user ID: {UserId}", id);
                    return ApiResponse<UploadPhotoResponseDto>.CreateErrorResponse(
                        new ErrorResponse("Image", "No image file provided."),
                        400
                    );
                }

                // Save the image
                var pathResult = await _imagesService.SaveImageAsync(image, "CustomerPhotos");
                if (!pathResult.Success || pathResult.Data == null)
                {
                    _logger.LogError("Failed to save image for user ID: {UserId}", id);
                    return ApiResponse<UploadPhotoResponseDto>.CreateErrorResponse(
                        new ErrorResponse("Image Saving", "Can't Save Image"),
                        500
                    );
                }

                // Begin transaction
                await using var transaction = await _unitOfWork.BeginTransactionAsync();

                var customer = await _userManager.FindByIdAsync(id);
                if (customer == null)
                {
                    _logger.LogError($"User not found with ID: {id}", id);
                    return ApiResponse<UploadPhotoResponseDto>.CreateErrorResponse(
                        new ErrorResponse(
                            "Userid",
                            "Can't Found User",
                            $"Can't Found User with this id:{id}"
                        ),
                        401
                    );
                }

                await ReplaceCustomerImageAsync(customer, pathResult.Data);

                var updateResult = await _userManager.UpdateAsync(customer);
                if (!updateResult.Succeeded)
                {
                    _logger.LogError(
                        "Failed to update user profile photo. Errors: {Errors}",
                        string.Join(", ", updateResult.Errors.Select(e => e.Description))
                    );
                    await transaction.RollbackAsync();
                    return ApiResponse<UploadPhotoResponseDto>.CreateErrorResponse(
                        new ErrorResponse("Image Updating", "Failed to update profile."),
                        500
                    );
                }
                await AddOperationAsync(id, "Update Profile photo", Opreations.UpdateOpreation);
                await transaction.CommitAsync();

                _logger.LogInformation("Successfully uploaded photo for user ID: {UserId}", id);
                return ApiResponse<UploadPhotoResponseDto>.CreateSuccessResponse(
                    "Photo uploaded successfully.",
                    new UploadPhotoResponseDto { ImageUrl = pathResult.Data },
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
                return ApiResponse<UploadPhotoResponseDto>.CreateErrorResponse(
                    new ErrorResponse("Error", "An unexpected error occurred.  Try Again Later"),
                    500
                );
            }
        }

        private async Task ReplaceCustomerImageAsync(Customer customer, string newImagePath)
        {
            if (customer.ImageUrl is not null)
            {
                _imagesService.DeleteImage("CustomerPhotos", customer.ImageUrl);
            }

            customer.ImageUrl = newImagePath;
            await AddOperationAsync(customer.Id, "Change Photo", Opreations.UpdateOpreation);
            await _unitOfWork.CommitAsync();
        }

        private async Task AddOperationAsync(
            string userid,
            string description,
            Opreations opreation
        )
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

        public async Task<ApiResponse<string>> RefreshTokenAsync(string userid, string refreshtoken)
        {
            Customer? customer = await _userManager.FindByIdAsync(userid);
            if (customer is null)
            {
                _logger.LogWarning($"Can't Find user with this id:{userid}");
                return ApiResponse<string>.CreateErrorResponse(
                    new ErrorResponse("User", $"Can't Find user with this id:{userid}"),
                    404
                );
            }
            var result = await _refrehtokenService.ValidateRefreshTokenAsync(userid, refreshtoken);
            if (!result.Success || !result.Data)
            {
                return ApiResponse<string>.CreateErrorResponse(
                    new ErrorResponse("RefreshToken", "Invalid Refrehtoekn... login again"),
                    400
                );
            }
            var token = await _refrehtokenService.RefreshTokenAsync(userid, refreshtoken);
            if (!token.Success || token.Data is null)
            {
                return ApiResponse<string>.CreateErrorResponse(
                    new ErrorResponse("Generate Token", "Filad Generate Token... try again later"),
                    500
                );
                //send email to me
            }
            return ApiResponse<string>.CreateSuccessResponse("Token Generate", token.Data, 200);
        }
    }
}
