using E_Commers.Enums;

namespace E_Commers.Models
{
	public class UserOperationsLog:BaseEntity
	{
		
		public string UserId { get; set; } = string.Empty;
		public Customer User { get; set; }  

		public Opreations OperationType { get; set; } = Opreations.AddOpreation;
		public int ItemId { get; set; }
		public string Description { get; set; } = string.Empty;
		public DateTime Timestamp { get; set; } = DateTime.UtcNow;
	}
}
