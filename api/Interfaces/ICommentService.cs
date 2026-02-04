using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using api.Dtos;
using api.Helpers;
using api.Response;

namespace api.Interfaces
{
    public interface ICommentService
    {
        Task<ApiResponse> GetAllComments(CommentQueryObject queryObject);
        Task<ApiResponse> GetCommentById(int id);
        Task<ApiResponse> CreateComment(string symbol, CreateCommentDto commentDto);
        Task<ApiResponse> UpdateComment(int id, UpdateCommentRequestDto updateDto);
        Task<ApiResponse> DeleteComment(int id);
    }
}