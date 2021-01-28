namespace WebApi.Models
{
    public class SendEmailRequest
    {
        public string mailTo{get;set;}
        public string subject{get;set;}
        public string html{get;set;}
    }
}