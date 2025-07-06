using System.ComponentModel.DataAnnotations;

namespace E_Commers.DtoModels.AccountDtos
{
    public class ConfirmEmailDto
    {
        [Required(ErrorMessage = "Token is required")]
        public string Token { get; set; }
    }
} 