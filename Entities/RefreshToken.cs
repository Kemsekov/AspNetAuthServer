using System;

namespace WebApi.Entities
{
    public class RefreshToken
    {
        public Guid Key { get; set; }
        public string Token { get; set; }
        public string UserId { get; set; }
        public DateTime CreatedOn { get; set; }
        public ApplicationUser User { get; set; }
    }
}
