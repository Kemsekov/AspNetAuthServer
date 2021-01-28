using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;
using WebApi.Entities;

namespace WebApi.Models
{
    public class CreateUserRequest
    {
        [Required]
        [MaxLength(100)]
        [MinLength(8)]
        public string Password{get;set;}      
        [MaxLength(256)] 
        public string UserName{get;set;}
        [MaxLength(256)]
        public string Email{get;set;}
        [Phone]
        public string Phone{get;set;}
    }
}