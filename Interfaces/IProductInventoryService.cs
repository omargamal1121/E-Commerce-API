using E_Commers.DtoModels;
using E_Commers.DtoModels.InventoryDtos;
using E_Commers.DtoModels.Responses;

namespace E_Commers.Interfaces
{
    public interface IProductInventoryService
    {
		Task<ApiResponse<InventoryDto>> CreateInventoryAsync(CreateInvetoryDto dto, string userId);
        Task<ApiResponse<InventoryDto>> TransferQuantityAsync(TransfereQuantityInvetoryDto dto, string userId);
        Task<ApiResponse<List<InventoryDto>>> GetWarehouseInventoryAsync(int warehouseId);
        Task<ApiResponse<InventoryDto>> GetInventoryById(int inventoryid);
        Task<ApiResponse<string>> DeleteInventoryAsync(int inventoryId, string userId);
        Task<ApiResponse<InventoryDto>> UpdateInventoryQuantityAsync(UpdateInventoryQuantityDto dto, string userId);

		Task<ApiResponse<List<InventoryDto>>> GetInventoryByProductIdAsync(int productId);
        Task<ApiResponse<List<InventoryDto>>> GetLowStockAlertsAsync(int threshold = 10);
        Task<ApiResponse<string>> BulkUpdateInventoryAsync(List<AddQuantityInvetoryDto> updates, string userId);
        Task<ApiResponse<List<InventoryDto>>> GetAllInventoryAsync(bool includeDeleted = false);
    }
} 