using E_Commers.Models;
using E_Commers.Services;

namespace E_Commers.Interfaces
{
    public interface IImageRepository : IRepository<Image>
    {
        Task<Image?> GetByUrlAsync(string url);
    }
} 