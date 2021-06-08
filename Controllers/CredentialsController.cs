using System;
using System.Threading.Tasks;
using Auth.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using OpenIddict.Abstractions;
using OpenIddict.Validation.AspNetCore;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace Auth.Controllers
{
    [ApiController]
    public class CredentialsController : ControllerBase
    {
        private readonly IOpenIddictApplicationManager _openIddictManager;
        private readonly UserManager<ApplicationUser> _userManager;
        public CredentialsController(
            IOpenIddictApplicationManager openIddictManager, 
            UserManager<ApplicationUser> userManager)
        {
            _openIddictManager = openIddictManager;
            _userManager = userManager;
        }
        [Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
        [Authorize(Roles="admin")]
        [HttpPost("~/connect/credentials"), Produces("application/json")]
        public async Task<IActionResult> AddCredentialsAsync(){
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return BadRequest();
            }
            throw new NotImplementedException();
        } 
    }
}