using System.ComponentModel.DataAnnotations;

namespace WebApi.Models.Requests
{
    public class NeedFindUser
    {
        [Required]
        public string FindUserBy{get;set;}
        [MinLength(5)]
        [MaxLength(256)]
        public string UserName{get;set;}
        [EmailAddress]
        public string Email{get;set;}
        public string Id{get;set;}
    }
}