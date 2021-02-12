using System.ComponentModel.DataAnnotations;

namespace WebApi.Models.Requests
{
    public class UpdateUserRequest : NeedFindUser
    {
        public string Name{get;set;}
        public string[] AddRoles{get;set;}
        public string[] RemoveRoles{get;set;}
        [MinLength(8)]
        [MaxLength(256)]
        public string NewPassword{get;set;}
        public string OldPassword{get;set;}
        [Phone]
        public string Phone{get;set;}
    }
}