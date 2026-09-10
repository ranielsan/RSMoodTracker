using InterviewProjectTemplate.Models;
using Microsoft.EntityFrameworkCore;

namespace InterviewProjectTemplate.Data;

public static class TestMoodSeeder
{
    public static async Task SeedAsync(AppDbContext context, bool enabled)
    {
        if (!enabled) return;

        string[] identifiers = ["EMP001", "EMP002", "EMP003", "EMP004", "EMP005"];
        var employees = await context.Employees
            .Where(e => identifiers.Contains(e.EmployeeIdentifier))
            .OrderBy(e => e.EmployeeIdentifier).ToListAsync();
        var start = new DateOnly(2026, 9, 7);
        var employeeIds = employees.Select(e => e.Id).ToArray();

        for (var day = 0; day < 3; day++)
        {
            var date = start.AddDays(day);
            // Compare dates in SQL; this provider cannot materialize DateOnly projections.
            var existingEmployeeIds = await context.MoodEntries
                .Where(e => employeeIds.Contains(e.EmployeeId) && e.SubmissionDate == date)
                .Select(e => e.EmployeeId).ToListAsync();
            var occupied = existingEmployeeIds.ToHashSet();
            foreach (var employee in employees)
            {
                if (occupied.Contains(employee.Id)) continue;
                var index = Array.IndexOf(identifiers, employee.EmployeeIdentifier);
                context.MoodEntries.Add(new MoodEntry
                {
                    EmployeeId = employee.Id,
                    SubmissionDate = date,
                    CreatedAtUtc = date.ToDateTime(new TimeOnly(9 + index, 0), DateTimeKind.Utc),
                    Rating = (MoodRating)((index + day) % 4 + 1),
                    Comment = (index + day) % 2 == 0 ? "Sample submission for dashboard testing." : null
                });
            }
        }
        await context.SaveChangesAsync();
    }
}
