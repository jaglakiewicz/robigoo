/*
 * ROBIGOO FIELD SPRAYER CONTROL STATION
 * Copyright (c) 2025 Wojciech Salamon <wojciech.salamon@yahoo.com>
 * All rights reserved. Unauthorized distribution or disclosure is prohibited.
*/

import { Component, OnInit } from '@angular/core';
import { Inspection } from '../inspection.service';
import { ColumnConfig, FilterDefinition } from '../shared/models/filter.model';

@Component({
  selector: 'app-inspections',
  templateUrl: './inspections.component.html',
  styleUrls: ['./inspections.component.css']
})
export class InspectionsComponent implements OnInit {
  endpoint = 'inspections';
  title = 'inspections.filterTitle';
  emptyMessage = 'inspections.empty';

  columns: ColumnConfig[] = [
    { key: 'vehiclePlate', label: 'inspections.columns.plate', sortable: true },
    { key: 'inspectorName', label: 'inspections.columns.inspector', sortable: true },
    { key: 'inspectionDate', label: 'inspections.columns.date', type: 'date' },
    { 
      key: 'items', 
      label: 'inspections.columns.items',
      render: (value) => String(value?.length || 0)
    },
    { 
      key: 'items', 
      label: 'inspections.columns.passRate',
      render: (value) => {
        const passed = value?.filter((it: any) => it.passed).length || 0;
        const total = value?.length || 0;
        return `${passed} / ${total}`;
      }
    }
  ];

  filters: FilterDefinition[] = [
    { key: 'vehiclePlate', type: 'text', placeholder: 'inspections.placeholders.plate' },
    { key: 'inspectorName', type: 'text', placeholder: 'inspections.placeholders.inspector' },
    { key: 'inspectionDate', type: 'date', label: 'inspections.placeholders.dateFrom' }
  ];

  constructor() { }

  ngOnInit(): void {
  }
}

