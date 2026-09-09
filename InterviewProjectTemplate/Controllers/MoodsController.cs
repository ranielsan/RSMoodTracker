using InterviewProjectTemplate.DTOs;
using InterviewProjectTemplate.Services;
using Microsoft.AspNetCore.Mvc;

namespace InterviewProjectTemplate.Controllers
{
    [ApiController]
    [Route("api/moods")]
    public class MoodsController : ControllerBase
    {
        private readonly IMoodService _moodService;

        public MoodsController(IMoodService moodService)
        {
            _moodService = moodService;
        }
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateMoodEntryRequest request, CancellationToken cancellationToken)
        {
            var result = await _moodService.CreateAsync(
                request,
                cancellationToken);

            return result.Status switch
            {
                CreateMoodEntryStatus.Success =>
                    StatusCode(StatusCodes.Status201Created, new
                    {
                        entryId = result.EntryId,
                        message = "Your mood has been recorded."
                    }),

                CreateMoodEntryStatus.InvalidInput =>
                    Problem(
                        statusCode: StatusCodes.Status400BadRequest,
                        title: "Invalid submission",
                        detail: "Check your employee ID, rating, and comment."),

                CreateMoodEntryStatus.EmployeeNotFound =>
                    Problem(
                        statusCode: StatusCodes.Status400BadRequest,
                        title: "Invalid employee ID",
                        detail: "Enter a valid employee ID."),

                CreateMoodEntryStatus.EmployeeInactive =>
                    Problem(
                        statusCode: StatusCodes.Status403Forbidden,
                        title: "Employee inactive",
                        detail: "This employee is not active."),

                CreateMoodEntryStatus.AlreadySubmitted =>
                    Problem(
                        statusCode: StatusCodes.Status409Conflict,
                        title: "Mood already submitted",
                        detail: "You have already recorded your mood today."),

                _ => throw new InvalidOperationException(
                    "Unexpected mood submission status.")
            };
        }
    }
}
