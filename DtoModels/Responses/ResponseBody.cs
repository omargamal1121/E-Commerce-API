using E_Commers.DtoModels.Shared;
using E_Commers.ErrorHnadling;

namespace E_Commers.DtoModels.Responses
{
	public class ResponseBody<T>where T:class{

		public ErrorResponse? Errors { get; set; }
		public string? Message { get; set; }
		public T? Data { get; set; }
		public List<LinkDto>? Links{ get; set; }
		public ResponseBody()
		{
			
		}
		public ResponseBody(ErrorResponse? error=null,string? message=null, T? data=null, List<LinkDto>? links = null, ErrorResponse? errorResponse = null)
		{
			Errors = error;
			Message= message;
			Data=data;
			Links=links;

		}
	}
}
