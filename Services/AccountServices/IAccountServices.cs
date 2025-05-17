using E_Commers.DtoModels;
using E_Commers.DtoModels.AccountDtos;
using E_Commers.DtoModels.Responses;
using E_Commers.DtoModels.TokenDtos;
using E_Commers.Models;

namespace E_Commers.Services.AccountServices
{
	public interface IAccountServices
	{
		public Task<ApiResponse<TokensDto>> LoginAsync(string email, string password);
		public Task<ApiResponse<string>> RefreshTokenAsync(string userid, string refreshtoken);
		public Task<ApiResponse<string>> ChangePasswordAsync(string userid, string oldPassword, string newPassword);
		public Task<ApiResponse<ChangeEmailResultDto>> ChangeEmailAsync(string newemail,string oldemail);
		public Task<ApiResponse<RegisterResponse>> RegisterAsync(RegisterDto model);
		public Task<ApiResponse<string>> LogoutAsync(string userid);
		public Task<ApiResponse<string>> DeleteAsync(string id);
		public Task<ApiResponse<UploadPhotoResponseDto>> UploadPhotoAsync(IFormFile image,string id);
	}
}
