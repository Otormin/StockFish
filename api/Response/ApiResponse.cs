using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace api.Response
{
    public class ApiResponse
    {
        public int StatusCode { get; set; }
        public string Message { get; set; } = string.Empty;
        public object? Data { get; set; }

        /* public static ApiResponse Success(string msg, object? data = null) => 
        new() { StatusCode = 200, Message = msg, Data = data }; */
        public static ApiResponse Success(string msg, object? data = null)
        { 
            return new ApiResponse{
                StatusCode = 200, 
                Message = msg, 
                Data = data 
            };
        }

        public static ApiResponse Unauthorized(string msg)
        {
            return new ApiResponse{ 
                StatusCode = 401, 
                Message = msg 
            };
        }

        public static ApiResponse BadRequest(string msg){ 
            return new ApiResponse
            {
                StatusCode = 400, 
                Message = msg 
            };
        }
            
        public static ApiResponse InternalError(string msg)
        {
            return new ApiResponse
            { 
                StatusCode = 500, 
                Message = msg 
            };
        }

        public static ApiResponse TooManyRequests(string msg){
            return new ApiResponse
            { 
                StatusCode = 429, 
                Message = msg 
            };
        }
    }
}