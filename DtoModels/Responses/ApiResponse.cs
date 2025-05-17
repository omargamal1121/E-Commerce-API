using E_Commers.DtoModels.Shared;
using E_Commers.ErrorHnadling;

namespace E_Commers.DtoModels.Responses
{
	public class ApiResponse<T> where T : class
	{
		public int Statuscode { get; set; }
		public ResponseBody<T> ResponseBody { get; set; }
		public ApiResponse()
		{

		}
		public ApiResponse(int statuscode, ResponseBody<T> Response)
		{
			Statuscode = statuscode;
			ResponseBody = Response;
		}
		public static ApiResponse<T> CreateSuccessResponse(string message, T? data=null, int statusCode = 200, List<LinkDto>? links=null)
		{
			return new ApiResponse<T>(statusCode, new ResponseBody<T>(message: message,data: data,links: links));
		}
		public static ApiResponse<T> CreateErrorResponse(ErrorResponse error, int statusCode = 400, List<LinkDto>? links=null)
		{
			var responsebody = new ResponseBody<T>(error, links: links);
			return new ApiResponse<T>(statusCode, responsebody );
		}

	}
}