/*
 * ROBIGOO FIELD SPRAYER CONTROL STATION
 * Client Autocomplete Component
 * Copyright (c) 2025 Wojciech Salamon <wojciech.salamon@yahoo.com>
 * All rights reserved. Unauthorized distribution or disclosure is prohibited.
 * 
 * Typeahead component for selecting clients with search functionality.
 * Shows client name and address details for better identification.
 */

import { Component, Input, Output, EventEmitter, forwardRef, ElementRef, HostListener, OnInit, OnDestroy } from '@angular/core';
import { ControlValueAccessor, NG_VALUE_ACCESSOR } from '@angular/forms';
import { DomSanitizer, SafeHtml } from '@angular/platform-browser';
import { Subject, Subscription } from 'rxjs';
import { debounceTime, distinctUntilChanged } from 'rxjs/operators';
import { ClientService, ClientListItem } from '../../../client.service';
import { TextService } from '../../../services/text.service';
import { SVG_ICONS } from '../../svg-icons';

export interface ClientOption {
  id: string;
  displayName: string;
  address: string;
  clientType: 'person' | 'company';
}

@Component({
  selector: 'app-client-autocomplete',
  templateUrl: './client-autocomplete.component.html',
  styleUrls: ['./client-autocomplete.component.css'],
  providers: [{
    provide: NG_VALUE_ACCESSOR,
    useExisting: forwardRef(() => ClientAutocompleteComponent),
    multi: true
  }]
})
export class ClientAutocompleteComponent implements ControlValueAccessor, OnInit, OnDestroy {
  @Input() placeholder: string = '';
  @Input() disabled: boolean = false;
  @Input() allowClear: boolean = true;
  @Output() selectionChange = new EventEmitter<string | null>();

  isOpen = false;
  isLoading = false;
  searchText = '';
  selectedClient: ClientOption | null = null;
  filteredClients: ClientOption[] = [];
  allClients: ClientOption[] = [];
  highlightedIndex = -1;

  private searchSubject = new Subject<string>();
  private searchSubscription?: Subscription;
  private clientsSubscription?: Subscription;
  private value: string | null = null;
  private onChange: (value: string | null) => void = () => {};
  private onTouched: () => void = () => {};

  constructor(
    private elementRef: ElementRef,
    private sanitizer: DomSanitizer,
    private clientService: ClientService,
    private textService: TextService
  ) {}

  ngOnInit(): void {
    this.loadClients();
    
    // Debounced search
    this.searchSubscription = this.searchSubject.pipe(
      debounceTime(150),
      distinctUntilChanged()
    ).subscribe(term => {
      this.filterClients(term);
    });
  }

  ngOnDestroy(): void {
    this.searchSubscription?.unsubscribe();
    this.clientsSubscription?.unsubscribe();
  }

  get clearIcon(): SafeHtml {
    return this.sanitizer.bypassSecurityTrustHtml(SVG_ICONS.closeIcon);
  }

  get chevronIcon(): SafeHtml {
    return this.sanitizer.bypassSecurityTrustHtml(SVG_ICONS.chevronDown);
  }

  private loadClients(): void {
    this.isLoading = true;
    this.clientsSubscription = this.clientService.getClientsList().subscribe({
      next: (clients: ClientListItem[]) => {
        this.allClients = clients.map(c => this.mapClientToOption(c));
        this.isLoading = false;
        
        // If we have a value set, find and display the selected client
        if (this.value) {
          this.selectedClient = this.allClients.find(c => c.id === this.value) || null;
          if (this.selectedClient) {
            this.searchText = this.selectedClient.displayName;
          }
        }
      },
      error: () => {
        this.isLoading = false;
      }
    });
  }

  private mapClientToOption(client: ClientListItem): ClientOption {
    const addressParts: string[] = [];
    
    if (client.city) {
      addressParts.push(client.city);
    }
    if (client.street) {
      let streetPart = client.street;
      if (client.buildingNumber) {
        streetPart += ' ' + client.buildingNumber;
        if (client.apartmentNumber) {
          streetPart += '/' + client.apartmentNumber;
        }
      }
      addressParts.push(streetPart);
    }
    if (client.zipCode) {
      addressParts.push(client.zipCode);
    }

    return {
      id: client.id,
      displayName: client.displayName,
      address: addressParts.join(', '),
      clientType: client.clientType
    };
  }

  private filterClients(term: string): void {
    if (!term || term.length < 1) {
      this.filteredClients = this.allClients.slice(0, 50); // Show first 50 when empty
      return;
    }

    const lowerTerm = term.toLowerCase();
    this.filteredClients = this.allClients
      .filter(c => 
        c.displayName.toLowerCase().includes(lowerTerm) ||
        c.address.toLowerCase().includes(lowerTerm)
      )
      .slice(0, 50); // Limit results
    
    this.highlightedIndex = this.filteredClients.length > 0 ? 0 : -1;
  }

  @HostListener('document:click', ['$event'])
  onDocumentClick(event: MouseEvent): void {
    if (!this.elementRef.nativeElement.contains(event.target)) {
      this.closeDropdown();
    }
  }

  onInputFocus(): void {
    if (!this.disabled) {
      this.isOpen = true;
      this.filterClients(this.searchText);
    }
  }

  onInputChange(event: Event): void {
    const input = event.target as HTMLInputElement;
    this.searchText = input.value;
    this.searchSubject.next(this.searchText);
    
    // Clear selection if user modifies text
    if (this.selectedClient && this.searchText !== this.selectedClient.displayName) {
      this.selectedClient = null;
      this.value = null;
      this.onChange(null);
    }
    
    if (!this.isOpen) {
      this.isOpen = true;
    }
  }

  onKeyDown(event: KeyboardEvent): void {
    if (this.disabled) return;

    switch (event.key) {
      case 'ArrowDown':
        event.preventDefault();
        if (!this.isOpen) {
          this.isOpen = true;
          this.filterClients(this.searchText);
        } else if (this.highlightedIndex < this.filteredClients.length - 1) {
          this.highlightedIndex++;
          this.scrollToHighlighted();
        }
        break;
      case 'ArrowUp':
        event.preventDefault();
        if (this.highlightedIndex > 0) {
          this.highlightedIndex--;
          this.scrollToHighlighted();
        }
        break;
      case 'Enter':
        event.preventDefault();
        if (this.isOpen && this.highlightedIndex >= 0 && this.filteredClients[this.highlightedIndex]) {
          this.selectClient(this.filteredClients[this.highlightedIndex]);
        }
        break;
      case 'Escape':
        this.closeDropdown();
        break;
      case 'Tab':
        this.closeDropdown();
        break;
    }
  }

  selectClient(client: ClientOption): void {
    this.selectedClient = client;
    this.value = client.id;
    this.searchText = client.displayName;
    this.isOpen = false;
    this.onChange(this.value);
    this.onTouched();
    this.selectionChange.emit(this.value);
  }

  clearSelection(event: MouseEvent): void {
    event.stopPropagation();
    this.selectedClient = null;
    this.value = null;
    this.searchText = '';
    this.onChange(null);
    this.onTouched();
    this.selectionChange.emit(null);
  }

  private closeDropdown(): void {
    this.isOpen = false;
    this.highlightedIndex = -1;
    
    // Restore selected client name if user didn't make a valid selection
    if (this.selectedClient) {
      this.searchText = this.selectedClient.displayName;
    } else if (this.searchText && !this.selectedClient) {
      // User typed something but didn't select - clear it
      this.searchText = '';
    }
  }

  private scrollToHighlighted(): void {
    setTimeout(() => {
      const container = this.elementRef.nativeElement.querySelector('.autocomplete-dropdown');
      const highlighted = container?.querySelector('.autocomplete-item.highlighted');
      if (highlighted && container) {
        highlighted.scrollIntoView({ block: 'nearest', behavior: 'smooth' });
      }
    }, 0);
  }

  // ControlValueAccessor implementation
  writeValue(value: string | null): void {
    this.value = value;
    if (value && this.allClients.length > 0) {
      this.selectedClient = this.allClients.find(c => c.id === value) || null;
      this.searchText = this.selectedClient?.displayName || '';
    } else if (!value) {
      this.selectedClient = null;
      this.searchText = '';
    }
  }

  registerOnChange(fn: (value: string | null) => void): void {
    this.onChange = fn;
  }

  registerOnTouched(fn: () => void): void {
    this.onTouched = fn;
  }

  setDisabledState(isDisabled: boolean): void {
    this.disabled = isDisabled;
  }
}
