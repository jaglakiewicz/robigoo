/*
 * ROBIGOO FIELD SPRAYER CONTROL STATION
 * Copyright (c) 2025 Wojciech Salamon <wojciech.salamon@yahoo.com>
 * All rights reserved. Unauthorized distribution or disclosure is prohibited.
*/

import { Injectable } from '@angular/core';
import { BehaviorSubject } from 'rxjs';

@Injectable({ providedIn: 'root' })
export class DirtyFormService {
  private forms = new Map<string, boolean>();
  private anyDirtySubject = new BehaviorSubject<boolean>(false);

  anyDirty$ = this.anyDirtySubject.asObservable();

  registerForm(id: string): void {
    if (!this.forms.has(id)) {
      this.forms.set(id, false);
    }
    this.updateAnyDirty();
  }

  unregisterForm(id: string): void {
    this.forms.delete(id);
    this.updateAnyDirty();
  }

  setDirty(id: string, dirty: boolean): void {
    if (!this.forms.has(id)) {
      this.forms.set(id, dirty);
    } else {
      this.forms.set(id, dirty);
    }
    this.updateAnyDirty();
  }

  isDirty(id: string): boolean {
    return this.forms.get(id) === true;
  }

  clear(): void {
    this.forms.clear();
    this.updateAnyDirty();
  }

  private updateAnyDirty(): void {
    const anyDirty = Array.from(this.forms.values()).some(v => v);
    this.anyDirtySubject.next(anyDirty);
  }
}
