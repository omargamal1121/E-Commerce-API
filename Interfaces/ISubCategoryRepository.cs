using E_Commers.Models;
using System.Linq;
using System.Threading.Tasks;

namespace E_Commers.Interfaces
{
    public interface ISubCategoryRepository : IRepository<SubCategory>
    {
        Task<bool> SubCategoryExistsAsync(int id);
        Task<SubCategory?> GetSubCategoryById(int id, bool isActiveFilter = false);
        public  Task<SubCategory?> FindByNameAsync(string name, bool? isDelete = null);
        public  Task<bool> IsExsistByNameAsync(string name);

		IQueryable<SubCategory> FindByNameContains(string partialName);
        Task<bool> HasProductsAsync(int subCategoryId);
    }
} 