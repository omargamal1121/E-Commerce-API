using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace E_Commers.Models
{
	public class PaymentProvider
	{
		public int Id { get; set; }

		[Required(ErrorMessage = "Payment Provider name is required.")]
		[StringLength(50, MinimumLength = 3, ErrorMessage = "Name must be between 3 and 50 characters.")]
		public string Name { get; set; } = string.Empty;

		[Required(ErrorMessage = "API Endpoint is required.")]
		[StringLength(100, MinimumLength = 3, ErrorMessage = "API must be between 3 and 100 characters.")]
		public string ApiEndpoint { get; set; } = string.Empty;

		[StringLength(200, ErrorMessage = "Public Key is too long.")]
		public string? PublicKey { get; set; }

		[StringLength(200, ErrorMessage = "Private Key is too long.")]
		public string? PrivateKey { get; set; }

		[ForeignKey("PaymentMethod")]
		public int PaymentMethodId { get; set; }
		public PaymentMethod PaymentMethod { get; set; }

		public bool IsActive { get; set; }
	}
}
