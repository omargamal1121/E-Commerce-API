using E_Commers.Context;
using E_Commers.Interfaces;
using E_Commers.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Linq;
using System.Threading.Tasks;

public class SubCategoryRepository : MainRepository<SubCategory>, ISubCategoryRepository
{
    private readonly DbSet<SubCategory> _subCategories;
    private readonly ILogger<SubCategoryRepository> _logger;

    public SubCategoryRepository(AppDbContext context, ILogger<SubCategoryRepository> logger) : base(context, logger)
    {
        _subCategories = context.Set<SubCategory>();
        _logger = logger;
    }

    public async Task<bool> SubCategoryExistsAsync(int id)
    {
        _logger.LogInformation($"Executing {nameof(SubCategoryExistsAsync)} Id: {id}");
        var subCategory = await GetByIdAsync(id);
        if (subCategory == null)
        {
            _logger.LogWarning($"No SubCategory With this id:{id}");
            return false;
        }
        _logger.LogInformation("SubCategory found");
        return true;
    }

    public async Task<SubCategory?> GetSubCategoryById(int id, bool isActiveFilter = false)
    {
        _logger.LogInformation($"Executing {nameof(GetSubCategoryById)} for id: {id}");
        var subCategory = await _subCategories
            .Where(sc => sc.Id == id && (!isActiveFilter || sc.IsActive))
            .Select(sc => new SubCategory
            {
                Id = sc.Id,
                Name = sc.Name,
                Description = sc.Description,
                IsActive = sc.IsActive,
                CategoryId = sc.CategoryId,
                CreatedAt = sc.CreatedAt,
                ModifiedAt = sc.ModifiedAt,
                DeletedAt = sc.DeletedAt,
                Images = sc.Images.Where(i => i.DeletedAt == null).Select(i => new Image
                {
                    Id = i.Id,
                    Url = i.Url,
                    IsMain = i.IsMain,
                    UploadDate = i.UploadDate, 
                    FileType= i.FileType,

				}).ToList(),
                Products = sc.Products.Where(p => p.DeletedAt == null).Select(p => new Product
                {
                    Id = p.Id,
                    Name = p.Name,
                    Description = p.Description,
                    Quantity = p.Quantity,
                    Gender = p.Gender,
                    SubCategoryId = p.SubCategoryId,
                    CreatedAt = p.CreatedAt,
                    ModifiedAt = p.ModifiedAt,
                    DeletedAt = p.DeletedAt,
                    Images = p.Images.Where(i => i.DeletedAt == null).Select(i => new Image
                    {
                        Id = i.Id,
                        Url = i.Url,
                        IsMain = i.IsMain,
                        FileType = i.FileType
                    }).ToList(),
					Discount = p.Discount != null ? new Discount
                    {
                        Id = p.Discount.Id,
                        Name = p.Discount.Name,
                        Description = p.Discount.Description,
                        DiscountPercent = p.Discount.DiscountPercent,
                        StartDate = p.Discount.StartDate,
                        EndDate = p.Discount.EndDate,
                        IsActive = p.Discount.IsActive,

                    } : null,
                    ProductVariants = p.ProductVariants.Where(v => v.DeletedAt == null).Select(v => new ProductVariant
                    {
                        Id = v.Id,
                        Color = v.Color,
                        Size = v.Size,
                        Price = v.Price,
                        Quantity = v.Quantity,
                        CreatedAt = v.CreatedAt,
                        ModifiedAt = v.ModifiedAt,
                        DeletedAt = v.DeletedAt
                    }).ToList()
                }).ToList()
            })
            .FirstOrDefaultAsync();
        if (subCategory == null)
        {
            _logger.LogWarning($"SubCategory with id: {id} doesn't exist");
            return null;
        }
        _logger.LogInformation($"SubCategory with id: {id} exists");
        return subCategory;
    }

	public async Task<bool> IsExsistByNameAsync(string name)
	{
		return await _subCategories
			.Where(sc => sc.Name == name)
			.FirstOrDefaultAsync()==null?false:true;
	}
	public async Task<SubCategory?> FindByNameAsync(string name, bool? isDelete = null)
	{
		var query = _subCategories.Where(sc => sc.Name == name);

		if (isDelete.HasValue)
		{
			query = query.Where(sc => isDelete.Value ? sc.DeletedAt != null : sc.DeletedAt == null);
		}

		return await query
			.Select(sc => new SubCategory
			{
				Id = sc.Id,
				Name = sc.Name,
				Description = sc.Description,
				IsActive = sc.IsActive,
				CategoryId = sc.CategoryId,
				CreatedAt = sc.CreatedAt,
				ModifiedAt = sc.ModifiedAt,
				DeletedAt = sc.DeletedAt,
				Images = sc.Images.Where(i => i.DeletedAt == null).Select(i => new Image
				{
					Id = i.Id,
					Url = i.Url,
					IsMain = i.IsMain,
			
				}).ToList()
			})
			.FirstOrDefaultAsync();
	}
	public IQueryable<SubCategory> FindByNameContains(string partialName)
    {
        if (string.IsNullOrWhiteSpace(partialName))
            return Enumerable.Empty<SubCategory>().AsQueryable();
        partialName = partialName.Trim().ToLowerInvariant();
        return _subCategories
            .Where(sc =>
                EF.Functions.Like(sc.Name, $"%{partialName}%") &&
                sc.DeletedAt == null
            )
            .Select(sc => new SubCategory
            {
                Id = sc.Id,
                Name = sc.Name,
                Description = sc.Description,
                IsActive = sc.IsActive,
                CategoryId = sc.CategoryId,
                CreatedAt = sc.CreatedAt,
                ModifiedAt = sc.ModifiedAt,
                DeletedAt = sc.DeletedAt,
                Images = sc.Images.Where(i => i.DeletedAt == null).Select(i => new Image
                {
                    Id = i.Id,
                    Url = i.Url,
                    IsMain = i.IsMain,
                    AltText = i.AltText,
                    Title = i.Title,
    
                }).ToList()
            });
    }

    public async Task<bool> HasProductsAsync(int subCategoryId)
    {
        return await _subCategories.AnyAsync(sc => sc.Id == subCategoryId && sc.Products.Any(p => p.DeletedAt == null));
    }
} 