using System.ComponentModel.DataAnnotations;
using E_Commers.Enums;

namespace E_Commers.DtoModels.ProductDtos
{
	public class CreateProductVariantDto
	{
		[Required(ErrorMessage = "Color is required.")]
		[StringLength(20, MinimumLength = 2, ErrorMessage = "Color must be between 2 and 20 characters.")]
		public string Color { get; set; } = string.Empty;
		
		[StringLength(10, ErrorMessage = "Size must not exceed 10 characters.")]
		public string Size { get; set; }
		
		[StringLength(10, ErrorMessage = "Waist must not exceed 10 characters.")]
		public int? Waist { get; set; }
		
		[StringLength(10, ErrorMessage = "Length must not exceed 10 characters.")]
		public int? Length { get; set; }

		[Required(ErrorMessage = "Quantity is required.")]
		[Range(0, int.MaxValue, ErrorMessage = "Quantity must be non-negative.")]
		public int Quantity { get; set; }

		[Required(ErrorMessage = "Price is required.")]
		[Range(0, (double)decimal.MaxValue, ErrorMessage = "Price must be non-negative.")]
		public decimal Price { get; set; }
	}
}