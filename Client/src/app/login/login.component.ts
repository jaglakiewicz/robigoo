/*
 * ROBIGOO FIELD SPRAYER CONTROL STATION
 * Copyright (c) 2025 Wojciech Salamon <wojciech.salamon@yahoo.com>
 * All rights reserved. Unauthorized distribution or disclosure is prohibited.
*/

import { Component, EventEmitter, Output } from '@angular/core';
import { AuthService } from '../services/auth.service';

@Component({
  selector: 'app-login',
  templateUrl: './login.component.html',
  styleUrls: ['./login.component.css']
})
export class LoginComponent {
  @Output() loginSuccess = new EventEmitter<void>();

  username = '';
  password = '';
  errorKey = '';
  logoutMessage = '';
  loading = false;
  passwordVisible = false;

  constructor(private authService: AuthService) {
    // Check if there's a logout message from session timeout
    this.logoutMessage = sessionStorage.getItem('logoutMessage') || '';
    if (this.logoutMessage) {
      sessionStorage.removeItem('logoutMessage');
      setTimeout(() => { this.logoutMessage = ''; }, 5000);
    }
  }

  onLogin() {
    this.errorKey = '';
    this.loading = true;

    this.authService.login(this.username, this.password).subscribe(
      () => {
        this.loading = false;
        this.loginSuccess.emit();
      },
      (error) => {
        this.errorKey = 'login.errors.invalidCredentials';
        this.loading = false;
      }
    );
  }

  onKeyPress(event: KeyboardEvent) {
    if (event.key === 'Enter') {
      this.onLogin();
    }
  }

  togglePasswordVisibility() {
    this.passwordVisible = !this.passwordVisible;
  }
}
