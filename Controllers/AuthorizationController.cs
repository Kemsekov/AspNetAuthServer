using Microsoft.AspNetCore.Mvc;
using Auth.Models;
//using Auth.Services;
using System.Linq;
using Microsoft.AspNetCore.Identity;
using Auth.Entities;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Http;
using System.Collections.Generic;
using System.Transactions;
using System;
using Auth.Models.Requests;
using Auth.Contexts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using OpenIddict.Abstractions;
using Microsoft.AspNetCore;
using OpenIddict.Server.AspNetCore;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace Auth.Controllers
{
    [ApiController]
    //[Route("connect/[action]")]
    public class AuthorizationController : ControllerBase
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IOpenIddictApplicationManager _openIddictManager;
        private readonly AuthDbContext _context;

        public AuthorizationController(
            SignInManager<ApplicationUser> signInManager,
            UserManager<ApplicationUser> userManager,
            IOpenIddictApplicationManager openIddictManager, AuthDbContext context)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _openIddictManager = openIddictManager;
            _context = context;
        }
        
        [HttpPost("~/connect/token"), Produces("application/json")]
        public async Task<IActionResult> Exchange()
        {
            var request = HttpContext.GetOpenIddictServerRequest() ??
               throw new InvalidOperationException("The OpenID Connect request cannot be retrieved.");
            if(request.IsPasswordGrantType()){
                var validation = await ValidateUsernameAndPassword(request);
                if(validation.Errors is not null)
                    return validation.Errors;
                var user = validation.User;

                //e.g object that will be turned into access_token
                var principal = await _signInManager.CreateUserPrincipalAsync(user);

                var scopes = request.GetScopes().ToList();

                
                principal.SetScopes(new[]
                {
                    Scopes.OpenId,
                    Scopes.Email,
                    Scopes.Profile,
                    Scopes.OfflineAccess,
                    Scopes.Roles
                }.Intersect(scopes));

                //adds to jwt claim "123" : "124"
                //principal.SetClaim("123","124");

                foreach (var claim in principal.Claims)
                {
                    claim.SetDestinations(GetDestinations(claim, principal));
                }
                var ticket = new AuthenticationTicket(
                     principal,
                     new AuthenticationProperties(),
                     OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
                
                return SignIn(ticket.Principal, ticket.Properties, ticket.AuthenticationScheme);
            }

            if(request.IsRefreshTokenGrantType()){
                var info = await HttpContext.AuthenticateAsync(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
                
                var res = await ValidateAuthenticateResult(info);
                if(res.Errors is not null)
                    return res.Errors;
                var user = res.User;

                var principal = await _signInManager.CreateUserPrincipalAsync(user);
                
                foreach (var claim in principal.Claims)
                {
                    claim.SetDestinations(GetDestinations(claim, principal));
                }
                return SignIn(principal, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);

            }
            throw new NotImplementedException("The specified grant type is not implemented.");
            
        #region Helpers
        async Task<(IActionResult Errors,ApplicationUser User)> ValidateUsernameAndPassword(OpenIddictRequest request){
                var user = await _userManager.FindByNameAsync(request.Username);
                if (user == null)
                {
                    var properties = new AuthenticationProperties(new Dictionary<string, string>
                    {
                        [OpenIddictServerAspNetCoreConstants.Properties.Error] = Errors.InvalidGrant,
                        [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] =
                            "The username/password couple is invalid."
                    });

                    return (Forbid(properties, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme),null);
                }

                // Validate the username/password parameters and ensure the account is not locked out.
                var result = await _signInManager.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: true);
                if (!result.Succeeded)
                {
                    var properties = new AuthenticationProperties(new Dictionary<string, string>
                    {
                        [OpenIddictServerAspNetCoreConstants.Properties.Error] = Errors.InvalidGrant,
                        [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] =
                            "The username/password couple is invalid."
                    });

                    return (Forbid(properties, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme),null);
                }
                return (null,user);
        }
        async Task<(IActionResult Errors,ApplicationUser User)> ValidateAuthenticateResult(AuthenticateResult info){
            var user = await _userManager.GetUserAsync(info.Principal);
            if (user == null)
            {
                var properties = new AuthenticationProperties(new Dictionary<string, string>
                {
                    [OpenIddictServerAspNetCoreConstants.Properties.Error] = Errors.InvalidGrant,
                    [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] = "The refresh token is no longer valid."
                });
                return (Forbid(properties, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme),null);
            }
            // Ensure the user is still allowed to sign in.
            if (!await _signInManager.CanSignInAsync(user))
            {
                var properties = new AuthenticationProperties(new Dictionary<string, string>
                {
                    [OpenIddictServerAspNetCoreConstants.Properties.Error] = Errors.InvalidGrant,
                    [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] = "The user is no longer allowed to sign in."
                });
                return (Forbid(properties, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme),null);
            }
            return (null,user);
        }
        #endregion
        }


        private IEnumerable<string> GetDestinations(Claim claim, ClaimsPrincipal principal)
        {
            // Note: by default, claims are NOT automatically included in the access and identity tokens.
            // To allow OpenIddict to serialize them, you must attach them a destination, that specifies
            // whether they should be included in access tokens, in identity tokens or in both.

            switch (claim.Type)
            {
                case Claims.Name:
                    yield return Destinations.AccessToken;

                    if (principal.HasScope(Scopes.Profile))
                        yield return Destinations.IdentityToken;

                    yield break;

                case Claims.Email:
                    yield return Destinations.AccessToken;

                    if (principal.HasScope(Scopes.Email))
                        yield return Destinations.IdentityToken;

                    yield break;

                case Claims.Role:
                    yield return Destinations.AccessToken;

                    if (principal.HasScope(Scopes.Roles))
                        yield return Destinations.IdentityToken;

                    yield break;
                // Never include the security stamp in the access and identity tokens, as it's a secret value.
                case "AspNet.Identity.SecurityStamp": yield break;

                default:
                    yield return Destinations.AccessToken;
                    yield break;
            }
        }
    }
}
