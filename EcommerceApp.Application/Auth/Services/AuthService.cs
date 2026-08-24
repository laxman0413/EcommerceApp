using AutoMapper;
using EcommerceApp.Application.Auth.DTOs;
using EcommerceApp.Application.Common.Exceptions;
using EcommerceApp.Application.Common.Security;
using EcommerceApp.Domain.Entities;
using EcommerceApp.Domain.Enums;
using EcommerceApp.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace EcommerceApp.Application.Auth.Services;

public class AuthService(
    IUserRepository userRepository,
    IRefreshTokenRepository refreshTokenRepository,
    IPasswordHasher passwordHasher,
    IJwtTokenService jwtTokenService,
    IMapper mapper,
    ILogger<AuthService> logger) : IAuthService
{
    public async Task<UserDto> RegisterAsync(RegisterDto dto)
    {
        var normalizedEmail = dto.Email.Trim().ToLowerInvariant();

        if (await userRepository.EmailExistsAsync(normalizedEmail))
        {
            logger.LogWarning("Registration attempted for an email that already exists: {Email}", normalizedEmail);
            throw new ConflictAppException("An account with this email already exists");
        }

        // Every self-service registration is a plain User. Admin accounts are never created
        // from client-supplied input — see the seeding note in Scripts/schema.sql.
        var user = new User
        {
            Email = normalizedEmail,
            PasswordHash = passwordHasher.Hash(dto.Password),
            FirstName = dto.FirstName.Trim(),
            LastName = dto.LastName.Trim(),
            Role = Role.User,
            IsActive = true
        };

        await userRepository.AddAsync(user);
        logger.LogInformation("New user registered: {UserId} ({Email})", user.Id, user.Email);

        return mapper.Map<UserDto>(user);
    }

    public async Task<AuthResponseDto?> LoginAsync(LoginDto dto)
    {
        var normalizedEmail = dto.Email.Trim().ToLowerInvariant();
        var user = await userRepository.GetByEmailAsync(normalizedEmail);

        if (user is null || !user.IsActive || !passwordHasher.Verify(dto.Password, user.PasswordHash))
        {
            logger.LogWarning("Failed login attempt for {Email}", normalizedEmail);
            return null;
        }

        logger.LogInformation("User {UserId} logged in", user.Id);
        return await IssueTokensAsync(user);
    }

    public async Task<AuthResponseDto> RefreshAsync(string rawRefreshToken)
    {
        var tokenHash = jwtTokenService.HashToken(rawRefreshToken);
        var existingToken = await refreshTokenRepository.GetByTokenHashAsync(tokenHash);

        if (existingToken is null)
            throw new UnauthorizedAppException("Invalid refresh token");

        if (!existingToken.IsActive)
        {
            // Reuse of an already-rotated/expired/revoked token is a strong signal of theft —
            // burn every other active token for this user rather than silently rejecting.
            logger.LogWarning(
                "Reuse of an inactive refresh token detected for user {UserId}; revoking all sessions",
                existingToken.UserId);
            await refreshTokenRepository.RevokeAllForUserAsync(existingToken.UserId);
            throw new UnauthorizedAppException("Refresh token is no longer valid");
        }

        var user = await userRepository.GetByIdAsync(existingToken.UserId);
        if (user is null || !user.IsActive)
            throw new UnauthorizedAppException("Account is no longer active");

        var response = await IssueTokensAsync(user);

        var newTokenHash = jwtTokenService.HashToken(response.RefreshToken);
        await refreshTokenRepository.RevokeAsync(existingToken.Id, newTokenHash);

        return response;
    }

    public async Task RevokeAsync(string rawRefreshToken)
    {
        var tokenHash = jwtTokenService.HashToken(rawRefreshToken);
        var existingToken = await refreshTokenRepository.GetByTokenHashAsync(tokenHash);

        if (existingToken is not null && existingToken.IsActive)
            await refreshTokenRepository.RevokeAsync(existingToken.Id, replacedByTokenHash: null);
    }

    private async Task<AuthResponseDto> IssueTokensAsync(User user)
    {
        var accessToken = jwtTokenService.GenerateAccessToken(user);
        var rawRefreshToken = jwtTokenService.GenerateRefreshToken();

        var refreshToken = new RefreshToken
        {
            UserId = user.Id,
            TokenHash = jwtTokenService.HashToken(rawRefreshToken),
            ExpiresAt = jwtTokenService.GetRefreshTokenExpiry()
        };
        await refreshTokenRepository.AddAsync(refreshToken);

        return new AuthResponseDto
        {
            AccessToken = accessToken,
            AccessTokenExpiresAt = jwtTokenService.GetAccessTokenExpiry(),
            RefreshToken = rawRefreshToken,
            RefreshTokenExpiresAt = refreshToken.ExpiresAt,
            User = mapper.Map<UserDto>(user)
        };
    }
}
