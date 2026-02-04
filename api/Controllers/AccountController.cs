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
        private readonly IAccountService _accountService;
        private readonly ILogger<AccountController> _logger;
        public AccountController(ILogger<AccountController> logger, IAccountService accountService)
        {
            _accountService = accountService;
            _logger = logger;
            _logger.LogDebug("Nlog is integrated to Account Controller");
        }

        [HttpPost("register")]
        public async Task<IActionResult> RegisterUser([FromBody] RegisterDto registerDto)
        {
            try
            {
                var registerUser = await _accountService.RegisterUser(registerDto);

                if (registerUser.StatusCode == 200)
                {
                    return Ok(registerUser.Data);
                }
                else if (registerUser.StatusCode == 400)
                {
                    return BadRequest(registerUser.Message);
                }
                else
                {
                    return StatusCode(500, registerUser.Message);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message, "Registration failed");
                return StatusCode(500, "An internal server error occurred.");
            }
        }

        [HttpPost("login")]
        public async Task<IActionResult> LoginUser([FromBody] LoginDto loginDto)
        {
            try{
                var loginUser = await _accountService.LoginUser(loginDto);

                if (loginUser.StatusCode == 200)
                {
                    return Ok(loginUser.Data);
                }
                else if (loginUser.StatusCode == 401)
                {
                    return BadRequest(loginUser.Message);
                }
                else if (loginUser.StatusCode == 429)
                {
                    return StatusCode(429, loginUser.Message);
                }
                else
                {
                    return StatusCode(500, loginUser.Message);
                }

            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message, "Login failed");
                return StatusCode(500, "An internal server error occurred.");
            }
        }
    }
}