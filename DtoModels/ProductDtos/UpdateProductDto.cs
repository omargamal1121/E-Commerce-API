using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace E_Commers.DtoModels.ProductDtos
{
	public class UpdateProductDto
	{
		[StringLength(20, MinimumLength = 5, ErrorMessage = "Name must be between 5 and 20 characters.")]
		[RegularExpression(@"^[a-zA-Z0-9][a-zA-Z0-9\s\-,]*[a-zA-Z0-9]$", ErrorMessage = "Name must start and end with an alphanumeric character and can contain spaces, hyphens, and commas in between.")]
		public string? Name { get; set; }

		[StringLength(50, MinimumLength = 10, ErrorMessage = "Description must be between 10 and 50 characters.")]
		[RegularExpression(@"^[\w\s.,\-()'\""]{0,500}$", ErrorMessage = "Description can contain up to 500 characters: letters, numbers, spaces, and .,-()'\"")]
		public string? Description { get; set; }

		public int? CategoryId { get; set; }

		[Range(0, int.MaxValue)]
		public int? Quantity { get; set; }

		public List<UpdateProductVariantDto>? Variants { get; set; }
		public List<int>? CollectionIds { get; set; }
		public List<IFormFile>? NewImages { get; set; }
		public List<int>? RemoveImageIds { get; set; }
	}
}
