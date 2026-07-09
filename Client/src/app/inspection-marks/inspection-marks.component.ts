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

interface MarkRow {
  lp: number;
  stickerNumber: string;
  issueDate: string;
  owner: string;
  protocolNumber: string;
}

interface MarkApiItem {
  id: number;
  controlStickerNumber?: string | null;
  inspectionDate: string;
  clientName?: string | null;
  protocolNumber: string;
}

@Component({
  selector: 'app-inspection-marks',
  templateUrl: './inspection-marks.component.html',
  styleUrls: ['./inspection-marks.component.css']
})
export class InspectionMarksComponent implements OnInit {
  @Input() instanceId?: number;

  dateFrom = '';
  dateTo = '';
  rows: MarkRow[] = [];
  loading = false;
  generated = false;
  isPrintPreview = false;

  iconPrintSafe: SafeHtml = '';
  iconSearchSafe: SafeHtml = '';

  constructor(
    private http: HttpClient,
    private sanitizer: DomSanitizer,
    private textService: TextService
  ) {}

  ngOnInit(): void {
    this.iconPrintSafe = this.sanitizer.bypassSecurityTrustHtml(SVG_ICONS.iconPdf);
    this.iconSearchSafe = this.sanitizer.bypassSecurityTrustHtml(SVG_ICONS.iconSearch);

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

    this.http.get<MarkApiItem[]>('/api/inspection-protocols/control-marks', { params }).subscribe({
      next: (list) => {
        this.rows = list.map((item, i) => this.toRow(item, i + 1));
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

  printMarks(): void {
    setTimeout(() => window.print(), 100);
  }

  formatDateRange(): string {
    return `${this.formatDate(this.dateFrom)} – ${this.formatDate(this.dateTo)}`;
  }

  private toRow(item: MarkApiItem, index: number): MarkRow {
    return {
      lp: index,
      stickerNumber: item.controlStickerNumber || '',
      issueDate: item.inspectionDate ? this.formatDate(item.inspectionDate) : '',
      owner: item.clientName || '',
      protocolNumber: item.protocolNumber || ''
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
}

