using Microsoft.AspNetCore.Mvc;
using WebApi.Models;
using WebApi.Services;
using System.Linq;
using Microsoft.AspNetCore.Identity;
using WebApi.Entities;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Http;
using System.Collections.Generic;
using WebApi.Models.Requests;
using Microsoft.AspNetCore.Authorization;

namespace WebApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class RolesController  : ControllerBase
    {
        private readonly RoleManager<IdentityRole> _roleManager;

        public RolesController(RoleManager<IdentityRole> roleManager)
        {
            _roleManager = roleManager;
        }   
        [HttpPost("[action]")]
        [Authorize(Roles="admin")]
        public async Task<IActionResult> Add(CreateRoleRequest request){
            var role = new IdentityRole(request.Name);
            var result = await _roleManager.CreateAsync(role);
            if(!result.Succeeded)
                return new JsonResult(result.Errors) { StatusCode=StatusCodes.Status406NotAcceptable};
            return new JsonResult(new {message="Created",RoleName=request.Name}){StatusCode=StatusCodes.Status201Created};            
        } 
    }
}