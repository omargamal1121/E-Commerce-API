using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace E_Commers.Models
{
	public class Order : BaseEntity
	{
		[ForeignKey("Customer")]
		public string CustomerId { get; set; } = string.Empty;
		public Customer Customer { get; set; }

		[Range(0.01, double.MaxValue, ErrorMessage = "Total must be greater than zero.")]
		[Required(ErrorMessage = "Total Required")]
		public decimal Total { get; set; }
		public Payment Payment { get; set; }

		public List<Item> Items { get; set; } = new List<Item>();
	}
}
