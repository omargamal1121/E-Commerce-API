using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace E_Commers.Models
{
	public class Product : BaseEntity
	{
		[Required(ErrorMessage = "Name Required")]
		public string Name { get; set; } = string.Empty;

		[Required(ErrorMessage = "Description is Required.")]
		[StringLength(50, MinimumLength = 10, ErrorMessage = "Description must be between 10 and 50 characters.")]
		public string Description { get; set; } = string.Empty;

		[ForeignKey("Category")]
		public int CategoryId { get; set; }
		public  Category Category { get; set; }

		[ForeignKey("Inventory")]
		public int InventoryId { get; set; }
		[Range(0,int.MaxValue)]
		public int Quantity { get; set; }
		public  ProductInventory Inventory { get; set; }

		[ForeignKey("Discount")]
		public int? DiscountId { get; set; } 
		public  Discount? Discount { get; set; }

		[Range(0, (double)decimal.MaxValue)]
		[Required(ErrorMessage = "Price Required")]
		public decimal Price { get; set; }
	}
}
