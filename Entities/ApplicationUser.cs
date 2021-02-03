using System.Collections.Generic;
using Microsoft.AspNetCore.Identity;

namespace WebApi.Entities
{
    public class ApplicationUser : IdentityUser
    {
        public ApplicationUser()
        {
            this.RefreshTokens = new List<RefreshToken>();
        }
        public ICollection<RefreshToken> RefreshTokens { get; set; }
    }
}
