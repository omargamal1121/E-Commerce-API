using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace E_Commers.Models
{
	public class PaymentProviders
	{
		[Key]
		public int Id { get; set; }

		[Required(ErrorMessage = "Provider name is required.")]
		[StringLength(50, MinimumLength = 3, ErrorMessage = "Provider name must be between 3 and 50 characters.")]
		public string Name { get; set; } = string.Empty;

		[ForeignKey("PaymentMethod")]
		public int PaymentMethodId { get; set; }
		public required  PaymentMethod PaymentMethod { get; set; }

		public bool IsActive { get; set; } = true;
	}

}
