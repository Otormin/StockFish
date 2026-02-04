using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using api.Response;

namespace api.Interfaces
{
    public interface IPortfolioService
    {
        Task<ApiResponse> GetUserPortfolio();
        Task<ApiResponse> AddPortfolio(string symbol);
        Task<ApiResponse> DeletePortfolio(string symbol);
    }
}