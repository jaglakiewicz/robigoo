/*
 * ROBIGOO FIELD SPRAYER CONTROL STATION
 * Copyright (c) 2025 Wojciech Salamon <wojciech.salamon@yahoo.com>
 * All rights reserved. Unauthorized distribution or disclosure is prohibited.
*/

import { Component, OnDestroy, OnInit } from '@angular/core';
import { DomSanitizer, SafeHtml, SafeResourceUrl } from '@angular/platform-browser';
import { Subscription } from 'rxjs';
import { ClientDetail, ClientListItem, ClientService, ClientCreateUpdateRequest } from '../client.service';
import { CropSprayerService, CropSprayerListItem } from '../crop-sprayer.service';
import { NavigationService } from '../services/navigation.service';
import { DirtyFormService } from '../shared/services/dirty-form.service';
import { TextService } from '../services/text.service';
import { NotificationService } from '../services/notification.service';
import { DataRefreshService } from '../services/data-refresh.service';
import { SVG_ICONS } from '../shared/svg-icons';
import { SelectOption } from '../shared/components/custom-select/custom-select.component';
import { FilterField, FilterValues } from '../shared/components/filter-panel/filter-panel.component';
import { Step } from '../shared/components/step-indicator/step-indicator.component';

interface ClientStep {
  id: number;
  key: string;
  titleKey: string;
}

@Component({
  selector: 'app-clients',
  templateUrl: './clients.component.html',
  styleUrls: ['./clients.component.css']
})
export class ClientsComponent implements OnInit, OnDestroy {
  // List
  clients: ClientListItem[] = [];
  selectedClientId: string | null = null;
  searchTerm = '';

  // Filter panel configuration
  filterFields: FilterField[] = [];
  filterValues: FilterValues = {};
  filterPanelOpen = false;

  // Detail / form
  currentClient: ClientDetail | null = null;
  formModel: ClientCreateUpdateRequest | null = null;
  isNew = false;
  isEditMode = false;
  private originalClientId: string | null = null;

  // Steps - 3 tabs: Basic data, Address, Crop Sprayers
  steps: ClientStep[] = [
    { id: 0, key: 'basics', titleKey: 'clients.steps.basics' },
    { id: 1, key: 'address', titleKey: 'clients.steps.address' },
    { id: 2, key: 'sprayers', titleKey: 'clients.steps.sprayers' }
  ];
  currentStep = 0;

  // Messages
  messageKey = '';
  messageError = false;
  loadingList = false;

  showHistoryDialog = false;
  showMapDialog = false;
  historyIcon = SVG_ICONS.historyIcon;
  mapIcon = SVG_ICONS.pinIcon;
  saving = false;
  deleting = false;

  // PESEL visibility toggle
  peselVisible = false;
  
  // Eye icons for PESEL toggle
  eyeIconOpen = `<svg viewBox="0 0 426.666667 341.333333" focusable="false" aria-hidden="true"><path fill="currentColor" d="M213.333333,1.42108547e-14 C64,1.42108547e-14 7.10542736e-15,170.666667 7.10542736e-15,170.666667 C7.10542736e-15,170.666667 64,341.333333 213.333333,341.333333 C362.666667,341.333333 426.666667,170.666667 426.666667,170.666667 C426.666667,170.666667 362.666667,1.42108547e-14 213.333333,1.42108547e-14 Z M213.333333,298.666667 C119.071573,298.666667 64.7370667,207.3632 46.7136,170.67328 C64.7850667,133.88928 119.114667,42.6666667 213.333333,42.6666667 C307.595093,42.6666667 361.9296,133.970133 379.954347,170.658773 C361.8816,207.444053 307.552,298.666667 213.333333,298.666667 Z M213.333333,96 C172.096427,96 138.666667,129.42976 138.666667,170.666667 C138.666667,211.903573 172.096427,245.333333 213.333333,245.333333 C254.57024,245.333333 288,211.903573 288,170.666667 C288,129.42976 254.57024,96 213.333333,96 Z M213.333333,202.666667 C195.688747,202.666667 181.333333,188.311253 181.333333,170.666667 C181.333333,153.02208 195.688747,138.666667 213.333333,138.666667 C230.97792,138.666667 245.333333,153.02208 245.333333,170.666667 C245.333333,188.311253 230.97792,202.666667 213.333333,202.666667 Z"></path></svg>`;
  eyeIconClosed = `<svg viewBox="0 0 426.666667 392.836561" focusable="false" aria-hidden="true"><path fill="currentColor" d="M47.0849493,2.84217094e-14 L185.740632,138.655563 C194.095501,134.657276 203.45297,132.418278 213.333333,132.418278 C248.679253,132.418278 277.333333,161.072358 277.333333,196.418278 C277.333333,206.299034 275.094157,215.656855 271.095572,224.011976 L409.751616,362.666662 L379.581717,392.836561 L320.374817,333.628896 C291.246618,353.329494 255.728838,367.084945 213.333333,367.084945 C64,367.084945 7.10542736e-15,196.418278 7.10542736e-15,196.418278 C7.10542736e-15,196.418278 22.862032,135.452859 73.1408088,86.3974274 L16.9150553,30.169894 L47.0849493,2.84217094e-14 Z M103.440016,116.694904 C74.7091717,144.512844 55.9626236,177.598744 46.7136,196.424891 C64.7370667,233.114811 119.071573,324.418278 213.333333,324.418278 C242.440012,324.418278 267.739844,315.712374 289.339919,302.595012 L240.926035,254.180993 C232.571166,258.17928 223.213696,260.418278 213.333333,260.418278 C177.987413,260.418278 149.333333,231.764198 149.333333,196.418278 C149.333333,186.537915 151.572331,177.180445 155.570618,168.825577 Z M213.333333,25.7516113 C362.666667,25.7516113 426.666667,196.418278 426.666667,196.418278 C426.666667,196.418278 412.428071,234.387867 381.712212,274.508373 L351.151213,243.941206 C364.581948,225.697449 374.142733,208.239347 379.954347,196.410385 C361.9296,159.721745 307.595093,68.418278 213.333333,68.418278 C201.495833,68.418278 190.287983,69.858232 179.702584,72.449263 L145.662385,38.4000762 C165.913597,30.494948 188.437631,25.7516113 213.333333,25.7516113 Z"></path></svg>`;

  toolbarIcons!: Record<'add' | 'edit' | 'delete' | 'save' | 'cancel', SafeHtml>;

  // Unsaved-changes dialog state
  pendingClientId: string | null = null;
  private pendingAction: 'select' | 'new' | null = null;
  unsavedDialogVisible = false;

  // Delete confirmation dialog state
  deleteDialogVisible = false;

  private readonly formId = 'clients-detail';

  // Select options
  clientTypeOptions: SelectOption[] = [];
  voivodeshipOptions: SelectOption[] = [];

  // Sprayers owned by current client
  ownerSprayers: CropSprayerListItem[] = [];
  loadingSprayers = false;

  // Subscription for crop sprayers data refresh
  private sprayersRefreshSub?: Subscription;

  constructor(
    private clientsService: ClientService,
    private cropSprayerService: CropSprayerService,
    private navigationService: NavigationService,
    private dirtyFormService: DirtyFormService,
    private textService: TextService,
    private notificationService: NotificationService,
    private dataRefreshService: DataRefreshService,
    private sanitizer: DomSanitizer
  ) {
    this.dirtyFormService.registerForm(this.formId);
    this.toolbarIcons = {
      add: this.getSafeHtml(SVG_ICONS.iconAdd),
      edit: this.getSafeHtml(SVG_ICONS.iconEdit),
      delete: this.getSafeHtml(SVG_ICONS.iconDelete),
      save: this.getSafeHtml(SVG_ICONS.iconCheck),
      cancel: this.getSafeHtml(SVG_ICONS.iconCancel)
    };
  }

  /** Convert steps to the format expected by step-indicator component */
  get stepsForIndicator(): Step[] {
    return this.steps.map(step => ({
      id: step.id,
      key: step.key,
      label: this.textService.get(step.titleKey),
      // Disable the Sprayers tab (id 2) when creating a new client
      disabled: step.id === 2 && this.isNew
    }));
  }

  ngOnInit(): void {
    this.initSelectOptions();
    this.initFilterFields();
    this.loadClients();
    
    // Subscribe to crop sprayers data changes to refresh owner sprayers list
    this.sprayersRefreshSub = this.dataRefreshService.onDataChanged('machines').subscribe(() => {
      if (this.currentClient && !this.isNew) {
        this.loadClientSprayers(this.currentClient.id);
      }
    });
  }

  private initFilterFields(): void {
    this.filterFields = [
      {
        key: 'clientType',
        label: this.textService.get('clients.fields.clientType'),
        type: 'select',
        placeholder: this.textService.get('clients.filters.allTypes'),
        options: [
          { value: 'person', label: this.textService.get('clients.filters.typePerson') },
          { value: 'company', label: this.textService.get('clients.filters.typeCompany') }
        ]
      },
      {
        key: 'city',
        label: this.textService.get('clients.fields.city'),
        type: 'text',
        placeholder: this.textService.get('clients.filters.city')
      }
    ];
  }

  private initSelectOptions(): void {
    this.clientTypeOptions = [
      { value: 'person', label: this.textService.get('clients.filters.typePerson') },
      { value: 'company', label: this.textService.get('clients.filters.typeCompany') }
    ];

    // All 16 Polish voivodeships
    this.voivodeshipOptions = [
      { value: 'dolnośląskie', label: 'Dolnośląskie' },
      { value: 'kujawsko-pomorskie', label: 'Kujawsko-pomorskie' },
      { value: 'lubelskie', label: 'Lubelskie' },
      { value: 'lubuskie', label: 'Lubuskie' },
      { value: 'łódzkie', label: 'Łódzkie' },
      { value: 'małopolskie', label: 'Małopolskie' },
      { value: 'mazowieckie', label: 'Mazowieckie' },
      { value: 'opolskie', label: 'Opolskie' },
      { value: 'podkarpackie', label: 'Podkarpackie' },
      { value: 'podlaskie', label: 'Podlaskie' },
      { value: 'pomorskie', label: 'Pomorskie' },
      { value: 'śląskie', label: 'Śląskie' },
      { value: 'świętokrzyskie', label: 'Świętokrzyskie' },
      { value: 'warmińsko-mazurskie', label: 'Warmińsko-mazurskie' },
      { value: 'wielkopolskie', label: 'Wielkopolskie' },
      { value: 'zachodniopomorskie', label: 'Zachodniopomorskie' }
    ];
  }

  ngOnDestroy(): void {
    this.dirtyFormService.unregisterForm(this.formId);
    this.sprayersRefreshSub?.unsubscribe();
  }

  // List handling

  loadClients(): void {
    this.loadingList = true;
    this.clientsService.getClientsList({
      q: this.searchTerm || undefined,
      clientType: this.filterValues['clientType'] || undefined,
      city: this.filterValues['city'] || undefined
    }).subscribe({
      next: list => {
        this.clients = list;
        this.loadingList = false;
        this.reconcileSelection(list);
      },
      error: () => {
        this.loadingList = false;
      }
    });
  }

  onSearchChange(value: string): void {
    this.searchTerm = value;
    this.loadClients();
  }

  onFilterChange(values: FilterValues): void {
    this.filterValues = values;
    this.loadClients();
  }

  onFilterClear(): void {
    this.filterValues = {};
    this.loadClients();
  }

  onFilterPanelOpenChange(isOpen: boolean): void {
    this.filterPanelOpen = isOpen;
  }

  onAddNew(): void {
    if (this.dirtyFormService.isDirty(this.formId)) {
      this.openUnsavedDialog('new', null);
      return;
    }
    this.startNewClient();
  }

  onSelectClient(clientId: string): void {
    if (clientId === this.selectedClientId && !this.isNew) {
      return;
    }

    if (this.dirtyFormService.isDirty(this.formId)) {
      this.openUnsavedDialog('select', clientId);
      return;
    }

    this.loadClientDetail(clientId);
  }

  onEdit(): void {
    if (!this.currentClient || this.isEditMode) {
      return;
    }
    this.isEditMode = true;
    this.setDirty(false);
  }

  openHistory(): void {
    if (this.currentClient && !this.isNew) {
      this.showHistoryDialog = true;
    }
  }

  openMapDialog(): void {
    if (this.currentClient && !this.isNew) {
      this.showMapDialog = true;
    }
  }

  closeMapDialog(): void {
    this.showMapDialog = false;
  }

  togglePeselVisibility(): void {
    this.peselVisible = !this.peselVisible;
  }

  onMapBackdropClick(event: MouseEvent): void {
    if (event.target === event.currentTarget) {
      this.closeMapDialog();
    }
  }

  private startNewClient(): void {
    this.isNew = true;
    this.isEditMode = true;
    this.originalClientId = null;
    this.selectedClientId = null;
    this.currentClient = null;
    this.currentStep = 0;
    this.messageKey = '';
    this.messageError = false;
    this.formModel = {
      clientType: 'person',
      firstName: '',
      lastName: '',
      pesel: '',
      companyName: '',
      nip: '',
      regon: '',
      voivodeship: '',
      city: '',
      street: '',
      buildingNumber: '',
      apartmentNumber: '',
      zipCode: ''
    };
    this.setDirty(false);
  }

  private loadClientDetail(clientId: string): void {
    this.isNew = false;
    this.isEditMode = false;
    this.selectedClientId = clientId;
    this.currentStep = 0;
    this.messageKey = '';
    this.messageError = false;
    this.ownerSprayers = [];
    this.clientsService.getClient(clientId).subscribe({
      next: detail => {
        this.currentClient = detail;
        this.originalClientId = detail.id;
        this.formModel = {
          clientType: detail.clientType,
          firstName: detail.firstName,
          lastName: detail.lastName,
          pesel: detail.pesel,
          companyName: detail.companyName,
          nip: detail.nip,
          regon: detail.regon,
          voivodeship: detail.voivodeship,
          city: detail.city,
          street: detail.street,
          buildingNumber: detail.buildingNumber,
          apartmentNumber: detail.apartmentNumber,
          zipCode: detail.zipCode
        };
        this.setDirty(false);
        // Load sprayers owned by this client
        this.loadClientSprayers(clientId);
      },
      error: () => {
        this.messageKey = 'clients.messages.error';
        this.messageError = true;
      }
    });
  }

  /** Load sprayers owned by the current client */
  private loadClientSprayers(clientId: string): void {
    this.loadingSprayers = true;
    this.cropSprayerService.getByOwner(clientId).subscribe({
      next: sprayers => {
        this.ownerSprayers = sprayers;
        this.loadingSprayers = false;
      },
      error: () => {
        this.ownerSprayers = [];
        this.loadingSprayers = false;
      }
    });
  }

  /** Navigate to crop sprayers module and select the given sprayer */
  navigateToSprayer(serialNumber: string): void {
    this.navigationService.navigateTo('types', 'menu.types', { select: serialNumber });
  }

  private reconcileSelection(list: ClientListItem[]): void {
    if (list.length === 0) {
      if (!this.isNew) {
        this.selectedClientId = null;
        this.currentClient = null;
        this.formModel = null;
      }
      return;
    }

    const hasSelected = !!this.selectedClientId && list.some(c => c.id === this.selectedClientId);

    if (hasSelected) {
      if (!this.formModel || this.currentClient?.id !== this.selectedClientId) {
        this.loadClientDetail(this.selectedClientId!);
      }
      return;
    }

    if (this.isEditMode || this.isNew || this.dirtyFormService.isDirty(this.formId)) {
      return;
    }

    const firstClientId = list[0].id;
    this.loadClientDetail(firstClientId);
  }

  // Step navigation

  goToStep(index: number): void {
    const step = this.steps.find(s => s.id === index);
    if (step) {
      this.currentStep = step.id;
    }
  }

  previousStep(): void {
    const currentIndex = this.steps.findIndex(s => s.id === this.currentStep);
    if (currentIndex > 0) {
      this.currentStep = this.steps[currentIndex - 1].id;
    }
  }

  nextStep(): void {
    const currentIndex = this.steps.findIndex(s => s.id === this.currentStep);
    if (currentIndex >= 0 && currentIndex < this.steps.length - 1) {
      this.currentStep = this.steps[currentIndex + 1].id;
    }
  }

  // Form helpers

  onFormChange(): void {
    if (!this.isEditMode) {
      return;
    }
    this.setDirty(true);
  }

  onClientTypeChange(type: string): void {
    if (!this.formModel) { return; }
    this.formModel.clientType = type as 'person' | 'company';
    this.onFormChange();
  }

  get displayName(): string {
    if (!this.formModel) { return ''; }
    if (this.formModel.clientType === 'person') {
      const name = `${this.formModel.firstName} ${this.formModel.lastName}`.trim();
      return name || this.textService.get('clients.fields.newClient');
    }
    return this.formModel.companyName || this.textService.get('clients.fields.newClient');
  }

  get displayAddress(): string {
    if (!this.formModel) { return ''; }
    const parts: string[] = [];
    if (this.formModel.city) {
      parts.push(this.formModel.city);
    }
    if (this.formModel.street) {
      let streetPart = this.formModel.street;
      if (this.formModel.buildingNumber) {
        streetPart += ' ' + this.formModel.buildingNumber;
        if (this.formModel.apartmentNumber) {
          streetPart += '/' + this.formModel.apartmentNumber;
        }
      }
      parts.push(streetPart);
    }
    return parts.join(', ');
  }

  /** Check if we have enough address data to show map */
  get hasValidAddress(): boolean {
    if (!this.formModel) { return false; }
    // Need at least city and street with building number
    return !!(this.formModel.city && this.formModel.street && this.formModel.buildingNumber);
  }

  /** Build Google Maps embed URL from address */
  get mapUrl(): SafeResourceUrl {
    if (!this.formModel) {
      return this.sanitizer.bypassSecurityTrustResourceUrl('');
    }
    
    const addressParts: string[] = [];
    
    if (this.formModel.street && this.formModel.buildingNumber) {
      addressParts.push(`${this.formModel.street} ${this.formModel.buildingNumber}`);
    }
    if (this.formModel.city) {
      addressParts.push(this.formModel.city);
    }
    if (this.formModel.zipCode) {
      addressParts.push(this.formModel.zipCode);
    }
    addressParts.push('Poland');
    
    const address = encodeURIComponent(addressParts.join(', '));
    const url = `https://maps.google.com/maps?q=${address}&t=&z=15&ie=UTF8&iwloc=&output=embed`;
    
    return this.sanitizer.bypassSecurityTrustResourceUrl(url);
  }

  save(): void {
    this.doSave();
  }

  private doSave(onSuccess?: () => void): void {
    if (!this.formModel || this.saving) {
      return;
    }

    // Validate required address fields
    const requiredAddressFields: { field: keyof ClientCreateUpdateRequest; label: string }[] = [
      { field: 'voivodeship', label: this.textService.get('clients.fields.voivodeship') },
      { field: 'city', label: this.textService.get('clients.fields.city') },
      { field: 'zipCode', label: this.textService.get('clients.fields.zipCode') },
      { field: 'buildingNumber', label: this.textService.get('clients.fields.buildingNumber') }
    ];

    const missingFields = requiredAddressFields.filter(f => !this.formModel![f.field]?.trim());
    if (missingFields.length > 0) {
      const fieldNames = missingFields.map(f => f.label).join(', ');
      this.notificationService.error(
        this.textService.get('clients.messages.requiredAddressFields') + ': ' + fieldNames
      );
      // Switch to address tab to show the user what's missing
      this.currentStep = 1;
      return;
    }

    this.saving = true;
    this.messageKey = '';
    this.messageError = false;

    const req: ClientCreateUpdateRequest = { ...this.formModel };
    const obs = this.isNew
      ? this.clientsService.createClient(req)
      : this.clientsService.updateClient(this.originalClientId ?? '', req);

    obs.subscribe({
      next: detail => {
        const wasNew = this.isNew;
        this.currentClient = detail;
        this.isNew = false;
        this.isEditMode = false;
        this.originalClientId = detail.id;
        this.selectedClientId = detail.id;
        this.messageKey = '';
        this.messageError = false;
        this.saving = false;
        this.setDirty(false);
        this.loadClients();
        this.notificationService.success(this.textService.get('clients.messages.saved'));
        // Notify other components that clients data has changed
        this.dataRefreshService.notifyClientsChanged(wasNew ? 'create' : 'update', detail.id);
        if (onSuccess) {
          onSuccess();
        }
      },
      error: err => {
        this.messageKey = '';
        this.messageError = false;
        this.saving = false;
        const backendMessage = err?.error?.message as string | undefined;
        const message = backendMessage || this.textService.get('clients.messages.error');
        this.notificationService.error(message);
      }
    });
  }

  cancelEditing(): void {
    if (this.isNew) {
      this.formModel = null;
      this.currentClient = null;
      this.isNew = false;
      this.isEditMode = false;
      this.setDirty(false);
    }
    else if (this.currentClient) {
      // Restore last state from API
      this.loadClientDetail(this.currentClient.id);
    }
  }

  onDelete(): void {
    if (!this.currentClient || this.isNew || this.deleting) {
      return;
    }

    this.deleteDialogVisible = true;
  }

  onDeleteConfirm(): void {
    this.deleteDialogVisible = false;
    
    if (!this.currentClient || this.deleting) {
      return;
    }

    this.deleting = true;
    const idToDelete = this.currentClient.id;

    this.clientsService.deleteClient(idToDelete).subscribe({
      next: () => {
        this.notificationService.success(this.textService.get('clients.messages.deleted'));
        this.deleting = false;
        this.isEditMode = false;
        this.isNew = false;
        this.currentClient = null;
        this.formModel = null;
        this.selectedClientId = null;
        this.messageKey = '';
        this.messageError = false;
        this.setDirty(false);
        this.loadClients();
        // Notify other components that clients data has changed
        this.dataRefreshService.notifyClientsChanged('delete', idToDelete);
      },
      error: err => {
        this.deleting = false;
        const backendMessage = err?.error?.message as string | undefined;
        const message = backendMessage || this.textService.get('clients.messages.error');
        this.notificationService.error(message);
      }
    });
  }

  onDeleteCancel(): void {
    this.deleteDialogVisible = false;
  }

  // Unsaved-changes logic
  private openUnsavedDialog(action: 'select' | 'new', targetId: string | null): void {
    this.pendingAction = action;
    this.pendingClientId = targetId;
    this.unsavedDialogVisible = true;
  }

  onUnsavedConfirmSave(): void {
    this.doSave(() => {
      this.unsavedDialogVisible = false;
      this.setDirty(false);
      this.runPendingAction();
    });
  }

  onUnsavedConfirmDiscard(): void {
    this.setDirty(false);
    this.cancelEditing();
    this.unsavedDialogVisible = false;
    this.runPendingAction();
  }

  onUnsavedConfirmCancel(): void {
    this.unsavedDialogVisible = false;
    this.pendingAction = null;
    this.pendingClientId = null;
  }

  private runPendingAction(): void {
    if (this.pendingAction === 'new') {
      this.startNewClient();
    } else if (this.pendingAction === 'select' && this.pendingClientId) {
      this.loadClientDetail(this.pendingClientId);
    }

    this.pendingAction = null;
    this.pendingClientId = null;
  }

  private setDirty(dirty: boolean): void {
    this.dirtyFormService.setDirty(this.formId, dirty);
  }

  getSafeHtml(icon: string): SafeHtml {
    return this.sanitizer.bypassSecurityTrustHtml(icon);
  }
}

