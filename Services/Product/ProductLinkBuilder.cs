using E_Commers.Controllers;
using E_Commers.DtoModels.Shared;
using E_Commers.Interfaces;
using E_Commers.Services.Category;

namespace E_Commers.Services.Product
{
    public class ProductLinkBuilder : BaseLinkBuilder,IProductLinkBuilder
    {
        protected override string ControllerName => "Products";

        public ProductLinkBuilder(IHttpContextAccessor context, LinkGenerator generator)
            : base(context, generator) { }

        public override List<LinkDto> GenerateLinks(int? id = null)
        {
            if (_context.HttpContext == null)
                return new List<LinkDto>();

            var links = new List<LinkDto>
            {
                new LinkDto(
                    GetUriByAction(nameof(ProductController.CreateProduct)) ?? "",
                    "create",
                    "POST"
                ),
                new LinkDto(
                    GetUriByAction(nameof(ProductController.GetAllProducts)) ?? "",
                    "get-all",
                    "GET"
                ),
            };

            if (id != null)
            {
                links.Add(
                    new LinkDto(
                        GetUriByAction(nameof(ProductController.GetProduct), new { id }) ?? "",
                        "self",
                        "GET"
                    )
                );

                links.Add(
                    new LinkDto(
                        GetUriByAction(nameof(ProductController.UpdateProduct), new { id }) ?? "",
                        "update",
                        "PUT"
                    )
                );

                links.Add(
                    new LinkDto(
                        GetUriByAction(nameof(ProductController.DeleteProductAsync), new { id }) ?? "",
                        "delete",
                        "DELETE"
                    )
                );
            }
            return links;
        }
    }
}
