using InterviewProjectTemplate.Models;

namespace InterviewProjectTemplate.DTOs;

public record MoodStatistic(MoodRating Rating, int Count, decimal Percentage);
public record MoodDashboardResponse(
    IReadOnlyList<MoodEntryResponse> Entries,
    int TotalCount,
    int Page,
    int PageSize,
    IReadOnlyList<MoodStatistic> Statistics);
