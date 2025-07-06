using AutoMapper;
using E_Commers.DtoModels.CategoryDtos;
using E_Commers.DtoModels.DiscoutDtos;
using E_Commers.DtoModels.ImagesDtos;
using E_Commers.DtoModels.InventoryDtos;
using E_Commers.DtoModels.ProductDtos;
using E_Commers.DtoModels.Responses;
using E_Commers.DtoModels.WareHouseDtos;
using E_Commers.Enums;
using E_Commers.ErrorHnadling;
using E_Commers.Interfaces;
using E_Commers.Models;
using E_Commers.Services.AdminOpreationServices;
using E_Commers.Services.EmailServices;
using E_Commers.Services.WareHouseServices;
using E_Commers.UOW;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.IO;

namespace E_Commers.Services.Product
{
	public interface IProductsServices
	{
		// Core product operations (delegated to ProductCatalogService)
		Task<Result<List<ProductDto>>> GetAllAsync();
		Task<Result<ProductDto>> GetProductByIdAsync(int id);
		Task<Result<ProductDto>> CreateProductAsync(CreateProductDto dto, string userId);
		Task<Result<ProductDto>> UpdateProductAsync(int id, UpdateProductDto dto, string userId);
		Task<Result<string>> DeleteProductAsync(int id, string userId);
		Task<Result<ProductDto>> RestoreProductAsync(int id, string userId);
		Task<Result<List<ProductDto>>> GetProductsByCategoryId(int categoryId);
		Task<Result<List<ProductDto>>> FilterAsync(string? search, bool? isActive, bool includeDeleted, int page, int pageSize, string role);

		// Search operations (delegated to ProductSearchService)
		Task<Result<List<ProductDto>>> SearchProductsAsync(string searchTerm, int page, int pageSize);
		Task<Result<List<ProductDto>>> FilterByPriceRangeAsync(decimal minPrice, decimal maxPrice, int page, int pageSize);
		Task<Result<List<ProductDto>>> FilterByGenderAsync(Gender gender, int page, int pageSize);
		Task<Result<List<ProductDto>>> GetProductsOnSaleAsync(int page, int pageSize);
		Task<Result<List<ProductDto>>> GetNewArrivalsAsync(int page, int pageSize);
		Task<Result<List<ProductDto>>> GetBestSellersAsync(int page, int pageSize);
		Task<Result<List<ProductDto>>> AdvancedSearchAsync(AdvancedSearchDto searchCriteria, int page, int pageSize);

		// Image operations (delegated to ProductImageService)
		Task<Result<List<ImageDto>>> GetProductImagesAsync(int productId);
		Task<Result<ImageDto>> AddProductImageAsync(int productId, CreateImageDto dto, string userId);
		Task<Result<string>> RemoveProductImageAsync(int productId, int imageId, string userId);
		Task<Result<string>> SetMainImageAsync(int productId, int imageId, string userId);

		// Variant operations (delegated to ProductVariantService)
		Task<Result<List<ProductVariantDto>>> GetProductVariantsAsync(int productId);
		Task<Result<ProductVariantDto>> AddVariantAsync(int productId, CreateProductVariantDto dto, string userId);
		Task<Result<ProductVariantDto>> UpdateVariantAsync(int variantId, UpdateProductVariantDto dto, string userId);
		Task<Result<string>> DeleteVariantAsync(int variantId, string userId);
		Task<Result<string>> UpdateVariantPriceAsync(int variantId, decimal newPrice, string userId);
		Task<Result<string>> UpdateVariantQuantityAsync(int variantId, int newQuantity, string userId);

		// Discount operations (delegated to ProductDiscountService)
		Task<Result<DiscountDto>> GetProductDiscountAsync(int productId);
		Task<Result<DiscountDto>> AddDiscountToProductAsync(int productId, CreateDiscountDto dto, string userId);
		Task<Result<DiscountDto>> UpdateProductDiscountAsync(int productId, UpdateDiscountDto dto, string userId);
		Task<Result<string>> RemoveDiscountFromProductAsync(int productId, string userId);
		Task<Result<decimal>> CalculateDiscountedPriceAsync(int productId, int variantId);

		// Inventory operations (delegated to ProductInventoryService)
		Task<Result<InventoryDto>> CreateInventoryAsync(CreateInvetoryDto dto, string userId);
		Task<Result<InventoryDto>> UpdateInventoryQuantityAsync(UpdateInventoryQuantityDto dto, string userId);
		Task<Result<List<InventoryDto>>> GetInventoryByProductIdAsync(int productId);
		Task<Result<List<InventoryDto>>> GetLowStockAlertsAsync(int threshold = 10);
	}

	public class ProductsServices : IProductsServices
	{
		private readonly IProductCatalogService _productCatalogService;
		private readonly IProductSearchService _productSearchService;
		private readonly IProductImageService _productImageService;
		private readonly IProductVariantService _productVariantService;
		private readonly IProductDiscountService _productDiscountService;
		private readonly IProductInventoryService _productInventoryService;
		private readonly ILogger<ProductsServices> _logger;

		public ProductsServices(
			IProductCatalogService productCatalogService,
			IProductSearchService productSearchService,
			IProductImageService productImageService,
			IProductVariantService productVariantService,
			IProductDiscountService productDiscountService,
			IProductInventoryService productInventoryService,
			ILogger<ProductsServices> logger)
		{
			_productCatalogService = productCatalogService;
			_productSearchService = productSearchService;
			_productImageService = productImageService;
			_productVariantService = productVariantService;
			_productDiscountService = productDiscountService;
			_productInventoryService = productInventoryService;
			_logger = logger;
		}

		#region Core Product Operations (Delegated to ProductCatalogService)

		public async Task<Result<List<ProductDto>>> GetAllAsync()
		{
			return await _productCatalogService.GetAllAsync();
		}

		public async Task<Result<ProductDto>> GetProductByIdAsync(int id)
		{
			return await _productCatalogService.GetProductByIdAsync(id);
		}

		public async Task<Result<ProductDto>> CreateProductAsync(CreateProductDto dto, string userId)
		{
			return await _productCatalogService.CreateProductAsync(dto, userId);
		}

		public async Task<Result<ProductDto>> UpdateProductAsync(int id, UpdateProductDto dto, string userId)
		{
			return await _productCatalogService.UpdateProductAsync(id, dto, userId);
		}

		public async Task<Result<string>> DeleteProductAsync(int id, string userId)
		{
			return await _productCatalogService.DeleteProductAsync(id, userId);
		}

		public async Task<Result<ProductDto>> RestoreProductAsync(int id, string userId)
		{
			return await _productCatalogService.RestoreProductAsync(id, userId);
		}

		public async Task<Result<List<ProductDto>>> GetProductsByCategoryId(int categoryId)
		{
			return await _productCatalogService.GetProductsByCategoryId(categoryId);
		}

		public async Task<Result<List<ProductDto>>> FilterAsync(string? search, bool? isActive, bool includeDeleted, int page, int pageSize, string role)
		{
			return await _productCatalogService.FilterAsync(search, isActive, includeDeleted, page, pageSize, role);
		}

		#endregion

		#region Search Operations (Delegated to ProductSearchService)

		public async Task<Result<List<ProductDto>>> SearchProductsAsync(string searchTerm, int page, int pageSize)
		{
			return await _productSearchService.SearchProductsAsync(searchTerm, page, pageSize);
		}

		public async Task<Result<List<ProductDto>>> FilterByPriceRangeAsync(decimal minPrice, decimal maxPrice, int page, int pageSize)
		{
			return await _productSearchService.FilterByPriceRangeAsync(minPrice, maxPrice, page, pageSize);
		}

		public async Task<Result<List<ProductDto>>> FilterByGenderAsync(Gender gender, int page, int pageSize)
		{
			return await _productSearchService.FilterByGenderAsync(gender, page, pageSize);
		}

		public async Task<Result<List<ProductDto>>> GetProductsOnSaleAsync(int page, int pageSize)
		{
			return await _productSearchService.GetProductsOnSaleAsync(page, pageSize);
		}

		public async Task<Result<List<ProductDto>>> GetNewArrivalsAsync(int page, int pageSize)
		{
			return await _productSearchService.GetNewArrivalsAsync(page, pageSize);
		}

		public async Task<Result<List<ProductDto>>> GetBestSellersAsync(int page, int pageSize)
		{
			return await _productSearchService.GetBestSellersAsync(page, pageSize);
		}

		public async Task<Result<List<ProductDto>>> AdvancedSearchAsync(AdvancedSearchDto searchCriteria, int page, int pageSize)
		{
			return await _productSearchService.AdvancedSearchAsync(searchCriteria, page, pageSize);
		}

		#endregion

		#region Image Operations (Delegated to ProductImageService)

		public async Task<Result<List<ImageDto>>> GetProductImagesAsync(int productId)
		{
			return await _productImageService.GetProductImagesAsync(productId);
		}

		public async Task<Result<ImageDto>> AddProductImageAsync(int productId, CreateImageDto dto, string userId)
		{
			return await _productImageService.AddProductImageAsync(productId, dto, userId);
		}

		public async Task<Result<string>> RemoveProductImageAsync(int productId, int imageId, string userId)
		{
			return await _productImageService.RemoveProductImageAsync(productId, imageId, userId);
		}

		public async Task<Result<string>> SetMainImageAsync(int productId, int imageId, string userId)
		{
			return await _productImageService.SetMainImageAsync(productId, imageId, userId);
		}

		#endregion

		#region Variant Operations (Delegated to ProductVariantService)

		public async Task<Result<List<ProductVariantDto>>> GetProductVariantsAsync(int productId)
		{
			return await _productVariantService.GetProductVariantsAsync(productId);
		}

		public async Task<Result<ProductVariantDto>> AddVariantAsync(int productId, CreateProductVariantDto dto, string userId)
		{
			return await _productVariantService.AddVariantAsync(productId, dto, userId);
		}

		public async Task<Result<ProductVariantDto>> UpdateVariantAsync(int variantId, UpdateProductVariantDto dto, string userId)
		{
			return await _productVariantService.UpdateVariantAsync(variantId, dto, userId);
		}

		public async Task<Result<string>> DeleteVariantAsync(int variantId, string userId)
		{
			return await _productVariantService.DeleteVariantAsync(variantId, userId);
		}

		public async Task<Result<string>> UpdateVariantPriceAsync(int variantId, decimal newPrice, string userId)
		{
			return await _productVariantService.UpdateVariantPriceAsync(variantId, newPrice, userId);
		}

		public async Task<Result<string>> UpdateVariantQuantityAsync(int variantId, int newQuantity, string userId)
		{
			return await _productVariantService.UpdateVariantQuantityAsync(variantId, newQuantity, userId);
		}

		#endregion

		#region Discount Operations (Delegated to ProductDiscountService)

		public async Task<Result<DiscountDto>> GetProductDiscountAsync(int productId)
		{
			return await _productDiscountService.GetProductDiscountAsync(productId);
		}

		public async Task<Result<DiscountDto>> AddDiscountToProductAsync(int productId, CreateDiscountDto dto, string userId)
		{
			return await _productDiscountService.AddDiscountToProductAsync(productId, dto, userId);
		}

		public async Task<Result<DiscountDto>> UpdateProductDiscountAsync(int productId, UpdateDiscountDto dto, string userId)
		{
			return await _productDiscountService.UpdateProductDiscountAsync(productId, dto, userId);
		}

		public async Task<Result<string>> RemoveDiscountFromProductAsync(int productId, string userId)
		{
			return await _productDiscountService.RemoveDiscountFromProductAsync(productId, userId);
		}

		public async Task<Result<decimal>> CalculateDiscountedPriceAsync(int productId, int variantId)
		{
			return await _productDiscountService.CalculateDiscountedPriceAsync(productId, variantId);
		}

		#endregion

		#region Inventory Operations (Delegated to ProductInventoryService)

		public async Task<Result<InventoryDto>> CreateInventoryAsync(CreateInvetoryDto dto, string userId)
		{
			return await _productInventoryService.CreateInventoryAsync(dto, userId);
		}

		public async Task<Result<InventoryDto>> UpdateInventoryQuantityAsync(UpdateInventoryQuantityDto dto, string userId)
		{
			return await _productInventoryService.UpdateInventoryQuantityAsync(dto, userId);
		}

		public async Task<Result<List<InventoryDto>>> GetInventoryByProductIdAsync(int productId)
		{
			return await _productInventoryService.GetInventoryByProductIdAsync(productId);
		}

		public async Task<Result<List<InventoryDto>>> GetLowStockAlertsAsync(int threshold = 10)
		{
			return await _productInventoryService.GetLowStockAlertsAsync(threshold);
		}

		#endregion
	}
}
