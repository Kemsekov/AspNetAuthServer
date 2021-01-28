using System.ComponentModel.DataAnnotations;

namespace WebApi.Models
{
    public class SendEmailRequest
    {
        [EmailAddress]
        [Required]
        public string mailTo{get;set;}
        [Required]
        [MinLength(5)]
        public string subject{get;set;}
        [Required]
        public string html{get;set;}
    }
}