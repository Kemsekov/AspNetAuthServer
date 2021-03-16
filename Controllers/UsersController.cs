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
        public async Task<IActionResult> FindRaw(FindUser request){
            var res = request.GetUser(_context);
            if(res==null)
                return new JsonResult(request.Errors){StatusCode = StatusCodes.Status406NotAcceptable};
            ApplicationUser user = await res.FirstOrDefaultAsync();
            if(user==null)
                return new JsonResult(new {message="User not found"}){StatusCode = StatusCodes.Status404NotFound};
            return new JsonResult(new {User = user,Roles = await _userManager.GetRolesAsync(user)}) { StatusCode=StatusCodes.Status302Found};
        }
        [HttpGet]
        [Authorize(Roles="admin,moderator")]
        public async Task<IActionResult> Find(FindUser request){
            var res = request.GetUser(_context);
            if(res==null)
                return new JsonResult(request.Errors){StatusCode = StatusCodes.Status406NotAcceptable};
            ApplicationUser user = await res.FirstOrDefaultAsync();
            if(user==null)
                return new JsonResult(new {message="User not found"}){StatusCode = StatusCodes.Status404NotFound};
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
        [Authorize]
        public async Task<IActionResult> UpdateAccessToken(){
            var userid = User.Claims.FirstOrDefault(c=>c.Type==ClaimTypes.NameIdentifier).Value;
            var securityStamp = User.Claims.FirstOrDefault(c=>c.Type==ClaimTypes.Sid).Value;
            
            var token = await _authentication.UpdateTokenAsync(userid,securityStamp);
            if(token==null){
                return new JsonResult(new {message="Token Denied. Authenticate again."}){StatusCode=StatusCodes.Status406NotAcceptable};
            }
            return Ok(new {token});
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
            return new JsonResult(result.Errors){StatusCode = StatusCodes.Status400BadRequest};
        }
        [HttpPost]
        [Authorize(Roles="admin,moderator")]
        public async Task<IActionResult> Update(UpdateUserRequest request){
            bool IsCredentialsChanged = request.UserName!=null || request.changePassword!=null;

            using var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled);
            
            //find user
            var res = request.findUser.GetUser(_context);
            if(res==null)
                return new JsonResult(request.findUser.Errors){StatusCode=StatusCodes.Status406NotAcceptable};
            
            ApplicationUser user = await res.FirstOrDefaultAsync();
            //if user not found return not found. 100% logic
            if(user==null)
                return new JsonResult(new {message="User not found"}){StatusCode = StatusCodes.Status404NotFound};
            
            //if value in request is present, then we update it in user 
            user.Email=request.Email ?? user.Email;
            user.UserName=request.UserName ?? user.UserName;
            user.PhoneNumber=request.Phone ?? user.PhoneNumber;
            user.Name = request.Name ?? user.Name;

            //summary errors from change password attempt and update user attempt
            List<IdentityError> summary = new List<IdentityError>();

            //try to update user and if error fill summary with errors
            var resultupdate = await _userManager.UpdateAsync(user);
            if(!resultupdate.Succeeded){
                summary.AddRange(resultupdate.Errors ?? new List<IdentityError>());
            }

            //try to change password if needed. if error fill summary with errors
            if(request.changePassword is not null){
                var pass_change = await _userManager.ChangePasswordAsync(user,request.changePassword.OldPassword ?? "",request.changePassword.NewPassword ?? "");
                if(!pass_change.Succeeded)
                    summary.AddRange(pass_change.Errors ?? new List<IdentityError>());
            }
            

            //here we try to add/remove roles of user if possible
            IList<string> roles = null;
            if(request.AddRoles is not null && request.AddRoles.Any()){
                roles = await _userManager.GetRolesAsync(user);
                var result = await _userManager.AddToRolesAsync(user,request.AddRoles.Except(roles));
                
                if(!result.Succeeded)
                    summary.AddRange(result.Errors);
            }
            if(request.RemoveRoles is not null && request.RemoveRoles.Any()){
                if(roles is null)
                roles = await _userManager.GetRolesAsync(user);

                //we need to ensure that we remove roles which we can remove and skip other
                var RolesThatUserNowBelongsTo = roles.Intersect(request.RemoveRoles);
                var result = await _userManager.RemoveFromRolesAsync(user,RolesThatUserNowBelongsTo);
                
                if(!result.Succeeded)
                    summary.AddRange(result.Errors);
            }

            //if there were any error we return errors and dispose scope to cancel tansaction
            if(summary.Count>0){
            scope.Dispose();
            return new JsonResult(new {message="Error while attempt to update user",summary}){StatusCode=StatusCodes.Status406NotAcceptable};
            }
            //cause we updated the user's Credentials we also need to remove current refresh token
            if(IsCredentialsChanged)
                _context.RefreshTokens.Remove(await _context.RefreshTokens.FirstOrDefaultAsync(t=>t.UserId==user.Id));
            await _userManager.UpdateSecurityStampAsync(user);
            await _context.SaveChangesAsync();
            scope.Complete();
            scope.Dispose();
            return Ok(new {Roles = await _userManager.GetRolesAsync(user),user = new SimpleUser(){identityUser = user}});
        }
        [HttpDelete]
        [Authorize(Roles="admin,moderator")]
        public async Task<IActionResult> Delete(FindUser request){
            using var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled);
            var res = request.GetUser(_context);
            if(res==null)
                return new JsonResult(new {request.Errors}){StatusCode=StatusCodes.Status406NotAcceptable};
            ApplicationUser user = await res.FirstOrDefaultAsync();
            if(user == null)
                return new JsonResult(new {message="User not found"}){StatusCode = StatusCodes.Status404NotFound};
            var result = await _userManager.DeleteAsync(user);
            scope.Complete();
            if(result.Succeeded)
                return Ok(new {message="User deleted"});
            return BadRequest(result.Errors);
        }

    }
}
