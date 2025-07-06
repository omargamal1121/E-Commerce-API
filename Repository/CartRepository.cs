using E_Commers.Context;
using E_Commers.Interfaces;
using E_Commers.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace E_Commers.Repository
{
    public class CartRepository : MainRepository<Cart>, ICartRepository
    {
        private readonly AppDbContext _context;
        private readonly ILogger<CartRepository> _logger;

        public CartRepository(AppDbContext context, ILogger<CartRepository> logger) 
            : base(context, logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<Cart?> GetCartByUserIdAsync(string userId)
        {
            _logger.LogInformation($"Getting cart for user: {userId}");
            
            return await _context.Carts
                .Where(c => c.UserId == userId && c.DeletedAt == null)
                .Include(c => c.Customer)
                .Include(c => c.Items.Where(i => i.DeletedAt == null))
                .ThenInclude(i => i.Product)
                .ThenInclude(p => p.ProductVariants.Where(v => v.DeletedAt == null))
                .Include(c => c.Items.Where(i => i.DeletedAt == null))
                .ThenInclude(i => i.Product)
                .ThenInclude(p => p.Discount)
                .Include(c => c.Items.Where(i => i.DeletedAt == null))
                .ThenInclude(i => i.Product)
                .ThenInclude(p => p.Images.Where(img => img.DeletedAt == null))
                .Include(c => c.Items.Where(i => i.DeletedAt == null))
                .ThenInclude(i => i.ProductVariant)
                .FirstOrDefaultAsync();
        }

        public async Task<CartItem?> GetCartItemAsync(int cartId, int productId, int? productVariantId = null)
        {
            _logger.LogInformation($"Getting cart item for cart: {cartId}, product: {productId}, variant: {productVariantId}");
            
            var query = _context.CartItems
                .Where(i => i.CartId == cartId && i.ProductId == productId && i.DeletedAt == null);

            if (productVariantId.HasValue)
            {
                query = query.Where(i => i.ProductVariantId == productVariantId);
            }
            else
            {
                query = query.Where(i => i.ProductVariantId == null);
            }

            return await query
                .Include(i => i.Product)
                .ThenInclude(p => p.ProductVariants.Where(v => v.DeletedAt == null))
                .Include(i => i.Product)
                .ThenInclude(p => p.Discount)
                .Include(i => i.ProductVariant)
                .FirstOrDefaultAsync();
        }

        public async Task<bool> AddItemToCartAsync(int cartId, CartItem item)
        {
            _logger.LogInformation($"Adding item to cart: {cartId}");
            
            try
            {
                item.CartId = cartId;
                await _context.CartItems.AddAsync(item);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error adding item to cart: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> UpdateCartItemAsync(CartItem item)
        {
            _logger.LogInformation($"Updating cart item: {item.Id}");
            
            try
            {
                _context.CartItems.Update(item);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error updating cart item: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> RemoveCartItemAsync(int cartId, int productId, int? productVariantId = null)
        {
            _logger.LogInformation($"Removing item from cart: {cartId}, product: {productId}, variant: {productVariantId}");
            
            try
            {
                var item = await GetCartItemAsync(cartId, productId, productVariantId);
                if (item == null)
                {
                    _logger.LogWarning($"Cart item not found for removal");
                    return false;
                }

                item.DeletedAt = DateTime.UtcNow;
                _context.CartItems.Update(item);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error removing cart item: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> ClearCartAsync(int cartId)
        {
            _logger.LogInformation($"Clearing cart: {cartId}");
            
            try
            {
                var items = await _context.CartItems
                    .Where(i => i.CartId == cartId && i.DeletedAt == null)
                    .ToListAsync();

                foreach (var item in items)
                {
                    item.DeletedAt = DateTime.UtcNow;
                }

                _context.CartItems.UpdateRange(items);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error clearing cart: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> CartExistsAsync(string userId)
        {
            return await _context.Carts
                .AnyAsync(c => c.UserId == userId && c.DeletedAt == null);
        }

        public async Task<int> GetCartItemCountAsync(string userId)
        {
            return await _context.Carts
                .Where(c => c.UserId == userId && c.DeletedAt == null)
                .SelectMany(c => c.Items.Where(i => i.DeletedAt == null))
                .SumAsync(i => i.Quantity);
        }

        public async Task<decimal> GetCartTotalPriceAsync(string userId)
        {
            var cart = await GetCartByUserIdAsync(userId);
            return cart?.TotalPrice ?? 0;
        }
    }
} 