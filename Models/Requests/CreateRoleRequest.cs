using System.ComponentModel.DataAnnotations;

namespace WebApi.Models.Requests
{
    public class CreateRoleRequest
    {
        [Required]
        [MaxLength(256)]
        public string Name{get;set;}
    }
}