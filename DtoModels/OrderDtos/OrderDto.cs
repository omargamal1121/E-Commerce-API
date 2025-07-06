using E_Commers.DtoModels.Shared;
using E_Commers.Enums;

namespace E_Commers.DtoModels.OrderDtos
{
    public class OrderDto : BaseDto
    {
        public string OrderNumber { get; set; } = string.Empty;
        public string CustomerId { get; set; } = string.Empty;
        public CustomerDto? Customer { get; set; }
        public OrderStatus Status { get; set; }
        public decimal Subtotal { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal ShippingCost { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal Total { get; set; }
        public string? Notes { get; set; }
        public DateTime? ShippedAt { get; set; }
        public DateTime? DeliveredAt { get; set; }
        public DateTime? CancelledAt { get; set; }
        public List<OrderItemDto> Items { get; set; } = new List<OrderItemDto>();
        public PaymentDto? Payment { get; set; }
        
        // Calculated properties
        public bool IsCancelled => Status == OrderStatus.Cancelled;
        public bool IsDelivered => Status == OrderStatus.Delivered;
        public bool IsShipped => Status == OrderStatus.Shipped;
        public bool CanBeCancelled => Status == OrderStatus.Pending || Status == OrderStatus.Confirmed;
        public bool CanBeReturned => Status == OrderStatus.Delivered;
        public string StatusDisplay => Status.ToString();
    }

    public class OrderItemDto : BaseDto
    {
        public int ProductId { get; set; }
        public ProductDto? Product { get; set; }
        public int? ProductVariantId { get; set; }
        public ProductVariantDto? ProductVariant { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal TotalPrice { get; set; }
        public DateTime OrderedAt { get; set; }
    }

    public class PaymentDto : BaseDto
    {
        public string CustomerId { get; set; } = string.Empty;
        public int PaymentMethodId { get; set; }
        public PaymentMethodDto? PaymentMethod { get; set; }
        public int PaymentProviderId { get; set; }
        public PaymentProviderDto? PaymentProvider { get; set; }
        public decimal Amount { get; set; }
        public DateTime PaymentDate { get; set; }
        public int OrderId { get; set; }
        public string Status { get; set; } = string.Empty;
    }

    public class PaymentMethodDto : BaseDto
    {
        public string Name { get; set; } = string.Empty;
    }

    public class PaymentProviderDto : BaseDto
    {
        public string Name { get; set; } = string.Empty;
        public string ApiEndpoint { get; set; } = string.Empty;
        public string? PublicKey { get; set; }
        public bool IsActive { get; set; }
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