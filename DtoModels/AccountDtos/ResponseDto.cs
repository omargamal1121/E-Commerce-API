namespace E_Commers.DtoModels.AccountDtos
{
	public class ResponseDto
	{
		public int StatusCode { get; set; } = 200;
		public dynamic Message { get; set; } = new object();
	}
}
