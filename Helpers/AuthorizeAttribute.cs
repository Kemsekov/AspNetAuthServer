using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System;
using System.Collections.Generic;
using WebApi.Entities;
using System.Linq;
using WebApi.Models.Roles;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class AuthorizeAttribute : Attribute, IAuthorizationFilter
{
    public AuthorizeAttribute()
    {
    }
    /// <summary>
    /// Value that represents roles statement
    /// Like this:
    /// <code>Roles = "admin or user moderator"</code>
    /// So user with admin role or with user and moderator roles would be able to get access
    /// to this action
    /// </summary>
    /// <returns>returns last element of Roles or default</returns>
    public string Roles{
        set{
        if(!string.IsNullOrEmpty(value)){
            try{
                var chain = new RoleStatement();
                RolesChain = chain;

                var rolesStatement = value.Split("or");
                foreach(var andRoles in rolesStatement){
                    foreach(var role in andRoles.Split(' '))
                        if(!string.IsNullOrEmpty(role))
                        chain.AndRoles.Add(role);
                    chain.NextOrRole = new RoleStatement();
                    chain=chain.NextOrRole;
                }
            }
            catch (IndexOutOfRangeException ex){
                System.Console.WriteLine("Error syntax in AuthorizeAttribute\n");
            }
            }
        }
        get=>"";
        }
    RoleStatement RolesChain{get;set;}
    public void OnAuthorization(AuthorizationFilterContext context)
    {
        var manager =  (UserManager<IdentityUser>)context.HttpContext.Items["userManager"];

        var user = (IdentityUser)context.HttpContext.Items["User"];
        
        if (user == null)
        {
            // not logged in
            context.Result = new JsonResult(new { message = "Unauthorized" }) { StatusCode = StatusCodes.Status401Unauthorized };
            return;
        }
        
        //if we need to validate user's role
        if(RolesChain.AndRoles.Any()){
            var user_roles = manager.GetRolesAsync(user).GetAwaiter().GetResult();
            for(var chain = RolesChain;chain!=null && chain.AndRoles.Any();chain=chain.NextOrRole){
                if(!chain.AndRoles.Except(user_roles).Any()){
                    return;
                }
                else{
                    if(chain.NextOrRole!=null)
                    continue;
                    context.Result = new JsonResult(new {message="Access denied"}) {StatusCode=StatusCodes.Status403Forbidden};
                    return;
                }
            }
        }
    }
}