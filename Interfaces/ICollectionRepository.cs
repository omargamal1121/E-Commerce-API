using E_Commers.Models;

namespace E_Commers.Interfaces
{
    public interface ICollectionRepository : IRepository<Collection>
    {
        Task<Collection?> GetCollectionByIdAsync(int collectionId);
        Task<Collection?> GetCollectionByNameAsync(string name);
        Task<List<Collection>> GetActiveCollectionsAsync();
        Task<List<Collection>> GetCollectionsByDisplayOrderAsync();
        Task<List<Collection>> GetCollectionsWithPaginationAsync(int page, int pageSize, bool? isActive = null);
        Task<int> GetTotalCollectionCountAsync(bool? isActive = null);
        Task<bool> AddProductsToCollectionAsync(int collectionId, List<int> productIds);
        Task<bool> RemoveProductsFromCollectionAsync(int collectionId, List<int> productIds);
        Task<List<Collection>> GetCollectionsByProductAsync(int productId);
        Task<bool> UpdateCollectionStatusAsync(int collectionId, bool isActive);
        Task<bool> UpdateCollectionDisplayOrderAsync(int collectionId, int displayOrder);
        Task<List<Collection>> SearchCollectionsAsync(string searchTerm);
        Task<int> GetProductCountInCollectionAsync(int collectionId);
        Task<decimal> GetMinPriceInCollectionAsync(int collectionId);
        Task<decimal> GetMaxPriceInCollectionAsync(int collectionId);
        Task<decimal> GetAveragePriceInCollectionAsync(int collectionId);
    }
} 