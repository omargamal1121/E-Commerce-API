using System.Text;
using AutoMapper;
using E_Commers.DtoModels.AccountDtos;
using E_Commers.DtoModels.Responses;
using E_Commers.Enums;
using E_Commers.ErrorHnadling;
using E_Commers.Models;
using E_Commers.Services.EmailServices;
using E_Commers.UOW;
using Hangfire;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;

namespace E_Commers.Services.AccountServices.Registration
{
    public class RegistrationService : IRegistrationService
    {
        private const string DefaultRole = "User";
        private readonly ILogger<RegistrationService> _logger;
        private readonly UserManager<Customer> _userManager;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IErrorNotificationService _errorNotificationService;

        public RegistrationService(
            ILogger<RegistrationService> logger,
            UserManager<Customer> userManager,
            IUnitOfWork unitOfWork,
            IMapper mapper,
            IErrorNotificationService errorNotificationService)
        {
            _logger = logger;
            _userManager = userManager;
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _errorNotificationService = errorNotificationService;
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
                    return Result<RegisterResponse>.Fail($"Registration failed: {errorMessages}", 400);
                }
                IdentityResult result1 = await _userManager.AddToRoleAsync(customer, DefaultRole);
                if (!result1.Succeeded)
                {
                    await tran.RollbackAsync();
                    _logger.LogError(result1.Errors.ToString());
                    return Result<RegisterResponse>.Fail("Errors:Sorry Try Again Later", 500);
                }
                await tran.CommitAsync();
                _logger.LogInformation("User registered successfully.");
                RegisterResponse response = _mapper.Map<RegisterResponse>(customer);
                BackgroundJob.Enqueue<IAccountEmailService>(e => e.SendValidationEmailAsync(customer));
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
                    _logger.LogError($"Failed to confirm email for user ID: {userId}. Errors: {errors}");
                    return Result<string>.Fail($"Failed to confirm email: {errors}", 400);
                }
                BackgroundJob.Enqueue<RegistrationService>(s => s.AddOperationAsync(userId, "Email Confirmation", Opreations.UpdateOpreation));
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
            _logger.LogInformation($"Executing {nameof(ResendConfirmationEmailAsync)} for email: {email}");
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

        private async Task AddOperationAsync(
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
    }
} 