using AutoMapper;
using E_Commers.DtoModels.AccountDtos;
using E_Commers.DtoModels.CartDtos;
using E_Commers.DtoModels.CategoryDtos;
using E_Commers.DtoModels.DiscoutDtos;
using E_Commers.DtoModels.CollectionDtos;
using E_Commers.DtoModels.ImagesDtos;
using E_Commers.DtoModels.InventoryDtos;
using E_Commers.DtoModels.OrderDtos;
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
			CreateMap<Category, CategoryDto>()
				.ForMember(dest => dest.images, opt => opt.Ignore()) // Ignore automatic mapping
				.ForMember(dest => dest.MainImageUrl, opt => opt.Ignore()) // Ignore automatic mapping
				.ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Name))
				.ForMember(dest => dest.Description, opt => opt.MapFrom(src => src.Description))
				.ForMember(dest => dest.DisplayOrder, opt => opt.MapFrom(src => src.DisplayOrder))
				.ForMember(dest => dest.IsActive, opt => opt.MapFrom(src => src.IsActive))
				.ForMember(dest => dest.DeletedAt, opt => opt.MapFrom(src => src.DeletedAt))
				.ForMember(dest => dest.ModifiedAt, opt => opt.MapFrom(src => src.ModifiedAt))
				.AfterMap((src, dest) =>
				{
					
					if (src.Images == null)
					{
						dest.images = new List<ImageDto>();
						dest.MainImageUrl = null;
						return;
					}

					var main = src.Images.FirstOrDefault(i => i.IsMain);
					if (main != null)
					{
						dest.MainImageUrl = new ImageDto
						{
							Id = main.Id,
							Url = main.Url
						};
					}
					else
					{
						dest.MainImageUrl = null;
					}

					dest.images = src.Images
						.Where(i => !i.IsMain&&i.DeletedAt==null)
						.Select(i => new ImageDto
						{
							Id = i.Id,
							Url = i.Url
						})
						.ToList();
				})
				.ReverseMap();


			CreateMap<CreateCategotyDto, Category>()
				.ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Name.Trim()))
				.ForMember(dest => dest.Description, opt => opt.MapFrom(src => src.Description.Trim()))
				.ForMember(dest => dest.DisplayOrder, opt => opt.MapFrom(src => src.DisplayOrder))
				.ForMember(dest => dest.Images, opt => opt.Ignore()) 
				.ForMember(dest => dest.Id, opt => opt.Ignore()) 
				.ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
				.ForMember(dest => dest.ModifiedAt, opt => opt.Ignore()) 
				.ForMember(dest => dest.DeletedAt, opt => opt.Ignore()) 
				.ForMember(dest => dest.SubCategories, opt => opt.Ignore());
			CreateMap<UpdateCategoryDto, Category>()
				.ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Name.Trim()))
				.ForMember(dest => dest.Description, opt => opt.MapFrom(src => src.Description.Trim()))
				.ForMember(dest => dest.Id, opt => opt.Ignore()) // Don't update ID
				.ForMember(dest => dest.CreatedAt, opt => opt.Ignore()) // Don't update creation date
				.ForMember(dest => dest.ModifiedAt, opt => opt.Ignore()) // Will be set by BaseEntity
				.ForMember(dest => dest.DeletedAt, opt => opt.Ignore()) // Don't update deletion date
				.ForMember(dest => dest.Images, opt => opt.Ignore()) // Don't update images
				.ForMember(dest => dest.SubCategories, opt => opt.Ignore()); // Don't update subcategories
	
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

			// Product mappings
			CreateMap<CreateProductDto, Product>()
				.ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Name))
				.ForMember(dest => dest.Description, opt => opt.MapFrom(src => src.Description))
				.ForMember(dest => dest.Quantity, opt => opt.MapFrom(src => src.Quantity))
				.ForMember(dest => dest.SubCategoryId, opt => opt.MapFrom(src => src.CategoryId));

			CreateMap<UpdateProductDto, Product>()
				.ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Name))
				.ForMember(dest => dest.Description, opt => opt.MapFrom(src => src.Description))
				.ForMember(dest => dest.Quantity, opt => opt.MapFrom(src => src.Quantity))
				.ForMember(dest => dest.SubCategoryId, opt => opt.MapFrom(src => src.CategoryId));

			CreateMap<Product, ProductDto>()
				.ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Name))
				.ForMember(dest => dest.Description, opt => opt.MapFrom(src => src.Description))
				.ForMember(dest => dest.AvailableQuantity, opt => opt.MapFrom(src => src.Quantity))
				.ForMember(dest => dest.MinPrice, opt => opt.MapFrom(src => CalculateMinPrice(src)))
				.ForMember(dest => dest.MaxPrice, opt => opt.MapFrom(src => CalculateMaxPrice(src)))
				.ForMember(dest => dest.FinalPrice, opt => opt.MapFrom(src => CalculateFinalPrice(src)))
				.ForMember(dest => dest.HasVariants, opt => opt.MapFrom(src => src.ProductVariants.Any()))
				.ForMember(dest => dest.TotalVariants, opt => opt.MapFrom(src => src.ProductVariants.Count))
				.ForMember(dest => dest.SubCategory, opt => opt.MapFrom(src => src.SubCategory))
				.ForMember(dest => dest.Discount, opt => opt.MapFrom(src => src.Discount))
				.ForMember(dest => dest.Variants, opt => opt.MapFrom(src => src.ProductVariants))
				.ForMember(dest => dest.Collections, opt => opt.MapFrom(src => src.ProductCollections))
				.ForMember(dest => dest.Reviews, opt => opt.MapFrom(src => src.Reviews))
				.ForMember(dest => dest.Images, opt => opt.MapFrom(src => src.Images))
				.ForMember(dest => dest.Inventory, opt => opt.MapFrom(src => src.InventoryEntries))
				.ForMember(dest => dest.WishlistItems, opt => opt.MapFrom(src => src.WishlistItems))
				.ForMember(dest => dest.ReturnRequests, opt => opt.MapFrom(src => src.ReturnRequestProducts));
			CreateMap<CreateProductVariantDto, ProductVariant>();
			CreateMap<UpdateProductVariantDto, ProductVariant>();
			CreateMap<ProductVariant, ProductVariantDto>();

			// Cart mappings
			CreateMap<Cart, CartDto>()
				.ForMember(dest => dest.UserId, opt => opt.MapFrom(src => src.UserId))
				.ForMember(dest => dest.Customer, opt => opt.MapFrom(src => src.Customer))
				.ForMember(dest => dest.Items, opt => opt.MapFrom(src => src.Items))
				.ForMember(dest => dest.TotalPrice, opt => opt.MapFrom(src => src.TotalPrice))
				.ForMember(dest => dest.TotalItems, opt => opt.MapFrom(src => src.TotalItems))
				.ForMember(dest => dest.IsEmpty, opt => opt.MapFrom(src => src.IsEmpty));

			CreateMap<CartItem, CartItemDto>()
				.ForMember(dest => dest.ProductId, opt => opt.MapFrom(src => src.ProductId))
				.ForMember(dest => dest.Product, opt => opt.MapFrom(src => src.Product))
				.ForMember(dest => dest.Quantity, opt => opt.MapFrom(src => src.Quantity))
				.ForMember(dest => dest.UnitPrice, opt => opt.MapFrom(src => CalculateUnitPrice(src)))
				.ForMember(dest => dest.TotalPrice, opt => opt.MapFrom(src => CalculateCartItemTotalPrice(src)))
				.ForMember(dest => dest.AddedAt, opt => opt.MapFrom(src => src.AddedAt));

			CreateMap<Customer, CustomerDto>()
				.ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Name))
				.ForMember(dest => dest.Email, opt => opt.MapFrom(src => src.Email))
				.ForMember(dest => dest.ProfilePicture, opt => opt.MapFrom(src => src.ProfilePicture));

			// Order mappings
			CreateMap<Order, OrderDto>()
				.ForMember(dest => dest.OrderNumber, opt => opt.MapFrom(src => src.OrderNumber))
				.ForMember(dest => dest.CustomerId, opt => opt.MapFrom(src => src.CustomerId))
				.ForMember(dest => dest.Customer, opt => opt.MapFrom(src => src.Customer))
				.ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status))
				.ForMember(dest => dest.Subtotal, opt => opt.MapFrom(src => src.Subtotal))
				.ForMember(dest => dest.TaxAmount, opt => opt.MapFrom(src => src.TaxAmount))
				.ForMember(dest => dest.ShippingCost, opt => opt.MapFrom(src => src.ShippingCost))
				.ForMember(dest => dest.DiscountAmount, opt => opt.MapFrom(src => src.DiscountAmount))
				.ForMember(dest => dest.Total, opt => opt.MapFrom(src => src.Total))
				.ForMember(dest => dest.Notes, opt => opt.MapFrom(src => src.Notes))
				.ForMember(dest => dest.ShippedAt, opt => opt.MapFrom(src => src.ShippedAt))
				.ForMember(dest => dest.DeliveredAt, opt => opt.MapFrom(src => src.DeliveredAt))
				.ForMember(dest => dest.CancelledAt, opt => opt.MapFrom(src => src.CancelledAt))
				.ForMember(dest => dest.Items, opt => opt.MapFrom(src => src.Items))
				.ForMember(dest => dest.Payment, opt => opt.MapFrom(src => src.Payment));

			CreateMap<OrderItem, OrderItemDto>()
				.ForMember(dest => dest.ProductId, opt => opt.MapFrom(src => src.ProductId))
				.ForMember(dest => dest.Product, opt => opt.MapFrom(src => src.Product))
				.ForMember(dest => dest.ProductVariantId, opt => opt.MapFrom(src => src.ProductVariantId))
				.ForMember(dest => dest.ProductVariant, opt => opt.MapFrom(src => src.ProductVariant))
				.ForMember(dest => dest.Quantity, opt => opt.MapFrom(src => src.Quantity))
				.ForMember(dest => dest.UnitPrice, opt => opt.MapFrom(src => src.UnitPrice))
				.ForMember(dest => dest.TotalPrice, opt => opt.MapFrom(src => src.TotalPrice))
				.ForMember(dest => dest.OrderedAt, opt => opt.MapFrom(src => src.OrderedAt));

			CreateMap<Payment, PaymentDto>()
				.ForMember(dest => dest.CustomerId, opt => opt.MapFrom(src => src.CustomerId))
				.ForMember(dest => dest.PaymentMethodId, opt => opt.MapFrom(src => src.PaymentMethodId))
				.ForMember(dest => dest.PaymentMethod, opt => opt.MapFrom(src => src.PaymentMethod))
				.ForMember(dest => dest.PaymentProviderId, opt => opt.MapFrom(src => src.PaymentProviderId))
				.ForMember(dest => dest.PaymentProvider, opt => opt.MapFrom(src => src.PaymentProvider))
				.ForMember(dest => dest.Amount, opt => opt.MapFrom(src => src.Amount))
				.ForMember(dest => dest.PaymentDate, opt => opt.MapFrom(src => src.PaymentDate))
				.ForMember(dest => dest.OrderId, opt => opt.MapFrom(src => src.OrderId))
				.ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status));

			CreateMap<PaymentMethod, PaymentMethodDto>();
			CreateMap<PaymentProvider, PaymentProviderDto>();

			// Collection mappings
			CreateMap<Collection, CollectionDto>()
				.ForMember(dest => dest.MainImage, opt => opt.Ignore())
				.ForMember(dest => dest.Images, opt => opt.Ignore())
				.ForMember(dest => dest.Products, opt => opt.MapFrom(src => src.ProductCollections.Select(pc => pc.Product)))
				.ForMember(dest => dest.TotalProducts, opt => opt.MapFrom(src => src.ProductCollections.Count))
				.AfterMap((src, dest) =>
				{
					if (src.Images == null)
					{
						dest.Images = new List<ImageDto>();
						dest.MainImage = null;
						return;
					}

					var main = src.Images.FirstOrDefault(i => i.IsMain && i.DeletedAt == null);
					if (main != null)
					{
						dest.MainImage = new ImageDto
						{
							Id = main.Id,
							Url = main.Url,
							IsMain = main.IsMain
						};
					}
					else
					{
						dest.MainImage = null;
					}

					dest.Images = src.Images
						.Where(i => i.DeletedAt == null)
						.Select(i => new ImageDto
						{
							Id = i.Id,
							Url = i.Url,
							IsMain = i.IsMain
						})
						.ToList();
				});

			CreateMap<CreateCollectionDto, Collection>()
				.ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Name.Trim()))
				.ForMember(dest => dest.Description, opt => opt.MapFrom(src => src.Description?.Trim()))
				.ForMember(dest => dest.DisplayOrder, opt => opt.MapFrom(src => src.DisplayOrder))
				.ForMember(dest => dest.IsActive, opt => opt.MapFrom(src => src.IsActive))
				.ForMember(dest => dest.Id, opt => opt.Ignore())
				.ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
				.ForMember(dest => dest.ModifiedAt, opt => opt.Ignore())
				.ForMember(dest => dest.DeletedAt, opt => opt.Ignore())
				.ForMember(dest => dest.ProductCollections, opt => opt.Ignore())
				.ForMember(dest => dest.Images, opt => opt.Ignore());

			CreateMap<UpdateCollectionDto, Collection>()
				.ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Name.Trim()))
				.ForMember(dest => dest.Description, opt => opt.MapFrom(src => src.Description?.Trim()))
				.ForMember(dest => dest.DisplayOrder, opt => opt.MapFrom(src => src.DisplayOrder))
				.ForMember(dest => dest.IsActive, opt => opt.MapFrom(src => src.IsActive))
				.ForMember(dest => dest.Id, opt => opt.Ignore())
				.ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
				.ForMember(dest => dest.ModifiedAt, opt => opt.Ignore())
				.ForMember(dest => dest.DeletedAt, opt => opt.Ignore())
				.ForMember(dest => dest.ProductCollections, opt => opt.Ignore())
				.ForMember(dest => dest.Images, opt => opt.Ignore());

			CreateMap<Collection, CollectionSummaryDto>()
				.ForMember(dest => dest.MainImage, opt => opt.Ignore())
				.ForMember(dest => dest.TotalProducts, opt => opt.MapFrom(src => src.ProductCollections.Count))
				.AfterMap((src, dest) =>
				{
					if (src.Images == null)
					{
						dest.MainImage = null;
						return;
					}

					var main = src.Images.FirstOrDefault(i => i.IsMain && i.DeletedAt == null);
					if (main != null)
					{
						dest.MainImage = new ImageDto
						{
							Id = main.Id,
							Url = main.Url,
							IsMain = main.IsMain
						};
					}
					else
					{
						dest.MainImage = null;
					}
				});

			// CustomerAddress mappings
			CreateMap<CustomerAddress, CustomerAddressDto>()
				.ForMember(dest => dest.CustomerId, opt => opt.MapFrom(src => src.CustomerId))
				.ForMember(dest => dest.FirstName, opt => opt.MapFrom(src => src.FirstName))
				.ForMember(dest => dest.LastName, opt => opt.MapFrom(src => src.LastName))
				.ForMember(dest => dest.PhoneNumber, opt => opt.MapFrom(src => src.PhoneNumber))
				.ForMember(dest => dest.Country, opt => opt.MapFrom(src => src.Country))
				.ForMember(dest => dest.State, opt => opt.MapFrom(src => src.State))
				.ForMember(dest => dest.City, opt => opt.MapFrom(src => src.City))
				.ForMember(dest => dest.StreetAddress, opt => opt.MapFrom(src => src.StreetAddress))
				.ForMember(dest => dest.ApartmentSuite, opt => opt.MapFrom(src => src.ApartmentSuite))
				.ForMember(dest => dest.PostalCode, opt => opt.MapFrom(src => src.PostalCode))
				.ForMember(dest => dest.AddressType, opt => opt.MapFrom(src => src.AddressType))
				.ForMember(dest => dest.IsDefault, opt => opt.MapFrom(src => src.IsDefault))
				.ForMember(dest => dest.AdditionalNotes, opt => opt.MapFrom(src => src.AdditionalNotes))
				.ForMember(dest => dest.FullName, opt => opt.MapFrom(src => src.FullName))
				.ForMember(dest => dest.FullAddress, opt => opt.MapFrom(src => src.FullAddress));

			CreateMap<CreateCustomerAddressDto, CustomerAddress>()
				.ForMember(dest => dest.FirstName, opt => opt.MapFrom(src => src.FirstName.Trim()))
				.ForMember(dest => dest.LastName, opt => opt.MapFrom(src => src.LastName.Trim()))
				.ForMember(dest => dest.PhoneNumber, opt => opt.MapFrom(src => src.PhoneNumber.Trim()))
				.ForMember(dest => dest.Country, opt => opt.MapFrom(src => src.Country.Trim()))
				.ForMember(dest => dest.State, opt => opt.MapFrom(src => src.State.Trim()))
				.ForMember(dest => dest.City, opt => opt.MapFrom(src => src.City.Trim()))
				.ForMember(dest => dest.StreetAddress, opt => opt.MapFrom(src => src.StreetAddress.Trim()))
				.ForMember(dest => dest.ApartmentSuite, opt => opt.MapFrom(src => src.ApartmentSuite?.Trim()))
				.ForMember(dest => dest.PostalCode, opt => opt.MapFrom(src => src.PostalCode.Trim()))
				.ForMember(dest => dest.AddressType, opt => opt.MapFrom(src => src.AddressType.Trim()))
				.ForMember(dest => dest.IsDefault, opt => opt.MapFrom(src => src.IsDefault))
				.ForMember(dest => dest.AdditionalNotes, opt => opt.MapFrom(src => src.AdditionalNotes?.Trim()))
				.ForMember(dest => dest.CustomerId, opt => opt.Ignore())
				.ForMember(dest => dest.Customer, opt => opt.Ignore())
				.ForMember(dest => dest.Id, opt => opt.Ignore())
				.ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
				.ForMember(dest => dest.ModifiedAt, opt => opt.Ignore())
				.ForMember(dest => dest.DeletedAt, opt => opt.Ignore());

			// Image mappings
			CreateMap<Image, ImageDto>()
				.ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
				.ForMember(dest => dest.Url, opt => opt.MapFrom(src => src.Url));

			CreateMap<SubCategory, SubCategoryDto>()
				.ForMember(dest => dest.MainImage, opt => opt.Ignore()) 
				.ForMember(dest => dest.Images, opt => opt.Ignore()) 
				.ForMember(dest => dest.Products, opt => opt.Ignore()) // Ignore automatic mapping
				.ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Name))
				.ForMember(dest => dest.Description, opt => opt.MapFrom(src => src.Description))
				.ForMember(dest => dest.IsActive, opt => opt.MapFrom(src => src.IsActive))
				.ForMember(dest => dest.DeletedAt, opt => opt.MapFrom(src => src.DeletedAt))
				.ForMember(dest => dest.ModifiedAt, opt => opt.MapFrom(src => src.ModifiedAt))
				.AfterMap((src, dest) =>
				{
					if (src.Images == null)
					{
						dest.MainImage = null;
						dest.Images = new List<ImageDto>();
					}
					else
					{
						var main = src.Images.FirstOrDefault(i => i.IsMain && i.DeletedAt == null);
						if (main != null)
						{
							dest.MainImage = new ImageDto
							{
								Id = main.Id,
								Url = main.Url
							};
						}
						else
						{
							dest.MainImage = null;
						}

						dest.Images = src.Images
							.Where(i => !i.IsMain && i.DeletedAt == null)
							.Select(i => new ImageDto
							{
								Id = i.Id,
								Url = i.Url
							})
							.ToList();
					}

					// Map Products if they exist
					if (src.Products != null)
					{
						dest.Products = src.Products
							.Where(p => p.DeletedAt == null)
							.Select(p => new ProductDto
							{
								Id = p.Id,
								Name = p.Name,
								Description = p.Description,
								AvailableQuantity = p.Quantity,
								Gender = p.Gender,
								SubCategoryId = p.SubCategoryId,
								MinPrice = p.ProductVariants?.Any() == true ? p.ProductVariants.Min(v => v.Price) : 0,
								MaxPrice = p.ProductVariants?.Any() == true ? p.ProductVariants.Max(v => v.Price) : 0,
								FinalPrice = CalculateFinalPrice(p),
								HasVariants = p.ProductVariants?.Any() == true,
								TotalVariants = p.ProductVariants?.Count ?? 0,
								Discount = p.Discount != null ? new DiscountDto
								{
									Id = p.Discount.Id,
									Name = p.Discount.Name,
									Description = p.Discount.Description,
									DiscountPercent = p.Discount.DiscountPercent,
									StartDate = p.Discount.StartDate,
									EndDate = p.Discount.EndDate,
									IsActive = p.Discount.IsActive
								} : null,
								Images = p.Images?.Where(i => i.DeletedAt == null).Select(i => new ImageDto
								{
									Id = i.Id,
									Url = i.Url
								}).ToList(),
								Variants = p.ProductVariants?.Where(v => v.DeletedAt == null).Select(v => new ProductVariantDto
								{
									Id = v.Id,
									Color = v.Color,
									Size = v.Size,
									Price = v.Price,
									Quantity = v.Quantity
								}).ToList()
							})
							.ToList();
					}
					else
					{
						dest.Products = new List<ProductDto>();
					}
				})
				.ReverseMap();

			CreateMap<CreateSubCategoryDto, SubCategory>()
				.ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Name.Trim()))
				.ForMember(dest => dest.Description, opt => opt.MapFrom(src => src.Description.Trim()))
				.ForMember(dest => dest.CategoryId, opt => opt.MapFrom(src => src.CategoryId))
				.ForMember(dest => dest.IsActive, opt => opt.MapFrom(src => src.IsActive))
				.ForMember(dest => dest.Images, opt => opt.Ignore())
				.ForMember(dest => dest.Id, opt => opt.Ignore()) 
				.ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
				.ForMember(dest => dest.ModifiedAt, opt => opt.Ignore()) 
				.ForMember(dest => dest.DeletedAt, opt => opt.Ignore()) 
				.ForMember(dest => dest.Category, opt => opt.Ignore())
				.ForMember(dest => dest.Products, opt => opt.Ignore());
			CreateMap<UpdateSubCategoryDto, SubCategory>()
				.ForMember(dest => dest.Images, opt => opt.Ignore())
				.ForMember(dest => dest.Id, opt => opt.Ignore())
				.ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
				.ForMember(dest => dest.ModifiedAt, opt => opt.Ignore())
				.ForMember(dest => dest.DeletedAt, opt => opt.Ignore())
				.ForMember(dest => dest.Category, opt => opt.Ignore())
				.ForMember(dest => dest.Products, opt => opt.Ignore())
				 .AfterMap((src, dest) =>
				{
					if (src.Name != null)
						dest.Name = src.Name.Trim();

					if (src.Description != null)
						dest.Description = src.Description.Trim();

					if (src.CategoryId.HasValue)
						dest.CategoryId = src.CategoryId.Value;

					if (src.IsActive)
						dest.IsActive = src.IsActive;
				});

		}

		private static decimal CalculateFinalPrice(Product product)
		{
			// If no variants, return 0
			if (!product.ProductVariants.Any())
				return 0;
			
			var minPrice = product.ProductVariants.Min(v => v.Price);
			
			// Apply discount if available
			if (product.Discount != null && product.Discount.IsActive)
			{
				minPrice = minPrice * (1 - product.Discount.DiscountPercent);
			}
			
			return Math.Round(minPrice, 2);
		}

		private static decimal CalculateMinPrice(Product product)
		{
			if (!product.ProductVariants.Any())
				return 0;
			
			return Math.Round(product.ProductVariants.Min(v => v.Price), 2);
		}

		private static decimal CalculateMaxPrice(Product product)
		{
			if (!product.ProductVariants.Any())
				return 0;
			
			return Math.Round(product.ProductVariants.Max(v => v.Price), 2);
		}

		private static decimal CalculateUnitPrice(CartItem cartItem)
		{
			if (cartItem.ProductVariant != null)
			{
				return cartItem.ProductVariant.Price;
			}
			
			if (cartItem.Product?.ProductVariants?.Any() == true)
			{
				return cartItem.Product.ProductVariants.Min(v => v.Price);
			}
			
			return 0;
		}

		private static decimal CalculateCartItemTotalPrice(CartItem cartItem)
		{
			var unitPrice = CalculateUnitPrice(cartItem);
			var quantity = cartItem.Quantity;
			
			// Apply discount if available
			if (cartItem.Product?.Discount != null && cartItem.Product.Discount.IsActive)
			{
				unitPrice *= (1 - cartItem.Product.Discount.DiscountPercent / 100);
			}
			
			return unitPrice * quantity;
		}
	}
}
