/*
 * ROBIGOO FIELD SPRAYER CONTROL STATION
 * Copyright (c) 2025 Wojciech Salamon <wojciech.salamon@yahoo.com>
 * All rights reserved. Unauthorized distribution or disclosure is prohibited.
*/

import { Component, Input, Output, EventEmitter, OnInit, OnDestroy } from '@angular/core';
import { Subject } from 'rxjs';
import { debounceTime, takeUntil } from 'rxjs/operators';
import { GenericCrudService } from '../../services/generic-crud.service';
import { ColumnConfig, FilterDefinition, FilterUtil } from '../../models/filter.model';

@Component({
  selector: 'app-generic-list',
  templateUrl: './generic-list.component.html',
  styleUrls: ['./generic-list.component.css']
})
export class GenericListComponent<T extends { id?: number | string }> implements OnInit, OnDestroy {
  @Input() endpoint: string = '';
  @Input() columns: ColumnConfig[] = [];
  @Input() filters: FilterDefinition[] = [];
  @Input() title: string = '';
  @Input() emptyMessage: string = 'No data available';
  @Input() filterConfig?: { [key: string]: (item: T, value: any) => boolean };

  @Output() itemSelected = new EventEmitter<T>();
  @Output() dataLoaded = new EventEmitter<T[]>();

  data: T[] = [];
  filtered: T[] = [];
  loading = false;
  filterValues: { [key: string]: any } = {};

  private destroy$ = new Subject<void>();
  private filterSubject$ = new Subject<void>();

  constructor(private crudService: GenericCrudService<T>) { }

  ngOnInit() {
    this.initializeFilters();
    this.setupFilterDebounce();
    this.load();
  }

  ngOnDestroy() {
    this.destroy$.next();
    this.destroy$.complete();
  }

  private initializeFilters() {
    this.filters.forEach(filter => {
      this.filterValues[filter.key] = '';
    });
  }

  private setupFilterDebounce() {
    this.filterSubject$
      .pipe(
        debounceTime(300),
        takeUntil(this.destroy$)
      )
      .subscribe(() => this.applyFilters());
  }

  load() {
    this.loading = true;
    this.crudService.getAll(this.endpoint)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (result) => {
          this.data = result;
          this.applyFilters();
          this.dataLoaded.emit(result);
          this.loading = false;
        },
        error: (err) => {
          console.error('Error loading data:', err);
          this.loading = false;
        }
      });
  }

  applyFilters() {
    this.filtered = FilterUtil.applyFilters(
      this.data,
      this.filterValues,
      this.filterConfig
    );
  }

  onFilterChange() {
    this.filterSubject$.next();
  }

  clearFilters() {
    Object.keys(this.filterValues).forEach(key => {
      this.filterValues[key] = '';
    });
    this.applyFilters();
  }

  getColumnValue(item: T, column: ColumnConfig): string {
    const value = (item as any)[column.key];

    if (column.render) {
      return column.render(value, item);
    }

    if (column.type === 'date' && value) {
      return new Date(value).toLocaleDateString();
    }

    if (column.type === 'number' && column.format) {
      return Number(value).toFixed(parseInt(column.format));
    }

    return String(value || '');
  }

  selectItem(item: T) {
    this.itemSelected.emit(item);
  }
}

