using System.ComponentModel.DataAnnotations;

namespace E_Commers.DtoModels.AccountDtos
{
    public class RequestPasswordResetDto
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;
    }
} 