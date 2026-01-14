/*
 * ROBIGOO FIELD SPRAYER CONTROL STATION
 * Copyright (c) 2025 Wojciech Salamon <wojciech.salamon@yahoo.com>
 * All rights reserved. Unauthorized distribution or disclosure is prohibited.
*/

import { Component, OnDestroy, OnInit } from '@angular/core';
import { DomSanitizer, SafeHtml } from '@angular/platform-browser';
import { MachineDetail, MachineListItem, MachineService, MachineCreateUpdateRequest } from '../machine.service';
import { DirtyFormService } from '../shared/services/dirty-form.service';
import { TextService } from '../services/text.service';
import { NotificationService } from '../services/notification.service';
import { SVG_ICONS } from '../shared/svg-icons';
import { SelectOption } from '../shared/components/custom-select/custom-select.component';
import { FilterField, FilterValues } from '../shared/components/filter-panel/filter-panel.component';

interface MachineStep {
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
  // List
  machines: MachineListItem[] = [];
  selectedSerialNumber: string | null = null;
  searchTerm = '';

  // Filter panel configuration
  filterFields: FilterField[] = [];
  filterValues: FilterValues = {};
  filterPanelOpen = false;

  // Detail / form
  currentMachine: MachineDetail | null = null;
  formModel: MachineCreateUpdateRequest | null = null;
  isNew = false;
  isEditMode = false;
  private originalSerialNumber: string | null = null;

  // Steps
  steps: MachineStep[] = [
    { id: 0, key: 'basics', titleKey: 'types.machines.steps.basics' },
    { id: 1, key: 'pump', titleKey: 'types.machines.steps.pump' },
    { id: 2, key: 'tank', titleKey: 'types.machines.steps.tank' },
    { id: 3, key: 'control', titleKey: 'types.machines.steps.control' },
    { id: 4, key: 'boom', titleKey: 'types.machines.steps.boom' },
    { id: 5, key: 'sections', titleKey: 'types.machines.steps.sections' },
    { id: 6, key: 'fieldNozzles', titleKey: 'types.machines.steps.fieldNozzles' },
    { id: 7, key: 'gardenNozzles', titleKey: 'types.machines.steps.gardenNozzles' },
    { id: 8, key: 'fan', titleKey: 'types.machines.steps.fan' }
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

  toolbarIcons!: Record<'add' | 'edit' | 'delete' | 'save' | 'cancel', SafeHtml>;

  // Unsaved-changes dialog state
  pendingSerialNumber: string | null = null;
  private pendingAction: 'select' | 'new' | null = null;
  unsavedDialogVisible = false;

  // Delete confirmation dialog state
  deleteDialogVisible = false;

  private readonly formId = 'machines-detail';

  // Select options
  typeOptions: SelectOption[] = [];
  kindOptions: SelectOption[] = [];
  pumpTypeOptions: SelectOption[] = [];

  constructor(
    private machinesService: MachineService,
    private dirtyFormService: DirtyFormService,
    private textService: TextService,
    private notificationService: NotificationService,
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

  // Visible steps depend on machine type (field / garden)
  get visibleSteps(): MachineStep[] {
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

  getStepTitleKey(step: MachineStep): string {
    if (step.key === 'fieldNozzles' || step.key === 'gardenNozzles') {
      return 'types.machines.steps.nozzles';
    }
    return step.titleKey;
  }

  ngOnInit(): void {
    this.initSelectOptions();
    this.initFilterFields();
    this.loadMachines();
  }

  private initFilterFields(): void {
    this.filterFields = [
      {
        key: 'type',
        label: this.textService.get('types.machines.fields.type'),
        type: 'select',
        placeholder: this.textService.get('types.machines.filters.allTypes'),
        options: [
          { value: '00', label: this.textService.get('types.machines.filters.typeField') },
          { value: '01', label: this.textService.get('types.machines.filters.typeGarden') }
        ]
      },
      {
        key: 'kind',
        label: this.textService.get('types.machines.fields.kind'),
        type: 'select',
        placeholder: this.textService.get('types.machines.filters.allKinds'),
        options: [
          { value: '00', label: this.textService.get('types.machines.filters.kindMounted') },
          { value: '01', label: this.textService.get('types.machines.filters.kindTrailed') },
          { value: '02', label: this.textService.get('types.machines.filters.kindSelfPropelled') },
          { value: '03', label: this.textService.get('types.machines.filters.kindOther') }
        ]
      },
      {
        key: 'manufacturer',
        label: this.textService.get('types.machines.fields.manufacturer'),
        type: 'text',
        placeholder: this.textService.get('types.machines.filters.manufacturer')
      },
      {
        key: 'productionYear',
        label: this.textService.get('types.machines.fields.productionYear'),
        type: 'range',
        rangeFromKey: 'yearFrom',
        rangeToKey: 'yearTo',
        rangeFromPlaceholder: this.textService.get('types.machines.filters.yearFrom'),
        rangeToPlaceholder: this.textService.get('types.machines.filters.yearTo'),
        min: 1900,
        max: 2100
      }
    ];
  }

  private initSelectOptions(): void {
    this.typeOptions = [
      { value: '00', label: this.textService.get('types.machines.filters.typeField') },
      { value: '01', label: this.textService.get('types.machines.filters.typeGarden') }
    ];
    this.kindOptions = [
      { value: '00', label: this.textService.get('types.machines.filters.kindMounted') },
      { value: '01', label: this.textService.get('types.machines.filters.kindTrailed') },
      { value: '02', label: this.textService.get('types.machines.filters.kindSelfPropelled') },
      { value: '03', label: this.textService.get('types.machines.filters.kindOther') }
    ];
    this.pumpTypeOptions = [
      { value: 'piston', label: this.textService.get('types.machines.fields.pumpPiston') },
      { value: 'diaphragm', label: this.textService.get('types.machines.fields.pumpDiaphragm') },
      { value: 'other', label: this.textService.get('types.machines.fields.pumpOther') }
    ];
  }

  ngOnDestroy(): void {
    this.dirtyFormService.unregisterForm(this.formId);
  }

  // List handling

  loadMachines(): void {
    this.loadingList = true;
    this.machinesService.getMachinesList({
      q: this.searchTerm || undefined,
      type: this.filterValues['type'] || undefined,
      kind: this.filterValues['kind'] || undefined,
      manufacturer: this.filterValues['manufacturer'] || undefined,
      yearFrom: this.filterValues['yearFrom'] || undefined,
      yearTo: this.filterValues['yearTo'] || undefined
    }).subscribe({
      next: list => {
        this.machines = list;
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
    this.loadMachines();
  }

  onFilterChange(values: FilterValues): void {
    this.filterValues = values;
    this.loadMachines();
  }

  onFilterClear(): void {
    this.filterValues = {};
    this.loadMachines();
  }

  onFilterPanelOpenChange(isOpen: boolean): void {
    this.filterPanelOpen = isOpen;
  }

  onAddNew(): void {
    if (this.dirtyFormService.isDirty(this.formId)) {
      this.openUnsavedDialog('new', null);
      return;
    }
    this.startNewMachine();
  }

  onSelectMachine(serialNumber: string): void {
    if (serialNumber === this.selectedSerialNumber && !this.isNew) {
      return;
    }

    if (this.dirtyFormService.isDirty(this.formId)) {
      this.openUnsavedDialog('select', serialNumber);
      return;
    }

    this.loadMachineDetail(serialNumber);
  }

  onEdit(): void {
    if (!this.currentMachine || this.isEditMode) {
      return;
    }
    this.isEditMode = true;
    this.setDirty(false);
  }

  openHistory(): void {
    if (this.currentMachine && !this.isNew) {
      this.showHistoryDialog = true;
    }
  }

  private startNewMachine(): void {
    this.isNew = true;
    this.isEditMode = true;
    this.originalSerialNumber = null;
    this.selectedSerialNumber = null;
    this.currentMachine = null;
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

  private loadMachineDetail(serialNumber: string): void {
    this.isNew = false;
    this.isEditMode = false;
    this.selectedSerialNumber = serialNumber;
    this.currentStep = 0;
    this.messageKey = '';
    this.messageError = false;
    this.machinesService.getMachine(serialNumber).subscribe({
      next: detail => {
        this.currentMachine = detail;
        this.originalSerialNumber = detail.serialNumber;
        this.formModel = {
          serialNumber: detail.serialNumber,
          sprayerName: detail.sprayerName,
          type: detail.type,
          kind: detail.kind,
          manufacturer: detail.manufacturer,
          productionYear: detail.productionYear,
          purchaseDate: detail.purchaseDate || null,
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
        this.messageKey = 'types.machines.messages.error';
        this.messageError = true;
      }
    });
  }

  private reconcileSelection(list: MachineListItem[]): void {
    if (list.length === 0) {
      if (!this.isNew) {
        this.selectedSerialNumber = null;
        this.currentMachine = null;
        this.formModel = null;
      }
      return;
    }

    const hasSelected = !!this.selectedSerialNumber && list.some(m => m.serialNumber === this.selectedSerialNumber);

    if (hasSelected) {
      if (!this.formModel || this.formModel.serialNumber !== this.selectedSerialNumber) {
        this.loadMachineDetail(this.selectedSerialNumber!);
      }
      return;
    }

    if (this.isEditMode || this.isNew || this.dirtyFormService.isDirty(this.formId)) {
      return;
    }

    const firstSerialNumber = list[0].serialNumber;
    this.loadMachineDetail(firstSerialNumber);
  }

  // Step navigation

  goToStep(index: number): void {
    const step = this.steps.find(s => s.id === index);
    if (step) {
      this.currentStep = step.id;
    }
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

    const req: MachineCreateUpdateRequest = { ...this.formModel };
    const obs = this.isNew
      ? this.machinesService.createMachine(req)
      : this.machinesService.updateMachine(this.originalSerialNumber ?? this.formModel.serialNumber, req);

    obs.subscribe({
      next: detail => {
        this.currentMachine = detail;
        this.isNew = false;
        this.isEditMode = false;
        this.originalSerialNumber = detail.serialNumber;
        this.selectedSerialNumber = detail.serialNumber;
        this.messageKey = '';
        this.messageError = false;
        this.saving = false;
        this.setDirty(false);
        this.loadMachines();
        this.notificationService.success(this.textService.get('types.machines.messages.saved'));
        if (onSuccess) {
          onSuccess();
        }
      },
      error: err => {
        this.messageKey = '';
        this.messageError = false;
        this.saving = false;
        const backendMessage = err?.error?.message as string | undefined;
        const message = backendMessage || this.textService.get('types.machines.messages.error');
        this.notificationService.error(message);
      }
    });
  }

  cancelEditing(): void {
    if (this.isNew) {
      this.formModel = null;
      this.currentMachine = null;
      this.isNew = false;
      this.isEditMode = false;
      this.setDirty(false);
    }
    else if (this.currentMachine) {
      // Przywróć ostatni stan z API
      this.loadMachineDetail(this.currentMachine.serialNumber);
    }
  }

  onDelete(): void {
    if (!this.currentMachine || this.isNew || this.deleting) {
      return;
    }

    this.deleteDialogVisible = true;
  }

  onDeleteConfirm(): void {
    this.deleteDialogVisible = false;
    
    if (!this.currentMachine || this.deleting) {
      return;
    }

    this.deleting = true;
    const serialToDelete = this.currentMachine.serialNumber;

    this.machinesService.deleteMachine(serialToDelete).subscribe({
      next: () => {
        this.notificationService.success(this.textService.get('types.machines.messages.deleted'));
        this.deleting = false;
        this.isEditMode = false;
        this.isNew = false;
        this.currentMachine = null;
        this.formModel = null;
        this.selectedSerialNumber = null;
        this.messageKey = '';
        this.messageError = false;
        this.setDirty(false);
        this.loadMachines();
      },
      error: err => {
        this.deleting = false;
        const backendMessage = err?.error?.message as string | undefined;
        const message = backendMessage || this.textService.get('types.machines.messages.error');
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
      this.startNewMachine();
    } else if (this.pendingAction === 'select' && this.pendingSerialNumber) {
      this.loadMachineDetail(this.pendingSerialNumber);
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
