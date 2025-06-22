using E_Commers.DtoModels.InventoryDtos;
using E_Commers.Models;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace E_Commers.DtoModels.WareHouseDtos
{
	public class WareHouseDto:BaseEntity
	{
		[Required(ErrorMessage = "Name Required")]
		[StringLength(20, MinimumLength = 5, ErrorMessage = "Must Between 5 t0 20 ")]
		public string Name { get; set; } = string.Empty;
		[Required(ErrorMessage = "Address Required")]
		[StringLength(50, MinimumLength = 10, ErrorMessage = "Must Between 10 t0 50 ")]
		public string Address { get; set; } = string.Empty;
		[Phone]
		public string Phone { get; set; } = string.Empty;

	}
}
