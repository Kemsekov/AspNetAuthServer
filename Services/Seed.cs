using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using WebApi.Contexts;
using WebApi.Entities;
using WebApi.Options;

namespace WebApi.Services
{
    public class Seed 
    {

        public static async Task Initialize(UserManager<ApplicationUser> userManager,RoleManager<IdentityRole> roleManager,WebApiDbContext dbContext,AdminOptions settings)
        {            
            var roles = new []{
             new IdentityRole("admin"),
             new IdentityRole("moderator"),
             new IdentityRole("user"),
             new IdentityRole("manager")
            };
            if(!dbContext.Roles.Any()){
                foreach(var role in roles)
                await roleManager.CreateAsync(role);;
            }
            if(!dbContext.Users.Any(u=>u.UserName=="admin")){
                var adminUser = new ApplicationUser(){
                    UserName="admin",
                    Email=settings.Email
                };
                var result = await userManager.CreateAsync(adminUser,settings.Password);
                if(!result.Succeeded){
                    foreach(var err in result.Errors){
                    System.Console.BackgroundColor = ConsoleColor.Red;
                    System.Console.WriteLine(err.Description);
                    }
                    System.Console.ResetColor();
                    throw new Exception();
                }
                    
                userManager.AddToRoleAsync(adminUser,"admin").GetAwaiter().GetResult();
            }
        }




    }
}