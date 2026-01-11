/*
 * ROBIGOO FIELD SPRAYER CONTROL STATION
 * Copyright (c) 2025 Wojciech Salamon <wojciech.salamon@yahoo.com>
 * All rights reserved. Unauthorized distribution or disclosure is prohibited.
*/

import { Component, Input, Output, EventEmitter, OnInit } from '@angular/core';
import { DomSanitizer, SafeHtml } from '@angular/platform-browser';
import { HistoryService, ChangeLog } from '../../../services/history.service';
import { SVG_ICONS } from '../../svg-icons';

@Component({
  selector: 'app-history-dialog',
  templateUrl: './history-dialog.component.html',
  styleUrls: ['./history-dialog.component.css']
})
export class HistoryDialogComponent implements OnInit {
  @Input() entityName: string = '';
  @Input() entityId: string = '';
  @Output() close = new EventEmitter<void>();

  logs: ChangeLog[] = [];
  loading: boolean = true;
  userIcon: string = SVG_ICONS.userDefaultAvatar;

  constructor(private historyService: HistoryService, private sanitizer: DomSanitizer) { }

  ngOnInit(): void {
    if (this.entityName && this.entityId) {
      this.loadHistory();
    }
  }

  loadHistory(): void {
    this.loading = true;
    this.historyService.getHistory(this.entityName, this.entityId).subscribe({
      next: (data) => {
        this.logs = data;
        this.loading = false;
      },
      error: (err) => {
        console.error('Failed to load history', err);
        this.loading = false;
      }
    });
  }

  onClose(): void {
    this.close.emit();
  }

  onBackdropClick(event: MouseEvent): void {
    if (event.target === event.currentTarget) {
      this.onClose();
    }
  }

  getSafeHtml(icon: string): SafeHtml {
    return this.sanitizer.bypassSecurityTrustHtml(icon);
  }
  
  getUserDisplayName(log: ChangeLog): string {
    if (log.userFirstName && log.userLastName) {
      return `${log.userFirstName} ${log.userLastName}`;
    }
    return log.who; // Fallback to login
  }

  hasAvatar(log: ChangeLog): boolean {
    return !!log.userAvatarData;
  }

  getAvatarSrc(log: ChangeLog): string {
    return `data:image/png;base64,${log.userAvatarData}`;
  }

  formatChanges(changes: string): SafeHtml {
    if (!changes) return '';

    const firstColonIndex = changes.indexOf(':');
    if (firstColonIndex === -1) {
      return changes; 
    }

    const action = changes.substring(0, firstColonIndex);
    const detailsRaw = changes.substring(firstColonIndex + 1).trim();
    
    // Split by comma followed by a quote (start of next key)
    const parts = detailsRaw.split(/, (?=')/);
    
    let html = `<div style="font-weight: 600; margin-bottom: 4px;">${action}:</div><ul style="list-style: none; padding-left: 0; margin: 0;">`;
    
    parts.forEach(part => {
        // Filter out UpdatedAt
        if (part.startsWith("'UpdatedAt'") || part.startsWith("'Updated At'")) {
            return;
        }

        html += `<li style="padding-left: 10px; margin-bottom: 2px;">${part.trim()}</li>`;
    });
    
    html += '</ul>';
    
    return this.sanitizer.bypassSecurityTrustHtml(html);
  }
}
