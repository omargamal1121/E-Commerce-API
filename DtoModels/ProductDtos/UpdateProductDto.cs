using System.ComponentModel.DataAnnotations;

namespace E_Commers.DtoModels.ProductDtos
{
	public class UpdateProductDto
	{
		[StringLength(20, MinimumLength = 5, ErrorMessage = "Description must be between 5 and 20 characters.")]
		public string? Name { get; set; }


		[StringLength(50, MinimumLength = 10, ErrorMessage = "Description must be between 10 and 50 characters.")]
		public string? Description { get; set; }

		public int? CategoryId { get; set; }

	
		[Range(0, int.MaxValue)]
		public int? Quantity { get; set; }


		[Range(0, (double)decimal.MaxValue)]
	
		public decimal? Price { get; set; }
	}
}
