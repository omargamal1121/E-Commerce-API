using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace E_Commers.Models
{
	public class Payment
	{
		[Key]
		public int Id { get; set; }

		[ForeignKey("Customer")]
		public string CustomerId { get; set; } = string.Empty;
		public Customer Customer { get; set; }

		[ForeignKey("PaymentMethod")]
		public int PaymentMethodId { get; set; }
		public PaymentMethod PaymentMethod { get; set; }

		[ForeignKey("PaymentProvider")]
		public int PaymentProviderId { get; set; }
		public PaymentProvider PaymentProvider { get; set; }

		[Required]
		[Range(0.01, double.MaxValue, ErrorMessage = "Amount must be greater than zero.")]
		public decimal Amount { get; set; }

		public DateTime PaymentDate { get; set; } = DateTime.UtcNow;

		[ForeignKey("Order")]
		public int OrderId { get; set; }
		public Order Order { get; set; }

		[Required(ErrorMessage = "Payment Status is required.")]
		[StringLength(20, MinimumLength = 3, ErrorMessage = "Status must be between 3 and 20 characters.")]
		public string Status { get; set; }
	}
}
