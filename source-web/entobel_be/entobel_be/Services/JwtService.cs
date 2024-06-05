using System;
using System.Linq;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using BC = BCrypt.Net.BCrypt;

namespace entobel_be.Services
{
    public class JwtService
    {
        public static void VerifyToken(string authHeader, string secretKey)
        {
            if (string.IsNullOrWhiteSpace(authHeader) || !authHeader.StartsWith("Bearer "))
            {
                throw new UnauthorizedAccessException("Unauthorized");
            }

            var token = authHeader.Substring(7);
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(secretKey);

            try
            {
                tokenHandler.ValidateToken(token, new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = false,
                    ValidateAudience = false
                }, out _);
            }
            catch (SecurityTokenException)
            {
                throw new UnauthorizedAccessException("Failed to authenticate token");
            }
        }

        public static string SignToken(string username, string secretKey, double expiresInSeconds = 3600)
        {
            var hashSecretKey = Hash(secretKey);
            var key = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(hashSecretKey));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                claims: new Claim[] { new Claim(ClaimTypes.Name, username) },
                expires: DateTime.UtcNow.AddSeconds(expiresInSeconds),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public static string Hash(string password)
        {
            var saltBytes = Encoding.ASCII.GetBytes(Guid.NewGuid().ToString("N"));
            var hashedBytes = KeyDerivation.Pbkdf2(password, saltBytes, KeyDerivationPrf.HMACSHA256, 10000, 256 / 8);
            return BitConverter.ToString(hashedBytes).Replace("-", "").ToLower();
        }

        public static bool HashCompare(string password, string hash)
        {
            return BC.Verify(password, hash);
        }
    }
}

