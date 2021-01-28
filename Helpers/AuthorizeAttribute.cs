using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System;
using WebApi.Entities;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class AuthorizeAttribute : Attribute, IAuthorizationFilter
{
    public string Role{get;set;}


    public void OnAuthorization(AuthorizationFilterContext context)
    {
        var manager =  (UserManager<IdentityUser>)context.HttpContext.Items["userManager"];
        var user = (IdentityUser)context.HttpContext.Items["User"];
        if (user == null)
        {
            // not logged in
            context.Result = new JsonResult(new { message = "Unauthorized" }) { StatusCode = StatusCodes.Status401Unauthorized };
        }

        if(!string.IsNullOrEmpty(Role)){
            if(manager!=null && user!=null)
            if(!manager.IsInRoleAsync(user,Role).GetAwaiter().GetResult()){
                context.Result = new JsonResult(new {message="User is not allowed to this resource"}) {StatusCode=StatusCodes.Status403Forbidden};
            }
        }
    }
}