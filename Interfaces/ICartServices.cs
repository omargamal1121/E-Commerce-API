using E_Commers.DtoModels.CartDtos;
using E_Commers.DtoModels.Responses;

namespace E_Commers.Interfaces
{
    public interface ICartServices
    {
        Task<Result<CartDto>> GetCartAsync(string userId);
        Task<Result<CartDto>> AddItemToCartAsync(string userId, CreateCartItemDto itemDto);
        Task<Result<CartDto>> UpdateCartItemAsync(string userId, int productId, UpdateCartItemDto itemDto, int? productVariantId = null);
        Task<Result<CartDto>> RemoveItemFromCartAsync(string userId, RemoveCartItemDto itemDto);
        Task<Result<string>> ClearCartAsync(string userId);
        Task<Result<int>> GetCartItemCountAsync(string userId);
        Task<Result<decimal>> GetCartTotalPriceAsync(string userId);
        Task<Result<bool>> IsCartEmptyAsync(string userId);
    }
} 