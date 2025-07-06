using E_Commers.DtoModels.CategoryDtos;
using E_Commers.DtoModels.DiscoutDtos;
using E_Commers.DtoModels.Shared;
using E_Commers.Models;
using System.ComponentModel.DataAnnotations;
using E_Commers.DtoModels.InventoryDtos;
using E_Commers.DtoModels.ImagesDtos;
using E_Commers.DtoModels.Responses;
using E_Commers.DtoModels.ProductDtos;
using E_Commers.Enums;

namespace E_Commers.DtoModels.ProductDtos
{
	public class ProductDto:BaseDto
	{
		[RegularExpression(@"^[a-zA-Z0-9][a-zA-Z0-9\s\-,]*[a-zA-Z0-9]$", ErrorMessage = "Name must start and end with an alphanumeric character and can contain spaces, hyphens, and commas in between.")]
		public string Name { get; set; } = string.Empty;
		
		[RegularExpression(@"^[\w\s.,\-()'\""]{0,500}$", ErrorMessage = "Description can contain up to 500 characters: letters, numbers, spaces, and .,-()'\"")]
		public string Description { get; set; } = string.Empty;
		
		public int SubCategoryId { get; set; }
		public SubCategoryDto? SubCategory { get; set; }
		public DiscountDto? Discount { get; set; }
		public int AvailableQuantity { get; set; }
		public decimal MinPrice { get; set; }
		public decimal MaxPrice { get; set; }
		public decimal FinalPrice { get; set; }
		public Gender Gender { get; set; }
		public bool HasVariants { get; set; }
		public int TotalVariants { get; set; }

		public List<ProductVariantDto>? Variants { get; set; }
		public List<CollectionDto>? Collections { get; set; }
		public List<ReviewDto>? Reviews { get; set; }
		public List<ImageDto>? Images { get; set; }
		public List<InventoryDto>? Inventory { get; set; }
		public List<WishlistItemDto>? WishlistItems { get; set; }
		public List<ReturnRequestProductDto>? ReturnRequests { get; set; }
	}

	public class ProductVariantDto : BaseDto
	{
		public string Color { get; set; } = string.Empty;
		public string? Size { get; set; }
		public int? Waist { get; set; }
		public int? Length { get; set; }
		public int? FitType { get; set; }
		public int Quantity { get; set; }
		public decimal Price { get; set; }
		public int ProductId { get; set; }
	}

	public class CollectionDto : BaseDto
	{
		public string Name { get; set; } = string.Empty;
		public string Description { get; set; } = string.Empty;
		public int DisplayOrder { get; set; }
		public bool IsActive { get; set; }
	}

	public class ReviewDto : BaseDto
	{
		public int Rating { get; set; }
		public string Comment { get; set; } = string.Empty;
		public string UserId { get; set; } = string.Empty;
		public string UserName { get; set; } = string.Empty;
		public int ProductId { get; set; }
	}

	public class WishlistItemDto : BaseDto
	{
		public int ProductId { get; set; }
		public string UserId { get; set; } = string.Empty;
		public ProductDto? Product { get; set; }
	}

	public class ReturnRequestProductDto : BaseDto
	{
		public int ReturnRequestId { get; set; }
		public int ProductId { get; set; }
		public int Quantity { get; set; }
		public string Reason { get; set; } = string.Empty;
		public ReturnStatus Status { get; set; }
	}

	public enum ReturnStatus
	{
		Pending = 1,
		Approved = 2,
		Rejected = 3,
		Completed = 4
	}
}
