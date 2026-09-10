using System.ComponentModel.DataAnnotations;
using InterviewProjectTemplate.Models;

namespace InterviewProjectTemplate.DTOs;

public class MoodDashboardRequest : IValidatableObject
{
    public DateOnly? From { get; set; }
    public DateOnly? To { get; set; }
    [EnumDataType(typeof(MoodRating))]
    public MoodRating? Rating { get; set; }
    [Range(1, 1000000)]
    public int Page { get; set; } = 1;
    [Range(1, 100)]
    public int PageSize { get; set; } = 10;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (From.HasValue && To.HasValue && From > To)
            yield return new ValidationResult("From must be on or before To.", new[] { nameof(To) });
    }
}
