using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Identity;

namespace Auth.Entities
{
    /// <summary>
    /// This is simplefied version of IdentityUser that helps incapsulate some IdentityUser fields
    /// </summary>
    public class SimpleUser
    {
        public SimpleUser()
        {
        }
        [JsonIgnore]
        public ApplicationUser identityUser {get; set;} = new ApplicationUser();
        public string Id{get=>identityUser.Id;set=>identityUser.Id=value;}
        public string UserName {get=>identityUser.UserName;set=>identityUser.UserName = value;}
        public string Email{get=>identityUser.Email;set=>identityUser.Email = value;}
        public string Phone{get=>identityUser.PhoneNumber;set=>identityUser.PhoneNumber=value;}

        [JsonIgnore]
        public string Password { get; set; }
        //public static implicit operator byte(Digit d) => d.digit;

    }
}