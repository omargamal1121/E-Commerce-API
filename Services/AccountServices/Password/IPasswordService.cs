using E_Commers.DtoModels.Responses;

namespace E_Commers.Services.AccountServices.Password
{
    public interface IPasswordService
    {
        Task<Result<string>> ChangePasswordAsync(string userid, string oldPassword, string newPassword);
        Task<Result<string>> RequestPasswordResetAsync(string email);
        Task<Result<string>> ResetPasswordAsync(string email, string token, string newPassword);
    }
} 