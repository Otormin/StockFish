using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using api.Extentions;
using api.Interfaces;
using api.Mappers;
using api.Models;
using api.Response;
using Microsoft.AspNetCore.Identity;

namespace api.Services
{
    public class PortfolioService : IPortfolioService
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly IStockRepository _stockRepo;
        private readonly IPortfolioRepository _portfolioRepo;
        private readonly IFMPService _fmpService;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<PortfolioService> _logger;
        public PortfolioService(UserManager<AppUser> userManager, 
        IStockRepository stockRepo, IPortfolioRepository portfolioRepo, IFMPService fmpService, IHttpContextAccessor httpContextAccessor, ILogger<PortfolioService> logger)
        {
            _userManager = userManager;
            _stockRepo = stockRepo;
            _portfolioRepo = portfolioRepo;
            _fmpService = fmpService;
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;
            _logger.LogDebug("Nlog is integrated to Portfolio Serivce");
        }

        public async Task<ApiResponse> AddPortfolio(string symbol)
        {
            try{
                var username = _httpContextAccessor.HttpContext?.User?.GetUsername();
                if (string.IsNullOrEmpty(username))
                {
                    return new ApiResponse{
                        StatusCode = 401,
                        Message = "Unauthorized"
                    };
                }

                var appUser = await _userManager.FindByNameAsync(username);

                if (appUser == null)
                {
                    return new ApiResponse
                    {
                        StatusCode = 401,
                        Message = "Unauthorized"
                    };
                }

                var stock = await _stockRepo.GetStockBySymbolAsync(symbol);

                if (stock == null)
                {
                    stock = await _fmpService.FindStockBySymbolAsync(symbol);
                    if (stock == null)
                    {
                        return new ApiResponse{
                            StatusCode = 404,
                            Message = "This Stock is not available right now."
                        };
                    }
                    else
                    {
                        var createdStock = await _stockRepo.CreateStockAsync(stock);
                        return new ApiResponse
                        {
                            StatusCode = 200,
                            Message = "Stock created successfully",
                            Data = createdStock.ToStockDto()
                        };
                    }
                }

                var userPortfolio = await _portfolioRepo.GetUserPortfolio(appUser);

                if(userPortfolio.Any(e => e.Symbol.ToLower() == symbol.ToLower())) return new ApiResponse{StatusCode = 400, Message = "Cannot add same stock twice"};
            
                var portfolioModel = new Portfolio
                {
                    StockId = stock.Id,
                    AppUserId = appUser.Id,
                };

                await _portfolioRepo.CreateUserPortfolioAsync(portfolioModel);

                if(portfolioModel == null)
                {
                    return new ApiResponse{
                        StatusCode = 500, 
                        Message = "Could not create"
                    };
                }
                else
                {
                    return new ApiResponse{
                        StatusCode = 200, 
                        Message = "Portfolio created successfully"
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message, "Add Portfolio failed");
                return new ApiResponse{
                    StatusCode = 500, 
                    Message = "An internal server error occurred."
                };
            }
        }

        public async Task<ApiResponse> DeletePortfolio(string symbol)
        {
            try
            {
                var username = _httpContextAccessor.HttpContext?.User?.GetUsername();
                if (string.IsNullOrEmpty(username))
                {
                    return new ApiResponse{
                        StatusCode = 401,
                        Message = "Unauthorized"
                    };
                }

                var appUser = await _userManager.FindByNameAsync(username);

                if (appUser == null)
                {
                    return new ApiResponse
                    {
                        StatusCode = 401,
                        Message = "Unauthorized"
                    };
                }

                var userPortfolio = await _portfolioRepo.GetUserPortfolio(appUser);

                var filteredStock = userPortfolio.Where(s => s.Symbol.ToLower() == symbol.ToLower()).ToList();

                if(filteredStock.Count() == 1)
                {
                    await _portfolioRepo.DeleteUserPortfolioAsync(appUser, symbol);
                }
                else
                {
                    return new ApiResponse{
                        StatusCode = 400, 
                        Message = "Stock is not in your portfolio."
                    };
                }

                return new ApiResponse{
                    StatusCode = 200, 
                    Message = "Portfolio successfully deleted"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message, "Delete Portfolio failed");
                return new ApiResponse{
                    StatusCode = 500, 
                    Message = "An internal server error occurred."
                };
            }
        }

        public async Task<ApiResponse> GetUserPortfolio()
        {
            try
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
                var username = _httpContextAccessor.HttpContext?.User?.GetUsername();
                if (string.IsNullOrEmpty(username))
                {
                    return new ApiResponse{
                        StatusCode = 401,
                        Message = "Unauthorized"
                    };
                }

                var appUser = await _userManager.FindByNameAsync(username);
                if (appUser == null)
                {
                    return new ApiResponse
                    {
                        StatusCode = 401,
                        Message = "Unauthorized"
                    };
                }

                var userPortfolio = await _portfolioRepo.GetUserPortfolio(appUser);
                return new ApiResponse
                {
                    StatusCode = 200,
                    Message = "User Portfolio successfully retrieved",
                    Data = userPortfolio
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message, "Get User Portfolio failed");
                return new ApiResponse{
                    StatusCode = 500, 
                    Message = "An internal server error occurred."
                };
            }
        }
    }
}