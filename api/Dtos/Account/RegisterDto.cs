using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace api.Dtos.Account
{
    public class RegisterDto
    {
        [Required]
        [MinLength(3, ErrorMessage = "Username must be up to 3 characters")]
        [MaxLength(16, ErrorMessage = "Username cannot be more than 16 characters")]
        public string? Username { get; set; }

        [Required]
        [EmailAddress]
        [MaxLength(100, ErrorMessage = "Email cannot be more than 100 characters")]
        public string? Email { get; set; }

        [Required]
        [MinLength(8, ErrorMessage = "Password must be up to 8 characters")]
        [MaxLength(64, ErrorMessage = "Password cannot be more than 64 characters")]
        public string? Password { get; set; }
    }
}