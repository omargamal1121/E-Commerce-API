using E_Commers.DtoModels.InventoryDtos;
using E_Commers.DtoModels.Responses;
using E_Commers.DtoModels.WareHouseDtos;

namespace E_Commers.Services.WareHouseServices
{
	public interface IWareHouseServices
	{
		public Task<ApiResponse<List< WareHouseDto>>> GetAllWareHousesAsync();
		public Task<ApiResponse<WareHouseDto>> GetWareHouseByIdAsync(int id);
		public Task<ApiResponse<WareHouseDto>> CreateWareHouseAsync(string userid, WareHouseDto wareHouse);
		public Task<ApiResponse<WareHouseDto>> UpdateWareHouseAsync(int id,string userid,WareHouseDto wareHouse);
		public Task<ApiResponse<string>> RemoveWareHouseAsync(int id,string userid);
		public Task<ApiResponse<InventoryDto>> AddInventoryToWareHouseAsync(int id,string userid,int Inventoryid);
		public Task<ApiResponse<WareHouseDto>> ReturnRemovedWareHouseAsync(int id,string userid);
		public Task<ApiResponse<string>> TransferProductsAsync(int from_warehouse_id, int to_warehouse_id, string userid,int Inventoryid);
		public Task<ApiResponse<string>> IsExsistAsync(int id);
	}
}
