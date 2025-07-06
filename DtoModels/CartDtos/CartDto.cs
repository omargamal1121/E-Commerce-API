using E_Commers.DtoModels.ImagesDtos;
using E_Commers.DtoModels.ProductDtos;
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

  

   
   
   
} 