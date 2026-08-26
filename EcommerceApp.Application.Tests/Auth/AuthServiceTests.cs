using AutoMapper;
using EcommerceApp.Application.Auth.DTOs;
using EcommerceApp.Application.Auth.Services;
using EcommerceApp.Application.Common.Exceptions;
using EcommerceApp.Application.Common.Security;
using EcommerceApp.Application.Tests.TestSupport;
using EcommerceApp.Domain.Entities;
using EcommerceApp.Domain.Enums;
using EcommerceApp.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;

namespace EcommerceApp.Application.Tests.Auth;

public class AuthServiceTests
{
    private readonly Mock<IUserRepository> _userRepository = new();
    private readonly Mock<IRefreshTokenRepository> _refreshTokenRepository = new();
    private readonly Mock<IPasswordHasher> _passwordHasher = new();
    private readonly Mock<IJwtTokenService> _jwtTokenService = new();
    private readonly IMapper _mapper = MapperFactory.Create();
    private readonly AuthService _sut;

    public AuthServiceTests()
    {
        _sut = new AuthService(
            _userRepository.Object,
            _refreshTokenRepository.Object,
            _passwordHasher.Object,
            _jwtTokenService.Object,
            _mapper,
            new Mock<ILogger<AuthService>>().Object);
    }

    [Fact]
    public async Task RegisterAsync_EmailAlreadyExists_ThrowsConflict()
    {
        _userRepository.Setup(r => r.EmailExistsAsync("existing@test.com")).ReturnsAsync(true);

        var dto = new RegisterDto { Email = "existing@test.com", Password = "pw", FirstName = "A", LastName = "B" };

        await Assert.ThrowsAsync<ConflictAppException>(() => _sut.RegisterAsync(dto));
        _userRepository.Verify(r => r.AddAsync(It.IsAny<User>()), Times.Never);
    }

    [Fact]
    public async Task RegisterAsync_NewEmail_NormalizesInputAndPersistsUser()
    {
        _userRepository.Setup(r => r.EmailExistsAsync("test@example.com")).ReturnsAsync(false);
        _passwordHasher.Setup(h => h.Hash("secret")).Returns("hashed-secret");

        var dto = new RegisterDto
        {
            Email = "  Test@Example.com  ",
            Password = "secret",
            FirstName = " John ",
            LastName = " Doe "
        };

        var result = await _sut.RegisterAsync(dto);

        _userRepository.Verify(r => r.AddAsync(It.Is<User>(u =>
            u.Email == "test@example.com" &&
            u.PasswordHash == "hashed-secret" &&
            u.FirstName == "John" &&
            u.LastName == "Doe" &&
            u.Role == Role.User &&
            u.IsActive)), Times.Once);

        Assert.Equal("test@example.com", result.Email);
        Assert.Equal("John", result.FirstName);
        Assert.Equal("Doe", result.LastName);
    }

    [Fact]
    public async Task LoginAsync_UserNotFound_ReturnsNull()
    {
        _userRepository.Setup(r => r.GetByEmailAsync(It.IsAny<string>())).ReturnsAsync((User?)null);

        var result = await _sut.LoginAsync(new LoginDto { Email = "nobody@test.com", Password = "pw" });

        Assert.Null(result);
    }

    [Fact]
    public async Task LoginAsync_InactiveUser_ReturnsNull()
    {
        var user = new User { Email = "a@b.com", PasswordHash = "hash", IsActive = false };
        _userRepository.Setup(r => r.GetByEmailAsync("a@b.com")).ReturnsAsync(user);
        _passwordHasher.Setup(h => h.Verify(It.IsAny<string>(), It.IsAny<string>())).Returns(true);

        var result = await _sut.LoginAsync(new LoginDto { Email = "a@b.com", Password = "pw" });

        Assert.Null(result);
    }

    [Fact]
    public async Task LoginAsync_WrongPassword_ReturnsNull()
    {
        var user = new User { Email = "a@b.com", PasswordHash = "hash", IsActive = true };
        _userRepository.Setup(r => r.GetByEmailAsync("a@b.com")).ReturnsAsync(user);
        _passwordHasher.Setup(h => h.Verify("wrong", "hash")).Returns(false);

        var result = await _sut.LoginAsync(new LoginDto { Email = "a@b.com", Password = "wrong" });

        Assert.Null(result);
        _jwtTokenService.Verify(j => j.GenerateAccessToken(It.IsAny<User>()), Times.Never);
    }

    [Fact]
    public async Task LoginAsync_ValidCredentials_IssuesTokensAndPersistsRefreshToken()
    {
        var user = new User { Email = "a@b.com", PasswordHash = "hash", IsActive = true, Role = Role.Admin };
        var accessExpiry = DateTime.UtcNow.AddMinutes(15);
        var refreshExpiry = DateTime.UtcNow.AddDays(7);

        _userRepository.Setup(r => r.GetByEmailAsync("a@b.com")).ReturnsAsync(user);
        _passwordHasher.Setup(h => h.Verify("pw", "hash")).Returns(true);
        _jwtTokenService.Setup(j => j.GenerateAccessToken(user)).Returns("access-token");
        _jwtTokenService.Setup(j => j.GetAccessTokenExpiry()).Returns(accessExpiry);
        _jwtTokenService.Setup(j => j.GenerateRefreshToken()).Returns("raw-refresh-token");
        _jwtTokenService.Setup(j => j.GetRefreshTokenExpiry()).Returns(refreshExpiry);
        _jwtTokenService.Setup(j => j.HashToken("raw-refresh-token")).Returns("hashed-refresh-token");

        var result = await _sut.LoginAsync(new LoginDto { Email = "a@b.com", Password = "pw" });

        Assert.NotNull(result);
        Assert.Equal("access-token", result!.AccessToken);
        Assert.Equal("raw-refresh-token", result.RefreshToken);
        Assert.Equal(accessExpiry, result.AccessTokenExpiresAt);
        Assert.Equal(refreshExpiry, result.RefreshTokenExpiresAt);
        Assert.Equal("Admin", result.User.Role);

        _refreshTokenRepository.Verify(r => r.AddAsync(It.Is<RefreshToken>(t =>
            t.UserId == user.Id &&
            t.TokenHash == "hashed-refresh-token" &&
            t.ExpiresAt == refreshExpiry)), Times.Once);
    }

    [Fact]
    public async Task RefreshAsync_TokenNotFound_ThrowsUnauthorized()
    {
        _jwtTokenService.Setup(j => j.HashToken("raw")).Returns("hash");
        _refreshTokenRepository.Setup(r => r.GetByTokenHashAsync("hash")).ReturnsAsync((RefreshToken?)null);

        await Assert.ThrowsAsync<UnauthorizedAppException>(() => _sut.RefreshAsync("raw"));
    }

    [Fact]
    public async Task RefreshAsync_ReusedInactiveToken_RevokesAllSessionsAndThrows()
    {
        var userId = Guid.NewGuid();
        var revokedToken = new RefreshToken { UserId = userId, RevokedAt = DateTime.UtcNow.AddMinutes(-1) };

        _jwtTokenService.Setup(j => j.HashToken("raw")).Returns("hash");
        _refreshTokenRepository.Setup(r => r.GetByTokenHashAsync("hash")).ReturnsAsync(revokedToken);

        await Assert.ThrowsAsync<UnauthorizedAppException>(() => _sut.RefreshAsync("raw"));

        _refreshTokenRepository.Verify(r => r.RevokeAllForUserAsync(userId), Times.Once);
    }

    [Fact]
    public async Task RefreshAsync_UserNoLongerActive_ThrowsUnauthorized()
    {
        var activeToken = new RefreshToken { UserId = Guid.NewGuid(), ExpiresAt = DateTime.UtcNow.AddDays(1) };
        _jwtTokenService.Setup(j => j.HashToken("raw")).Returns("hash");
        _refreshTokenRepository.Setup(r => r.GetByTokenHashAsync("hash")).ReturnsAsync(activeToken);
        _userRepository.Setup(r => r.GetByIdAsync(activeToken.UserId)).ReturnsAsync((User?)null);

        await Assert.ThrowsAsync<UnauthorizedAppException>(() => _sut.RefreshAsync("raw"));
    }

    [Fact]
    public async Task RefreshAsync_Valid_RotatesTokenAndReturnsNewTokens()
    {
        var user = new User { Email = "a@b.com", IsActive = true };
        var existingToken = new RefreshToken { Id = Guid.NewGuid(), UserId = user.Id, ExpiresAt = DateTime.UtcNow.AddDays(1) };

        // Deterministic per-input hashing so the "old" raw token and the newly generated raw
        // token map to distinguishable hashes, mirroring how HashToken behaves in production.
        _jwtTokenService.Setup(j => j.HashToken(It.IsAny<string>())).Returns((string raw) => $"hash-{raw}");
        _refreshTokenRepository.Setup(r => r.GetByTokenHashAsync("hash-old-raw")).ReturnsAsync(existingToken);
        _userRepository.Setup(r => r.GetByIdAsync(user.Id)).ReturnsAsync(user);
        _jwtTokenService.Setup(j => j.GenerateAccessToken(user)).Returns("new-access-token");
        _jwtTokenService.Setup(j => j.GenerateRefreshToken()).Returns("new-raw-refresh");
        _jwtTokenService.Setup(j => j.GetAccessTokenExpiry()).Returns(DateTime.UtcNow.AddMinutes(15));
        _jwtTokenService.Setup(j => j.GetRefreshTokenExpiry()).Returns(DateTime.UtcNow.AddDays(7));

        var result = await _sut.RefreshAsync("old-raw");

        Assert.Equal("new-access-token", result.AccessToken);
        Assert.Equal("new-raw-refresh", result.RefreshToken);

        _refreshTokenRepository.Verify(r => r.RevokeAsync(existingToken.Id, "hash-new-raw-refresh"), Times.Once);
    }

    [Fact]
    public async Task RevokeAsync_ActiveTokenFound_RevokesIt()
    {
        var token = new RefreshToken { Id = Guid.NewGuid(), ExpiresAt = DateTime.UtcNow.AddDays(1) };
        _jwtTokenService.Setup(j => j.HashToken("raw")).Returns("hash");
        _refreshTokenRepository.Setup(r => r.GetByTokenHashAsync("hash")).ReturnsAsync(token);

        await _sut.RevokeAsync("raw");

        _refreshTokenRepository.Verify(r => r.RevokeAsync(token.Id, null), Times.Once);
    }

    [Fact]
    public async Task RevokeAsync_TokenNotFound_DoesNothing()
    {
        _jwtTokenService.Setup(j => j.HashToken("raw")).Returns("hash");
        _refreshTokenRepository.Setup(r => r.GetByTokenHashAsync("hash")).ReturnsAsync((RefreshToken?)null);

        await _sut.RevokeAsync("raw");

        _refreshTokenRepository.Verify(r => r.RevokeAsync(It.IsAny<Guid>(), It.IsAny<string?>()), Times.Never);
    }
}
