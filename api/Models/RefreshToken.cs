using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using api.Models;

namespace api.Helpers
{
    public class RefreshToken
    {
        public Guid Id { get; set; }
        public string Token { get; set; }
        public string UserId { get; set; } = string.Empty;
        public DateTime ExpiresOnUtc { get; set; }
        public DateTime Created { get; set; }
        public DateTime? Revoked { get; set; }

        public bool IsExpired => DateTime.UtcNow >= ExpiresOnUtc;
        public bool IsActive => Revoked == null && !IsExpired;
    }
}