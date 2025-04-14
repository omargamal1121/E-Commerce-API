using E_Commers.DtoModels.InventoryDtos;
using E_Commers.Models;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace E_Commers.DtoModels.WareHouseDtos
{
	public class WareHouseDto:BaseEntity
	{
		public string Name { get; set; } = string.Empty;

		public string Address { get; set; } = string.Empty;
		[Phone]
		[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
		public string Phone { get; set; } = string.Empty;

		public IEnumerable<InventoryDto> Inventory { get; set; } = new List<InventoryDto>();
	}
}
