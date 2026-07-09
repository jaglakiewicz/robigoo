/*
 * ROBIGOO FIELD SPRAYER CONTROL STATION
 * Copyright (c) 2025 Wojciech Salamon <wojciech.salamon@yahoo.com>
 * All rights reserved. Unauthorized distribution or disclosure is prohibited.
*/

import { Component, OnDestroy, OnInit, ViewChild } from '@angular/core';
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
import { FilterField, FilterValues, FilterPanelComponent } from '../shared/components/filter-panel/filter-panel.component';
import { Step } from '../shared/components/step-indicator/step-indicator.component';
import { EntityDetailPanelComponent } from '../shared/components/entity-detail-panel/entity-detail-panel.component';

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
  private allClients: ClientListItem[] = [];
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
  peselFocused = false;
  
  // Lucide eye icons for PESEL toggle
  eyeIconOpen = SVG_ICONS.iconEye;
  eyeIconClosed = SVG_ICONS.iconEyeOff;

  @ViewChild('detailPanel') detailPanel?: EntityDetailPanelComponent;

  toolbarIcons!: Record<'add' | 'edit' | 'delete' | 'save' | 'cancel' | 'preview', SafeHtml>;

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
      delete: this.getSafeHtml(SVG_ICONS.deleteIcon),
      save: this.getSafeHtml(SVG_ICONS.iconSave),
      cancel: this.getSafeHtml(SVG_ICONS.iconCancel),
      preview: this.getSafeHtml(SVG_ICONS.iconEye)
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
        options: [
          { value: 'person', label: this.textService.get('clients.filters.typePerson') },
          { value: 'company', label: this.textService.get('clients.filters.typeCompany') }
        ]
      },
      {
        key: 'displayName',
        label: this.textService.get('clients.fields.displayName'),
        type: 'text'
      },
      {
        key: 'firstName',
        label: this.textService.get('clients.fields.firstName'),
        type: 'text'
      },
      {
        key: 'lastName',
        label: this.textService.get('clients.fields.lastName'),
        type: 'text'
      },
      {
        key: 'companyName',
        label: this.textService.get('clients.fields.companyName'),
        type: 'text'
      },
      {
        key: 'nip',
        label: this.textService.get('clients.fields.nip'),
        type: 'text'
      },
      {
        key: 'regon',
        label: this.textService.get('clients.fields.regon'),
        type: 'text'
      },
      {
        key: 'voivodeship',
        label: this.textService.get('clients.fields.voivodeship'),
        type: 'select',
        options: this.voivodeshipOptions.map(v => ({ value: v.value, label: v.label }))
      },
      {
        key: 'city',
        label: this.textService.get('clients.fields.city'),
        type: 'text'
      },
      {
        key: 'street',
        label: this.textService.get('clients.fields.street'),
        type: 'text'
      },
      {
        key: 'zipCode',
        label: this.textService.get('clients.fields.zipCode'),
        type: 'text'
      },
      {
        key: 'post',
        label: this.textService.get('clients.fields.post'),
        type: 'text'
      },
      {
        key: 'createdAt',
        label: this.textService.get('clients.fields.createdAt'),
        type: 'date'
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
      q: this.searchTerm || undefined
    }).subscribe({
      next: list => {
        this.allClients = list;
        this.clients = FilterPanelComponent.applyFilters(list, this.filterValues, this.filterFields);
        this.loadingList = false;
        this.reconcileSelection(this.clients);
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
    this.clients = FilterPanelComponent.applyFilters(this.allClients, this.filterValues, this.filterFields);
    this.reconcileSelection(this.clients);
  }

  onFilterClear(): void {
    this.filterValues = {};
    this.clients = [...this.allClients];
    this.reconcileSelection(this.clients);
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

  onMobileBack(): void {
    this.currentClient = null;
    this.formModel = null;
    this.isNew = false;
    this.isEditMode = false;
    this.selectedClientId = null;
    this.setDirty(false);
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

  onPeselFocus(): void {
    this.peselFocused = true;
    this.peselVisible = true;
  }

  onPeselBlur(): void {
    this.peselFocused = false;
    this.peselVisible = false;
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
      zipCode: '',
      post: ''
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
          zipCode: detail.zipCode,
          post: detail.post
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
    this.currentStep = index;
    this.detailPanel?.scrollToSection(`section-${index}`);
  }

  onScrollSection(id: string): void {
    const index = parseInt(id.replace('section-', ''), 10);
    if (!isNaN(index)) this.currentStep = index;
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

