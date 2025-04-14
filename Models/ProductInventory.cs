using System.ComponentModel.DataAnnotations;

namespace E_Commers.Models
{
	public class ProductInventory : BaseEntity
	{
		public int ProductId { get; set; }
		public  Product Product { get; set; }

		public int WarehouseId { get; set; }
		public  Warehouse Warehouse { get; set; }
		[Range(0, int.MaxValue)]
		[Required(ErrorMessage = "Quantity Required")]
		public int Quantity { get; set; }
	}

}
