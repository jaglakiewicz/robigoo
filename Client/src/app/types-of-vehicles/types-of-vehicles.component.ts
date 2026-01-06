/*
 * ROBIGOO FIELD SPRAYER CONTROL STATION
 * Copyright (c) 2025 Wojciech Salamon <wojciech.salamon@yahoo.com>
 * All rights reserved. Unauthorized distribution or disclosure is prohibited.
*/

import { Component, OnDestroy, OnInit } from '@angular/core';
import { MachineDetail, MachineListItem, MachineService, MachineCreateUpdateRequest } from '../machine.service';
import { DirtyFormService } from '../shared/services/dirty-form.service';
import { TranslationService } from '../i18n/translation.service';
import { NotificationService } from '../services/notification.service';

interface MachineStep {
  id: number;
  key: string;
  titleKey: string;
}

@Component({
  selector: 'app-types-of-vehicles',
  templateUrl: './types-of-vehicles.component.html',
  styleUrls: ['./types-of-vehicles.component.css']
})
export class TypesOfVehiclesComponent implements OnInit, OnDestroy {
  // List
  machines: MachineListItem[] = [];
  selectedSerialNumber: string | null = null;
  searchTerm = '';
  filterType: string | null = null;
  filterKind: string | null = null;
  filterManufacturer = '';
  filterYearFrom = '';
  filterYearTo = '';
  showAdvancedFilters = false;

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
  saving = false;

  // Unsaved-changes dialog state
  pendingSerialNumber: string | null = null;
  private pendingAction: 'select' | 'new' | null = null;
  unsavedDialogVisible = false;

  private readonly formId = 'machines-detail';

  constructor(
    private machinesService: MachineService,
    private dirtyFormService: DirtyFormService,
    private translations: TranslationService,
    private notificationService: NotificationService
  ) {
    this.dirtyFormService.registerForm(this.formId);
  }

  toggleAdvancedFilters(): void {
    this.showAdvancedFilters = !this.showAdvancedFilters;
  }

  ngOnInit(): void {
    this.loadMachines();
  }

  ngOnDestroy(): void {
    this.dirtyFormService.unregisterForm(this.formId);
  }

  // List handling

  loadMachines(): void {
    this.loadingList = true;
    this.machinesService.getMachinesList({
      q: this.searchTerm || undefined,
      type: this.filterType || undefined,
      kind: this.filterKind || undefined,
      manufacturer: this.filterManufacturer || undefined,
      yearFrom: this.filterYearFrom || undefined,
      yearTo: this.filterYearTo || undefined
    }).subscribe({
      next: list => {
        this.machines = list;
        this.loadingList = false;
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

  onFilterTypeChange(value: string): void {
    this.filterType = value || null;
    this.loadMachines();
  }

  onFilterKindChange(value: string): void {
    this.filterKind = value || null;
    this.loadMachines();
  }

  onFilterManufacturerChange(value: string): void {
    this.filterManufacturer = value;
    this.loadMachines();
  }

  onFilterYearFromChange(value: string): void {
    this.filterYearFrom = value;
    this.loadMachines();
  }

  onFilterYearToChange(value: string): void {
    this.filterYearTo = value;
    this.loadMachines();
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

  // Step navigation

  goToStep(index: number): void {
    if (index >= 0 && index < this.steps.length) {
      this.currentStep = index;
    }
  }

  previousStep(): void {
    if (this.currentStep > 0) {
      this.currentStep--;
    }
  }

  nextStep(): void {
    if (this.currentStep < this.steps.length - 1) {
      this.currentStep++;
    }
  }

  // Form helpers

  onFormChange(): void {
    if (!this.isEditMode) {
      return;
    }
    this.setDirty(true);
  }

  onPumpTypeChange(type: 'piston' | 'diaphragm' | 'other'): void {
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
        this.notificationService.success(this.translations.translate('types.machines.messages.saved'));
        if (onSuccess) {
          onSuccess();
        }
      },
      error: err => {
        this.messageKey = '';
        this.messageError = false;
        this.saving = false;
        const backendMessage = err?.error?.message as string | undefined;
        const message = backendMessage || this.translations.translate('types.machines.messages.error');
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
}

