/*
 * ROBIGOO FIELD SPRAYER CONTROL STATION
 * Inspection Protocol Service
 * Copyright (c) 2025 Wojciech Salamon <wojciech.salamon@yahoo.com>
 * All rights reserved. Unauthorized distribution or disclosure is prohibited.
*/

import { Injectable } from '@angular/core';
import { GenericCrudService } from './shared/services/generic-crud.service';
import { Observable } from 'rxjs';

/** List item DTO for inspection protocols */
export interface InspectionProtocolListItem {
  id: number;
  protocolNumber: string;
  inspectionDate: string;
  inspectorName: string;
  clientName?: string | null;
  cropSprayerName?: string | null;
  cropSprayerSerialNumber?: string | null;
  cropSprayerType?: string | null;
  finalResult?: boolean | null;
  validUntil?: string | null;
  createdAt: string;
}

/** Full inspection protocol detail */
export interface InspectionProtocolDetail {
  id: number;
  protocolNumber: string;
  inspectionDate: string;
  inspectionLocation?: string | null;
  inspectorName: string;
  inspectorLicenseNumber?: string | null;

  // Client
  clientId?: string | null;
  clientName?: string | null;
  clientAddress?: string | null;
  clientTaxId?: string | null;

  // CropSprayer
  cropSprayerSerialNumber?: string | null;
  cropSprayerName?: string | null;
  cropSprayerType?: string | null;
  cropSprayerKind?: string | null;
  cropSprayerManufacturer?: string | null;
  cropSprayerProductionYear?: string | null;
  tankCapacity?: number | null;
  boomWidth?: number | null;
  sectionCount?: number | null;

  // Section 1: General
  generalConditionPassed?: boolean | null;
  markingsReadablePassed?: boolean | null;
  equipmentCompletePassed?: boolean | null;
  generalSectionNotes?: string | null;

  // Section 2: Pump
  pumpOperationPassed?: boolean | null;
  pumpSealingPassed?: boolean | null;
  pressurePulsationPassed?: boolean | null;
  pumpSectionNotes?: string | null;

  // Section 3: Agitation
  agitatorOperationPassed?: boolean | null;
  agitatorSectionNotes?: string | null;

  // Section 4: Tank
  tankConditionPassed?: boolean | null;
  tankSealingPassed?: boolean | null;
  levelIndicatorPassed?: boolean | null;
  flushingSystemPassed?: boolean | null;
  tankSectionNotes?: string | null;

  // Section 5: Measuring
  manometerPassed?: boolean | null;
  manometerReading2Bar?: number | null;
  manometerReading4Bar?: number | null;
  manometerReading6Bar?: number | null;
  manometerDialSizePassed?: boolean | null;
  measuringSectionNotes?: string | null;

  // Section 6: Piping
  pipesConditionPassed?: boolean | null;
  connectionsSealingPassed?: boolean | null;
  pipingSectionNotes?: string | null;

  // Section 7: Filtration
  suctionFilterPassed?: boolean | null;
  pressureFilterPassed?: boolean | null;
  nozzleFiltersPassed?: boolean | null;
  filtrationSectionNotes?: string | null;

  // Section 8: Boom
  fieldBoomConditionPassed?: boolean | null;
  boomStabilityPassed?: boolean | null;
  boomHeightPassed?: boolean | null;
  boomSymmetryPassed?: boolean | null;
  orchardSprayerConditionPassed?: boolean | null;
  airStreamDirectionPassed?: boolean | null;
  boomSectionNotes?: string | null;

  // Section 9: Nozzles
  nozzleUniformityPassed?: boolean | null;
  nozzleFlowRatePassed?: boolean | null;
  nozzleConditionPassed?: boolean | null;
  nozzleMeasurements?: string | null;
  nozzlesSectionNotes?: string | null;

  // Section 10: Distribution
  transverseDistributionPassed?: boolean | null;
  coefficientOfVariation?: number | null;
  distributionSectionNotes?: string | null;

  // Final
  finalResult?: boolean | null;
  validUntil?: string | null;
  controlStickerNumber?: string | null;
  generalNotes?: string | null;

  createdAt: string;
  updatedAt?: string | null;
}

/** Request payload for creating/updating an inspection protocol */
export interface InspectionProtocolCreateUpdateRequest {
  inspectionDate: string;
  inspectionLocation?: string | null;
  inspectorName: string;
  inspectorLicenseNumber?: string | null;

  // Client
  clientId?: string | null;
  clientName?: string | null;
  clientAddress?: string | null;
  clientTaxId?: string | null;

  // CropSprayer
  cropSprayerSerialNumber?: string | null;
  cropSprayerName?: string | null;
  cropSprayerType?: string | null;
  cropSprayerKind?: string | null;
  cropSprayerManufacturer?: string | null;
  cropSprayerProductionYear?: string | null;
  tankCapacity?: number | null;
  boomWidth?: number | null;
  sectionCount?: number | null;

  // Section 1
  generalConditionPassed?: boolean | null;
  markingsReadablePassed?: boolean | null;
  equipmentCompletePassed?: boolean | null;
  generalSectionNotes?: string | null;

  // Section 2
  pumpOperationPassed?: boolean | null;
  pumpSealingPassed?: boolean | null;
  pressurePulsationPassed?: boolean | null;
  pumpSectionNotes?: string | null;

  // Section 3
  agitatorOperationPassed?: boolean | null;
  agitatorSectionNotes?: string | null;

  // Section 4
  tankConditionPassed?: boolean | null;
  tankSealingPassed?: boolean | null;
  levelIndicatorPassed?: boolean | null;
  flushingSystemPassed?: boolean | null;
  tankSectionNotes?: string | null;

  // Section 5
  manometerPassed?: boolean | null;
  manometerReading2Bar?: number | null;
  manometerReading4Bar?: number | null;
  manometerReading6Bar?: number | null;
  manometerDialSizePassed?: boolean | null;
  measuringSectionNotes?: string | null;

  // Section 6
  pipesConditionPassed?: boolean | null;
  connectionsSealingPassed?: boolean | null;
  pipingSectionNotes?: string | null;

  // Section 7
  suctionFilterPassed?: boolean | null;
  pressureFilterPassed?: boolean | null;
  nozzleFiltersPassed?: boolean | null;
  filtrationSectionNotes?: string | null;

  // Section 8
  fieldBoomConditionPassed?: boolean | null;
  boomStabilityPassed?: boolean | null;
  boomHeightPassed?: boolean | null;
  boomSymmetryPassed?: boolean | null;
  orchardSprayerConditionPassed?: boolean | null;
  airStreamDirectionPassed?: boolean | null;
  boomSectionNotes?: string | null;

  // Section 9
  nozzleUniformityPassed?: boolean | null;
  nozzleFlowRatePassed?: boolean | null;
  nozzleConditionPassed?: boolean | null;
  nozzleMeasurements?: string | null;
  nozzlesSectionNotes?: string | null;

  // Section 10
  transverseDistributionPassed?: boolean | null;
  coefficientOfVariation?: number | null;
  distributionSectionNotes?: string | null;

  // Final
  finalResult?: boolean | null;
  validUntil?: string | null;
  controlStickerNumber?: string | null;
  generalNotes?: string | null;
}

/** Search parameters for protocols list */
export interface InspectionProtocolSearchParams {
  q?: string;
  clientId?: string;
  cropSprayerSerialNumber?: string;
  dateFrom?: string;
  dateTo?: string;
  finalResult?: boolean;
}

@Injectable({ providedIn: 'root' })
export class InspectionProtocolService extends GenericCrudService<any> {
  private readonly endpoint = 'inspection-protocols';

  /** Get list of protocols with optional filters */
  getList(params?: InspectionProtocolSearchParams): Observable<InspectionProtocolListItem[]> {
    return this.search(this.endpoint, params || {}) as unknown as Observable<InspectionProtocolListItem[]>;
  }

  /** Get single protocol by ID */
  get(id: number): Observable<InspectionProtocolDetail> {
    return this.getById(this.endpoint, id) as unknown as Observable<InspectionProtocolDetail>;
  }

  /** Get next protocol number */
  getNextNumber(): Observable<{ protocolNumber: string }> {
    return this.http.get<{ protocolNumber: string }>(`${this.baseUrl}/${this.endpoint}/next-number`);
  }

  /** Create a new protocol */
  createProtocol(req: InspectionProtocolCreateUpdateRequest): Observable<InspectionProtocolDetail> {
    return this.create(this.endpoint, req) as unknown as Observable<InspectionProtocolDetail>;
  }

  /** Update an existing protocol */
  updateProtocol(id: number, req: InspectionProtocolCreateUpdateRequest): Observable<InspectionProtocolDetail> {
    return this.update(this.endpoint, id, req) as unknown as Observable<InspectionProtocolDetail>;
  }

  /** Delete a protocol */
  deleteProtocol(id: number): Observable<void> {
    return this.delete(this.endpoint, id);
  }
}
