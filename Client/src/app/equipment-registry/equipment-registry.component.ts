/*
 * ROBIGOO FIELD SPRAYER CONTROL STATION
 * Copyright (c) 2025 Wojciech Salamon <wojciech.salamon@yahoo.com>
 * All rights reserved. Unauthorized distribution or disclosure is prohibited.
*/

import { Component, Input, OnInit } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { DomSanitizer, SafeHtml } from '@angular/platform-browser';
import { SVG_ICONS } from '../shared/svg-icons';
import { TextService } from '../services/text.service';

export interface RegistryRow {
  lp: number;
  protocolNumber: string;
  inspectionDate: string;
  owner: string;
  ownerAddress: string;
  ownerTaxId: string;
  sprayerType: string;
  sprayerKind: string;
  sprayerName: string;
  manufacturer: string;
  serialNumber: string;
  productionYear: string;
  result: string;
  stickerNumber: string;
  validUntil: string;
  inspector: string;
}

interface RegistryApiItem {
  id: number;
  protocolNumber: string;
  inspectionDate: string;
  inspectorName: string;
  clientName?: string | null;
  clientAddress?: string | null;
  clientTaxId?: string | null;
  cropSprayerType?: string | null;
  cropSprayerKind?: string | null;
  cropSprayerManufacturer?: string | null;
  cropSprayerSerialNumber?: string | null;
  cropSprayerName?: string | null;
  cropSprayerProductionYear?: string | null;
  finalResult?: boolean | null;
  controlStickerNumber?: string | null;
  validUntil?: string | null;
}

@Component({
  selector: 'app-equipment-registry',
  templateUrl: './equipment-registry.component.html',
  styleUrls: ['./equipment-registry.component.css']
})
export class EquipmentRegistryComponent implements OnInit {
  @Input() instanceId?: number;

  dateFrom = '';
  dateTo = '';
  rows: RegistryRow[] = [];
  loading = false;
  generated = false;
  isPrintPreview = false;

  // Station info for print header
  stationName = '';
  stationAddress = '';

  iconPrintSafe: SafeHtml = '';
  iconSearchSafe: SafeHtml = '';
  iconRefreshSafe: SafeHtml = '';

  constructor(
    private http: HttpClient,
    private sanitizer: DomSanitizer,
    private textService: TextService
  ) {}

  ngOnInit(): void {
    this.iconPrintSafe = this.sanitizer.bypassSecurityTrustHtml(SVG_ICONS.iconPdf);
    this.iconSearchSafe = this.sanitizer.bypassSecurityTrustHtml(SVG_ICONS.iconSearch);
    this.iconRefreshSafe = this.sanitizer.bypassSecurityTrustHtml(SVG_ICONS.iconRefresh);

    // Set default date range: start of current year to today
    const now = new Date();
    const year = now.getFullYear();
    this.dateFrom = `${year}-01-01`;
    this.dateTo = now.toISOString().split('T')[0];
  }

  generate(): void {
    if (!this.dateFrom || !this.dateTo) return;
    this.loading = true;
    this.generated = false;

    let params = new HttpParams();
    params = params.set('dateFrom', this.dateFrom);
    params = params.set('dateTo', this.dateTo);

    this.http.get<RegistryApiItem[]>('/api/inspection-protocols/registry', { params }).subscribe({
      next: (list) => {
        this.rows = list.map((p, i) => this.toRow(p, i + 1));
        this.generated = true;
        this.loading = false;
      },
      error: () => {
        this.rows = [];
        this.generated = true;
        this.loading = false;
      }
    });
  }

  openPrintPreview(): void {
    this.isPrintPreview = true;
  }

  closePrintPreview(): void {
    this.isPrintPreview = false;
  }

  printRegistry(): void {
    setTimeout(() => window.print(), 100);
  }

  private toRow(p: RegistryApiItem, index: number): RegistryRow {
    return {
      lp: index,
      protocolNumber: p.protocolNumber || '',
      inspectionDate: p.inspectionDate ? this.formatDate(p.inspectionDate) : '',
      owner: p.clientName || '',
      ownerAddress: p.clientAddress || '',
      ownerTaxId: p.clientTaxId || '',
      sprayerType: this.mapType(p.cropSprayerType),
      sprayerKind: this.mapKind(p.cropSprayerKind),
      sprayerName: p.cropSprayerName || '',
      manufacturer: p.cropSprayerManufacturer || '',
      serialNumber: p.cropSprayerSerialNumber || '',
      productionYear: p.cropSprayerProductionYear || '',
      result: this.mapResult(p.finalResult),
      stickerNumber: p.controlStickerNumber || '',
      validUntil: p.validUntil ? this.formatDate(p.validUntil) : '',
      inspector: p.inspectorName || ''
    };
  }

  private formatDate(dateStr: string): string {
    if (!dateStr) return '';
    const d = new Date(dateStr);
    if (isNaN(d.getTime())) return dateStr;
    const dd = String(d.getDate()).padStart(2, '0');
    const mm = String(d.getMonth() + 1).padStart(2, '0');
    const yyyy = d.getFullYear();
    return `${dd}.${mm}.${yyyy}`;
  }

  private mapType(type?: string | null): string {
    if (type === '00') return this.textService.get('registry.typeField');
    if (type === '01') return this.textService.get('registry.typeGarden');
    return '';
  }

  private mapKind(kind?: string | null): string {
    if (kind === '00') return this.textService.get('registry.kindMounted');
    if (kind === '01') return this.textService.get('registry.kindTrailed');
    if (kind === '02') return this.textService.get('registry.kindSelfPropelled');
    if (kind === '03') return this.textService.get('registry.kindOther');
    return '';
  }

  private mapResult(result?: boolean | null): string {
    if (result === true) return this.textService.get('registry.resultPositive');
    if (result === false) return this.textService.get('registry.resultNegative');
    return this.textService.get('registry.resultPending');
  }

  formatDateRange(): string {
    return `${this.formatDate(this.dateFrom)} – ${this.formatDate(this.dateTo)}`;
  }
}
