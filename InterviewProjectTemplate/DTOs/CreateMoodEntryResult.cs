namespace InterviewProjectTemplate.DTOs
{
    public enum CreateMoodEntryStatus
    {
        Success,
        InvalidInput,
        EmployeeNotFound,
        EmployeeInactive,
        AlreadySubmitted
    }

    public record CreateMoodEntryResult(CreateMoodEntryStatus Status, long? EntryId = null);
}
