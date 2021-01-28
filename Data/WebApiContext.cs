using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace WebAppApi.Data
{
    public class WebApiContext : IdentityDbContext
    {
        public WebApiContext(DbContextOptions<WebApiContext> options) : base(options)
        {
            
        }
        protected override void OnModelCreating(ModelBuilder builder){
            //builder.Entity<IdentityRole>().HasData(new IdentityRole("admin"));
            base.OnModelCreating(builder);
        }
    }
}