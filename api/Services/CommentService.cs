using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using api.Dtos;
using api.Extentions;
using api.Helpers;
using api.Interfaces;
using api.Mappers;
using api.Models;
using api.Response;
using Microsoft.AspNetCore.Identity;
using Microsoft.CodeAnalysis.CSharp;

namespace api.Services
{
    public class CommentService : ICommentService
    {
        private readonly ICommentRepository _commentRepo;
        private readonly IStockRepository _stockRepo;
        private readonly UserManager<AppUser> _usermanager;
        private readonly IFMPService _fmpService;
        private readonly ILogger<CommentService> _logger;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public CommentService(ICommentRepository commentRepo, IStockRepository stockRepo, UserManager<AppUser> usermanager, IFMPService fmpService, ILogger<CommentService> logger, IHttpContextAccessor httpContextAccessor)
        {
            _commentRepo = commentRepo;
            _stockRepo = stockRepo;
            _usermanager = usermanager;
            _fmpService = fmpService;
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;
            _logger.LogDebug("Nlog is integrated to Comment Service");
        }

        public async Task<ApiResponse> CreateComment(string symbol, CreateCommentDto commentDto)
        {
            try{
                var stock = await _stockRepo.GetStockBySymbolAsync(symbol);

                if (stock == null)
                {
                    stock = await _fmpService.FindStockBySymbolAsync(symbol);
                    if (stock == null)
                    {
                        return new ApiResponse
                        {
                            StatusCode = 404,
                            Message = "This Stock is not available right now.",
                        };
                    }
                    else
                    {
                        var createdStock = await _stockRepo.CreateStockAsync(stock);
                        return new ApiResponse
                        {
                            StatusCode = 200,
                            Message = "Stock Created Successfully",
                            Data = createdStock.ToStockDto()
                        };
                    }
                }

                var username = _httpContextAccessor.HttpContext?.User?.GetUsername();
                if (string.IsNullOrEmpty(username))
                {
                    return new ApiResponse{
                        StatusCode = 401,
                        Message = "Unauthorized."
                    };
                }

                var appUser = await _usermanager.FindByNameAsync(username);
                if (appUser == null)
                {
                    return new ApiResponse{
                        StatusCode = 404,
                        Message = "User not found."
                    };
                }

                var commentModel = commentDto.ToCommentFromCreate(stock.Id);
                commentModel.AppUserId = appUser.Id;
                await _commentRepo.CreateCommentAsync(commentModel);
                return new ApiResponse
                {
                    StatusCode = 200,
                    Message = "Comment Created Sucessfully",
                    Data = commentModel.ToCommentDto(),
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message, "Get Comments failed");
                return new ApiResponse
                {
                    StatusCode = 500,
                    Message = "An internal server error occurred.",
                };
            }
        }

        public async Task<ApiResponse> DeleteComment(int id)
        {
            try
            {
                var commentModel = await _commentRepo.DeleteCommentAsync(id);
            
                if (commentModel == null)
                {
                     return new ApiResponse
                    {
                        StatusCode = 500,
                        Message = "Could not delete.",
                    };
                }
                
                 return new ApiResponse
                {
                    StatusCode = 200,
                    Message = "Comment deleted successfully.",
                    Data = commentModel
                };
            }   
            catch (Exception ex)
            {
                _logger.LogError(ex.Message, "Delete Comment failed");
                return new ApiResponse
                {
                    StatusCode = 500,
                    Message = "An internal server error occurred.",
                };            
            }
        }

        public async Task<ApiResponse> GetAllComments(CommentQueryObject queryObject)
        {
            try
            {
                var comments = await _commentRepo.GetAllCommentsAsync(queryObject);
                var CommentDto = comments.Select(s => s.ToCommentDto());
                return new ApiResponse
                {
                    StatusCode = 200,
                    Message = "Comments gotten successfully",
                    Data = CommentDto
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message, "Get Comments failed");
                return new ApiResponse
                {
                    StatusCode = 500,
                    Message = "An internal server error occurred.",
                };
            }
        }

        public async Task<ApiResponse> GetCommentById(int id)
        {
            try{
                var comment = await _commentRepo.GetCommentByIdAsync(id);
                
                if(comment == null)
                {
                    return new ApiResponse
                    {
                        StatusCode = 404,
                        Message = "Comment not found.",
                    };
                }

                return new ApiResponse
                {
                    StatusCode = 200,
                    Message = "Comment gotten by Id successfully.",
                    Data = comment.ToCommentDto()
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message, "Get Comments by ID failed");

                return new ApiResponse
                {
                    StatusCode = 500,
                    Message = "An internal server error occurred.",
                };
            }
        }

        public async Task<ApiResponse> UpdateComment(int id, UpdateCommentRequestDto updateDto)
        {
            try{
                var comment = await _commentRepo.UpdateCommentAsync(id, updateDto.ToCommentFromUpdate());
                
                if (comment == null)
                {
                   return new ApiResponse
                    {
                        StatusCode = 500,
                        Message = "Could not update.",
                    };
                }
                
                return new ApiResponse
                {
                    StatusCode = 200,
                    Message = "Comment updated successfully.",
                    Data = comment.ToCommentDto()
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message, "Get Update Comment failed");

                return new ApiResponse
                {
                    StatusCode = 500,
                    Message = "An internal server error occurred.",
                };
            }
        }
    }
}