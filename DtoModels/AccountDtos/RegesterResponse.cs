namespace E_Commers.DtoModels.AccountDtos
{
	public class RegisterResponse
	{
		public Guid UserId { get; set; }
		public string Name { get; set; } = string.Empty;
		public string UserName { get; set; } = string.Empty;
		public string PhoneNumber { get; set; } = string.Empty;
		public int Age { get; set; }
		public string Email { get; set; } = string.Empty;


	}
}
