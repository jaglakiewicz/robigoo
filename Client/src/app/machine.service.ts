/*
 * ROBIGOO FIELD SPRAYER CONTROL STATION
 * Copyright (c) 2025 Wojciech Salamon <wojciech.salamon@yahoo.com>
 * All rights reserved. Unauthorized distribution or disclosure is prohibited.
*/

import { Injectable } from '@angular/core';
import { GenericCrudService } from './shared/services/generic-crud.service';
import { Observable } from 'rxjs';

export interface MachineListItem {
  serialNumber: string;
  sprayerName: string;
  manufacturer: string;
  productionYear: string;
  type: string;
  kind: string;
  createdAt: string;
}

export interface MachineDetail {
  serialNumber: string;
  sprayerName: string;
  type: string;
  kind: string;
  manufacturer: string;
  productionYear: string;
  purchaseDate?: string | null;
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

export interface MachineCreateUpdateRequest {
  serialNumber: string;
  sprayerName: string;
  type: string;
  kind: string;
  manufacturer: string;
  productionYear: string;
  purchaseDate?: string | null;
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

@Injectable({ providedIn: 'root' })
export class MachineService extends GenericCrudService<any> {
  private readonly endpoint = 'machines';

  getMachinesList(params?: {
    q?: string;
    type?: string;
    kind?: string;
    manufacturer?: string;
    yearFrom?: string;
    yearTo?: string;
  }): Observable<MachineListItem[]> {
    return this.search(this.endpoint, params || {}) as unknown as Observable<MachineListItem[]>;
  }

  getMachine(serialNumber: string): Observable<MachineDetail> {
    return this.getById(this.endpoint, serialNumber) as unknown as Observable<MachineDetail>;
  }

  createMachine(req: MachineCreateUpdateRequest): Observable<MachineDetail> {
    return this.create(this.endpoint, req) as unknown as Observable<MachineDetail>;
  }

  updateMachine(serialNumber: string, req: MachineCreateUpdateRequest): Observable<MachineDetail> {
    return this.update(this.endpoint, serialNumber, req) as unknown as Observable<MachineDetail>;
  }
}
