/*
 * ROBIGOO FIELD SPRAYER CONTROL STATION
 * Copyright (c) 2025 Wojciech Salamon <wojciech.salamon@yahoo.com>
 * All rights reserved. Unauthorized distribution or disclosure is prohibited.
*/

import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { BehaviorSubject, Observable } from 'rxjs';
import { tap } from 'rxjs/operators';
import { TextService } from './text.service';

export interface LoginResponse {
  token: string;
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
}

@Injectable({
  providedIn: 'root'
})
export class AuthService {
  private apiUrl = '/api/auth';
  private currentUserSubject = new BehaviorSubject<CurrentUser | null>(this.getCurrentUserFromStorage());
  public currentUser$ = this.currentUserSubject.asObservable();

  constructor(private http: HttpClient, private textService: TextService) {
    this.registerSessionTakeoverListener();
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
          const currentUser: CurrentUser = {
            ...response.user,
            token: response.token
          };
          sessionStorage.setItem('currentUser', JSON.stringify(currentUser));
          sessionStorage.setItem('token', response.token);
          this.currentUserSubject.next(currentUser);

          // Jeśli przejmujemy sesję, poinformuj pozostałe zakładki
          if (options?.force) {
            this.broadcastSessionTakeover(response.user.login);
          }
        })
      );
  }

  logout(): void {
    sessionStorage.removeItem('currentUser');
    sessionStorage.removeItem('token');
    this.currentUserSubject.next(null);
  }

  getCurrentUser(): CurrentUser | null {
    return this.currentUserSubject.value;
  }

  getToken(): string | null {
    return sessionStorage.getItem('token');
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
    return !!this.getToken();
  }

  isAdmin(): boolean {
    return this.currentUserSubject.value?.role === 'admin';
  }

  me(): Observable<any> {
    return this.http.get(`${this.apiUrl}/me`);
  }
}
