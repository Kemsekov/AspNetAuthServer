using System.ComponentModel.DataAnnotations;

namespace WebApi.Models
{
    public class CreateRoleRequest
    {
        [Required]
        public string Name{get;set;}
    }
}