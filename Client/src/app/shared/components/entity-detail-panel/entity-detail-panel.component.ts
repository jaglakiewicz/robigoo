/*
 * ROBIGOO FIELD SPRAYER CONTROL STATION
 * Copyright (c) 2025 Wojciech Salamon <wojciech.salamon@yahoo.com>
 * All rights reserved. Unauthorized distribution or disclosure is prohibited.
 */

import { Component, ContentChild, ElementRef, EventEmitter, Input, Output, TemplateRef, ViewChild } from '@angular/core';
import { DomSanitizer, SafeHtml } from '@angular/platform-browser';
import { SVG_ICONS } from '../../svg-icons';

/**
 * Generic entity detail panel component for master-detail layouts.
 * 
 * This component provides a reusable detail panel with:
 * - Header with title and subtitle
 * - Optional history button
 * - Step indicator (optional)
 * - Content projection via ng-content
 * 
 * Usage example:
 * ```html
 * <app-entity-detail-panel
 *   [title]="item.name"
 *   [subtitle]="item.description"
 *   [showHistory]="true"
 *   [isNew]="isNew"
 *   [isEditMode]="isEditMode"
 *   (historyClick)="openHistory()">
 *   
 *   <!-- Your detail form content here -->
 *   <div class="form-content">...</div>
 * </app-entity-detail-panel>
 * ```
 */
@Component({
  selector: 'app-entity-detail-panel',
  templateUrl: './entity-detail-panel.component.html',
  styleUrls: ['./entity-detail-panel.component.css']
})
export class EntityDetailPanelComponent {
  /** Main title displayed in the header */
  @Input() title = '';

  /** Placeholder text when title is empty */
  @Input() titlePlaceholder = '';

  /** Subtitle displayed below the title */
  @Input() subtitle = '';

  /** Whether to show the history button */
  @Input() showHistoryButton = false;

  /** Whether entity is new (unsaved) */
  @Input() isNew = false;

  /** Whether currently in edit mode */
  @Input() isEditMode = false;

  /** Title for the history button */
  @Input() historyButtonTitle = 'History';

  /** Emitted when history button is clicked */
  @Output() historyClick = new EventEmitter<void>();

  /** Emitted when the mobile back button is clicked */
  @Output() backClick = new EventEmitter<void>();

  /** Template for custom header actions */
  @ContentChild('headerActions') headerActionsTemplate!: TemplateRef<any>;

  @ViewChild('detailContent') detailContent!: ElementRef<HTMLElement>;

  historyIcon: SafeHtml;

  constructor(private sanitizer: DomSanitizer) {
    this.historyIcon = this.sanitizer.bypassSecurityTrustHtml(SVG_ICONS.historyIcon);
  }

  get displayTitle(): string {
    return this.title || this.titlePlaceholder;
  }

  get showHistory(): boolean {
    return this.showHistoryButton && !this.isNew && !this.isEditMode;
  }

  scrollToSection(sectionId: string): void {
    const el = this.detailContent?.nativeElement?.querySelector(`#${sectionId}`);
    if (el) el.scrollIntoView({ behavior: 'smooth', block: 'start' });
  }

  onHistoryClick(): void {
    this.historyClick.emit();
  }
}
