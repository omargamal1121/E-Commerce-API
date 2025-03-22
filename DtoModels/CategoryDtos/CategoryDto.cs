using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace E_Commers.DtoModels.CategoryDtos
{
	public class CategoryDto
	{
		public int Id { get; set; }
		public string Name { get; set; }
		public string? Description { get; set; }

		[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
		public DateTime? CreatedAt { get; set; }

		[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
		public DateTime? ModifiedAt { get; set; }

		[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
		public DateTime? DeletedAt { get; set; }
	}
}
