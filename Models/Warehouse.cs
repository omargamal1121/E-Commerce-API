using System.ComponentModel.DataAnnotations;

namespace E_Commers.Models
{
	public class Warehouse : BaseEntity
	{
		[Required(ErrorMessage ="Name Required")]
		[StringLength(20,MinimumLength =5,ErrorMessage ="Must Between 5 t0 20 ")]
		public string Name { get; set; } = string.Empty;
		[Required(ErrorMessage = "Address Required")]
		[StringLength(50,MinimumLength =10,ErrorMessage ="Must Between 10 t0 50 ")]
		public string Address { get; set; } = string.Empty;
		[Phone]
		public string Phone { get; set; } = string.Empty;
		public ICollection<ProductInventory> ProductInventories { get; set; } = new List<ProductInventory>();
	}
}
