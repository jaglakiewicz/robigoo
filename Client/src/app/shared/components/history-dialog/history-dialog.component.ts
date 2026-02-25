/*
 * ROBIGOO FIELD SPRAYER CONTROL STATION
 * Copyright (c) 2025 Wojciech Salamon <wojciech.salamon@yahoo.com>
 * All rights reserved. Unauthorized distribution or disclosure is prohibited.
*/

import { Component, Input, Output, EventEmitter, OnInit } from '@angular/core';
import { DomSanitizer, SafeHtml } from '@angular/platform-browser';
import { HistoryService, ChangeLog } from '../../../services/history.service';
import { TextService } from '../../../services/text.service';

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

  // Lucide icons
  userIcon = '<svg xmlns="http://www.w3.org/2000/svg" width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M19 21v-2a4 4 0 0 0-4-4H9a4 4 0 0 0-4 4v2"></path><circle cx="12" cy="7" r="4"></circle></svg>';
  arrowIcon = '<svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><line x1="5" y1="12" x2="19" y2="12"></line><polyline points="12 5 19 12 12 19"></polyline></svg>';

  constructor(
    private historyService: HistoryService, 
    private sanitizer: DomSanitizer,
    private textService: TextService
  ) { }

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
    return log.who;
  }

  hasAvatar(log: ChangeLog): boolean {
    return !!log.userAvatarData;
  }

  getAvatarSrc(log: ChangeLog): string {
    return `data:image/png;base64,${log.userAvatarData}`;
  }

  parseChanges(changes: string): { action: string; fields: Array<{ label: string; oldValue?: string; newValue?: string }> } {
    if (!changes) return { action: '', fields: [] };

    const firstColonIndex = changes.indexOf(':');
    if (firstColonIndex === -1) {
      return { action: changes, fields: [] };
    }

    const action = changes.substring(0, firstColonIndex).trim();
    const detailsRaw = changes.substring(firstColonIndex + 1).trim();
    
    // Split by comma followed by a quote (start of next key)
    const parts = detailsRaw.split(/, (?=')/);
    
    const fields: Array<{ label: string; oldValue?: string; newValue?: string }> = [];
    
    parts.forEach(part => {
      const trimmed = part.trim();
      
      // Skip UpdatedAt field
      if (trimmed.startsWith("'UpdatedAt'") || trimmed.startsWith("'Updated At'")) {
        return;
      }

      // Parse field: 'FieldName': oldValue -> newValue OR 'FieldName': value
      const match = trimmed.match(/^'([^']+)':\s*(.+)$/);
      if (match) {
        const fieldName = match[1];
        const valuesPart = match[2];
        
        // Get Polish label
        const label = this.getFieldLabel(fieldName);
        
        // Check if it's a modification (contains ->)
        if (valuesPart.includes(' -> ')) {
          const [oldVal, newVal] = valuesPart.split(' -> ').map(v => v.trim());
          fields.push({ label, oldValue: oldVal, newValue: newVal });
        } else {
          // It's a creation or single value
          fields.push({ label, newValue: valuesPart });
        }
      }
    });
    
    return { action, fields };
  }

  getFieldLabel(fieldName: string): string {
    // Map database field names to text service keys
    const fieldMap: Record<string, string> = {
      'ClientType': 'clients.fields.clientType',
      'FirstName': 'clients.fields.firstName',
      'LastName': 'clients.fields.lastName',
      'Pesel': 'clients.fields.pesel',
      'CompanyName': 'clients.fields.companyName',
      'Nip': 'clients.fields.nip',
      'Regon': 'clients.fields.regon',
      'Voivodeship': 'clients.fields.voivodeship',
      'City': 'clients.fields.city',
      'Street': 'clients.fields.street',
      'BuildingNumber': 'clients.fields.buildingNumber',
      'ApartmentNumber': 'clients.fields.apartmentNumber',
      'ZipCode': 'clients.fields.zipCode',
      'DisplayName': 'clients.fields.displayName'
    };

    const key = fieldMap[fieldName];
    if (key) {
      return this.textService.get(key);
    }
    
    // Fallback to field name
    return fieldName;
  }
}
