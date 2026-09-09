using InterviewProjectTemplate.DTOs;

namespace InterviewProjectTemplate.Services
{
    public interface IMoodService
    {
        Task<CreateMoodEntryResult> CreateAsync(CreateMoodEntryRequest request, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<MoodEntryResponse>> GetAllAsync(CancellationToken cancellationToken = default);
    }
}
