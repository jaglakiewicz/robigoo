/*
 * ROBIGOO FIELD SPRAYER CONTROL STATION
 * Copyright (c) 2025 Wojciech Salamon <wojciech.salamon@yahoo.com>
 * All rights reserved. Unauthorized distribution or disclosure is prohibited.
*/

import { Injectable } from '@angular/core';
import { HttpInterceptor, HttpRequest, HttpHandler, HttpEvent } from '@angular/common/http';
import { Observable, throwError } from 'rxjs';
import { catchError } from 'rxjs/operators';
import { AuthService } from './auth.service';
import { TextService } from './text.service';

@Injectable()
export class AuthInterceptor implements HttpInterceptor {
  constructor(
    private authService: AuthService,
    private textService: TextService
  ) {}

  intercept(req: HttpRequest<any>, next: HttpHandler): Observable<HttpEvent<any>> {
    const token = sessionStorage.getItem('token');
    
    if (token) {
      req = req.clone({
        setHeaders: {
          Authorization: `Bearer ${token}`
        }
      });
    }

    return next.handle(req).pipe(
      catchError(error => {
        // Handle 401 Unauthorized - logout user (except direct login attempts)
        if (error.status === 401 && !req.url.endsWith('/api/auth/login')) {
          console.warn('[Auth Interceptor] 401 Unauthorized - logging out');

          // Pokaż przy następnym ekranie logowania informację, że sesja wygasła / została przejęta
          try {
            const message = this.textService.get('login.session.terminated');
            sessionStorage.setItem('logoutMessage', message);
          } catch {
            // ignore storage or translation errors
          }

          this.authService.logout();
          window.location.href = '/login';
        }

        return throwError(() => error);
      })
    );
  }
}