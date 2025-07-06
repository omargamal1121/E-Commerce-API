using System.ComponentModel.DataAnnotations;

namespace E_Commers.DtoModels.ImagesDtos
{
	public class CreateImageDto
	{
		[Required(ErrorMessage = "Image URL is required")]
		[Url(ErrorMessage = "Invalid URL format")]
		public string Url { get; set; } = string.Empty;

		public bool? IsMain { get; set; } = false;
	}
} 