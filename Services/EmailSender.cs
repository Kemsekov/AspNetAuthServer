using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Responses;
using Google.Apis.Services;
using Google.Apis.Util;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.Extensions.Options;
using MimeKit;
using NETCore.MailKit.Infrastructure.Internal;
using WebApi.Models;
using  MailKit.Net;
using System.Security.Cryptography.X509Certificates;
using System.Net.Security;
using MailKit.Security;
using Google.Apis.Auth.OAuth2.Flows;
using System.Collections.Generic;
using WebApi.Options;

namespace WebApi.Services
{
    public class EmailSender : IEmailSender
    {
        private readonly Token _tokenConf;
        private readonly CertsOptions _certs;
        private readonly Options.ClientSecrets _client_secrets;
        private readonly EmailConfiguration _email;

        public EmailSender(IOptions<CertsOptions> certs,
                           IOptions<Options.ClientSecrets> client_secrets,
                           IOptions<EmailConfiguration> email,
                           IOptions<Token> token)
        {
            _tokenConf = token.Value;
            _certs = certs.Value;
            _client_secrets = client_secrets.Value;
            _email = email.Value;
            _token = UpdateTokenAsync().GetAwaiter().GetResult();
            token_expires_in = DateTime.Now.AddSeconds(_token.ExpiresInSeconds.Value);
        }
        protected readonly EmailConfiguration _configuration;
        protected TokenResponse _token;
        protected DateTime token_expires_in;

        public async Task SendAsync(string mailTo, string subject, string message, bool isHtml = false, SenderInfo sender = null)
        {
            var server =_configuration.Server;
            var port = _configuration.Port;
            var username = _configuration.Username;
            var path = _certs.email;
            var mime_message = new MimeMessage();

            mime_message.To.Add(new MailboxAddress(mailTo));
            mime_message.Subject = subject;
            mime_message.Sender = new MailboxAddress(username,sender!=null && sender.SenderName!=null ? sender.SenderName:"WebApp");
            
            if(isHtml)
            mime_message.Body = new TextPart(MimeKit.Text.TextFormat.Html){
                Text = message
            };
            else
            mime_message.Body = new TextPart(MimeKit.Text.TextFormat.Plain){
                Text = message
            };
            
            Task<TokenResponse> updatetoken_task = null;
            
            if(DateTime.Now >= token_expires_in)
            updatetoken_task = UpdateTokenAsync();
            
            using var client = new  MailKit.Net.Smtp.SmtpClient();
            var cert = new X509Certificate(File.ReadAllBytes(path));
            client.ClientCertificates = new X509CertificateCollection();
            client.ClientCertificates.Add(cert);

            client.ServerCertificateValidationCallback = (s, c, h, e) => e==SslPolicyErrors.None;
            await client.ConnectAsync(server,port,SecureSocketOptions.StartTls);
            
            if(updatetoken_task!=null) {
            _token = await updatetoken_task;
            this.token_expires_in = DateTime.Now.AddSeconds(_token.ExpiresInSeconds.Value);
            }
            var oauth2 = new SaslMechanismOAuth2(username,_token.AccessToken);

            try{
            client.Authenticate(oauth2);
            await client.SendAsync(mime_message);
            client.Disconnect(true);
            }
            catch(MailKit.Security.AuthenticationException ex ){
                System.Console.WriteLine(ex.Message);
                client.Disconnect(true);
            }
        }

        protected async Task<TokenResponse> UpdateTokenAsync(){
            
            var auth_uri = _client_secrets.auth_uri;
            var token_uri = _client_secrets.token_uri;
            var scope = _tokenConf.scope;
            var refresh_token = _tokenConf.refresh_token;
            var client_id = _client_secrets.client_id;
            var client_secret = _client_secrets.client_secret;

            var client_secrets = new Google.Apis.Auth.OAuth2.ClientSecrets(){
                ClientId = client_id,
                ClientSecret = client_secret
            };
            var init = 
                new AuthorizationCodeFlow.Initializer
                (authorizationServerUrl : auth_uri,
                 tokenServerUrl: token_uri)
                {
                    ClientSecrets = client_secrets,
                    Scopes= new List<string>(){scope}
                };

            var authCodeFlow = new AuthorizationCodeFlow(init);
            return await authCodeFlow.RefreshTokenAsync(
                    authCodeFlow.ClientSecrets.ClientId,
                    refresh_token,
                    System.Threading.CancellationToken.None);
        }
        public async Task SendEmailAsync(string mailTo, string subject, string htmlMessage)
        {
            await SendAsync(mailTo,subject,htmlMessage,true);
        }
    }
}