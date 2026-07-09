/*
 * ROBIGOO FIELD SPRAYER CONTROL STATION
 * New Inspection Protocol Component
 * Copyright (c) 2025 Wojciech Salamon <wojciech.salamon@yahoo.com>
 * All rights reserved. Unauthorized distribution or disclosure is prohibited.
*/

import { Component, EventEmitter, Input, Output, OnInit, OnDestroy } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { DomSanitizer, SafeHtml } from '@angular/platform-browser';
import { Subscription } from 'rxjs';
import { InspectionProtocolService, InspectionProtocolCreateUpdateRequest, InspectionProtocolDetail } from '../inspection-protocol.service';
import { ClientService, ClientDetail, ClientListItem, ClientCreateUpdateRequest } from '../client.service';
import { CropSprayerService, CropSprayerDetail, CropSprayerListItem, CropSprayerCreateUpdateRequest } from '../crop-sprayer.service';
import { TextService } from '../services/text.service';
import { AuthService } from '../services/auth.service';
import { NavigationService } from '../services/navigation.service';
import { SVG_ICONS } from '../shared/svg-icons';
import { Step } from '../shared/components/step-indicator/step-indicator.component';

@Component({
  selector: 'app-new-inspection',
  templateUrl: './new-inspection.component.html',
  styleUrls: ['./new-inspection.component.css']
})
export class NewInspectionComponent implements OnInit, OnDestroy {
  @Input() instanceId: number = 0;
  @Output() close = new EventEmitter<void>();

  // Main step management (2 steps: General Data + Protocol)
  currentStep = 0;
  steps: Step[] = [];

  // Protocol section navigation (nested within step 1)
  currentProtocolSection = 0;
  protocolSections: Step[] = [];

  // Client selection
  clients: ClientListItem[] = [];
  selectedClientId: string | null = null;
  selectedClient: ClientDetail | null = null;
  clientSearchText = '';
  filteredClients: ClientListItem[] = [];
  isClientDropdownOpen = false;
  isAddingNewClient = false;

  // New client form (if adding manually)
  newClient: ClientCreateUpdateRequest = this.getEmptyClientRequest();

  // CropSprayer selection
  clientSprayers: CropSprayerListItem[] = [];
  selectedSprayerSerialNumber: string | null = null;
  selectedSprayer: CropSprayerDetail | null = null;
  isAddingNewSprayer = false;

  // New sprayer form (if adding manually)
  newSprayer: CropSprayerCreateUpdateRequest = this.getEmptySprayerRequest();

  // Inspection metadata
  inspectionDate = new Date().toISOString().slice(0, 10);
  inspectionLocation = '';
  inspectorName = '';
  inspectorLicenseNumber = '';
  protocolNumber = '';
  today = new Date().toLocaleDateString('pl-PL');

  // Section 1: General condition
  generalConditionPassed: boolean | null = null;
  generalConditionPassedDriveOn: boolean | null = null;
  markingsReadablePassed: boolean | null = null;
  markingsReadablePassedDriveOn: boolean | null = null;
  equipmentCompletePassed: boolean | null = null;
  equipmentCompletePassedDriveOn: boolean | null = null;
  generalSectionNotes = '';

  // Section 2: Pump
  pumpOperationPassed: boolean | null = null;
  pumpOperationPassedDriveOn: boolean | null = null;
  pumpSealingPassed: boolean | null = null;
  pumpSealingPassedDriveOn: boolean | null = null;
  pressurePulsationPassed: boolean | null = null;
  pressurePulsationPassedDriveOn: boolean | null = null;
  pumpSectionNotes = '';

  // Section 3: Agitation
  agitatorOperationPassed: boolean | null = null;
  agitatorOperationPassedDriveOn: boolean | null = null;
  agitatorSectionNotes = '';

  // Section 4: Tank
  tankConditionPassed: boolean | null = null;
  tankConditionPassedDriveOn: boolean | null = null;
  tankSealingPassed: boolean | null = null;
  tankSealingPassedDriveOn: boolean | null = null;
  levelIndicatorPassed: boolean | null = null;
  levelIndicatorPassedDriveOn: boolean | null = null;
  flushingSystemPassed: boolean | null = null;
  flushingSystemPassedDriveOn: boolean | null = null;
  tankSectionNotes = '';

  // Section 5: Measuring instruments
  manometerPassed: boolean | null = null;
  manometerPassedDriveOn: boolean | null = null;
  manometerReading2Bar: number | null = null;
  manometerReading4Bar: number | null = null;
  manometerReading6Bar: number | null = null;
  manometerDialSizePassed: boolean | null = null;
  manometerDialSizePassedDriveOn: boolean | null = null;
  measuringSectionNotes = '';

  // Section 6: Piping
  pipesConditionPassed: boolean | null = null;
  pipesConditionPassedDriveOn: boolean | null = null;
  connectionsSealingPassed: boolean | null = null;
  connectionsSealingPassedDriveOn: boolean | null = null;
  pipingSectionNotes = '';

  // Section 7: Filtration
  suctionFilterPassed: boolean | null = null;
  suctionFilterPassedDriveOn: boolean | null = null;
  pressureFilterPassed: boolean | null = null;
  pressureFilterPassedDriveOn: boolean | null = null;
  nozzleFiltersPassed: boolean | null = null;
  nozzleFiltersPassedDriveOn: boolean | null = null;
  filtrationSectionNotes = '';

  // Section 8: Boom/Spray equipment
  fieldBoomConditionPassed: boolean | null = null;
  fieldBoomConditionPassedDriveOn: boolean | null = null;
  boomStabilityPassed: boolean | null = null;
  boomStabilityPassedDriveOn: boolean | null = null;
  boomHeightPassed: boolean | null = null;
  boomHeightPassedDriveOn: boolean | null = null;
  boomSymmetryPassed: boolean | null = null;
  boomSymmetryPassedDriveOn: boolean | null = null;
  orchardSprayerConditionPassed: boolean | null = null;
  orchardSprayerConditionPassedDriveOn: boolean | null = null;
  airStreamDirectionPassed: boolean | null = null;
  airStreamDirectionPassedDriveOn: boolean | null = null;
  boomSectionNotes = '';

  // Section 9: Nozzles
  nozzleUniformityPassed: boolean | null = null;
  nozzleUniformityPassedDriveOn: boolean | null = null;
  nozzleFlowRatePassed: boolean | null = null;
  nozzleFlowRatePassedDriveOn: boolean | null = null;
  nozzleConditionPassed: boolean | null = null;
  nozzleConditionPassedDriveOn: boolean | null = null;
  nozzleMeasurements = '';
  nozzlesSectionNotes = '';

  // Section 10: Distribution
  transverseDistributionPassed: boolean | null = null;
  transverseDistributionPassedDriveOn: boolean | null = null;
  coefficientOfVariation: number | null = null;
  distributionSectionNotes = '';

  // Final result
  finalResult: boolean | null = null;
  validUntil: string | null = null;
  controlStickerNumber = '';
  generalNotes = '';

  // Edit mode
  editProtocolId: number | null = null;
  isEditMode = false;

  // UI state
  saving = false;
  messageKey = '';
  messageType: 'success' | 'error' | '' = '';
  isLoadingClients = false;
  isLoadingSprayers = false;
  isPdfPreviewOpen = false;
  generatingPdf = false;

  // Icons
  SVG_ICONS = SVG_ICONS;
  iconBack = SVG_ICONS.iconBack;
  iconNext = SVG_ICONS.iconNext;
  iconSave = SVG_ICONS.iconSave;
  iconCancel = SVG_ICONS.iconCancel;
  iconAdd = SVG_ICONS.iconAdd;
  iconPdf = SVG_ICONS.iconPdf;
  iconEye = SVG_ICONS.iconEye;
  iconCheckSafe: any;
  iconXSafe: any;

  private subscriptions: Subscription[] = [];

  constructor(
    private protocolService: InspectionProtocolService,
    private clientService: ClientService,
    private cropSprayerService: CropSprayerService,
    private textService: TextService,
    private authService: AuthService,
    private sanitizer: DomSanitizer,
    private navigationService: NavigationService,
    private http: HttpClient
  ) {}

  private readonly STORAGE_KEY_BASE = 'robigoo_new_inspection_state';
  private get STORAGE_KEY(): string { return `${this.STORAGE_KEY_BASE}_${this.instanceId}`; }

  ngOnInit(): void {
    this.iconCheckSafe = this.getSafeHtml(SVG_ICONS.iconCheck);
    this.iconXSafe = this.getSafeHtml(SVG_ICONS.iconCancel);
    this.initSteps();
    this.loadClients();
    this.loadInspectorInfo();

    const navParams = this.navigationService.getPendingParams();
    const editId = navParams?.['edit'];
    if (editId) {
      const id = parseInt(editId, 10);
      if (!isNaN(id)) {
        this.editProtocolId = id;
        this.isEditMode = true;
        this.loadProtocolForEdit(id);
        return;
      }
    }

    this.restoreState();
  }

  ngOnDestroy(): void {
    this.saveState();
    this.subscriptions.forEach(s => s.unsubscribe());
  }

  /** Save current form state to localStorage */
  private saveState(): void {
    const state = {
      currentStep: this.currentStep,
      currentProtocolSection: this.currentProtocolSection,
      selectedClientId: this.selectedClientId,
      selectedSprayerSerialNumber: this.selectedSprayerSerialNumber,
      inspectionDate: this.inspectionDate,
      inspectionLocation: this.inspectionLocation,
      inspectorName: this.inspectorName,
      inspectorLicenseNumber: this.inspectorLicenseNumber,
      // Section 1
      generalConditionPassed: this.generalConditionPassed,
      generalConditionPassedDriveOn: this.generalConditionPassedDriveOn,
      markingsReadablePassed: this.markingsReadablePassed,
      markingsReadablePassedDriveOn: this.markingsReadablePassedDriveOn,
      equipmentCompletePassed: this.equipmentCompletePassed,
      equipmentCompletePassedDriveOn: this.equipmentCompletePassedDriveOn,
      generalSectionNotes: this.generalSectionNotes,
      // Section 2
      pumpOperationPassed: this.pumpOperationPassed,
      pumpOperationPassedDriveOn: this.pumpOperationPassedDriveOn,
      pumpSealingPassed: this.pumpSealingPassed,
      pumpSealingPassedDriveOn: this.pumpSealingPassedDriveOn,
      pressurePulsationPassed: this.pressurePulsationPassed,
      pressurePulsationPassedDriveOn: this.pressurePulsationPassedDriveOn,
      pumpSectionNotes: this.pumpSectionNotes,
      // Section 3
      agitatorOperationPassed: this.agitatorOperationPassed,
      agitatorOperationPassedDriveOn: this.agitatorOperationPassedDriveOn,
      agitatorSectionNotes: this.agitatorSectionNotes,
      // Section 4
      tankConditionPassed: this.tankConditionPassed,
      tankConditionPassedDriveOn: this.tankConditionPassedDriveOn,
      tankSealingPassed: this.tankSealingPassed,
      tankSealingPassedDriveOn: this.tankSealingPassedDriveOn,
      levelIndicatorPassed: this.levelIndicatorPassed,
      levelIndicatorPassedDriveOn: this.levelIndicatorPassedDriveOn,
      flushingSystemPassed: this.flushingSystemPassed,
      flushingSystemPassedDriveOn: this.flushingSystemPassedDriveOn,
      tankSectionNotes: this.tankSectionNotes,
      // Section 5
      manometerPassed: this.manometerPassed,
      manometerPassedDriveOn: this.manometerPassedDriveOn,
      manometerReading2Bar: this.manometerReading2Bar,
      manometerReading4Bar: this.manometerReading4Bar,
      manometerReading6Bar: this.manometerReading6Bar,
      manometerDialSizePassed: this.manometerDialSizePassed,
      manometerDialSizePassedDriveOn: this.manometerDialSizePassedDriveOn,
      measuringSectionNotes: this.measuringSectionNotes,
      // Section 6
      pipesConditionPassed: this.pipesConditionPassed,
      pipesConditionPassedDriveOn: this.pipesConditionPassedDriveOn,
      connectionsSealingPassed: this.connectionsSealingPassed,
      connectionsSealingPassedDriveOn: this.connectionsSealingPassedDriveOn,
      pipingSectionNotes: this.pipingSectionNotes,
      // Section 7
      suctionFilterPassed: this.suctionFilterPassed,
      suctionFilterPassedDriveOn: this.suctionFilterPassedDriveOn,
      pressureFilterPassed: this.pressureFilterPassed,
      pressureFilterPassedDriveOn: this.pressureFilterPassedDriveOn,
      nozzleFiltersPassed: this.nozzleFiltersPassed,
      nozzleFiltersPassedDriveOn: this.nozzleFiltersPassedDriveOn,
      filtrationSectionNotes: this.filtrationSectionNotes,
      // Section 8
      fieldBoomConditionPassed: this.fieldBoomConditionPassed,
      fieldBoomConditionPassedDriveOn: this.fieldBoomConditionPassedDriveOn,
      boomStabilityPassed: this.boomStabilityPassed,
      boomStabilityPassedDriveOn: this.boomStabilityPassedDriveOn,
      boomHeightPassed: this.boomHeightPassed,
      boomHeightPassedDriveOn: this.boomHeightPassedDriveOn,
      boomSymmetryPassed: this.boomSymmetryPassed,
      boomSymmetryPassedDriveOn: this.boomSymmetryPassedDriveOn,
      orchardSprayerConditionPassed: this.orchardSprayerConditionPassed,
      orchardSprayerConditionPassedDriveOn: this.orchardSprayerConditionPassedDriveOn,
      airStreamDirectionPassed: this.airStreamDirectionPassed,
      airStreamDirectionPassedDriveOn: this.airStreamDirectionPassedDriveOn,
      boomSectionNotes: this.boomSectionNotes,
      // Section 9
      nozzleUniformityPassed: this.nozzleUniformityPassed,
      nozzleUniformityPassedDriveOn: this.nozzleUniformityPassedDriveOn,
      nozzleFlowRatePassed: this.nozzleFlowRatePassed,
      nozzleFlowRatePassedDriveOn: this.nozzleFlowRatePassedDriveOn,
      nozzleConditionPassed: this.nozzleConditionPassed,
      nozzleConditionPassedDriveOn: this.nozzleConditionPassedDriveOn,
      nozzleMeasurements: this.nozzleMeasurements,
      nozzlesSectionNotes: this.nozzlesSectionNotes,
      // Section 10
      transverseDistributionPassed: this.transverseDistributionPassed,
      transverseDistributionPassedDriveOn: this.transverseDistributionPassedDriveOn,
      coefficientOfVariation: this.coefficientOfVariation,
      distributionSectionNotes: this.distributionSectionNotes,
      // Final
      finalResult: this.finalResult,
      validUntil: this.validUntil,
      controlStickerNumber: this.controlStickerNumber,
      generalNotes: this.generalNotes
    };
    localStorage.setItem(this.STORAGE_KEY, JSON.stringify(state));
  }

  /** Restore form state from localStorage */
  private restoreState(): void {
    const stored = localStorage.getItem(this.STORAGE_KEY);
    if (!stored) return;

    try {
      const state = JSON.parse(stored);
      this.currentStep = state.currentStep ?? 0;
      this.currentProtocolSection = state.currentProtocolSection ?? 0;
      this.selectedClientId = state.selectedClientId ?? null;
      this.selectedSprayerSerialNumber = state.selectedSprayerSerialNumber ?? null;
      this.inspectionDate = state.inspectionDate ?? new Date().toISOString().slice(0, 10);
      this.inspectionLocation = state.inspectionLocation ?? '';
      this.inspectorName = state.inspectorName ?? '';
      this.inspectorLicenseNumber = state.inspectorLicenseNumber ?? '';
      // Section 1
      this.generalConditionPassed = state.generalConditionPassed ?? true;
      this.generalConditionPassedDriveOn = state.generalConditionPassedDriveOn ?? true;
      this.markingsReadablePassed = state.markingsReadablePassed ?? true;
      this.markingsReadablePassedDriveOn = state.markingsReadablePassedDriveOn ?? true;
      this.equipmentCompletePassed = state.equipmentCompletePassed ?? true;
      this.equipmentCompletePassedDriveOn = state.equipmentCompletePassedDriveOn ?? true;
      this.generalSectionNotes = state.generalSectionNotes ?? '';
      // Section 2
      this.pumpOperationPassed = state.pumpOperationPassed ?? true;
      this.pumpOperationPassedDriveOn = state.pumpOperationPassedDriveOn ?? true;
      this.pumpSealingPassed = state.pumpSealingPassed ?? true;
      this.pumpSealingPassedDriveOn = state.pumpSealingPassedDriveOn ?? true;
      this.pressurePulsationPassed = state.pressurePulsationPassed ?? true;
      this.pressurePulsationPassedDriveOn = state.pressurePulsationPassedDriveOn ?? true;
      this.pumpSectionNotes = state.pumpSectionNotes ?? '';
      // Section 3
      this.agitatorOperationPassed = state.agitatorOperationPassed ?? true;
      this.agitatorOperationPassedDriveOn = state.agitatorOperationPassedDriveOn ?? true;
      this.agitatorSectionNotes = state.agitatorSectionNotes ?? '';
      // Section 4
      this.tankConditionPassed = state.tankConditionPassed ?? true;
      this.tankConditionPassedDriveOn = state.tankConditionPassedDriveOn ?? true;
      this.tankSealingPassed = state.tankSealingPassed ?? true;
      this.tankSealingPassedDriveOn = state.tankSealingPassedDriveOn ?? true;
      this.levelIndicatorPassed = state.levelIndicatorPassed ?? true;
      this.levelIndicatorPassedDriveOn = state.levelIndicatorPassedDriveOn ?? true;
      this.flushingSystemPassed = state.flushingSystemPassed ?? true;
      this.flushingSystemPassedDriveOn = state.flushingSystemPassedDriveOn ?? true;
      this.tankSectionNotes = state.tankSectionNotes ?? '';
      // Section 5
      this.manometerPassed = state.manometerPassed ?? true;
      this.manometerPassedDriveOn = state.manometerPassedDriveOn ?? true;
      this.manometerReading2Bar = state.manometerReading2Bar ?? null;
      this.manometerReading4Bar = state.manometerReading4Bar ?? null;
      this.manometerReading6Bar = state.manometerReading6Bar ?? null;
      this.manometerDialSizePassed = state.manometerDialSizePassed ?? true;
      this.manometerDialSizePassedDriveOn = state.manometerDialSizePassedDriveOn ?? true;
      this.measuringSectionNotes = state.measuringSectionNotes ?? '';
      // Section 6
      this.pipesConditionPassed = state.pipesConditionPassed ?? true;
      this.pipesConditionPassedDriveOn = state.pipesConditionPassedDriveOn ?? true;
      this.connectionsSealingPassed = state.connectionsSealingPassed ?? true;
      this.connectionsSealingPassedDriveOn = state.connectionsSealingPassedDriveOn ?? true;
      this.pipingSectionNotes = state.pipingSectionNotes ?? '';
      // Section 7
      this.suctionFilterPassed = state.suctionFilterPassed ?? true;
      this.suctionFilterPassedDriveOn = state.suctionFilterPassedDriveOn ?? true;
      this.pressureFilterPassed = state.pressureFilterPassed ?? true;
      this.pressureFilterPassedDriveOn = state.pressureFilterPassedDriveOn ?? true;
      this.nozzleFiltersPassed = state.nozzleFiltersPassed ?? true;
      this.nozzleFiltersPassedDriveOn = state.nozzleFiltersPassedDriveOn ?? true;
      this.filtrationSectionNotes = state.filtrationSectionNotes ?? '';
      // Section 8
      this.fieldBoomConditionPassed = state.fieldBoomConditionPassed ?? true;
      this.fieldBoomConditionPassedDriveOn = state.fieldBoomConditionPassedDriveOn ?? true;
      this.boomStabilityPassed = state.boomStabilityPassed ?? true;
      this.boomStabilityPassedDriveOn = state.boomStabilityPassedDriveOn ?? true;
      this.boomHeightPassed = state.boomHeightPassed ?? true;
      this.boomHeightPassedDriveOn = state.boomHeightPassedDriveOn ?? true;
      this.boomSymmetryPassed = state.boomSymmetryPassed ?? true;
      this.boomSymmetryPassedDriveOn = state.boomSymmetryPassedDriveOn ?? true;
      this.orchardSprayerConditionPassed = state.orchardSprayerConditionPassed ?? true;
      this.orchardSprayerConditionPassedDriveOn = state.orchardSprayerConditionPassedDriveOn ?? true;
      this.airStreamDirectionPassed = state.airStreamDirectionPassed ?? true;
      this.airStreamDirectionPassedDriveOn = state.airStreamDirectionPassedDriveOn ?? true;
      this.boomSectionNotes = state.boomSectionNotes ?? '';
      // Section 9
      this.nozzleUniformityPassed = state.nozzleUniformityPassed ?? true;
      this.nozzleUniformityPassedDriveOn = state.nozzleUniformityPassedDriveOn ?? true;
      this.nozzleFlowRatePassed = state.nozzleFlowRatePassed ?? true;
      this.nozzleFlowRatePassedDriveOn = state.nozzleFlowRatePassedDriveOn ?? true;
      this.nozzleConditionPassed = state.nozzleConditionPassed ?? true;
      this.nozzleConditionPassedDriveOn = state.nozzleConditionPassedDriveOn ?? true;
      this.nozzleMeasurements = state.nozzleMeasurements ?? '';
      this.nozzlesSectionNotes = state.nozzlesSectionNotes ?? '';
      // Section 10
      this.transverseDistributionPassed = state.transverseDistributionPassed ?? true;
      this.transverseDistributionPassedDriveOn = state.transverseDistributionPassedDriveOn ?? true;
      this.coefficientOfVariation = state.coefficientOfVariation ?? null;
      this.distributionSectionNotes = state.distributionSectionNotes ?? '';
      // Final
      this.finalResult = state.finalResult ?? null;
      this.validUntil = state.validUntil ?? null;
      this.controlStickerNumber = state.controlStickerNumber ?? '';
      this.generalNotes = state.generalNotes ?? '';

      // Restore selected client and sprayer if IDs exist
      if (this.selectedClientId) {
        this.loadSelectedClient(this.selectedClientId, this.selectedSprayerSerialNumber);
      }
    } catch (e) {
      console.error('Error restoring inspection state:', e);
      localStorage.removeItem(this.STORAGE_KEY);
    }
  }

  /** Load an existing protocol into the form for editing */
  private loadProtocolForEdit(id: number): void {
    const sub = this.protocolService.get(id).subscribe({
      next: (p: InspectionProtocolDetail) => {
        this.inspectionDate = p.inspectionDate ? p.inspectionDate.slice(0, 10) : new Date().toISOString().slice(0, 10);
        this.inspectionLocation = p.inspectionLocation || '';
        this.inspectorName = p.inspectorName || '';
        this.inspectorLicenseNumber = p.inspectorLicenseNumber || '';
        this.protocolNumber = p.protocolNumber || '';

        this.generalConditionPassed = p.generalConditionPassed ?? null;
        this.generalConditionPassedDriveOn = p.generalConditionPassedDriveOn ?? null;
        this.markingsReadablePassed = p.markingsReadablePassed ?? null;
        this.markingsReadablePassedDriveOn = p.markingsReadablePassedDriveOn ?? null;
        this.equipmentCompletePassed = p.equipmentCompletePassed ?? null;
        this.equipmentCompletePassedDriveOn = p.equipmentCompletePassedDriveOn ?? null;
        this.generalSectionNotes = p.generalSectionNotes || '';

        this.pumpOperationPassed = p.pumpOperationPassed ?? null;
        this.pumpOperationPassedDriveOn = p.pumpOperationPassedDriveOn ?? null;
        this.pumpSealingPassed = p.pumpSealingPassed ?? null;
        this.pumpSealingPassedDriveOn = p.pumpSealingPassedDriveOn ?? null;
        this.pressurePulsationPassed = p.pressurePulsationPassed ?? null;
        this.pressurePulsationPassedDriveOn = p.pressurePulsationPassedDriveOn ?? null;
        this.pumpSectionNotes = p.pumpSectionNotes || '';

        this.agitatorOperationPassed = p.agitatorOperationPassed ?? null;
        this.agitatorOperationPassedDriveOn = p.agitatorOperationPassedDriveOn ?? null;
        this.agitatorSectionNotes = p.agitatorSectionNotes || '';

        this.tankConditionPassed = p.tankConditionPassed ?? null;
        this.tankConditionPassedDriveOn = p.tankConditionPassedDriveOn ?? null;
        this.tankSealingPassed = p.tankSealingPassed ?? null;
        this.tankSealingPassedDriveOn = p.tankSealingPassedDriveOn ?? null;
        this.levelIndicatorPassed = p.levelIndicatorPassed ?? null;
        this.levelIndicatorPassedDriveOn = p.levelIndicatorPassedDriveOn ?? null;
        this.flushingSystemPassed = p.flushingSystemPassed ?? null;
        this.flushingSystemPassedDriveOn = p.flushingSystemPassedDriveOn ?? null;
        this.tankSectionNotes = p.tankSectionNotes || '';

        this.manometerPassed = p.manometerPassed ?? null;
        this.manometerPassedDriveOn = p.manometerPassedDriveOn ?? null;
        this.manometerReading2Bar = p.manometerReading2Bar ?? null;
        this.manometerReading4Bar = p.manometerReading4Bar ?? null;
        this.manometerReading6Bar = p.manometerReading6Bar ?? null;
        this.manometerDialSizePassed = p.manometerDialSizePassed ?? null;
        this.manometerDialSizePassedDriveOn = p.manometerDialSizePassedDriveOn ?? null;
        this.measuringSectionNotes = p.measuringSectionNotes || '';

        this.pipesConditionPassed = p.pipesConditionPassed ?? null;
        this.pipesConditionPassedDriveOn = p.pipesConditionPassedDriveOn ?? null;
        this.connectionsSealingPassed = p.connectionsSealingPassed ?? null;
        this.connectionsSealingPassedDriveOn = p.connectionsSealingPassedDriveOn ?? null;
        this.pipingSectionNotes = p.pipingSectionNotes || '';

        this.suctionFilterPassed = p.suctionFilterPassed ?? null;
        this.suctionFilterPassedDriveOn = p.suctionFilterPassedDriveOn ?? null;
        this.pressureFilterPassed = p.pressureFilterPassed ?? null;
        this.pressureFilterPassedDriveOn = p.pressureFilterPassedDriveOn ?? null;
        this.nozzleFiltersPassed = p.nozzleFiltersPassed ?? null;
        this.nozzleFiltersPassedDriveOn = p.nozzleFiltersPassedDriveOn ?? null;
        this.filtrationSectionNotes = p.filtrationSectionNotes || '';

        this.fieldBoomConditionPassed = p.fieldBoomConditionPassed ?? null;
        this.fieldBoomConditionPassedDriveOn = p.fieldBoomConditionPassedDriveOn ?? null;
        this.boomStabilityPassed = p.boomStabilityPassed ?? null;
        this.boomStabilityPassedDriveOn = p.boomStabilityPassedDriveOn ?? null;
        this.boomHeightPassed = p.boomHeightPassed ?? null;
        this.boomHeightPassedDriveOn = p.boomHeightPassedDriveOn ?? null;
        this.boomSymmetryPassed = p.boomSymmetryPassed ?? null;
        this.boomSymmetryPassedDriveOn = p.boomSymmetryPassedDriveOn ?? null;
        this.orchardSprayerConditionPassed = p.orchardSprayerConditionPassed ?? null;
        this.orchardSprayerConditionPassedDriveOn = p.orchardSprayerConditionPassedDriveOn ?? null;
        this.airStreamDirectionPassed = p.airStreamDirectionPassed ?? null;
        this.airStreamDirectionPassedDriveOn = p.airStreamDirectionPassedDriveOn ?? null;
        this.boomSectionNotes = p.boomSectionNotes || '';

        this.nozzleUniformityPassed = p.nozzleUniformityPassed ?? null;
        this.nozzleUniformityPassedDriveOn = p.nozzleUniformityPassedDriveOn ?? null;
        this.nozzleFlowRatePassed = p.nozzleFlowRatePassed ?? null;
        this.nozzleFlowRatePassedDriveOn = p.nozzleFlowRatePassedDriveOn ?? null;
        this.nozzleConditionPassed = p.nozzleConditionPassed ?? null;
        this.nozzleConditionPassedDriveOn = p.nozzleConditionPassedDriveOn ?? null;
        this.nozzleMeasurements = p.nozzleMeasurements || '';
        this.nozzlesSectionNotes = p.nozzlesSectionNotes || '';

        this.transverseDistributionPassed = p.transverseDistributionPassed ?? null;
        this.transverseDistributionPassedDriveOn = p.transverseDistributionPassedDriveOn ?? null;
        this.coefficientOfVariation = p.coefficientOfVariation ?? null;
        this.distributionSectionNotes = p.distributionSectionNotes || '';

        this.finalResult = p.finalResult ?? null;
        this.validUntil = p.validUntil ? p.validUntil.slice(0, 10) : null;
        this.controlStickerNumber = p.controlStickerNumber || '';
        this.generalNotes = p.generalNotes || '';

        if (p.clientId) {
          this.selectedClientId = p.clientId;
          this.loadSelectedClient(p.clientId, p.cropSprayerSerialNumber);
        }
      },
      error: () => {
        this.showMessage('inspectionProtocol.messages.loadError', 'error');
      }
    });
    this.subscriptions.push(sub);
  }

  /** Load selected client details after restoring state */
  private loadSelectedClient(clientId: string, restoreSprayerSerial?: string | null): void {
    const sub = this.clientService.getClient(clientId).subscribe({
      next: (client) => {
        this.selectedClient = client;
        this.clientSearchText = client.clientType === 'person' 
          ? `${client.firstName} ${client.lastName}` 
          : client.companyName || '';
        this.loadClientSprayers(clientId, restoreSprayerSerial);
      },
      error: () => {
        this.selectedClientId = null;
        this.selectedClient = null;
      }
    });
    this.subscriptions.push(sub);
  }

  /** Clear saved state */
  clearSavedState(): void {
    localStorage.removeItem(this.STORAGE_KEY);
  }

  private initSteps(): void {
    // 2 main steps: General Data + Protocol
    this.steps = [
      { id: 0, key: 'generalData', label: this.textService.get('inspectionProtocol.steps.generalData') },
      { id: 1, key: 'protocol', label: this.textService.get('inspectionProtocol.steps.protocol') }
    ];

    // 11 protocol sections (nested within step 1)
    this.protocolSections = [
      { id: 0, key: 'general', label: this.textService.get('inspectionProtocol.steps.general') },
      { id: 1, key: 'pump', label: this.textService.get('inspectionProtocol.steps.pump') },
      { id: 2, key: 'agitation', label: this.textService.get('inspectionProtocol.steps.agitation') },
      { id: 3, key: 'tank', label: this.textService.get('inspectionProtocol.steps.tank') },
      { id: 4, key: 'measuring', label: this.textService.get('inspectionProtocol.steps.measuring') },
      { id: 5, key: 'piping', label: this.textService.get('inspectionProtocol.steps.piping') },
      { id: 6, key: 'filtration', label: this.textService.get('inspectionProtocol.steps.filtration') },
      { id: 7, key: 'boom', label: this.textService.get('inspectionProtocol.steps.boom') },
      { id: 8, key: 'nozzles', label: this.textService.get('inspectionProtocol.steps.nozzles') },
      { id: 9, key: 'distribution', label: this.textService.get('inspectionProtocol.steps.distribution') },
      { id: 10, key: 'result', label: 'Wynik końcowy' }
    ];
  }

  private loadClients(): void {
    this.isLoadingClients = true;
    const sub = this.clientService.getClientsList().subscribe({
      next: (clients) => {
        this.clients = clients;
        this.filteredClients = clients.slice(0, 50);
        this.isLoadingClients = false;
      },
      error: () => {
        this.isLoadingClients = false;
      }
    });
    this.subscriptions.push(sub);
  }

  private loadInspectorInfo(): void {
    const currentUser = this.authService.getCurrentUser();
    if (currentUser) {
      this.inspectorName = `${currentUser.firstName} ${currentUser.lastName}`.trim() || currentUser.login || '';
    }
    // Load global unit number from app settings
    this.http.get<any>('/api/settings').subscribe({
      next: (data) => {
        if (data?.organization?.unitAuthorizationNumber) {
          this.inspectorLicenseNumber = data.organization.unitAuthorizationNumber;
        }
      },
      error: () => {}
    });
  }

  onClientSearchChange(term: string): void {
    this.clientSearchText = term;
    this.isClientDropdownOpen = true;
    
    if (!term || term.length < 1) {
      this.filteredClients = this.clients.slice(0, 50);
    } else {
      const lowerTerm = term.toLowerCase();
      this.filteredClients = this.clients
        .filter(c => {
          // Search by firstName, lastName for persons
          const firstNameMatch = c.firstName && c.firstName.toLowerCase().includes(lowerTerm);
          const lastNameMatch = c.lastName && c.lastName.toLowerCase().includes(lowerTerm);
          // Search by companyName for companies
          const companyMatch = c.companyName && c.companyName.toLowerCase().includes(lowerTerm);
          // Search by displayName (combined name)
          const displayNameMatch = c.displayName && c.displayName.toLowerCase().includes(lowerTerm);
          // Search by city
          const cityMatch = c.city && c.city.toLowerCase().includes(lowerTerm);
          // Search by NIP or PESEL
          const nipMatch = c.nip && c.nip.includes(term);
          const peselMatch = c.pesel && c.pesel.includes(term);
          
          return firstNameMatch || lastNameMatch || companyMatch || displayNameMatch || cityMatch || nipMatch || peselMatch;
        })
        .slice(0, 50);
    }
  }

  onStepContentClick(event: Event): void {
    // Close dropdown when clicking outside the search container
    this.isClientDropdownOpen = false;
  }

  selectClient(client: ClientListItem): void {
    this.selectedClientId = client.id;
    this.clientSearchText = client.displayName;
    this.isClientDropdownOpen = false;
    this.isAddingNewClient = false;

    const sub = this.clientService.getClient(client.id).subscribe({
      next: (detail) => {
        this.selectedClient = detail;
        this.loadClientSprayers(client.id);
      }
    });
    this.subscriptions.push(sub);
  }

  clearClientSelection(): void {
    this.selectedClientId = null;
    this.selectedClient = null;
    this.clientSearchText = '';
    this.clientSprayers = [];
    this.selectedSprayerSerialNumber = null;
    this.selectedSprayer = null;
  }

  toggleAddNewClient(): void {
    this.isAddingNewClient = !this.isAddingNewClient;
    if (this.isAddingNewClient) {
      this.newClient = this.getEmptyClientRequest();
      this.clearClientSelection();
    }
  }

  private getEmptyClientRequest(): ClientCreateUpdateRequest {
    return {
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
  }

  onNewClientTypeChange(type: 'person' | 'company'): void {
    this.newClient.clientType = type;
  }

  private loadClientSprayers(clientId: string, restoreSprayerSerial?: string | null): void {
    this.isLoadingSprayers = true;
    const serialToRestore = restoreSprayerSerial ?? this.selectedSprayerSerialNumber;
    
    const sub = this.cropSprayerService.getByOwner(clientId).subscribe({
      next: (sprayers) => {
        this.clientSprayers = sprayers;
        this.isLoadingSprayers = false;
        
        // Try to restore previously selected sprayer
        if (serialToRestore) {
          const savedSprayer = sprayers.find(s => s.serialNumber === serialToRestore);
          if (savedSprayer) {
            this.selectSprayer(savedSprayer);
          }
        } else if (sprayers.length === 1) {
          this.selectSprayer(sprayers[0]);
        }
      },
      error: () => {
        this.isLoadingSprayers = false;
      }
    });
    this.subscriptions.push(sub);
  }

  selectSprayer(sprayer: CropSprayerListItem): void {
    this.selectedSprayerSerialNumber = sprayer.serialNumber;
    this.isAddingNewSprayer = false;

    const sub = this.cropSprayerService.get(sprayer.serialNumber).subscribe({
      next: (detail) => {
        this.selectedSprayer = detail;
      }
    });
    this.subscriptions.push(sub);
  }

  clearSprayerSelection(): void {
    this.selectedSprayerSerialNumber = null;
    this.selectedSprayer = null;
  }

  toggleAddNewSprayer(): void {
    this.isAddingNewSprayer = !this.isAddingNewSprayer;
    if (this.isAddingNewSprayer) {
      this.newSprayer = this.getEmptySprayerRequest();
      this.clearSprayerSelection();
    }
  }

  private getEmptySprayerRequest(): CropSprayerCreateUpdateRequest {
    return {
      serialNumber: '',
      sprayerName: '',
      type: '00',
      kind: '00',
      manufacturer: '',
      productionYear: new Date().getFullYear().toString(),
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
  }

  goToStep(stepId: number): void {
    // Allow clicking on completed or current steps
    if (stepId <= this.currentStep || this.validateCurrentStep()) {
      this.currentStep = stepId;
      // Reset protocol section when going to step 0
      if (stepId === 0) {
        this.currentProtocolSection = 0;
      }
      this.saveState(); // Save state when changing steps
    }
  }

  goToProtocolSection(sectionId: number): void {
    this.currentProtocolSection = sectionId;
    this.saveState(); // Save state when changing protocol sections
  }

  isSectionCompleted(sectionIndex: number): boolean {
    // Check if all checks in a protocol section have been filled
    switch (sectionIndex) {
      case 0: return this.generalConditionPassed !== null && this.markingsReadablePassed !== null && this.equipmentCompletePassed !== null;
      case 1: return this.pumpOperationPassed !== null && this.pumpSealingPassed !== null && this.pressurePulsationPassed !== null;
      case 2: return this.agitatorOperationPassed !== null;
      case 3: return this.tankConditionPassed !== null && this.tankSealingPassed !== null && this.levelIndicatorPassed !== null && this.flushingSystemPassed !== null;
      case 4: return this.manometerPassed !== null && this.manometerDialSizePassed !== null;
      case 5: return this.pipesConditionPassed !== null && this.connectionsSealingPassed !== null;
      case 6: return this.suctionFilterPassed !== null && this.pressureFilterPassed !== null && this.nozzleFiltersPassed !== null;
      case 7: return this.fieldBoomConditionPassed !== null && this.boomStabilityPassed !== null && this.boomHeightPassed !== null && this.boomSymmetryPassed !== null;
      case 8: return this.nozzleUniformityPassed !== null && this.nozzleFlowRatePassed !== null && this.nozzleConditionPassed !== null;
      case 9: return this.transverseDistributionPassed !== null;
      case 10: return this.finalResult !== null;
      default: return false;
    }
  }

  setFinalResult(result: boolean): void {
    this.finalResult = result;
    if (result && this.inspectionDate) {
      const date = new Date(this.inspectionDate);
      date.setFullYear(date.getFullYear() + 3);
      this.validUntil = date.toISOString().slice(0, 10);
    } else {
      this.validUntil = null;
    }
  }

  previousStep(): void {
    if (this.currentStep > 0) {
      this.currentStep--;
    }
  }

  nextStep(): void {
    if (!this.validateCurrentStep()) {
      return;
    }
    if (this.currentStep < this.steps.length - 1) {
      this.currentStep++;
    }
  }

  private validateCurrentStep(): boolean {
    // Only validate when moving forward or submitting, not when just switching tabs
    if (this.currentStep === 0) {
      if (!this.selectedClientId && !this.isAddingNewClient) {
        // Don't show error, just return false to prevent navigation
        return false;
      }
      if (this.isAddingNewClient) {
        if (this.newClient.clientType === 'person') {
          if (!this.newClient.firstName || !this.newClient.lastName) {
            return false;
          }
        } else {
          if (!this.newClient.companyName) {
            return false;
          }
        }
      }
    }

    if (this.currentStep === 1) {
      if (!this.selectedSprayerSerialNumber && !this.isAddingNewSprayer) {
        return false;
      }
      if (this.isAddingNewSprayer) {
        if (!this.newSprayer.serialNumber || !this.newSprayer.sprayerName || !this.newSprayer.manufacturer) {
          return false;
        }
      }
    }

    return true;
  }

  private showMessage(key: string, type: 'success' | 'error'): void {
    this.messageKey = key;
    this.messageType = type;
    setTimeout(() => {
      this.messageKey = '';
      this.messageType = '';
    }, 5000);
  }

  calculateFinalResult(): void {
    const allChecks = [
      this.generalConditionPassed,
      this.generalConditionPassedDriveOn,
      this.markingsReadablePassed,
      this.markingsReadablePassedDriveOn,
      this.equipmentCompletePassed,
      this.equipmentCompletePassedDriveOn,
      this.pumpOperationPassed,
      this.pumpOperationPassedDriveOn,
      this.pumpSealingPassed,
      this.pumpSealingPassedDriveOn,
      this.pressurePulsationPassed,
      this.pressurePulsationPassedDriveOn,
      this.agitatorOperationPassed,
      this.agitatorOperationPassedDriveOn,
      this.tankConditionPassed,
      this.tankConditionPassedDriveOn,
      this.tankSealingPassed,
      this.tankSealingPassedDriveOn,
      this.levelIndicatorPassed,
      this.levelIndicatorPassedDriveOn,
      this.flushingSystemPassed,
      this.flushingSystemPassedDriveOn,
      this.manometerPassed,
      this.manometerPassedDriveOn,
      this.manometerDialSizePassed,
      this.manometerDialSizePassedDriveOn,
      this.pipesConditionPassed,
      this.pipesConditionPassedDriveOn,
      this.connectionsSealingPassed,
      this.connectionsSealingPassedDriveOn,
      this.suctionFilterPassed,
      this.suctionFilterPassedDriveOn,
      this.pressureFilterPassed,
      this.pressureFilterPassedDriveOn,
      this.nozzleFiltersPassed,
      this.nozzleFiltersPassedDriveOn,
      this.fieldBoomConditionPassed,
      this.fieldBoomConditionPassedDriveOn,
      this.boomStabilityPassed,
      this.boomStabilityPassedDriveOn,
      this.boomHeightPassed,
      this.boomHeightPassedDriveOn,
      this.boomSymmetryPassed,
      this.boomSymmetryPassedDriveOn,
      this.nozzleUniformityPassed,
      this.nozzleUniformityPassedDriveOn,
      this.nozzleFlowRatePassed,
      this.nozzleFlowRatePassedDriveOn,
      this.nozzleConditionPassed,
      this.nozzleConditionPassedDriveOn,
      this.transverseDistributionPassed,
      this.transverseDistributionPassedDriveOn
    ];

    const inspectedChecks = allChecks.filter(c => c !== null);
    this.finalResult = inspectedChecks.every(c => c === true);

    if (this.finalResult && this.inspectionDate) {
      const date = new Date(this.inspectionDate);
      date.setFullYear(date.getFullYear() + 3);
      this.validUntil = date.toISOString().slice(0, 10);
    } else {
      this.validUntil = null;
    }
  }

  openPdfPreview(): void {
    this.isPdfPreviewOpen = true;
    this.calculateFinalResult();
  }

  closePdfPreview(): void {
    this.isPdfPreviewOpen = false;
  }

  onBackdropClick(event: MouseEvent): void {
    if (event.target === event.currentTarget) {
      this.closePdfPreview();
    }
  }

  printPreview(): void {
    window.print();
  }

  async generatePdf(): Promise<void> {
    this.generatingPdf = true;
    try {
      // TODO: Implement PDF generation with actual data
      // This would call a service to generate the PDF
      await new Promise(resolve => setTimeout(resolve, 1000)); // Simulate generation
      this.showMessage('inspectionProtocol.messages.pdfGenerated', 'success');
    } catch (error) {
      this.showMessage('inspectionProtocol.messages.pdfError', 'error');
    } finally {
      this.generatingPdf = false;
    }
  }

  /** Check if form can be submitted */
  canSubmit(): boolean {
    // Must have client selected or adding new one
    const hasClient = this.selectedClientId || this.isAddingNewClient;
    // Must have sprayer selected or adding new one
    const hasSprayer = this.selectedSprayerSerialNumber || this.isAddingNewSprayer;
    // Must have inspector info
    const hasInspector = !!this.inspectorName;
    // Must have final result
    const hasFinalResult = this.finalResult !== null;
    
    return hasClient && hasSprayer && hasInspector && hasFinalResult;
  }

  /** Cancel and optionally clear saved state */
  cancelInspection(): void {
    if (confirm(this.textService.get('newInspection.confirmCancel') || 'Czy na pewno chcesz anulować? Niezapisane zmiany zostaną utracone.')) {
      this.clearSavedState();
      this.close.emit();
    }
  }

  async submit(): Promise<void> {
    this.saving = true;
    this.messageKey = '';
    this.messageType = '';

    try {
      let clientId = this.selectedClientId;
      let clientName = this.selectedClient
        ? (this.selectedClient.clientType === 'person'
            ? `${this.selectedClient.firstName} ${this.selectedClient.lastName}`.trim()
            : this.selectedClient.companyName)
        : '';
      let clientAddress = '';
      let clientTaxId = '';

      if (this.isAddingNewClient) {
        const createdClient = await this.clientService.createClient(this.newClient).toPromise();
        if (createdClient) {
          clientId = createdClient.id;
          clientName = createdClient.clientType === 'person'
            ? `${createdClient.firstName} ${createdClient.lastName}`
            : createdClient.companyName;
          clientAddress = this.formatAddress(createdClient);
          clientTaxId = createdClient.clientType === 'person' ? createdClient.pesel : createdClient.nip;
        }
      } else if (this.selectedClient) {
        clientAddress = this.formatAddress(this.selectedClient);
        clientTaxId = this.selectedClient.clientType === 'person'
          ? this.selectedClient.pesel
          : this.selectedClient.nip;
      }

      let sprayerSerial = this.selectedSprayerSerialNumber;
      let sprayerDetail = this.selectedSprayer;

      if (this.isAddingNewSprayer) {
        this.newSprayer.ownerId = clientId;
        this.newSprayer.ownerName = clientName;
        const createdSprayer = await this.cropSprayerService.createSprayer(this.newSprayer).toPromise();
        if (createdSprayer) {
          sprayerSerial = createdSprayer.serialNumber;
          sprayerDetail = createdSprayer;
        }
      }

      const request: InspectionProtocolCreateUpdateRequest = {
        inspectionDate: this.inspectionDate,
        inspectionLocation: this.inspectionLocation || null,
        inspectorName: this.inspectorName,
        inspectorLicenseNumber: this.inspectorLicenseNumber || null,

        clientId: clientId,
        clientName: clientName,
        clientAddress: clientAddress || null,
        clientTaxId: clientTaxId || null,

        cropSprayerSerialNumber: sprayerSerial,
        cropSprayerName: sprayerDetail?.sprayerName || this.newSprayer.sprayerName || null,
        cropSprayerType: sprayerDetail?.type || this.newSprayer.type || null,
        cropSprayerKind: sprayerDetail?.kind || this.newSprayer.kind || null,
        cropSprayerManufacturer: sprayerDetail?.manufacturer || this.newSprayer.manufacturer || null,
        cropSprayerProductionYear: sprayerDetail?.productionYear || this.newSprayer.productionYear || null,
        tankCapacity: sprayerDetail?.tankCapacity ?? this.newSprayer.tankCapacity ?? null,
        boomWidth: sprayerDetail?.boomWidth ?? this.newSprayer.boomWidth ?? null,
        sectionCount: sprayerDetail?.sectionCount ?? this.newSprayer.sectionCount ?? null,

        generalConditionPassed: this.generalConditionPassed,
        generalConditionPassedDriveOn: this.generalConditionPassedDriveOn,
        markingsReadablePassed: this.markingsReadablePassed,
        markingsReadablePassedDriveOn: this.markingsReadablePassedDriveOn,
        equipmentCompletePassed: this.equipmentCompletePassed,
        equipmentCompletePassedDriveOn: this.equipmentCompletePassedDriveOn,
        generalSectionNotes: this.generalSectionNotes || null,

        pumpOperationPassed: this.pumpOperationPassed,
        pumpOperationPassedDriveOn: this.pumpOperationPassedDriveOn,
        pumpSealingPassed: this.pumpSealingPassed,
        pumpSealingPassedDriveOn: this.pumpSealingPassedDriveOn,
        pressurePulsationPassed: this.pressurePulsationPassed,
        pressurePulsationPassedDriveOn: this.pressurePulsationPassedDriveOn,
        pumpSectionNotes: this.pumpSectionNotes || null,

        agitatorOperationPassed: this.agitatorOperationPassed,
        agitatorOperationPassedDriveOn: this.agitatorOperationPassedDriveOn,
        agitatorSectionNotes: this.agitatorSectionNotes || null,

        tankConditionPassed: this.tankConditionPassed,
        tankConditionPassedDriveOn: this.tankConditionPassedDriveOn,
        tankSealingPassed: this.tankSealingPassed,
        tankSealingPassedDriveOn: this.tankSealingPassedDriveOn,
        levelIndicatorPassed: this.levelIndicatorPassed,
        levelIndicatorPassedDriveOn: this.levelIndicatorPassedDriveOn,
        flushingSystemPassed: this.flushingSystemPassed,
        flushingSystemPassedDriveOn: this.flushingSystemPassedDriveOn,
        tankSectionNotes: this.tankSectionNotes || null,

        manometerPassed: this.manometerPassed,
        manometerPassedDriveOn: this.manometerPassedDriveOn,
        manometerReading2Bar: this.manometerReading2Bar,
        manometerReading4Bar: this.manometerReading4Bar,
        manometerReading6Bar: this.manometerReading6Bar,
        manometerDialSizePassed: this.manometerDialSizePassed,
        manometerDialSizePassedDriveOn: this.manometerDialSizePassedDriveOn,
        measuringSectionNotes: this.measuringSectionNotes || null,

        pipesConditionPassed: this.pipesConditionPassed,
        pipesConditionPassedDriveOn: this.pipesConditionPassedDriveOn,
        connectionsSealingPassed: this.connectionsSealingPassed,
        connectionsSealingPassedDriveOn: this.connectionsSealingPassedDriveOn,
        pipingSectionNotes: this.pipingSectionNotes || null,

        suctionFilterPassed: this.suctionFilterPassed,
        suctionFilterPassedDriveOn: this.suctionFilterPassedDriveOn,
        pressureFilterPassed: this.pressureFilterPassed,
        pressureFilterPassedDriveOn: this.pressureFilterPassedDriveOn,
        nozzleFiltersPassed: this.nozzleFiltersPassed,
        nozzleFiltersPassedDriveOn: this.nozzleFiltersPassedDriveOn,
        filtrationSectionNotes: this.filtrationSectionNotes || null,

        fieldBoomConditionPassed: this.fieldBoomConditionPassed,
        fieldBoomConditionPassedDriveOn: this.fieldBoomConditionPassedDriveOn,
        boomStabilityPassed: this.boomStabilityPassed,
        boomStabilityPassedDriveOn: this.boomStabilityPassedDriveOn,
        boomHeightPassed: this.boomHeightPassed,
        boomHeightPassedDriveOn: this.boomHeightPassedDriveOn,
        boomSymmetryPassed: this.boomSymmetryPassed,
        boomSymmetryPassedDriveOn: this.boomSymmetryPassedDriveOn,
        orchardSprayerConditionPassed: this.orchardSprayerConditionPassed,
        orchardSprayerConditionPassedDriveOn: this.orchardSprayerConditionPassedDriveOn,
        airStreamDirectionPassed: this.airStreamDirectionPassed,
        airStreamDirectionPassedDriveOn: this.airStreamDirectionPassedDriveOn,
        boomSectionNotes: this.boomSectionNotes || null,

        nozzleUniformityPassed: this.nozzleUniformityPassed,
        nozzleUniformityPassedDriveOn: this.nozzleUniformityPassedDriveOn,
        nozzleFlowRatePassed: this.nozzleFlowRatePassed,
        nozzleFlowRatePassedDriveOn: this.nozzleFlowRatePassedDriveOn,
        nozzleConditionPassed: this.nozzleConditionPassed,
        nozzleConditionPassedDriveOn: this.nozzleConditionPassedDriveOn,
        nozzleMeasurements: this.nozzleMeasurements || null,
        nozzlesSectionNotes: this.nozzlesSectionNotes || null,

        transverseDistributionPassed: this.transverseDistributionPassed,
        transverseDistributionPassedDriveOn: this.transverseDistributionPassedDriveOn,
        coefficientOfVariation: this.coefficientOfVariation,
        distributionSectionNotes: this.distributionSectionNotes || null,

        finalResult: this.finalResult,
        validUntil: this.validUntil,
        controlStickerNumber: this.controlStickerNumber || null,
        generalNotes: this.generalNotes || null
      };

      if (this.isEditMode && this.editProtocolId !== null) {
        await this.protocolService.updateProtocol(this.editProtocolId, request).toPromise();
      } else {
        await this.protocolService.createProtocol(request).toPromise();
      }

      this.showMessage('inspectionProtocol.messages.saved', 'success');
      this.saving = false;
      this.clearSavedState();

      setTimeout(() => {
        this.close.emit();
      }, 1500);
    } catch (error) {
      this.showMessage('inspectionProtocol.messages.error', 'error');
      this.saving = false;
    }
  }

  private formatAddress(client: ClientDetail): string {
    const parts: string[] = [];
    if (client.street) {
      let streetPart = client.street;
      if (client.buildingNumber) {
        streetPart += ' ' + client.buildingNumber;
        if (client.apartmentNumber) {
          streetPart += '/' + client.apartmentNumber;
        }
      }
      parts.push(streetPart);
    }
    if (client.zipCode) {
      parts.push(client.zipCode);
    }
    if (client.city) {
      parts.push(client.city);
    }
    if (client.voivodeship) {
      parts.push(client.voivodeship);
    }
    return parts.join(', ');
  }

  getSafeHtml(html: string): SafeHtml {
    return this.sanitizer.bypassSecurityTrustHtml(html);
  }

  isFieldSprayer(): boolean {
    const type = this.selectedSprayer?.type || this.newSprayer.type;
    return type === '00';
  }

  isOrchardSprayer(): boolean {
    const type = this.selectedSprayer?.type || this.newSprayer.type;
    return type === '01';
  }

  getSprayerTypeLabel(type: string): string {
    return type === '00'
      ? this.textService.get('types.cropSprayers.filters.typeField')
      : this.textService.get('types.cropSprayers.filters.typeGarden');
  }

  getSprayerKindLabel(kind: string): string {
    switch (kind) {
      case '00': return this.textService.get('types.cropSprayers.filters.kindMounted');
      case '01': return this.textService.get('types.cropSprayers.filters.kindTrailed');
      case '02': return this.textService.get('types.cropSprayers.filters.kindSelfPropelled');
      case '03': return this.textService.get('types.cropSprayers.filters.kindOther');
      default: return kind;
    }
  }
}
