/*
 * ROBIGOO FIELD SPRAYER CONTROL STATION
 * Copyright (c) 2025 Wojciech Salamon <wojciech.salamon@yahoo.com>
 * All rights reserved. Unauthorized distribution or disclosure is prohibited.
*/

import { Injectable } from '@angular/core';
import { HttpInterceptor, HttpRequest, HttpHandler, HttpEvent, HttpErrorResponse } from '@angular/common/http';
import { Observable, throwError, BehaviorSubject } from 'rxjs';
import { catchError, filter, take, switchMap } from 'rxjs/operators';
import { AuthService } from './auth.service';
import { TextService } from './text.service';

@Injectable()
export class AuthInterceptor implements HttpInterceptor {
  private isRefreshing = false;
  private refreshTokenSubject: BehaviorSubject<string | null> = new BehaviorSubject<string | null>(null);

  constructor(
    private authService: AuthService,
    private textService: TextService
  ) {}

  intercept(req: HttpRequest<any>, next: HttpHandler): Observable<HttpEvent<any>> {
    // Don't add token to refresh endpoint to avoid circular dependency
    if (req.url.includes('/api/auth/refresh')) {
      return next.handle(req);
    }

    const token = this.authService.getToken();
    
    if (token) {
      req = this.addTokenToRequest(req, token);
    }

    return next.handle(req).pipe(
      catchError(error => {
        if (error instanceof HttpErrorResponse) {
          // Handle 401 Unauthorized
          if (error.status === 401 && !req.url.endsWith('/api/auth/login')) {
            return this.handle401Error(req, next);
          }
          
          // Handle 429 Too Many Requests
          if (error.status === 429) {
            console.warn('[Auth Interceptor] Rate limited - too many requests');
            return throwError(() => ({
              ...error,
              error: { message: 'Zbyt wiele żądań. Spróbuj ponownie za chwilę.' }
            }));
          }
        }

        return throwError(() => error);
      })
    );
  }

  private addTokenToRequest(request: HttpRequest<any>, token: string): HttpRequest<any> {
    return request.clone({
      setHeaders: {
        Authorization: `Bearer ${token}`
      }
    });
  }

  private handle401Error(request: HttpRequest<any>, next: HttpHandler): Observable<HttpEvent<any>> {
    if (!this.isRefreshing) {
      this.isRefreshing = true;
      this.refreshTokenSubject.next(null);

      const refreshToken = this.authService.getRefreshToken();
      
      if (refreshToken) {
        return this.authService.refreshToken().pipe(
          switchMap(response => {
            this.isRefreshing = false;
            this.refreshTokenSubject.next(response.token);
            return next.handle(this.addTokenToRequest(request, response.token));
          }),
          catchError(error => {
            this.isRefreshing = false;
            this.handleLogout();
            return throwError(() => error);
          })
        );
      } else {
        this.isRefreshing = false;
        this.handleLogout();
        return throwError(() => new Error('No refresh token available'));
      }
    }

    // Wait for the refresh to complete
    return this.refreshTokenSubject.pipe(
      filter(token => token !== null),
      take(1),
      switchMap(token => next.handle(this.addTokenToRequest(request, token!)))
    );
  }

  private handleLogout(): void {
    console.warn('[Auth Interceptor] Session expired - logging out');

    try {
      const message = this.textService.get('login.session.terminated');
      sessionStorage.setItem('logoutMessage', message);
    } catch {
      // ignore storage or translation errors
    }

    this.authService.logout();
    window.location.href = '/login';
  }
}
