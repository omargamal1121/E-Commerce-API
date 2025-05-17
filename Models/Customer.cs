using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace E_Commers.Models
{
	public class Customer:IdentityUser
	{
		[Required(ErrorMessage = "Name Required")]
		[RegularExpression(@"^[a-zA-Z\s]+$", ErrorMessage = "Name must contain only letters and spaces")]
		public string Name { get; set; } = string.Empty;

		[Required(ErrorMessage = "Age Required")]
		[Range(18, 100, ErrorMessage = "Must be between 18 and 100")]
		public int Age { get; set; }

		public string? ProfilePicture { get; set; }
		public DateTime CreateAt { get; set; } = DateTime.UtcNow;
		public DateTime? DeletedAt { get; set; }
		public DateTime? LastVisit { get; set; }

		public List<Order> Orders { get; set; } = new List<Order>();
		public ICollection<CustomerAddress> Addresses { get; set; } = new List<CustomerAddress>();
		public ICollection<AdminOperationsLog> adminOperationsLogs{ get; set; } = new List<AdminOperationsLog>();
		public ICollection<UserOperationsLog>  userOperationsLogs { get; set; } = new List<UserOperationsLog>();

		public string? ImageUrl { get; set; } 
	}
}
