using InterviewProjectTemplate.Controllers;
using InterviewProjectTemplate.DTOs;
using InterviewProjectTemplate.Models;
using InterviewProjectTemplate.Services;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace InterviewProjectTemplate.Tests
{
    public class MoodsControllerTests
    {
        Mock<IMoodService> service;

        public MoodsControllerTests()
        {
            service = new Mock<IMoodService>();
        }

        [Theory]
        [InlineData(CreateMoodEntryStatus.Success, 201)]
        [InlineData(CreateMoodEntryStatus.InvalidInput, 400)]
        [InlineData(CreateMoodEntryStatus.EmployeeNotFound, 400)]
        [InlineData(CreateMoodEntryStatus.EmployeeInactive, 403)]
        [InlineData(CreateMoodEntryStatus.AlreadySubmitted, 409)]
        public async Task Create_ReturnsExpectedHttpStatus(CreateMoodEntryStatus serviceStatus, int expectedStatusCode)
        {
            // Arrange
            var request = new CreateMoodEntryRequest
            {
                EmployeeIdentifier = "EMP001",
                Rating = MoodRating.PrettyGood
            };

            service
                .Setup(s => s.CreateAsync(
                    request,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new CreateMoodEntryResult(serviceStatus));

            var controller = new MoodsController(service.Object);

            // Act
            var response = await controller.Create(
                request,
                CancellationToken.None);

            // Assert
            var objectResult = Assert.IsAssignableFrom<ObjectResult>(response);
            Assert.Equal(expectedStatusCode, objectResult.StatusCode);
        }

        [Fact]
        public async Task Create_PassesRequestAndCancellationTokenToService()
        {
            // Arrange
            var request = new CreateMoodEntryRequest
            {
                EmployeeIdentifier = "EMP002",
                Rating = MoodRating.FeelingGreat,
                Comment = "Good day."
            };

            using var cancellationSource = new CancellationTokenSource();
            var token = cancellationSource.Token;

            service
                .Setup(s => s.CreateAsync(request, token))
                .ReturnsAsync(new CreateMoodEntryResult(
                    CreateMoodEntryStatus.Success,
                    EntryId: 42));

            var controller = new MoodsController(service.Object);

            // Act
            await controller.Create(request, token);

            // Assert
            service.Verify(
                s => s.CreateAsync(request, token),
                Times.Once);
        }
    }
}