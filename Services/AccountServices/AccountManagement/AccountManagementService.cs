using E_Commers.DtoModels.Responses;
using E_Commers.Enums;
using E_Commers.ErrorHnadling;
using E_Commers.Models;
using E_Commers.Services.EmailServices;
using E_Commers.UOW;
using Hangfire;
using Microsoft.AspNetCore.Identity;

namespace E_Commers.Services.AccountServices.AccountManagement
{
    public class AccountManagementService : IAccountManagementService
    {
        private readonly ILogger<AccountManagementService> _logger;
        private readonly UserManager<Customer> _userManager;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IErrorNotificationService _errorNotificationService;

        public AccountManagementService(
            ILogger<AccountManagementService> logger,
            UserManager<Customer> userManager,
            IUnitOfWork unitOfWork,
            IErrorNotificationService errorNotificationService)
        {
            _logger = logger;
            _userManager = userManager;
            _unitOfWork = unitOfWork;
            _errorNotificationService = errorNotificationService;
        }

        public async Task<Result<string>> DeleteAsync(string id)
        {
            _logger.LogInformation($"Execute :{nameof(DeleteAsync)} in services");
            using var Tran = await _unitOfWork.BeginTransactionAsync();
            try
            {
                Customer? customer = await _userManager.FindByIdAsync(id);
                if (customer is null)
                {
                    _logger.LogError($"Can't find Customer with this id: {id}");
                    return Result<string>.Fail($"Can't find Customer with this id: {id}", 401);
                }
                customer.DeletedAt = DateTime.Now;
                var isupdated = await _userManager.UpdateAsync(customer);
                if (!isupdated.Succeeded)
                {
                    var errors = string.Join(", ", isupdated.Errors.Select(e => e.Description));
                    _logger.LogError($"Can't update Customer: {customer.Id}. Errors: {errors}");
                    return Result<string>.Fail($"Can't delete account now. Errors: {errors}", 500);
                }
                BackgroundJob.Enqueue<AccountManagementService>(s => s.AddOperationAsync(customer.Id, "Delete Account", Opreations.DeleteOpreation));
                await _unitOfWork.CommitAsync();
                await Tran.CommitAsync();
                _logger.LogInformation("Soft deleted is done");
                return Result<string>.Ok("Deleted", "Deleted", 200);
            }
            catch (Exception ex)
            {
                await Tran.RollbackAsync();
                BackgroundJob.Enqueue<IErrorNotificationService>(e =>
                    e.SendErrorNotificationAsync(ex.Message, ex.StackTrace)
                );
                _logger.LogError($"Error:{ex.Message}");
                return Result<string>.Fail($"Error:{ex.Message}", 500);
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