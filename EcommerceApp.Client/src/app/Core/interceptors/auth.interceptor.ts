import {
  HttpErrorResponse,
  HttpEvent,
  HttpHandler,
  HttpInterceptor,
  HttpRequest,
} from '@angular/common/http';
import { Injectable } from '@angular/core';
import { BehaviorSubject, Observable, throwError } from 'rxjs';
import { catchError, filter, switchMap, take } from 'rxjs/operators';
import { AuthService } from '../services/auth.service';

const AUTH_ENDPOINTS = ['/Auth/login', '/Auth/register', '/Auth/refresh', '/Auth/revoke'];

@Injectable()
export class AuthInterceptor implements HttpInterceptor {
  private isRefreshing = false;
  private readonly refreshedToken$ = new BehaviorSubject<string | null>(null);

  constructor(private readonly authService: AuthService) {}

  intercept(req: HttpRequest<unknown>, next: HttpHandler): Observable<HttpEvent<unknown>> {
    const isAuthEndpoint = AUTH_ENDPOINTS.some((path) => req.url.includes(path));
    const accessToken = this.authService.getAccessToken();
    const authReq = accessToken && !isAuthEndpoint ? this.withToken(req, accessToken) : req;

    return next.handle(authReq).pipe(
      catchError((error: unknown) => {
        if (error instanceof HttpErrorResponse && error.status === 401 && !isAuthEndpoint) {
          return this.handleUnauthorized(req, next);
        }
        return throwError(() => error);
      })
    );
  }

  private withToken(req: HttpRequest<unknown>, token: string): HttpRequest<unknown> {
    return req.clone({ setHeaders: { Authorization: `Bearer ${token}` } });
  }

  private handleUnauthorized(req: HttpRequest<unknown>, next: HttpHandler): Observable<HttpEvent<unknown>> {
    const refreshToken = this.authService.getRefreshToken();
    if (!refreshToken) {
      return throwError(() => new HttpErrorResponse({ status: 401 }));
    }

    if (!this.isRefreshing) {
      this.isRefreshing = true;
      this.refreshedToken$.next(null);

      return this.authService.refreshToken().pipe(
        switchMap((response) => {
          this.isRefreshing = false;
          this.refreshedToken$.next(response.accessToken);
          return next.handle(this.withToken(req, response.accessToken));
        }),
        catchError((error: unknown) => {
          this.isRefreshing = false;
          this.authService.logout().subscribe();
          return throwError(() => error);
        })
      );
    }

    return this.refreshedToken$.pipe(
      filter((token): token is string => token !== null),
      take(1),
      switchMap((token) => next.handle(this.withToken(req, token)))
    );
  }
}
