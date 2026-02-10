using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using api.Extentions;
using api.Interfaces;
using api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace api.Controllers
{
    [Route("api/portfolios")]
    [ApiController]
    public class PortfolioController : ControllerBase
    {
        private readonly IPortfolioService _portfolioService;
        private readonly ILogger<CommentController> _logger;
        public PortfolioController(IPortfolioService portfolioService, ILogger<CommentController> logger)
        {
            _portfolioService = portfolioService;
            _logger = logger;
            _logger.LogDebug("Nlog is integrated to Portfolio Controller");
        }

        [HttpGet]
        [Authorize]
        [EnableRateLimiting("ip-sliding")]
        public async Task<IActionResult> GetUserPortfolio()
        {
            try
            {
                var userPortfolio = await _portfolioService.GetUserPortfolio();
                if(userPortfolio.StatusCode == 200)
                {
                    return Ok(userPortfolio.Data);
                }
                else if (userPortfolio.StatusCode == 401)
                {
                    return Unauthorized(userPortfolio.Message);
                }
                else
                {
                    return StatusCode(500, "An internal server error occurred.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message, "Get User Portfolio failed");
                return StatusCode(500, "An internal server error occurred.");
            }
        }

        [HttpPost]
        [Authorize]
        [EnableRateLimiting("ip-sliding")]
        public async Task<IActionResult> AddPortfolio(string symbol)
        {
            try{
                var addedPortfolio = await _portfolioService.AddPortfolio(symbol);
                if(addedPortfolio.StatusCode == 200)
                {
                    if(addedPortfolio.Data != Empty)
                    {
                        return Ok(addedPortfolio.Data);
                    }

                    return Ok(addedPortfolio.Message);
                }
                else if (addedPortfolio.StatusCode == 401)
                {
                    return Unauthorized(addedPortfolio.Message);
                }
                else if (addedPortfolio.StatusCode == 404)
                {
                    return NotFound(addedPortfolio.Message);
                }
                else
                {
                    return StatusCode(500, "An internal server error occurred.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message, "Add Portfolio failed");
                return StatusCode(500, "An internal server error occurred.");
            }
        }

        [HttpDelete]
        [Authorize]
        [EnableRateLimiting("ip-sliding")]
        public async Task<IActionResult> DeletePortfolio(string symbol)
        {
            try
            {
                var deletedPortfolio = await _portfolioService.DeletePortfolio(symbol);
                if(deletedPortfolio.StatusCode == 200)
                {
                    return Ok(deletedPortfolio.Message);
                }
                else if (deletedPortfolio.StatusCode == 400)
                {
                    return Unauthorized(deletedPortfolio.Message);
                }
                else if (deletedPortfolio.StatusCode == 401)
                {
                    return NotFound(deletedPortfolio.Message);
                }
                else
                {
                    return StatusCode(500, "An internal server error occurred.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message, "Delete Portfolio failed");
                return StatusCode(500, "An internal server error occurred.");
            }
        }
    }
}