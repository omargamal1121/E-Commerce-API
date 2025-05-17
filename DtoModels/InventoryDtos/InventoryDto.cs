using E_Commers.DtoModels.ProductDtos;
using E_Commers.DtoModels.Shared;
using E_Commers.Models;

namespace E_Commers.DtoModels.InventoryDtos
{
	public class InventoryDto:BaseDto
	{
		public int Quantityinsidewarehouse { get; set; }
		public int WareHousid { get; set; }
		public ProductDto Product { get; set; }
	}
}
