using E_Commers.Models;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace E_Commers.DtoModels.CategoryDtos
{
	public class CategoryDto:BaseEntity
	{
		public CategoryDto()
		{
			
		}

		public CategoryDto(int id, string name,string? description,DateTime? create, DateTime? modeify, DateTime? delete)
		{
			Id = id;
			Name = name;
			Description = Description;
			CreatedAt = create;
			ModifiedAt = modeify;
			DeletedAt = delete;
		}

		public string Name { get; set; }
		public string? Description { get; set; }

	
	}
}
