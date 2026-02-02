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
        private readonly UserManager<AppUser> _userManager;
        private readonly IStockRepository _stockRepo;
        private readonly IPortfolioRepository _portfolioRepo;
        private readonly IFMPService _fmpService;
        private readonly ILogger<CommentController> _logger;
        public PortfolioController(UserManager<AppUser> userManager, 
        IStockRepository stockRepo, IPortfolioRepository portfolioRepo, IFMPService fmpService, ILogger<CommentController> logger)
        {
            _userManager = userManager;
            _stockRepo = stockRepo;
            _portfolioRepo = portfolioRepo;
            _fmpService = fmpService;
            _logger = logger;
            _logger.LogDebug("Nlog is integrated to Portfolio Controller");
        }

        [HttpGet]
        [Authorize]
        [EnableRateLimiting("fixed")]
        public async Task<IActionResult> GetUserPortfolio()
        {
            /*
                var user = await _userManager.GetUserAsync(User);

                if (user == null)
                    return Unauthorized();

                // user.Id is now available
                var portfolio = await _context.Portfolios
                    .Where(p => p.UserId == user.Id)
                    .ToListAsync();
                
                return Ok(portfolio);
            */
            
            try
            {
                var username = User.GetUsername();
                var appUser = await _userManager.FindByNameAsync(username);
                var userPortfolio = await _portfolioRepo.GetUserPortfolio(appUser);
                return Ok(userPortfolio);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message, "Get User Portfolio failed");
                return StatusCode(500, "An internal server error occurred.");
            }
        }

        [HttpPost]
        [Authorize]
        [EnableRateLimiting("fixed")]
        public async Task<IActionResult> AddPortfolio(string symbol)
        {
            try{
                var username = User.GetUsername();
                var appUser = await _userManager.FindByNameAsync(username);
                var stock = await _stockRepo.GetStockBySymbolAsync(symbol);

                if (stock == null)
                {
                    stock = await _fmpService.FindStockBySymbolAsync(symbol);
                    if (stock == null)
                    {
                        return BadRequest("This Stock does not exist");
                    }
                    else
                    {
                        await _stockRepo.CreateStockAsync(stock);
                    }
                }

                if(stock == null) return BadRequest("Stock not found");

                var userPortfolio = await _portfolioRepo.GetUserPortfolio(appUser);

                if(userPortfolio.Any(e => e.Symbol.ToLower() == symbol.ToLower())) return BadRequest("Cannot add same stock twice");
            
                var portfolioModel = new Portfolio
                {
                    StockId = stock.Id,
                    AppUserId = appUser.Id,
                };

                await _portfolioRepo.CreateUserPortfolioAsync(portfolioModel);

                if(portfolioModel == null)
                {
                    return StatusCode(500, "Could not create");
                }
                else
                {
                    return Created();
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
        [EnableRateLimiting("fixed")]
        public async Task<IActionResult> DeletePortfolio(string symbol)
        {
            try
            {
                var username = User.GetUsername();
                var appUser = await _userManager.FindByNameAsync(username);

                var userPortfolio = await _portfolioRepo.GetUserPortfolio(appUser);

                var filteredStock = userPortfolio.Where(s => s.Symbol.ToLower() == symbol.ToLower()).ToList();

                if(filteredStock.Count() == 1)
                {
                    await _portfolioRepo.DeleteUserPortfolioAsync(appUser, symbol);
                }
                else
                {
                    return BadRequest("Stock is not in your portfolio");
                }

                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message, "Delete Portfolio failed");
                return StatusCode(500, "An internal server error occurred.");
            }
        }
    }
}