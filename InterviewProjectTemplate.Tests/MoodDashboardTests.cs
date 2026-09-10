using System.ComponentModel.DataAnnotations;
using InterviewProjectTemplate.Data;
using InterviewProjectTemplate.DTOs;
using InterviewProjectTemplate.Models;
using InterviewProjectTemplate.Services;
using InterviewProjectTemplate.Controllers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace InterviewProjectTemplate.Tests;

public class MoodDashboardTests
{
    private static AppDbContext Context() => new(new DbContextOptionsBuilder<AppDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    private static async Task Seed(AppDbContext context)
    {
        var day = new DateTime(2026, 9, 10, 0, 0, 0, DateTimeKind.Utc);
        for (var i = 0; i < 6; i++)
        {
            var timestamp = i switch { 0 => day.AddTicks(-1), 5 => day.AddDays(1), _ => day.AddHours(i) };
            context.MoodEntries.Add(new MoodEntry {
                Id = i + 1, Employee = new Employee { EmployeeIdentifier = $"EMP{i}", IsActive = false },
                Rating = (MoodRating)(i % 4 + 1), CreatedAtUtc = timestamp,
                SubmissionDate = DateOnly.FromDateTime(timestamp), Comment = null });
        }
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();
    }

    [Fact]
    public async Task Dashboard_InclusiveUtcDayAndPaginationPreserveStatistics()
    {
        // Arrange
        await using var context = Context();
        await Seed(context);
        var service = new MoodService(context, TimeProvider.System);
        var query = new MoodDashboardRequest { From = new(2026, 9, 10), To = new(2026, 9, 10), PageSize = 2 };
        // Act
        var first = await service.GetDashboardAsync(query);
        query.Page = 2;
        var second = await service.GetDashboardAsync(query);
        // Assert
        Assert.Equal(4, first.TotalCount);
        Assert.Equal(new long[] { 5, 4 }, first.Entries.Select(e => e.Id));
        Assert.Equal(new long[] { 3, 2 }, second.Entries.Select(e => e.Id));
        Assert.Equal(first.Statistics, second.Statistics);
        Assert.All(first.Statistics, s => { Assert.Equal(1, s.Count); Assert.Equal(25m, s.Percentage); });
        Assert.Equal("EMP4", first.Entries[0].EmployeeIdentifier);
        Assert.Null(first.Entries[0].Comment);
    }

    [Fact]
    public async Task Dashboard_MoodFilterAndOutOfRangePage()
    {
        // Arrange
        await using var context = Context();
        await Seed(context);
        var service = new MoodService(context, TimeProvider.System);
        // Act
        var result = await service.GetDashboardAsync(new() { Rating = MoodRating.NotGoodAtAll, Page = 999, PageSize = 1 });
        // Assert
        Assert.Equal(2, result.TotalCount);
        Assert.Equal(2, result.Page);
        Assert.Single(result.Entries);
        Assert.Equal(100m, result.Statistics.Single(s => s.Rating == MoodRating.NotGoodAtAll).Percentage);
        Assert.Equal(0m, result.Statistics.Single(s => s.Rating == MoodRating.FeelingGreat).Percentage);
    }

    [Fact]
    public async Task Dashboard_EmptyAndOneSidedDatesAndRounding()
    {
        // Arrange
        await using var context = Context();
        await Seed(context);
        var service = new MoodService(context, TimeProvider.System);
        // Act
        var empty = await service.GetDashboardAsync(new() { From = new(2027, 1, 1) });
        var before = await service.GetDashboardAsync(new() { To = new(2026, 9, 9) });
        var all = await service.GetDashboardAsync(new());
        // Assert
        Assert.Empty(empty.Entries);
        Assert.Equal(0, empty.TotalCount);
        Assert.Equal(1, empty.Page);
        Assert.Equal(4, empty.Statistics.Count);
        Assert.All(empty.Statistics, s => Assert.Equal(0m, s.Percentage));
        Assert.Single(before.Entries);
        Assert.Equal(6, all.TotalCount);
        Assert.Equal(33.3m, all.Statistics[0].Percentage);
    }

    [Theory]
    [InlineData(0, 10, null)]
    [InlineData(1, 0, null)]
    [InlineData(1, 101, null)]
    [InlineData(1000001, 10, null)]
    [InlineData(1, 10, 5)]
    public async Task Dashboard_RejectsInvalidParameters(int page, int size, int? rating)
    {
        // Arrange
        await using var context = Context();
        var service = new MoodService(context, TimeProvider.System);
        // Act / Assert
        await Assert.ThrowsAsync<ValidationException>(() => service.GetDashboardAsync(new() { Page = page, PageSize = size, Rating = (MoodRating?)rating }));
    }

    [Fact]
    public async Task Dashboard_RejectsReversedDates()
    {
        // Arrange
        await using var context = Context();
        var service = new MoodService(context, TimeProvider.System);
        // Act / Assert
        await Assert.ThrowsAsync<ValidationException>(() => service.GetDashboardAsync(new() { From = new(2026, 9, 11), To = new(2026, 9, 10) }));
    }

    [Fact]
    public async Task Controller_ForwardsQueryAndCancellation()
    {
        // Arrange
        var query = new MoodDashboardRequest { Page = 2 };
        using var cancellation = new CancellationTokenSource();
        var response = new MoodDashboardResponse([], 0, 1, 10, []);
        var service = new Mock<IMoodService>();
        service.Setup(s => s.GetDashboardAsync(query, cancellation.Token)).ReturnsAsync(response);
        var controller = new AdminMoodsController(service.Object);
        // Act
        var result = await controller.GetDashboard(query, cancellation.Token);
        // Assert
        Assert.Same(response, Assert.IsType<OkObjectResult>(result.Result).Value);
        service.Verify(s => s.GetDashboardAsync(query, cancellation.Token), Times.Once);
    }
}
