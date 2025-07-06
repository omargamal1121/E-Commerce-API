using E_Commers.DtoModels.ImagesDtos;
using E_Commers.DtoModels.ProductDtos;
using E_Commers.DtoModels.Shared;

namespace E_Commers.DtoModels.CollectionDtos
{
    public class CollectionDto : BaseDto
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int DisplayOrder { get; set; }
        public bool IsActive { get; set; }
        public List<ProductDto> Products { get; set; } = new List<ProductDto>();
        public List<ImageDto> Images { get; set; } = new List<ImageDto>();
        public ImageDto? MainImage { get; set; }
        public int TotalProducts { get; set; }
        public decimal MinPrice { get; set; }
        public decimal MaxPrice { get; set; }
        public decimal AveragePrice { get; set; }
    }

    public class CreateCollectionDto
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int DisplayOrder { get; set; }
        public bool IsActive { get; set; } = true;
        public List<int> ProductIds { get; set; } = new List<int>();
    }

    public class UpdateCollectionDto
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int DisplayOrder { get; set; }
        public bool IsActive { get; set; }
        public List<int> ProductIds { get; set; } = new List<int>();
    }

    public class AddProductsToCollectionDto
    {
        public List<int> ProductIds { get; set; } = new List<int>();
    }

    public class RemoveProductsFromCollectionDto
    {
        public List<int> ProductIds { get; set; } = new List<int>();
    }

    public class CollectionSummaryDto : BaseDto
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int DisplayOrder { get; set; }
        public bool IsActive { get; set; }
        public ImageDto? MainImage { get; set; }
        public int TotalProducts { get; set; }
        public decimal MinPrice { get; set; }
        public decimal MaxPrice { get; set; }
    }
} 