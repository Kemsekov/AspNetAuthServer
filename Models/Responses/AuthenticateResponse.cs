using Microsoft.AspNetCore.Identity;
using Auth.Entities;

namespace Auth.Models.Responses
{
    public class AuthenticateResponse
    {
        public string Id { get; set; }
        public string Email { get; set; }
        public string UserName { get; set; }
        public string Token { get; set; }

        public AuthenticateResponse(ApplicationUser user, string token)
        {
            Id = user.Id;
            Email = user.Email;
            UserName = user.UserName;
            Token = token;
        }
    }
}