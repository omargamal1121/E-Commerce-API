namespace E_Commers.DtoModels.AccountDtos
{
	public class ResponseDto
	{
		public ResponseDto()
		{
			
		}
		public ResponseDto(int code,string message,object? data=null)
		{
			StatusCode = code;
			Message = message;
			Data = data ?? new object();
			
		}
		public int StatusCode { get; set; }
		public string Message { get; set; }=string.Empty;
		public object Data { get; set; }=new object();
	}
}
