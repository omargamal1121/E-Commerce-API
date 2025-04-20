namespace E_Commers.DtoModels
{
	public record ResponseDto
	{
		public ResponseDto()
		{
			
		}
		public ResponseDto(string message,object? data=null)
		{
			Message = message;
			Data = data ?? new object();
			Links = new List<LinkDto>();
			
		}
		public string Message { get; set; }=string.Empty;
		public object Data { get; set; }=new object();

		public List<LinkDto> Links { get; set; }
	}
}
