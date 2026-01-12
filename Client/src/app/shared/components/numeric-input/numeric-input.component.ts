/*
 * ROBIGOO FIELD SPRAYER CONTROL STATION
 * Copyright (c) 2025 Wojciech Salamon <wojciech.salamon@yahoo.com>
 * All rights reserved. Unauthorized distribution or disclosure is prohibited.
*/

import { Component, Input, Output, EventEmitter, forwardRef } from '@angular/core';
import { ControlValueAccessor, NG_VALUE_ACCESSOR } from '@angular/forms';
import { DomSanitizer, SafeHtml } from '@angular/platform-browser';
import { SVG_ICONS } from '../../svg-icons';

@Component({
  selector: 'app-numeric-input',
  templateUrl: './numeric-input.component.html',
  styleUrls: ['./numeric-input.component.css'],
  providers: [{
    provide: NG_VALUE_ACCESSOR,
    useExisting: forwardRef(() => NumericInputComponent),
    multi: true
  }]
})
export class NumericInputComponent implements ControlValueAccessor {
  @Input() min: number = -Infinity;
  @Input() max: number = Infinity;
  @Input() step: number = 1;
  @Input() placeholder: string = '';
  @Input() disabled: boolean = false;
  @Output() valueChange = new EventEmitter<number | null>();

  value: number | null = null;
  
  private onChange: (value: number | null) => void = () => {};
  onTouched: () => void = () => {};

  constructor(private sanitizer: DomSanitizer) {}

  get chevronUpIcon(): SafeHtml {
    return this.sanitizer.bypassSecurityTrustHtml('<svg viewBox="0 0 243.626667 151.893333" width="10" height="10"><path d="M121.813333 0 L243.626667 121.6 L213.333333 151.893333 L121.813333 60.16 L30.2933333 151.893333 L0 121.6 L121.813333 0" fill="currentColor"/></svg>');
  }

  get chevronDownIcon(): SafeHtml {
    return this.sanitizer.bypassSecurityTrustHtml(SVG_ICONS.chevronDown);
  }

  increment() {
    if (this.disabled) return;
    const numValue = this.value !== null ? Number(this.value) : null;
    const currentValue = numValue ?? (this.min !== -Infinity ? this.min - this.step : 0);
    const newValue = currentValue + this.step;
    if (newValue <= this.max) {
      this.setValue(newValue);
    }
  }

  decrement() {
    if (this.disabled) return;
    const numValue = this.value !== null ? Number(this.value) : null;
    const currentValue = numValue ?? (this.max !== Infinity ? this.max + this.step : 0);
    const newValue = currentValue - this.step;
    if (newValue >= this.min) {
      this.setValue(newValue);
    }
  }

  onInputChange(event: Event) {
    const input = event.target as HTMLInputElement;
    const newValue = input.value === '' ? null : Number(input.value);
    
    if (newValue !== null && !isNaN(newValue)) {
      const clampedValue = Math.min(Math.max(newValue, this.min), this.max);
      this.setValue(clampedValue);
    } else if (newValue === null) {
      this.setValue(null);
    }
  }

  private setValue(value: number | null) {
    this.value = value;
    // Emit as string if value exists, null otherwise
    const emitValue = value !== null ? String(value) : null;
    this.onChange(emitValue as any);
    this.onTouched();
    this.valueChange.emit(value);
  }

  writeValue(value: number | string | null): void {
    this.value = value !== null && value !== '' ? Number(value) : null;
  }

  registerOnChange(fn: (value: number | null) => void): void {
    this.onChange = fn;
  }

  registerOnTouched(fn: () => void): void {
    this.onTouched = fn;
  }

  setDisabledState(isDisabled: boolean): void {
    this.disabled = isDisabled;
  }
}
