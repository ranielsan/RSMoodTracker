using InterviewProjectTemplate.Models;

namespace InterviewProjectTemplate.DTOs
{
    public record MoodEntryResponse(
    long Id,
    string EmployeeIdentifier,
    MoodRating Rating,
    string? Comment,
    DateTime CreatedAtUtc);
}
