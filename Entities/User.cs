using System.Text.Json.Serialization;
using AutoMapper;
using Microsoft.AspNetCore.Identity;

namespace WebApi.Entities
{
    public class User
    {
        public User()
        {
        }
        [JsonIgnore]
        public IdentityUser identityUser {get; set;} = new IdentityUser();
        public string Id{get=>identityUser.Id;set=>identityUser.Id=value;}
        public string UserName {get=>identityUser.UserName;set=>identityUser.UserName = value;}
        public string Email{get=>identityUser.Email;set=>identityUser.Email = value;}
        public string Phone{get=>identityUser.PhoneNumber;set=>identityUser.PhoneNumber=value;}

        [JsonIgnore]
        public string Password { get; set; }
        //public static implicit operator byte(Digit d) => d.digit;

    }
}