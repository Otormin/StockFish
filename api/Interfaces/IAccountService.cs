using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using api.Dtos.Account;
using api.Response;

namespace api.Interfaces
{
    public interface IAccountService
    {
        Task<ApiResponse> LoginUser(LoginDto loginDto);
        Task<ApiResponse> RegisterUser(RegisterDto registerDto);
    }
}