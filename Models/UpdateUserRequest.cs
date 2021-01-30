using System.ComponentModel.DataAnnotations;

namespace WebApi.Models
{
    public class UpdateUserRequest
    {
        [Required]
        public string FindUserBy{get;set;}
        public string Id{get;set;}
        [MinLength(5)]
        [MaxLength(256)]
        public string UserName{get;set;}    
        public string[] AddRoles{get;set;}
        public string[] RemoveRoles{get;set;}
        [EmailAddress]
        public string Email{get;set;}
        [MinLength(8)]
        [MaxLength(256)]
        public string Password{get;set;}
        [Phone]
        public string Phone{get;set;}
    }
}