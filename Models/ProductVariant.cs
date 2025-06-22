using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace E_Commers.Models
{
    public class ProductVariant : BaseEntity
    {
        [Required]
        public string? Color { get; set; }
        
        public string? Size { get; set; }
        public string? Waist { get; set; }
        public string? Length { get; set; }
        public string? FitType { get; set; }

        [Required]
        public int Quantity { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Price { get; set; }

        [Required]
        public int ProductId { get; set; }
        public Product Product { get; set; }
    }
} 