using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace api.Dtos
{
    public class UpdateCommentRequestDto
    {
        [Required]
        [MinLength(5, ErrorMessage = "Title must be up to 5 characters")]
        [MaxLength(280, ErrorMessage = "Title cannot be more than 280 characters")]
        public string Title {get; set; } = string.Empty;
        
        [Required]
        [MinLength(5, ErrorMessage = "Content must be up to 5 characters")]
        [MaxLength(280, ErrorMessage = "Content cannot be more than 280 characters")]
        public string Content {get; set; } = string.Empty;
    }
}