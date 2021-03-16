using System;
using System.Text;
using System.Threading.Tasks;
using WebApi.Contexts;
using WebApi.Entities;
using WebApi.Services;
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
using WebApi.Options;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Authorization;
using Polly;
using WebApi.HealthCheck;
namespace WebApi
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
            services.Configure<JwtOptions>(Configuration.GetSection(nameof(JwtOptions)));
            services.Configure<CertsOptions>(Configuration.GetSection(nameof(CertsOptions)));
            services.Configure<AdminOptions>(Configuration.GetSection(nameof(AdminOptions)));
            services.Configure<EmailConfiguration>(Configuration.GetSection(nameof(EmailConfiguration)));
            services.Configure<Token>(Configuration.GetSection(nameof(Token)));
            services.Configure<Options.ClientSecrets>(Configuration.GetSection(nameof(Options.ClientSecrets)));
                services.AddIdentity<ApplicationUser, IdentityRole>(options=>{
                options.SignIn.RequireConfirmedEmail=true;
                options.Password.RequireDigit=false;
                options.Password.RequireNonAlphanumeric=false;
                options.Password.RequireUppercase=false;
                options.User.RequireUniqueEmail=true;
                options.SignIn.RequireConfirmedAccount=true;
                options.Password.RequiredLength=8;
            })
                .AddEntityFrameworkStores<WebApiDbContext>()
                .AddDefaultTokenProviders();
            services.AddSingleton<IEmailSender,EmailSender>();
            services.AddDbContextPool<WebApiDbContext>(options =>
            options.UseNpgsql(Configuration.GetConnectionString("WebApi")));

            AddAuthentication(services);
            services.AddAuthorization();
            services.AddScoped<IUserAuthenticationService, UserAuthenticationService>();
            services.AddControllers();

            //just an example of http polly
            services.AddHttpClient<IEmailSender>()
                .AddTransientHttpErrorPolicy(builder=>builder.WaitAndRetryAsync(10,retryAttempt=>TimeSpan.FromSeconds(Math.Pow(2,retryAttempt))))
                .AddTransientHttpErrorPolicy(builder=>builder.CircuitBreakerAsync(3,TimeSpan.FromSeconds(10)));
            
            services.AddHealthChecks()
                .AddCheck<EmailSenderHealthCheck>(nameof(IEmailSender));
            
            services.AddSwaggerGen(c =>
            {
                
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "WebApi", Version = "v1" });
            });
            services.AddCors(c =>{ c.AddPolicy("dev", opt =>
            {
                opt.AllowAnyHeader()
                .WithExposedHeaders(AuthSettings.EXPIRED_TOKEN_HEADER) //https://stackoverflow.com/questions/37897523/axios-get-access-to-response-header-fields#answer-55714686
                .AllowAnyMethod()
                .AllowCredentials()
                .WithOrigins("http://localhost");
            });
            c.AddPolicy("prod",opt=>{
                //todo later
            });
            });
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            app.UsePathBase("/api");
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseSwagger();
                app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "WebApi v1"));
                app.UseCors("dev");
            }
            else{
                app.UseCors("prod");
            }

            //app.UseHttpsRedirection();

            app.UseRouting();
            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
                endpoints.MapHealthChecks("/health");
            });
        }
        private void AddAuthentication(IServiceCollection services)
        {
            services.AddAuthentication(opts =>
            {
                opts.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                opts.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            }).AddJwtBearer(jwtOptions =>
            {
                jwtOptions.SaveToken = false;
                jwtOptions.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
                {
                    ValidIssuer = Configuration["JwtOptions:Issuer"],
                    ValidAudience = Configuration["JwtOptions:Audience"],
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Configuration["JwtOptions:Key"])),
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateIssuerSigningKey = true,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero
                };
                jwtOptions.Events = new JwtBearerEvents
                {
                    OnAuthenticationFailed = failedContext =>
                    {
                        if (failedContext.Exception.GetType() == typeof(SecurityTokenExpiredException))
                        {
                            failedContext.Response.Headers.Add(AuthSettings.EXPIRED_TOKEN_HEADER, "true");
                        }
                        return Task.CompletedTask;
                    }
                };
            });
        }
    }
}
