using InterviewProjectTemplate.Data;
using InterviewProjectTemplate.Models;
using InterviewProjectTemplate.Services;
using Microsoft.EntityFrameworkCore;

namespace InterviewProjectTemplate.Tests;

public class MoodServiceListTests
{
    private static AppDbContext CreateContext() => new(
        new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    [Fact]
    public async Task GetAll_ReturnsEmptyListWhenNoEntriesExist()
    {
        // Arrange
        await using var context = CreateContext();
        var service = new MoodService(context, TimeProvider.System);
        // Act
        var entries = await service.GetAllAsync();
        // Assert
        Assert.Empty(entries);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("A productive day.")]
    public async Task GetAll_MapsFieldsAndIncludesInactiveEmployeeHistory(string? comment)
    {
        // Arrange
        await using var context = CreateContext();
        var timestamp = new DateTime(2026, 9, 9, 10, 30, 0, DateTimeKind.Utc);
        context.MoodEntries.Add(new MoodEntry
        {
            Id = 42,
            Employee = new Employee { EmployeeIdentifier = "EMP001", IsActive = false },
            Rating = MoodRating.FeelingGreat,
            Comment = comment,
            CreatedAtUtc = timestamp,
            SubmissionDate = DateOnly.FromDateTime(timestamp)
        });
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();
        var service = new MoodService(context, TimeProvider.System);
        // Act
        var entries = await service.GetAllAsync();
        // Assert
        var entry = Assert.Single(entries);
        Assert.Equal(42, entry.Id);
        Assert.Equal("EMP001", entry.EmployeeIdentifier);
        Assert.Equal(MoodRating.FeelingGreat, entry.Rating);
        Assert.Equal(comment, entry.Comment);
        Assert.Equal(timestamp, entry.CreatedAtUtc);
    }

    [Fact]
    public async Task GetAll_OrdersByTimestampDescendingThenIdDescending()
    {
        // Arrange
        await using var context = CreateContext();
        var employee = new Employee { EmployeeIdentifier = "EMP001" };
        var timestamp = new DateTime(2026, 9, 9, 12, 0, 0, DateTimeKind.Utc);
        // IDs and insertion order deliberately differ from chronological order.
        context.MoodEntries.AddRange(
            Entry(20, timestamp, employee),
            Entry(99, timestamp.AddDays(-1), employee),
            Entry(30, timestamp, employee),
            Entry(1, timestamp.AddDays(1), employee));
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();
        var service = new MoodService(context, TimeProvider.System);
        // Act
        var entries = await service.GetAllAsync();
        // Assert
        Assert.Equal(new long[] { 1, 30, 20, 99 }, entries.Select(e => e.Id));
    }

    private static MoodEntry Entry(long id, DateTime timestamp, Employee employee) => new()
    {
        Id = id,
        Employee = employee,
        Rating = MoodRating.PrettyGood,
        CreatedAtUtc = timestamp,
        SubmissionDate = DateOnly.FromDateTime(timestamp)
    };
}
