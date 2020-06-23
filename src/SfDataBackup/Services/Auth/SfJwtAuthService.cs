using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;

namespace SfDataBackup.Services.Auth
{
    public class SfJwtAuthService : ISfJwtAuthService
    {
        private ILogger<SfJwtAuthService> logger;
        private IHttpClientFactory httpClientFactory;
        private SfOptions options;

        private string accessToken;

        private const string tokenEndpoint = "/services/oauth2/token";

        public SfJwtAuthService(
            ILogger<SfJwtAuthService> logger,
            IHttpClientFactory httpClientFactory,
            IOptionsSnapshot<SfOptions> optionsProvider
        )
        {
            this.logger = logger;
            this.httpClientFactory = httpClientFactory;
            this.options = optionsProvider.Value;
        }

        public async Task<string> GetAccessTokenAsync()
        {
            if (!string.IsNullOrWhiteSpace(accessToken))
                return accessToken;

            var privateKeyPath = Path.Combine(Directory.GetCurrentDirectory(), options.AppCertPath);

            var privateKey = File.ReadAllText(privateKeyPath);
            privateKey = privateKey.Replace("-----BEGIN PRIVATE KEY-----", string.Empty)
                                   .Replace("-----END PRIVATE KEY-----", string.Empty)
                                   .Trim();

            var privateKeyBytes = Convert.FromBase64String(privateKey);
            using var rsa = RSA.Create();
            rsa.ImportRSAPrivateKey(privateKeyBytes, out _);

            var rsaParams = rsa.ExportParameters(true);
            var rsaKey = new RsaSecurityKey(rsaParams);

            var descriptor = new SecurityTokenDescriptor
            {
                Issuer = options.AppClientId,
                Audience = options.OrganisationUrl.ToString(),
                Expires = DateTime.UtcNow.AddMinutes(5),
                Subject = new ClaimsIdentity(
                    new List<Claim>
                    {
                        new Claim("sub", options.OrganisationUser)
                    }
                ),
                SigningCredentials = new SigningCredentials(rsaKey, SecurityAlgorithms.RsaSha256)
            };

            var handler = new JsonWebTokenHandler();
            var jwt = handler.CreateToken(descriptor);

            var requestUrl = new Uri(options.OrganisationUrl, tokenEndpoint);

            var client = httpClientFactory.CreateClient();

            var request = new HttpRequestMessage(HttpMethod.Post, requestUrl);
            request.Content = new StringContent($"grant_type=urn:ietf:params:oauth:grant-type:jwt-bearer&assertion{jwt}", Encoding.UTF8, "application/x-www-form-urlencoded");

            var response = await client.SendAsync(request);

            var deserialisedResponse = JsonConvert.DeserializeObject<SfAuthTokenResponse>(await response.Content.ReadAsStringAsync());

            accessToken = deserialisedResponse.AccessToken;

            return accessToken;
        }

        private class SfAuthTokenResponse
        {
            [JsonProperty("access_token")]
            public string AccessToken { get; set; }
        }
    }
}