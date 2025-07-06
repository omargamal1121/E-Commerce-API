using System.ComponentModel.DataAnnotations;

namespace E_Commers.Models
{
	public class PaymentMethod:BaseEntity
	{
		

		[Required(ErrorMessage = "Payment method name is required.")]
		[StringLength(20, MinimumLength = 3, ErrorMessage = "Name must be between 3 and 20 characters.")]
		[RegularExpression(@"^[a-zA-Z0-9][a-zA-Z0-9\s\-,]*[a-zA-Z0-9]$", ErrorMessage = "Name must start and end with an alphanumeric character and can contain spaces, hyphens, and commas in between.")]
		public string Name { get; set; } = string.Empty;
	}
}
