using System.ComponentModel.DataAnnotations;

namespace WebApi.Models.Requests
{
    public class CreateRoleRequest
    {
        [Required]
        public string Name{get;set;}
    }
}