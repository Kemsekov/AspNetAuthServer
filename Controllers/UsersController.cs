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

namespace WebApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UsersController : ControllerBase
    {
        private readonly IEmailSender _emailSender;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly WebApiDbContext _context;

        public UsersController(WebApiDbContext context,
                               UserManager<ApplicationUser> userManager,
                               IEmailSender emailSender)
        {
            _emailSender = emailSender;
            _userManager = userManager;
            _context = context;
        }

        [HttpPost("Authenticate")]
        public IActionResult Authenticate(AuthenticateRequest model)
        {
            throw new NotImplementedException();
        }

        [HttpGet("[action]")]
        public async Task<IActionResult> GetAll()
        {
            var usersAndRoles = _context.Users.ToList().Select(u=>new {User=new SimpleUser(){identityUser = u},Roles= _userManager.GetRolesAsync(u).GetAwaiter().GetResult()});
            return Ok(usersAndRoles);
        }
        [HttpGet("[action]")]
        public async Task<IActionResult> GetAllRaw(){
            var usersAndRoles = _context.Users.ToList().Select(u=>new {User=u,Roles= _userManager.GetRolesAsync(u).GetAwaiter().GetResult()});
            return Ok(usersAndRoles);
        }
        [HttpGet("[action]")]
        public async Task<IActionResult> GetById(string id){
            var user = new SimpleUser(){identityUser= await _userManager.FindByIdAsync(id)};
            if(user!=null)
            return new JsonResult(user){StatusCode = StatusCodes.Status200OK};
            return new JsonResult(new {message="There is no user with such id"}){StatusCode = StatusCodes.Status404NotFound};
        }
        [HttpGet("[action]")]
        public async Task<IActionResult> GetByIdRaw(string id){
            var user = await _userManager.FindByIdAsync(id);
            if(user!=null)
            return new JsonResult(user){StatusCode = StatusCodes.Status200OK};
            return new JsonResult(new {message="There is no user with such id"}){StatusCode = StatusCodes.Status404NotFound};
        }

        [HttpPost("[action]")]
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
        public async Task<IActionResult> SmartUpdate(UpdateUserRequest request){
            using var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled);
            
            ApplicationUser user = null;
            (IActionResult,ApplicationUser) res = await FindUser(request,_userManager);
            if(res.Item1!=null)
                return res.Item1;
            user = res.Item2;

            user.Email=request.Email!=null ? request.Email : user.Email;
            user.UserName=request.UserName!=null ? request.UserName : user.UserName;
            user.PhoneNumber=request.Password!=null ? request.Phone : user.PhoneNumber;
            
            var resultupdate = await _userManager.UpdateAsync(user);
            if(!resultupdate.Succeeded)
            return new JsonResult(new {message="Error while attemt to update user",resultupdate.Errors}){StatusCode=StatusCodes.Status406NotAcceptable};
            
            IList<string> roles = null;
            if(request.AddRoles!=null && request.AddRoles.Any()){
                roles = await _userManager.GetRolesAsync(user);
                var result = await _userManager.AddToRolesAsync(user,request.AddRoles.Except(roles));
            }
            if(request.RemoveRoles!=null && request.RemoveRoles.Any()){
                
                if(roles==null) 
                roles = await _userManager.GetRolesAsync(user);
                var removeRolesThatExistNow = roles.Intersect(request.RemoveRoles);
                var result = await _userManager.RemoveFromRolesAsync(user,removeRolesThatExistNow);
                roles = roles.Except(removeRolesThatExistNow).ToList();
            }
            scope.Complete();
            return Ok(new {Roles = roles,user = new SimpleUser(){identityUser = user}});

        }
        public async Task<IActionResult> Delete(RemoveRequest request){
            using var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled);
            
            ApplicationUser user = null;
            (IActionResult,ApplicationUser) res = await FindUser(request,_userManager);
            if(res.Item1!=null)
                return res.Item1;
            user = res.Item2;

            var result = await _userManager.DeleteAsync(user);
            scope.Complete();
            if(result.Succeeded)
                return Ok(new {message="user deleted"});
            return BadRequest(result.Errors);
        }
        public static async Task<(IActionResult,ApplicationUser)> FindUser(INeedFindUser request,UserManager<ApplicationUser> _userManager){
            var findByLower = request.FindUserBy.ToLower();
            ApplicationUser user = null;
            switch(findByLower){
                case "username":
                    if(string.IsNullOrEmpty(request.UserName)) 
                    return (new JsonResult(new {message=$"Empty ot null {findByLower}"}){StatusCode=StatusCodes.Status400BadRequest},null);
                    user = await _userManager.FindByNameAsync(request.UserName);
                break;
                case "email":
                    if(string.IsNullOrEmpty(request.Email)) 
                    return (new JsonResult(new {message=$"Empty ot null {findByLower}"}){StatusCode=StatusCodes.Status400BadRequest},null);
                    user = await _userManager.FindByEmailAsync(request.Email);
                break;
                case "id" :
                    if(string.IsNullOrEmpty(request.Id)) 
                    return (new JsonResult(new {message=$"Empty ot null {findByLower}"}){StatusCode=StatusCodes.Status400BadRequest},null);
                    user = await _userManager.FindByIdAsync(request.Id);
                break;
                default:
                    return (new JsonResult(new {message="Wrong 'findUserBy' argument. It should be 'user' or 'email' or 'id'"}){StatusCode=StatusCodes.Status400BadRequest},null);
            };
            if(user==null){
              return (new JsonResult(new {message=$"Wrong {findByLower}"}){StatusCode=StatusCodes.Status404NotFound},null);  
            }
            return (null,user);
        }
    }
}
