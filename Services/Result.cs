namespace E_Commers.Services
{
	public class Result<T>
	{
		public bool Success { get; set; }
		public string Message { get; set; } = string.Empty;
		public  T Data { get; set; } 

		

		public static Result<T> Fail(string message) => new Result<T> { Success = false, Message = message };

		public static Result<T> Ok(T data, string message = "Operation succeeded")
			=> new Result<T> { Success = true, Data = data, Message = message };
	}
}
