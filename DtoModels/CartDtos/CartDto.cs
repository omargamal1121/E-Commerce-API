using E_Commers.DtoModels.Shared;
using E_Commers.Models;

namespace E_Commers.DtoModels.CartDtos
{
    public class CartDto : BaseDto
    {
        public string UserId { get; set; } = string.Empty;
        public CustomerDto? Customer { get; set; }
        public List<CartItemDto> Items { get; set; } = new List<CartItemDto>();
        public decimal TotalPrice { get; set; }
        public int TotalItems { get; set; }
        public bool IsEmpty => !Items.Any();
    }

    public class CartItemDto : BaseDto
    {
        public int ProductId { get; set; }
        public ProductDto? Product { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal TotalPrice { get; set; }
        public DateTime AddedAt { get; set; }
    }

    public class CustomerDto : BaseDto
    {
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? ProfilePicture { get; set; }
    }

    public class ProductDto : BaseDto
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal MinPrice { get; set; }
        public decimal MaxPrice { get; set; }
        public decimal FinalPrice { get; set; }
        public int AvailableQuantity { get; set; }
        public bool HasVariants { get; set; }
        public int TotalVariants { get; set; }
        public DiscountDto? Discount { get; set; }
        public List<ImageDto> Images { get; set; } = new List<ImageDto>();
        public List<ProductVariantDto> Variants { get; set; } = new List<ProductVariantDto>();
    }

    public class ProductVariantDto : BaseDto
    {
        public string Color { get; set; } = string.Empty;
        public string Size { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int Quantity { get; set; }
    }

    public class DiscountDto : BaseDto
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal DiscountPercent { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public bool IsActive { get; set; }
    }

    public class ImageDto : BaseDto
    {
        public string Url { get; set; } = string.Empty;
        public bool IsMain { get; set; }
    }
} 