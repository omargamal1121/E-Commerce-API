namespace E_Commers.Services.EmailServices
{
	public interface IErrorNotificationService
    {
        Task SendErrorNotificationAsync(string errorMessage, string? stackTrace = null);
	}
} 