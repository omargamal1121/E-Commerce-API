using System.ComponentModel.DataAnnotations;

namespace E_Commers.Models
{
	public class PaymentMethod
	{
		public int Id { get; set; }

		[Required(ErrorMessage = "Payment method name is required.")]
		[StringLength(20, MinimumLength = 3, ErrorMessage = "Name must be between 3 and 20 characters.")]
		public string Name { get; set; } = string.Empty;
	}
}
