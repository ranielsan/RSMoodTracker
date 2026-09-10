using InterviewProjectTemplate.DTOs;
using InterviewProjectTemplate.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InterviewProjectTemplate.Controllers
{
    [ApiController]
    [Route("api/admin/moods")]
    [Authorize(Roles = "Admin")]
    public class AdminMoodsController : ControllerBase
    {
        private readonly IMoodService _moodService;

        public AdminMoodsController(IMoodService moodService)
        {
            _moodService = moodService;
        }

        [HttpGet]
        [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
        public async Task<ActionResult<IReadOnlyList<MoodEntryResponse>>> GetAll(
            CancellationToken cancellationToken)
        {
            var entries = await _moodService.GetAllAsync(cancellationToken);

            return Ok(entries);
        }

        [HttpGet("dashboard")]
        [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
        public async Task<ActionResult<MoodDashboardResponse>> GetDashboard(
            [FromQuery] MoodDashboardRequest request, CancellationToken cancellationToken)
        {
            return Ok(await _moodService.GetDashboardAsync(request, cancellationToken));
        }
    }
}
