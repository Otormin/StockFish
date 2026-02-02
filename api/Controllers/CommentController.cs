using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using api.Dtos;
using api.Dtos.Comment;
using api.Extentions;
using api.Helpers;
using api.Interfaces;
using api.Mappers;
using api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace api.Controllers
{
    [Route("api/comments")]
    [ApiController]
    //To add rate limiting to all your endpoints, althugh it is not adviced
    //[EnableRateLimiting("fixed")]
    public class CommentController : ControllerBase
    {
        private readonly ICommentRepository _commentRepo;
        private readonly IStockRepository _stockRepo;
        private readonly UserManager<AppUser> _usermanager;
        private readonly IFMPService _fmpService;
        private readonly ILogger<CommentController> _logger;

        public CommentController(ICommentRepository commentRepo, IStockRepository stockRepo, UserManager<AppUser> usermanager, IFMPService fmpService, ILogger<CommentController> logger)
        {
            _commentRepo = commentRepo;
            _stockRepo = stockRepo;
            _usermanager = usermanager;
            _fmpService = fmpService;
            _logger = logger;
            _logger.LogDebug("Nlog is integrated to Comment Controller");
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> GetAllComments([FromQuery] CommentQueryObject queryObject)
        {
            try
            {
                var comments = await _commentRepo.GetAllCommentsAsync(queryObject);
                var CommentDto = comments.Select(s => s.ToCommentDto());
                return Ok(CommentDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message, "Get Comments failed");
                return StatusCode(500, "An internal server error occurred.");
            }
        }

        [HttpGet("{id:int}")]
        [EnableRateLimiting("fixed")]
        public async Task<IActionResult> GetCommentById([FromRoute] int id)
        {
            try{
                var comment = await _commentRepo.GetCommentByIdAsync(id);
                
                if(comment == null)
                {
                    return NotFound();
                }

                return Ok(comment.ToCommentDto());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message, "Get Comments by ID failed");
                return StatusCode(500, "An internal server error occurred.");
            }
        }

        [HttpPost]
        [Authorize]
        // The leading "/" overrides the controller's base route (e.g., api/comment)
        // This creates the exact URL: http://localhost:5172/api/stocks/{symbol}/comments
        [Route("/api/stocks/{symbol:alpha}/comments")]
        [EnableRateLimiting("fixed")]
        public async Task<IActionResult> CreateComment([FromRoute] string symbol, CreateCommentDto commentDto)
        {
            try{
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

                var username = User.GetUsername();
                var appUser = await _usermanager.FindByNameAsync(username); 

                var commentModel = commentDto.ToCommentFromCreate(stock.Id);
                commentModel.AppUserId = appUser.Id;
                await _commentRepo.CreateCommentAsync(commentModel);
                return CreatedAtAction(nameof(GetCommentById), new {id = commentModel.Id}, commentModel.ToCommentDto());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message, "Post Comment under a particular stock failed");
                return StatusCode(500, "An internal server error occurred.");
            }
        }

        [HttpPut]
        [Route("{id:int}")]
        [Authorize]
        public async Task<IActionResult> UpdateComment([FromRoute] int id, [FromBody] UpdateCommentRequestDto updateDto)
        {
            try{
                var comment = await _commentRepo.UpdateCommentAsync(id, updateDto.ToCommentFromUpdate());
                
                if (comment == null)
                {
                    return NotFound("Comment not found");
                }
                
                return Ok(comment.ToCommentDto());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message, "Get Update Comment failed");
                return StatusCode(500, "An internal server error occurred.");
            }
        }

        [HttpDelete]
        [Route("{id:int}")]
        [Authorize]
        [EnableRateLimiting("fixed")]
        public async Task<IActionResult> DeleteComment([FromRoute] int id)
        {
            try
            {
                var commentModel = await _commentRepo.DeleteCommentAsync(id);
            
                if (commentModel == null)
                {
                    return NotFound("Comment does not exist");
                }
                
                return Ok(commentModel);
            }   
            catch (Exception ex)
            {
                _logger.LogError(ex.Message, "Delete Comment failed");
                return StatusCode(500, "An internal server error occurred.");
            }
        }
    }
}