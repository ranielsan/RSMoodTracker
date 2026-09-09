using System.Security.Claims;
using System.Text.Json;
using InterviewProjectTemplate.Controllers;
using InterviewProjectTemplate.DTOs;
using InterviewProjectTemplate.Services;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace InterviewProjectTemplate.Tests;

// Direct action tests do not execute authorization or antiforgery filters.
public class AdminAuthControllerTests
{
    private readonly Mock<IAdminAuthService> _service = new();
    private readonly Mock<IAntiforgery> _antiforgery = new();
    private AdminAuthController CreateController() => new(_service.Object, _antiforgery.Object)
    {
        ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
    };

    [Theory]
    [InlineData(AdminLoginStatus.Success, 200, "Signed in successfully.")]
    [InlineData(AdminLoginStatus.InvalidCredentials, 401, "Invalid username or password.")]
    [InlineData(AdminLoginStatus.LockedOut, 401, "Unable to sign in. Please try again later.")]
    [InlineData(AdminLoginStatus.NotAllowed, 403, "This account cannot access the admin area.")]
    public async Task Login_MapsEveryOutcome(AdminLoginStatus status, int code, string message)
    {
        // Arrange
        var request = new AdminLoginRequest { Username = "admin", Password = "test-password" };
        _service.Setup(s => s.LoginAsync(request)).ReturnsAsync(new AdminLoginResult(status));
        var controller = CreateController();
        // Act
        var response = await controller.Login(request);
        // Assert
        var result = Assert.IsAssignableFrom<ObjectResult>(response);
        Assert.Equal(code, result.StatusCode);
        if (status == AdminLoginStatus.Success)
            Assert.Equal(message, JsonSerializer.SerializeToElement(result.Value).GetProperty("message").GetString());
        else
        {
            var problem = Assert.IsType<ProblemDetails>(result.Value);
            Assert.Equal(code, problem.Status);
            Assert.Equal(message, problem.Detail);
        }
        _service.Verify(s => s.LoginAsync(request), Times.Once);
    }

    [Fact]
    public async Task Login_RejectsUnexpectedServiceOutcome()
    {
        // Arrange
        _service.Setup(s => s.LoginAsync(It.IsAny<AdminLoginRequest>()))
            .ReturnsAsync(new AdminLoginResult((AdminLoginStatus)999));
        var controller = CreateController();
        // Act
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => controller.Login(new()));
        // Assert
        Assert.Equal("Unexpected admin login status.", exception.Message);
    }

    [Fact]
    public async Task Logout_CallsServiceAndReturnsNoContent()
    {
        // Arrange
        _service.Setup(s => s.LogoutAsync()).Returns(Task.CompletedTask);
        var controller = CreateController();
        // Act
        var result = await controller.Logout();
        // Assert
        Assert.IsType<NoContentResult>(result);
        _service.Verify(s => s.LogoutAsync(), Times.Once);
    }

    [Fact]
    public void Session_ReturnsCurrentUsername()
    {
        // Arrange
        var controller = CreateController();
        controller.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity(
            new[] { new Claim(ClaimTypes.Name, "admin") }, "test"));
        // Act
        var response = controller.Session();
        // Assert
        var result = Assert.IsType<OkObjectResult>(response);
        Assert.Equal("admin", JsonSerializer.SerializeToElement(result.Value).GetProperty("username").GetString());
    }

    [Fact]
    public void GetCsrfToken_ReturnsRequestTokenFromAntiforgeryService()
    {
        // Arrange
        var controller = CreateController();
        _antiforgery.Setup(a => a.GetAndStoreTokens(controller.HttpContext))
            .Returns(new AntiforgeryTokenSet("request-token", "cookie-token", "field", "X-CSRF-TOKEN"));
        // Act
        var response = controller.GetCsrfToken();
        // Assert
        var result = Assert.IsType<OkObjectResult>(response);
        var body = JsonSerializer.SerializeToElement(result.Value);
        Assert.Equal("request-token", body.GetProperty("token").GetString());
        Assert.False(body.TryGetProperty("cookieToken", out _));
        _antiforgery.Verify(a => a.GetAndStoreTokens(controller.HttpContext), Times.Once);
    }
}
