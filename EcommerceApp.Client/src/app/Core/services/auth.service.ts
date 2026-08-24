import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { BehaviorSubject, Observable, of } from 'rxjs';
import { catchError, map, tap } from 'rxjs/operators';
import { environment } from '../../../environments/environment';
import {
  AuthResponse,
  CurrentUser,
  LoginDto,
  RefreshTokenRequestDto,
  RegisterDto,
} from '../models/auth.model';

const ACCESS_TOKEN_KEY = 'ecommerce.accessToken';
const REFRESH_TOKEN_KEY = 'ecommerce.refreshToken';
const CURRENT_USER_KEY = 'ecommerce.currentUser';

@Injectable({
  providedIn: 'root',
})
export class AuthService {
  private readonly apiUrl = `${environment.apiUrl}/Auth`;

  private readonly currentUserSubject = new BehaviorSubject<CurrentUser | null>(
    this.readStoredUser()
  );
  readonly currentUser$ = this.currentUserSubject.asObservable();
  readonly isAuthenticated$: Observable<boolean> = this.currentUser$.pipe(map((user) => !!user));

  constructor(private readonly http: HttpClient) {}

  register(dto: RegisterDto): Observable<void> {
    return this.http.post<void>(`${this.apiUrl}/register`, dto);
  }

  login(dto: LoginDto): Observable<AuthResponse> {
    return this.http
      .post<AuthResponse>(`${this.apiUrl}/login`, dto)
      .pipe(tap((response) => this.setSession(response)));
  }

  refreshToken(): Observable<AuthResponse> {
    const refreshToken = this.getRefreshToken();
    if (!refreshToken) {
      throw new Error('No refresh token available');
    }
    const request: RefreshTokenRequestDto = { refreshToken };
    return this.http
      .post<AuthResponse>(`${this.apiUrl}/refresh`, request)
      .pipe(tap((response) => this.setSession(response)));
  }

  logout(): Observable<void> {
    const refreshToken = this.getRefreshToken();
    if (!refreshToken) {
      this.clearSession();
      return of(void 0);
    }
    const request: RefreshTokenRequestDto = { refreshToken };
    return this.http.post<void>(`${this.apiUrl}/revoke`, request).pipe(
      catchError(() => of(void 0)),
      tap(() => this.clearSession())
    );
  }

  isAuthenticated(): boolean {
    return !!this.currentUserSubject.value && !!this.getAccessToken();
  }

  getAccessToken(): string | null {
    return localStorage.getItem(ACCESS_TOKEN_KEY);
  }

  getRefreshToken(): string | null {
    return localStorage.getItem(REFRESH_TOKEN_KEY);
  }

  private setSession(response: AuthResponse): void {
    localStorage.setItem(ACCESS_TOKEN_KEY, response.accessToken);
    localStorage.setItem(REFRESH_TOKEN_KEY, response.refreshToken);
    const user: CurrentUser = {
      email: response.email ?? '',
      firstName: response.firstName,
      lastName: response.lastName,
    };
    localStorage.setItem(CURRENT_USER_KEY, JSON.stringify(user));
    this.currentUserSubject.next(user);
  }

  private clearSession(): void {
    localStorage.removeItem(ACCESS_TOKEN_KEY);
    localStorage.removeItem(REFRESH_TOKEN_KEY);
    localStorage.removeItem(CURRENT_USER_KEY);
    this.currentUserSubject.next(null);
  }

  private readStoredUser(): CurrentUser | null {
    const raw = localStorage.getItem(CURRENT_USER_KEY);
    if (!raw) {
      return null;
    }
    try {
      return JSON.parse(raw) as CurrentUser;
    } catch {
      return null;
    }
  }
}
