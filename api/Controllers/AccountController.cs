using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using api.Dtos.Account;
using api.Interfaces;
using api.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace api.Controllers
{
    [Route("api/account")]
    [ApiController]
    public class AccountController : ControllerBase
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly ITokenService _tokenService;
        private readonly SignInManager<AppUser> _signinManager;
        private readonly ILogger<AccountController> _logger;
        public AccountController(UserManager<AppUser> userManager, ITokenService tokenService, SignInManager<AppUser> signinManager, ILogger<AccountController> logger)
        {
            _userManager = userManager;
            _tokenService = tokenService;
            _signinManager = signinManager;
            _logger = logger;
            _logger.LogDebug("Nlog is integrated to Account Controller");
        }

        [HttpPost("register")]
        public async Task<IActionResult> RegisterUser([FromBody] RegisterDto registerDto)
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
                        return Ok(
                            new NewUserDto
                            {
                                UserName = appUser.UserName,
                                Email = appUser.Email,
                                Token = _tokenService.CreateToken(appUser)
                            }
                        );
                    }
                    else
                    {
                        return StatusCode(500, roleResult.Errors);
                    }
                }
                else
                {
                    return BadRequest(createdUser.Errors);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message, "Registration failed");
                return StatusCode(500, "An internal server error occurred.");
            }
        }

        [HttpPost("login")]
        public async Task<IActionResult> LoginUser(LoginDto loginDto)
        {
            try{
                var user = await _userManager.FindByNameAsync(loginDto.Username);

                if(user == null)
                {
                    return Unauthorized("Invalid Username or Password");
                }

                // Enable LockoutOnFailure
                var result = await _signinManager.CheckPasswordSignInAsync(user, loginDto.Password, true);

                if (result.IsLockedOut)
                {
                    return StatusCode(429, "Account locked due to multiple failed attempts. Please try again later.");
                }

                if (!result.Succeeded)
                {
                    return Unauthorized("Invalid Username or Password");
                }

                return Ok(
                    new NewUserDto
                    {
                        UserName = user.UserName,
                        Email = user.Email,
                        Token = _tokenService.CreateToken(user)
                    }
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message, "Login failed");
                return StatusCode(500, "An internal server error occurred.");
            }
        }
    }
}