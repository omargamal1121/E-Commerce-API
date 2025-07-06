using System.ComponentModel.DataAnnotations;

namespace E_Commers.Models
{
    public class Image : BaseEntity
    {
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
        public ICollection<SubCategory>? subCategories { get; set; }
		public ICollection<Customer>? Customers { get; set; }
        public int? CategoryId { get; set; }
        public Category? Category { get; set; }
        public int? SubCategoryId { get; set; }
        public SubCategory? SubCategory { get; set; }
        public int? ProductId { get; set; }
        public Product? Product { get; set; }
        public int? CollectionId { get; set; }
        public Collection? Collection { get; set; }
	}
} 