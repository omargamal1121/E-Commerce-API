using E_Commers.Enums;
using System.ComponentModel.DataAnnotations;

namespace E_Commers.DtoModels.OrderDtos
{
    public class CreateOrderDto
    {
        [Required(ErrorMessage = "Payment method ID is required")]
        [Range(1, int.MaxValue, ErrorMessage = "Payment method ID must be greater than 0")]
        public int PaymentMethodId { get; set; }

        [Required(ErrorMessage = "Payment provider ID is required")]
        [Range(1, int.MaxValue, ErrorMessage = "Payment provider ID must be greater than 0")]
        public int PaymentProviderId { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "Tax amount cannot be negative")]
        public decimal TaxAmount { get; set; } = 0;

        [Range(0, double.MaxValue, ErrorMessage = "Shipping cost cannot be negative")]
        public decimal ShippingCost { get; set; } = 0;

        [Range(0, double.MaxValue, ErrorMessage = "Discount amount cannot be negative")]
        public decimal DiscountAmount { get; set; } = 0;

        [StringLength(500, ErrorMessage = "Notes cannot exceed 500 characters")]
        public string? Notes { get; set; }
    }

    public class UpdateOrderStatusDto
    {
        [Required(ErrorMessage = "Order status is required")]
        public OrderStatus Status { get; set; }

        [StringLength(500, ErrorMessage = "Notes cannot exceed 500 characters")]
        public string? Notes { get; set; }
    }

    public class CancelOrderDto
    {
        [Required(ErrorMessage = "Cancellation reason is required")]
        [StringLength(500, MinimumLength = 10, ErrorMessage = "Cancellation reason must be between 10 and 500 characters")]
        public string CancellationReason { get; set; } = string.Empty;
    }
} 