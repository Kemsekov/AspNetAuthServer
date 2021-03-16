using System.Collections.Generic;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using WebApi.Options;

namespace WebApi.HealthCheck
{
    public class EmailSenderHealthCheck : IHealthCheck
    {
        private EmailConfiguration Configuration { get; }

        public EmailSenderHealthCheck(IOptions<EmailConfiguration> configuration){
            Configuration = configuration.Value;
        }


        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            Ping ping = new();
            var scopeReply = await ping.SendPingAsync(Configuration.Scope.Replace("https://","").Replace("http://",""));
            var serverReply = await ping.SendPingAsync(Configuration.Server.Replace("https://","").Replace("http://",""));
         
            string Errors = null; 
            if(scopeReply.Status != IPStatus.Success)
                Errors = "Scope Reply : "+scopeReply.Status.ToString()+"\n";

            if(serverReply.Status != IPStatus.Success)
                Errors = "Server Reply : "+serverReply.Status.ToString();
            if(Errors!=null)
                return HealthCheckResult.Unhealthy(Errors);
            return HealthCheckResult.Healthy();
        }   
    }
}