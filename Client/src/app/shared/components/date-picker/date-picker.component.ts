/*
 * ROBIGOO FIELD SPRAYER CONTROL STATION
 * Copyright (c) 2025 Wojciech Salamon <wojciech.salamon@yahoo.com>
 * All rights reserved. Unauthorized distribution or disclosure is prohibited.
*/

import { Component, Input, Output, EventEmitter, forwardRef, ElementRef, HostListener } from '@angular/core';
import { ControlValueAccessor, NG_VALUE_ACCESSOR } from '@angular/forms';
import { DomSanitizer, SafeHtml } from '@angular/platform-browser';
import { SVG_ICONS } from '../../svg-icons';

@Component({
  selector: 'app-date-picker',
  templateUrl: './date-picker.component.html',
  styleUrls: ['./date-picker.component.css'],
  providers: [{
    provide: NG_VALUE_ACCESSOR,
    useExisting: forwardRef(() => DatePickerComponent),
    multi: true
  }]
})
export class DatePickerComponent implements ControlValueAccessor {
  @Input() placeholder: string = 'Wybierz datę...';
  @Input() disabled: boolean = false;
  @Input() minDate: string = '';
  @Input() maxDate: string = '';
  @Output() dateChange = new EventEmitter<string>();

  isOpen = false;
  value: string = '';
  
  currentMonth: Date = new Date();
  weeks: Date[][] = [];
  weekDays = ['Pn', 'Wt', 'Śr', 'Cz', 'Pt', 'So', 'Nd'];
  
  private onChange: (value: string) => void = () => {};
  private onTouched: () => void = () => {};

  constructor(private elementRef: ElementRef, private sanitizer: DomSanitizer) {
    this.generateCalendar();
  }

  get calendarIcon(): SafeHtml {
    return this.sanitizer.bypassSecurityTrustHtml(SVG_ICONS.calendarIcon);
  }

  get chevronLeftIcon(): SafeHtml {
    return this.sanitizer.bypassSecurityTrustHtml(SVG_ICONS.iconBack);
  }

  get chevronRightIcon(): SafeHtml {
    return this.sanitizer.bypassSecurityTrustHtml(SVG_ICONS.iconNext);
  }

  get displayValue(): string {
    if (!this.value) return this.placeholder;
    const date = new Date(this.value);
    return date.toLocaleDateString('pl-PL');
  }

  get monthYearLabel(): string {
    return this.currentMonth.toLocaleDateString('pl-PL', { month: 'long', year: 'numeric' });
  }

  @HostListener('document:click', ['$event'])
  onDocumentClick(event: MouseEvent) {
    if (!this.elementRef.nativeElement.contains(event.target)) {
      this.isOpen = false;
    }
  }

  toggle() {
    if (!this.disabled) {
      this.isOpen = !this.isOpen;
      if (this.isOpen && this.value) {
        this.currentMonth = new Date(this.value);
        this.generateCalendar();
      }
    }
  }

  prevMonth() {
    this.currentMonth = new Date(this.currentMonth.getFullYear(), this.currentMonth.getMonth() - 1, 1);
    this.generateCalendar();
  }

  nextMonth() {
    this.currentMonth = new Date(this.currentMonth.getFullYear(), this.currentMonth.getMonth() + 1, 1);
    this.generateCalendar();
  }

  generateCalendar() {
    const year = this.currentMonth.getFullYear();
    const month = this.currentMonth.getMonth();
    
    const firstDay = new Date(year, month, 1);
    const lastDay = new Date(year, month + 1, 0);
    
    // Adjust for Monday start (0 = Monday, 6 = Sunday)
    let startDay = firstDay.getDay() - 1;
    if (startDay < 0) startDay = 6;
    
    const days: Date[] = [];
    
    // Previous month days
    for (let i = startDay - 1; i >= 0; i--) {
      days.push(new Date(year, month, -i));
    }
    
    // Current month days
    for (let i = 1; i <= lastDay.getDate(); i++) {
      days.push(new Date(year, month, i));
    }
    
    // Next month days
    const remaining = 42 - days.length;
    for (let i = 1; i <= remaining; i++) {
      days.push(new Date(year, month + 1, i));
    }
    
    // Split into weeks
    this.weeks = [];
    for (let i = 0; i < days.length; i += 7) {
      this.weeks.push(days.slice(i, i + 7));
    }
  }

  selectDate(date: Date) {
    const formatted = this.formatDate(date);
    this.value = formatted;
    this.isOpen = false;
    this.onChange(formatted);
    this.onTouched();
    this.dateChange.emit(formatted);
  }

  isCurrentMonth(date: Date): boolean {
    return date.getMonth() === this.currentMonth.getMonth();
  }

  isSelected(date: Date): boolean {
    if (!this.value) return false;
    return this.formatDate(date) === this.value;
  }

  isToday(date: Date): boolean {
    const today = new Date();
    return date.toDateString() === today.toDateString();
  }

  isDisabledDate(date: Date): boolean {
    const formatted = this.formatDate(date);
    if (this.minDate && formatted < this.minDate) return true;
    if (this.maxDate && formatted > this.maxDate) return true;
    return false;
  }

  private formatDate(date: Date): string {
    const year = date.getFullYear();
    const month = String(date.getMonth() + 1).padStart(2, '0');
    const day = String(date.getDate()).padStart(2, '0');
    return `${year}-${month}-${day}`;
  }

  clearDate(event: Event) {
    event.stopPropagation();
    this.value = '';
    this.onChange('');
    this.onTouched();
    this.dateChange.emit('');
  }

  writeValue(value: string): void {
    this.value = value || '';
    if (this.value) {
      this.currentMonth = new Date(this.value);
      this.generateCalendar();
    }
  }

  registerOnChange(fn: (value: string) => void): void {
    this.onChange = fn;
  }

  registerOnTouched(fn: () => void): void {
    this.onTouched = fn;
  }

  setDisabledState(isDisabled: boolean): void {
    this.disabled = isDisabled;
  }
}
