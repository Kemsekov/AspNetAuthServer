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
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace WebApi.Controllers
{
    [ApiController]
    [Route("[controller]/[action]")]
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

        [HttpPost]
        public async Task<IActionResult> Authenticate(AuthenticateRequest model)
        {
            var result = await _authentication.AuthenticateAsync(model);
            if(result.IsAuthenticated)
                return Ok(result);
            else
                return new JsonResult(new {message="Wrong username or password"});
        }
        [HttpGet]
        [Authorize(Roles="admin")]
        public async Task<IActionResult> FindRaw(FindWhereRequest request){
            ApplicationUser user = null;
            if(!string.IsNullOrEmpty(request.Id)){
                user = await _context.Users.FindAsync(request.Id);
            }
            if(!string.IsNullOrEmpty(request.UserName)){
                request.UserName = request.UserName.ToUpper(); 
                user = await _context.Users.FirstOrDefaultAsync(u=>u.NormalizedUserName==request.UserName);
            }
            if(!string.IsNullOrEmpty(request.Email)){
                request.Email = request.Email.ToUpper(); 
                user = await _context.Users.FirstOrDefaultAsync(u=>u.NormalizedEmail==request.Email);
            }
            if(user is null)
            return NotFound(new {message="user not found"});

            return new JsonResult(new {user,Roles=await _userManager.GetRolesAsync(user)}) { StatusCode=StatusCodes.Status302Found};
        }
        [HttpGet]
        [Authorize(Roles="admin,moderator")]
        public async Task<IActionResult> Find(FindWhereRequest request){
            ApplicationUser user = null;
            if(!string.IsNullOrEmpty(request.Id)){
                user = await _context.Users.FindAsync(request.Id);
            }
            if(!string.IsNullOrEmpty(request.UserName)){
                request.UserName = request.UserName.ToUpper(); 
                user = await _context.Users.FirstOrDefaultAsync(u=>u.NormalizedUserName==request.UserName);
            }
            if(!string.IsNullOrEmpty(request.Email)){
                request.Email = request.Email.ToUpper(); 
                user = await _context.Users.FirstOrDefaultAsync(u=>u.NormalizedEmail==request.Email);
            }

            if(user is null)
            return NotFound(new {message="user not found"});

            return new JsonResult(new {User = new SimpleUser{identityUser= user},Roles = await _userManager.GetRolesAsync(user)}) { StatusCode=StatusCodes.Status302Found};
        }

        [HttpPost]
        public async Task<IActionResult> GetAccessToken(RefreshAcessTokenRequest request){
            var result = await _authentication.RefreshTokenAsync(request.RefreshToken);
            if(!string.IsNullOrEmpty(result))
                return Ok(new {AccessToken=result});
            else
                return new JsonResult(new {message="Wrong RefreshToken"});
        }
        
        [HttpGet]
        [Authorize(Roles="admin,modifier")]
        public async Task<IActionResult> GetAll()
        {
            var roles = _context.Roles.ToList();
            var userRoles = _context.UserRoles.ToList();
            var result = _context.Users
                        .ToList()
                        .Select(u=>
                            new {
                            User = new SimpleUser{identityUser = u},
                            Roles = userRoles
                                .Where(ur=>ur.UserId==u.Id)
                                .Select(ur=>roles
                                    .Where(r=>r.Id==ur.RoleId)
                                    .FirstOrDefault().Name)
                            });
            return Ok(result);
        }

        [HttpGet]
        [Authorize(Roles="admin")]
        public async Task<IActionResult> GetAllRaw(){

            var roles = _context.Roles.ToList();
            var userRoles = _context.UserRoles.ToList();
            var result = _context.Users
                        .ToList()
                        .Select(u=>
                            new {
                            User = u,
                            Roles = userRoles
                                .Where(ur=>ur.UserId==u.Id)
                                .Select(ur=>roles
                                    .Where(r=>r.Id==ur.RoleId)
                                    .FirstOrDefault())
                            });
            return Ok(result);
        }
        [HttpGet]
        [Authorize]
        public async Task<IActionResult> GetMyself(){
            var userName =  this.User.Claims.Where(c=>c.Type == ClaimTypes.Name).Select(n=>n.Value).FirstOrDefault();
            var email = User.Claims.Where(c=>c.Type==ClaimTypes.Email).FirstOrDefault().Value;
            var roles = User.Claims.Where(c=>c.Type==ClaimTypes.Role).Select(r=>r.Value).ToList();
            var id = User.Claims.Where(c=>c.Type==ClaimTypes.NameIdentifier).FirstOrDefault().Value;
            var user = await _userManager.FindByIdAsync(id);
            return new JsonResult(new {UserName=userName,Name=user.Name,Email=email,Roles=roles}){StatusCode = StatusCodes.Status200OK};
        }
        [HttpGet]
        [Authorize(Roles="admin")]
        public async Task<IActionResult> GetMyselfRaw(){
            var id = User.Claims.Where(c=>c.Type==ClaimTypes.NameIdentifier).FirstOrDefault().Value;
            var user = await _userManager.FindByIdAsync(id);
            return new JsonResult(new {User=user,Claims=User.Claims.Select(c=>new {ValueType = c.ValueType,Value=c.Value})}){StatusCode = StatusCodes.Status200OK};
        }

        [HttpPost]
        [Authorize(Roles="admin,moderator")]
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
        [HttpPost]
        [Authorize(Roles="admin,moderator")]
        public async Task<IActionResult> Update(UpdateUserRequest request){
            using var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled);
            try{
            ApplicationUser user = null;
            (IActionResult, IQueryable<ApplicationUser>) res = await FindUser(request,_userManager);
            if(res.Item1!=null){
                return res.Item1;
            }
            user = res.Item2.FirstOrDefault();
            if(user is null){
                return new JsonResult(new {message="User is not found"}) {StatusCode=StatusCodes.Status404NotFound};
            }
            user.Email=request.Email ?? user.Email;
            user.UserName=request.UserName ?? user.UserName;
            user.PhoneNumber=request.Phone ?? user.PhoneNumber;
            
            var resultupdate = await _userManager.UpdateAsync(user);
            if(!resultupdate.Succeeded){
                return new JsonResult(new {message="Error while attempt to update user",resultupdate.Errors}){StatusCode=StatusCodes.Status406NotAcceptable};
            }
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
            return Ok(new {Roles = await _userManager.GetRolesAsync(user),user = new SimpleUser(){identityUser = user}});
            }
            finally{
                scope.Complete();
            }
        }
        [HttpDelete]
        [Authorize(Roles="admin,moderator")]
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
