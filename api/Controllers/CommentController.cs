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
        private readonly ICommentService _commentService;
        private readonly ILogger<CommentController> _logger;

        public CommentController(ICommentService commentService, ILogger<CommentController> logger)
        {
            _commentService = commentService;
            _logger = logger;
            _logger.LogDebug("Nlog is integrated to Comment Controller");
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> GetAllComments([FromQuery] CommentQueryObject queryObject)
        {
            try
            {
                var comments = await _commentService.GetAllComments(queryObject);

                if (comments.StatusCode == 200)
                {
                    return Ok(comments.Data);
                }

                return StatusCode(500, comments.Message);
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
                var comment = await _commentService.GetCommentById(id);

                if (comment.StatusCode == 200)
                {
                    return Ok(comment.Data);
                }

                else if (comment.StatusCode == 404)
                {
                    return NotFound(comment.Message);
                }

                else
                {
                    return StatusCode(500, comment.Message);
                }

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
                var createdComment = await _commentService.CreateComment(symbol, commentDto);

                if (createdComment.StatusCode == 200)
                {
                    return Ok(createdComment.Data);
                }

                else if (createdComment.StatusCode == 400)
                {
                    return BadRequest(createdComment.Message);
                }

                else if (createdComment.StatusCode == 401)
                {
                    return Unauthorized(createdComment.Message);
                }

                else if (createdComment.StatusCode == 404)
                {
                    return NotFound(createdComment.Message);
                }

                else
                {
                    return StatusCode(500, createdComment.Message);
                }
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
                var updatedComment = await _commentService.UpdateComment(id, updateDto);

                if (updatedComment.StatusCode == 200)
                {
                    return Ok(updatedComment.Data);
                }

                else
                {
                    return StatusCode(500, updatedComment.Message);
                }
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
                var deletedComment = await _commentService.DeleteComment(id);

                if (deletedComment.StatusCode == 200)
                {
                    return Ok(deletedComment.Data);
                }

                else
                {
                    return StatusCode(500, deletedComment.Message);
                }
            }   
            catch (Exception ex)
            {
                _logger.LogError(ex.Message, "Delete Comment failed");
                return StatusCode(500, "An internal server error occurred.");
            }
        }
    }
}