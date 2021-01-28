using Microsoft.AspNetCore.Identity;
using WebApi.Entities;

namespace WebApi.Models
{
    public class AuthenticateResponse
    {
        public string Id { get; set; }
        public string Email { get; set; }
        public string UserName { get; set; }
        public string Token { get; set; }

        public AuthenticateResponse(IdentityUser user, string token)
        {
            Id = user.Id;
            Email = user.Email;
            UserName = user.UserName;
            Token = token;
        }
    }
}