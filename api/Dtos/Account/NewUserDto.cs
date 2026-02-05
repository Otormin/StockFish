using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace api.Dtos.Account
{
    public class NewUserDto
    {
        public string UserName { get; set; }
        public string Email { get; set; }
        public string Token { get; set; } = string.Empty;
        public DateTime TokenExpires { get; set; }
        public string RefreshToken{ get; set; } = string.Empty;
        public DateTime RefreshTokenExpires { get; set; }
    }
}