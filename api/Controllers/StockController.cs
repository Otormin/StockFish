using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using api.Dtos.Stock;
using api.Helpers;
using api.Interfaces;
using api.Mappers;
using api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace api.Controllers
{
    [Route("api/stocks")]
    [ApiController]
    public class StockController : ControllerBase
    {
        private readonly IStockService _stockservice;
        private readonly ILogger<CommentController> _logger;
        public StockController(IStockService stockservice, ILogger<CommentController> logger)
        {
            _stockservice = stockservice;
            _logger = logger;
             _logger.LogDebug("Nlog is integrated to Stock Controller");
        }

        [AllowAnonymous]
        [HttpGet]
        [EnableRateLimiting("ip-sliding")]
        public async Task<IActionResult> GetAllStocks([FromQuery] QueryObject query)
        {
            try{
                var stocks = await _stockservice.GetAllStocks(query);

                if (stocks.StatusCode == 200)
                {
                    return Ok(stocks.Data);
                }

                return StatusCode(500, "An internal server error occurred.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message, "Get All Stocks failed");
                return StatusCode(500, "An internal server error occurred.");
            }
        }

        [Authorize]
        [HttpGet("{id:int}")]
        [EnableRateLimiting("ip-sliding")]
        public async Task<IActionResult> GetStockById([FromRoute] int id)
        {
            try{
                var stock = await _stockservice.GetStockById(id);

                if (stock.StatusCode == 200)
                {
                    return Ok(stock.Data);
                }
                else if (stock.StatusCode == 404)
                {
                    return NotFound(stock.Message);
                }

                return StatusCode(500, "An internal server error occurred.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message, "Get Stock by ID failed");
                return StatusCode(500, "An internal server error occurred.");
            }
        }

        [HttpPost]
        [Authorize(Roles = "SuperAdmin,Admin")]
        [EnableRateLimiting("ip-sliding")]
        public async Task<IActionResult> CreateStock([FromBody] CreateStockRequestDto stockDto)
        {
            try
            {
                var createdStock = await _stockservice.CreateStock(stockDto);

                if (createdStock.StatusCode == 200)
                {
                    return Ok(createdStock.Data);
                }

                return StatusCode(500, "An internal server error occurred.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message, "Create Stock failed");
                return StatusCode(500, "An internal server error occurred.");
            }
        }

        [HttpPut]
        [Authorize(Roles = "SuperAdmin,Admin")]
        [Route("{id:int}")]
        [EnableRateLimiting("ip-sliding")]
        public async Task<IActionResult> UpdateStock([FromRoute] int id, [FromBody] UpdateStockrequestDto updateDto)
        {
            try
            {
                var updatedStock = await _stockservice.UpdateStock(id, updateDto);

                if (updatedStock.StatusCode == 200)
                {
                    return Ok(updatedStock.Data);
                }
                else if (updatedStock.StatusCode == 404)
                {
                    return NotFound(updatedStock.Message);
                }

                return StatusCode(500, "An internal server error occurred.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message, "Update Stock failed");
                return StatusCode(500, "An internal server error occurred.");
            }
        }

        [HttpDelete]
        [Authorize(Roles = "SuperAdmin,Admin")]
        [Route("{id:int}")]
        [EnableRateLimiting("ip-sliding")]
        public async Task<IActionResult> DeleteStock([FromRoute] int id)
        {
            try{
                var deletedStock = await _stockservice.DeleteStock(id);

                if (deletedStock.StatusCode == 200)
                {
                    return Ok(deletedStock.Message);
                }
                else if (deletedStock.StatusCode == 404)
                {
                    return NotFound(deletedStock.Message);
                }

                return StatusCode(500, "An internal server error occurred.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message, "Delete Stock failed");
                return StatusCode(500, "An internal server error occurred.");
            }
        }
    }
}