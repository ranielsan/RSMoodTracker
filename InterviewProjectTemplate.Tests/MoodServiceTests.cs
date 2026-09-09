using InterviewProjectTemplate.Data;
using InterviewProjectTemplate.DTOs;
using InterviewProjectTemplate.Models;
using InterviewProjectTemplate.Services;
using Microsoft.EntityFrameworkCore;

namespace InterviewProjectTemplate.Tests
{
    public class MoodServiceTests
    {
        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData(null)]
        public async Task Create_RejectsMissingEmployeeIdentifier(
        string? identifier)
        {
            // Arrange
            var request = ValidRequest();
            request.EmployeeIdentifier = identifier!;

            // Act and Assert (performed in the helper)
            await AssertInvalidAsync(request);
        }

        [Fact]
        public async Task Create_RejectsEmployeeIdentifierOver50Characters()
        {
            // Arrange
            var request = ValidRequest();
            request.EmployeeIdentifier = new string('A', 51);

            // Act and Assert (performed in the helper)
            await AssertInvalidAsync(request);
        }

        [Theory]
        [InlineData(null)]
        [InlineData(0)]
        [InlineData(5)]
        public async Task Create_RejectsMissingOrInvalidRating(int? rating)
        {
            // Arrange
            var request = ValidRequest();
            request.Rating = rating.HasValue
                ? (MoodRating)rating.Value
                : null;

            // Act and Assert (performed in the helper)
            await AssertInvalidAsync(request);
        }

        [Fact]
        public async Task Create_RejectsCommentOver1000Characters()
        {
            // Arrange
            var request = ValidRequest();
            request.Comment = new string('A', 1001);

            // Act and Assert (performed in the helper)
            await AssertInvalidAsync(request);
        }

        [Fact]
        public async Task Create_RejectsUnknownEmployee()
        {
            // Arrange
            await using var context = CreateContext();
            var service = new MoodService(context, new TestClock());

            // Act
            var result = await service.CreateAsync(ValidRequest());

            // Assert
            Assert.Equal(CreateMoodEntryStatus.EmployeeNotFound, result.Status);
            Assert.Null(result.EntryId);
            Assert.Empty(await context.MoodEntries.ToListAsync());
        }

        [Fact]
        public async Task Create_RejectsInactiveEmployee()
        {
            // Arrange
            await using var context = CreateContext();
            await AddEmployeeAsync(context, isActive: false);
            var service = new MoodService(context, new TestClock());

            // Act
            var result = await service.CreateAsync(ValidRequest());

            // Assert
            Assert.Equal(CreateMoodEntryStatus.EmployeeInactive, result.Status);
            Assert.Null(result.EntryId);
            Assert.Empty(await context.MoodEntries.ToListAsync());
        }

        [Theory]
        [InlineData(MoodRating.NotGoodAtAll)]
        [InlineData(MoodRating.ABitMeh)]
        [InlineData(MoodRating.PrettyGood)]
        [InlineData(MoodRating.FeelingGreat)]
        public async Task Create_SavesEachValidRating(MoodRating rating)
        {
            // Arrange
            await using var context = CreateContext();
            var employee = await AddEmployeeAsync(context);
            var clock = new TestClock();
            var service = new MoodService(context, clock);
            var request = ValidRequest();
            request.Rating = rating;
            request.Comment = "  Good morning.  ";

            // Act
            var result = await service.CreateAsync(request);

            // Assert
            Assert.Equal(CreateMoodEntryStatus.Success, result.Status);
            context.ChangeTracker.Clear();
            var saved = Assert.Single(await context.MoodEntries.ToListAsync());
            Assert.Equal(saved.Id, result.EntryId);
            Assert.Equal(employee.Id, saved.EmployeeId);
            Assert.Equal(rating, saved.Rating);
            Assert.Equal("Good morning.", saved.Comment);
            Assert.Equal(clock.UtcNow.UtcDateTime, saved.CreatedAtUtc);
            Assert.Equal(DateOnly.FromDateTime(clock.UtcNow.UtcDateTime), saved.SubmissionDate);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public async Task Create_SavesEmptyCommentAsNull(string? comment)
        {
            // Arrange
            await using var context = CreateContext();
            await AddEmployeeAsync(context);
            var request = ValidRequest();
            request.Comment = comment;
            var service = new MoodService(context, new TestClock());

            // Act
            var result = await service.CreateAsync(request);

            // Assert
            Assert.Equal(CreateMoodEntryStatus.Success, result.Status);
            Assert.Null((await context.MoodEntries.SingleAsync()).Comment);
        }

        [Fact]
        public async Task Create_AcceptsMaximumFieldLengths()
        {
            // Arrange
            await using var context = CreateContext();
            var identifier = new string('A', 50);
            await AddEmployeeAsync(context, identifier);
            var request = ValidRequest();
            request.EmployeeIdentifier = identifier;
            request.Comment = new string('B', 1000);
            var service = new MoodService(context, new TestClock());

            // Act
            var result = await service.CreateAsync(request);

            // Assert
            Assert.Equal(CreateMoodEntryStatus.Success, result.Status);
            Assert.Equal(request.Comment, (await context.MoodEntries.SingleAsync()).Comment);
        }

        [Fact]
        public async Task Create_NormalizesEmployeeIdentifierAndPreventsDuplicate()
        {
            // Arrange
            await using var context = CreateContext();
            await AddEmployeeAsync(context);
            var service = new MoodService(context, new TestClock());
            var request = ValidRequest();
            request.EmployeeIdentifier = "  emp001  ";

            // Act
            var first = await service.CreateAsync(request);
            var second = await service.CreateAsync(ValidRequest());

            // Assert
            Assert.Equal(CreateMoodEntryStatus.Success, first.Status);
            Assert.Equal(CreateMoodEntryStatus.AlreadySubmitted, second.Status);
            Assert.Null(second.EntryId);
            Assert.Single(await context.MoodEntries.ToListAsync());
        }

        [Fact]
        public async Task Create_RejectsAnotherSubmissionLaterOnSameUtcDay()
        {
            // Arrange
            await using var context = CreateContext();
            await AddEmployeeAsync(context);
            var clock = new TestClock();
            var service = new MoodService(context, clock);
            await service.CreateAsync(ValidRequest());
            clock.UtcNow = clock.UtcNow.AddHours(10);

            // Act
            var result = await service.CreateAsync(ValidRequest());

            // Assert
            Assert.Equal(CreateMoodEntryStatus.AlreadySubmitted, result.Status);
            Assert.Null(result.EntryId);
            Assert.Single(await context.MoodEntries.ToListAsync());
        }

        [Fact]
        public async Task Create_AllowsSubmissionImmediatelyAfterUtcMidnight()
        {
            // Arrange
            await using var context = CreateContext();
            await AddEmployeeAsync(context);
            var clock = new TestClock
            {
                UtcNow = new DateTimeOffset(2026, 9, 9, 23, 59, 59, TimeSpan.Zero)
            };
            var service = new MoodService(context, clock);
            var first = await service.CreateAsync(ValidRequest());
            clock.UtcNow = clock.UtcNow.AddSeconds(1);

            // Act
            var second = await service.CreateAsync(ValidRequest());

            // Assert
            Assert.Equal(CreateMoodEntryStatus.Success, first.Status);
            Assert.Equal(CreateMoodEntryStatus.Success, second.Status);
            Assert.NotEqual(first.EntryId, second.EntryId);
            var entries = await context.MoodEntries.OrderBy(e => e.CreatedAtUtc).ToListAsync();
            Assert.Equal(2, entries.Count);
            Assert.Equal(new DateOnly(2026, 9, 9), entries[0].SubmissionDate);
            Assert.Equal(new DateOnly(2026, 9, 10), entries[1].SubmissionDate);
        }

        [Fact]
        public async Task Create_AllowsDifferentEmployeesOnSameDay()
        {
            // Arrange
            await using var context = CreateContext();
            await AddEmployeeAsync(context);
            await AddEmployeeAsync(context, "EMP002");
            var service = new MoodService(context, new TestClock());
            var first = await service.CreateAsync(ValidRequest());
            var request = ValidRequest();
            request.EmployeeIdentifier = "EMP002";

            // Act
            var second = await service.CreateAsync(request);

            // Assert
            Assert.Equal(CreateMoodEntryStatus.Success, first.Status);
            Assert.Equal(CreateMoodEntryStatus.Success, second.Status);
            Assert.Equal(2, await context.MoodEntries.CountAsync());
        }

        // This provider tests service decisions, not MySQL constraints or concurrency.
        private static AppDbContext CreateContext()
        {
            return new AppDbContext(new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options);
        }

        private static async Task<Employee> AddEmployeeAsync(
            AppDbContext context,
            string identifier = "EMP001",
            bool isActive = true)
        {
            var employee = new Employee
            {
                EmployeeIdentifier = identifier,
                IsActive = isActive
            };
            context.Employees.Add(employee);
            await context.SaveChangesAsync();
            return employee;
        }

        private sealed class TestClock : TimeProvider
        {
            public DateTimeOffset UtcNow { get; set; } =
                new(2026, 9, 9, 8, 0, 0, TimeSpan.Zero);

            public override DateTimeOffset GetUtcNow() => UtcNow;
        }

        private static CreateMoodEntryRequest ValidRequest()
        {
            return new CreateMoodEntryRequest
            {
                EmployeeIdentifier = "EMP001",
                Rating = MoodRating.PrettyGood
            };
        }

        private static async Task AssertInvalidAsync(
            CreateMoodEntryRequest request)
        {
            // Arrange
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .Options;

            await using var context = new AppDbContext(options);

            var service = new MoodService(
                context,
                TimeProvider.System);

            // Act
            var result = await service.CreateAsync(request);

            // Assert
            Assert.Equal(CreateMoodEntryStatus.InvalidInput, result.Status);
            Assert.Null(result.EntryId);
        }
    }
}
