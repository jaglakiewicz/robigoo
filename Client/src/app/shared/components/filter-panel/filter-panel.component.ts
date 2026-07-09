/*
 * ROBIGOO FIELD SPRAYER CONTROL STATION
 * Reusable Filter Panel Component - Filter builder pattern
 * Copyright (c) 2025 Wojciech Salamon
 */

import { Component, Input, Output, EventEmitter, OnInit, OnDestroy, OnChanges, SimpleChanges, ElementRef, HostListener } from '@angular/core';
import { DomSanitizer, SafeHtml } from '@angular/platform-browser';
import { Subject } from 'rxjs';
import { SVG_ICONS } from '../../svg-icons';

export type FilterFieldType = 'text' | 'number' | 'select' | 'date' | 'boolean';

export interface FilterFieldOption {
  value: string | number | boolean;
  label: string;
}

export interface FilterField {
  key: string;
  label: string;
  type: FilterFieldType;
  options?: FilterFieldOption[];
  min?: number;
  max?: number;
  step?: number;
}

export interface ActiveFilter {
  columnKey: string;
  operator: string;
  value: any;
  valueTo?: any;
}

export interface FilterValues {
  [key: string]: any;
}

const OPERATORS: Record<FilterFieldType, { value: string; label: string }[]> = {
  'text': [
    { value: 'contains', label: 'Zawiera' },
    { value: 'equals', label: 'Równe' },
    { value: 'startsWith', label: 'Zaczyna się od' }
  ],
  'number': [
    { value: 'equals', label: '=' },
    { value: 'gt', label: '>' },
    { value: 'lt', label: '<' },
    { value: 'gte', label: '≥' },
    { value: 'lte', label: '≤' },
    { value: 'between', label: 'Między' }
  ],
  'date': [
    { value: 'equals', label: 'Równe' },
    { value: 'after', label: 'Po' },
    { value: 'before', label: 'Przed' },
    { value: 'between', label: 'Między' }
  ],
  'select': [
    { value: 'equals', label: 'Równe' }
  ],
  'boolean': [
    { value: 'equals', label: 'Równe' }
  ]
};

@Component({
  selector: 'app-filter-panel',
  templateUrl: './filter-panel.component.html',
  styleUrls: ['./filter-panel.component.css']
})
export class FilterPanelComponent implements OnInit, OnDestroy, OnChanges {
  @Input() fields: FilterField[] = [];
  @Input() values: FilterValues = {};
  @Input() showClearButton = true;
  @Input() clearButtonLabel = 'Wyczyść';
  @Input() applyButtonLabel = 'Zastosuj';
  
  @Output() valuesChange = new EventEmitter<FilterValues>();
  @Output() filterApply = new EventEmitter<FilterValues>();
  @Output() filterClear = new EventEmitter<void>();
  @Output() openChange = new EventEmitter<boolean>();

  isOpen = false;
  activeFilters: ActiveFilter[] = [];
  showColumnPicker = false;
  activeFilterCount = 0;

  filterIcon!: SafeHtml;
  addIcon!: SafeHtml;
  removeIcon!: SafeHtml;

  private destroy$ = new Subject<void>();

  constructor(
    private elementRef: ElementRef,
    private sanitizer: DomSanitizer
  ) {
    this.filterIcon = this.sanitizer.bypassSecurityTrustHtml(SVG_ICONS.filterIcon);
    this.addIcon = this.sanitizer.bypassSecurityTrustHtml(
      '<svg viewBox="0 0 24 24" width="14" height="14" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M12 5v14"/><path d="M5 12h14"/></svg>'
    );
    this.removeIcon = this.sanitizer.bypassSecurityTrustHtml(
      '<svg viewBox="0 0 24 24" width="14" height="14" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M18 6 6 18"/><path d="m6 6 12 12"/></svg>'
    );
  }

  ngOnInit(): void {
    this.restoreFiltersFromValues();
  }

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['values'] && !changes['values'].firstChange) {
      this.restoreFiltersFromValues();
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
      this.showColumnPicker = false;
      this.openChange.emit(true);
    }
  }

  private closeAndReset(): void {
    if (this.isOpen) {
      this.restoreFiltersFromValues();
      this.isOpen = false;
      this.showColumnPicker = false;
      this.openChange.emit(false);
    }
  }

  // --- Column picker ---
  get availableColumns(): FilterField[] {
    const usedKeys = new Set(this.activeFilters.map(f => f.columnKey));
    return this.fields.filter(f => !usedKeys.has(f.key));
  }

  toggleColumnPicker(): void {
    this.showColumnPicker = !this.showColumnPicker;
  }

  addFilter(field: FilterField): void {
    const ops = this.getOperators(field.type);
    const defaultOp = ops[0]?.value || 'equals';
    let defaultVal: any = '';
    if (field.type === 'boolean') {
      defaultVal = field.options?.[0]?.value ?? true;
    } else if (field.type === 'select') {
      defaultVal = field.options?.[0]?.value ?? '';
    }
    this.activeFilters.push({
      columnKey: field.key,
      operator: defaultOp,
      value: defaultVal
    });
    this.showColumnPicker = false;
    this.updateActiveFilterCount();
  }

  removeFilter(index: number): void {
    this.activeFilters.splice(index, 1);
    this.updateActiveFilterCount();
  }

  // --- Operators ---
  getOperators(type: FilterFieldType): { value: string; label: string }[] {
    return OPERATORS[type] || OPERATORS['text'];
  }

  getFieldForFilter(filter: ActiveFilter): FilterField | undefined {
    return this.fields.find(f => f.key === filter.columnKey);
  }

  onOperatorChange(index: number, operator: string): void {
    this.activeFilters[index].operator = operator;
    if (operator !== 'between') {
      this.activeFilters[index].valueTo = undefined;
    }
  }

  onValueChange(index: number, value: any): void {
    this.activeFilters[index].value = value;
  }

  onValueToChange(index: number, value: any): void {
    this.activeFilters[index].valueTo = value;
  }

  // --- Apply / Clear ---
  applyFilters(): void {
    const values = this.buildFilterValues();
    this.valuesChange.emit(values);
    this.filterApply.emit(values);
    this.isOpen = false;
    this.showColumnPicker = false;
    this.openChange.emit(false);
  }

  clearFilters(): void {
    this.activeFilters = [];
    this.updateActiveFilterCount();
    this.valuesChange.emit({});
    this.filterClear.emit();
    this.isOpen = false;
    this.showColumnPicker = false;
    this.openChange.emit(false);
  }

  private buildFilterValues(): FilterValues {
    const values: FilterValues = {};
    for (const filter of this.activeFilters) {
      if (filter.value === '' || filter.value === null || filter.value === undefined) continue;
      const field = this.getFieldForFilter(filter);
      if (!field) continue;

      // Store as structured: key__operator = value
      values[`${filter.columnKey}__${filter.operator}`] = filter.value;
      if (filter.operator === 'between' && filter.valueTo != null && filter.valueTo !== '') {
        values[`${filter.columnKey}__between_to`] = filter.valueTo;
      }
    }
    return values;
  }

  private restoreFiltersFromValues(): void {
    this.activeFilters = [];
    if (!this.values) {
      this.updateActiveFilterCount();
      return;
    }

    const parsed = new Map<string, ActiveFilter>();
    for (const [compositeKey, value] of Object.entries(this.values)) {
      if (value === '' || value === null || value === undefined) continue;
      
      const parts = compositeKey.split('__');
      const columnKey = parts[0];
      const operatorPart = parts[1] || 'contains';

      if (operatorPart === 'between_to') {
        const existing = parsed.get(columnKey);
        if (existing) {
          existing.valueTo = value;
        }
        continue;
      }

      if (!parsed.has(columnKey)) {
        parsed.set(columnKey, {
          columnKey,
          operator: operatorPart,
          value
        });
      }
    }

    this.activeFilters = Array.from(parsed.values());
    this.updateActiveFilterCount();
  }

  private updateActiveFilterCount(): void {
    this.activeFilterCount = this.activeFilters.filter(f => 
      f.value !== '' && f.value !== null && f.value !== undefined
    ).length;
  }

  // --- Static utility for client-side filtering ---
  static applyFilters<T>(items: T[], activeValues: FilterValues, fields: FilterField[]): T[] {
    if (!activeValues || Object.keys(activeValues).length === 0) return items;

    const fieldMap = new Map(fields.map(f => [f.key, f]));

    return items.filter(item => {
      for (const [compositeKey, filterValue] of Object.entries(activeValues)) {
        if (filterValue === '' || filterValue === null || filterValue === undefined) continue;
        if (compositeKey.endsWith('__between_to')) continue;

        const parts = compositeKey.split('__');
        const columnKey = parts[0];
        const operator = parts[1] || 'contains';
        const field = fieldMap.get(columnKey);
        if (!field) continue;

        const itemValue = (item as any)[columnKey];

        if (!FilterPanelComponent.matchFilter(itemValue, filterValue, operator, field.type, activeValues[`${columnKey}__between_to`])) {
          return false;
        }
      }
      return true;
    });
  }

  private static matchFilter(itemValue: any, filterValue: any, operator: string, type: FilterFieldType, valueTo?: any): boolean {
    if (itemValue === null || itemValue === undefined) {
      return false;
    }

    const strItem = String(itemValue).toLowerCase();
    const strFilter = String(filterValue).toLowerCase();

    switch (type) {
      case 'text':
        switch (operator) {
          case 'contains': return strItem.includes(strFilter);
          case 'equals': return strItem === strFilter;
          case 'startsWith': return strItem.startsWith(strFilter);
          default: return strItem.includes(strFilter);
        }

      case 'number': {
        const numItem = Number(itemValue);
        const numFilter = Number(filterValue);
        if (isNaN(numItem) || isNaN(numFilter)) return false;
        switch (operator) {
          case 'equals': return numItem === numFilter;
          case 'gt': return numItem > numFilter;
          case 'lt': return numItem < numFilter;
          case 'gte': return numItem >= numFilter;
          case 'lte': return numItem <= numFilter;
          case 'between': {
            const numTo = valueTo != null ? Number(valueTo) : Infinity;
            return numItem >= numFilter && numItem <= numTo;
          }
          default: return numItem === numFilter;
        }
      }

      case 'date': {
        const dateItem = new Date(itemValue).getTime();
        const dateFilter = new Date(filterValue).getTime();
        if (isNaN(dateItem) || isNaN(dateFilter)) return false;
        switch (operator) {
          case 'equals': {
            const d1 = new Date(itemValue); d1.setHours(0,0,0,0);
            const d2 = new Date(filterValue); d2.setHours(0,0,0,0);
            return d1.getTime() === d2.getTime();
          }
          case 'after': return dateItem > dateFilter;
          case 'before': return dateItem < dateFilter;
          case 'between': {
            const dateTo = valueTo ? new Date(valueTo).getTime() : Infinity;
            return dateItem >= dateFilter && dateItem <= dateTo;
          }
          default: return dateItem === dateFilter;
        }
      }

      case 'select':
        return strItem === strFilter;

      case 'boolean':
        return String(itemValue) === strFilter;

      default:
        return strItem.includes(strFilter);
    }
  }
}
