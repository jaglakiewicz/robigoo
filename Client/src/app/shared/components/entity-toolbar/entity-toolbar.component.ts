/*
 * ROBIGOO FIELD SPRAYER CONTROL STATION
 * Copyright (c) 2025 Wojciech Salamon <wojciech.salamon@yahoo.com>
 * All rights reserved. Unauthorized distribution or disclosure is prohibited.
 */

import { Component, Input } from '@angular/core';

/**
 * Toolbar component for entity management actions.
 * 
 * Provides a consistent layout for action buttons with:
 * - Left-aligned primary actions (add, edit, delete)
 * - Right-aligned contextual actions (save, cancel)
 * 
 * Usage example:
 * ```html
 * <app-entity-toolbar>
 *   <ng-container left>
 *     <button class="btn btn-success">Add</button>
 *     <button class="btn btn-primary">Edit</button>
 *   </ng-container>
 *   
 *   <ng-container right>
 *     <button class="btn btn-primary">Save</button>
 *     <button class="btn btn-secondary">Cancel</button>
 *   </ng-container>
 * </app-entity-toolbar>
 * ```
 */
@Component({
  selector: 'app-entity-toolbar',
  templateUrl: './entity-toolbar.component.html',
  styleUrls: ['./entity-toolbar.component.css']
})
export class EntityToolbarComponent {
  /** Additional CSS class for the toolbar container */
  @Input() containerClass = '';
}
