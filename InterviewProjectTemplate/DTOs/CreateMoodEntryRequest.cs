using InterviewProjectTemplate.Models;
using System.ComponentModel.DataAnnotations;

namespace InterviewProjectTemplate.DTOs
{
    public class CreateMoodEntryRequest
    {
        [Required]
        [StringLength(50)]
        public string EmployeeIdentifier { get; set; } = string.Empty;

        [Required]
        [EnumDataType(typeof(MoodRating))]
        public MoodRating? Rating { get; set; }

        [StringLength(1000)]
        public string? Comment { get; set; }
    }
}
