using AutoMapper;
using E_Commers.DtoModels.ProductDtos;
using E_Commers.DtoModels.Responses;
using E_Commers.Interfaces;
using E_Commers.UOW;
using Microsoft.EntityFrameworkCore;
using E_Commers.DtoModels.InventoryDtos;
using E_Commers.Models;
using E_Commers.ErrorHnadling;
using Microsoft.Extensions.Logging;
using E_Commers.Services.WareHouseServices;
using E_Commers.DtoModels.WareHouseDtos;

namespace E_Commers.Services.Product
{
	public interface IProductsServices
	{
		public Task<ApiResponse<List<ProductDto>>> GetAllAsync();
		public Task<ApiResponse<List<ProductDto>>> GetProductsByCategoryId(int categoryid);
		public Task<ApiResponse<ProductDto>> CreateProductAsync(CreateProductDto dto, string userId);
		public Task<ApiResponse<ProductDto>> UpdateProductAsync(int id, UpdateProductDto dto, string userId);
		public Task<ApiResponse<string>> DeleteProductAsync(int id, string userId);
		public Task<ApiResponse<ProductDto>> GetProductByIdAsync(int id);
		public Task<ApiResponse<List<InventoryDto>>> GetProductInventoryAsync(int productId);
		public Task<ApiResponse<ProductDto>> UpdateProductQuantityAsync(int productId, int quantity, string userId);
	}
	public class ProductsServices : IProductsServices
	{
		private readonly IUnitOfWork _unitOfWork;
		private readonly IMapper _mapper;
		private readonly ICategoryServices _categoryServices;
		private readonly IWareHouseServices _warehouseServices;
		private readonly IProductInventoryService _inventoryService;
		private readonly ILogger<ProductsServices> _logger;
		public ProductsServices(IUnitOfWork unitOfWork, IMapper mapper, IWareHouseServices warehouseServices, ICategoryServices categoryServices, IProductInventoryService inventoryService, ILogger<ProductsServices> logger)
		{
			_warehouseServices=warehouseServices;
			_categoryServices = categoryServices;
			_mapper = mapper;
			_unitOfWork = unitOfWork;
			_inventoryService = inventoryService;
			_logger = logger;
		}
		public async Task<ApiResponse<List<ProductDto>>> GetAllAsync()
		{
			var products = await _unitOfWork.Product.GetAllAsync();
			if (products.Data is null)
				return ApiResponse<List<ProductDto>>.CreateSuccessResponse("No Products Found", new List<ProductDto>());

			var productsdto = await products.Data.Where(p => p.DeletedAt == null).Select(p => _mapper.Map<ProductDto>(p)).ToListAsync();
			return ApiResponse<List<ProductDto>>.CreateSuccessResponse("All Products", productsdto);
		}
		public async Task<ApiResponse<List<ProductDto>>> GetProductsByCategoryId(int categoryid)
		{
			var isfound = await _categoryServices.IsExsistAsync(categoryid);
			if (isfound.Statuscode == 404)
				return ApiResponse<List<ProductDto>>.CreateErrorResponse(new ErrorResponse("Category Id", $"No Category with this id:{categoryid}"), 404);

			var products = await _unitOfWork.Product.GetAllAsync();
			if (products.Data is null || products.Data.Any())
				return ApiResponse<List<ProductDto>>.CreateSuccessResponse("No Products Found", new List<ProductDto>());

			var productsdto = await products.Data.Include(p => p.Discount).Where(p => p.DeletedAt == null && p.SubCategoryId == categoryid).Select(p => _mapper.Map<ProductDto>(p)).ToListAsync();
			return ApiResponse<List<ProductDto>>.CreateSuccessResponse("All Products", productsdto);
		}
		public async Task<ApiResponse<ProductDto>> CreateProductAsync(CreateProductDto dto, string userId)
		{
			_logger.LogInformation($"Creating new product: {dto.Name}");
	
			var categoryExists = await _categoryServices.IsExsistAsync(dto.CategoryId);
			if (categoryExists.Statuscode == 404)
				return ApiResponse<ProductDto>.CreateErrorResponse(
					new ErrorResponse("Category", "Category not found"), 404);


			var warehouseExists = await _warehouseServices.IsExsistAsync(dto.WarehouseId);
			if (warehouseExists.Statuscode == 404)
				return ApiResponse<ProductDto>.CreateErrorResponse(
					new ErrorResponse("Warehouse", "Warehouse not found"), 404);

			var transaction = await _unitOfWork.BeginTransactionAsync();
			var product = _mapper.Map<Models.Product>(dto);
			var result = await _unitOfWork.Product.CreateAsync(product);
			if (!result.Success){
				await transaction.RollbackAsync();
				return ApiResponse<ProductDto>.CreateErrorResponse(
					new ErrorResponse("Product", result.Message), 400);
			}

		
				var inventoryDto = new CreateInvetoryDto
				{
					ProductId = product.Id,
					WareHouseId = dto.WarehouseId,
					Quantity = dto.Quantity
				};

				var inventoryResult = await _inventoryService.CreateInventoryAsync(inventoryDto, userId);
				if (inventoryResult.Statuscode!=201)
				{

				await transaction.RollbackAsync();

				return ApiResponse<ProductDto>.CreateErrorResponse(
						new ErrorResponse("Inventory", inventoryResult.ResponseBody.Message??"Error While Create inverntory"), 400);
				}
			

			await _unitOfWork.CommitAsync();
			var productDto = _mapper.Map<ProductDto>(product);
			return ApiResponse<ProductDto>.CreateSuccessResponse("Product created successfully", productDto);
		}
		public async Task<ApiResponse<ProductDto>> UpdateProductAsync(int id, UpdateProductDto dto, string userId)
		{
			_logger.LogInformation($"Updating product: {id}");

			var product = await _unitOfWork.Product.GetByIdAsync(id);
			if (!product.Success || product.Data == null)
				return ApiResponse<ProductDto>.CreateErrorResponse(
					new ErrorResponse("Product", "Product not found"), 404);

			_mapper.Map(dto, product.Data);
			var result = await _unitOfWork.Product.UpdateAsync(product.Data);
			if (!result.Success)
				return ApiResponse<ProductDto>.CreateErrorResponse(
					new ErrorResponse("Product", result.Message), 400);

			await _unitOfWork.CommitAsync();
			var productDto = _mapper.Map<ProductDto>(product.Data);
			return ApiResponse<ProductDto>.CreateSuccessResponse("Product updated successfully", productDto);
		}
		public async Task<ApiResponse<string>> DeleteProductAsync(int id, string userId)
		{
			_logger.LogInformation($"Deleting product: {id}");

			var product = await _unitOfWork.Product.GetByIdAsync(id);
			if (!product.Success || product.Data == null)
				return ApiResponse<string>.CreateErrorResponse(
					new ErrorResponse("Product", "Product not found"), 404);

		
			var result = await _unitOfWork.Product.RemoveAsync(product.Data);
			if (!result.Success)
				return ApiResponse<string>.CreateErrorResponse(
					new ErrorResponse("Product", result.Message), 400);

			await _unitOfWork.CommitAsync();
			return ApiResponse<string>.CreateSuccessResponse("Product deleted successfully", "Product deleted");
		}
		public async Task<ApiResponse<ProductDto>> GetProductByIdAsync(int id)
		{
			var product = await _unitOfWork.Product.GetByIdAsync(id);
			if (!product.Success || product.Data == null)
				return ApiResponse<ProductDto>.CreateErrorResponse(
					new ErrorResponse("Product", "Product not found"), 404);

			var productDto = _mapper.Map<ProductDto>(product.Data);
			return ApiResponse<ProductDto>.CreateSuccessResponse("Product retrieved successfully", productDto);
		}
		public async Task<ApiResponse<List<InventoryDto>>> GetProductInventoryAsync(int productId)
		{
			return await _inventoryService.GetInventoryByProductIdAsync(productId);
		}
		public async Task<ApiResponse<ProductDto>> UpdateProductQuantityAsync(int productId, int quantity, string userId)
		{
			_logger.LogInformation($"Updating product quantity: {productId}");

			var product = await _unitOfWork.Product.GetByIdAsync(productId);
			if (!product.Success || product.Data == null)
				return ApiResponse<ProductDto>.CreateErrorResponse(
					new ErrorResponse("Product", "Product not found"), 404);

			var result = await _unitOfWork.Product.UpdateQuantityAsync(productId, quantity);
			if (!result.Success)
				return ApiResponse<ProductDto>.CreateErrorResponse(
					new ErrorResponse("Product", result.Message), 400);

			await _unitOfWork.CommitAsync();
			var productDto = _mapper.Map<ProductDto>(product.Data);
			return ApiResponse<ProductDto>.CreateSuccessResponse("Product quantity updated successfully", productDto);
		}
	}
}
