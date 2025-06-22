using E_Commers.DtoModels.Shared;

namespace E_Commers.Interfaces
{
    public interface IProductInventoryLinkBuilder : ILinkBuilder
    {
        List<LinkDto> GenerateLinks(int? id = null);
    }
} 