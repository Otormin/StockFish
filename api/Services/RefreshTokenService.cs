using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Microsoft.IdentityModel.Tokens;

namespace api.Services
{
    public class RefreshTokenService
    {
        public static (string rawToken, string hashedToken) GenerateRefreshToken()
        {
            var bytes = RandomNumberGenerator.GetBytes(64);
            var raw = Base64UrlEncoder.Encode(bytes);

            using var sha = SHA256.Create();
            var hashed = Convert.ToBase64String(sha.ComputeHash(bytes));
            return (raw, hashed);
        }

        public static string HashToken(string rawToken)
        {
            var bytes = Base64UrlEncoder.DecodeBytes(rawToken);
            using var sha = SHA256.Create();
            return Convert.ToBase64String(sha.ComputeHash(bytes));
        }
    }
}