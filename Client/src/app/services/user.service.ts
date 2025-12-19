/*
 * ROBIGOO FIELD SPRAYER CONTROL STATION
 * Copyright (c) 2025 Wojciech Salamon <wojciech.salamon@yahoo.com>
 * All rights reserved. Unauthorized distribution or disclosure is prohibited.
*/

import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface UserDTO {
  userId: number;
  login: string;
  firstName: string;
  lastName: string;
  email: string;
  phone: string;
  permissionNumber: string;
  role: string;
  sessionTimeoutMinutes?: number | null;
}

export interface CreateUserRequest {
  login: string;
  password: string;
  firstName: string;
  lastName: string;
  email: string;
  phone: string;
  permissionNumber: string;
  role: string;
}

export interface UpdateProfileRequest {
  firstName: string;
  lastName: string;
  email: string;
  phone: string;
  permissionNumber: string;
  language: string;
  theme: string;
}

export interface ChangePasswordRequest {
  oldPassword: string;
  newPassword: string;
}

export interface UserProfileResponse {
  userId: number;
  login: string;
  firstName: string;
  lastName: string;
  email: string;
  phone: string;
  permissionNumber: string;
  language: string;
  theme: string;
  avatarBase64?: string;
}

export interface CanDeleteUserResponse {
  canDelete: boolean;
  reason: string;
}

@Injectable({
  providedIn: 'root'
})
export class UserService {
  private apiUrl = '/api/users';

  constructor(private http: HttpClient) { }

  /**
   * Get all users (admin only)
   */
  getUsers(): Observable<UserDTO[]> {
    return this.http.get<UserDTO[]>(this.apiUrl);
  }

  /**
   * Create new user (admin only)
   */
  createUser(request: CreateUserRequest): Observable<UserDTO> {
    return this.http.post<UserDTO>(this.apiUrl, request);
  }

  /**
   * Get current user's profile
   */
  getProfile(): Observable<UserProfileResponse> {
    return this.http.get<UserProfileResponse>(`${this.apiUrl}/profile`);
  }

  /**
   * Update current user's profile (with server-side validation)
   */
  updateProfile(request: UpdateProfileRequest): Observable<UserProfileResponse> {
    return this.http.put<UserProfileResponse>(`${this.apiUrl}/profile`, request);
  }

  /**
   * Change password with server-side validation
   */
  changePassword(request: ChangePasswordRequest): Observable<any> {
    return this.http.post(`${this.apiUrl}/change-password`, request);
  }

  /**
   * Upload avatar with server-side validation (max 5MB, image formats only)
   */
  uploadAvatar(file: File): Observable<any> {
    const formData = new FormData();
    formData.append('file', file);
    return this.http.post(`${this.apiUrl}/avatar`, formData);
  }

  /**
   * Check if current user can delete another user
   */
  canDeleteUser(userId: number): Observable<CanDeleteUserResponse> {
    return this.http.get<CanDeleteUserResponse>(`${this.apiUrl}/${userId}/can-delete`);
  }

  /**
   * Delete user with password confirmation
   */
  deleteUser(userId: number, password: string): Observable<any> {
    return this.http.delete(`${this.apiUrl}/${userId}`, { 
      body: { password: password }
    });
  }

  /**
   * Update user role (admin only)
   */
  updateUserRole(userId: number, role: string): Observable<UserProfileResponse> {
    return this.http.put<UserProfileResponse>(`${this.apiUrl}/${userId}/role`, { role: role });
  }
}

