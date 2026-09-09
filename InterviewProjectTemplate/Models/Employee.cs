namespace InterviewProjectTemplate.Models
{
    public class Employee
    {
        public int Id { get; set; }

        public string EmployeeIdentifier { get; set; } = string.Empty;

        public bool IsActive { get; set; } = true;

        public ICollection<MoodEntry> MoodEntries { get; set; }
            = new List<MoodEntry>();
    }
}
