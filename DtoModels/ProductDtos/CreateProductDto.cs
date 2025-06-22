using System.ComponentModel.DataAnnotations;

namespace E_Commers.DtoModels.ProductDtos
{
	public class CreateProductDto 
	{
		[Required(ErrorMessage = "Name Required")]
		[StringLength(20, MinimumLength = 5, ErrorMessage = "Description must be between 5 and 20 characters.")]
		public string Name { get; set; } = string.Empty;

		[Required(ErrorMessage = "Description is Required.")]
		[StringLength(50, MinimumLength = 10, ErrorMessage = "Description must be between 10 and 50 characters.")]
		public string Description { get; set; } = string.Empty;

		public int CategoryId { get; set; }

		[Required(ErrorMessage = "Quantity Required")]
		[Range(0, int.MaxValue)]
		public int Quantity { get; set; }


		[Range(0, (double)decimal.MaxValue)]
		[Required(ErrorMessage = "Price Required")]
		public decimal Price { get; set; }

		[Required(ErrorMessage = "Warehouse Id Required")]
		[Range(0, int.MaxValue, ErrorMessage = "Invalid Warehouse Id")]
		public int WarehouseId { get; set; }
	}
}
