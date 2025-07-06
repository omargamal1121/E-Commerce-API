using E_Commers.DtoModels.AccountDtos;
using E_Commers.DtoModels.Responses;

namespace E_Commers.Services.AccountServices.Profile
{
    public interface IProfileService
    {
        Task<Result<ChangeEmailResultDto>> ChangeEmailAsync(string newEmail, string userid);
        Task<Result<UploadPhotoResponseDto>> UploadPhotoAsync(IFormFile image, string id);
    }
} 