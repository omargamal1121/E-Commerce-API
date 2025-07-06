using E_Commers.DtoModels.AccountDtos;
using E_Commers.DtoModels.Responses;

namespace E_Commers.Services.AccountServices.Registration
{
    public interface IRegistrationService
    {
        Task<Result<RegisterResponse>> RegisterAsync(RegisterDto model);
        Task<Result<string>> ConfirmEmailAsync(string userId, string token);
        Task<Result<string>> ResendConfirmationEmailAsync(string email);
    }
} 