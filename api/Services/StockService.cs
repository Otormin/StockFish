using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using api.Dtos.Stock;
using api.Helpers;
using api.Interfaces;
using api.Mappers;
using api.Models;
using api.Response;
using Microsoft.AspNetCore.Http.HttpResults;

namespace api.Services
{
    public class StockService : IStockService
    {
        private readonly IStockRepository _stockrepo;
        private readonly ILogger<StockService> _logger;
        public StockService(IStockRepository stockrepo, ILogger<StockService> logger)
        {
            _stockrepo = stockrepo;
            _logger = logger;
             _logger.LogDebug("Nlog is integrated to Stock Service");
        }

        public async Task<ApiResponse> CreateStock(CreateStockRequestDto stockDto)
        {
            try
            {
                var stockModel = stockDto.ToStockFromCreateDto();
                var createdStock = await _stockrepo.CreateStockAsync(stockModel);
                return new ApiResponse
                {
                    StatusCode = 200,
                    Message = "Stock created successfully",
                    Data = createdStock.ToStockDto()
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message, "Create Stock failed");
                return new ApiResponse
                {
                    StatusCode = 500,
                    Message = "An internal server error occurred.",
                };
            }
        }

        public async Task<ApiResponse> DeleteStock(int id)
        {
            try{
                var stockModel = await _stockrepo.DeleteStockAsync(id);
                if(stockModel == null)
                {
                    return new ApiResponse
                    {
                        StatusCode = 404,
                        Message = "Stock not found"
                    };
                }

                return new ApiResponse
                {
                    StatusCode = 200,
                    Message = "Stock deleted successfully"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message, "Delete Stock failed");
                return new ApiResponse
                {
                    StatusCode = 500,
                    Message = "An internal server error occurred."
                };
            }
        }

        public async Task<ApiResponse> GetAllStocks(QueryObject query)
        {
            try{
                var stocks = await _stockrepo.GetAllStocksAsync(query);
                
                var stockDto = stocks.Select(s =>s.ToStockDto()).ToList();
                
                return new ApiResponse
                {
                    StatusCode = 200,
                    Message = "Stocks gotten successfully",
                    Data = stockDto
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message, "Get All Stocks failed");
                return new ApiResponse
                {
                    StatusCode = 500,
                    Message = "An internal server error occurred.",
                };
            }
        }

        public async Task<ApiResponse> GetStockById(int id)
        {
            try{
                var stock = await _stockrepo.GetStockByIdAsync(id);

                if (stock == null)
                {
                    return new ApiResponse
                    {
                        StatusCode = 404,
                        Message = "Stock not found"
                    };
                }

                return new ApiResponse
                {
                    StatusCode = 200,
                    Message = "Stock gotten by ID successfuly",
                    Data = stock.ToStockDto(),
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message, "Get Stock by ID failed");
                return new ApiResponse
                {
                    StatusCode = 500,
                    Message = "An internal server error occurred.",
                };
            }
        }

        public async Task<ApiResponse> UpdateStock(int id, UpdateStockrequestDto updateDto)
        {
            try
            {
                var stockModel = await _stockrepo.UpdateStockAsync(id, updateDto);
                if(stockModel == null)
                {
                    return new ApiResponse
                    {
                        StatusCode = 404,
                        Message = "Stock not found",
                    };
                }

                return new ApiResponse{
                    StatusCode = 200,
                    Message = "Stocks updated successfully",
                    Data = stockModel.ToStockDto()
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message, "Update Stock failed");
                return new ApiResponse
                {
                    StatusCode = 500,
                    Message = "An internal server error occurred.",
                };
            }
        }
    }
}