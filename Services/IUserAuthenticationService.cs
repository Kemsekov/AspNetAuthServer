using System.Threading.Tasks;
using WebApi.Models;
using WebApi.Models.Requests;

namespace WebApi.Services
{
    public interface IUserAuthenticationService
    {
        Task<AuthResult> AuthenticateAsync(AuthenticateRequest authModel);
        Task<AuthResult> RefreshTokenAsync(string refreshToken);
    }
}
