using System.ComponentModel.DataAnnotations;

namespace E_Commers.Models
{
	public class CustomerAddress
	{
		public int Id { get; set; }

		[Required]
		public string CustomerId { get; set; } = string.Empty;
		public Customer Customer { get; set; } 

		[Required(ErrorMessage = "Country Required")]
		[StringLength(15, MinimumLength = 5, ErrorMessage = "Country must be between 5 and 15 characters")]
		public string Country { get; set; } = string.Empty;

		[Required(ErrorMessage = "City Required")]
		[StringLength(15, MinimumLength = 5, ErrorMessage = "City must be between 5 and 15 characters")]
		public string City { get; set; } = string.Empty;

		[Required(ErrorMessage = "Address Required")]
		[StringLength(25, MinimumLength = 10, ErrorMessage = "Address must be between 10 and 25 characters")]
		public string Address { get; set; } = string.Empty;

		[Required(ErrorMessage = "Postal Code Required")]
		[StringLength(8, MinimumLength = 6, ErrorMessage = "Postal Code length must be between 6 to 8")]
		public string PostalCode { get; set; } = string.Empty;

		public string AddressType { get; set; } = "Home"; 
	}
}
