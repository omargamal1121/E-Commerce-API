using AutoMapper;
using E_Commers.DtoModels.CollectionDtos;
using E_Commers.DtoModels.Responses;
using E_Commers.ErrorHnadling;
using E_Commers.Interfaces;
using E_Commers.Models;
using E_Commers.Services.AdminOpreationServices;
using E_Commers.Services.Cache;
using E_Commers.Services.EmailServices;
using E_Commers.UOW;
using Hangfire;
using Microsoft.Extensions.Logging;

namespace E_Commers.Services.Collection
{
    public class CollectionServices :  ICollectionServices
    {
        private readonly ILogger<CollectionServices> _logger;
        private readonly IMapper _mapper;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICollectionRepository _collectionRepository;
        private readonly IAdminOpreationServices _adminOperationServices;
        private readonly ICacheManager _cacheManager;
        private const string CACHE_TAG_COLLECTION = "collection";

        public CollectionServices(
            ILogger<CollectionServices> logger,
            IMapper mapper,
            IUnitOfWork unitOfWork,
            ICollectionRepository collectionRepository,
            IAdminOpreationServices adminOperationServices,
            ICacheManager cacheManager)
        {
            _logger = logger;
            _mapper = mapper;
            _unitOfWork = unitOfWork;
            _collectionRepository = collectionRepository;
            _adminOperationServices = adminOperationServices;
            _cacheManager = cacheManager;
        }

        private void NotifyAdminOfError(string message, string? stackTrace = null)
        {
            BackgroundJob.Enqueue<IErrorNotificationService>(_ => _.SendErrorNotificationAsync(message, stackTrace));
        }

        public async Task<Result<CollectionDto>> GetCollectionByIdAsync(int collectionId)
        {
            _logger.LogInformation($"Getting collection by ID: {collectionId}");

            var cacheKey = $"{CACHE_TAG_COLLECTION}_id_{collectionId}";
            var cached = await _cacheManager.GetAsync<CollectionDto>(cacheKey);
            if (cached != null)
            {
                _logger.LogInformation($"Cache hit for collection {collectionId}");
                return Result<CollectionDto>.Ok(cached, "Collection retrieved from cache", 200);
            }

            try
            {
                var collection = await _collectionRepository.GetCollectionByIdAsync(collectionId);
                if (collection == null)
                {
                    return Result<CollectionDto>.Fail("Collection not found", 404);
                }

                var collectionDto = _mapper.Map<CollectionDto>(collection);
                
                // Calculate collection statistics
                collectionDto.TotalProducts = await _collectionRepository.GetProductCountInCollectionAsync(collectionId);
                collectionDto.MinPrice = await _collectionRepository.GetMinPriceInCollectionAsync(collectionId);
                collectionDto.MaxPrice = await _collectionRepository.GetMaxPriceInCollectionAsync(collectionId);
                collectionDto.AveragePrice = await _collectionRepository.GetAveragePriceInCollectionAsync(collectionId);

                await _cacheManager.SetAsync(cacheKey, collectionDto, tags: new[] { CACHE_TAG_COLLECTION });

                return Result<CollectionDto>.Ok(collectionDto, "Collection retrieved successfully", 200);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting collection {collectionId}: {ex.Message}");
                NotifyAdminOfError($"Error getting collection {collectionId}: {ex.Message}", ex.StackTrace);
                return Result<CollectionDto>.Fail("An error occurred while retrieving collection", 500);
            }
        }

        public async Task<Result<CollectionDto>> GetCollectionByNameAsync(string name)
        {
            _logger.LogInformation($"Getting collection by name: {name}");

            try
            {
                var collection = await _collectionRepository.GetCollectionByNameAsync(name);
                if (collection == null)
                {
                    return Result<CollectionDto>.Fail("Collection not found", 404);
                }

                var collectionDto = _mapper.Map<CollectionDto>(collection);
                return Result<CollectionDto>.Ok(collectionDto, "Collection retrieved successfully", 200);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting collection by name {name}: {ex.Message}");
                NotifyAdminOfError($"Error getting collection by name {name}: {ex.Message}", ex.StackTrace);
                return Result<CollectionDto>.Fail("An error occurred while retrieving collection", 500);
            }
        }

        public async Task<Result<List<CollectionDto>>> GetActiveCollectionsAsync()
        {
            _logger.LogInformation("Getting active collections");

            try
            {
                var collections = await _collectionRepository.GetActiveCollectionsAsync();
                var collectionDtos = _mapper.Map<List<CollectionDto>>(collections);
                return Result<List<CollectionDto>>.Ok(collectionDtos, "Active collections retrieved successfully", 200);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting active collections: {ex.Message}");
                NotifyAdminOfError($"Error getting active collections: {ex.Message}", ex.StackTrace);
                return Result<List<CollectionDto>>.Fail("An error occurred while retrieving active collections", 500);
            }
        }

        public async Task<Result<List<CollectionDto>>> GetCollectionsByDisplayOrderAsync()
        {
            _logger.LogInformation("Getting collections by display order");

            try
            {
                var collections = await _collectionRepository.GetCollectionsByDisplayOrderAsync();
                var collectionDtos = _mapper.Map<List<CollectionDto>>(collections);
                return Result<List<CollectionDto>>.Ok(collectionDtos, "Collections retrieved successfully", 200);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting collections by display order: {ex.Message}");
                NotifyAdminOfError($"Error getting collections by display order: {ex.Message}", ex.StackTrace);
                return Result<List<CollectionDto>>.Fail("An error occurred while retrieving collections", 500);
            }
        }

        public async Task<Result<List<CollectionDto>>> GetCollectionsWithPaginationAsync(int page, int pageSize, bool? isActive = null)
        {
            _logger.LogInformation($"Getting collections with pagination: page {page}, size {pageSize}, active: {isActive}");

            try
            {
                var collections = await _collectionRepository.GetCollectionsWithPaginationAsync(page, pageSize, isActive);
                var collectionDtos = _mapper.Map<List<CollectionDto>>(collections);
                return Result<List<CollectionDto>>.Ok(collectionDtos, "Collections retrieved successfully", 200);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting collections with pagination: {ex.Message}");
                NotifyAdminOfError($"Error getting collections with pagination: {ex.Message}", ex.StackTrace);
                return Result<List<CollectionDto>>.Fail("An error occurred while retrieving collections", 500);
            }
        }

        public async Task<Result<int?>> GetTotalCollectionCountAsync(bool? isActive = null)
        {
            try
            {
                var count = await _collectionRepository.GetTotalCollectionCountAsync(isActive);
                return Result<int?>.Ok(count, "Collection count retrieved", 200);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting collection count: {ex.Message}");
                return Result<int?>.Fail("An error occurred while getting collection count", 500);
            }
        }

        public async Task<Result<CollectionDto>> CreateCollectionAsync(CreateCollectionDto collectionDto, string userRole)
        {
            _logger.LogInformation($"Creating collection: {collectionDto.Name}");

            if (userRole != "Admin")
            {
                return Result<CollectionDto>.Fail("Unauthorized access", 403);
            }

            using var transaction = await _unitOfWork.BeginTransactionAsync();
            try
            {
                // Check if collection name already exists
                var existingCollection = await _collectionRepository.GetCollectionByNameAsync(collectionDto.Name);
                if (existingCollection != null)
                {
                    await transaction.RollbackAsync();
                    return Result<CollectionDto>.Fail("Collection with this name already exists", 400);
                }

                // Validate product IDs
                if (collectionDto.ProductIds.Any())
                {
                    foreach (var productId in collectionDto.ProductIds)
                    {
                        var product = await _unitOfWork.Repository<E_Commers.Models.Product>().GetByIdAsync(productId);
                        if (product == null)
                        {
                            await transaction.RollbackAsync();
                            return Result<CollectionDto>.Fail($"Product with ID {productId} not found", 400);
                        }
                    }
                }

                var collection = _mapper.Map<E_Commers.Models.Collection>(collectionDto);
                var createdCollection = await _collectionRepository.CreateAsync(collection);
                if (createdCollection == null)
                {
                    await transaction.RollbackAsync();
                    return Result<CollectionDto>.Fail("Failed to create collection", 500);
                }

                // Add products to collection if specified
                if (collectionDto.ProductIds.Any())
                {
                    var addProductsResult = await _collectionRepository.AddProductsToCollectionAsync(createdCollection.Id, collectionDto.ProductIds);
                    if (!addProductsResult)
                    {
                        await transaction.RollbackAsync();
                        return Result<CollectionDto>.Fail("Failed to add products to collection", 500);
                    }
                }

                // Log admin operation
                var adminLog = await _adminOperationServices.AddAdminOpreationAsync(
                    $"Created collection '{collectionDto.Name}'",
                    Enums.Opreations.AddOpreation,
                    "Admin",
                    createdCollection.Id
                );

                if (!adminLog.Success)
                {
                    _logger.LogWarning($"Failed to log admin operation: {adminLog.Message}");
                }

                await _unitOfWork.CommitAsync();
                await transaction.CommitAsync();

                // Clear cache
                await _cacheManager.RemoveByTagAsync(CACHE_TAG_COLLECTION);

                // Get complete collection with all details
                var completeCollection = await _collectionRepository.GetCollectionByIdAsync(createdCollection.Id);
                var collectionDtoResult = _mapper.Map<CollectionDto>(completeCollection);

                return Result<CollectionDto>.Ok(collectionDtoResult, "Collection created successfully", 201);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError($"Error creating collection: {ex.Message}");
                NotifyAdminOfError($"Error creating collection: {ex.Message}", ex.StackTrace);
                return Result<CollectionDto>.Fail("An error occurred while creating collection", 500);
            }
        }

        public async Task<Result<CollectionDto>> UpdateCollectionAsync(int collectionId, UpdateCollectionDto collectionDto, string userRole)
        {
            _logger.LogInformation($"Updating collection {collectionId}");

            if (userRole != "Admin")
            {
                return Result<CollectionDto>.Fail("Unauthorized access", 403);
            }

            using var transaction = await _unitOfWork.BeginTransactionAsync();
            try
            {
                var collection = await _collectionRepository.GetCollectionByIdAsync(collectionId);
                if (collection == null)
                {
                    await transaction.RollbackAsync();
                    return Result<CollectionDto>.Fail("Collection not found", 404);
                }

                // Check if new name conflicts with existing collection
                if (collection.Name != collectionDto.Name)
                {
                    var existingCollection = await _collectionRepository.GetCollectionByNameAsync(collectionDto.Name);
                    if (existingCollection != null && existingCollection.Id != collectionId)
                    {
                        await transaction.RollbackAsync();
                        return Result<CollectionDto>.Fail("Collection with this name already exists", 400);
                    }
                }

                // Validate product IDs
                if (collectionDto.ProductIds.Any())
                {
                    foreach (var productId in collectionDto.ProductIds)
                    {
                        var product = await _unitOfWork.Repository<E_Commers.Models.Product>().GetByIdAsync(productId);
                        if (product == null)
                        {
                            await transaction.RollbackAsync();
                            return Result<CollectionDto>.Fail($"Product with ID {productId} not found", 400);
                        }
                    }
                }

                // Update collection properties
                collection.Name = collectionDto.Name.Trim();
                collection.Description = collectionDto.Description?.Trim();
                collection.DisplayOrder = collectionDto.DisplayOrder;
                collection.IsActive = collectionDto.IsActive;
                collection.ModifiedAt = DateTime.UtcNow;

                var updatedCollection =  _collectionRepository.Update(collection);
                if (!updatedCollection)
                {
                    await transaction.RollbackAsync();
                    return Result<CollectionDto>.Fail("Failed to update collection", 500);
                }

                // Update products in collection
                var currentProductIds = collection.ProductCollections.Select(pc => pc.ProductId).ToList();
                var newProductIds = collectionDto.ProductIds;

                var productsToRemove = currentProductIds.Except(newProductIds).ToList();
                var productsToAdd = newProductIds.Except(currentProductIds).ToList();

                if (productsToRemove.Any())
                {
                    var removeResult = await _collectionRepository.RemoveProductsFromCollectionAsync(collectionId, productsToRemove);
                    if (!removeResult)
                    {
                        await transaction.RollbackAsync();
                        return Result<CollectionDto>.Fail("Failed to remove products from collection", 500);
                    }
                }

                if (productsToAdd.Any())
                {
                    var addResult = await _collectionRepository.AddProductsToCollectionAsync(collectionId, productsToAdd);
                    if (!addResult)
                    {
                        await transaction.RollbackAsync();
                        return Result<CollectionDto>.Fail("Failed to add products to collection", 500);
                    }
                }

                // Log admin operation
                var adminLog = await _adminOperationServices.AddAdminOpreationAsync(
                    $"Updated collection '{collectionDto.Name}'",
                    Enums.Opreations.UpdateOpreation,
                    "Admin",
                    collectionId
                );

                if (!adminLog.Success)
                {
                    _logger.LogWarning($"Failed to log admin operation: {adminLog.Message}");
                }

                await _unitOfWork.CommitAsync();
                await transaction.CommitAsync();

                // Clear cache
                await _cacheManager.RemoveByTagAsync(CACHE_TAG_COLLECTION);

                // Get updated collection
                var updatedCollectionWithDetails = await _collectionRepository.GetCollectionByIdAsync(collectionId);
                var collectionDtoResult = _mapper.Map<CollectionDto>(updatedCollectionWithDetails);

                return Result<CollectionDto>.Ok(collectionDtoResult, "Collection updated successfully", 200);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError($"Error updating collection {collectionId}: {ex.Message}");
                NotifyAdminOfError($"Error updating collection {collectionId}: {ex.Message}", ex.StackTrace);
                return Result<CollectionDto>.Fail("An error occurred while updating collection", 500);
            }
        }

        public async Task<Result<string>> DeleteCollectionAsync(int collectionId, string userRole)
        {
            _logger.LogInformation($"Deleting collection {collectionId}");

            if (userRole != "Admin")
            {
                return Result<string>.Fail("Unauthorized access", 403);
            }

            using var transaction = await _unitOfWork.BeginTransactionAsync();
            try
            {
                var collection = await _collectionRepository.GetCollectionByIdAsync(collectionId);
                if (collection == null)
                {
                    await transaction.RollbackAsync();
                    return Result<string>.Fail("Collection not found", 404);
                }

                var deleteResult = await _collectionRepository.SoftDeleteAsync(collectionId);
                if (!deleteResult)
                {
                    await transaction.RollbackAsync();
                    return Result<string>.Fail("Failed to delete collection", 500);
                }

                // Log admin operation
                var adminLog = await _adminOperationServices.AddAdminOpreationAsync(
                    $"Deleted collection '{collection.Name}'",
                    Enums.Opreations.DeleteOpreation,
                    "Admin",
                    collectionId
                );

                if (!adminLog.Success)
                {
                    _logger.LogWarning($"Failed to log admin operation: {adminLog.Message}");
                }

                await _unitOfWork.CommitAsync();
                await transaction.CommitAsync();

                // Clear cache
                await _cacheManager.RemoveByTagAsync(CACHE_TAG_COLLECTION);

                return Result<string>.Ok(null, "Collection deleted successfully", 200);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError($"Error deleting collection {collectionId}: {ex.Message}");
                NotifyAdminOfError($"Error deleting collection {collectionId}: {ex.Message}", ex.StackTrace);
                return Result<string>.Fail("An error occurred while deleting collection", 500);
            }
        }

        public async Task<Result<string>> AddProductsToCollectionAsync(int collectionId, AddProductsToCollectionDto productsDto, string userRole)
        {
            _logger.LogInformation($"Adding {productsDto.ProductIds.Count} products to collection {collectionId}");

            if (userRole != "Admin")
            {
                return Result<string>.Fail("Unauthorized access", 403);
            }

            using var transaction = await _unitOfWork.BeginTransactionAsync();
            try
            {
                var collection = await _collectionRepository.GetCollectionByIdAsync(collectionId);
                if (collection == null)
                {
                    await transaction.RollbackAsync();
                    return Result<string>.Fail("Collection not found", 404);
                }

                // Validate product IDs
                foreach (var productId in productsDto.ProductIds)
                {
                    var product = await _unitOfWork.Repository<E_Commers.Models.Product>().GetByIdAsync(productId);
                    if (product == null)
                    {
                        await transaction.RollbackAsync();
                        return Result<string>.Fail($"Product with ID {productId} not found", 400);
                    }
                }

                var addResult = await _collectionRepository.AddProductsToCollectionAsync(collectionId, productsDto.ProductIds);
                if (!addResult)
                {
                    await transaction.RollbackAsync();
                    return Result<string>.Fail("Failed to add products to collection", 500);
                }

                // Log admin operation
                var adminLog = await _adminOperationServices.AddAdminOpreationAsync(
                    $"Added {productsDto.ProductIds.Count} products to collection '{collection.Name}'",
                    Enums.Opreations.UpdateOpreation,
                    "Admin",
                    collectionId
                );

                if (!adminLog.Success)
                {
                    _logger.LogWarning($"Failed to log admin operation: {adminLog.Message}");
                }

                await _unitOfWork.CommitAsync();
                await transaction.CommitAsync();

                // Clear cache
                await _cacheManager.RemoveByTagAsync(CACHE_TAG_COLLECTION);

                return Result<string>.Ok(null, "Products added to collection successfully", 200);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError($"Error adding products to collection {collectionId}: {ex.Message}");
                NotifyAdminOfError($"Error adding products to collection {collectionId}: {ex.Message}", ex.StackTrace);
                return Result<string>.Fail("An error occurred while adding products to collection", 500);
            }
        }

        public async Task<Result<string>> RemoveProductsFromCollectionAsync(int collectionId, RemoveProductsFromCollectionDto productsDto, string userRole)
        {
            _logger.LogInformation($"Removing {productsDto.ProductIds.Count} products from collection {collectionId}");

            if (userRole != "Admin")
            {
                return Result<string>.Fail("Unauthorized access", 403);
            }

            using var transaction = await _unitOfWork.BeginTransactionAsync();
            try
            {
                var collection = await _collectionRepository.GetCollectionByIdAsync(collectionId);
                if (collection == null)
                {
                    await transaction.RollbackAsync();
                    return Result<string>.Fail("Collection not found", 404);
                }

                var removeResult = await _collectionRepository.RemoveProductsFromCollectionAsync(collectionId, productsDto.ProductIds);
                if (!removeResult)
                {
                    await transaction.RollbackAsync();
                    return Result<string>.Fail("Failed to remove products from collection", 500);
                }

                // Log admin operation
                var adminLog = await _adminOperationServices.AddAdminOpreationAsync(
                    $"Removed {productsDto.ProductIds.Count} products from collection '{collection.Name}'",
                    Enums.Opreations.UpdateOpreation,
                    "Admin",
                    collectionId
                );

                if (!adminLog.Success)
                {
                    _logger.LogWarning($"Failed to log admin operation: {adminLog.Message}");
                }

                await _unitOfWork.CommitAsync();
                await transaction.CommitAsync();

                // Clear cache
                await _cacheManager.RemoveByTagAsync(CACHE_TAG_COLLECTION);

                return Result<string>.Ok(null, "Products removed from collection successfully", 200);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError($"Error removing products from collection {collectionId}: {ex.Message}");
                NotifyAdminOfError($"Error removing products from collection {collectionId}: {ex.Message}", ex.StackTrace);
                return Result<string>.Fail("An error occurred while removing products from collection", 500);
            }
        }

        public async Task<Result<List<CollectionDto>>> GetCollectionsByProductAsync(int productId)
        {
            _logger.LogInformation($"Getting collections for product {productId}");

            try
            {
                var collections = await _collectionRepository.GetCollectionsByProductAsync(productId);
                var collectionDtos = _mapper.Map<List<CollectionDto>>(collections);
                return Result<List<CollectionDto>>.Ok(collectionDtos, "Collections retrieved successfully", 200);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting collections for product {productId}: {ex.Message}");
                NotifyAdminOfError($"Error getting collections for product {productId}: {ex.Message}", ex.StackTrace);
                return Result<List<CollectionDto>>.Fail("An error occurred while retrieving collections", 500);
            }
        }

        public async Task<Result<string>> UpdateCollectionStatusAsync(int collectionId, bool isActive, string userRole)
        {
            _logger.LogInformation($"Updating collection {collectionId} status to {isActive}");

            if (userRole != "Admin")
            {
                return Result<string>.Fail("Unauthorized access", 403);
            }

            try
            {
                var updateResult = await _collectionRepository.UpdateCollectionStatusAsync(collectionId, isActive);
                if (!updateResult)
                {
                    return Result<string>.Fail("Failed to update collection status", 500);
                }

                await _cacheManager.RemoveByTagAsync(CACHE_TAG_COLLECTION);
                return Result<string>.Ok(null, "Collection status updated successfully", 200);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error updating collection status for collection {collectionId}: {ex.Message}");
                return Result<string>.Fail("An error occurred while updating collection status", 500);
            }
        }

        public async Task<Result<string>> UpdateCollectionDisplayOrderAsync(int collectionId, int displayOrder, string userRole)
        {
            _logger.LogInformation($"Updating collection {collectionId} display order to {displayOrder}");

            if (userRole != "Admin")
            {
                return Result<string>.Fail("Unauthorized access", 403);
            }

            try
            {
                var updateResult = await _collectionRepository.UpdateCollectionDisplayOrderAsync(collectionId, displayOrder);
                if (!updateResult)
                {
                    return Result<string>.Fail("Failed to update collection display order", 500);
                }

                await _cacheManager.RemoveByTagAsync(CACHE_TAG_COLLECTION);
                return Result<string>.Ok(null, "Collection display order updated successfully", 200);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error updating collection display order for collection {collectionId}: {ex.Message}");
                return Result<string>.Fail("An error occurred while updating collection display order", 500);
            }
        }

        public async Task<Result<List<CollectionDto>>> SearchCollectionsAsync(string searchTerm)
        {
            _logger.LogInformation($"Searching collections with term: {searchTerm}");

            try
            {
                var collections = await _collectionRepository.SearchCollectionsAsync(searchTerm);
                var collectionDtos = _mapper.Map<List<CollectionDto>>(collections);
                return Result<List<CollectionDto>>.Ok(collectionDtos, "Collections search completed successfully", 200);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error searching collections: {ex.Message}");
                NotifyAdminOfError($"Error searching collections: {ex.Message}", ex.StackTrace);
                return Result<List<CollectionDto>>.Fail("An error occurred while searching collections", 500);
            }
        }

        public async Task<Result<CollectionSummaryDto>> GetCollectionSummaryAsync(int collectionId)
        {
            _logger.LogInformation($"Getting collection summary for {collectionId}");

            try
            {
                var collection = await _collectionRepository.GetCollectionByIdAsync(collectionId);
                if (collection == null)
                {
                    return Result<CollectionSummaryDto>.Fail("Collection not found", 404);
                }

                var summaryDto = _mapper.Map<CollectionSummaryDto>(collection);
                summaryDto.TotalProducts = await _collectionRepository.GetProductCountInCollectionAsync(collectionId);
                summaryDto.MinPrice = await _collectionRepository.GetMinPriceInCollectionAsync(collectionId);
                summaryDto.MaxPrice = await _collectionRepository.GetMaxPriceInCollectionAsync(collectionId);

                return Result<CollectionSummaryDto>.Ok(summaryDto, "Collection summary retrieved successfully", 200);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting collection summary for {collectionId}: {ex.Message}");
                return Result<CollectionSummaryDto>.Fail("An error occurred while getting collection summary", 500);
            }
        }

        public async Task<Result<List<CollectionSummaryDto>>> GetCollectionSummariesAsync(int page, int pageSize, bool? isActive = null)
        {
            _logger.LogInformation($"Getting collection summaries: page {page}, size {pageSize}, active: {isActive}");

            try
            {
                var collections = await _collectionRepository.GetCollectionsWithPaginationAsync(page, pageSize, isActive);
                var summaryDtos = new List<CollectionSummaryDto>();

                foreach (var collection in collections)
                {
                    var summaryDto = _mapper.Map<CollectionSummaryDto>(collection);
                    summaryDto.TotalProducts = await _collectionRepository.GetProductCountInCollectionAsync(collection.Id);
                    summaryDto.MinPrice = await _collectionRepository.GetMinPriceInCollectionAsync(collection.Id);
                    summaryDto.MaxPrice = await _collectionRepository.GetMaxPriceInCollectionAsync(collection.Id);
                    summaryDtos.Add(summaryDto);
                }

                return Result<List<CollectionSummaryDto>>.Ok(summaryDtos, "Collection summaries retrieved successfully", 200);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting collection summaries: {ex.Message}");
                return Result<List<CollectionSummaryDto>>.Fail("An error occurred while getting collection summaries", 500);
            }
        }
    }
} 