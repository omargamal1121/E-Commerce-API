using E_Commers.DtoModels;
using E_Commers.DtoModels.AccountDtos;
using E_Commers.DtoModels.Responses;
using E_Commers.DtoModels.TokenDtos;
using E_Commers.Models;

namespace E_Commers.Services.AccountServices.Shared
{
	public interface IAccountServices
	{
		public  Task<Result<string>> RequestPasswordResetAsync(string email);
		public Task<Result<string>> ResetPasswordAsync(string email, string token, string newPassword);
		public Task<Result<TokensDto>> LoginAsync(string email, string password);
		public Task<Result<string>> RefreshTokenAsync(string userid, string refreshtoken);
		public Task<Result<string>> ChangePasswordAsync(string userid, string oldPassword, string newPassword);
		public Task<Result<ChangeEmailResultDto>> ChangeEmailAsync(string newEmail, string userid);
		public Task<Result<RegisterResponse>> RegisterAsync(RegisterDto model);
		public Task<Result<string>> LogoutAsync(string userid);
		public Task<Result<string>> DeleteAsync(string id);
		public Task<Result<UploadPhotoResponseDto>> UploadPhotoAsync(IFormFile image, string id);
		public Task<Result<string>> ConfirmEmailAsync(string userId, string token);
		public Task<Result<string>> ResendConfirmationEmailAsync(string email);
	}
}
