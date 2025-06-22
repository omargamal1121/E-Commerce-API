using AutoMapper;
using E_Commers.DtoModels.AccountDtos;
using E_Commers.DtoModels.CategoryDtos;
using E_Commers.DtoModels.InventoryDtos;
using E_Commers.DtoModels.ProductDtos;
using E_Commers.DtoModels.WareHouseDtos;
using E_Commers.Models;

namespace E_Commers.Mappings
{
	public class MappingProfile:Profile
	{
		public MappingProfile()
		{
			//CreateMap<Product,ProductDto>().ForMember(c => c.FinalPrice, op => op.MapFrom(c => c.Discount==null?c.Price: c.Price - c.Discount.DiscountPercent * c.Price)).ForMember(p=>p.AvailabeQuantity,op=>op.MapFrom(p=>p.InventoryEntries.Sum(x=>x.Quantity))).ReverseMap();
			CreateMap<Category, CategoryDto>().ReverseMap();
			CreateMap< CreateCategotyDto, CategoryDto>().ReverseMap();
			CreateMap<RegisterDto, Customer>().ReverseMap();
			CreateMap<RegisterDto, RegisterResponse>().ReverseMap();
			CreateMap<WareHouseDto,Warehouse>().ReverseMap();
			CreateMap<Customer, RegisterResponse>()
			.ForMember(dest => dest.UserId, opt => opt.MapFrom(src => src.Id.ToString()))
			.ReverseMap()
			.ForMember(dest => dest.Id, opt => opt.MapFrom(src =>src.UserId));

			// Inventory mappings
			CreateMap<ProductInventory, InventoryDto>()
				.ForMember(dest => dest.Quantityinsidewarehouse, opt => opt.MapFrom(src => src.Quantity))
				.ForMember(dest => dest.WareHousid, opt => opt.MapFrom(src => src.WarehouseId))
				.ForMember(dest => dest.Product, opt => opt.MapFrom(src => src.Product));
		}
	}
}
