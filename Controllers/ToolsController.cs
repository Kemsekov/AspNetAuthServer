using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using WebApi.Models;
using WebApi.Models.Requests;

namespace WebApi.Controllers
{
    [ApiController]
    [Route("[controller]/[action]")]
    public class ToolsController : ControllerBase
    {
        private readonly IEmailSender _emailSender;

        public ToolsController(IEmailSender emailSender)
        {
            _emailSender = emailSender;
        }
        [HttpPost]
        [Authorize(Roles="admin")]
        public async Task<IActionResult> SendEmail(SendEmailRequest request){

            try{
            await _emailSender.SendEmailAsync(request.mailTo,request.subject,request.html);
            return Ok(new {message="Message sent"});
            }
            catch{
                return this.StatusCode(501);
            }
        }
    }
}