import {
  HttpErrorResponse,
  HttpEvent,
  HttpHandlerFn,
  HttpInterceptorFn,
  HttpRequest,
} from '@angular/common/http';
import { inject } from '@angular/core';
import { BehaviorSubject, Observable, throwError } from 'rxjs';
import { catchError, filter, switchMap, take } from 'rxjs/operators';
import { AuthService } from '../services/auth.service';

let isRefreshing = false;
const refreshedToken$ = new BehaviorSubject<string | null>(null);

const AUTH_ENDPOINTS = ['/Auth/login', '/Auth/register', '/Auth/refresh', '/Auth/revoke'];

function withToken(req: HttpRequest<unknown>, token: string): HttpRequest<unknown> {
  return req.clone({ setHeaders: { Authorization: `Bearer ${token}` } });
}

export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const authService = inject(AuthService);

  const isAuthEndpoint = AUTH_ENDPOINTS.some((path) => req.url.includes(path));
  const accessToken = authService.getAccessToken();
  const authReq = accessToken && !isAuthEndpoint ? withToken(req, accessToken) : req;

  return next(authReq).pipe(
    catchError((error: unknown) => {
      if (error instanceof HttpErrorResponse && error.status === 401 && !isAuthEndpoint) {
        return handleUnauthorized(req, next, authService);
      }
      return throwError(() => error);
    })
  );
};

function handleUnauthorized(
  req: HttpRequest<unknown>,
  next: HttpHandlerFn,
  authService: AuthService
): Observable<HttpEvent<unknown>> {
  const refreshToken = authService.getRefreshToken();
  if (!refreshToken) {
    return throwError(() => new HttpErrorResponse({ status: 401 }));
  }

  if (!isRefreshing) {
    isRefreshing = true;
    refreshedToken$.next(null);

    return authService.refreshToken().pipe(
      switchMap((response) => {
        isRefreshing = false;
        refreshedToken$.next(response.accessToken);
        return next(withToken(req, response.accessToken));
      }),
      catchError((error: unknown) => {
        isRefreshing = false;
        authService.logout().subscribe();
        return throwError(() => error);
      })
    );
  }

  return refreshedToken$.pipe(
    filter((token): token is string => token !== null),
    take(1),
    switchMap((token) => next(withToken(req, token)))
  );
}
