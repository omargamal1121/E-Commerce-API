using E_Commers.DtoModels.Shared;
using E_Commers.Interfaces;

namespace E_Commers.Services.Category
{
    public abstract class BaseLinkBuilder 
    {
        protected readonly LinkGenerator _generator;
        protected readonly IHttpContextAccessor _context;
        protected abstract string ControllerName { get; }

        protected BaseLinkBuilder(IHttpContextAccessor context, LinkGenerator generator)
        {
            _context = context;
            _generator = generator;
        }

        public abstract List<LinkDto> GenerateLinks(int? id = null);

        public virtual List<LinkDto> MakeRelSelf(List<LinkDto> links, string rel)
        {
            if (string.IsNullOrEmpty(rel) || links == null || links.Count == 0)
                return links;

            var link = links.FirstOrDefault(l =>
                l.Rel.Equals(rel, StringComparison.OrdinalIgnoreCase)
                || l.Rel.Contains(rel, StringComparison.OrdinalIgnoreCase)
            );

            if (link == null)
                return links;

            links.Remove(link);
            links.Add(new LinkDto(link.Href, "self", link.Method));

            return links;
        }

        protected string? GetUriByAction(string actionName, object? values = null)
        {
            return _generator.GetUriByAction(
                httpContext: _context.HttpContext,
                action: actionName,
                controller: ControllerName,
                values: values
            );
        }
    }
}
