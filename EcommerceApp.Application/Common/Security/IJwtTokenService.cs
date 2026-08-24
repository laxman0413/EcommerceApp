using EcommerceApp.Domain.Entities;

namespace EcommerceApp.Application.Common.Security;

public interface IJwtTokenService
{
    // Short-lived JWT carrying the user's identity + role claims.
    string GenerateAccessToken(User user);
    DateTime GetAccessTokenExpiry();

    // Opaque, high-entropy random string handed to the client; never stored as-is.
    string GenerateRefreshToken();
    DateTime GetRefreshTokenExpiry();

    // One-way hash used to look up / store refresh tokens server-side.
    string HashToken(string rawToken);
}
