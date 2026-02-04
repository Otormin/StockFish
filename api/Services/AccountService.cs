using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using api.Dtos.Account;
using api.Interfaces;
using api.Models;
using api.Response;
using Microsoft.AspNetCore.Identity;

namespace api.Services
{
    public class AccountService : IAccountService
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly ITokenService _tokenService;
        private readonly SignInManager<AppUser> _signinManager;
        private readonly ILogger<AccountService> _logger;
        public AccountService(UserManager<AppUser> userManager, ITokenService tokenService, SignInManager<AppUser> signinManager, ILogger<AccountService> logger)
        {
            _userManager = userManager;
            _tokenService = tokenService;
            _signinManager = signinManager;
            _logger = logger;
            _logger.LogDebug("Nlog is integrated to Account Service");
        }

        public async Task<ApiResponse> LoginUser(LoginDto loginDto)
        {
            try{
                if (string.IsNullOrEmpty(loginDto.Username) && string.IsNullOrEmpty(loginDto.Email))
                {
                    return new ApiResponse{
                        StatusCode = 401,
                        Message = "Please provide either a Username or an Email address."
                    };
                }

                AppUser? user = null;

                if (!string.IsNullOrEmpty(loginDto.Username))
                {
                    user = await _userManager.FindByNameAsync(loginDto.Username);
                }
                else if (!string.IsNullOrEmpty(loginDto.Email))
                {
                    user = await _userManager.FindByEmailAsync(loginDto.Email);
                }

                if (user == null)
                {
                    return new ApiResponse
                    {
                        StatusCode = 401,
                        Message = "invalid Credentials",
                    };
                }

                //Check Password and Lockout
                var result = await _signinManager.CheckPasswordSignInAsync(user, loginDto.Password, true);

                if (result.IsLockedOut)
                {
                    return new ApiResponse
                    {
                        StatusCode = 429,
                        Message = "Account locked due to multiple failed attempts. Please try again later.",
                    };
                }

                if (!result.Succeeded)
                {
                    return new ApiResponse
                    {
                        StatusCode = 401,
                        Message = "invalid Credentials",
                    };
                }

                return new ApiResponse
                {
                    StatusCode = 200,
                    Message = "User Successfully Logged in",
                    Data = new NewUserDto
                    {
                        UserName = user.UserName,
                        Email = user.Email,
                        Token = _tokenService.CreateToken(user)
                    }
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message, "Login failed");
                return new ApiResponse
                {
                    StatusCode = 500,
                    Message = "An internal server error occurred.",
                };
            }
        }

        public async Task<ApiResponse> RegisterUser(RegisterDto registerDto)
        {
            try
            {
                var appUser = new AppUser
                {
                    UserName = registerDto.Username,
                    Email = registerDto.Email,
                };

                var createdUser = await _userManager.CreateAsync(appUser, registerDto.Password);

                if (createdUser.Succeeded)
                {
                    var roleResult = await _userManager.AddToRoleAsync(appUser, "User");
                    if (roleResult.Succeeded)
                    {
                        return new ApiResponse
                        {
                            StatusCode = 200,
                            Message = "User Successfully Created",
                            Data = new NewUserDto
                            {
                                UserName = appUser.UserName,
                                Email = appUser.Email,
                                Token = _tokenService.CreateToken(appUser)
                            }
                        };
                    }
                    else
                    {
                        var errors = string.Join(", ", createdUser.Errors.Select(e => e.Description));

                        return new ApiResponse
                        {
                            StatusCode = 500,
                            Message = errors,
                        };
                    }
                }
                else
                {
                    var errors = string.Join(", ", createdUser.Errors.Select(e => e.Description));

                    return new ApiResponse
                    {
                        StatusCode = 500,
                        Message = errors,
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message, "Registration failed");
                return new ApiResponse
                {
                    StatusCode = 500,
                    Message = "An internal server error occurred.",
                };
            }
        }
    }
}