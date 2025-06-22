using AutoMapper;
using E_Commers.DtoModels;
using E_Commers.DtoModels.InventoryDtos;
using E_Commers.DtoModels.ProductDtos;
using E_Commers.DtoModels.Responses;
using E_Commers.Enums;
using E_Commers.ErrorHnadling;
using E_Commers.Interfaces;
using E_Commers.Models;
using E_Commers.Services.AdminOpreationServices;
using E_Commers.Services.Cache;
using E_Commers.UOW;
using Microsoft.EntityFrameworkCore;

namespace E_Commers.Services.ProductInventoryServices
{
    public class ProductInventoryService : IProductInventoryService
    {
        private readonly ILogger<ProductInventoryService> _logger;
        private readonly IMapper _mapper;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IAdminOpreationServices _adminOpreationServices;
        private readonly ICacheManager _cacheManager;
        private const string CACHE_TAG_PRODUCT = "product";
        private const string CACHE_TAG_INVENTORY = "inventory";

        public ProductInventoryService(
            ILogger<ProductInventoryService> logger,
            IMapper mapper,
            IUnitOfWork unitOfWork,
            IAdminOpreationServices adminOpreationServices,
            ICacheManager cacheManager)
        {
            _logger = logger;
            _mapper = mapper;
            _unitOfWork = unitOfWork;
            _adminOpreationServices = adminOpreationServices;
            _cacheManager = cacheManager;
        }

        public async Task<ApiResponse<InventoryDto>> CreateInventoryAsync(CreateInvetoryDto dto, string userId)
        {
            _logger.LogInformation($"Executing {nameof(CreateInventoryAsync)}");

            // Validate product exists
            var product = await _unitOfWork.Product.GetByIdAsync(dto.ProductId);
            if (!product.Success || product.Data == null)
            {
                return ApiResponse<InventoryDto>.CreateErrorResponse(
                    new ErrorResponse("Product", "Product not found"),
                    404
                );
            }

            // Validate warehouse exists
            var warehouse = await _unitOfWork.WareHouse.GetByIdAsync(dto.WareHouseId);
            if (!warehouse.Success || warehouse.Data == null)
            {
                return ApiResponse<InventoryDto>.CreateErrorResponse(
                    new ErrorResponse("Warehouse", "Warehouse not found"),
                    404
                );
            }

            // Check if inventory entry already exists
            var existingInventory = await _unitOfWork.Repository<ProductInventory>()
                .GetByQuery(i => i.ProductId == dto.ProductId && i.WarehouseId == dto.WareHouseId);
            if (existingInventory.Success)
            {
                return ApiResponse<InventoryDto>.CreateErrorResponse(
                    new ErrorResponse("Inventory", "Product already exists in this warehouse"),
                    409
                );
            }

            using var transaction = await _unitOfWork.BeginTransactionAsync();
            try
            {
                // Create inventory entry
                var inventory = new ProductInventory
                {
                    ProductId = dto.ProductId,
                    WarehouseId = dto.WareHouseId,
                    Quantity = dto.Quantity,
                    CreatedAt = DateTime.UtcNow,
                };

                var result = await _unitOfWork.Repository<ProductInventory>().CreateAsync(inventory);
                if (!result.Success)
                {
                    await transaction.RollbackAsync();
                    return ApiResponse<InventoryDto>.CreateErrorResponse(
                        new ErrorResponse("Server", "Failed to create inventory entry"),
                        500
                    );
                }

                // Log admin operation
                var adminLog = await _adminOpreationServices.AddAdminOpreationAsync(
                    $"Created inventory for product {dto.ProductId} in warehouse {dto.WareHouseId} with quantity {dto.Quantity}",
                    Opreations.AddOpreation,
                    userId,
                    dto.ProductId
                );

                if (!adminLog.Success)
                {
                    await transaction.RollbackAsync();
                    return ApiResponse<InventoryDto>.CreateErrorResponse(
                        new ErrorResponse("Server", "Failed to log operation"),
                        500
                    );
                }

                await _unitOfWork.CommitAsync();
                await transaction.CommitAsync();
                await _cacheManager.RemoveByTagAsync(CACHE_TAG_INVENTORY);
                await _cacheManager.RemoveByTagAsync(CACHE_TAG_PRODUCT);

                var inventoryDto = _mapper.Map<InventoryDto>(inventory);
                return ApiResponse<InventoryDto>.CreateSuccessResponse("Inventory created successfully", inventoryDto, 201);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError($"Error in {nameof(CreateInventoryAsync)}: {ex.Message}");
                return ApiResponse<InventoryDto>.CreateErrorResponse(
                    new ErrorResponse("Server", "An error occurred while processing your request"),
                    500
                );
            }
        }

        public async Task<ApiResponse<InventoryDto>> UpdateInventoryQuantityAsync(UpdateInventoryQuantityDto dto, string userId)
        {
            _logger.LogInformation($"Executing {nameof(UpdateInventoryQuantityAsync)}");

            // Validate inventory exists
            var inventory = await _unitOfWork.Repository<ProductInventory>()
                .GetByQuery(i => i.ProductId == dto.ProductId && i.WarehouseId == dto.WarehouseId);
            
            if (!inventory.Success || inventory.Data == null)
            {
                return ApiResponse<InventoryDto>.CreateErrorResponse(
                    new ErrorResponse("Inventory", "Inventory not found"),
                    404
                );
            }

            // Validate product exists
            var product = await _unitOfWork.Product.GetByIdAsync(dto.ProductId);
            if (!product.Success || product.Data == null)
            {
                return ApiResponse<InventoryDto>.CreateErrorResponse(
                    new ErrorResponse("Product", "Product not found"),
                    404
                );
            }

            using var transaction = await _unitOfWork.BeginTransactionAsync();
            try
            {
                // Update inventory quantity
                inventory.Data.Quantity = dto.NewQuantity;
                inventory.Data.ModifiedAt = DateTime.UtcNow;

                var updateResult = await _unitOfWork.Repository<ProductInventory>().UpdateAsync(inventory.Data);
                if (!updateResult.Success)
                {
                    await transaction.RollbackAsync();
                    return ApiResponse<InventoryDto>.CreateErrorResponse(
                        new ErrorResponse("Server", "Failed to update inventory quantity"),
                        500
                    );
                }

                // Log admin operation
                var adminLog = await _adminOpreationServices.AddAdminOpreationAsync(
                    $"Updated inventory quantity for product {dto.ProductId} in warehouse {dto.WarehouseId} to {dto.NewQuantity}",
                    Opreations.UpdateOpreation,
                    userId,
                    dto.ProductId
                );

                if (!adminLog.Success)
                {
                    await transaction.RollbackAsync();
                    return ApiResponse<InventoryDto>.CreateErrorResponse(
                        new ErrorResponse("Server", "Failed to log operation"),
                        500
                    );
                }

                await _unitOfWork.CommitAsync();
                await transaction.CommitAsync();
                await _cacheManager.RemoveByTagAsync(CACHE_TAG_INVENTORY);
                await _cacheManager.RemoveByTagAsync(CACHE_TAG_PRODUCT);

                var inventoryDto = _mapper.Map<InventoryDto>(inventory.Data);
                return ApiResponse<InventoryDto>.CreateSuccessResponse("Inventory quantity updated successfully", inventoryDto);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError($"Error in {nameof(UpdateInventoryQuantityAsync)}: {ex.Message}");
                return ApiResponse<InventoryDto>.CreateErrorResponse(
                    new ErrorResponse("Server", "An error occurred while processing your request"),
                    500
                );
            }
        }

        public async Task<ApiResponse<InventoryDto>> TransferQuantityAsync(TransfereQuantityInvetoryDto dto, string userId)
        {
            _logger.LogInformation($"Executing {nameof(TransferQuantityAsync)}");

            // Validate source inventory
            var sourceInventory = await _unitOfWork.Repository<ProductInventory>()
                .GetByIdAsync(dto.FromInventoryId);
            if (!sourceInventory.Success || sourceInventory.Data == null)
            {
                return ApiResponse<InventoryDto>.CreateErrorResponse(
                    new ErrorResponse("Source Inventory", "Source inventory not found"),
                    404
                );
            }

            // Validate target inventory
            var targetInventory = await _unitOfWork.Repository<ProductInventory>()
                .GetByIdAsync(dto.ToInventoryId);
            if (!targetInventory.Success || targetInventory.Data == null)
            {
                return ApiResponse<InventoryDto>.CreateErrorResponse(
                    new ErrorResponse("Target Inventory", "Target inventory not found"),
                    404
                );
            }

            // Validate product matches
            if (sourceInventory.Data.ProductId != dto.ProductId || targetInventory.Data.ProductId != dto.ProductId)
            {
                return ApiResponse<InventoryDto>.CreateErrorResponse(
                    new ErrorResponse("Product", "Product mismatch between inventories"),
                    400
                );
            }

            // Validate quantity
            if (sourceInventory.Data.Quantity < dto.Quantity)
            {
                return ApiResponse<InventoryDto>.CreateErrorResponse(
                    new ErrorResponse("Quantity", "Insufficient quantity in source inventory"),
                    400
                );
            }

            using var transaction = await _unitOfWork.BeginTransactionAsync();
            try
            {
                // Update quantities
                sourceInventory.Data.Quantity -= dto.Quantity;
                targetInventory.Data.Quantity += dto.Quantity;

                var sourceUpdate = await _unitOfWork.Repository<ProductInventory>().UpdateAsync(sourceInventory.Data);
                var targetUpdate = await _unitOfWork.Repository<ProductInventory>().UpdateAsync(targetInventory.Data);

                if (!sourceUpdate.Success || !targetUpdate.Success)
                {
                    await transaction.RollbackAsync();
                    return ApiResponse<InventoryDto>.CreateErrorResponse(
                        new ErrorResponse("Server", "Failed to update inventory quantities"),
                        500
                    );
                }

                // Log admin operation
                var adminLog = await _adminOpreationServices.AddAdminOpreationAsync(
                    $"Transferred {dto.Quantity} units of product {dto.ProductId} from inventory {dto.FromInventoryId} to {dto.ToInventoryId}",
                    Opreations.UpdateOpreation,
                    userId,
                    dto.ProductId
                );

                if (!adminLog.Success)
                {
                    await transaction.RollbackAsync();
                    return ApiResponse<InventoryDto>.CreateErrorResponse(
                        new ErrorResponse("Server", "Failed to log operation"),
                        500
                    );
                }

                await _unitOfWork.CommitAsync();
                await transaction.CommitAsync();
                await _cacheManager.RemoveByTagAsync(CACHE_TAG_INVENTORY);

                var inventoryDto = _mapper.Map<InventoryDto>(targetInventory.Data);
                return ApiResponse<InventoryDto>.CreateSuccessResponse("Transfer completed successfully", inventoryDto);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError($"Error in {nameof(TransferQuantityAsync)}: {ex.Message}");
                return ApiResponse<InventoryDto>.CreateErrorResponse(
                    new ErrorResponse("Server", "An error occurred while processing your request"),
                    500
                );
            }
        }

        public async Task<ApiResponse<List<InventoryDto>>> GetWarehouseInventoryAsync(int warehouseId)
        {
            _logger.LogInformation($"Executing {nameof(GetWarehouseInventoryAsync)}");

            var cacheKey = $"{CACHE_TAG_INVENTORY}warehouse:{warehouseId}";
            var cachedInventory = await _cacheManager.GetAsync<List<InventoryDto>>(cacheKey);
            if (cachedInventory != null)
            {
                return ApiResponse<List<InventoryDto>>.CreateSuccessResponse("Inventory retrieved from cache", cachedInventory);
            }

            var warehouse = await _unitOfWork.WareHouse.GetByIdAsync(warehouseId);
            if (!warehouse.Success || warehouse.Data == null)
            {
                return ApiResponse<List<InventoryDto>>.CreateErrorResponse(
                    new ErrorResponse("Warehouse", "Warehouse not found"),
                    404
                );
            }

            var inventory = warehouse.Data.ProductInventories
                .Where(i => i.DeletedAt == null)
                .Select(i => _mapper.Map<InventoryDto>(i))
                .ToList();

            await _cacheManager.SetAsync(cacheKey, inventory, tags: new[] { CACHE_TAG_INVENTORY });

            return ApiResponse<List<InventoryDto>>.CreateSuccessResponse("Inventory retrieved successfully", inventory);
        }

        public async Task<ApiResponse<string>> DeleteInventoryAsync(int inventoryId, string userId)
        {
            _logger.LogInformation($"Executing {nameof(DeleteInventoryAsync)}");

            var inventory = await _unitOfWork.Repository<ProductInventory>().GetByIdAsync(inventoryId);
            if (!inventory.Success || inventory.Data == null)
            {
                return ApiResponse<string>.CreateErrorResponse(
                    new ErrorResponse("Inventory", "Inventory not found"),
                    404
                );
            }

            if (inventory.Data.Quantity > 0)
            {
                return ApiResponse<string>.CreateErrorResponse(
                    new ErrorResponse("Inventory", "Cannot delete inventory with remaining quantity"),
                    400
                );
            }

            using var transaction = await _unitOfWork.BeginTransactionAsync();
            try
            {
                inventory.Data.DeletedAt = DateTime.UtcNow;
                var updateResult = await _unitOfWork.Repository<ProductInventory>().UpdateAsync(inventory.Data);
                if (!updateResult.Success)
                {
                    await transaction.RollbackAsync();
                    return ApiResponse<string>.CreateErrorResponse(
                        new ErrorResponse("Server", "Failed to delete inventory"),
                        500
                    );
                }

                var adminLog = await _adminOpreationServices.AddAdminOpreationAsync(
                    $"Deleted inventory {inventoryId}",
                    Opreations.DeleteOpreation,
                    userId,
                    inventory.Data.ProductId
                );

                if (!adminLog.Success)
                {
                    await transaction.RollbackAsync();
                    return ApiResponse<string>.CreateErrorResponse(
                        new ErrorResponse("Server", "Failed to log operation"),
                        500
                    );
                }

                await _unitOfWork.CommitAsync();
                await transaction.CommitAsync();
                await _cacheManager.RemoveByTagAsync(CACHE_TAG_INVENTORY);

                return ApiResponse<string>.CreateSuccessResponse("Inventory deleted successfully");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError($"Error in {nameof(DeleteInventoryAsync)}: {ex.Message}");
                return ApiResponse<string>.CreateErrorResponse(
                    new ErrorResponse("Server", "An error occurred while processing your request"),
                    500
                );
            }
        }

        public async Task<ApiResponse<List<InventoryDto>>> GetInventoryByProductIdAsync(int productId)
        {
            _logger.LogInformation($"Executing {nameof(GetInventoryByProductIdAsync)}");

            var cacheKey = $"{CACHE_TAG_INVENTORY}product:{productId}";
            var cachedInventory = await _cacheManager.GetAsync<List<InventoryDto>>(cacheKey);
            if (cachedInventory != null)
            {
                return ApiResponse<List<InventoryDto>>.CreateSuccessResponse("Inventory retrieved from cache", cachedInventory);
            }

            var product = await _unitOfWork.Product.GetByIdAsync(productId);
            if (!product.Success || product.Data == null)
            {
                _logger.LogWarning($"Product not found with id: {productId}");
                return ApiResponse<List<InventoryDto>>.CreateErrorResponse(
                    new ErrorResponse("Product", new List<string> { "Product not found" })
                );
            }

            var inventory = product.Data.InventoryEntries
                .Where(i => i.DeletedAt == null)
                .Select(i => _mapper.Map<InventoryDto>(i))
                .ToList();

            await _cacheManager.SetAsync(cacheKey, inventory, tags: new[] { CACHE_TAG_INVENTORY });

            return ApiResponse<List<InventoryDto>>.CreateSuccessResponse("Inventory retrieved successfully", inventory);
        }

        public async Task<ApiResponse<List<InventoryDto>>> GetLowStockAlertsAsync(int threshold = 10)
        {
            _logger.LogInformation($"Executing {nameof(GetLowStockAlertsAsync)} with threshold: {threshold}");

            var cacheKey = $"{CACHE_TAG_INVENTORY}lowstock:{threshold}";
            var cachedAlerts = await _cacheManager.GetAsync<List<InventoryDto>>(cacheKey);
            if (cachedAlerts != null)
            {
                return ApiResponse<List<InventoryDto>>.CreateSuccessResponse("Low stock alerts retrieved from cache", cachedAlerts);
            }

            var lowStockInventory = await _unitOfWork.Repository<ProductInventory>()
                .GetAllAsync(filter: i => i.DeletedAt == null && i.Quantity <= threshold);

            if (!lowStockInventory.Success)
            {
                return ApiResponse<List<InventoryDto>>.CreateErrorResponse(
                    new ErrorResponse("Server", "Failed to retrieve low stock inventory"),
                    500
                );
            }

            var alerts = lowStockInventory.Data
                .Select(i => _mapper.Map<InventoryDto>(i))
                .ToList();

            await _cacheManager.SetAsync(cacheKey, alerts, tags: new[] { CACHE_TAG_INVENTORY });

            return ApiResponse<List<InventoryDto>>.CreateSuccessResponse(
                $"Found {alerts.Count} items with low stock",
                alerts
            );
        }

        public async Task<ApiResponse<string>> BulkUpdateInventoryAsync(List<AddQuantityInvetoryDto> updates, string userId)
        {
            _logger.LogInformation($"Executing {nameof(BulkUpdateInventoryAsync)} with {updates.Count} updates");

            using var transaction = await _unitOfWork.BeginTransactionAsync();
            try
            {
                foreach (var update in updates)
                {
                    var inventory = await _unitOfWork.Repository<ProductInventory>().GetByIdAsync(update.Id);
                    if (!inventory.Success || inventory.Data == null)
                    {
                        await transaction.RollbackAsync();
                        return ApiResponse<string>.CreateErrorResponse(
                            new ErrorResponse("Inventory", $"Inventory not found for ID: {update.Id}"),
                            404
                        );
                    }

                    inventory.Data.Quantity = update.Quantity;
                    inventory.Data.ModifiedAt = DateTime.UtcNow;

                    var updateResult = await _unitOfWork.Repository<ProductInventory>().UpdateAsync(inventory.Data);
                    if (!updateResult.Success)
                    {
                        await transaction.RollbackAsync();
                        return ApiResponse<string>.CreateErrorResponse(
                            new ErrorResponse("Server", $"Failed to update inventory {update.Id}"),
                            500
                        );
                    }
                }

                // Log bulk operation
                var adminLog = await _adminOpreationServices.AddAdminOpreationAsync(
                    $"Bulk updated {updates.Count} inventory items",
                    Opreations.UpdateOpreation,
                    userId,
                    0 // No specific product ID for bulk operation
                );

                if (!adminLog.Success)
                {
                    await transaction.RollbackAsync();
                    return ApiResponse<string>.CreateErrorResponse(
                        new ErrorResponse("Server", "Failed to log operation"),
                        500
                    );
                }

                await _unitOfWork.CommitAsync();
                await transaction.CommitAsync();
                await _cacheManager.RemoveByTagAsync(CACHE_TAG_INVENTORY);

                return ApiResponse<string>.CreateSuccessResponse(
                    $"Successfully updated {updates.Count} inventory items"
                );
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError($"Error in {nameof(BulkUpdateInventoryAsync)}: {ex.Message}");
                return ApiResponse<string>.CreateErrorResponse(
                    new ErrorResponse("Server", "An error occurred while performing bulk update"),
                    500
                );
            }
        }

        public async Task<ApiResponse<List<InventoryDto>>> GetAllInventoryAsync(bool includeDeleted = false)
        {
            _logger.LogInformation($"Executing {nameof(GetAllInventoryAsync)} with includeDeleted: {includeDeleted}");

            var cacheKey = $"{CACHE_TAG_INVENTORY}all:{includeDeleted}";
            var cachedInventory = await _cacheManager.GetAsync<List<InventoryDto>>(cacheKey);
            if (cachedInventory != null)
            {
                return ApiResponse<List<InventoryDto>>.CreateSuccessResponse("Inventory retrieved from cache", cachedInventory);
            }
            var inventoryQuery = await _unitOfWork.Repository<ProductInventory>().GetAllAsync();
            if(!inventoryQuery.Success||inventoryQuery.Data is null)
            {
                return ApiResponse<List<InventoryDto>>.CreateErrorResponse(new ErrorResponse("server error", "Try Again later"));
            }

            if (!includeDeleted)
            {
                inventoryQuery.Data = inventoryQuery.Data.Where(i => i.DeletedAt == null);
            }

            var inventory = await inventoryQuery.Data
                .Select(i => _mapper.Map<InventoryDto>(i))
                .ToListAsync();

            await _cacheManager.SetAsync(cacheKey, inventory, tags: new[] { CACHE_TAG_INVENTORY });

            return ApiResponse<List<InventoryDto>>.CreateSuccessResponse(
                $"Retrieved {inventory.Count} inventory items",
                inventory
            );
        }

        public async Task<ApiResponse<InventoryDto>> GetInventoryById(int id)
        {
            _logger.LogInformation($"Executing {nameof(GetInventoryById)} for ID: {id}");

            var cacheKey = $"{CACHE_TAG_INVENTORY}id:{id}";
            var cachedInventory = await _cacheManager.GetAsync<InventoryDto>(cacheKey);
            if (cachedInventory != null)
            {
                return ApiResponse<InventoryDto>.CreateSuccessResponse("Inventory retrieved from cache", cachedInventory);
            }

            var inventory = await _unitOfWork.ProductInventory.GetByInvetoryIdWithProductAsync(id);
            if (!inventory.Success || inventory.Data == null)
            {
                return ApiResponse<InventoryDto>.CreateErrorResponse(
                    new ErrorResponse("Inventory", "Inventory not found"),
                    404
                );
            }

            var inventoryDto = _mapper.Map<InventoryDto>(inventory.Data);
            await _cacheManager.SetAsync(cacheKey, inventoryDto, tags: new[] { CACHE_TAG_INVENTORY });

            return ApiResponse<InventoryDto>.CreateSuccessResponse("Inventory retrieved successfully", inventoryDto);
        }

	
	}
} 