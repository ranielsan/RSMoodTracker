namespace InterviewProjectTemplate.Models
{
    public class MoodEntry
    {
        public long Id { get; set; }

        public int EmployeeId { get; set; }

        public Employee Employee { get; set; } = null!;

        public MoodRating Rating { get; set; }

        public string? Comment { get; set; }

        public DateTime CreatedAtUtc { get; set; }

        public DateOnly SubmissionDate { get; set; }
    }
}
