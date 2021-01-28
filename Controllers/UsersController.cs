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

namespace WebApi.Controllers
{
    [ApiController]
    [Route("[controller]")]
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
            var currentUser = (IdentityUser)HttpContext.Items["User"];
            if(!await _userManager.IsInRoleAsync(currentUser,"admin")) return Forbid(new string[]{"User is not claimed with admin role"});
            var users = _userManager.Users.Select(usr => new User(){identityUser = usr});
            return Ok(users);
        }
        [HttpGet("[action]")]
        [Authorize(Role="admin")]
        public async Task<IActionResult> GetAllRaw(){
            var currentUser = (IdentityUser)HttpContext.Items["User"];
            if(!await _userManager.IsInRoleAsync(currentUser,"admin")) return Forbid(new string[]{"User is not claimed with admin role"});
            var users = _userManager.Users;
            return Ok(users);
        }
        [HttpGet("[action]")]
        [Authorize(Role="admin")]
        public async Task<IActionResult> GetById(string id){
            var currentUser = (IdentityUser)HttpContext.Items["User"];
            if(!await _userManager.IsInRoleAsync(currentUser,"admin")) return Forbid(new string[]{"User is not claimed with admin role"});
            var user = new User(){identityUser= await _userManager.FindByIdAsync(id)};
            return Ok(user);
        }
        [HttpGet("[action]")]
        [Authorize(Role="admin")]
        public async Task<IActionResult> GetByIdRaw(string id){
            var currentUser = (IdentityUser)HttpContext.Items["User"];
            if(!await _userManager.IsInRoleAsync(currentUser,"admin")) return Forbid(new string[]{"User is not claimed with admin role"});
            var user = await _userManager.FindByIdAsync(id);
            return Ok(user);
        }

        [Authorize(Role="admin")]
        [HttpPost("Add")]
        public async Task<IActionResult> Add(CreateUserRequest user){
            if(!string.IsNullOrEmpty(user.Email) || !string.IsNullOrEmpty(user.UserName)){
                var identityUser = new IdentityUser(){
                    Email = user.Email,
                    UserName = user.UserName,
                    PhoneNumber = user.Phone
                };
                var result = await _userManager.CreateAsync(identityUser,user.Password);
                return Created(HttpContext.Request.Host.Host,new {message="Success"});
            }
            return BadRequest("Email or UserName in not present");
        }
        [HttpGet("[action]")]
        [Authorize(Role="admin")]
        public async Task<IActionResult> SendEmail(){
            
            try{
            await _emailSender.SendEmailAsync("pikova98@bk.ru","Hello","<p>Hello</p>");
            return Ok();
            }
            catch{
                return this.StatusCode(501);
            }
        }
    }
}
