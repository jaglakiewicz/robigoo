/*
 * ROBIGOO FIELD SPRAYER CONTROL STATION
 * Copyright (c) 2025 Wojciech Salamon <wojciech.salamon@yahoo.com>
 * All rights reserved. Unauthorized distribution or disclosure is prohibited.
*/

import { Component, EventEmitter, Output } from '@angular/core';
import { InspectionService, InspectionCreateDto } from '../inspection.service';
import { TranslationService } from '../i18n/translation.service';

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

  iconBack = '<svg fill="currentColor" viewBox="0 0 24 24"><path d="M20 11H7.83l5.59-5.59L12 4l-8 8 8 8 1.41-1.41L7.83 13H20v-2z"/></svg>';
  iconNext = '<svg fill="currentColor" viewBox="0 0 24 24"><path d="M12 4l-1.41 1.41L16.17 11H4v2h12.17l-5.58 5.59L12 20l8-8z"/></svg>';
  iconSave = '<svg fill="currentColor" viewBox="0 0 24 24"><path d="M17 3H5c-1.1 0-2 .9-2 2v14c0 1.1.9 2 2 2h14c1.1 0 2-.9 2-2V7l-4-4zm-5 16c-1.1 0-2-.9-2-2s.9-2 2-2 2 .9 2 2-.9 2-2 2zm3-10H5V5h10v4z"/></svg>';
  iconCancel = '<svg fill="currentColor" viewBox="0 0 24 24"><path d="M19 6.41L17.59 5 12 10.59 6.41 5 5 6.41 10.59 12 5 17.59 6.41 19 12 13.41 17.59 19 19 17.59 13.41 12 19 6.41z"/></svg>';
  iconAdd = '<svg fill="currentColor" viewBox="0 0 24 24"><path d="M19 13h-6v6h-2v-6H5v-2h6V5h2v6h6v2z"/></svg>';
  iconRemove = '<svg fill="currentColor" viewBox="0 0 24 24"><path d="M6 19c0 1.1.9 2 2 2h8c1.1 0 2-.9 2-2V7H6v12zM19 4h-3.5l-1-1h-9l-1 1H5v2h14V4z"/></svg>';

  constructor(private svc: InspectionService, private translation: TranslationService) {
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
      { description: this.translation.translate('newInspection.defaults.suspension.springs'), passed: true },
      { description: this.translation.translate('newInspection.defaults.suspension.shocks'), passed: true },
      { description: this.translation.translate('newInspection.defaults.suspension.controlArms'), passed: true }
    ];
    this.alignmentItems = [
      { description: this.translation.translate('newInspection.defaults.alignment.camber'), passed: true },
      { description: this.translation.translate('newInspection.defaults.alignment.caster'), passed: true },
      { description: this.translation.translate('newInspection.defaults.alignment.toe'), passed: true }
    ];
    this.lightsItems = [
      { description: this.translation.translate('newInspection.defaults.lights.headlights'), passed: true },
      { description: this.translation.translate('newInspection.defaults.lights.tailLights'), passed: true },
      { description: this.translation.translate('newInspection.defaults.lights.turnSignals'), passed: true }
    ];
    this.brakesItems = [
      { description: this.translation.translate('newInspection.defaults.brakes.front'), passed: true },
      { description: this.translation.translate('newInspection.defaults.brakes.rear'), passed: true },
      { description: this.translation.translate('newInspection.defaults.brakes.fluid'), passed: true }
    ];
  }
}

