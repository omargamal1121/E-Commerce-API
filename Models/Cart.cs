namespace E_Commers.Models
{
	public class Cart:BaseEntity
	{
		public string Userid { get; set; } = string.Empty;
		public required Customer  customer { get; set; }
		public List<Item> Items { get; set; } = new List<Item>();
	}
}
