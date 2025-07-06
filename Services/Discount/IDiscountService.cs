using E_Commers.DtoModels.DiscoutDtos;
using E_Commers.DtoModels.Responses;
using E_Commers.Enums;

namespace E_Commers.Services.Discount
{
	public interface IDiscountService
	{
		// Basic CRUD Operations
		Task<Result<List<DiscountDto>>> GetAllAsync();
		Task<Result<DiscountDto>> GetDiscountByIdAsync(int id, bool? isActive = null, bool includeDeleted = false);
		Task<Result<DiscountDto>> CreateDiscountAsync(CreateDiscountDto dto, string userId);
		Task<Result<DiscountDto>> UpdateDiscountAsync(int id, UpdateDiscountDto dto, string userId);
		Task<Result<string>> DeleteDiscountAsync(int id, string userId);
		Task<Result<DiscountDto>> RestoreDiscountAsync(int id, string userId);

		// Filtering and Search
		Task<Result<List<DiscountDto>>> FilterAsync(string? search, bool? isActive, bool includeDeleted, int page, int pageSize, string role);
		Task<Result<List<DiscountDto>>> GetActiveDiscountsAsync();
		Task<Result<List<DiscountDto>>> GetExpiredDiscountsAsync();
		Task<Result<List<DiscountDto>>> GetUpcomingDiscountsAsync();
		Task<Result<List<DiscountDto>>> GetDiscountsByCategoryAsync(int categoryId);

		// Status Management
		Task<Result<string>> ActivateDiscountAsync(int id, string userId);
		Task<Result<string>> DeactivateDiscountAsync(int id, string userId);

		// Validation
		Task<Result<bool>> IsDiscountValidAsync(int id);
		Task<Result<decimal>> CalculateDiscountedPriceAsync(int discountId, decimal originalPrice);
	}
} 