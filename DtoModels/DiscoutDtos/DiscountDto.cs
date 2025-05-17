using E_Commers.DtoModels.ProductDtos;
using E_Commers.DtoModels.Shared;
using E_Commers.Models;
using System.ComponentModel.DataAnnotations;

namespace E_Commers.DtoModels.DiscoutDtos
{
	public class DiscountDto:BaseDto
	{
		public DiscountDto()
		{
			
		}
		public DiscountDto(int id, string? name = null, decimal discount = 0, string? description=null, bool isactive = false,DateTime? createAt=null, DateTime? modifiedAt = null, DateTime? deleteAt = null)
		{
			  Id=id;
			Name=Name; Description=Description; DiscountPercent = discount;
			IsActive=isactive;
			CreatedAt = createAt;
			  ModifiedAt= modifiedAt;
			ModifiedAt=deleteAt;


		}
		[Required(ErrorMessage = "Name is required.")]
		[StringLength(20, MinimumLength = 5, ErrorMessage = "Name must be between 5 and 20 characters.")]
		public string Name { get; set; } = string.Empty;

		[Required(ErrorMessage = "Description is required.")]
		[StringLength(50, MinimumLength = 10, ErrorMessage = "Description must be between 10 and 50 characters.")]
		public string Description { get; set; } = string.Empty;

		[Required(ErrorMessage = "Discount Percent Required")]
		[Range(0, 1, ErrorMessage = "Must be between 0 and 1")]
		public decimal DiscountPercent { get; set; }
		public bool IsActive { get; set; } = false;

		public List<ProductDto> products { get; set; }

	}
}
