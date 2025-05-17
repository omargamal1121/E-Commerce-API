namespace E_Commers.DtoModels.AccountDtos
{
	public class UploadPhotoResponseDto
	{
		public string ImageUrl { get; set; }  
		public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
	}

}
