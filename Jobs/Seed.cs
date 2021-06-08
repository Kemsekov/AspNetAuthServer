using System;
using System.Net.Mime;
using System.Threading.Tasks;
using Auth.Contexts;
using Auth.Entities;
using Microsoft.AspNetCore.Identity;

namespace Auth.Jobs
{
    ///<summary>
    ///Seed users table with data
    ///</summary>
    public class Seed
    {
        public class User{
            public string UserName{get;set;}
            public string Password{get;set;}
            public string Email{get;set;}
            public string[] Roles{get;set;}
        }
        public User[] Users{get;set;}
        public async Task Initialize(UserManager<ApplicationUser> userManager,RoleManager<IdentityRole> roleManager){
            foreach(var usr in Users){
                var result = await userManager.CreateAsync(new ApplicationUser(){UserName=usr.UserName,Email=usr.Email},usr.Password);
                if(result.Succeeded){
                    var user = await userManager.FindByNameAsync(usr.UserName);
                    if(user != null){
                        user.EmailConfirmed=true;
                        foreach (var role in usr.Roles){
                            if(!await roleManager.RoleExistsAsync(role))
                                await roleManager.CreateAsync(new IdentityRole(role));
                           await userManager.AddToRoleAsync(user,role);
                        }
                    }
                }
            }
        }
    }
}