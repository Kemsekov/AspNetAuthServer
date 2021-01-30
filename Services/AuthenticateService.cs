using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using WebApi.Entities;
using WebApi.Helpers;
using WebApi.Models;

namespace WebApi.Services
{
    public interface IAuthenticateService
    {
        AuthenticateResponse Authenticate(AuthenticateRequest model);
    }

    public class AuthenticateService : IAuthenticateService
    {

        private readonly AppSettings _appSettings;
        private readonly UserManager<IdentityUser> _userManager;

        public AuthenticateService(IOptions<AppSettings> appSettings,UserManager<IdentityUser> userManager)
        {
            _appSettings = appSettings.Value;
            _userManager = userManager;
        }

        public async Task<AuthenticateResponse> AuthenticateAsync(AuthenticateRequest model)
        {
            
            //var user = _users.SingleOrDefault(x => x.Username == model.Username && x.Password == model.Password);
            IdentityUser identityUser = null;
            
            if(!string.IsNullOrEmpty(model.Email))
            {
                identityUser = await _userManager.FindByEmailAsync(model.Email);
            }
            else{
                identityUser = await _userManager.FindByNameAsync(model.UserName);
            }
            
            // return null if user not found or wrong password
            if (identityUser == null) return null;
            var isPasswordValid = await _userManager.CheckPasswordAsync(identityUser,model.Password);
            if(!isPasswordValid) return null;

            // authentication successful so generate jwt token
            var token = generateJwtToken(identityUser);

            return new AuthenticateResponse(identityUser, token);
        }
        public AuthenticateResponse Authenticate(AuthenticateRequest model)
        {
            return this.AuthenticateAsync(model).GetAwaiter().GetResult();
        }

        // helper methods

        private string generateJwtToken(IdentityUser user)
        {
            // generate token that is valid for 7 days
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_appSettings.Secret);
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[] { new Claim("id", user.Id) }),
                Expires = DateTime.UtcNow.AddHours(2),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };
            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }

    }
}

