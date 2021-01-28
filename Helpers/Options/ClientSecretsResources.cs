using Microsoft.Extensions.Configuration;

namespace WebApi.Helpers
{
    public class ClientSecrets
    {
        public string client_id{get;set;}
        public string auth_provider_x509_cert_url{get;set;}
        public string auth_uri{get;set;}
        public string client_secret{get;set;}
        public string project_id{get;set;}
        public string redirect_uri{get;set;}
        public string token_uri{get;set;}
        
        public ClientSecrets()
        {

        }
    }
}