using System;
using System.Text;
using System.Threading.Tasks;
using Auth.Contexts;
using Auth.Entities;
//using Auth.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Auth.Options;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Authorization;
using Polly;
using Quartz;
using static OpenIddict.Abstractions.OpenIddictConstants;
using Auth.Jobs;
//using Auth.HealthCheck;

namespace Auth
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }
        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddIdentity<ApplicationUser, IdentityRole>(options=>{
                options.SignIn.RequireConfirmedEmail=true;
                options.Password.RequireDigit=true;
                options.Password.RequireNonAlphanumeric=false;
                options.Password.RequireUppercase=false;
                options.User.RequireUniqueEmail=true;
                options.SignIn.RequireConfirmedAccount=true;
                options.Password.RequiredLength=8;
            })
                .AddEntityFrameworkStores<AuthDbContext>()
                .AddDefaultTokenProviders();
            
            services.AddDbContextPool<AuthDbContext>(options =>{
                options.UseNpgsql(Configuration.GetConnectionString("AuthDb"));
                options.UseOpenIddict();
            });

            services.Configure<IdentityOptions>(options=>{
                options.ClaimsIdentity.UserNameClaimType = Claims.Name;
                options.ClaimsIdentity.UserIdClaimType = Claims.Subject;
                options.ClaimsIdentity.RoleClaimType = Claims.Role;
            });

            services.AddOpenIddict()
                .AddCore(options =>{
                    options.UseEntityFrameworkCore()
                    .UseDbContext<AuthDbContext>();
                })
                .AddServer(options=>{
                    options
                        .SetAuthorizationEndpointUris("/connect/authorize")
                        .SetLogoutEndpointUris("/connect/logout")
                        .SetTokenEndpointUris("/connect/token")
                        .SetUserinfoEndpointUris("/connect/userinfo");

                    options.RegisterScopes(Scopes.Profile, Scopes.Email, Scopes.OfflineAccess);
                    options.AllowPasswordFlow();
                    options.AllowAuthorizationCodeFlow()
                            .AllowRefreshTokenFlow();
                    
                    options.AcceptAnonymousClients();

                    //api scope is just for those users who can use api(idk)
                    options
                        .RegisterScopes("api");
                    options.Configure(cfg =>{
                    });
                    options
                        //for development, need to change to actual cert
                        .AddDevelopmentEncryptionCertificate()
                        .AddDevelopmentSigningCertificate()
                        //for development
                        .DisableAccessTokenEncryption();
                    
                    options.UseAspNetCore()
                        .EnableAuthorizationEndpointPassthrough()
                        .EnableLogoutEndpointPassthrough()
                        .EnableTokenEndpointPassthrough()
                        .EnableUserinfoEndpointPassthrough()
                        .EnableStatusCodePagesIntegration()
                        .DisableTransportSecurityRequirement();
                })
                .AddValidation(options =>{
                    options.UseLocalServer();
                    options.UseAspNetCore();
                });
            services.AddSwaggerGen(c =>
            {    
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "WebApi", Version = "v1" });
            });

            services.AddQuartz(options =>
            {
                options.UseMicrosoftDependencyInjectionJobFactory();
                options.UseSimpleTypeLoader();
                options.UseInMemoryStore();
            });

            services.AddQuartzHostedService(options => options.WaitForJobsToComplete = true);
            services.AddHostedService<OpenIddictWork>();
            services.AddControllers();
            services.AddCors();
            //just an example of http polly
            //services.AddHttpClient<IEmailSender>()
            //    .AddTransientHttpErrorPolicy(builder=>builder.WaitAndRetryAsync(10,retryAttempt=>TimeSpan.FromSeconds(Math.Pow(2,retryAttempt))))
            //    .AddTransientHttpErrorPolicy(builder=>builder.CircuitBreakerAsync(3,TimeSpan.FromSeconds(10)));
            
            /* services.AddHealthChecks()
                .AddCheck<EmailSenderHealthCheck>(nameof(IEmailSender)); */
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            app.UsePathBase("/auth");
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseSwagger();
                app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Auth v1"));
            }
            else{
            }

            //app.UseHttpsRedirection();

            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
                //endpoints.MapHealthChecks("/health");
            });
        }
        
    }
}
