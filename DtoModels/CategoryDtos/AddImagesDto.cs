using System.ComponentModel.DataAnnotations;

namespace E_Commers.DtoModels.CategoryDtos
{
	public class AddImagesDto
	{
		[Required]
		public List<IFormFile> Images { get; set; }
	}
}
