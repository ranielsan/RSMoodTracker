using InterviewProjectTemplate.DTOs;
using InterviewProjectTemplate.Services;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InterviewProjectTemplate.Controllers
{
    [ApiController]
    [Route("api/admin/auth")]
    public class AdminAuthController : ControllerBase
    {
        private readonly IAdminAuthService _authService;
        private readonly IAntiforgery _antiforgery;

        public AdminAuthController(
            IAdminAuthService authService,
            IAntiforgery antiforgery)
        {
            _authService = authService;
            _antiforgery = antiforgery;
        }

        [HttpGet("csrf")]
        [AllowAnonymous]
        [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
        public IActionResult GetCsrfToken()
        {
            var tokens = _antiforgery.GetAndStoreTokens(HttpContext);

            return Ok(new
            {
                token = tokens.RequestToken
            });
        }

        [HttpPost("login")]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(
        [FromBody] AdminLoginRequest request)
        {
            var result = await _authService.LoginAsync(request);

            return result.Status switch
            {
                AdminLoginStatus.Success =>
                    Ok(new { message = "Signed in successfully." }),

                AdminLoginStatus.InvalidCredentials =>
                    Problem(
                        statusCode: StatusCodes.Status401Unauthorized,
                        title: "Sign-in failed",
                        detail: "Invalid username or password."),

                AdminLoginStatus.LockedOut =>
                    Problem(
                        statusCode: StatusCodes.Status401Unauthorized,
                        title: "Sign-in failed",
                        detail: "Unable to sign in. Please try again later."),

                AdminLoginStatus.NotAllowed =>
                    Problem(
                        statusCode: StatusCodes.Status403Forbidden,
                        title: "Access denied",
                        detail: "This account cannot access the admin area."),

                _ => throw new InvalidOperationException(
                    "Unexpected admin login status.")
            };
        }

        [HttpPost("logout")]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _authService.LogoutAsync();

            return NoContent();
        }

        [HttpGet("session")]
        [Authorize(Roles = "Admin")]
        [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
        public IActionResult Session()
        {
            return Ok(new
            {
                username = User.Identity?.Name
            });
        }
    }
}
