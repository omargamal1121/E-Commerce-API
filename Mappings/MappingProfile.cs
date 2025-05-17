using AutoMapper;
using E_Commers.DtoModels.AccountDtos;
using E_Commers.DtoModels.CategoryDtos;
using E_Commers.DtoModels.ProductDtos;
using E_Commers.Models;

namespace E_Commers.Mappings
{
	public class MappingProfile:Profile
	{
		public MappingProfile()
		{
			CreateMap<Product,ProductDto>().ForMember(c => c.FinalPrice, op => op.MapFrom(c => c.Discount==null?c.Price: c.Price - c.Discount.DiscountPercent * c.Price)).ReverseMap();
			CreateMap<Category, CategoryDto>().ReverseMap();
			CreateMap< CreateCategotyDto, CategoryDto>().ReverseMap();
			CreateMap<RegisterDto, Customer>().ReverseMap();
			CreateMap<RegisterDto, RegisterResponse>().ReverseMap();
		}
	}
}
