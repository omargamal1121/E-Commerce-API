using E_Commers.DtoModels.CollectionDtos;
using E_Commers.DtoModels.Responses;
using E_Commers.Services;

namespace E_Commers.Interfaces
{
    public interface ICollectionServices
    {
        Task<Result<CollectionDto>> GetCollectionByIdAsync(int collectionId);
        Task<Result<CollectionDto>> GetCollectionByNameAsync(string name);
        Task<Result<List<CollectionDto>>> GetActiveCollectionsAsync();
        Task<Result<List<CollectionDto>>> GetCollectionsByDisplayOrderAsync();
        Task<Result<List<CollectionDto>>> GetCollectionsWithPaginationAsync(int page, int pageSize, bool? isActive = null);
        Task<Result<int?>> GetTotalCollectionCountAsync(bool? isActive = null);
        Task<Result<CollectionDto>> CreateCollectionAsync(CreateCollectionDto collectionDto, string userRole);
        Task<Result<CollectionDto>> UpdateCollectionAsync(int collectionId, UpdateCollectionDto collectionDto, string userRole);
        Task<Result<string>> DeleteCollectionAsync(int collectionId, string userRole);
        Task<Result<string>> AddProductsToCollectionAsync(int collectionId, AddProductsToCollectionDto productsDto, string userRole);
        Task<Result<string>> RemoveProductsFromCollectionAsync(int collectionId, RemoveProductsFromCollectionDto productsDto, string userRole);
        Task<Result<List<CollectionDto>>> GetCollectionsByProductAsync(int productId);
        Task<Result<string>> UpdateCollectionStatusAsync(int collectionId, bool isActive, string userRole);
        Task<Result<string>> UpdateCollectionDisplayOrderAsync(int collectionId, int displayOrder, string userRole);
        Task<Result<List<CollectionDto>>> SearchCollectionsAsync(string searchTerm);
        Task<Result<CollectionSummaryDto>> GetCollectionSummaryAsync(int collectionId);
        Task<Result<List<CollectionSummaryDto>>> GetCollectionSummariesAsync(int page, int pageSize, bool? isActive = null);
    }
} 