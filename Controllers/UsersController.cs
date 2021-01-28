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
        [Authorize(Role="admin")]
        public async Task<IActionResult> GetAll()
        {
            var users = _userManager.Users.Select(usr => new User(){identityUser = usr});
            return Ok(users);
        }
        [HttpGet("[action]")]
        [Authorize(Role="admin")]
        public async Task<IActionResult> GetAllRaw(){
            var users = _userManager.Users;
            return Ok(users);
        }
        [HttpGet("[action]")]
        [Authorize(Role="admin")]
        public async Task<IActionResult> GetById(string id){
            var user = new User(){identityUser= await _userManager.FindByIdAsync(id)};
            if(user!=null)
            return new JsonResult(user){StatusCode = StatusCodes.Status200OK};
            return new JsonResult(new {message="There is no user with such id"}){StatusCode = StatusCodes.Status404NotFound};
        }
        [HttpGet("[action]")]
        [Authorize(Role="admin")]
        public async Task<IActionResult> GetByIdRaw(string id){
            var user = await _userManager.FindByIdAsync(id);
            if(user!=null)
            return new JsonResult(user){StatusCode = StatusCodes.Status200OK};
            return new JsonResult(new {message="There is no user with such id"}){StatusCode = StatusCodes.Status404NotFound};
        }

        [Authorize(Role="admin")]
        [HttpPost("Add")]
        public async Task<IActionResult> Add(CreateUserRequest user){
            if(!string.IsNullOrEmpty(user.Email) && !string.IsNullOrEmpty(user.UserName)){
                var identityUser = new IdentityUser(){
                    Email = user.Email,
                    UserName = user.UserName,
                    PhoneNumber = user.Phone
                };
                var result = await _userManager.CreateAsync(identityUser,user.Password);
                if(result.Succeeded){
                    identityUser = await _userManager.FindByEmailAsync(identityUser.Email);
                    var verificationCode = await _userManager.GenerateEmailConfirmationTokenAsync(identityUser);
                    return new JsonResult(new {message="Success", email_verification_code = verificationCode}){StatusCode = StatusCodes.Status201Created};
                }
                else 
                    return new JsonResult(result.Errors){StatusCode = StatusCodes.Status400BadRequest};
            }
            return BadRequest("Email or UserName in not present");
        }
    }
}
