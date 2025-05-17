using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace E_Commers.DtoModels.AccountDtos
{
	public class RegisterDto
	{
		[Required(ErrorMessage = "Name is required.")]
		[RegularExpression(@"^[a-zA-Z\s]+$", ErrorMessage = "Name must contain only letters and spaces.")]
		[Display(Name = "Full Name")]
		public string Name { get; set; } = string.Empty;

		[Required(ErrorMessage = "Name is required.")]
		[RegularExpression(@"^[a-zA-Z0-9]+$", ErrorMessage = "Name must contain only letters and spaces.")]
		[Display(Name = "User Name")]
		public string UserName { get; set; } = string.Empty;

		[Required(ErrorMessage = "Phone number is required.")]
		[Phone(ErrorMessage = "Invalid phone number format.")]
		[RegularExpression(@"^01[0-9]{9}$", ErrorMessage = "Phone number must be a valid 11-digit Egyptian number starting with 01.")]
		[Display(Name = "Phone Number")]
		public string PhoneNumber { get; set; } = string.Empty;

		[Required(ErrorMessage = "Age is required.")]
		[Range(18, 100, ErrorMessage = "Age must be between 18 and 100.")]
		[Display(Name = "Age")]
		public int Age { get; set; }

		[Required(ErrorMessage = "Email address is required.")]
		[EmailAddress(ErrorMessage = "Invalid email format.")]
		[Display(Name = "Email Address")]
		public string Email { get; set; } = string.Empty;

		//[Required(ErrorMessage = "Address is required.")]
		//[StringLength(100, MinimumLength = 5, ErrorMessage = "Address must be between 5 and 100 characters.")]
		//[Display(Name = "Address")]
		//public string Address { get; set; } = string.Empty;




		[Required(ErrorMessage = "Password is required.")]
		[Display(Name = "Password")]
		public string Password { get; set; } = string.Empty;

		[Required(ErrorMessage = "Confirm password is required.")]
		[Compare("Password", ErrorMessage = "Confirm password does not match the password.")]
		[Display(Name = "Confirm Password")]
		public string ConfirmPassword { get; set; } = string.Empty;
	}}
