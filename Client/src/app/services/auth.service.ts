/*
 * ROBIGOO FIELD SPRAYER CONTROL STATION
 * Copyright (c) 2025 Wojciech Salamon <wojciech.salamon@yahoo.com>
 * All rights reserved. Unauthorized distribution or disclosure is prohibited.
*/

import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { BehaviorSubject, Observable } from 'rxjs';
import { tap } from 'rxjs/operators';

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

  constructor(private http: HttpClient) { }

  private getCurrentUserFromStorage(): CurrentUser | null {
    const stored = localStorage.getItem('currentUser');
    return stored ? JSON.parse(stored) : null;
  }

  login(login: string, password: string): Observable<LoginResponse> {
    return this.http.post<LoginResponse>(`${this.apiUrl}/login`, { login, password })
      .pipe(
        tap(response => {
          const currentUser: CurrentUser = {
            ...response.user,
            token: response.token
          };
          localStorage.setItem('currentUser', JSON.stringify(currentUser));
          localStorage.setItem('token', response.token);
          this.currentUserSubject.next(currentUser);
        })
      );
  }

  logout(): void {
    localStorage.removeItem('currentUser');
    localStorage.removeItem('token');
    this.currentUserSubject.next(null);
  }

  getCurrentUser(): CurrentUser | null {
    return this.currentUserSubject.value;
  }

  getToken(): string | null {
    return localStorage.getItem('token');
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
