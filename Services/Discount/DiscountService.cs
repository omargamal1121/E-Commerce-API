using E_Commers.DtoModels.DiscoutDtos;
using E_Commers.DtoModels.Responses;
using E_Commers.Enums;
using E_Commers.ErrorHnadling;
using E_Commers.Interfaces;
using E_Commers.Models;
using E_Commers.Services.AdminOpreationServices;
using E_Commers.Services.EmailServices;
using E_Commers.UOW;
using Microsoft.EntityFrameworkCore;

namespace E_Commers.Services.Discount
{
	public class DiscountService : IDiscountService
	{
		private readonly IUnitOfWork _unitOfWork;
		private readonly ILogger<DiscountService> _logger;
		private readonly IAdminOpreationServices _adminOpreationServices;
		private readonly IErrorNotificationService _errorNotificationService;

		public DiscountService(
			IUnitOfWork unitOfWork,
			ILogger<DiscountService> logger,
			IAdminOpreationServices adminOpreationServices,
			IErrorNotificationService errorNotificationService)
		{
			_unitOfWork = unitOfWork;
			_logger = logger;
			_adminOpreationServices = adminOpreationServices;
			_errorNotificationService = errorNotificationService;
		}

		public async Task<Result<List<DiscountDto>>> GetAllAsync()
		{
			try
			{
				var discounts = await _unitOfWork.Repository<Models.Discount>().GetAll()
					.Where(d => d.DeletedAt == null)
					.Select(d => new DiscountDto
					{
						Id = d.Id,
						Name = d.Name,
						Description = d.Description,
						DiscountPercent = d.DiscountPercent,
						StartDate = d.StartDate,
						EndDate = d.EndDate,
						IsActive = d.IsActive,
						CreatedAt = d.CreatedAt,
						ModifiedAt = d.ModifiedAt
					})
					.ToListAsync();

				if (!discounts.Any())
					return Result<List<DiscountDto>>.Fail("No discounts found", 404);

				return Result<List<DiscountDto>>.Ok(discounts, "All discounts retrieved successfully", 200);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error in GetAllAsync");
				await _errorNotificationService.SendErrorNotificationAsync(ex.Message, ex.StackTrace);
				return Result<List<DiscountDto>>.Fail("Error retrieving discounts", 500);
			}
		}

		public async Task<Result<DiscountDto>> GetDiscountByIdAsync(int id, bool? isActive = null, bool includeDeleted = false)
		{
			try
			{
				var query = _unitOfWork.Repository<Models.Discount>().GetAll().Where(d => d.Id == id);

				if (!includeDeleted)
					query = query.Where(d => d.DeletedAt == null);

				if (isActive.HasValue)
					query = query.Where(d => d.IsActive == isActive.Value);

				var discount = await query
					.Select(d => new DiscountDto
					{
						Id = d.Id,
						Name = d.Name,
						Description = d.Description,
						DiscountPercent = d.DiscountPercent,
						StartDate = d.StartDate,
						EndDate = d.EndDate,
						IsActive = d.IsActive,
						CreatedAt = d.CreatedAt,
						ModifiedAt = d.ModifiedAt,
						DeletedAt = d.DeletedAt
					})
					.FirstOrDefaultAsync();

				if (discount == null)
					return Result<DiscountDto>.Fail("Discount not found", 404);

				return Result<DiscountDto>.Ok(discount, "Discount retrieved successfully", 200);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, $"Error in GetDiscountByIdAsync for id: {id}");
				await _errorNotificationService.SendErrorNotificationAsync(ex.Message, ex.StackTrace);
				return Result<DiscountDto>.Fail("Error retrieving discount", 500);
			}
		}

		public async Task<Result<DiscountDto>> CreateDiscountAsync(CreateDiscountDto dto, string userId)
		{
			_logger.LogInformation($"Creating new discount: {dto.Name}");
			try
			{
				// Validate dates
				if (dto.StartDate >= dto.EndDate)
					return Result<DiscountDto>.Fail("Start date must be before end date", 400);

				// Check for duplicate name
				var existingDiscount = await _unitOfWork.Repository<Models.Discount>().GetAll()
					.FirstOrDefaultAsync(d => d.Name == dto.Name && d.DeletedAt == null);

				if (existingDiscount != null)
					return Result<DiscountDto>.Fail($"Discount with name '{dto.Name}' already exists", 409);

				using var transaction = await _unitOfWork.BeginTransactionAsync();

				var discount = new Models.Discount
				{
					Name = dto.Name,
					Description = dto.Description,
					DiscountPercent = dto.DiscountPercent,
					StartDate = dto.StartDate,
					EndDate = dto.EndDate,
					IsActive = dto.IsActive ?? true
				};

				var result = await _unitOfWork.Repository<Models.Discount>().CreateAsync(discount);
				if (result == null)
				{
					await transaction.RollbackAsync();
					return Result<DiscountDto>.Fail("Failed to create discount", 400);
				}

				// Log admin operation
				await _adminOpreationServices.AddAdminOpreationAsync(
					$"Create Discount {discount.Id}",
					Opreations.AddOpreation,
					userId,
					discount.Id
				);

				await _unitOfWork.CommitAsync();

				var discountDto = new DiscountDto
				{
					Id = discount.Id,
					Name = discount.Name,
					Description = discount.Description,
					DiscountPercent = discount.DiscountPercent,
					StartDate = discount.StartDate,
					EndDate = discount.EndDate,
					IsActive = discount.IsActive,
					CreatedAt = discount.CreatedAt
				};

				return Result<DiscountDto>.Ok(discountDto, "Discount created successfully", 201);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, $"Error in CreateDiscountAsync for discount {dto.Name}");
				await _errorNotificationService.SendErrorNotificationAsync(ex.Message, ex.StackTrace);
				return Result<DiscountDto>.Fail("Error creating discount", 500);
			}
		}

		public async Task<Result<DiscountDto>> UpdateDiscountAsync(int id, UpdateDiscountDto dto, string userId)
		{
			_logger.LogInformation($"Updating discount: {id}");
			try
			{
				var discount = await _unitOfWork.Repository<Models.Discount>().GetByIdAsync(id);
				if (discount == null)
					return Result<DiscountDto>.Fail("Discount not found", 404);

				using var transaction = await _unitOfWork.BeginTransactionAsync();

				// Update fields
				if (!string.IsNullOrEmpty(dto.Name))
					discount.Name = dto.Name;
				if (!string.IsNullOrEmpty(dto.Description))
					discount.Description = dto.Description;
				if (dto.DiscountPercent.HasValue)
					discount.DiscountPercent = dto.DiscountPercent.Value;
				if (dto.StartDate.HasValue)
					discount.StartDate = dto.StartDate.Value;
				if (dto.EndDate.HasValue)
					discount.EndDate = dto.EndDate.Value;
				if (dto.IsActive.HasValue)
					discount.IsActive = dto.IsActive.Value;

				// Validate dates
				if (discount.StartDate >= discount.EndDate)
					return Result<DiscountDto>.Fail("Start date must be before end date", 400);

				var result = _unitOfWork.Repository<Models.Discount>().Update(discount);
				if (!result)
				{
					await transaction.RollbackAsync();
					return Result<DiscountDto>.Fail("Failed to update discount", 400);
				}

				// Log admin operation
				await _adminOpreationServices.AddAdminOpreationAsync(
					$"Update Discount {id}",
					Opreations.UpdateOpreation,
					userId,
					id
				);

				await _unitOfWork.CommitAsync();

				var discountDto = new DiscountDto
				{
					Id = discount.Id,
					Name = discount.Name,
					Description = discount.Description,
					DiscountPercent = discount.DiscountPercent,
					StartDate = discount.StartDate,
					EndDate = discount.EndDate,
					IsActive = discount.IsActive,
					CreatedAt = discount.CreatedAt,
					ModifiedAt = discount.ModifiedAt
				};

				return Result<DiscountDto>.Ok(discountDto, "Discount updated successfully", 200);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, $"Error in UpdateDiscountAsync for id: {id}");
				await _errorNotificationService.SendErrorNotificationAsync(ex.Message, ex.StackTrace);
				return Result<DiscountDto>.Fail("Error updating discount", 500);
			}
		}

		public async Task<Result<string>> DeleteDiscountAsync(int id, string userId)
		{
			_logger.LogInformation($"Deleting discount: {id}");
			try
			{
				var discount = await _unitOfWork.Repository<Models.Discount>().GetByIdAsync(id);
				if (discount == null)
					return Result<string>.Fail("Discount not found", 404);

				using var transaction = await _unitOfWork.BeginTransactionAsync();

				var result = await _unitOfWork.Repository<Models.Discount>().SoftDeleteAsync(id);
				if (!result)
				{
					await transaction.RollbackAsync();
					return Result<string>.Fail("Failed to delete discount", 400);
				}

				// Log admin operation
				await _adminOpreationServices.AddAdminOpreationAsync(
					$"Delete Discount {id}",
					Opreations.DeleteOpreation,
					userId,
					id
				);

				await _unitOfWork.CommitAsync();
				return Result<string>.Ok("Discount deleted successfully", "Discount deleted", 200);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, $"Error in DeleteDiscountAsync for id: {id}");
				await _errorNotificationService.SendErrorNotificationAsync(ex.Message, ex.StackTrace);
				return Result<string>.Fail("Error deleting discount", 500);
			}
		}

		public async Task<Result<DiscountDto>> RestoreDiscountAsync(int id, string userId)
		{
			_logger.LogInformation($"Restoring discount: {id}");
			try
			{
				var discount = await _unitOfWork.Repository<Models.Discount>().GetByIdAsync(id);
				if (discount == null)
					return Result<DiscountDto>.Fail("Discount not found", 404);

				using var transaction = await _unitOfWork.BeginTransactionAsync();

				discount.DeletedAt = null;
				var result = _unitOfWork.Repository<Models.Discount>().Update(discount);
				if (!result)
				{
					await transaction.RollbackAsync();
					return Result<DiscountDto>.Fail("Failed to restore discount", 400);
				}

				// Log admin operation
				await _adminOpreationServices.AddAdminOpreationAsync(
					$"Restore Discount {id}",
					Opreations.UpdateOpreation,
					userId,
					id
				);

				await _unitOfWork.CommitAsync();

				var discountDto = new DiscountDto
				{
					Id = discount.Id,
					Name = discount.Name,
					Description = discount.Description,
					DiscountPercent = discount.DiscountPercent,
					StartDate = discount.StartDate,
					EndDate = discount.EndDate,
					IsActive = discount.IsActive,
					CreatedAt = discount.CreatedAt,
					ModifiedAt = discount.ModifiedAt
				};

				return Result<DiscountDto>.Ok(discountDto, "Discount restored successfully", 200);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, $"Error in RestoreDiscountAsync for id: {id}");
				await _errorNotificationService.SendErrorNotificationAsync(ex.Message, ex.StackTrace);
				return Result<DiscountDto>.Fail("Error restoring discount", 500);
			}
		}

		public async Task<Result<List<DiscountDto>>> FilterAsync(string? search, bool? isActive, bool includeDeleted, int page, int pageSize, string role)
		{
			try
			{
				var query = _unitOfWork.Repository<Models.Discount>().GetAll();

				if (!includeDeleted)
					query = query.Where(d => d.DeletedAt == null);

				if (!string.IsNullOrWhiteSpace(search))
					query = query.Where(d => d.Name.Contains(search) || d.Description.Contains(search));

				if (isActive.HasValue)
					query = query.Where(d => d.IsActive == isActive.Value);

				var totalCount = await query.CountAsync();

				var discounts = await query
					.OrderBy(d => d.CreatedAt)
					.Skip((page - 1) * pageSize)
					.Take(pageSize)
					.Select(d => new DiscountDto
					{
						Id = d.Id,
						Name = d.Name,
						Description = d.Description,
						DiscountPercent = d.DiscountPercent,
						StartDate = d.StartDate,
						EndDate = d.EndDate,
						IsActive = d.IsActive,
						CreatedAt = d.CreatedAt,
						ModifiedAt = d.ModifiedAt
					})
					.ToListAsync();

				if (!discounts.Any())
					return Result<List<DiscountDto>>.Fail("No discounts found matching criteria", 404);

				return Result<List<DiscountDto>>.Ok(discounts, $"Found {discounts.Count} discounts out of {totalCount}", 200);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error in FilterAsync");
				await _errorNotificationService.SendErrorNotificationAsync(ex.Message, ex.StackTrace);
				return Result<List<DiscountDto>>.Fail("Error filtering discounts", 500);
			}
		}

		public async Task<Result<List<DiscountDto>>> GetActiveDiscountsAsync()
		{
			try
			{
				var now = DateTime.UtcNow;
				var discounts = await _unitOfWork.Repository<Models.Discount>().GetAll()
					.Where(d => d.DeletedAt == null && 
						d.IsActive && 
						d.StartDate <= now && 
						d.EndDate >= now)
					.Select(d => new DiscountDto
					{
						Id = d.Id,
						Name = d.Name,
						Description = d.Description,
						DiscountPercent = d.DiscountPercent,
						StartDate = d.StartDate,
						EndDate = d.EndDate,
						IsActive = d.IsActive,
						CreatedAt = d.CreatedAt
					})
					.ToListAsync();

				if (!discounts.Any())
					return Result<List<DiscountDto>>.Fail("No active discounts found", 404);

				return Result<List<DiscountDto>>.Ok(discounts, "Active discounts retrieved successfully", 200);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error in GetActiveDiscountsAsync");
				await _errorNotificationService.SendErrorNotificationAsync(ex.Message, ex.StackTrace);
				return Result<List<DiscountDto>>.Fail("Error retrieving active discounts", 500);
			}
		}

		public async Task<Result<List<DiscountDto>>> GetExpiredDiscountsAsync()
		{
			try
			{
				var now = DateTime.UtcNow;
				var discounts = await _unitOfWork.Repository<Models.Discount>().GetAll()
					.Where(d => d.DeletedAt == null && d.EndDate < now)
					.Select(d => new DiscountDto
					{
						Id = d.Id,
						Name = d.Name,
						Description = d.Description,
						DiscountPercent = d.DiscountPercent,
						StartDate = d.StartDate,
						EndDate = d.EndDate,
						IsActive = d.IsActive,
						CreatedAt = d.CreatedAt
					})
					.ToListAsync();

				if (!discounts.Any())
					return Result<List<DiscountDto>>.Fail("No expired discounts found", 404);

				return Result<List<DiscountDto>>.Ok(discounts, "Expired discounts retrieved successfully", 200);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error in GetExpiredDiscountsAsync");
				await _errorNotificationService.SendErrorNotificationAsync(ex.Message, ex.StackTrace);
				return Result<List<DiscountDto>>.Fail("Error retrieving expired discounts", 500);
			}
		}

		public async Task<Result<List<DiscountDto>>> GetUpcomingDiscountsAsync()
		{
			try
			{
				var now = DateTime.UtcNow;
				var discounts = await _unitOfWork.Repository<Models.Discount>().GetAll()
					.Where(d => d.DeletedAt == null && d.StartDate > now)
					.Select(d => new DiscountDto
					{
						Id = d.Id,
						Name = d.Name,
						Description = d.Description,
						DiscountPercent = d.DiscountPercent,
						StartDate = d.StartDate,
						EndDate = d.EndDate,
						IsActive = d.IsActive,
						CreatedAt = d.CreatedAt
					})
					.ToListAsync();

				if (!discounts.Any())
					return Result<List<DiscountDto>>.Fail("No upcoming discounts found", 404);

				return Result<List<DiscountDto>>.Ok(discounts, "Upcoming discounts retrieved successfully", 200);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error in GetUpcomingDiscountsAsync");
				await _errorNotificationService.SendErrorNotificationAsync(ex.Message, ex.StackTrace);
				return Result<List<DiscountDto>>.Fail("Error retrieving upcoming discounts", 500);
			}
		}

		public async Task<Result<List<DiscountDto>>> GetDiscountsByCategoryAsync(int categoryId)
		{
			try
			{
				var discounts = await _unitOfWork.Repository<Models.Discount>().GetAll()
					.Where(d => d.DeletedAt == null && d.CategoryId == categoryId)
					.Select(d => new DiscountDto
					{
						Id = d.Id,
						Name = d.Name,
						Description = d.Description,
						DiscountPercent = d.DiscountPercent,
						StartDate = d.StartDate,
						EndDate = d.EndDate,
						IsActive = d.IsActive,
						CreatedAt = d.CreatedAt
					})
					.ToListAsync();

				if (!discounts.Any())
					return Result<List<DiscountDto>>.Fail($"No discounts found for category {categoryId}", 404);

				return Result<List<DiscountDto>>.Ok(discounts, "Category discounts retrieved successfully", 200);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, $"Error in GetDiscountsByCategoryAsync for categoryId: {categoryId}");
				await _errorNotificationService.SendErrorNotificationAsync(ex.Message, ex.StackTrace);
				return Result<List<DiscountDto>>.Fail("Error retrieving category discounts", 500);
			}
		}

		public async Task<Result<string>> ActivateDiscountAsync(int id, string userId)
		{
			_logger.LogInformation($"Activating discount: {id}");
			try
			{
				var discount = await _unitOfWork.Repository<Models.Discount>().GetByIdAsync(id);
				if (discount == null)
					return Result<string>.Fail("Discount not found", 404);

				if (discount.IsActive)
					return Result<string>.Fail("Discount is already active", 400);

				using var transaction = await _unitOfWork.BeginTransactionAsync();

				discount.IsActive = true;
				var result = _unitOfWork.Repository<Models.Discount>().Update(discount);
				if (!result)
				{
					await transaction.RollbackAsync();
					return Result<string>.Fail("Failed to activate discount", 400);
				}

				// Log admin operation
				await _adminOpreationServices.AddAdminOpreationAsync(
					$"Activate Discount {id}",
					Opreations.UpdateOpreation,
					userId,
					id
				);

				await _unitOfWork.CommitAsync();
				return Result<string>.Ok("Discount activated successfully", "Discount activated", 200);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, $"Error in ActivateDiscountAsync for id: {id}");
				await _errorNotificationService.SendErrorNotificationAsync(ex.Message, ex.StackTrace);
				return Result<string>.Fail("Error activating discount", 500);
			}
		}

		public async Task<Result<string>> DeactivateDiscountAsync(int id, string userId)
		{
			_logger.LogInformation($"Deactivating discount: {id}");
			try
			{
				var discount = await _unitOfWork.Repository<Models.Discount>().GetByIdAsync(id);
				if (discount == null)
					return Result<string>.Fail("Discount not found", 404);

				if (!discount.IsActive)
					return Result<string>.Fail("Discount is already inactive", 400);

				using var transaction = await _unitOfWork.BeginTransactionAsync();

				discount.IsActive = false;
				var result = _unitOfWork.Repository<Models.Discount>().Update(discount);
				if (!result)
				{
					await transaction.RollbackAsync();
					return Result<string>.Fail("Failed to deactivate discount", 400);
				}

				// Log admin operation
				await _adminOpreationServices.AddAdminOpreationAsync(
					$"Deactivate Discount {id}",
					Opreations.UpdateOpreation,
					userId,
					id
				);

				await _unitOfWork.CommitAsync();
				return Result<string>.Ok("Discount deactivated successfully", "Discount deactivated", 200);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, $"Error in DeactivateDiscountAsync for id: {id}");
				await _errorNotificationService.SendErrorNotificationAsync(ex.Message, ex.StackTrace);
				return Result<string>.Fail("Error deactivating discount", 500);
			}
		}

		public async Task<Result<bool>> IsDiscountValidAsync(int id)
		{
			try
			{
				var now = DateTime.UtcNow;
				var discount = await _unitOfWork.Repository<Models.Discount>().GetAll()
					.Where(d => d.Id == id && d.DeletedAt == null)
					.FirstOrDefaultAsync();

				if (discount == null)
					return Result<bool>.Ok(false, "Discount not found", 200);

				var isValid = discount.IsActive && 
					discount.StartDate <= now && 
					discount.EndDate >= now;

				return Result<bool>.Ok(isValid, "Discount validation completed", 200);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, $"Error in IsDiscountValidAsync for id: {id}");
				await _errorNotificationService.SendErrorNotificationAsync(ex.Message, ex.StackTrace);
				return Result<bool>.Fail("Error validating discount", 500);
			}
		}

		public async Task<Result<decimal>> CalculateDiscountedPriceAsync(int discountId, decimal originalPrice)
		{
			try
			{
				var discount = await _unitOfWork.Repository<Models.Discount>().GetByIdAsync(discountId);
				if (discount == null)
					return Result<decimal>.Fail("Discount not found", 404);

				var now = DateTime.UtcNow;
				if (!discount.IsActive || discount.StartDate > now || discount.EndDate < now)
					return Result<decimal>.Ok(originalPrice, "Discount not valid, returning original price", 200);

				var discountAmount = originalPrice * (discount.DiscountPercent / 100m);
				var discountedPrice = originalPrice - discountAmount;

				return Result<decimal>.Ok(discountedPrice, "Discounted price calculated successfully", 200);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, $"Error in CalculateDiscountedPriceAsync for discountId: {discountId}");
				await _errorNotificationService.SendErrorNotificationAsync(ex.Message, ex.StackTrace);
				return Result<decimal>.Fail("Error calculating discounted price", 500);
			}
		}
	}
} 