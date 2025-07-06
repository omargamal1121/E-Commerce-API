using E_Commers.DtoModels.Responses;
using E_Commers.DtoModels.TokenDtos;

namespace E_Commers.Services.AccountServices.Authentication
{
    public interface IAuthenticationService
    {
        Task<Result<TokensDto>> LoginAsync(string email, string password);
        Task<Result<string>> LogoutAsync(string userid);
        Task<Result<string>> RefreshTokenAsync(string userid, string refreshtoken);
    }
} 