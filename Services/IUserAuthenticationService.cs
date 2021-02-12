using System.Threading.Tasks;
using WebApi.Entities;
using WebApi.Models;
using WebApi.Models.Requests;

namespace WebApi.Services
{
    public interface IUserAuthenticationService
    {
        Task<AuthResult> AuthenticateAsync(AuthenticateRequest authModel);
        Task<string> RefreshTokenAsync(string refreshToken);
        Task<string> UpdateTokenAsync(string userid,string securityStampFromJWToken);
    }
}
