/*
 * ROBIGOO FIELD SPRAYER CONTROL STATION
 * Copyright (c) 2025 Wojciech Salamon <wojciech.salamon@yahoo.com>
 * All rights reserved. Unauthorized distribution or disclosure is prohibited.
*/

import { Component, ElementRef, EventEmitter, HostListener, OnDestroy, Output, ViewChild } from '@angular/core';
import { Subscription } from 'rxjs';
import { TranslationService } from '../i18n/translation.service';
import { AuthService } from '../services/auth.service';
import { LANGUAGES, LanguageCode } from '../i18n/translations';

@Component({
  selector: 'app-login',
  templateUrl: './login.component.html',
  styleUrls: ['./login.component.css']
})
export class LoginComponent implements OnDestroy {
  @Output() loginSuccess = new EventEmitter<void>();
  @ViewChild('languageDropdown') languageDropdownRef?: ElementRef<HTMLDivElement>;

  username = '';
  password = '';
  errorKey = '';
  logoutMessage = '';
  loading = false;
  languages = LANGUAGES;
  currentLanguage: LanguageCode;
  languageDropdownOpen = false;
  private langSub: Subscription;

  constructor(
    private translation: TranslationService,
    private authService: AuthService
  ) {
    this.currentLanguage = this.translation.currentLanguage;
    this.langSub = this.translation.language$.subscribe(lang => {
      this.currentLanguage = lang;
    });
    
    // Check if there's a logout message from session timeout
    this.logoutMessage = sessionStorage.getItem('logoutMessage') || '';
    if (this.logoutMessage) {
      sessionStorage.removeItem('logoutMessage');
      setTimeout(() => { this.logoutMessage = ''; }, 5000);
    }
  }

  ngOnDestroy(): void {
    this.langSub?.unsubscribe();
  }

  onLogin() {
    this.errorKey = '';
    this.loading = true;

    this.authService.login(this.username, this.password).subscribe(
      (response) => {
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

  changeLanguage(language: LanguageCode) {
    this.translation.setLanguage(language);
  }

  toggleLanguageDropdown() {
    this.languageDropdownOpen = !this.languageDropdownOpen;
  }

  selectLanguage(language: LanguageCode) {
    this.changeLanguage(language);
    this.languageDropdownOpen = false;
  }

  getLanguageName(language: LanguageCode): string {
    return this.languages.find(lang => lang.code === language)?.nativeName ?? language.toUpperCase();
  }

  @HostListener('document:click', ['$event'])
  onDocumentClick(event: MouseEvent) {
    if (!this.languageDropdownOpen) {
      return;
    }
    const target = event.target as Node | null;
    if (target && this.languageDropdownRef?.nativeElement.contains(target)) {
      return;
    }
    this.languageDropdownOpen = false;
  }
}
