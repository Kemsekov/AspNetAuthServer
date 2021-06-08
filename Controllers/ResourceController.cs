using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Auth.Entities;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using OpenIddict.Server.AspNetCore;
using OpenIddict.Validation.AspNetCore;

namespace Auth.Controllers.Entities
{
    [ApiController]
    [Route("[controller]/[action]")]
    [Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
    public class ResourceController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        public ResourceController(UserManager<ApplicationUser> _userManager)
        {
            this._userManager = _userManager;

        }
        [Route("~/resource/")]
        [Route("~/resource/get")]
        [HttpGet,Produces("application/json")]
        public IActionResult Get()
        {
            return Ok($"Resource of {User.Identity.Name}");
        }
    }
}