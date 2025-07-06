using E_Commers.Context;
using E_Commers.Interfaces;
using E_Commers.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace E_Commers.Repository
{
    public class CollectionRepository : MainRepository<Collection>, ICollectionRepository
    {
        private readonly AppDbContext _context;
        private readonly ILogger<CollectionRepository> _logger;

        public CollectionRepository(AppDbContext context, ILogger<CollectionRepository> logger) 
            : base(context, logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<Collection?> GetCollectionByIdAsync(int collectionId)
        {
            _logger.LogInformation($"Getting collection by ID: {collectionId}");
            
            return await _context.Collections
                .Where(c => c.Id == collectionId && c.DeletedAt == null)
                .Include(c => c.ProductCollections.Where(pc => pc.Product.DeletedAt == null))
                .ThenInclude(pc => pc.Product)
                .ThenInclude(p => p.ProductVariants.Where(v => v.DeletedAt == null))
                .Include(c => c.ProductCollections.Where(pc => pc.Product.DeletedAt == null))
                .ThenInclude(pc => pc.Product)
                .ThenInclude(p => p.Discount)
                .Include(c => c.ProductCollections.Where(pc => pc.Product.DeletedAt == null))
                .ThenInclude(pc => pc.Product)
                .ThenInclude(p => p.Images.Where(img => img.DeletedAt == null))
                .Include(c => c.Images.Where(img => img.DeletedAt == null))
                .FirstOrDefaultAsync();
        }

        public async Task<Collection?> GetCollectionByNameAsync(string name)
        {
            _logger.LogInformation($"Getting collection by name: {name}");
            
            return await _context.Collections
                .Where(c => c.Name.ToLower() == name.ToLower() && c.DeletedAt == null)
                .Include(c => c.ProductCollections.Where(pc => pc.Product.DeletedAt == null))
                .ThenInclude(pc => pc.Product)
                .Include(c => c.Images.Where(img => img.DeletedAt == null))
                .FirstOrDefaultAsync();
        }

        public async Task<List<Collection>> GetActiveCollectionsAsync()
        {
            _logger.LogInformation("Getting active collections");
            
            return await _context.Collections
                .Where(c => c.IsActive && c.DeletedAt == null)
                .Include(c => c.Images.Where(img => img.DeletedAt == null))
                .OrderBy(c => c.DisplayOrder)
                .ThenBy(c => c.Name)
                .ToListAsync();
        }

        public async Task<List<Collection>> GetCollectionsByDisplayOrderAsync()
        {
            _logger.LogInformation("Getting collections by display order");
            
            return await _context.Collections
                .Where(c => c.DeletedAt == null)
                .Include(c => c.Images.Where(img => img.DeletedAt == null))
                .OrderBy(c => c.DisplayOrder)
                .ThenBy(c => c.Name)
                .ToListAsync();
        }

        public async Task<List<Collection>> GetCollectionsWithPaginationAsync(int page, int pageSize, bool? isActive = null)
        {
            _logger.LogInformation($"Getting collections with pagination: page {page}, size {pageSize}, active: {isActive}");
            
            var query = _context.Collections
                .Where(c => c.DeletedAt == null)
                .Include(c => c.Images.Where(img => img.DeletedAt == null))
                .Include(c => c.ProductCollections.Where(pc => pc.Product.DeletedAt == null))
                .ThenInclude(pc => pc.Product);


            return await query
                .OrderBy(c => c.DisplayOrder)
                .ThenBy(c => c.Name)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<int> GetTotalCollectionCountAsync(bool? isActive = null)
        {
            var query = _context.Collections.Where(c => c.DeletedAt == null);

            if (isActive.HasValue)
            {
                query = query.Where(c => c.IsActive == isActive.Value);
            }

            return await query.CountAsync();
        }

        public async Task<bool> AddProductsToCollectionAsync(int collectionId, List<int> productIds)
        {
            _logger.LogInformation($"Adding {productIds.Count} products to collection {collectionId}");
            
            try
            {
                var existingProductCollections = await _context.ProductCollections
                    .Where(pc => pc.CollectionId == collectionId && productIds.Contains(pc.ProductId))
                    .ToListAsync();

                var existingProductIds = existingProductCollections.Select(pc => pc.ProductId).ToList();
                var newProductIds = productIds.Except(existingProductIds).ToList();

                var newProductCollections = newProductIds.Select(productId => new ProductCollection
                {
                    ProductId = productId,
                    CollectionId = collectionId
                }).ToList();

                await _context.ProductCollections.AddRangeAsync(newProductCollections);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error adding products to collection {collectionId}: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> RemoveProductsFromCollectionAsync(int collectionId, List<int> productIds)
        {
            _logger.LogInformation($"Removing {productIds.Count} products from collection {collectionId}");
            
            try
            {
                var productCollections = await _context.ProductCollections
                    .Where(pc => pc.CollectionId == collectionId && productIds.Contains(pc.ProductId))
                    .ToListAsync();

                _context.ProductCollections.RemoveRange(productCollections);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error removing products from collection {collectionId}: {ex.Message}");
                return false;
            }
        }

        public async Task<List<Collection>> GetCollectionsByProductAsync(int productId)
        {
            _logger.LogInformation($"Getting collections for product {productId}");
            
            return await _context.Collections
                .Where(c => c.DeletedAt == null && c.ProductCollections.Any(pc => pc.ProductId == productId))
                .Include(c => c.Images.Where(img => img.DeletedAt == null))
                .OrderBy(c => c.DisplayOrder)
                .ThenBy(c => c.Name)
                .ToListAsync();
        }

        public async Task<bool> UpdateCollectionStatusAsync(int collectionId, bool isActive)
        {
            _logger.LogInformation($"Updating collection {collectionId} status to {isActive}");
            
            try
            {
                var collection = await _context.Collections
                    .Where(c => c.Id == collectionId && c.DeletedAt == null)
                    .FirstOrDefaultAsync();

                if (collection == null)
                {
                    _logger.LogWarning($"Collection {collectionId} not found");
                    return false;
                }

                collection.IsActive = isActive;
                collection.ModifiedAt = DateTime.UtcNow;

                _context.Collections.Update(collection);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error updating collection status: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> UpdateCollectionDisplayOrderAsync(int collectionId, int displayOrder)
        {
            _logger.LogInformation($"Updating collection {collectionId} display order to {displayOrder}");
            
            try
            {
                var collection = await _context.Collections
                    .Where(c => c.Id == collectionId && c.DeletedAt == null)
                    .FirstOrDefaultAsync();

                if (collection == null)
                {
                    _logger.LogWarning($"Collection {collectionId} not found");
                    return false;
                }

                collection.DisplayOrder = displayOrder;
                collection.ModifiedAt = DateTime.UtcNow;

                _context.Collections.Update(collection);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error updating collection display order: {ex.Message}");
                return false;
            }
        }

        public async Task<List<Collection>> SearchCollectionsAsync(string searchTerm)
        {
            _logger.LogInformation($"Searching collections with term: {searchTerm}");
            
            return await _context.Collections
                .Where(c => c.DeletedAt == null && 
                           (c.Name.ToLower().Contains(searchTerm.ToLower()) || 
                            (c.Description != null && c.Description.ToLower().Contains(searchTerm.ToLower()))))
                .Include(c => c.Images.Where(img => img.DeletedAt == null))
                .OrderBy(c => c.DisplayOrder)
                .ThenBy(c => c.Name)
                .ToListAsync();
        }

        public async Task<int> GetProductCountInCollectionAsync(int collectionId)
        {
            return await _context.ProductCollections
                .Where(pc => pc.CollectionId == collectionId && pc.Product.DeletedAt == null)
                .CountAsync();
        }

        public async Task<decimal> GetMinPriceInCollectionAsync(int collectionId)
        {
            var minPrice = await _context.ProductCollections
                .Where(pc => pc.CollectionId == collectionId && pc.Product.DeletedAt == null)
                .Select(pc => pc.Product.ProductVariants.Where(v => v.DeletedAt == null).Min(v => v.Price))
                .MinAsync();

            return minPrice;
        }

        public async Task<decimal> GetMaxPriceInCollectionAsync(int collectionId)
        {
            var maxPrice = await _context.ProductCollections
                .Where(pc => pc.CollectionId == collectionId && pc.Product.DeletedAt == null)
                .Select(pc => pc.Product.ProductVariants.Where(v => v.DeletedAt == null).Max(v => v.Price))
                .MaxAsync();

            return maxPrice;
        }

        public async Task<decimal> GetAveragePriceInCollectionAsync(int collectionId)
        {
            var averagePrice = await _context.ProductCollections
                .Where(pc => pc.CollectionId == collectionId && pc.Product.DeletedAt == null)
                .SelectMany(pc => pc.Product.ProductVariants.Where(v => v.DeletedAt == null))
                .AverageAsync(v => v.Price);

            return averagePrice;
        }
    }
} 