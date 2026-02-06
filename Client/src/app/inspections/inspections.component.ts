/*
 * ROBIGOO FIELD SPRAYER CONTROL STATION
 * Inspection Protocols Management Component
 * Copyright (c) 2025 Wojciech Salamon <wojciech.salamon@yahoo.com>
 * All rights reserved. Unauthorized distribution or disclosure is prohibited.
*/

import { Component, OnDestroy, OnInit } from '@angular/core';
import { DomSanitizer, SafeHtml } from '@angular/platform-browser';
import { Router } from '@angular/router';
import { Subscription } from 'rxjs';
import { InspectionProtocolService, InspectionProtocolListItem, InspectionProtocolDetail } from '../inspection-protocol.service';
import { TextService } from '../services/text.service';
import { NotificationService } from '../services/notification.service';
import { SVG_ICONS } from '../shared/svg-icons';
import { SelectOption } from '../shared/components/custom-select/custom-select.component';
import { FilterField, FilterValues } from '../shared/components/filter-panel/filter-panel.component';
import { Step } from '../shared/components/step-indicator/step-indicator.component';

interface ProtocolStep {
  id: number;
  key: string;
  titleKey: string;
}

@Component({
  selector: 'app-inspections',
  templateUrl: './inspections.component.html',
  styleUrls: ['./inspections.component.css']
})
export class InspectionsComponent implements OnInit, OnDestroy {
  // List
  protocols: InspectionProtocolListItem[] = [];
  selectedProtocolId: number | null = null;
  searchTerm = '';

  // Filter panel configuration
  filterFields: FilterField[] = [];
  filterValues: FilterValues = {};
  filterPanelOpen = false;

  // Detail
  currentProtocol: InspectionProtocolDetail | null = null;
  loadingDetail = false;

  // Steps for detail view (protocol sections)
  steps: ProtocolStep[] = [
    { id: 0, key: 'general', titleKey: 'inspections.steps.general' },
    { id: 1, key: 'sprayer', titleKey: 'inspections.steps.sprayer' },
    { id: 2, key: 'results', titleKey: 'inspections.steps.results' },
    { id: 3, key: 'final', titleKey: 'inspections.steps.final' }
  ];
  currentStep = 0;

  // UI state
  loadingList = false;
  showHistoryDialog = false;
  historyIcon = SVG_ICONS.historyIcon;
  deleting = false;

  // View mode: 'dashboard' (default) or 'document'
  viewMode: 'dashboard' | 'document' = 'dashboard';

  // Accordion expanded sections state
  expandedSections: Record<string, boolean> = {};

  // Additional icons for dashboard view
  documentIcon = '<svg viewBox="0 0 24 24" width="16" height="16" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M15 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V7Z"/><path d="M14 2v4a2 2 0 0 0 2 2h4"/><path d="M10 9H8"/><path d="M16 13H8"/><path d="M16 17H8"/></svg>';
  userIcon = SVG_ICONS.clients;
  sprayerIcon = '<svg viewBox="0 0 24 24" width="16" height="16" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><circle cx="12" cy="12" r="10"/><path d="M12 2a14.5 14.5 0 0 0 0 20 14.5 14.5 0 0 0 0-20"/><path d="M2 12h20"/></svg>';
  stickerIcon = SVG_ICONS.marks;

  // Delete confirmation dialog state
  deleteDialogVisible = false;

  toolbarIcons!: Record<'add' | 'edit' | 'delete' | 'pdf' | 'print', SafeHtml>;

  // Select options for filters
  yearOptions: SelectOption[] = [];
  resultOptions: SelectOption[] = [];
  distanceOptions: SelectOption[] = [];

  // Subscription
  private dataSub?: Subscription;

  constructor(
    private protocolService: InspectionProtocolService,
    private textService: TextService,
    private notificationService: NotificationService,
    private sanitizer: DomSanitizer,
    private router: Router
  ) {
    this.toolbarIcons = {
      add: this.getSafeHtml(SVG_ICONS.iconAdd),
      edit: this.getSafeHtml(SVG_ICONS.iconEdit),
      delete: this.getSafeHtml(SVG_ICONS.deleteIcon),
      pdf: this.getSafeHtml(SVG_ICONS.iconPdf),
      print: this.getSafeHtml(SVG_ICONS.iconPdf)
    };
  }

  /** Convert steps to the format expected by step-indicator component */
  get stepsForIndicator(): Step[] {
    return this.steps.map(step => ({
      id: step.id,
      key: step.key,
      label: this.textService.get(step.titleKey)
    }));
  }

  ngOnInit(): void {
    this.initFilterFields();
    this.initSelectOptions();
    this.loadProtocols();
  }

  ngOnDestroy(): void {
    this.dataSub?.unsubscribe();
  }

  private initFilterFields(): void {
    // Generate year options (last 10 years)
    const currentYear = new Date().getFullYear();
    const yearOpts: SelectOption[] = [];
    for (let y = currentYear; y >= currentYear - 10; y--) {
      yearOpts.push({ value: y.toString(), label: y.toString() });
    }
    this.yearOptions = yearOpts;

    this.filterFields = [
      {
        key: 'year',
        label: this.textService.get('inspections.filters.year'),
        type: 'select',
        placeholder: this.textService.get('inspections.filters.allYears'),
        options: yearOpts
      },
      {
        key: 'finalResult',
        label: this.textService.get('inspections.filters.result'),
        type: 'select',
        placeholder: this.textService.get('inspections.filters.allResults'),
        options: [
          { value: 'true', label: this.textService.get('inspections.filters.resultPositive') },
          { value: 'false', label: this.textService.get('inspections.filters.resultNegative') }
        ]
      },
      {
        key: 'inspectorName',
        label: this.textService.get('inspections.filters.inspector'),
        type: 'text',
        placeholder: this.textService.get('inspections.placeholders.inspector')
      },
      {
        key: 'city',
        label: this.textService.get('inspections.filters.city'),
        type: 'text',
        placeholder: this.textService.get('inspections.filters.cityPlaceholder')
      },
      {
        key: 'voivodeship',
        label: this.textService.get('inspections.filters.voivodeship'),
        type: 'text',
        placeholder: this.textService.get('inspections.filters.voivodeshipPlaceholder')
      },
      {
        key: 'distance',
        label: this.textService.get('inspections.filters.distance'),
        type: 'select',
        placeholder: this.textService.get('inspections.filters.noDistanceLimit'),
        options: [
          { value: '10', label: '+10 km' },
          { value: '15', label: '+15 km' },
          { value: '20', label: '+20 km' },
          { value: '30', label: '+30 km' },
          { value: '50', label: '+50 km' }
        ]
      },
      {
        key: 'dateRange',
        label: this.textService.get('inspections.filters.dateRange'),
        type: 'range',
        rangeFromKey: 'dateFrom',
        rangeToKey: 'dateTo',
        rangeFromPlaceholder: this.textService.get('inspections.placeholders.dateFrom'),
        rangeToPlaceholder: this.textService.get('inspections.placeholders.dateTo')
      },
      {
        key: 'sprayerType',
        label: this.textService.get('inspections.filters.sprayerType'),
        type: 'select',
        placeholder: this.textService.get('inspections.filters.allSprayerTypes'),
        options: [
          { value: '00', label: this.textService.get('types.cropSprayers.filters.typeField') },
          { value: '01', label: this.textService.get('types.cropSprayers.filters.typeGarden') }
        ]
      }
    ];
  }

  private initSelectOptions(): void {
    this.resultOptions = [
      { value: '', label: this.textService.get('inspections.filters.allResults') },
      { value: 'true', label: this.textService.get('inspections.filters.resultPositive') },
      { value: 'false', label: this.textService.get('inspections.filters.resultNegative') }
    ];

    this.distanceOptions = [
      { value: '', label: this.textService.get('inspections.filters.noDistanceLimit') },
      { value: '10', label: '+10 km' },
      { value: '15', label: '+15 km' },
      { value: '20', label: '+20 km' },
      { value: '30', label: '+30 km' },
      { value: '50', label: '+50 km' }
    ];
  }

  // List handling

  loadProtocols(): void {
    this.loadingList = true;
    
    // Build search params from filter values
    const params: any = {};
    
    if (this.searchTerm) {
      params.q = this.searchTerm;
    }
    
    if (this.filterValues['year']) {
      // Convert year to date range
      const year = parseInt(this.filterValues['year'], 10);
      params.dateFrom = `${year}-01-01`;
      params.dateTo = `${year}-12-31`;
    } else {
      if (this.filterValues['dateFrom']) {
        params.dateFrom = this.filterValues['dateFrom'];
      }
      if (this.filterValues['dateTo']) {
        params.dateTo = this.filterValues['dateTo'];
      }
    }
    
    if (this.filterValues['finalResult'] !== undefined && this.filterValues['finalResult'] !== '') {
      params.finalResult = this.filterValues['finalResult'] === 'true';
    }
    
    // Note: City/distance/voivodeship filtering would require backend geo-coordinates support
    // For now, we apply client-side filtering for these fields

    this.protocolService.getList(params).subscribe({
      next: list => {
        // Apply client-side filters for city/inspector/voivodeship/sprayerType
        let filtered = list;
        
        if (this.filterValues['inspectorName']) {
          const term = this.filterValues['inspectorName'].toLowerCase();
          filtered = filtered.filter(p => p.inspectorName?.toLowerCase().includes(term));
        }
        
        if (this.filterValues['sprayerType']) {
          filtered = filtered.filter(p => p.cropSprayerType === this.filterValues['sprayerType']);
        }
        
        // City and voivodeship filtering would need client data from backend
        // This is a placeholder for future implementation
        
        this.protocols = filtered;
        this.loadingList = false;
        this.reconcileSelection(filtered);
      },
      error: () => {
        this.loadingList = false;
        this.notificationService.error(this.textService.get('inspections.messages.loadError'));
      }
    });
  }

  onSearchChange(value: string): void {
    this.searchTerm = value;
    this.loadProtocols();
  }

  onFilterChange(values: FilterValues): void {
    this.filterValues = values;
    this.loadProtocols();
  }

  onFilterClear(): void {
    this.filterValues = {};
    this.loadProtocols();
  }

  onFilterPanelOpenChange(isOpen: boolean): void {
    this.filterPanelOpen = isOpen;
  }

  onSelectProtocol(id: number): void {
    if (id === this.selectedProtocolId) {
      return;
    }
    this.loadProtocolDetail(id);
  }

  private loadProtocolDetail(id: number): void {
    this.selectedProtocolId = id;
    this.currentStep = 0;
    this.loadingDetail = true;

    this.protocolService.get(id).subscribe({
      next: detail => {
        this.currentProtocol = detail;
        this.loadingDetail = false;
      },
      error: () => {
        this.loadingDetail = false;
        this.notificationService.error(this.textService.get('inspections.messages.loadDetailError'));
      }
    });
  }

  private reconcileSelection(list: InspectionProtocolListItem[]): void {
    if (list.length === 0) {
      this.selectedProtocolId = null;
      this.currentProtocol = null;
      return;
    }

    const hasSelected = !!this.selectedProtocolId && list.some(p => p.id === this.selectedProtocolId);

    if (hasSelected) {
      if (!this.currentProtocol || this.currentProtocol.id !== this.selectedProtocolId) {
        this.loadProtocolDetail(this.selectedProtocolId!);
      }
      return;
    }

    // Select first item
    this.loadProtocolDetail(list[0].id);
  }

  // Step navigation

  goToStep(index: number): void {
    const step = this.steps.find(s => s.id === index);
    if (step) {
      this.currentStep = step.id;
    }
  }

  // Actions

  openHistory(): void {
    if (this.currentProtocol) {
      this.showHistoryDialog = true;
    }
  }

  onDelete(): void {
    if (!this.currentProtocol || this.deleting) {
      return;
    }
    this.deleteDialogVisible = true;
  }

  onDeleteConfirm(): void {
    this.deleteDialogVisible = false;
    
    if (!this.currentProtocol || this.deleting) {
      return;
    }

    this.deleting = true;
    const idToDelete = this.currentProtocol.id;

    this.protocolService.deleteProtocol(idToDelete).subscribe({
      next: () => {
        this.notificationService.success(this.textService.get('inspections.messages.deleted'));
        this.deleting = false;
        this.currentProtocol = null;
        this.selectedProtocolId = null;
        this.loadProtocols();
      },
      error: err => {
        this.deleting = false;
        const backendMessage = err?.error?.message as string | undefined;
        const message = backendMessage || this.textService.get('inspections.messages.deleteError');
        this.notificationService.error(message);
      }
    });
  }

  onDeleteCancel(): void {
    this.deleteDialogVisible = false;
  }

  // Navigation to new-inspection
  onAddNew(): void {
    this.router.navigate(['/new-inspection']);
  }

  onEdit(): void {
    if (!this.currentProtocol) {
      return;
    }
    this.router.navigate(['/new-inspection'], { queryParams: { edit: this.currentProtocol.id } });
  }

  // Helpers

  getSafeHtml(icon: string): SafeHtml {
    return this.sanitizer.bypassSecurityTrustHtml(icon);
  }

  getResultBadgeClass(result: boolean | null | undefined): string {
    if (result === true) return 'badge-success';
    if (result === false) return 'badge-danger';
    return 'badge-secondary';
  }

  getResultText(result: boolean | null | undefined): string {
    if (result === true) return this.textService.get('inspections.result.positive');
    if (result === false) return this.textService.get('inspections.result.negative');
    return this.textService.get('inspections.result.pending');
  }

  getSprayerTypeText(type: string | null | undefined): string {
    if (type === '00') return this.textService.get('types.cropSprayers.filters.typeField');
    if (type === '01') return this.textService.get('types.cropSprayers.filters.typeGarden');
    return '—';
  }

  getSprayerKindText(kind: string | null | undefined): string {
    switch (kind) {
      case '00': return 'Zawieszany';
      case '01': return 'Przyczepny';
      case '02': return 'Samobieżny';
      case '03': return 'Zamontowany na pojeździe';
      case '10': return 'Zawieszany';
      case '11': return 'Przyczepny';
      case '12': return 'Samobieżny';
      case '13': return 'Zamontowany na pojeździe';
      default: return '—';
    }
  }

  formatDate(dateStr: string | null | undefined): string {
    if (!dateStr) return '—';
    try {
      return new Date(dateStr).toLocaleDateString('pl-PL');
    } catch {
      return dateStr;
    }
  }

  // Check result helpers for detail view
  getCheckResult(value: boolean | null | undefined): string {
    if (value === true) return '✓';
    if (value === false) return '✗';
    return '—';
  }

  getCheckClass(value: boolean | null | undefined): string {
    if (value === true) return 'check-pass';
    if (value === false) return 'check-fail';
    return 'check-pending';
  }

  // View mode toggle
  toggleViewMode(): void {
    this.viewMode = this.viewMode === 'dashboard' ? 'document' : 'dashboard';
  }

  // Accordion section toggle
  toggleSection(section: string): void {
    this.expandedSections[section] = !this.expandedSections[section];
  }

  // Get all inspection check values as array for counting
  private getCheckValues(protocol: InspectionProtocolDetail): (boolean | null | undefined)[] {
    const checks = [
      protocol.generalConditionPassed,
      protocol.markingsReadablePassed,
      protocol.equipmentCompletePassed,
      protocol.pumpOperationPassed,
      protocol.pumpSealingPassed,
      protocol.pressurePulsationPassed,
      protocol.agitatorOperationPassed,
      protocol.tankConditionPassed,
      protocol.tankSealingPassed,
      protocol.levelIndicatorPassed,
      protocol.flushingSystemPassed,
      protocol.manometerPassed,
      protocol.manometerDialSizePassed,
      protocol.pipesConditionPassed,
      protocol.connectionsSealingPassed,
      protocol.suctionFilterPassed,
      protocol.pressureFilterPassed,
      protocol.nozzleFiltersPassed,
      protocol.nozzleUniformityPassed,
      protocol.nozzleFlowRatePassed,
      protocol.nozzleConditionPassed,
      protocol.transverseDistributionPassed
    ];
    
    // Add boom-specific checks based on type
    if (protocol.cropSprayerType === '00') {
      checks.push(
        protocol.fieldBoomConditionPassed,
        protocol.boomStabilityPassed,
        protocol.boomHeightPassed,
        protocol.boomSymmetryPassed
      );
    } else if (protocol.cropSprayerType === '01') {
      checks.push(
        protocol.orchardSprayerConditionPassed,
        protocol.airStreamDirectionPassed
      );
    }
    
    return checks;
  }

  // Count passed checks
  getPassedCount(protocol: InspectionProtocolDetail): number {
    return this.getCheckValues(protocol).filter(v => v === true).length;
  }

  // Count failed checks
  getFailedCount(protocol: InspectionProtocolDetail): number {
    return this.getCheckValues(protocol).filter(v => v === false).length;
  }

  // Count pending checks
  getPendingCount(protocol: InspectionProtocolDetail): number {
    return this.getCheckValues(protocol).filter(v => v === null || v === undefined).length;
  }

  // Get section check values
  private getSectionCheckValues(protocol: InspectionProtocolDetail, section: string): (boolean | null | undefined)[] {
    switch (section) {
      case 'general':
        return [protocol.generalConditionPassed, protocol.markingsReadablePassed, protocol.equipmentCompletePassed];
      case 'pump':
        return [protocol.pumpOperationPassed, protocol.pumpSealingPassed, protocol.pressurePulsationPassed];
      case 'agitation':
        return [protocol.agitatorOperationPassed];
      case 'tank':
        return [protocol.tankConditionPassed, protocol.tankSealingPassed, protocol.levelIndicatorPassed, protocol.flushingSystemPassed];
      case 'measuring':
        return [protocol.manometerPassed, protocol.manometerDialSizePassed];
      case 'piping':
        return [protocol.pipesConditionPassed, protocol.connectionsSealingPassed];
      case 'filtration':
        return [protocol.suctionFilterPassed, protocol.pressureFilterPassed, protocol.nozzleFiltersPassed];
      case 'boom':
        if (protocol.cropSprayerType === '00') {
          return [protocol.fieldBoomConditionPassed, protocol.boomStabilityPassed, protocol.boomHeightPassed, protocol.boomSymmetryPassed];
        } else {
          return [protocol.orchardSprayerConditionPassed, protocol.airStreamDirectionPassed];
        }
      case 'nozzles':
        return [protocol.nozzleUniformityPassed, protocol.nozzleFlowRatePassed, protocol.nozzleConditionPassed];
      case 'distribution':
        return [protocol.transverseDistributionPassed];
      default:
        return [];
    }
  }

  // Get section status class
  getSectionStatus(protocol: InspectionProtocolDetail, section: string): string {
    const checks = this.getSectionCheckValues(protocol, section);
    const hasFailed = checks.some(v => v === false);
    const allPassed = checks.every(v => v === true);
    const hasPending = checks.some(v => v === null || v === undefined);
    
    if (hasFailed) return 'status-fail';
    if (allPassed) return 'status-pass';
    if (hasPending) return 'status-pending';
    return 'status-pending';
  }

  // Get section status icon
  getSectionStatusIcon(protocol: InspectionProtocolDetail, section: string): string {
    const checks = this.getSectionCheckValues(protocol, section);
    const hasFailed = checks.some(v => v === false);
    const allPassed = checks.every(v => v === true);
    
    if (hasFailed) return '✗';
    if (allPassed) return '✓';
    return '○';
  }

  // Get section summary (e.g., "3/3" or "2/3")
  getSectionSummary(protocol: InspectionProtocolDetail, section: string): string {
    const checks = this.getSectionCheckValues(protocol, section);
    const passed = checks.filter(v => v === true).length;
    return `${passed}/${checks.length}`;
  }
}

