using E_Commers.DtoModels.Responses;
using E_Commers.DtoModels.TokenDtos;
using E_Commers.ErrorHnadling;
using E_Commers.Interfaces;
using E_Commers.Models;
using E_Commers.Services.EmailServices;
using Hangfire;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;

namespace E_Commers.Services.AccountServices.Authentication
{
    public class AuthenticationService : IAuthenticationService
    {
        private readonly ILogger<AuthenticationService> _logger;
        private readonly UserManager<Customer> _userManager;
        private readonly IRefreshTokenService _refreshTokenService;
        private readonly ITokenService _tokenService;
        private readonly IErrorNotificationService _errorNotificationService;
        private readonly IAccountEmailService _accountEmailService;
        private readonly IConfiguration _configuration;

        public AuthenticationService(
            ILogger<AuthenticationService> logger,
            UserManager<Customer> userManager,
            IRefreshTokenService refreshTokenService,
            ITokenService tokenService,
            IErrorNotificationService errorNotificationService,
            IAccountEmailService accountEmailService,
            IConfiguration configuration)
        {
            _logger = logger;
            _userManager = userManager;
            _refreshTokenService = refreshTokenService;
            _tokenService = tokenService;
            _errorNotificationService = errorNotificationService;
            _accountEmailService = accountEmailService;
            _configuration = configuration;
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
                    var maxFailedAttempts = _configuration.GetValue<int>("Security:LockoutPolicy:MaxFailedAttempts", 5);
                    var lockoutDurationMinutes = _configuration.GetValue<int>("Security:LockoutPolicy:LockoutDurationMinutes", 15);
                    var permanentLockoutAfterAttempts = _configuration.GetValue<int>("Security:LockoutPolicy:PermanentLockoutAfterAttempts", 10);
                    if (failedCount >= permanentLockoutAfterAttempts)
                    {
                        user.LockoutEnd = DateTime.UtcNow.AddYears(100);
                        var updateResult = await _userManager.UpdateAsync(user);
                        if (!updateResult.Succeeded)
                        {
                            _ = _errorNotificationService.SendErrorNotificationAsync(updateResult.Errors.ToString() ?? "Error updating lockout state at 10 failed attempts.");
                        }
                        BackgroundJob.Enqueue<IAccountEmailService>(e => e.SendAccountLockedEmailAsync(user, $"Multiple failed login attempts ({permanentLockoutAfterAttempts}+ times)"));
                        BackgroundJob.Enqueue<IAccountEmailService>(e => e.SendPasswordResetEmailAsync(user));
                        return Result<TokensDto>.Fail("Your account has been locked due to multiple failed login attempts. Please use the code sent to your email to reset.", 403);
                    }
                    if (failedCount >= maxFailedAttempts)
                    {
                        user.LockoutEnd = DateTime.UtcNow.AddMinutes(lockoutDurationMinutes);
                        var updateResult = await _userManager.UpdateAsync(user);
                        if (!updateResult.Succeeded)
                        {
                            _ = _errorNotificationService.SendErrorNotificationAsync(updateResult.Errors.ToString() ?? "Error updating temporary lockout.");
                        }
                        BackgroundJob.Enqueue<IAccountEmailService>(e => e.SendAccountLockedEmailAsync(user, $"Multiple failed login attempts ({maxFailedAttempts}+ times)"));
                        return Result<TokensDto>.Fail($"Too many failed login attempts. Please try again after {lockoutDurationMinutes} minutes.", 403);
                    }
                    return Result<TokensDto>.Fail("Invalid email or password.", 401);
                }
                await _userManager.ResetAccessFailedCountAsync(user);
                var tokens = await _tokenService.GenerateTokenAsync(user);
                var refreshTokenResult = await _refreshTokenService.GenerateRefreshTokenAsync(user.Id);
                var response = new TokensDto
                {
                    RefreshToken = refreshTokenResult.Data,
                    Token = tokens.Data,
                    Userid = user.Id,
                };
                return Result<TokensDto>.Ok(response, "Login successfully", 200);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred in LoginAsync.");
                return Result<TokensDto>.Fail("An error occurred during login.", 500);
            }
        }

        public async Task<Result<string>> LogoutAsync(string userid)
        {
            _logger.LogInformation($"Execute:{nameof(LogoutAsync)} in services");
            BackgroundJob.Enqueue<AuthenticationService>(s => s.RemoveUserTokensAsync(userid));
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
                        $"{nameof(AuthenticationService)}/{nameof(LogoutAsync)}"
                    )
                );
            }
            return Result<string>.Ok("Logout Successful", "Logout Successful", 200);
        }

        public async Task<Result<string>> RefreshTokenAsync(string userid, string refreshtoken)
        {
            Customer? customer = await _userManager.FindByIdAsync(userid);
            if (customer is null)
            {
                _logger.LogWarning($"Can't Find user with this id:{userid}");
                return Result<string>.Fail($"Can't Find user with this id:{userid}", 404);
            }
            var result = await _refreshTokenService.ValidateRefreshTokenAsync(userid, refreshtoken);
            if (!result.Success || !result.Data)
            {
                return Result<string>.Fail("Invalid RefreshToken... login again", 400);
            }
            var token = await _refreshTokenService.RefreshTokenAsync(userid, refreshtoken);
            if (!token.Success || token.Data is null)
            {
                return Result<string>.Fail("Failed Generate Token... try again later", 500);
            }
            return Result<string>.Ok(token.Data, "Token Generate", 200);
        }

        // Background method for removing user tokens
        public async Task RemoveUserTokensAsync(string userid)
        {
            try
            {
                await _refreshTokenService.RemoveRefreshTokenAsync(userid);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in RemoveUserTokensAsync: {ex.Message}");
            }
        }
    }
} 