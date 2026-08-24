using EcommerceApp.Application.Auth.DTOs;

namespace EcommerceApp.Application.Auth.Services;

public interface IAuthService
{
    Task<UserDto> RegisterAsync(RegisterDto dto);
    Task<AuthResponseDto?> LoginAsync(LoginDto dto);
    Task<AuthResponseDto> RefreshAsync(string rawRefreshToken);
    Task RevokeAsync(string rawRefreshToken);
}
