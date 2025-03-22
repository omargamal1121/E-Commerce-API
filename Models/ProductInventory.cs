using System.ComponentModel.DataAnnotations;

namespace E_Commers.Models
{
	public class ProductInventory:BaseEntity
	{

		[Required(ErrorMessage = "Name is required.")]
		[StringLength(20, MinimumLength = 5, ErrorMessage = "Name must be between 5 and 20 characters.")]
		public string Name { get; set; }

		[Required(ErrorMessage = "Quantity is required.")]
		[Range(0,int.MaxValue, ErrorMessage = "Quantity Can't be Negative")]

		public List<Product> products { get; set; } = new List<Product>();
	}
}
