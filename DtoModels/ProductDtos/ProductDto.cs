using E_Commers.DtoModels.CategoryDtos;
using E_Commers.DtoModels.DiscoutDtos;
using E_Commers.DtoModels.Shared;
using E_Commers.Models;


namespace E_Commers.DtoModels.ProductDtos
{
	public class ProductDto:BaseDto
	{

	
		public string Name { get; set; } = string.Empty;
		public string Description { get; set; } = string.Empty;
		public  CategoryDto? Category { get; set; }
		public  DiscountDto? Discount { get; set; }
		public int AvailabeQuantity { get; set; }
		public decimal FinalPrice { get; set; }
	}
}
