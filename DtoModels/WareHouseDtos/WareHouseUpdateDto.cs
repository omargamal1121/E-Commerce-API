using System.ComponentModel.DataAnnotations;

namespace E_Commers.DtoModels.WareHouseDtos
{
	public class UpdateWareHouseDto
	{

		[StringLength(20, MinimumLength = 5, ErrorMessage = "Must Between 5 t0 20 ")]
		public string? NewName { get; set; } 
		[StringLength(50, MinimumLength = 10, ErrorMessage = "Must Between 10 t0 50 ")]
		public string? NewAddress { get; set; } 
		[Phone]
		public string? NewPhone { get; set; }
	}
}
