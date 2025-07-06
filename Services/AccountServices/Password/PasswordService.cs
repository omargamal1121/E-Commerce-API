using E_Commers.DtoModels.Responses;
using E_Commers.ErrorHnadling;
using E_Commers.Interfaces;
using E_Commers.Models;
using E_Commers.Services.EmailServices;
using Hangfire;
using Microsoft.AspNetCore.Identity;

namespace E_Commers.Services.AccountServices.Password
{
    public class PasswordService : IPasswordService
    {
        private readonly ILogger<PasswordService> _logger;
        private readonly UserManager<Customer> _userManager;
        private readonly IRefreshTokenService _refreshTokenService;
        private readonly IErrorNotificationService _errorNotificationService;

        public PasswordService(
            ILogger<PasswordService> logger,
            UserManager<Customer> userManager,
            IRefreshTokenService refreshTokenService,
            IErrorNotificationService errorNotificationService)
        {
            _logger = logger;
            _userManager = userManager;
            _refreshTokenService = refreshTokenService;
            _errorNotificationService = errorNotificationService;
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
                    _logger.LogWarning("Change password failed: New password same as old password");
                    return Result<string>.Fail("Can't use the same password.", 400);
                }
                var result = await _userManager.ChangePasswordAsync(user, oldPassword, newPassword);
                if (!result.Succeeded)
                {
                    var errorMessages = string.Join("\nError: ", result.Errors.Select(e => e.Description));
                    _logger.LogError($"Failed to change password: {errorMessages}");
                    return Result<string>.Fail($"Errors: {errorMessages}", 400);
                }
                BackgroundJob.Enqueue<PasswordService>(s => s.RemoveUserTokensAsync(userid));
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

        public async Task<Result<string>> RequestPasswordResetAsync(string email)
        {
            try
            {
                var user = await _userManager.FindByEmailAsync(email);
                if (user != null)
                {
                    BackgroundJob.Enqueue<IAccountEmailService>(e =>
                        e.SendPasswordResetEmailAsync(user)
                    );
                }
                return Result<string>.Ok("If the email exists, a reset link has been sent.", "If the email exists, a reset link has been sent.", 200);
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
                var result = await _userManager.ResetPasswordAsync(user, token, newPassword);
                if (!result.Succeeded)
                {
                    var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                    _logger.LogWarning($"Password reset failed for {email}: {errors}");
                    return Result<string>.Fail($"Reset Failed: {errors}", 400);
                }
                BackgroundJob.Enqueue<IAccountEmailService>(e =>
                    e.SendPasswordResetSuccessEmailAsync(email)
                );
                BackgroundJob.Enqueue<PasswordService>(s => s.RemoveUserTokensAsync(user.Id));
                return Result<string>.Ok("Password has been reset successfully.", "Password has been reset successfully.", 200);
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