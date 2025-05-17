using E_Commers.Controllers;
using E_Commers.DtoModels.Shared;
using E_Commers.Interfaces;

namespace E_Commers.Services.Category
{
    public class CategoryLinkBuilder : BaseLinkBuilder, ICategoryLinkBuilder
    {
        protected override string ControllerName => "Categories";

        public CategoryLinkBuilder(IHttpContextAccessor context, LinkGenerator generator)
            : base(context, generator) { }

        public override List<LinkDto> GenerateLinks(int? id = null)
        {
            if (_context.HttpContext == null)
                return new List<LinkDto>();

            var links = new List<LinkDto>
            {
                new LinkDto(
                    GetUriByAction(nameof(categoriesController.CreateAsync)) ?? "",
                    "create",
                    "POST"
                ),
                new LinkDto(
                    GetUriByAction(nameof(categoriesController.GetAllAsync)) ?? "",
                    "get-all",
                    "GET"
                ),
            };

            if (id != null)
            {
                links.Add(
                    new LinkDto(
                        GetUriByAction(nameof(categoriesController.GetProduectsByCategoryIdAsync), new { id })
                            ?? "",
                        "self",
                        "GET"
                    )
                );

                links.Add(
                    new LinkDto(
                        GetUriByAction(nameof(categoriesController.DeleteAsync), new { id })
                            ?? "",
                        "delete",
                        "DELETE"
                    )
                );

                links.Add(
                    new LinkDto(
                        GetUriByAction(nameof(categoriesController.UpdateAsync), new { id })
                            ?? "",
                        "update",
                        "PATCH"
                    )
                );

                links.Add(
                    new LinkDto(
                        GetUriByAction(
                            nameof(categoriesController.GetProduectsByCategoryIdAsync),
                            new { id }
                        ) ?? "",
                        "Get-Products",
                        "GET"
                    )
                );
            }
            return links;
        }
    }
}
