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
using System.Transactions;
using System;
using WebApi.Models.Requests;
using WebApi.Contexts;
using Microsoft.AspNetCore.Authorization;
using WebApi.Helpers;
using Microsoft.EntityFrameworkCore;

namespace WebApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UsersController : ControllerBase
    {
        private readonly IUserAuthenticationService _authentication;
        private readonly IEmailSender _emailSender;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly WebApiDbContext _context;

        public UsersController(WebApiDbContext context,
                               UserManager<ApplicationUser> userManager,
                               IEmailSender emailSender,
                               IUserAuthenticationService authentication)
        {
            _authentication = authentication;
            _emailSender = emailSender;
            _userManager = userManager;
            _context = context;
        }

        [HttpPost("[action]")]
        public async Task<IActionResult> Authenticate(AuthenticateRequest model)
        {
            var result = await _authentication.AuthenticateAsync(model);
            if(result.IsAuthenticated)
                return Ok(result);
            else
                return new JsonResult(new {message="Wrong username or password"});
        }

        [HttpPost("[action]")]
        public async Task<IActionResult> RefreshAcessToken(RefreshAcessTokenRequest request){
            var result = await _authentication.RefreshTokenAsync(request.RefreshToken);
            if(result.IsAuthenticated)
                return Ok(result);
            else
                return new JsonResult(new {message="Wrong username or password"});
        }
        
        [HttpGet("[action]")]
        [Authorize(Roles="admin,modifier")]
        public async Task<IActionResult> GetAll()
        {
            var usersAndRoles = _context.Users
                .Join(_context.UserRoles,
                    u=>u.Id,
                    ur=>ur.UserId,
                    (u,ur)=>new {User=u,RoleId=ur.RoleId})
                .Join(_context.Roles,
                    u=>u.RoleId,
                    r=>r.Id,
                    (u,r)=>new {User=u.User,RoleName=r.Name})
                .ToList();
            
            var result = new List<(ApplicationUser User,List<string> Roles)>();

            usersAndRoles.ForEach(el=>{
                if (result.Find(t=>t.User==el.User) is (ApplicationUser,List<string>) found){
                    found.Roles ??= new List<string>();
                    found.Item2.Add(el.RoleName);
                }
                else{
                    var Roleslist = new List<string>();
                    Roleslist.Add(el.RoleName);
                    result.Add((el.User,Roleslist));
                }
            });
            return Ok(result.Select(res=>new {User=new SimpleUser{identityUser= res.User},Roles=res.Roles}));
        }
        [HttpGet("[action]")]
        [Authorize(Roles="admin,modifier")]
        public async Task<IActionResult> GetAllRaw(){
            var usersAndRoles = _context.Users
                .Join(_context.UserRoles,
                    u=>u.Id,
                    ur=>ur.UserId,
                    (u,ur)=>new {User=u,RoleId=ur.RoleId})
                .Join(_context.Roles,
                    u=>u.RoleId,
                    r=>r.Id,
                    (u,r)=>new {User=u.User,RoleName=r.Name})
                .ToList();
            
            var result = new List<(ApplicationUser User,List<string> Roles)>();

            usersAndRoles.ForEach(el=>{
                if (result.Find(t=>t.User==el.User) is (ApplicationUser,List<string>) found){
                    found.Roles ??= new List<string>();
                    found.Item2.Add(el.RoleName);
                }
                else{
                    var Roleslist = new List<string>();
                    Roleslist.Add(el.RoleName);
                    result.Add((el.User,Roleslist));
                }
            });
            return Ok(result.Select(res=>new {User=res.User,Roles=res.Roles}));
            
        }
        [HttpGet("[action]")]
        [Authorize(Roles="admin,modifier")]
        public async Task<IActionResult> GetById(string id){
            var user = new SimpleUser(){identityUser= await _userManager.FindByIdAsync(id)};
            if(user!=null)
            return new JsonResult(user){StatusCode = StatusCodes.Status200OK};
            return new JsonResult(new {message="There is no user with such id"}){StatusCode = StatusCodes.Status404NotFound};
        }
        [HttpGet("[action]")]
        [Authorize(Roles="admin,modifier")]
        public async Task<IActionResult> GetByIdRaw(string id){
            var user = await _userManager.FindByIdAsync(id);
            if(user!=null)
            return new JsonResult(user){StatusCode = StatusCodes.Status200OK};
            return new JsonResult(new {message="There is no user with such id"}){StatusCode = StatusCodes.Status404NotFound};
        }

        [HttpPost("[action]")]
        [Authorize(Roles="admin,modifier")]
        public async Task<IActionResult> Add(CreateUserRequest user){
            var identityUser = new ApplicationUser(){
                Email = user.Email,
                UserName = user.UserName,
                PhoneNumber = user.Phone
            };
            using var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled);
            var result = await _userManager.CreateAsync(identityUser,user.Password);
            if(result.Succeeded){
                identityUser = await _userManager.FindByEmailAsync(identityUser.Email);
                var verificationCode = await _userManager.GenerateEmailConfirmationTokenAsync(identityUser);
                scope.Complete();
                return new JsonResult(new {message="Success",
                 email_verification_code = verificationCode,
                 User = new SimpleUser(){identityUser = identityUser}}) 
                 {StatusCode = StatusCodes.Status201Created};
            }
            scope.Complete();
            return new JsonResult(result.Errors){StatusCode = StatusCodes.Status400BadRequest};
        }
        [HttpPost("[action]")]
        [Authorize(Roles="admin,modifier")]
        public async Task<IActionResult> SmartUpdate(UpdateUserRequest request){
            using var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled);
            ApplicationUser user = null;
            (IActionResult, IQueryable<ApplicationUser>) res = await FindUser(request,_userManager);
            if(res.Item1!=null)
                return res.Item1;
            user = res.Item2.FirstOrDefault();
            if(user is null)
                return new JsonResult(new {message="User is not found"}) {StatusCode=StatusCodes.Status404NotFound};

            user.Email=request.Email ?? user.Email;
            user.UserName=request.UserName ?? user.UserName;
            user.PhoneNumber=request.Phone ?? user.PhoneNumber;
            
            var resultupdate = await _userManager.UpdateAsync(user);
            if(!resultupdate.Succeeded)
                return new JsonResult(new {message="Error while attempt to update user",resultupdate.Errors}){StatusCode=StatusCodes.Status406NotAcceptable};
            IList<string> roles = null;
            if(request.AddRoles is not null && request.AddRoles.Any()){
                roles = await _userManager.GetRolesAsync(user);
                var result = await _userManager.AddToRolesAsync(user,request.AddRoles.Except(roles));
            }
            if(request.RemoveRoles is not null && request.RemoveRoles.Any()){
                if(roles is null)
                roles = await _userManager.GetRolesAsync(user);
                var removeRolesThatExistNow = roles.Intersect(request.RemoveRoles);
                var result = await _userManager.RemoveFromRolesAsync(user,removeRolesThatExistNow);
            }
            scope.Complete();
            return Ok(new {Roles = await _userManager.GetRolesAsync(user),user = new SimpleUser(){identityUser = user}});
        }
        [HttpDelete("[action]")]
        [Authorize(Roles="admin,modifier")]
        public async Task<IActionResult> Delete(RemoveRequest request){
            using var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled);
            
            ApplicationUser user = null;
            (IActionResult,IQueryable<ApplicationUser>) res = await FindUser(request,_userManager);
            if(res.Item1!=null)
                return res.Item1;
            user = res.Item2.FirstOrDefault();
            if(user is null)
                return new JsonResult(new {message="User is not found"}){StatusCode=StatusCodes.Status404NotFound};

            var result = await _userManager.DeleteAsync(user);
            scope.Complete();
            if(result.Succeeded)
                return Ok(new {message="user deleted"});
            return BadRequest(result.Errors);
        }
        [NonAction]
        public static async Task<(IActionResult, IQueryable<ApplicationUser>)> FindUser(INeedFindUser request,UserManager<ApplicationUser> _userManager){
            var findByLower = request.FindUserBy.ToLower();
            var query = _userManager.Users.AsQueryable();
            switch(findByLower){
                case "username":
                    if(string.IsNullOrEmpty(request.UserName)) 
                    return (new JsonResult(new {message=$"Empty or null {findByLower}"}){StatusCode=StatusCodes.Status400BadRequest},null);
                    query = query.Where(u=>u.UserName==request.UserName);
                break;
                case "email":
                    if(string.IsNullOrEmpty(request.Email)) 
                    return (new JsonResult(new {message=$"Empty or null {findByLower}"}){StatusCode=StatusCodes.Status400BadRequest},null);
                    query = query.Where(u=>u.Email==request.Email);
                break;
                case "id" :
                    if(string.IsNullOrEmpty(request.Id)) 
                    return (new JsonResult(new {message=$"Empty or null {findByLower}"}){StatusCode=StatusCodes.Status400BadRequest},null);
                    query = query.Where(u=>u.Id==request.Id);
                break;
                default:
                    return (new JsonResult(new {message="Wrong 'findUserBy' argument. It should be 'user' or 'email' or 'id'"}){StatusCode=StatusCodes.Status400BadRequest},null);
            };
            return (null,query);
        }
    }
}
