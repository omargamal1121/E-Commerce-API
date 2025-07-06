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
                    GetUriByAction(nameof(CategoriesController.CreateAsync)) ?? "",
                    "create",
                    "POST"
                ),
                new LinkDto(
                    GetUriByAction(nameof(CategoriesController.GetAllAsync)) ?? "",
                    "get-all",
                    "GET"
                ),
               
            };

            if (id != null)
            {
                links.Add(
                    new LinkDto(
                        GetUriByAction(nameof(CategoriesController.GetByIdAsync), new { id }) ?? "",
                        "get-by-id",
                        "GET"
                    )
                );
                links.Add(
                    new LinkDto(
                        GetUriByAction(nameof(CategoriesController.DeleteAsync), new { id })
                            ?? "",
                        "delete",
                        "DELETE"
                    )
                );
                links.Add(
                    new LinkDto(
                        GetUriByAction(nameof(CategoriesController.UpdateAsync), new { id })
                            ?? "",
                        "update",
                        "PUT"
                    )
                );
                links.Add(
                    new LinkDto(
                        GetUriByAction(nameof(CategoriesController.ReturnRemovedCategoryAsync), new { id }) ?? "",
                        "restore",
                        "PATCH"
                    )
                );
                links.Add(
                    new LinkDto(
                        GetUriByAction(nameof(CategoriesController.AddMainImageAsync), new { id }) ?? "",
                        "add-main-image",
                        "POST"
                    )
                );
                links.Add(
                    new LinkDto(
                        GetUriByAction(nameof(CategoriesController.AddExtraImagesAsync), new { id }) ?? "",
                        "add-extra-images",
                        "POST"
                    )
                );
                links.Add(
                    new LinkDto(
                        GetUriByAction(nameof(CategoriesController.ChangeActiveStatus), new { categoryId = id }) ?? "",
                        "change-active-status",
                        "PATCH"
                    )
                );
                links.Add(
                    new LinkDto(
                        GetUriByAction(nameof(CategoriesController.RemoveImageAsync), new { categoryId = id, imageId = 0 }) ?? "",
                        "remove-image",
                        "DELETE"
                    )
                );
            }
            return links;
        }
    }
}
