/*
 * ROBIGOO FIELD SPRAYER CONTROL STATION
 * Copyright (c) 2025 Wojciech Salamon <wojciech.salamon@yahoo.com>
 * All rights reserved. Unauthorized distribution or disclosure is prohibited.
*/

import { Component, ElementRef, OnDestroy, OnInit, ViewChild } from '@angular/core';
import { DomSanitizer, SafeHtml } from '@angular/platform-browser';
import { Subscription } from 'rxjs';
import { CropSprayerDetail, CropSprayerListItem, CropSprayerService, CropSprayerCreateUpdateRequest } from '../crop-sprayer.service';
import { ClientService, ClientListItem } from '../client.service';
import { NavigationService } from '../services/navigation.service';
import { DirtyFormService } from '../shared/services/dirty-form.service';
import { TextService } from '../services/text.service';
import { NotificationService } from '../services/notification.service';
import { DataRefreshService } from '../services/data-refresh.service';
import { SVG_ICONS } from '../shared/svg-icons';
import { SelectOption } from '../shared/components/custom-select/custom-select.component';
import { FilterField, FilterValues, FilterPanelComponent } from '../shared/components/filter-panel/filter-panel.component';
import { Step } from '../shared/components/step-indicator/step-indicator.component';

interface SprayerStep {
  id: number;
  key: string;
  titleKey: string;
}

@Component({
  selector: 'app-crop-sprayers',
  templateUrl: './crop-sprayers.component.html',
  styleUrls: ['./crop-sprayers.component.css']
})
export class CropSprayersComponent implements OnInit, OnDestroy {
  @ViewChild('detailScroll') detailScroll?: ElementRef<HTMLElement>;
  @ViewChild('formSections') formSections?: ElementRef<HTMLElement>;

  readonly currentYear = new Date().getFullYear();

  // List
  sprayers: CropSprayerListItem[] = [];
  private allSprayers: CropSprayerListItem[] = [];
  selectedSerialNumber: string | null = null;
  searchTerm = '';

  // Filter panel configuration
  filterFields: FilterField[] = [];
  filterValues: FilterValues = {};
  filterPanelOpen = false;

  // Detail / form
  currentSprayer: CropSprayerDetail | null = null;
  formModel: CropSprayerCreateUpdateRequest | null = null;
  isNew = false;
  isEditMode = false;
  private originalSerialNumber: string | null = null;

  // Steps
  steps: SprayerStep[] = [
    { id: 0, key: 'basics', titleKey: 'types.cropSprayers.steps.basics' },
    { id: 1, key: 'pump', titleKey: 'types.cropSprayers.steps.pump' },
    { id: 2, key: 'tank', titleKey: 'types.cropSprayers.steps.tank' },
    { id: 3, key: 'control', titleKey: 'types.cropSprayers.steps.control' },
    { id: 4, key: 'boom', titleKey: 'types.cropSprayers.steps.boom' },
    { id: 5, key: 'sections', titleKey: 'types.cropSprayers.steps.sections' },
    { id: 6, key: 'fieldNozzles', titleKey: 'types.cropSprayers.steps.fieldNozzles' },
    { id: 7, key: 'gardenNozzles', titleKey: 'types.cropSprayers.steps.gardenNozzles' },
    { id: 8, key: 'fan', titleKey: 'types.cropSprayers.steps.fan' }
  ];
  currentStep = 0;

  // Messages
  messageKey = '';
  messageError = false;
  loadingList = false;

  showHistoryDialog = false;
  historyIcon = SVG_ICONS.historyIcon;
  saving = false;
  deleting = false;

  toolbarIcons!: Record<'add' | 'edit' | 'delete' | 'save' | 'cancel' | 'preview', SafeHtml>;

  // Unsaved-changes dialog state
  pendingSerialNumber: string | null = null;
  private pendingAction: 'select' | 'new' | null = null;
  unsavedDialogVisible = false;

  // Delete confirmation dialog state
  deleteDialogVisible = false;

  // Navigation from clients module
  private pendingSelectSerial: string | null = null;

  private readonly formId = 'sprayers-detail';

  // Select options
  typeOptions: SelectOption[] = [];
  kindOptions: SelectOption[] = [];
  pumpTypeOptions: SelectOption[] = [];
  ownerOptions: SelectOption[] = [];

  // Autosuggestions
  manufacturerSuggestions: string[] = [];
  sprayerNameSuggestions: string[] = [];

  // Subscription for clients data refresh
  private clientsRefreshSub?: Subscription;

  constructor(
    private sprayersService: CropSprayerService,
    private clientService: ClientService,
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

  sanitizeYearInput(value: string | number | null | undefined): string {
    if (value === null || value === undefined) {
      return '';
    }

    const raw = String(value);
    const digits = raw.replace(/\D+/g, '').slice(0, 4);

    // Allow partial typing (e.g. '1', '19', '20')
    if (digits.length < 4) {
      return digits;
    }

    const year = Number(digits);
    if (!Number.isFinite(year)) {
      return '';
    }

    const clamped = Math.min(Math.max(year, 1900), this.currentYear);
    return String(clamped);
  }

  // Visible steps depend on sprayer type (field / garden)
  get visibleSteps(): SprayerStep[] {
    const type = this.formModel?.type;
    return this.steps.filter(step => {
      if (step.key === 'fieldNozzles') {
        // Polowy
        return !type || type === '00';
      }
      if (step.key === 'gardenNozzles') {
        // Sadowniczy
        return !type || type === '01';
      }
      return true;
    });
  }

  /** Convert visible steps to the format expected by step-indicator component */
  get stepsForIndicator(): Step[] {
    return this.visibleSteps.map(step => ({
      id: step.id,
      key: step.key,
      label: this.textService.get(this.getStepTitleKey(step))
    }));
  }

  getStepTitleKey(step: SprayerStep): string {
    if (step.key === 'fieldNozzles' || step.key === 'gardenNozzles') {
      return 'types.cropSprayers.steps.nozzles';
    }
    return step.titleKey;
  }

  ngOnInit(): void {
    this.initSelectOptions();
    this.initFilterFields();
    this.loadOwnerOptions();
    this.loadSuggestions();

    // Check for pending navigation params from clients module
    const navParams = this.navigationService.getPendingParams();
    if (navParams?.['select']) {
      this.pendingSelectSerial = navParams['select'];
    }

    this.loadSprayers();
    
    // Subscribe to clients data changes to refresh owner options
    this.clientsRefreshSub = this.dataRefreshService.onDataChanged('clients').subscribe(() => {
      this.loadOwnerOptions();
    });
  }

  private initFilterFields(): void {
    this.filterFields = [
      {
        key: 'type',
        label: this.textService.get('types.cropSprayers.fields.type'),
        type: 'select',
        options: [
          { value: '00', label: this.textService.get('types.cropSprayers.filters.typeField') },
          { value: '01', label: this.textService.get('types.cropSprayers.filters.typeGarden') }
        ]
      },
      {
        key: 'kind',
        label: this.textService.get('types.cropSprayers.fields.kind'),
        type: 'select',
        options: [
          { value: '00', label: this.textService.get('types.cropSprayers.filters.kindMounted') },
          { value: '01', label: this.textService.get('types.cropSprayers.filters.kindTrailed') },
          { value: '02', label: this.textService.get('types.cropSprayers.filters.kindSelfPropelled') },
          { value: '03', label: this.textService.get('types.cropSprayers.filters.kindOther') }
        ]
      },
      {
        key: 'serialNumber',
        label: this.textService.get('types.cropSprayers.fields.serialNumber'),
        type: 'text'
      },
      {
        key: 'sprayerName',
        label: this.textService.get('types.cropSprayers.fields.sprayerName'),
        type: 'text'
      },
      {
        key: 'manufacturer',
        label: this.textService.get('types.cropSprayers.fields.manufacturer'),
        type: 'text'
      },
      {
        key: 'productionYear',
        label: this.textService.get('types.cropSprayers.fields.productionYear'),
        type: 'number',
        min: 1900,
        max: this.currentYear
      },
      {
        key: 'ownerName',
        label: this.textService.get('types.cropSprayers.fields.owner'),
        type: 'text'
      },
      {
        key: 'createdAt',
        label: this.textService.get('types.cropSprayers.fields.createdAt'),
        type: 'date'
      }
    ];
  }

  private initSelectOptions(): void {
    this.typeOptions = [
      { value: '00', label: this.textService.get('types.cropSprayers.filters.typeField') },
      { value: '01', label: this.textService.get('types.cropSprayers.filters.typeGarden') }
    ];
    this.kindOptions = [
      { value: '00', label: this.textService.get('types.cropSprayers.filters.kindMounted') },
      { value: '01', label: this.textService.get('types.cropSprayers.filters.kindTrailed') },
      { value: '02', label: this.textService.get('types.cropSprayers.filters.kindSelfPropelled') },
      { value: '03', label: this.textService.get('types.cropSprayers.filters.kindOther') }
    ];
    this.pumpTypeOptions = [
      { value: 'piston', label: this.textService.get('types.cropSprayers.fields.pumpPiston') },
      { value: 'diaphragm', label: this.textService.get('types.cropSprayers.fields.pumpDiaphragm') },
      { value: 'other', label: this.textService.get('types.cropSprayers.fields.pumpOther') }
    ];
  }

  /** Load clients for owner dropdown */
  private loadOwnerOptions(): void {
    this.clientService.getClientsList().subscribe({
      next: (clients: ClientListItem[]) => {
        this.ownerOptions = [
          { value: '', label: this.textService.get('types.cropSprayers.fields.noOwner') },
          ...clients.map(c => ({
            value: c.id,
            label: c.displayName
          }))
        ];
      }
    });
  }

  /** Load unique field values for autosuggestions */
  private loadSuggestions(): void {
    this.sprayersService.getSuggestions('manufacturer').subscribe({
      next: (values) => this.manufacturerSuggestions = values
    });
    this.sprayersService.getSuggestions('sprayerName').subscribe({
      next: (values) => this.sprayerNameSuggestions = values
    });
  }

  ngOnDestroy(): void {
    this.dirtyFormService.unregisterForm(this.formId);
    this.clientsRefreshSub?.unsubscribe();
  }

  // List handling

  loadSprayers(): void {
    this.loadingList = true;
    this.sprayersService.getList({
      q: this.searchTerm || undefined
    }).subscribe({
      next: list => {
        this.allSprayers = list;
        this.sprayers = FilterPanelComponent.applyFilters(list, this.filterValues, this.filterFields);
        this.loadingList = false;

        // Handle pending selection from navigation
        if (this.pendingSelectSerial) {
          this.selectSprayerBySerialNumber(this.pendingSelectSerial);
          this.pendingSelectSerial = null;
        } else {
          this.reconcileSelection(this.sprayers);
        }
      },
      error: () => {
        this.loadingList = false;
      }
    });
  }

  /** Select a sprayer by its serial number (used for navigation from clients) */
  private selectSprayerBySerialNumber(serialNumber: string): void {
    const sprayer = this.sprayers.find(s => s.serialNumber === serialNumber);
    if (sprayer) {
      this.loadSprayerDetail(serialNumber);
    }
  }

  onSearchChange(value: string): void {
    this.searchTerm = value;
    this.loadSprayers();
  }

  onFilterChange(values: FilterValues): void {
    this.filterValues = values;
    this.sprayers = FilterPanelComponent.applyFilters(this.allSprayers, this.filterValues, this.filterFields);
    this.reconcileSelection(this.sprayers);
  }

  onFilterClear(): void {
    this.filterValues = {};
    this.sprayers = [...this.allSprayers];
    this.reconcileSelection(this.sprayers);
  }

  onFilterPanelOpenChange(isOpen: boolean): void {
    this.filterPanelOpen = isOpen;
  }

  onAddNew(): void {
    if (this.dirtyFormService.isDirty(this.formId)) {
      this.openUnsavedDialog('new', null);
      return;
    }
    this.startNewSprayer();
  }

  onSelectSprayer(serialNumber: string): void {
    if (serialNumber === this.selectedSerialNumber && !this.isNew) {
      return;
    }

    if (this.dirtyFormService.isDirty(this.formId)) {
      this.openUnsavedDialog('select', serialNumber);
      return;
    }

    this.loadSprayerDetail(serialNumber);
  }

  onEdit(): void {
    if (!this.currentSprayer || this.isEditMode) {
      return;
    }
    this.isEditMode = true;
    this.setDirty(false);
  }

  openHistory(): void {
    if (this.currentSprayer && !this.isNew) {
      this.showHistoryDialog = true;
    }
  }

  private startNewSprayer(): void {
    this.isNew = true;
    this.isEditMode = true;
    this.originalSerialNumber = null;
    this.selectedSerialNumber = null;
    this.currentSprayer = null;
    this.currentStep = 0;
    this.messageKey = '';
    this.messageError = false;
    this.formModel = {
      serialNumber: '',
      sprayerName: '',
      type: '00',
      kind: '00',
      manufacturer: '',
      productionYear: '',
      purchaseDate: null,
      ownerId: null,
      ownerName: null,
      pumpPiston: true,
      pumpDiaphragm: false,
      pumpOther: false,
      pumpOtherType: null,
      pumpFlowRate: null,
      tankCapacity: null,
      hasFlushing: false,
      hasDiluter: false,
      hasWashingDevice: false,
      hasManometer: false,
      hasComputer: false,
      boomWidth: null,
      boomWet: false,
      boomDry: false,
      boomDampeningMechanism: false,
      sectionCount: null,
      nozzlesFieldFeatures: null,
      nozzlesGardenFeatures: null,
      fanType: null
    };
    this.setDirty(false);
  }

  private loadSprayerDetail(serialNumber: string): void {
    this.isNew = false;
    this.isEditMode = false;
    this.selectedSerialNumber = serialNumber;
    this.currentStep = 0;
    this.messageKey = '';
    this.messageError = false;
    this.sprayersService.get(serialNumber).subscribe({
      next: detail => {
        this.currentSprayer = detail;
        this.originalSerialNumber = detail.serialNumber;
        this.formModel = {
          serialNumber: detail.serialNumber,
          sprayerName: detail.sprayerName,
          type: detail.type,
          kind: detail.kind,
          manufacturer: detail.manufacturer,
          productionYear: detail.productionYear,
          purchaseDate: detail.purchaseDate || null,
          ownerId: detail.ownerId || null,
          ownerName: detail.ownerName || null,
          pumpPiston: detail.pumpPiston,
          pumpDiaphragm: detail.pumpDiaphragm,
          pumpOther: detail.pumpOther,
          pumpOtherType: detail.pumpOtherType || null,
          pumpFlowRate: detail.pumpFlowRate ?? null,
          tankCapacity: detail.tankCapacity ?? null,
          hasFlushing: detail.hasFlushing,
          hasDiluter: detail.hasDiluter,
          hasWashingDevice: detail.hasWashingDevice,
          hasManometer: detail.hasManometer,
          hasComputer: detail.hasComputer,
          boomWidth: detail.boomWidth ?? null,
          boomWet: detail.boomWet,
          boomDry: detail.boomDry,
          boomDampeningMechanism: detail.boomDampeningMechanism,
          sectionCount: detail.sectionCount ?? null,
          nozzlesFieldFeatures: detail.nozzlesFieldFeatures || null,
          nozzlesGardenFeatures: detail.nozzlesGardenFeatures || null,
          fanType: detail.fanType || null
        };
        this.setDirty(false);
      },
      error: () => {
        this.messageKey = 'types.cropSprayers.messages.error';
        this.messageError = true;
      }
    });
  }

  private reconcileSelection(list: CropSprayerListItem[]): void {
    if (list.length === 0) {
      if (!this.isNew) {
        this.selectedSerialNumber = null;
        this.currentSprayer = null;
        this.formModel = null;
      }
      return;
    }

    const hasSelected = !!this.selectedSerialNumber && list.some(m => m.serialNumber === this.selectedSerialNumber);

    if (hasSelected) {
      if (!this.formModel || this.formModel.serialNumber !== this.selectedSerialNumber) {
        this.loadSprayerDetail(this.selectedSerialNumber!);
      }
      return;
    }

    if (this.isEditMode || this.isNew || this.dirtyFormService.isDirty(this.formId)) {
      return;
    }

    const firstSerialNumber = list[0].serialNumber;
    this.loadSprayerDetail(firstSerialNumber);
  }

  onMobileBack(): void {
    if (this.isEditMode || this.dirtyFormService.isDirty(this.formId)) {
      return;
    }
    this.currentSprayer = null;
    this.formModel = null;
    this.isNew = false;
    this.isEditMode = false;
    this.selectedSerialNumber = null;
    this.setDirty(false);
  }

  // Step navigation

  goToStep(index: number): void {
    this.currentStep = index;
    const el = this.formSections?.nativeElement?.querySelector(`#section-${index}`);
    if (el) el.scrollIntoView({ behavior: 'smooth', block: 'start' });
  }

  onScrollSection(id: string): void {
    const index = parseInt(id.replace('section-', ''), 10);
    if (!isNaN(index)) this.currentStep = index;
  }

  previousStep(): void {
    const visible = this.visibleSteps;
    const currentIndex = visible.findIndex(s => s.id === this.currentStep);
    if (currentIndex > 0) {
      this.currentStep = visible[currentIndex - 1].id;
    }
  }

  nextStep(): void {
    const visible = this.visibleSteps;
    const currentIndex = visible.findIndex(s => s.id === this.currentStep);
    if (currentIndex >= 0 && currentIndex < visible.length - 1) {
      this.currentStep = visible[currentIndex + 1].id;
    }
  }

  // Form helpers

  onFormChange(): void {
    if (!this.isEditMode) {
      return;
    }
    this.setDirty(true);
  }

  get selectedPumpType(): string {
    if (!this.formModel) { return 'piston'; }
    if (this.formModel.pumpDiaphragm) { return 'diaphragm'; }
    if (this.formModel.pumpOther) { return 'other'; }
    return 'piston';
  }

  onPumpTypeChange(type: string): void {
    if (!this.formModel) { return; }
    // Ekskluzywny wybór jednego typu pompy
    this.formModel.pumpPiston = type === 'piston';
    this.formModel.pumpDiaphragm = type === 'diaphragm';
    this.formModel.pumpOther = type === 'other';
    if (!this.formModel.pumpOther) {
      this.formModel.pumpOtherType = null;
    }
    this.onFormChange();
  }

  onTypeChange(value: string): void {
    if (!this.formModel) { return; }
    this.formModel.type = value;
    this.onFormChange();

    // Ensure current step is still visible after type change
    const visible = this.visibleSteps;
    if (!visible.some(s => s.id === this.currentStep)) {
      this.currentStep = visible.length ? visible[0].id : 0;
    }
  }

  onOwnerChange(ownerId: string | null): void {
    if (!this.formModel) { return; }
    this.formModel.ownerId = ownerId || null;
    
    // Owner name will be looked up by the backend or can be loaded separately
    if (!ownerId) {
      this.formModel.ownerName = null;
    }
    
    this.onFormChange();
  }

  save(): void {
    this.doSave();
  }

  private doSave(onSuccess?: () => void): void {
    if (!this.formModel || this.saving) {
      return;
    }

    this.saving = true;
    this.messageKey = '';
    this.messageError = false;

    const req: CropSprayerCreateUpdateRequest = { ...this.formModel };
    
    const obs = this.isNew
      ? this.sprayersService.createSprayer(req)
      : this.sprayersService.updateSprayer(this.originalSerialNumber ?? this.formModel.serialNumber, req);

    obs.subscribe({
      next: detail => {
        this.currentSprayer = detail;
        this.isNew = false;
        this.isEditMode = false;
        this.originalSerialNumber = detail.serialNumber;
        this.selectedSerialNumber = detail.serialNumber;
        this.messageKey = '';
        this.messageError = false;
        this.saving = false;
        this.setDirty(false);
        this.loadSprayers();
        this.loadSuggestions();
        this.notificationService.success(this.textService.get('types.cropSprayers.messages.saved'));
        // Notify other components that machines data has changed
        this.dataRefreshService.notifyMachinesChanged(this.isNew ? 'create' : 'update', detail.serialNumber);
        if (onSuccess) {
          onSuccess();
        }
      },
      error: err => {
        this.messageKey = '';
        this.messageError = false;
        this.saving = false;
        const backendMessage = err?.error?.message as string | undefined;
        const message = backendMessage || this.textService.get('types.cropSprayers.messages.error');
        this.notificationService.error(message);
      }
    });
  }

  cancelEditing(): void {
    if (this.isNew) {
      this.formModel = null;
      this.currentSprayer = null;
      this.isNew = false;
      this.isEditMode = false;
      this.setDirty(false);
    }
    else if (this.currentSprayer) {
      // Przywróć ostatni stan z API
      this.loadSprayerDetail(this.currentSprayer.serialNumber);
    }
  }

  onDelete(): void {
    if (!this.currentSprayer || this.isNew || this.deleting) {
      return;
    }

    this.deleteDialogVisible = true;
  }

  onDeleteConfirm(): void {
    this.deleteDialogVisible = false;
    
    if (!this.currentSprayer || this.deleting) {
      return;
    }

    this.deleting = true;
    const serialToDelete = this.currentSprayer.serialNumber;

    this.sprayersService.deleteSprayer(serialToDelete).subscribe({
      next: () => {
        this.notificationService.success(this.textService.get('types.cropSprayers.messages.deleted'));
        this.deleting = false;
        this.isEditMode = false;
        this.isNew = false;
        this.currentSprayer = null;
        this.formModel = null;
        this.selectedSerialNumber = null;
        this.messageKey = '';
        this.messageError = false;
        this.setDirty(false);
        this.loadSprayers();
        // Notify other components that machines data has changed
        this.dataRefreshService.notifyMachinesChanged('delete', serialToDelete);
      },
      error: err => {
        this.deleting = false;
        const backendMessage = err?.error?.message as string | undefined;
        const message = backendMessage || this.textService.get('types.cropSprayers.messages.error');
        this.notificationService.error(message);
      }
    });
  }

  onDeleteCancel(): void {
    this.deleteDialogVisible = false;
  }

  // Unsaved-changes logic
  private openUnsavedDialog(action: 'select' | 'new', targetSerial: string | null): void {
    this.pendingAction = action;
    this.pendingSerialNumber = targetSerial;
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
    this.pendingSerialNumber = null;
  }

  private runPendingAction(): void {
    if (this.pendingAction === 'new') {
      this.startNewSprayer();
    } else if (this.pendingAction === 'select' && this.pendingSerialNumber) {
      this.loadSprayerDetail(this.pendingSerialNumber);
    }

    this.pendingAction = null;
    this.pendingSerialNumber = null;
  }

  private setDirty(dirty: boolean): void {
    this.dirtyFormService.setDirty(this.formId, dirty);
  }

  getSafeHtml(icon: string): SafeHtml {
    return this.sanitizer.bypassSecurityTrustHtml(icon);
  }
}
