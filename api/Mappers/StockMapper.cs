using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using api.Dtos.Stock;
using api.Models;

namespace api.Mappers
{
    public static class StockMapper
    {
        public static StockDto ToStockDto(this Stock stockModel)
        {
            return new StockDto
            {
                Id = stockModel.Id, 
                Symbol = stockModel.Symbol, 
                CompanyName = stockModel.CompanyName, 
                Purchase = stockModel.Purchase, 
                LastDiv = stockModel.LastDiv, 
                Industry = stockModel.Industry, 
                MarketCap = stockModel.MarketCap,
                Comments = stockModel.Comments.Select(c => c.ToCommentDto()).ToList(),
                
                /* how to map without using the function
                Comments = stockModel.Comments
                .Select(c => new CommentDto
                {
                    Id = c.Id,
                    Content = c.Content,
                    CreatedAt = c.CreatedAt,
                    StockId = c.StockId
                })
                .ToList(), */

            };
        }

        public static Stock ToStockFromCreateDto(this CreateStockRequestDto stockDto)
        {
            return new Stock
            {
                Symbol = stockDto.Symbol, 
                CompanyName = stockDto.CompanyName, 
                Purchase = stockDto.Purchase, 
                LastDiv = stockDto.LastDiv, 
                Industry = stockDto.Industry, 
                MarketCap = stockDto.MarketCap
            };
        }
    }
}