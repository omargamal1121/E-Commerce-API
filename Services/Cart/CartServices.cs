using AutoMapper;
using E_Commers.DtoModels.CartDtos;
using E_Commers.DtoModels.Responses;
using E_Commers.Enums;
using E_Commers.ErrorHnadling;
using E_Commers.Interfaces;
using E_Commers.Models;
using E_Commers.Services.AdminOpreationServices;
using E_Commers.Services.Cache;
using E_Commers.Services.EmailServices;
using E_Commers.UOW;
using Hangfire;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace E_Commers.Services.Cart
{
    public class CartServices : ICartServices
    {
        private readonly ILogger<CartServices> _logger;
        private readonly IErrorNotificationService _errorNotificationService;
        private readonly UserManager<Customer>_userManager;
		private readonly IMapper _mapper;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICartRepository _cartRepository;
        private readonly IAdminOpreationServices _adminOperationServices;
        private readonly ICacheManager _cacheManager;
        private const string CACHE_TAG_CART = "cart";

        public CartServices(
            UserManager<Customer> userManager,
            IErrorNotificationService errorNotificationService,
			ILogger<CartServices> logger,
            IMapper mapper,
            IUnitOfWork unitOfWork,
            ICartRepository cartRepository,
            IAdminOpreationServices adminOperationServices,
            ICacheManager cacheManager)
        { 
            _userManager = userManager;
            _errorNotificationService = errorNotificationService;
			_logger = logger;
            _mapper = mapper;
            _unitOfWork = unitOfWork;
            _cartRepository = cartRepository;
            _adminOperationServices = adminOperationServices;
            _cacheManager = cacheManager;
        }

        private void NotifyAdminOfError(string message, string? stackTrace = null)
        {
            BackgroundJob.Enqueue<IErrorNotificationService>(_ => _.SendErrorNotificationAsync(message, stackTrace));
        }

        public async Task<Result<CartDto>> GetCartAsync(string userId)
        {
            _logger.LogInformation($"Getting cart for user: {userId}");

            var cacheKey = $"{CACHE_TAG_CART}_user_{userId}";
            var cached = await _cacheManager.GetAsync<CartDto>(cacheKey);
            if (cached != null)
            {
                _logger.LogInformation($"Cache hit for cart user: {userId}");
                return Result<CartDto>.Ok(cached, "Cart retrieved from cache", 200);
            }

            try
            {
                var cart = await _cartRepository.GetCartByUserIdAsync(userId);
                if (cart == null)
                {
                    // Create a new cart if it doesn't exist
                    cart = await CreateNewCartAsync(userId);
                    if (cart == null)
                    {
                        return Result<CartDto>.Fail("Failed to create cart", 500);
                    }
                }

                var cartDto = _mapper.Map<CartDto>(cart);
                await _cacheManager.SetAsync(cacheKey, cartDto, tags: new[] { CACHE_TAG_CART });

                return Result<CartDto>.Ok(cartDto, "Cart retrieved successfully", 200);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting cart for user {userId}: {ex.Message}");
                NotifyAdminOfError($"Error getting cart for user {userId}: {ex.Message}", ex.StackTrace);
                return Result<CartDto>.Fail("An error occurred while retrieving cart", 500);
            }
        }

        public async Task<Result<CartDto>> AddItemToCartAsync(string userId, CreateCartItemDto itemDto)
        {
            _logger.LogInformation($"Adding item to cart for user: {userId}, product: {itemDto.ProductId}");

            using var transaction = await _unitOfWork.BeginTransactionAsync();
            try
            {
                // Validate product exists and has sufficient quantity
                var product = await _unitOfWork.Product.GetAll()
                    .Where(p => p.Id == itemDto.ProductId && p.DeletedAt == null)
                    .Include(p => p.ProductVariants.Where(v => v.DeletedAt == null))
                    .Include(p => p.Discount)
                    .FirstOrDefaultAsync();

                if (product == null)
                {
                    await transaction.RollbackAsync();
                    return Result<CartDto>.Fail("Product not found", 404);
                }

                // Check if product variant exists if specified
                if (itemDto.ProductVariantId.HasValue)
                {
                    var variant = product.ProductVariants.FirstOrDefault(v => v.Id == itemDto.ProductVariantId);
                    if (variant == null)
                    {
                        await transaction.RollbackAsync();
                        return Result<CartDto>.Fail("Product variant not found", 404);
                    }

                    if (variant.Quantity < itemDto.Quantity)
                    {
                        await transaction.RollbackAsync();
                        return Result<CartDto>.Fail($"Insufficient quantity. Available: {variant.Quantity}", 400);
                    }
                }
                else
                {
                    if (product.Quantity < itemDto.Quantity)
                    {
                        await transaction.RollbackAsync();
                        return Result<CartDto>.Fail($"Insufficient quantity. Available: {product.Quantity}", 400);
                    }
                }

                // Get or create cart
                var cart = await _cartRepository.GetCartByUserIdAsync(userId);
                if (cart == null)
                {
                    cart = await CreateNewCartAsync(userId);
                    if (cart == null)
                    {
                        await transaction.RollbackAsync();
                        return Result<CartDto>.Fail("Failed to create cart", 500);
                    }
                }

                // Check if item already exists in cart
                var existingItem = await _cartRepository.GetCartItemAsync(cart.Id, itemDto.ProductId, itemDto.ProductVariantId);
                if (existingItem != null)
                {
                    // Update quantity
                    existingItem.Quantity += itemDto.Quantity;
                    existingItem.ModifiedAt = DateTime.UtcNow;
                    
                    var updateResult = await _cartRepository.UpdateCartItemAsync(existingItem);
                    if (!updateResult)
                    {
                        await transaction.RollbackAsync();
                        return Result<CartDto>.Fail("Failed to update cart item", 500);
                    }
                }
                else
                {
                    // Add new item
                    var cartItem = new CartItem
                    {
                        CartId = cart.Id,
                        ProductId = itemDto.ProductId,
                        ProductVariantId = itemDto.ProductVariantId,
                        Quantity = itemDto.Quantity,
                        AddedAt = DateTime.UtcNow
                    };

                    var addResult = await _cartRepository.AddItemToCartAsync(cart.Id, cartItem);
                    if (!addResult)
                    {
                        await transaction.RollbackAsync();
                        return Result<CartDto>.Fail("Failed to add item to cart", 500);
                    }
                }

                // Log admin operation
                var adminLog = await _adminOperationServices.AddAdminOpreationAsync(
                    $"Added item to cart - Product: {itemDto.ProductId}, Quantity: {itemDto.Quantity}",
                    Opreations.UpdateOpreation,
                    userId,
                    cart.Id
                );

                if (!adminLog.Success)
                {
                    _logger.LogWarning($"Failed to log admin operation: {adminLog.Message}");
                }

                await _unitOfWork.CommitAsync();
                await transaction.CommitAsync();

                // Clear cache and return updated cart
                await _cacheManager.RemoveByTagAsync(CACHE_TAG_CART);
                var updatedCart = await _cartRepository.GetCartByUserIdAsync(userId);
                var cartDto = _mapper.Map<CartDto>(updatedCart);

                return Result<CartDto>.Ok(cartDto, "Item added to cart successfully", 200);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError($"Error adding item to cart for user {userId}: {ex.Message}");
                NotifyAdminOfError($"Error adding item to cart for user {userId}: {ex.Message}", ex.StackTrace);
                return Result<CartDto>.Fail("An error occurred while adding item to cart", 500);
            }
        }

        public async Task<Result<CartDto>> UpdateCartItemAsync(string userId, int productId, UpdateCartItemDto itemDto, int? productVariantId = null)
        {
            _logger.LogInformation($"Updating cart item for user: {userId}, product: {productId}");

            using var transaction = await _unitOfWork.BeginTransactionAsync();
            try
            {
                var cart = await _cartRepository.GetCartByUserIdAsync(userId);
                if (cart == null)
                {
                    await transaction.RollbackAsync();
                    return Result<CartDto>.Fail("Cart not found", 404);
                }

                var cartItem = await _cartRepository.GetCartItemAsync(cart.Id, productId, productVariantId);
                if (cartItem == null)
                {
                    await transaction.RollbackAsync();
                    return Result<CartDto>.Fail("Cart item not found", 404);
                }

                // Validate quantity
                var product = await _unitOfWork.Product.GetAll()
                    .Where(p => p.Id == productId && p.DeletedAt == null)
                    .Include(p => p.ProductVariants.Where(v => v.DeletedAt == null))
                    .FirstOrDefaultAsync();

                if (product == null)
                {
                    await transaction.RollbackAsync();
                    return Result<CartDto>.Fail("Product not found", 404);
                }

                if (productVariantId.HasValue)
                {
                    var variant = product.ProductVariants.FirstOrDefault(v => v.Id == productVariantId);
                    if (variant == null)
                    {
                        await transaction.RollbackAsync();
                        return Result<CartDto>.Fail("Product variant not found", 404);
                    }

                    if (variant.Quantity < itemDto.Quantity)
                    {
                        await transaction.RollbackAsync();
                        return Result<CartDto>.Fail($"Insufficient quantity. Available: {variant.Quantity}", 400);
                    }
                }
                else
                {
                    if (product.Quantity < itemDto.Quantity)
                    {
                        await transaction.RollbackAsync();
                        return Result<CartDto>.Fail($"Insufficient quantity. Available: {product.Quantity}", 400);
                    }
                }

                cartItem.Quantity = itemDto.Quantity;
                cartItem.ModifiedAt = DateTime.UtcNow;

                var updateResult = await _cartRepository.UpdateCartItemAsync(cartItem);
                if (!updateResult)
                {
                    await transaction.RollbackAsync();
                    return Result<CartDto>.Fail("Failed to update cart item", 500);
                }

                // Log admin operation
                var adminLog = await _adminOperationServices.AddAdminOpreationAsync(
                    $"Updated cart item - Product: {productId}, Quantity: {itemDto.Quantity}",
                    Opreations.UpdateOpreation,
                    userId,
                    cart.Id
                );

                if (!adminLog.Success)
                {
                    _logger.LogWarning($"Failed to log admin operation: {adminLog.Message}");
                }

                await _unitOfWork.CommitAsync();
                await transaction.CommitAsync();

                // Clear cache and return updated cart
                await _cacheManager.RemoveByTagAsync(CACHE_TAG_CART);
                var updatedCart = await _cartRepository.GetCartByUserIdAsync(userId);
                var cartDto = _mapper.Map<CartDto>(updatedCart);

                return Result<CartDto>.Ok(cartDto, "Cart item updated successfully", 200);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError($"Error updating cart item for user {userId}: {ex.Message}");
                NotifyAdminOfError($"Error updating cart item for user {userId}: {ex.Message}", ex.StackTrace);
                return Result<CartDto>.Fail("An error occurred while updating cart item", 500);
            }
        }

        public async Task<Result<CartDto>> RemoveItemFromCartAsync(string userId, RemoveCartItemDto itemDto)
        {
            _logger.LogInformation($"Removing item from cart for user: {userId}, product: {itemDto.ProductId}");

            using var transaction = await _unitOfWork.BeginTransactionAsync();
            try
            {
                var cart = await _cartRepository.GetCartByUserIdAsync(userId);
                if (cart == null)
                {
                    await transaction.RollbackAsync();
                    return Result<CartDto>.Fail("Cart not found", 404);
                }

                var removeResult = await _cartRepository.RemoveCartItemAsync(cart.Id, itemDto.ProductId, itemDto.ProductVariantId);
                if (!removeResult)
                {
                    await transaction.RollbackAsync();
                    return Result<CartDto>.Fail("Cart item not found", 404);
                }

                // Log admin operation
                var adminLog = await _adminOperationServices.AddAdminOpreationAsync(
                    $"Removed item from cart - Product: {itemDto.ProductId}",
                    Opreations.UpdateOpreation,
                    userId,
                    cart.Id
                );

                if (!adminLog.Success)
                {
                    _logger.LogWarning($"Failed to log admin operation: {adminLog.Message}");
                }

                await _unitOfWork.CommitAsync();
                await transaction.CommitAsync();

                // Clear cache and return updated cart
                await _cacheManager.RemoveByTagAsync(CACHE_TAG_CART);
                var updatedCart = await _cartRepository.GetCartByUserIdAsync(userId);
                var cartDto = _mapper.Map<CartDto>(updatedCart);

                return Result<CartDto>.Ok(cartDto, "Item removed from cart successfully", 200);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError($"Error removing item from cart for user {userId}: {ex.Message}");
                NotifyAdminOfError($"Error removing item from cart for user {userId}: {ex.Message}", ex.StackTrace);
                return Result<CartDto>.Fail("An error occurred while removing item from cart", 500);
            }
        }

        public async Task<Result<string>> ClearCartAsync(string userId)
        {
            _logger.LogInformation($"Clearing cart for user: {userId}");

            using var transaction = await _unitOfWork.BeginTransactionAsync();
            try
            {
                var cart = await _cartRepository.GetCartByUserIdAsync(userId);
                if (cart == null)
                {
                    await transaction.RollbackAsync();
                    return Result<string>.Fail("Cart not found", 404);
                }

                var clearResult = await _cartRepository.ClearCartAsync(cart.Id);
                if (!clearResult)
                {
                    await transaction.RollbackAsync();
                    return Result<string>.Fail("Failed to clear cart", 500);
                }

                // Log admin operation
                var adminLog = await _adminOperationServices.AddAdminOpreationAsync(
                    "Cleared cart",
                    Opreations.UpdateOpreation,
                    userId,
                    cart.Id
                );

                if (!adminLog.Success)
                {
                    _logger.LogWarning($"Failed to log admin operation: {adminLog.Message}");
                }

                await _unitOfWork.CommitAsync();
                await transaction.CommitAsync();

                // Clear cache
                await _cacheManager.RemoveByTagAsync(CACHE_TAG_CART);

                return Result<string>.Ok(null, "Cart cleared successfully", 200);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError($"Error clearing cart for user {userId}: {ex.Message}");
                NotifyAdminOfError($"Error clearing cart for user {userId}: {ex.Message}", ex.StackTrace);
                return Result<string>.Fail("An error occurred while clearing cart", 500);
            }
        }

        public async Task<Result<int?>> GetCartItemCountAsync(string userId)
        {
            try
            {
                var count = await _cartRepository.GetCartItemCountAsync(userId);
                return Result<int?>.Ok(count, "Cart item count retrieved", 200);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting cart item count for user {userId}: {ex.Message}");
                return Result<int?>.Fail("An error occurred while getting cart item count", 500);
            }
        }

        public async Task<Result<decimal>> GetCartTotalPriceAsync(string userId)
        {
            try
            {
                var total = await _cartRepository.GetCartTotalPriceAsync(userId);
                return Result<decimal>.Ok(total, "Cart total price retrieved", 200);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting cart total price for user {userId}: {ex.Message}");
                return Result<decimal>.Fail("An error occurred while getting cart total price", 500);
            }
        }

        public async Task<Result<bool>> IsCartEmptyAsync(string userId)
        {
            try
            {
                var cart = await _cartRepository.GetCartByUserIdAsync(userId);
                var isEmpty = cart?.IsEmpty ?? true;
                return Result<bool>.Ok(isEmpty, "Cart empty status retrieved", 200);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error checking if cart is empty for user {userId}: {ex.Message}");
                return Result<bool>.Fail("An error occurred while checking cart status", 500);
            }
        }

        private async Task<E_Commers.Models.Cart?> CreateNewCartAsync(string userId)
        {
            try
            {
                var customer = await _userManager.FindByIdAsync (userId);
                if (customer == null)
                {
                    _logger.LogWarning($"Customer not found for user: {userId}");
                    return null;
                }

                var cart = new Models.Cart
                {
                    UserId = userId,
                    CustomerId = userId,
                    CreatedAt = DateTime.UtcNow
                };

                var createdCart = await _cartRepository.CreateAsync(cart);
                if (createdCart == null)
                {
                    _logger.LogError($"Failed to create cart for user: {userId}");
                    return null;
                }

                return createdCart;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error creating new cart for user {userId}: {ex.Message}");
                return null;
            }
        }
    }
} 