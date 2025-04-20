using Microsoft.VisualBasic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace E_Commers.Models
{
	public class Product : BaseEntity
	{
		[Required(ErrorMessage = "Name Required")]
		[StringLength(20, MinimumLength = 5, ErrorMessage = "Description must be between 5 and 20 characters.")]
		public string Name { get; set; } = string.Empty;

		[Required(ErrorMessage = "Description is Required.")]
		[StringLength(50, MinimumLength = 10, ErrorMessage = "Description must be between 10 and 50 characters.")]
		public string Description { get; set; } = string.Empty;

		[ForeignKey("Category")]
		public int CategoryId { get; set; }
		public   Category Category { get; set; }

		[Required(ErrorMessage = "Quantity Required")]
		[Range(0,int.MaxValue)]
		public int Quantity { get; set; }
		public ICollection<ProductInventory> InventoryEntries { get; set; } = new List<ProductInventory>();

		[ForeignKey("Discount")]
		public int? DiscountId { get; set; } 
		public  Discount? Discount { get; set; }

		[Range(0, (double)decimal.MaxValue)]
		[Required(ErrorMessage = "Price Required")]
		public decimal Price { get; set; }
		public ICollection<string>?ImagesUrl { get; set; }
	}
}
