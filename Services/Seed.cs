using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using WebAppApi.Data;
using WebApi.Helpers;
namespace WebApi.Services
{
    public class Seed 
    {

        public static async Task Initialize(UserManager<IdentityUser> userManager,RoleManager<IdentityRole> roleManager,WebApiContext dbContext,AppSettings settings)
        {            
            var adminRole = new IdentityRole("admin");
            if(!dbContext.Roles.Any()){
                roleManager.CreateAsync(adminRole).GetAwaiter().GetResult();
            }
            if(!dbContext.Users.Any(u=>u.UserName==settings.AdminUsername)){
                var adminUser = new IdentityUser(){
                    UserName=settings.AdminUsername,
                    Email=settings.AdminEmail
                };
                var result = userManager.CreateAsync(adminUser,settings.AdminPassword).GetAwaiter().GetResult();
                userManager.AddToRoleAsync(adminUser,adminRole.Name).GetAwaiter().GetResult();
            }
        }




    }
}