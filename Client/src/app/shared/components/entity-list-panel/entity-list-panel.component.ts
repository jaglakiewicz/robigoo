/*
 * ROBIGOO FIELD SPRAYER CONTROL STATION
 * Copyright (c) 2025 Wojciech Salamon <wojciech.salamon@yahoo.com>
 * All rights reserved. Unauthorized distribution or disclosure is prohibited.
 */

import { Component, ContentChild, EventEmitter, Input, Output, TemplateRef } from '@angular/core';
import { FilterField, FilterValues } from '../filter-panel/filter-panel.component';

/**
 * Generic entity list panel component for master-detail layouts.
 * 
 * This component provides a reusable list panel with:
 * - Search input
 * - Filter panel integration
 * - Card-based item display via ng-template
 * - Loading and empty states
 * 
 * Usage example:
 * ```html
 * <app-entity-list-panel
 *   [title]="'My Items'"
 *   [items]="items"
 *   [loading]="isLoading"
 *   [selectedId]="selectedItemId"
 *   [filterFields]="filterFields"
 *   [filterValues]="filterValues"
 *   [searchPlaceholder]="'Search...'"
 *   [emptyMessage]="'No items found'"
 *   (searchChange)="onSearch($event)"
 *   (filterChange)="onFilter($event)"
 *   (filterClear)="onFilterClear()"
 *   (itemSelect)="onItemSelect($event)">
 *   
 *   <ng-template #itemTemplate let-item>
 *     <div class="card-title">{{ item.name }}</div>
 *   </ng-template>
 * </app-entity-list-panel>
 * ```
 */
@Component({
  selector: 'app-entity-list-panel',
  templateUrl: './entity-list-panel.component.html',
  styleUrls: ['./entity-list-panel.component.css']
})
export class EntityListPanelComponent<T> {
  /** Title displayed at the top of the list panel */
  @Input() title = '';

  /** Array of items to display in the list */
  @Input() items: T[] = [];

  /** Whether the list is currently loading */
  @Input() loading = false;

  /** Currently selected item ID for highlighting */
  @Input() selectedId: string | number | null = null;

  /** Key in item object to use as ID for selection comparison */
  @Input() idKey = 'id';

  /** Current search term */
  @Input() searchTerm = '';

  /** Search input placeholder text */
  @Input() searchPlaceholder = 'Search...';

  /** Filter panel field definitions */
  @Input() filterFields: FilterField[] = [];

  /** Current filter values */
  @Input() filterValues: FilterValues = {};

  /** Label for the clear filters button */
  @Input() clearButtonLabel = 'Clear';

  /** Label for the apply filters button */
  @Input() applyButtonLabel = 'Apply';

  /** Message to display when the list is empty */
  @Input() emptyMessage = 'No items found';

  /** Message to display while loading */
  @Input() loadingMessage = 'Loading...';

  /** Whether to blur the list when filter panel is open */
  @Input() blurOnFilterOpen = true;

  /** CSS class for items in the list (used for scroll centering) */
  @Input() itemClass = 'entity-card';

  /** Emitted when the search term changes */
  @Output() searchChange = new EventEmitter<string>();

  /** Emitted when filter values change */
  @Output() filterChange = new EventEmitter<FilterValues>();

  /** Emitted when filters are cleared */
  @Output() filterClear = new EventEmitter<void>();

  /** Emitted when an item is selected */
  @Output() itemSelect = new EventEmitter<T>();

  /** Template for rendering individual items */
  @ContentChild('itemTemplate') itemTemplate!: TemplateRef<{ $implicit: T; selected: boolean }>;

  filterPanelOpen = false;

  onSearchChange(value: string): void {
    this.searchChange.emit(value);
  }

  onFilterChange(values: FilterValues): void {
    this.filterChange.emit(values);
  }

  onFilterClear(): void {
    this.filterClear.emit();
  }

  onFilterPanelOpenChange(isOpen: boolean): void {
    this.filterPanelOpen = isOpen;
  }

  onItemClick(item: T): void {
    this.itemSelect.emit(item);
  }

  isSelected(item: T): boolean {
    return (item as any)[this.idKey] === this.selectedId;
  }

  trackByFn(index: number, item: T): any {
    return (item as any)[this.idKey] ?? index;
  }
}
