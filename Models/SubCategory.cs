﻿namespace E_Commers.Models
{
	public class SubCategory
	{
		public int Id { get; set; }
		public string Name { get; set; }

		public int CategoryId { get; set; }
		public Category Category { get; set; }
		public ICollection<Image> Images { get; set; }


		public ICollection<Product> Products { get; set; } = new List<Product>();
	}
}
