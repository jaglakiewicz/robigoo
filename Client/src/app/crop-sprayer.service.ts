/*
 * ROBIGOO FIELD SPRAYER CONTROL STATION
 * Crop Sprayer Service
 * Copyright (c) 2025 Wojciech Salamon <wojciech.salamon@yahoo.com>
 * All rights reserved. Unauthorized distribution or disclosure is prohibited.
*/

import { Injectable } from '@angular/core';
import { GenericCrudService } from './shared/services/generic-crud.service';
import { HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';

/** Crop sprayer list item for display in lists */
export interface CropSprayerListItem {
  serialNumber: string;
  sprayerName: string;
  manufacturer: string;
  productionYear: string;
  type: string;
  kind: string;
  ownerId?: string | null;
  ownerName?: string | null;
  createdAt: string;
}

/** Full crop sprayer details */
export interface CropSprayerDetail {
  serialNumber: string;
  sprayerName: string;
  type: string;
  kind: string;
  manufacturer: string;
  productionYear: string;
  purchaseDate?: string | null;
  ownerId?: string | null;
  ownerName?: string | null;
  pumpPiston: boolean;
  pumpDiaphragm: boolean;
  pumpOther: boolean;
  pumpOtherType?: string | null;
  pumpFlowRate?: number | null;
  tankCapacity?: number | null;
  hasFlushing: boolean;
  hasDiluter: boolean;
  hasWashingDevice: boolean;
  hasManometer: boolean;
  hasComputer: boolean;
  boomWidth?: number | null;
  boomWet: boolean;
  boomDry: boolean;
  boomDampeningMechanism: boolean;
  sectionCount?: number | null;
  nozzlesFieldFeatures?: string | null;
  nozzlesGardenFeatures?: string | null;
  fanType?: string | null;
  createdAt: string;
  updatedAt?: string | null;
}

/** Request payload for creating/updating a crop sprayer */
export interface CropSprayerCreateUpdateRequest {
  serialNumber: string;
  sprayerName: string;
  type: string;
  kind: string;
  manufacturer: string;
  productionYear: string;
  purchaseDate?: string | null;
  ownerId?: string | null;
  ownerName?: string | null;
  pumpPiston: boolean;
  pumpDiaphragm: boolean;
  pumpOther: boolean;
  pumpOtherType?: string | null;
  pumpFlowRate?: number | null;
  tankCapacity?: number | null;
  hasFlushing: boolean;
  hasDiluter: boolean;
  hasWashingDevice: boolean;
  hasManometer: boolean;
  hasComputer: boolean;
  boomWidth?: number | null;
  boomWet: boolean;
  boomDry: boolean;
  boomDampeningMechanism: boolean;
  sectionCount?: number | null;
  nozzlesFieldFeatures?: string | null;
  nozzlesGardenFeatures?: string | null;
  fanType?: string | null;
}

/** Search parameters for crop sprayers list */
export interface CropSprayerSearchParams {
  q?: string;
  type?: string;
  kind?: string;
  manufacturer?: string;
  yearFrom?: string;
  yearTo?: string;
  ownerId?: string;
}

@Injectable({ providedIn: 'root' })
export class CropSprayerService extends GenericCrudService<any> {
  private readonly endpoint = 'machines';

  /** Get list of crop sprayers with optional filters */
  getList(params?: CropSprayerSearchParams): Observable<CropSprayerListItem[]> {
    return this.search(this.endpoint, params || {}) as unknown as Observable<CropSprayerListItem[]>;
  }

  /** Get crop sprayers owned by a specific client */
  getByOwner(ownerId: string): Observable<CropSprayerListItem[]> {
    return this.getList({ ownerId });
  }

  /** Get single crop sprayer by serial number */
  get(serialNumber: string): Observable<CropSprayerDetail> {
    return this.getById(this.endpoint, serialNumber) as unknown as Observable<CropSprayerDetail>;
  }

  /** Create a new crop sprayer */
  createSprayer(req: CropSprayerCreateUpdateRequest): Observable<CropSprayerDetail> {
    return this.create(this.endpoint, req) as unknown as Observable<CropSprayerDetail>;
  }

  /** Update an existing crop sprayer */
  updateSprayer(serialNumber: string, req: CropSprayerCreateUpdateRequest): Observable<CropSprayerDetail> {
    return this.update(this.endpoint, serialNumber, req) as unknown as Observable<CropSprayerDetail>;
  }

  /** Delete a crop sprayer */
  deleteSprayer(serialNumber: string): Observable<void> {
    return this.delete(this.endpoint, serialNumber);
  }

  /** Get distinct values for a field (for autosuggestions) */
  getSuggestions(field: string): Observable<string[]> {
    const params = new HttpParams().set('field', field);
    return this.http.get<string[]>(`/api/${this.endpoint}/suggestions`, { params });
  }
}
