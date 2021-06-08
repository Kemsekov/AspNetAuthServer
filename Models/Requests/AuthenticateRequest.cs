using System.ComponentModel.DataAnnotations;

namespace Auth.Models.Requests
{
    public class AuthenticateRequest
    {
        [MinLength(5)]
        [MaxLength(256)]
        [Required]
        public string UserName { get; set; }
        [Required]
        [MaxLength(256)]
        [MinLength(8)]
        public string Password { get; set; }
        [Required]
        public string[] Scopes {get;set;}
        public string Service{get;set;}
    }
}