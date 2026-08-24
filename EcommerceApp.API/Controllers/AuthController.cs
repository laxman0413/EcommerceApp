using EcommerceApp.Application.Auth.DTOs;
using EcommerceApp.Application.Auth.Services;
using EcommerceApp.Application.Common.Exceptions;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EcommerceApp.API.Controllers;

// Every endpoint here is [AllowAnonymous] except Revoke — you can't be authenticated yet
// when you're trying to register/login/refresh.
[ApiController]
[Route("api/[controller]")]
public class AuthController(
    IAuthService authService,
    IValidator<RegisterDto> registerValidator,
    IValidator<LoginDto> loginValidator,
    IValidator<RefreshTokenRequestDto> refreshValidator) : ControllerBase
{
    // Creates a plain "User" account. AuthService hard-codes Role.User regardless of what's
    // in the request body — admins are promoted by hand in SQL, never granted via the API.
    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<IActionResult> Register([FromBody] RegisterDto dto)
    {
        var validation = await registerValidator.ValidateAsync(dto);
        if (!validation.IsValid)
            return BadRequest(validation.Errors.Select(e => new { field = e.PropertyName, error = e.ErrorMessage }));

        try
        {
            var user = await authService.RegisterAsync(dto);
            return CreatedAtAction(nameof(Register), new { user.Id }, user);
        }
        catch (ConflictAppException ex)
        {
            // Caught locally (rather than left to ExceptionHandlingMiddleware) so the body
            // stays the simple { message } shape clients expect for "email already taken".
            return Conflict(new { message = ex.Message });
        }
    }

    // Returns null (not an exception) for bad credentials — a failed login is an expected
    // outcome, not an error condition, so it's modeled as data rather than a thrown exception.
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginDto dto)
    {
        var validation = await loginValidator.ValidateAsync(dto);
        if (!validation.IsValid)
            return BadRequest(validation.Errors.Select(e => new { field = e.PropertyName, error = e.ErrorMessage }));

        var result = await authService.LoginAsync(dto);
        return result is null
            ? Unauthorized(new { message = "Invalid email or password" })
            : Ok(result);
    }

    // Exchanges a still-valid refresh token for a new access/refresh pair (rotation).
    // Reuse of an already-rotated token is treated as theft server-side — see AuthService.
    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<IActionResult> Refresh([FromBody] RefreshTokenRequestDto dto)
    {
        var validation = await refreshValidator.ValidateAsync(dto);
        if (!validation.IsValid)
            return BadRequest(validation.Errors.Select(e => new { field = e.PropertyName, error = e.ErrorMessage }));

        var result = await authService.RefreshAsync(dto.RefreshToken);
        return Ok(result);
    }

    // Logs out by revoking one refresh token. Requires a valid access token, unlike the
    // other three endpoints — you can only revoke your own session.
    [HttpPost("revoke")]
    [Authorize]
    public async Task<IActionResult> Revoke([FromBody] RefreshTokenRequestDto dto)
    {
        await authService.RevokeAsync(dto.RefreshToken);
        return NoContent();
    }
}
