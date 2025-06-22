using System.ComponentModel.DataAnnotations;

namespace E_Commers.Models
{
    public class Image
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string Url { get; set; }

        public string? AltText { get; set; }

        public string? Title { get; set; }

        public int? Width { get; set; }

        public int? Height { get; set; }

        public long? FileSize { get; set; }

        public DateTime UploadDate { get; set; } = DateTime.UtcNow;

		public string? Folder { get; set; }
		public bool IsMain { get; set; } = false;
		public string? FileType { get; set; }
		public ICollection<Product>? Products { get; set; }
        public ICollection<Category>? Categories { get; set; }
		public ICollection<Customer>? Customers { get; set; }
	}
} 