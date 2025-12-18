/*
 * ROBIGOO FIELD SPRAYER CONTROL STATION
 * Copyright (c) 2025 Wojciech Salamon <wojciech.salamon@yahoo.com>
 * All rights reserved. Unauthorized distribution or disclosure is prohibited.
*/

export interface FilterDefinition {
  key: string;
  type: 'text' | 'date' | 'number' | 'select';
  placeholder?: string;
  label?: string;
  options?: Array<{ label: string; value: any }>;
}

export interface ColumnConfig {
  key: string;
  label: string;
  type?: 'text' | 'date' | 'number' | 'custom' | 'boolean';
  format?: string;
  sortable?: boolean;
  width?: string;
  render?: (value: any, row: any) => string;
}

export class FilterUtil {
  static applyFilters<T>(
    items: T[],
    filters: { [key: string]: any },
    filterConfig?: { [key: string]: (item: T, value: any) => boolean }
  ): T[] {
    return items.filter(item => {
      return Object.entries(filters).every(([key, value]) => {
        if (value == null || value === '') return true;

        if (filterConfig?.[key]) {
          return filterConfig[key](item, value);
        }

        const itemValue = (item as any)[key];
        if (typeof itemValue === 'string') {
          return itemValue.toLowerCase().includes(String(value).toLowerCase());
        }
        if (itemValue instanceof Date) {
          return itemValue >= new Date(value);
        }
        return itemValue === value;
      });
    });
  }

  static searchInFields<T>(
    items: T[],
    searchTerm: string,
    fields: (keyof T)[]
  ): T[] {
    if (!searchTerm) return items;

    const lowerSearch = searchTerm.toLowerCase();
    return items.filter(item =>
      fields.some(field => {
        const value = String(item[field]).toLowerCase();
        return value.includes(lowerSearch);
      })
    );
  }
}

