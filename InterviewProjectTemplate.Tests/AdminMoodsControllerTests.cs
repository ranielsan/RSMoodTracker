using InterviewProjectTemplate.Controllers;
using InterviewProjectTemplate.DTOs;
using InterviewProjectTemplate.Models;
using InterviewProjectTemplate.Services;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace InterviewProjectTemplate.Tests;

// Authorization must also be checked through the HTTP pipeline.
public class AdminMoodsControllerTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task GetAll_ReturnsServiceEntriesAndForwardsCancellation(bool populated)
    {
        // Arrange
        IReadOnlyList<MoodEntryResponse> entries = populated
            ? new[] { new MoodEntryResponse(1, "EMP001", MoodRating.PrettyGood, null, DateTime.UtcNow) }
            : Array.Empty<MoodEntryResponse>();
        using var cancellation = new CancellationTokenSource();
        var service = new Mock<IMoodService>();
        service.Setup(s => s.GetAllAsync(cancellation.Token)).ReturnsAsync(entries);
        var controller = new AdminMoodsController(service.Object);
        // Act
        var response = await controller.GetAll(cancellation.Token);
        // Assert
        var result = Assert.IsType<OkObjectResult>(response.Result);
        Assert.Same(entries, result.Value);
        service.Verify(s => s.GetAllAsync(cancellation.Token), Times.Once);
    }
}
