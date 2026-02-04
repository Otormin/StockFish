using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using api.Dtos.Stock;
using api.Helpers;
using api.Response;

namespace api.Interfaces
{
    public interface IStockService
    {
        Task<ApiResponse> GetAllStocks(QueryObject query);
        Task<ApiResponse> GetStockById(int id);
        Task<ApiResponse> CreateStock(CreateStockRequestDto stockDto);
        Task<ApiResponse> UpdateStock(int id, UpdateStockrequestDto updateDto);
        Task<ApiResponse> DeleteStock(int id);
    }
}