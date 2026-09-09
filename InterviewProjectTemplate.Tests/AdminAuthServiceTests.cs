using InterviewProjectTemplate.DTOs;
using InterviewProjectTemplate.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace InterviewProjectTemplate.Tests;

public class AdminAuthServiceTests
{
    private readonly Mock<UserManager<IdentityUser>> _users;
    private readonly Mock<SignInManager<IdentityUser>> _signIn;
    private readonly AdminAuthService _service;
    private readonly IdentityUser _admin = new() { UserName = "admin" };

    public AdminAuthServiceTests()
    {
        _users = new Mock<UserManager<IdentityUser>>(
            Mock.Of<IUserStore<IdentityUser>>(), Options.Create(new IdentityOptions()),
            Mock.Of<IPasswordHasher<IdentityUser>>(), Array.Empty<IUserValidator<IdentityUser>>(),
            Array.Empty<IPasswordValidator<IdentityUser>>(), Mock.Of<ILookupNormalizer>(),
            new IdentityErrorDescriber(), Mock.Of<IServiceProvider>(),
            Mock.Of<ILogger<UserManager<IdentityUser>>>());
        _signIn = new Mock<SignInManager<IdentityUser>>(
            _users.Object, Mock.Of<IHttpContextAccessor>(),
            Mock.Of<IUserClaimsPrincipalFactory<IdentityUser>>(),
            Options.Create(new IdentityOptions()), Mock.Of<ILogger<SignInManager<IdentityUser>>>(),
            Mock.Of<IAuthenticationSchemeProvider>(), Mock.Of<IUserConfirmation<IdentityUser>>());
        _users.Setup(u => u.FindByNameAsync("admin")).ReturnsAsync(_admin);
        _users.Setup(u => u.IsInRoleAsync(_admin, "Admin")).ReturnsAsync(true);
        _signIn.Setup(s => s.CheckPasswordSignInAsync(_admin, It.IsAny<string>(), true))
            .ReturnsAsync(SignInResult.Success);
        _signIn.Setup(s => s.SignInAsync(_admin, false, null)).Returns(Task.CompletedTask);
        _signIn.Setup(s => s.SignOutAsync()).Returns(Task.CompletedTask);
        _service = new AdminAuthService(_users.Object, _signIn.Object);
    }

    [Theory]
    [InlineData(null, "password")]
    [InlineData("", "password")]
    [InlineData("   ", "password")]
    [InlineData("admin", null)]
    [InlineData("admin", "")]
    public async Task Login_RejectsMissingCredentials(string? username, string? password)
    {
        // Arrange
        var request = new AdminLoginRequest { Username = username!, Password = password! };
        // Act
        var result = await _service.LoginAsync(request);
        // Assert
        Assert.Equal(AdminLoginStatus.InvalidCredentials, result.Status);
        _users.Verify(u => u.FindByNameAsync(It.IsAny<string>()), Times.Never);
        VerifyNoSignIn();
    }

    [Fact]
    public async Task Login_RejectsUnknownUsername()
    {
        // Arrange
        var request = new AdminLoginRequest { Username = "unknown", Password = "password" };
        // Act
        var result = await _service.LoginAsync(request);
        // Assert
        Assert.Equal(AdminLoginStatus.InvalidCredentials, result.Status);
        _signIn.Verify(s => s.CheckPasswordSignInAsync(It.IsAny<IdentityUser>(), It.IsAny<string>(), It.IsAny<bool>()), Times.Never);
        VerifyNoSignIn();
    }

    [Theory]
    [InlineData("failed", AdminLoginStatus.InvalidCredentials)]
    [InlineData("locked", AdminLoginStatus.LockedOut)]
    [InlineData("notAllowed", AdminLoginStatus.NotAllowed)]
    public async Task Login_HandlesIdentityRejections(string outcome, AdminLoginStatus expected)
    {
        // Arrange
        var identityResult = outcome switch
        {
            "locked" => SignInResult.LockedOut,
            "notAllowed" => SignInResult.NotAllowed,
            _ => SignInResult.Failed
        };
        _signIn.Setup(s => s.CheckPasswordSignInAsync(_admin, "password", true)).ReturnsAsync(identityResult);
        // Act
        var result = await _service.LoginAsync(new() { Username = "admin", Password = "password" });
        // Assert
        Assert.Equal(expected, result.Status);
        _signIn.Verify(s => s.CheckPasswordSignInAsync(_admin, "password", true), Times.Once);
        _users.Verify(u => u.IsInRoleAsync(It.IsAny<IdentityUser>(), It.IsAny<string>()), Times.Never);
        VerifyNoSignIn();
    }

    [Fact]
    public async Task Login_RejectsNonAdminWithoutIssuingCookie()
    {
        // Arrange
        _users.Setup(u => u.IsInRoleAsync(_admin, "Admin")).ReturnsAsync(false);
        // Act
        var result = await _service.LoginAsync(new() { Username = "admin", Password = "password" });
        // Assert
        Assert.Equal(AdminLoginStatus.NotAllowed, result.Status);
        VerifyNoSignIn();
    }

    [Fact]
    public async Task Login_DoesNotBypassTwoFactorAuthentication()
    {
        // Arrange
        _users.Setup(u => u.GetTwoFactorEnabledAsync(_admin)).ReturnsAsync(true);
        // Act
        var result = await _service.LoginAsync(new() { Username = "admin", Password = "password" });
        // Assert
        Assert.Equal(AdminLoginStatus.NotAllowed, result.Status);
        VerifyNoSignIn();
    }

    [Fact]
    public async Task Login_TrimsUsernamePreservesPasswordAndSignsInAdmin()
    {
        // Arrange
        var request = new AdminLoginRequest { Username = "  admin  ", Password = " password " };
        // Act
        var result = await _service.LoginAsync(request);
        // Assert
        Assert.Equal(AdminLoginStatus.Success, result.Status);
        _users.Verify(u => u.FindByNameAsync("admin"), Times.Once);
        _signIn.Verify(s => s.CheckPasswordSignInAsync(_admin, " password ", true), Times.Once);
        _users.Verify(u => u.IsInRoleAsync(_admin, "Admin"), Times.Once);
        _signIn.Verify(s => s.SignInAsync(_admin, false, null), Times.Once);
    }

    [Fact]
    public async Task Logout_SignsOut()
    {
        // Arrange
        // The fixture supplies the mocked Identity sign-in manager.
        // Act
        await _service.LogoutAsync();
        // Assert
        _signIn.Verify(s => s.SignOutAsync(), Times.Once);
    }

    private void VerifyNoSignIn() => _signIn.Verify(
        s => s.SignInAsync(It.IsAny<IdentityUser>(), It.IsAny<bool>(), It.IsAny<string>()), Times.Never);
}
