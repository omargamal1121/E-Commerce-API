using E_Commers.Models;

namespace E_Commers.Interfaces
{
    public interface ICartRepository : IRepository<Cart>
    {
        Task<Cart?> GetCartByUserIdAsync(string userId);
        Task<CartItem?> GetCartItemAsync(int cartId, int productId, int? productVariantId = null);
        Task<bool> AddItemToCartAsync(int cartId, CartItem item);
        Task<bool> UpdateCartItemAsync(CartItem item);
        Task<bool> RemoveCartItemAsync(int cartId, int productId, int? productVariantId = null);
        Task<bool> ClearCartAsync(int cartId);
        Task<bool> CartExistsAsync(string userId);
        Task<int> GetCartItemCountAsync(string userId);
        Task<decimal> GetCartTotalPriceAsync(string userId);
    }
} 