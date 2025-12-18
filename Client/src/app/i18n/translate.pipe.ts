/*
 * ROBIGOO FIELD SPRAYER CONTROL STATION
 * Copyright (c) 2025 Wojciech Salamon <wojciech.salamon@yahoo.com>
 * All rights reserved. Unauthorized distribution or disclosure is prohibited.
*/

import { ChangeDetectorRef, OnDestroy, Pipe, PipeTransform } from '@angular/core';
import { Subscription } from 'rxjs';
import { TranslationService } from './translation.service';

@Pipe({
  name: 'translate',
  pure: false
})
export class TranslatePipe implements PipeTransform, OnDestroy {
  private currentKey?: string;
  private currentValue = '';
  private readonly sub: Subscription;

  constructor(private i18n: TranslationService, private cdr: ChangeDetectorRef) {
    this.sub = this.i18n.language$.subscribe(() => {
      if (this.currentKey) {
        this.currentValue = this.i18n.translate(this.currentKey);
        this.cdr.markForCheck();
      }
    });
  }

  transform(key: string | null | undefined): string {
    if (!key) {
      this.currentKey = undefined;
      this.currentValue = '';
      return '';
    }

    if (key !== this.currentKey) {
      this.currentKey = key;
      this.currentValue = this.i18n.translate(key);
    }

    return this.currentValue;
  }

  ngOnDestroy(): void {
    this.sub.unsubscribe();
  }
}

