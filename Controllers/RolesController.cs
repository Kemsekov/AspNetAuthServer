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
using Auth.Models.Requests;
using Microsoft.AspNetCore.Authorization;
using OpenIddict.Validation.AspNetCore;

namespace Auth.Controllers
{
    [ApiController]
    [Route("[controller]/[action]")]
    public class RolesController  : ControllerBase
    {
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly UserManager<ApplicationUser> _userManager;
        public RolesController(RoleManager<IdentityRole> roleManager, UserManager<ApplicationUser> userManager)
        {
            _roleManager = roleManager;
            _userManager = userManager;
        }
        [HttpPost]
        [Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
        [Authorize(Roles="admin")]
        public async Task<IActionResult> Add(CreateRoleRequest request){
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return BadRequest();
            }
            var role = new IdentityRole(request.Name);
            var result = await _roleManager.CreateAsync(role);
            if(!result.Succeeded)
                return new JsonResult(result.Errors) { StatusCode=StatusCodes.Status406NotAcceptable};
            return new JsonResult(new {message="Created",RoleName=request.Name}){StatusCode=StatusCodes.Status201Created};            
        } 
    }
}