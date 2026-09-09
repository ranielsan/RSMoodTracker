using System.ComponentModel.DataAnnotations;

namespace InterviewProjectTemplate.DTOs
{
    public class AdminLoginRequest
    {
        [Required]
        [StringLength(256)]
        public string Username { get; set; } = string.Empty;

        [Required]
        public string Password { get; set; } = string.Empty;
    }
}
