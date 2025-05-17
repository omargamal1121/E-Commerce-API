using AutoMapper;
using E_Commers.DtoModels.ProductDtos;
using E_Commers.DtoModels.Responses;
using E_Commers.Interfaces;
using E_Commers.UOW;
using Microsoft.EntityFrameworkCore;

namespace E_Commers.Services.Product
{
	public interface IProductsServices
	{
		public  Task<ApiResponse<List<ProductDto>>> GetAllAsync();
		public Task<ApiResponse<List<ProductDto>>> GetProductsByCategoryId(int categoryid);

	}
	public class ProductsServices:IProductsServices
	{
		private readonly IUnitOfWork _unitOfWork;
		private readonly IMapper _mapper;
		private readonly ICategoryServices _categoryServices;
		public ProductsServices(IUnitOfWork unitOfWork, IMapper mapper, ICategoryServices categoryServices)
		{
			_categoryServices = categoryServices;
			_mapper = mapper;
			_unitOfWork = unitOfWork;
		}
		public async Task<ApiResponse<List<ProductDto>>> GetAllAsync()
		{
			var products= await _unitOfWork.Product.GetAllAsync();
			if(products.Data is null|| products.Data.Any())
				return  ApiResponse<List<ProductDto>>.CreateSuccessResponse("No Products Found",new List<ProductDto>());

			var productsdto = await products.Data.Where(p=>p.DeletedAt==null).Select(p => _mapper.Map<ProductDto>(p)).ToListAsync();
			return ApiResponse<List<ProductDto>>.CreateSuccessResponse("All Products",productsdto);
		}public async Task<ApiResponse<List<ProductDto>>> GetProductsByCategoryId(int categoryid)
		{
			var isfound = await _categoryServices.IsExsistAsync(categoryid);
			if (isfound.Statuscode == 404)
				return ApiResponse<List<ProductDto>>.CreateErrorResponse(new ErrorHnadling.ErrorResponse("Category Id", $"No Category with this id:{categoryid}"), 404);

			var products= await _unitOfWork.Product.GetAllAsync();
			if(products.Data is null|| products.Data.Any())
				return  ApiResponse<List<ProductDto>>.CreateSuccessResponse("No Products Found",new List<ProductDto>());

			var productsdto = await products.Data.Include(p => p.Discount).Where(p=>p.DeletedAt==null&&p.CategoryId==categoryid).Select(p => _mapper.Map<ProductDto>(p)).ToListAsync();
			return ApiResponse<List<ProductDto>>.CreateSuccessResponse("All Products",productsdto);
		}
	}
}
