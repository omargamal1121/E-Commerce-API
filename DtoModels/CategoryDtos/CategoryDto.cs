using E_Commers.DtoModels.ProductDtos;
using E_Commers.DtoModels.Shared;
using E_Commers.Models;


namespace E_Commers.DtoModels.CategoryDtos
{
	public class UpdateCategoryDto 
	{
		public string Name { get; set; }
		public string Description { get; set; }
	}
	public class CategoryDto:BaseDto
	{
		public CategoryDto()
		{
			
		}

		public CategoryDto(int id, string name,string? description,DateTime? create, DateTime? modeify=null, DateTime? delete=null)
		{
			Id = id;
			Name = name;
			Description = description;
			CreatedAt = create;
			ModifiedAt = modeify;
			DeletedAt = delete;
		}

		public string Name { get; set; }
		public string? Description { get; set; }

	}
}
