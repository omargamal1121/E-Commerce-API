using E_Commers.Controllers;
using E_Commers.DtoModels.Shared;
using E_Commers.Interfaces;
using E_Commers.Services.Category;

namespace E_Commers.Services.ProductInventoryServices
{
    public class ProductInventoryLinkBuilder : BaseLinkBuilder, IProductInventoryLinkBuilder
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly LinkGenerator _linkGenerator;

        public ProductInventoryLinkBuilder(IHttpContextAccessor httpContextAccessor, LinkGenerator linkGenerator) 
            : base(httpContextAccessor, linkGenerator)
        {
            _httpContextAccessor = httpContextAccessor;
            _linkGenerator = linkGenerator;
        }

        protected override string ControllerName => nameof(ProductInventoriesController).Replace("Controller", "");

        public override List<LinkDto> GenerateLinks(int? id = null)
        {
            var list = new List<LinkDto>
            {
                new LinkDto(GetUriByAction(nameof(ProductInventoriesController.AddProductToWarehouse)) ?? "", "add-product-to-warehouse", "POST"),
                new LinkDto(GetUriByAction(nameof(ProductInventoriesController.GetAllAsync)) ?? "", "get-all-inventory", "GET"),
                new LinkDto(GetUriByAction(nameof(ProductInventoriesController.GetDeletedInvetoriesAsync)) ?? "", "get-deleted-inventories", "GET")
            };

            if (id != null)
            {
                list.AddRange(new[]
                {
                    new LinkDto(GetUriByAction(nameof(ProductInventoriesController.GetInventory), id) ?? "", "get-inventory", "GET"),
                    new LinkDto(GetUriByAction(nameof(ProductInventoriesController.IncreaseQuantityofProductToWarehouse)) ?? "", "increase-quantity", "PATCH"),
                    new LinkDto(GetUriByAction(nameof(ProductInventoriesController.TransferQuantityOfProductToWarehouse)) ?? "", "transfer-quantity", "PATCH"),
                    new LinkDto(GetUriByAction(nameof(ProductInventoriesController.DeleteInventoryAsync), id) ?? "", "delete-inventory", "DELETE"),
                    new LinkDto(GetUriByAction(nameof(ProductInventoriesController.ReturnRemovedInventoryAsync), id) ?? "", "return-removed-inventory", "PATCH")
                });
            }

            return list;
        }
    }
} 