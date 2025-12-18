/*
 * ROBIGOO FIELD SPRAYER CONTROL STATION
 * Copyright (c) 2025 Wojciech Salamon <wojciech.salamon@yahoo.com>
 * All rights reserved. Unauthorized distribution or disclosure is prohibited.
*/

import { Injectable } from '@angular/core';
import { BehaviorSubject } from 'rxjs';
import { DEFAULT_LANGUAGE, LanguageCode, translations } from './translations';

@Injectable({ providedIn: 'root' })
export class TranslationService {
  private readonly storageKey = 'app-language';
  private readonly languageSubject = new BehaviorSubject<LanguageCode>(this.loadLanguage());

  readonly language$ = this.languageSubject.asObservable();

  get currentLanguage(): LanguageCode {
    return this.languageSubject.value;
  }

  setLanguage(language: LanguageCode) {
    if (language === this.languageSubject.value) {
      return;
    }
    this.languageSubject.next(language);
    try {
      localStorage.setItem(this.storageKey, language);
    } catch {
      // ignore storage errors (e.g., SSR)
    }
  }

  translate(key: string): string {
    if (!key) {
      return '';
    }
    const tree = translations[this.currentLanguage];
    const value = key.split('.').reduce<any>((acc, segment) => {
      if (acc && typeof acc === 'object' && segment in acc) {
        return (acc as Record<string, unknown>)[segment];
      }
      return undefined;
    }, tree);
    return typeof value === 'string' ? value : key;
  }

  private loadLanguage(): LanguageCode {
    if (typeof localStorage !== 'undefined') {
      const stored = localStorage.getItem(this.storageKey) as LanguageCode | null;
      if (stored === 'pl' || stored === 'uk') {
        return stored;
      }
    }
    return DEFAULT_LANGUAGE;
  }
}

