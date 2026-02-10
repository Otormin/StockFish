using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using api.Dtos.Account;
using api.Helpers;
using api.Interfaces;
using api.Models;
using api.Repository;
using api.Response;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.JsonWebTokens;

namespace api.Services
{
    public class AccountService : IAccountService
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly ITokenService _tokenService;
        private readonly IConfiguration _config;
        private readonly IRefreshTokenRepository _refreshTokenRepository;
        private readonly SignInManager<AppUser> _signinManager;
        private readonly ILogger<AccountService> _logger;
        public AccountService(UserManager<AppUser> userManager, ITokenService tokenService, IConfiguration config, IRefreshTokenRepository refreshTokenRepository, SignInManager<AppUser> signinManager, ILogger<AccountService> logger)
        {
            _userManager = userManager;
            _tokenService = tokenService;
            _config = config;
            _refreshTokenRepository = refreshTokenRepository;
            _signinManager = signinManager;
            _logger = logger;
            _logger.LogDebug("Nlog is integrated to Account Service");
        }

        public async Task<ApiResponse> LoginUser(LoginDto loginDto)
        {
            try{
                if (string.IsNullOrEmpty(loginDto.UsernameOrEmail))
                {
                    return new ApiResponse{
                        StatusCode = 401,
                        Message = "Please provide either a Username or an Email address."
                    };
                }

                AppUser? user = null;

                user = await _userManager.FindByNameAsync(loginDto.UsernameOrEmail);

                if (user == null)
                {
                    user = await _userManager.FindByEmailAsync(loginDto.UsernameOrEmail);
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

                var (token, tokenExpires) = await _tokenService.CreateToken(user);
                var refresh = RefreshTokenService.GenerateRefreshToken();
                var refreshToken = new RefreshToken
                {
                    Token = refresh.hashedToken,
                    UserId = user.Id,
                    ExpiresOnUtc = DateTime.UtcNow.AddDays(_config.GetValue<int>("JWT:RefreshTokenExpirationInDays")),
                    Created = DateTime.UtcNow
                };

                var createdRefreshToken = await _refreshTokenRepository.CreateRefreshTokenAsync(refreshToken);
                if(createdRefreshToken == null)
                {
                    return new ApiResponse
                    {
                       StatusCode = 500,
                       Message = "Could not create refresh token"  
                    };
                }

                var refreshTokenValidityInDays = _config.GetValue<int>("JWT:RefreshTokenExpirationInDays");
                var refreshTokenValidityTimeStamp =  DateTime.UtcNow.AddDays(refreshTokenValidityInDays);

                return new ApiResponse
                {
                    StatusCode = 200,
                    Message = "User Successfully Logged in",
                    Data = new NewUserDto
                    {
                        UserName = user.UserName,
                        Email = user.Email,
                        Token = token,
                        TokenExpires = tokenExpires,
                        RefreshToken = refresh.rawToken,
                        RefreshTokenExpires = (int)refreshTokenValidityTimeStamp.Subtract(DateTime.UtcNow).TotalSeconds
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
                if (string.IsNullOrEmpty(registerDto.Username) && string.IsNullOrEmpty(registerDto.Email))
                {
                    return new ApiResponse{
                        StatusCode = 401,
                        Message = "Please provide a Username and an Email address."
                    };
                }

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
                        var (token, tokenExpires) = await _tokenService.CreateToken(appUser);
                        var refresh = RefreshTokenService.GenerateRefreshToken();
                        var refreshToken = new RefreshToken
                        {
                            Token = refresh.hashedToken,
                            UserId = appUser.Id,
                            ExpiresOnUtc = DateTime.UtcNow.AddDays(_config.GetValue<int>("JWT:RefreshTokenExpirationInDays")),
                            Created = DateTime.UtcNow
                        };

                        var createdRefreshToken = await _refreshTokenRepository.CreateRefreshTokenAsync(refreshToken);
                        if(createdRefreshToken == null)
                        {
                            return new ApiResponse
                            {
                                StatusCode = 500,
                                Message = "Could not create refresh token"  
                            };
                        }

                        var refreshTokenValidityInDays = _config.GetValue<int>("JWT:RefreshTokenExpirationInDays");
                        var refreshTokenValidityTimeStamp =  DateTime.UtcNow.AddDays(refreshTokenValidityInDays);

                        return new ApiResponse
                        {
                            StatusCode = 200,
                            Message = "User Successfully Created",
                            Data = new NewUserDto
                            {
                                UserName = appUser.UserName,
                                Email = appUser.Email,
                                Token = token,
                                TokenExpires = tokenExpires,
                                RefreshToken = refresh.rawToken,
                                RefreshTokenExpires = (int)refreshTokenValidityTimeStamp.Subtract(DateTime.UtcNow).TotalSeconds
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

        public async Task<ApiResponse> RefreshToken(RefreshTokenDto refreshTokenDto)
        {
            try
            {
                var hashedToken = RefreshTokenService.HashToken(refreshTokenDto.refreshToken);
                var refreshToken = await _refreshTokenRepository.FindHashedToken(hashedToken);
                
                if (refreshToken == null || !refreshToken.IsActive)
                {
                    return new ApiResponse { 
                        StatusCode = 401, 
                        Message = "Invalid or expired refresh token" 
                    };
                }

                refreshToken.Revoked = DateTime.UtcNow;
                await _refreshTokenRepository.UpdateRefreshTokenAsync(refreshToken);

                var user = await _userManager.FindByIdAsync(refreshToken.UserId);
                if(user == null)
                {
                    return new ApiResponse
                    {
                        StatusCode = 401,
                        Message = "Invalid token user"
                    };
                }

                var (token, tokenExpires) = await _tokenService.CreateToken(user);
                var generatedRefreshToken = RefreshTokenService.GenerateRefreshToken();
                var newRefreshToken = new RefreshToken()
                {
                    Token = generatedRefreshToken.hashedToken,
                    UserId = user.Id,
                    Created = DateTime.UtcNow,
                    ExpiresOnUtc = DateTime.UtcNow.AddDays(_config.GetValue<int>("JWT:RefreshTokenExpirationInDays")),
                };

                var createdRefreshToken = await _refreshTokenRepository.CreateRefreshTokenAsync(newRefreshToken);
                if(createdRefreshToken == null)
                {
                    return new ApiResponse
                    {
                       StatusCode = 500,
                       Message = "Could not create refresh token"  
                    };
                }

                var refreshTokenValidityInDays = _config.GetValue<int>("JWT:RefreshTokenExpirationInDays");
                var refreshTokenValidityTimeStamp =  DateTime.UtcNow.AddDays(refreshTokenValidityInDays);

                return new ApiResponse
                {
                    StatusCode = 200,
                    Message = "Token refreshed successfully",
                    Data = new NewUserDto
                    {
                        UserName = user.UserName,
                        Email = user.Email,
                        Token = token,
                        TokenExpires = tokenExpires,
                        RefreshToken = generatedRefreshToken.rawToken,
                        RefreshTokenExpires = (int)refreshTokenValidityTimeStamp.Subtract(DateTime.UtcNow).TotalSeconds
                    }
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during token refresh for token hash: {Hash}", RefreshTokenService.HashToken(refreshTokenDto.refreshToken));
                return new ApiResponse
                {
                    StatusCode = 500,
                    Message = "An internal server error occurred.",
                };
            }
        }
    }
}