export interface RegisterDto {
  email: string;
  password: string;
  firstName: string;
  lastName: string;
}

export interface LoginDto {
  email: string;
  password: string;
}

export interface RefreshTokenRequestDto {
  refreshToken: string;
}

// Response body isn't documented in the OpenAPI spec (200 with no schema);
// field names assumed from typical ASP.NET JWT auth responses.
export interface AuthResponse {
  accessToken: string;
  refreshToken: string;
  expiresAtUtc?: string;
  email?: string;
  firstName?: string;
  lastName?: string;
}

export interface CurrentUser {
  email: string;
  firstName?: string;
  lastName?: string;
}
