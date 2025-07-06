using System.ComponentModel.DataAnnotations;
using E_Commers.Enums;

namespace E_Commers.DtoModels.ProductDtos
{
	public class UpdateProductVariantDto
	{
		public int? Id { get; set; } // For existing variants

		[StringLength(20, MinimumLength = 2, ErrorMessage = "Color must be between 2 and 20 characters.")]
		public string? Color { get; set; }
		
		[StringLength(10, ErrorMessage = "Size must not exceed 10 characters.")]
		public string? Size { get; set; }
		
		[StringLength(10, ErrorMessage = "Waist must not exceed 10 characters.")]
		public int? Waist { get; set; }
		
		[StringLength(10, ErrorMessage = "Length must not exceed 10 characters.")]
		public int? Length { get; set; }
		
		[StringLength(20, ErrorMessage = "Fit type must not exceed 20 characters.")]
		public int? FitType { get; set; }

		[Range(0, int.MaxValue, ErrorMessage = "Quantity must be non-negative.")]
		public int? Quantity { get; set; }

		[Range(0, (double)decimal.MaxValue, ErrorMessage = "Price must be non-negative.")]
		public decimal? Price { get; set; }
	}
}