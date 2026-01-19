/*
 * ROBIGOO FIELD SPRAYER CONTROL STATION
 * Reusable Filter Panel Component
 * Copyright (c) 2025 Wojciech Salamon
 */

import { Component, Input, Output, EventEmitter, OnInit, OnDestroy, OnChanges, SimpleChanges, ElementRef, HostListener } from '@angular/core';
import { DomSanitizer, SafeHtml } from '@angular/platform-browser';
import { Subject } from 'rxjs';
import { takeUntil } from 'rxjs/operators';
import { SVG_ICONS } from '../../svg-icons';

export type FilterFieldType = 'text' | 'number' | 'select' | 'date' | 'checkbox' | 'range';

export interface FilterFieldOption {
  value: string | number | boolean;
  label: string;
}

export interface FilterField {
  key: string;
  label: string;
  type: FilterFieldType;
  placeholder?: string;
  options?: FilterFieldOption[];
  min?: number;
  max?: number;
  step?: number;
  // For range type
  rangeFromKey?: string;
  rangeToKey?: string;
  rangeFromPlaceholder?: string;
  rangeToPlaceholder?: string;
}

export interface FilterValues {
  [key: string]: any;
}

@Component({
  selector: 'app-filter-panel',
  templateUrl: './filter-panel.component.html',
  styleUrls: ['./filter-panel.component.css']
})
export class FilterPanelComponent implements OnInit, OnDestroy, OnChanges {
  @Input() fields: FilterField[] = [];
  @Input() values: FilterValues = {};
  @Input() showClearButton = true;
  @Input() clearButtonLabel = 'Wyczyść filtry';
  @Input() applyButtonLabel = 'Zastosuj';
  
  @Output() valuesChange = new EventEmitter<FilterValues>();
  @Output() filterApply = new EventEmitter<FilterValues>();
  @Output() filterClear = new EventEmitter<void>();
  @Output() openChange = new EventEmitter<boolean>();

  isOpen = false;
  localValues: FilterValues = {};
  activeFilterCount = 0;

  // Track open select dropdowns
  openSelectKey: string | null = null;

  filterIcon!: SafeHtml;

  private destroy$ = new Subject<void>();

  constructor(
    private elementRef: ElementRef,
    private sanitizer: DomSanitizer
  ) {
    this.filterIcon = this.sanitizer.bypassSecurityTrustHtml(SVG_ICONS.filterIcon);
  }

  ngOnInit(): void {
    this.localValues = { ...this.values };
    this.updateActiveFilterCount();
  }

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['values'] && !changes['values'].firstChange) {
      this.localValues = { ...this.values };
      this.updateActiveFilterCount();
    }
    if (changes['fields'] && !changes['fields'].firstChange) {
      this.updateActiveFilterCount();
    }
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  @HostListener('document:click', ['$event'])
  onDocumentClick(event: MouseEvent): void {
    if (!this.elementRef.nativeElement.contains(event.target)) {
      this.closeAndReset();
    }
  }

  toggle(): void {
    if (this.isOpen) {
      this.closeAndReset();
    } else {
      this.isOpen = true;
      this.openChange.emit(true);
    }
  }

  close(): void {
    this.closeAndReset();
  }

  private closeAndReset(): void {
    if (this.isOpen) {
      // Reset local values to last applied values
      this.localValues = { ...this.values };
      this.updateActiveFilterCount();
      this.isOpen = false;
      this.openSelectKey = null;
      this.openChange.emit(false);
    }
  }

  // Select dropdown handling
  toggleSelect(key: string): void {
    this.openSelectKey = this.openSelectKey === key ? null : key;
  }

  selectOption(key: string, value: any): void {
    this.localValues[key] = value;
    this.openSelectKey = null;
    this.updateActiveFilterCount();
  }

  getSelectedLabel(field: FilterField): string {
    const value = this.localValues[field.key];
    if (value === null || value === undefined || value === '') {
      return field.placeholder || 'Wybierz...';
    }
    const option = field.options?.find(o => o.value === value);
    return option ? option.label : String(value);
  }

  onFieldChange(key: string, value: any): void {
    this.localValues[key] = value;
    this.updateActiveFilterCount();
  }

  onRangeChange(fromKey: string, toKey: string, isFrom: boolean, value: any): void {
    const key = isFrom ? fromKey : toKey;
    this.localValues[key] = value;
    this.updateActiveFilterCount();
  }

  applyFilters(): void {
    this.valuesChange.emit({ ...this.localValues });
    this.filterApply.emit({ ...this.localValues });
    this.isOpen = false;
    this.openSelectKey = null;
    this.openChange.emit(false);
  }

  clearFilters(): void {
    this.localValues = {};
    this.updateActiveFilterCount();
    this.valuesChange.emit(this.localValues);
    this.filterClear.emit();
    this.isOpen = false;
    this.openSelectKey = null;
    this.openChange.emit(false);
  }

  private updateActiveFilterCount(): void {
    this.activeFilterCount = Object.values(this.localValues).filter(v => 
      v !== null && v !== undefined && v !== ''
    ).length;
  }

  getFieldValue(key: string): any {
    return this.localValues[key] ?? '';
  }

  getRangeValue(key: string): any {
    return this.localValues[key] ?? '';
  }

  hasValue(key: string): boolean {
    const v = this.localValues[key];
    return v !== null && v !== undefined && v !== '';
  }
}
