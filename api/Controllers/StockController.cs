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
        private readonly IStockRepository _stockrepo;
        private readonly ILogger<CommentController> _logger;
        public StockController(IStockRepository stockrepo, ILogger<CommentController> logger)
        {
            _stockrepo = stockrepo;
            _logger = logger;
             _logger.LogDebug("Nlog is integrated to Stock Controller");
        }

        [HttpGet]
        [EnableRateLimiting("fixed")]
        public async Task<IActionResult> GetAllStocks([FromQuery] QueryObject query)
        {
            try{
                var stocks = await _stockrepo.GetAllStocksAsync(query);
                
                var stockDto = stocks.Select(s =>s.ToStockDto()).ToList();
                
                return Ok(stockDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message, "Get All Stocks failed");
                return StatusCode(500, "An internal server error occurred.");
            }
        }

        [HttpGet("{id:int}")]
        [EnableRateLimiting("fixed")]
        public async Task<IActionResult> GetStockById([FromRoute] int id)
        {
            try{
                var stock = await _stockrepo.GetStockByIdAsync(id);

                if (stock == null)
                {
                    return NotFound();
                }

                return Ok(stock.ToStockDto());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message, "Get Stock by ID failed");
                return StatusCode(500, "An internal server error occurred.");
            }
        }

        [HttpPost]
        [Authorize]
        [EnableRateLimiting("fixed")]
        public async Task<IActionResult> Create([FromBody] CreateStockRequestDto stockDto)
        {
            try
            {
                var stockModel = stockDto.ToStockFromCreateDto();
                await _stockrepo.CreateStockAsync(stockModel);
                return CreatedAtAction(nameof(GetStockById), new {id = stockModel.Id}, stockModel.ToStockDto());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message, "Create Stock failed");
                return StatusCode(500, "An internal server error occurred.");
            }
        }

        [HttpPut]
        [Authorize]
        [Route("{id:int}")]
        [EnableRateLimiting("fixed")]
        public async Task<IActionResult> Update([FromRoute] int id, [FromBody] UpdateStockrequestDto updateDto)
        {
            try
                {
                var stockModel = await _stockrepo.UpdateStockAsync(id, updateDto);
                if(stockModel == null)
                {
                    return NotFound();
                }

                return Ok(stockModel.ToStockDto());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message, "Update Stock failed");
                return StatusCode(500, "An internal server error occurred.");
            }
        }

        [HttpDelete]
        [Authorize]
        [Route("{id:int}")]
        [EnableRateLimiting("fixed")]
        public async Task<IActionResult> Delete([FromRoute] int id)
        {
            try{
                var stockModel = await _stockrepo.DeleteStockAsync(id);
                if(stockModel == null)
                {
                    return NotFound();
                }

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message, "Delete Stock failed");
                return StatusCode(500, "An internal server error occurred.");
            }
        }
    }
}