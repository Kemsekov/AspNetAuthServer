using System.ComponentModel.DataAnnotations;

namespace WebApi.Models.Requests
{
    public class UpdateUserRequest
    {
        public FindUser findUser{get;set;}
        public string Email{get;set;}
        public string UserName{get;set;}
        public string Name{get;set;}
        public string[] AddRoles{get;set;}
        public string[] RemoveRoles{get;set;}
        [Phone]
        public string Phone{get;set;}
        public ChangePassword changePassword{get;set;}
        public class ChangePassword{
        [Required]
        [MinLength(8)]
        [MaxLength(256)]
        public string NewPassword{get;set;}
        [Required]
        [MinLength(8)]
        [MaxLength(256)]
        public string OldPassword{get;set;}

        }
    }
}