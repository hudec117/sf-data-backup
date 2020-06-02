using System;
using System.Security.Cryptography;
using System.IO;
using Microsoft.IdentityModel.Tokens;
using Microsoft.IdentityModel.JsonWebTokens;
using System.Security.Claims;
using System.Collections.Generic;

// References:
// https://vcsjones.dev/2019/10/07/key-formats-dotnet-3/
// https://stackoverflow.com/questions/38794670/how-to-create-encrypted-jwt-in-c-sharp-using-rs256-with-rsa-private-key
// https://blog.angular-university.io/angular-jwt/
// http://travistidwell.com/jsencrypt/demo/
// https://jwt.io/

namespace SfJwtGenerator
{
    public class Program
    {
        private static void Main(string[] args)
        {
            const string clientId = "";
            const string audience = "https://login.salesforce.com";
            const string user = "";
            const string privateKeyName = "";

            var privateKeyPath = Path.Combine(Directory.GetCurrentDirectory(), privateKeyName);

            var privateKey = File.ReadAllText(privateKeyPath);
            privateKey = privateKey.Replace("-----BEGIN RSA PRIVATE KEY-----", string.Empty)
                                   .Replace("-----END RSA PRIVATE KEY-----", string.Empty)
                                   .Trim();

            var privateKeyBytes = Convert.FromBase64String(privateKey);
            using var rsa = RSA.Create();
            rsa.ImportRSAPrivateKey(privateKeyBytes, out _);

            var rsaParams = rsa.ExportParameters(true);
            var rsaKey = new RsaSecurityKey(rsaParams);

            var descriptor = new SecurityTokenDescriptor
            {
                Issuer = clientId,
                Audience = audience,
                Expires = DateTime.UtcNow.AddMinutes(5),
                Subject = new ClaimsIdentity(
                    new List<Claim>
                    {
                        new Claim("sub", user)
                    }
                ),
                SigningCredentials = new SigningCredentials(rsaKey, SecurityAlgorithms.RsaSha256)
            };

            var handler = new JsonWebTokenHandler();
            var jwt = handler.CreateToken(descriptor);

            Console.WriteLine(jwt);
        }
    }
}