/*
 * ROBIGOO FIELD SPRAYER CONTROL STATION
 * Copyright (c) 2025 Wojciech Salamon <wojciech.salamon@yahoo.com>
 * All rights reserved. Unauthorized distribution or disclosure is prohibited.
 */

import { Component, Input } from '@angular/core';

/**
 * Master-detail layout component that provides a responsive two-column layout.
 * 
 * This component creates a reusable layout structure with:
 * - Left panel (master/list view) - typically 1fr
 * - Right panel (detail/form view) - typically 2.5fr
 * - Responsive: stacks on smaller screens
 * 
 * Usage example:
 * ```html
 * <app-master-detail-layout>
 *   <ng-container master>
 *     <app-entity-list-panel ...></app-entity-list-panel>
 *   </ng-container>
 *   
 *   <ng-container detail>
 *     <app-entity-detail-panel ...></app-entity-detail-panel>
 *   </ng-container>
 * </app-master-detail-layout>
 * ```
 */
@Component({
  selector: 'app-master-detail-layout',
  templateUrl: './master-detail-layout.component.html',
  styleUrls: ['./master-detail-layout.component.css']
})
export class MasterDetailLayoutComponent {
  /** CSS class to add to the container */
  @Input() containerClass = '';

  /** Minimum width for the list panel */
  @Input() listMinWidth = '220px';

  /** Flex ratio for the list panel */
  @Input() listFlex = '1';

  /** Flex ratio for the detail panel */
  @Input() detailFlex = '2.5';

  /** Breakpoint for responsive stacking (in px) */
  @Input() stackBreakpoint = 894;
}
