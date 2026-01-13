/*
 * ROBIGOO FIELD SPRAYER CONTROL STATION
 * Copyright (c) 2025 Wojciech Salamon <wojciech.salamon@yahoo.com>
 * All rights reserved. Unauthorized distribution or disclosure is prohibited.
*/

import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { BehaviorSubject, Observable, throwError, of } from 'rxjs';
import { tap, catchError, switchMap } from 'rxjs/operators';
import { TextService } from './text.service';

export interface LoginResponse {
  token: string;
  refreshToken: string;
  expiresIn: number;
  user: {
    userId: number;
    login: string;
    firstName: string;
    lastName: string;
    email: string;
    phone: string;
    permissionNumber: string;
    role: string;
    avatarBase64?: string;
  };
}

export interface RefreshResponse {
  token: string;
  refreshToken: string;
  expiresIn: number;
}

export interface CurrentUser {
  userId: number;
  login: string;
  firstName: string;
  lastName: string;
  email: string;
  phone: string;
  permissionNumber: string;
  role: string;
  avatarBase64?: string;
  token: string;
  refreshToken?: string;
  tokenExpiresAt?: number;
}

@Injectable({
  providedIn: 'root'
})
export class AuthService {
  private apiUrl = '/api/auth';
  private currentUserSubject = new BehaviorSubject<CurrentUser | null>(this.getCurrentUserFromStorage());
  public currentUser$ = this.currentUserSubject.asObservable();
  
  private refreshTokenTimeout: any;
  private isRefreshing = false;

  constructor(private http: HttpClient, private textService: TextService) {
    this.registerSessionTakeoverListener();
    this.startRefreshTokenTimer();
  }

  private getCurrentUserFromStorage(): CurrentUser | null {
    const stored = sessionStorage.getItem('currentUser');
    return stored ? JSON.parse(stored) : null;
  }

  login(login: string, password: string, options?: { force?: boolean }): Observable<LoginResponse> {
    const body: any = { login, password };
    if (options?.force) {
      body.force = true;
    }

    return this.http.post<LoginResponse>(`${this.apiUrl}/login`, body)
      .pipe(
        tap(response => {
          const expiresAt = Date.now() + (response.expiresIn * 1000);
          
          const currentUser: CurrentUser = {
            ...response.user,
            token: response.token,
            refreshToken: response.refreshToken,
            tokenExpiresAt: expiresAt
          };
          
          sessionStorage.setItem('currentUser', JSON.stringify(currentUser));
          sessionStorage.setItem('token', response.token);
          sessionStorage.setItem('refreshToken', response.refreshToken);
          sessionStorage.setItem('tokenExpiresAt', expiresAt.toString());
          
          this.currentUserSubject.next(currentUser);
          this.startRefreshTokenTimer();

          // Jeśli przejmujemy sesję, poinformuj pozostałe zakładki
          if (options?.force) {
            this.broadcastSessionTakeover(response.user.login);
          }
        })
      );
  }

  refreshToken(): Observable<RefreshResponse> {
    const token = sessionStorage.getItem('token');
    const refreshToken = sessionStorage.getItem('refreshToken');
    
    if (!token || !refreshToken) {
      return throwError(() => new Error('No tokens available'));
    }

    if (this.isRefreshing) {
      return of({ token, refreshToken, expiresIn: 0 } as RefreshResponse);
    }

    this.isRefreshing = true;

    return this.http.post<RefreshResponse>(`${this.apiUrl}/refresh`, {
      accessToken: token,
      refreshToken: refreshToken
    }).pipe(
      tap(response => {
        const expiresAt = Date.now() + (response.expiresIn * 1000);
        
        sessionStorage.setItem('token', response.token);
        sessionStorage.setItem('refreshToken', response.refreshToken);
        sessionStorage.setItem('tokenExpiresAt', expiresAt.toString());
        
        const currentUser = this.currentUserSubject.value;
        if (currentUser) {
          currentUser.token = response.token;
          currentUser.refreshToken = response.refreshToken;
          currentUser.tokenExpiresAt = expiresAt;
          sessionStorage.setItem('currentUser', JSON.stringify(currentUser));
          this.currentUserSubject.next(currentUser);
        }
        
        this.isRefreshing = false;
        this.startRefreshTokenTimer();
      }),
      catchError(error => {
        this.isRefreshing = false;
        this.logout();
        return throwError(() => error);
      })
    );
  }

  private startRefreshTokenTimer(): void {
    this.stopRefreshTokenTimer();
    
    const expiresAtStr = sessionStorage.getItem('tokenExpiresAt');
    if (!expiresAtStr) return;
    
    const expiresAt = parseInt(expiresAtStr, 10);
    const now = Date.now();
    
    // Refresh 1 minute before expiration
    const refreshTime = expiresAt - now - (60 * 1000);
    
    if (refreshTime > 0) {
      this.refreshTokenTimeout = setTimeout(() => {
        this.refreshToken().subscribe({
          error: () => {
            // Token refresh failed, user will be logged out on next API call
          }
        });
      }, refreshTime);
    }
  }

  private stopRefreshTokenTimer(): void {
    if (this.refreshTokenTimeout) {
      clearTimeout(this.refreshTokenTimeout);
      this.refreshTokenTimeout = null;
    }
  }

  logout(): void {
    this.stopRefreshTokenTimer();
    sessionStorage.removeItem('currentUser');
    sessionStorage.removeItem('token');
    sessionStorage.removeItem('refreshToken');
    sessionStorage.removeItem('tokenExpiresAt');
    this.currentUserSubject.next(null);
  }

  getCurrentUser(): CurrentUser | null {
    return this.currentUserSubject.value;
  }

  getToken(): string | null {
    return sessionStorage.getItem('token');
  }

  getRefreshToken(): string | null {
    return sessionStorage.getItem('refreshToken');
  }

  isTokenExpired(): boolean {
    const expiresAtStr = sessionStorage.getItem('tokenExpiresAt');
    if (!expiresAtStr) return true;
    
    const expiresAt = parseInt(expiresAtStr, 10);
    // Consider expired if less than 30 seconds remaining
    return Date.now() >= (expiresAt - 30000);
  }

  private registerSessionTakeoverListener(): void {
    if (typeof window === 'undefined' || typeof window.addEventListener === 'undefined') {
      return;
    }

    window.addEventListener('storage', (event: StorageEvent) => {
      if (event.key !== 'robigoo_session_takeover' || !event.newValue) {
        return;
      }

      try {
        const payload = JSON.parse(event.newValue) as { login?: string };
        const current = this.getCurrentUser();
        if (!current) {
          return;
        }

        // Jeśli komunikat dotyczy innego użytkownika, ignorujemy
        if (payload.login && payload.login !== current.login) {
          return;
        }

        // Sesja została przejęta w innej zakładce – wyloguj się z komunikatem
        const message = this.textService.get('login.session.terminated');
        try {
          sessionStorage.setItem('logoutMessage', message);
        } catch {
          // ignoruj błąd zapisu
        }

        this.logout();
        window.location.href = '/login';
      } catch {
        // niepoprawny JSON – ignoruj
      }
    });
  }

  private broadcastSessionTakeover(login: string): void {
    try {
      const payload = {
        login,
        timestamp: Date.now()
      };
      localStorage.setItem('robigoo_session_takeover', JSON.stringify(payload));
    } catch {
      // brak localStorage lub błąd zapisu – ignorujemy, zostaje fallback na 401
    }
  }

  isAuthenticated(): boolean {
    return !!this.getToken() && !this.isTokenExpired();
  }

  isAdmin(): boolean {
    return this.currentUserSubject.value?.role === 'admin';
  }

  me(): Observable<any> {
    return this.http.get(`${this.apiUrl}/me`);
  }
}
