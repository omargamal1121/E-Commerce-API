namespace E_Commers.ErrorHnadling
{
	public class ErrorResponse
	{
		
		public string Title { get; set; }
		public string Message { get; set; }
		public string Detail { get; set; }
		public string Instance { get; set; }

		public ErrorResponse( string title, string message, string detail = null, string instance = null)
		{
			Title = title;
			Message = message;
			Detail = detail;
			Instance = instance;
		}

		// يمكنك أيضًا إضافة أي خصائص إضافية تحتاجها مثل:
		// - Timestamp (تاريخ ووقت حدوث الخطأ)
		// - DeveloperMessage (رسالة للمطور فقط، يمكن استخدامها أثناء التطوير)
	}

}
