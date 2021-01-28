using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using Pomelo.EntityFrameworkCore.MySql.Infrastructure;
using WebAppApi.Data;
using WebApi.Models;
using WebApi.Helpers;
using AutoMapper;
using WebApi.Services;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Identity.UI.Services;

namespace WebApi
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }
        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddDbContext<WebApiContext>(builder=>
            {
                builder.UseMySql(
                Configuration.GetConnectionString("WebApi"),
                new MySqlServerVersion(new Version(8, 0, 22)),
                mySqlOptions => mySqlOptions
                .CharSetBehavior(CharSetBehavior.NeverAppend));
            });
            services.AddIdentity<IdentityUser,IdentityRole>(options=>{
                options.SignIn.RequireConfirmedEmail=true;
                options.Password.RequireDigit=false;
                options.Password.RequireNonAlphanumeric=false;
                options.Password.RequireUppercase=false;
                options.User.RequireUniqueEmail=true;
                options.SignIn.RequireConfirmedAccount=true;
                options.Password.RequiredLength=8;
            })
                    .AddEntityFrameworkStores<WebApiContext>()
                    .AddDefaultTokenProviders();
                
            services.AddMvc();
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo 
                {
                    Title = "WebApi",
                    Version = "v1", 
                });
            });

            services.Configure<ClientSecrets>(Configuration.GetSection(nameof(ClientSecrets)));
            services.Configure<Token>(Configuration.GetSection(nameof(Token)));
            services.Configure<EmailConfiguration>(Configuration.GetSection(nameof(EmailConfiguration)));
            services.Configure<AppSettings>(Configuration.GetSection(nameof(AppSettings)));
            services.AddScoped<IAuthenticateService, AuthenticateService>();
            services.AddSingleton<IEmailSender,EmailSender>();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, 
                              IWebHostEnvironment env,
                              UserManager<IdentityUser> userManager,
                              RoleManager<IdentityRole> roleManager,
                              WebApiContext dbContext,
                              IOptions<AppSettings> settings)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseSwagger();
                app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "WebApi v1"));
            }

            app.UseMiddleware<JwtMiddleware>();
            app.UseRouting();

            app.UseAuthorization();
            app.UseAuthentication();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(   
                    name: "default",
                    pattern: "{controller=Home}/{action=Index}/{id?}");
            });
            Seed.Initialize(userManager,roleManager,dbContext,settings.Value).Wait();
        }
    }
}
