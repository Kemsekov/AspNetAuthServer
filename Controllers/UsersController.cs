using Microsoft.AspNetCore.Mvc;
using WebApi.Models;
using WebApi.Services;
using WebAppApi.Data;
using System.Linq;
using Microsoft.AspNetCore.Identity;
using WebApi.Entities;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Http;
using System.Collections.Generic;

namespace WebApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UsersController : ControllerBase
    {
        private readonly IEmailSender _emailSender;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly WebApiContext _context;
        private readonly IAuthenticateService _authenticateService;

        public UsersController(IAuthenticateService authenticateService,
                               WebApiContext context,
                               UserManager<IdentityUser> userManager,
                               IEmailSender emailSender)
        {
            _emailSender = emailSender;
            _userManager = userManager;
            _context = context;
            _authenticateService = authenticateService;
        }

        [HttpPost("Authenticate")]
        public IActionResult Authenticate(AuthenticateRequest model)
        {
            var response = _authenticateService.Authenticate(model);

            if (response == null)
                return BadRequest(new { message = "Username/Email or password is incorrect" });
            return Ok(response);
        }

        [HttpGet("[action]")]
        [Authorize(Roles="admin or modifier user")]
        public async Task<IActionResult> GetAll()
        {
            var users = _userManager.Users.Select(usr => new User(){identityUser = usr});
            return Ok(users);
        }
        [HttpGet("[action]")]
        public async Task<IActionResult> GetAllRaw(){
            var users = _userManager.Users;
            return Ok(users);
        }
        [HttpGet("[action]")]
        [Authorize(Roles="admin or modifier user")]
        public async Task<IActionResult> GetById(string id){
            var user = new User(){identityUser= await _userManager.FindByIdAsync(id)};
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
        [Authorize(Roles="admin")]
        public async Task<IActionResult> Add(CreateUserRequest user){
            var identityUser = new IdentityUser(){
                Email = user.Email,
                UserName = user.UserName,
                PhoneNumber = user.Phone
            };
            var result = await _userManager.CreateAsync(identityUser,user.Password);
            if(result.Succeeded){
                identityUser = await _userManager.FindByEmailAsync(identityUser.Email);
                var verificationCode = await _userManager.GenerateEmailConfirmationTokenAsync(identityUser);
                return new JsonResult(new {message="Success",
                 email_verification_code = verificationCode,
                 User = new User(){identityUser = identityUser}}) 
                 {StatusCode = StatusCodes.Status201Created};
            }
            else 
                return new JsonResult(result.Errors){StatusCode = StatusCodes.Status400BadRequest};
        }
        [HttpPost("[action]")]
        [Authorize(Roles="admin")]
        //this update action is called smart because of unstrict requirements to
        //request. You may choose what way to find user you need, by id, username or email.
        //You can specify properties such as Email or Phone if you need and not specify if you
        //don't need them. You may specify AddRoles and RemoveRoles
        //as you please, this method will find what roles can be added and what
        //roles can be removed and ignore anything else,
        //so you don't need to remember user's roles list to be sure that
        //you can remove some role or add.
        //Very handy tool slow as f. A lot of database queries.
        public async Task<IActionResult> SmartUpdate(UpdateUserRequest request){
            var findByLower = request.FindUserBy.ToLower();
            IdentityUser user = null;
            switch(findByLower){
                case "username":
                    if(string.IsNullOrEmpty(request.UserName)) 
                    return new JsonResult(new {message=$"Empty ot null {findByLower}"}){StatusCode=StatusCodes.Status400BadRequest};
                    user = await _userManager.FindByNameAsync(request.UserName);
                break;
                case "email":
                    if(string.IsNullOrEmpty(request.Email)) 
                    return new JsonResult(new {message=$"Empty ot null {findByLower}"}){StatusCode=StatusCodes.Status400BadRequest};
                    user = await _userManager.FindByEmailAsync(request.Email);
                break;
                case "id" :
                    if(string.IsNullOrEmpty(request.Id)) 
                    return new JsonResult(new {message=$"Empty ot null {findByLower}"}){StatusCode=StatusCodes.Status400BadRequest};
                    user = await _userManager.FindByIdAsync(request.Id);
                break;
                default:
                    return new JsonResult(new {message="Wrong 'findUserBy' argument. It should be 'user' or 'email' or 'id'"}){StatusCode=StatusCodes.Status400BadRequest};
            };
            if(user==null){
              return new JsonResult(new {message=$"Wrong {findByLower}"}){StatusCode=StatusCodes.Status404NotFound};  
            }

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
                var removeRolesThatExistNow = roles.Join(request.RemoveRoles,
                                                        roles1=>roles1,
                                                        roles2=>roles2,
                                                        (roles1,roles2)=>roles1);
                var result = await _userManager.RemoveFromRolesAsync(user,removeRolesThatExistNow);
            }

            return Ok(new {Roles = await _userManager.GetRolesAsync(user),user = new User(){identityUser = user}});
        }
    }
}
