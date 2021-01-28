using System.ComponentModel.DataAnnotations;

namespace WebApi.Models
{
    public class AuthenticateRequest
    {
        public string Username { get; set; }
        [EmailAddress]
        public string Email{get;set;}
        [Required]
        [MaxLength(256)]
        [MinLength(8)]
        public string Password { get; set; }
    }
}