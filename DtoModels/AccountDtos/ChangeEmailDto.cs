using System.ComponentModel.DataAnnotations;

namespace E_Commers.DtoModels.AccountDtos
{
	public class ChangeEmailDto
	{
		[EmailAddress]
		public string Email { get; set; }
	}
}
