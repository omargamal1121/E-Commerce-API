using System.ComponentModel.DataAnnotations.Schema;

namespace E_Commers.Models
{
	public class Item
	{
		public int Id { get; set; }

		[ForeignKey("Order")]
		public int OrderId { get; set; }
		public required Order Order { get; set; }

		[ForeignKey("Product")]
		public int ProductId { get; set; }
		public required Product Product { get; set; }

		public  int Quantity { get; set; }
		public DateTime AddedAt { get; set; } = DateTime.UtcNow;
	}
}
