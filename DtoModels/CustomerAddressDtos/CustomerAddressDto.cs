using E_Commers.DtoModels.Shared;
using System.ComponentModel.DataAnnotations;

namespace E_Commers.DtoModels.CustomerAddressDtos
{
	public class CustomerAddressDto : BaseDto
	{
		public string CustomerId { get; set; } = string.Empty;
		
		[Required(ErrorMessage = "First Name Required")]
		[StringLength(50, MinimumLength = 2, ErrorMessage = "First Name must be between 2 and 50 characters")]
		[RegularExpression(@"^[a-zA-Z\s]+$", ErrorMessage = "First Name can only contain letters and spaces")]
		public string FirstName { get; set; } = string.Empty;

		[Required(ErrorMessage = "Last Name Required")]
		[StringLength(50, MinimumLength = 2, ErrorMessage = "Last Name must be between 2 and 50 characters")]
		[RegularExpression(@"^[a-zA-Z\s]+$", ErrorMessage = "Last Name can only contain letters and spaces")]
		public string LastName { get; set; } = string.Empty;

		[Required(ErrorMessage = "Phone Number Required")]
		[Phone(ErrorMessage = "Invalid phone number format")]
		[StringLength(20, ErrorMessage = "Phone number too long")]
		public string PhoneNumber { get; set; } = string.Empty;

		[Required(ErrorMessage = "Country Required")]
		[StringLength(50, MinimumLength = 2, ErrorMessage = "Country must be between 2 and 50 characters")]
		public string Country { get; set; } = string.Empty;

		[Required(ErrorMessage = "State/Province Required")]
		[StringLength(50, MinimumLength = 2, ErrorMessage = "State/Province must be between 2 and 50 characters")]
		public string State { get; set; } = string.Empty;

		[Required(ErrorMessage = "City Required")]
		[StringLength(50, MinimumLength = 2, ErrorMessage = "City must be between 2 and 50 characters")]
		public string City { get; set; } = string.Empty;

		[Required(ErrorMessage = "Street Address Required")]
		[StringLength(200, MinimumLength = 5, ErrorMessage = "Street Address must be between 5 and 200 characters")]
		public string StreetAddress { get; set; } = string.Empty;

		[StringLength(100, ErrorMessage = "Apartment/Suite too long")]
		public string? ApartmentSuite { get; set; }

		[Required(ErrorMessage = "Postal Code Required")]
		[StringLength(20, MinimumLength = 3, ErrorMessage = "Postal Code must be between 3 and 20 characters")]
		[RegularExpression(@"^[a-zA-Z0-9\s\-]+$", ErrorMessage = "Postal Code can only contain letters, numbers, spaces, and hyphens")]
		public string PostalCode { get; set; } = string.Empty;

		[Required(ErrorMessage = "Address Type Required")]
		[StringLength(20, ErrorMessage = "Address Type too long")]
		public string AddressType { get; set; } = "Home";

		public bool IsDefault { get; set; } = false;

		[StringLength(500, ErrorMessage = "Additional Notes too long")]
		public string? AdditionalNotes { get; set; }

		// Calculated properties
		public string FullName => $"{FirstName} {LastName}".Trim();
		public string FullAddress => $"{StreetAddress}{(string.IsNullOrEmpty(ApartmentSuite) ? "" : $" {ApartmentSuite}")}, {City}, {State} {PostalCode}, {Country}".Trim();
	}

	public class CreateCustomerAddressDto
	{
		[Required(ErrorMessage = "First Name Required")]
		[StringLength(50, MinimumLength = 2, ErrorMessage = "First Name must be between 2 and 50 characters")]
		[RegularExpression(@"^[a-zA-Z\s]+$", ErrorMessage = "First Name can only contain letters and spaces")]
		public string FirstName { get; set; } = string.Empty;

		[Required(ErrorMessage = "Last Name Required")]
		[StringLength(50, MinimumLength = 2, ErrorMessage = "Last Name must be between 2 and 50 characters")]
		[RegularExpression(@"^[a-zA-Z\s]+$", ErrorMessage = "Last Name can only contain letters and spaces")]
		public string LastName { get; set; } = string.Empty;

		[Required(ErrorMessage = "Phone Number Required")]
		[Phone(ErrorMessage = "Invalid phone number format")]
		[StringLength(20, ErrorMessage = "Phone number too long")]
		public string PhoneNumber { get; set; } = string.Empty;

		[Required(ErrorMessage = "Country Required")]
		[StringLength(50, MinimumLength = 2, ErrorMessage = "Country must be between 2 and 50 characters")]
		public string Country { get; set; } = string.Empty;

		[Required(ErrorMessage = "State/Province Required")]
		[StringLength(50, MinimumLength = 2, ErrorMessage = "State/Province must be between 2 and 50 characters")]
		public string State { get; set; } = string.Empty;

		[Required(ErrorMessage = "City Required")]
		[StringLength(50, MinimumLength = 2, ErrorMessage = "City must be between 2 and 50 characters")]
		public string City { get; set; } = string.Empty;

		[Required(ErrorMessage = "Street Address Required")]
		[StringLength(200, MinimumLength = 5, ErrorMessage = "Street Address must be between 5 and 200 characters")]
		public string StreetAddress { get; set; } = string.Empty;

		[StringLength(100, ErrorMessage = "Apartment/Suite too long")]
		public string? ApartmentSuite { get; set; }

		[Required(ErrorMessage = "Postal Code Required")]
		[StringLength(20, MinimumLength = 3, ErrorMessage = "Postal Code must be between 3 and 20 characters")]
		[RegularExpression(@"^[a-zA-Z0-9\s\-]+$", ErrorMessage = "Postal Code can only contain letters, numbers, spaces, and hyphens")]
		public string PostalCode { get; set; } = string.Empty;

		[Required(ErrorMessage = "Address Type Required")]
		[StringLength(20, ErrorMessage = "Address Type too long")]
		public string AddressType { get; set; } = "Home";

		public bool IsDefault { get; set; } = false;

		[StringLength(500, ErrorMessage = "Additional Notes too long")]
		public string? AdditionalNotes { get; set; }
	}

	public class UpdateCustomerAddressDto
	{
		[StringLength(50, MinimumLength = 2, ErrorMessage = "First Name must be between 2 and 50 characters")]
		[RegularExpression(@"^[a-zA-Z\s]+$", ErrorMessage = "First Name can only contain letters and spaces")]
		public string? FirstName { get; set; }

		[StringLength(50, MinimumLength = 2, ErrorMessage = "Last Name must be between 2 and 50 characters")]
		[RegularExpression(@"^[a-zA-Z\s]+$", ErrorMessage = "Last Name can only contain letters and spaces")]
		public string? LastName { get; set; }

		[Phone(ErrorMessage = "Invalid phone number format")]
		[StringLength(20, ErrorMessage = "Phone number too long")]
		public string? PhoneNumber { get; set; }

		[StringLength(50, MinimumLength = 2, ErrorMessage = "Country must be between 2 and 50 characters")]
		public string? Country { get; set; }

		[StringLength(50, MinimumLength = 2, ErrorMessage = "State/Province must be between 2 and 50 characters")]
		public string? State { get; set; }

		[StringLength(50, MinimumLength = 2, ErrorMessage = "City must be between 2 and 50 characters")]
		public string? City { get; set; }

		[StringLength(200, MinimumLength = 5, ErrorMessage = "Street Address must be between 5 and 200 characters")]
		public string? StreetAddress { get; set; }

		[StringLength(100, ErrorMessage = "Apartment/Suite too long")]
		public string? ApartmentSuite { get; set; }

		[StringLength(20, MinimumLength = 3, ErrorMessage = "Postal Code must be between 3 and 20 characters")]
		[RegularExpression(@"^[a-zA-Z0-9\s\-]+$", ErrorMessage = "Postal Code can only contain letters, numbers, spaces, and hyphens")]
		public string? PostalCode { get; set; }

		[StringLength(20, ErrorMessage = "Address Type too long")]
		public string? AddressType { get; set; }

		public bool? IsDefault { get; set; }

		[StringLength(500, ErrorMessage = "Additional Notes too long")]
		public string? AdditionalNotes { get; set; }
	}

	public class SetDefaultAddressDto
	{
		[Required(ErrorMessage = "Address ID Required")]
		[Range(1, int.MaxValue, ErrorMessage = "Address ID must be greater than 0")]
		public int AddressId { get; set; }
	}
} 