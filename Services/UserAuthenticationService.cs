using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using WebApi.Contexts;
using WebApi.Entities;
using WebApi.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using WebApi.Options;
using WebApi.Models.Requests;

namespace WebApi.Services
{
    public class UserAuthenticationService : IUserAuthenticationService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IOptions<JwtOptions> _jwtOptions;
        private readonly WebApiDbContext _context;
        public UserAuthenticationService(UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager, IOptions<JwtOptions> jwtOptions,
            WebApiDbContext context)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _jwtOptions = jwtOptions;
            _context = context;
        }
        /// <summary>
        /// Authenticate by login and password. Always deletes old refresh token, create new one and create new access token
        /// </summary>
        /// <param name="authModel"></param>
        /// <returns>refresh and access token</returns>
        public async Task<AuthResult> AuthenticateAsync(AuthenticateRequest authModel)
        {
            var result = new AuthResult();

            var user = await _userManager.FindByNameAsync(authModel.UserName);
            if (user == null)
                return result;

            var pass_check = await _userManager.CheckPasswordAsync(user, authModel.Password);
            if (!pass_check)
                return result;

            result.Token = await GenerateTokenAsync(user);
            if(await _context.RefreshTokens.FirstOrDefaultAsync(rt=>rt.UserId==user.Id) is RefreshToken token)
                result.RefreshToken = token.Token;
            else
                result.RefreshToken = await GenerateRefreshTokenAsync();
            
            _context.Entry(user).State = EntityState.Modified;
            await _context.SaveChangesAsync();
            return result;
        }

        /// <summary>
        /// Get new access token by refresh token
        /// </summary>
        /// <param name="refreshToken"></param>
        /// <returns>Access token if refresh token is valid</returns>
        public async Task<string> RefreshTokenAsync(string refreshToken)
        {
            var result = new AuthResult();
            var user = await _context.Users
                .Include(t => t.RefreshTokens)
                .FirstOrDefaultAsync(t => t.RefreshTokens
                .Any(r => r.Token.Equals(refreshToken)));

            if (user == null)
                return null;

            return await GenerateTokenAsync(user);
        }

        public async Task<string> UpdateTokenAsync(string userid,string securityStampFromJWToken){
            var user = await _userManager.FindByIdAsync(userid);
            if(user==null) return null;

            //check if user's credentials changed recently. If it is, then we cannot trust current request.
            if(securityStampFromJWToken!=user.SecurityStamp){
                return null;
            }
            var refresh_token = await _context.RefreshTokens.FirstOrDefaultAsync(t=>t.UserId==user.Id);
            var token = await RefreshTokenAsync(refresh_token.Token);
            
            return token;
        }

        private async Task<string> GenerateTokenAsync(ApplicationUser user)
        {
            var token = string.Empty;
            var userClaims = await _userManager.GetClaimsAsync(user);
            var userRoles = await _userManager.GetRolesAsync(user);
            var claims = userClaims.ToList();
            claims.AddRange(userRoles.Select(u => new Claim(ClaimTypes.Role, u)));
            claims.AddRange(new Claim[]
            {
               new Claim(ClaimTypes.Name, user.UserName),
               new Claim(ClaimTypes.Email, user.Email),
               new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
               new Claim(JwtRegisteredClaimNames.Sub,user.Id.ToString()),
               new Claim(ClaimTypes.Sid,user.SecurityStamp)
            });

            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtOptions?.Value?.Key));
            var signingCredentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);
            var securityToken = new JwtSecurityToken(
                claims: claims,
                signingCredentials: signingCredentials,
                issuer: _jwtOptions?.Value?.Issuer,
                expires: DateTime.UtcNow.AddSeconds(_jwtOptions.Value.Expiry),
                audience: _jwtOptions?.Value?.Audience
                );
            token = new JwtSecurityTokenHandler().WriteToken(securityToken);

            return await Task.FromResult(token);
        }
        private async Task<string> GenerateRefreshTokenAsync(int byteSize = 64)
        {
            using var cryptoProvider = new RNGCryptoServiceProvider();
            var randomBytes = new byte[byteSize];
            cryptoProvider.GetBytes(randomBytes);
            return await Task.FromResult(Convert.ToBase64String(randomBytes));
        }
    }
}
