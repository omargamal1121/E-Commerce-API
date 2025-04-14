using E_Commers.Enums;

namespace E_Commers.Models
{
	public class AdminOperationsLog :BaseEntity
	{
		public string AdminId { get; set; } = string.Empty;
		public   Customer Admin { get; set; }

		public Opreations OperationType { get; set; } = Opreations.AddOpreation;
		public int ItemId { get; set; }
		public string Description { get; set; } = string.Empty;
		public DateTime Timestamp { get; set; } = DateTime.UtcNow;
	}
}
