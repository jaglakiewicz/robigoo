/*
 * ROBIGOO FIELD SPRAYER CONTROL STATION
 * Copyright (c) 2025 Wojciech Salamon <wojciech.salamon@yahoo.com>
 * All rights reserved. Unauthorized distribution or disclosure is prohibited.
*/

import { Component, EventEmitter, Output, OnDestroy } from '@angular/core';
import { DomSanitizer, SafeHtml } from '@angular/platform-browser';
import { InspectionService, InspectionCreateDto } from '../inspection.service';
import { TextService } from '../services/text.service';
import { SVG_ICONS } from '../shared/svg-icons';

interface Section {
  id: number;
  name: string;
  titleKey: string;
}

@Component({
  selector: 'app-new-inspection',
  templateUrl: './new-inspection.component.html',
  styleUrls: ['./new-inspection.component.css']
})
export class NewInspectionComponent {
  @Output() close = new EventEmitter<void>();

  currentStep = 0;
  
  sections: Section[] = [
    { id: 0, name: 'vehicle', titleKey: 'newInspection.sections.vehicle' },
    { id: 1, name: 'suspension', titleKey: 'newInspection.sections.suspension' },
    { id: 2, name: 'alignment', titleKey: 'newInspection.sections.alignment' },
    { id: 3, name: 'lights', titleKey: 'newInspection.sections.lights' },
    { id: 4, name: 'brakes', titleKey: 'newInspection.sections.brakes' }
  ];

  vehiclePlate = '';
  inspectorName = '';
  inspectionDate = new Date().toISOString().slice(0,10);
  notes = '';
  
  suspensionItems: { description: string; passed: boolean }[] = [];
  alignmentItems: { description: string; passed: boolean }[] = [];
  lightsItems: { description: string; passed: boolean }[] = [];
  brakesItems: { description: string; passed: boolean }[] = [];

  saving = false;
  messageKey = '';

  SVG_ICONS = SVG_ICONS;

  iconBack = SVG_ICONS.iconBack;
  iconNext = SVG_ICONS.iconNext;
  iconSave = SVG_ICONS.iconSave;
  iconCancel = SVG_ICONS.iconCancel;
  iconAdd = SVG_ICONS.iconAdd;

  constructor(private svc: InspectionService, private textService: TextService, private sanitizer: DomSanitizer) {
    this.seedDefaults();
  }

  previousStep() { if (this.currentStep > 0) this.currentStep--; }
  nextStep() { if (this.currentStep < this.sections.length - 1) this.currentStep++; }
  goToStep(index: number) { this.currentStep = index; }
  
  addItem(items: any[]) { items.push({ description: '', passed: false }); }
  removeItem(items: any[], i: number) { items.splice(i, 1); }

  async submit() {
    this.saving = true;
    this.messageKey = '';
    const allItems = [
      ...this.suspensionItems,
      ...this.alignmentItems,
      ...this.lightsItems,
      ...this.brakesItems
    ];
    const fallbackValue = '-';
    const dto: InspectionCreateDto = {
      vehiclePlate: this.vehiclePlate || fallbackValue,
      inspectorName: this.inspectorName || fallbackValue,
      inspectionDate: this.inspectionDate,
      notes: this.notes,
      items: allItems.map(i => ({ description: i.description || fallbackValue, passed: i.passed }))
    };
    this.svc.createInspection(dto).subscribe({
      next: () => {
        this.messageKey = 'newInspection.messages.saved';
        this.saving = false;
        setTimeout(() => this.close.emit(), 800);
      },
      error: () => {
        this.messageKey = 'newInspection.messages.error';
        this.saving = false;
      }
    });
  }

  private seedDefaults() {
    this.suspensionItems = [
      { description: this.textService.get('newInspection.defaults.suspension.springs'), passed: true },
      { description: this.textService.get('newInspection.defaults.suspension.shocks'), passed: true },
      { description: this.textService.get('newInspection.defaults.suspension.controlArms'), passed: true }
    ];
    this.alignmentItems = [
      { description: this.textService.get('newInspection.defaults.alignment.camber'), passed: true },
      { description: this.textService.get('newInspection.defaults.alignment.caster'), passed: true },
      { description: this.textService.get('newInspection.defaults.alignment.toe'), passed: true }
    ];
    this.lightsItems = [
      { description: this.textService.get('newInspection.defaults.lights.headlights'), passed: true },
      { description: this.textService.get('newInspection.defaults.lights.tailLights'), passed: true },
      { description: this.textService.get('newInspection.defaults.lights.turnSignals'), passed: true }
    ];
    this.brakesItems = [
      { description: this.textService.get('newInspection.defaults.brakes.front'), passed: true },
      { description: this.textService.get('newInspection.defaults.brakes.rear'), passed: true },
      { description: this.textService.get('newInspection.defaults.brakes.fluid'), passed: true }
    ];
  }

  getSafeHtml(html: string): SafeHtml {
    return this.sanitizer.bypassSecurityTrustHtml(html);
  }

  ngOnDestroy(): void {
    // Cleanup if needed
  }
}

