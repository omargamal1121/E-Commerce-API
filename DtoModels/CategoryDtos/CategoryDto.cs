using E_Commers.Models;


namespace E_Commers.DtoModels.CategoryDtos
{
	public record CategoryDto:BaseDto
	{
		public CategoryDto()
		{
			
		}

		public CategoryDto(int id, string name,string? description,DateTime? create, DateTime? modeify=null, DateTime? delete=null)
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
