using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Auth.Contexts;
using Auth.Entities;
using Auth.Jobs;
using Auth.Options;
//using Auth.Services;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Serilog;

namespace Auth
{
    public class Program
    {
        public static IConfiguration Configuration { get; } = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("seed.json", optional: true, reloadOnChange: false)
            .AddEnvironmentVariables()
            .Build();
        public static IWebHost BuildWebHost(string[] args) =>
            WebHost.CreateDefaultBuilder(args)
                .UseStartup<Startup>()
                .UseSerilog()
                //.UseUrls("http://localhost:5000")
                .Build();
        public static async Task Main(string[] args)
        {
            Log.Logger = new Serilog.LoggerConfiguration()
            .MinimumLevel.Debug()
            .MinimumLevel.Override("Microsoft", Serilog.Events.LogEventLevel.Debug)
            .Enrich.FromLogContext()
            .WriteTo.Console()
            .WriteTo.File("./log.txt")
            .CreateLogger();    
            
            try
            {
                var webHost = BuildWebHost(args);
                using var scope = webHost.Services.CreateScope();
                var service = scope.ServiceProvider;
                var userManager = service.GetRequiredService<UserManager<ApplicationUser>>();
                var roleManager = service.GetRequiredService<RoleManager<IdentityRole>>();
                
                var seed = Configuration.GetSection(nameof(Seed)).Get<Seed>();
                await seed.Initialize(userManager, roleManager);
                
                webHost.Run();
                return;
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "Host terminated unexpectedly");
                return;
            }
            finally
            {
                Log.CloseAndFlush();
            }
        }
    }
}