using System.ComponentModel.DataAnnotations;

namespace WebApi.Models
{
    public class RemoveRequest : INeedFindUser
    {
        [Required]
        public string FindUserBy{get;set;}
        public string Id{get;set;}
        [MinLength(5)]
        [MaxLength(256)]
        public string UserName{get;set;}    
        [EmailAddress]
        public string Email{get;set;}
    }
}