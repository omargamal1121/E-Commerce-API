using E_Commers.DtoModels.Responses;

namespace E_Commers.Services.AccountServices.AccountManagement
{
    public interface IAccountManagementService
    {
        Task<Result<string>> DeleteAsync(string id);
    }
} 