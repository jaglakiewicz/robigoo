/*
 * ROBIGOO FIELD SPRAYER CONTROL STATION
 * Copyright (c) 2025 Wojciech Salamon <wojciech.salamon@yahoo.com>
 * All rights reserved. Unauthorized distribution or disclosure is prohibited.
*/

import { Injectable } from '@angular/core';
import { GenericCrudService } from './shared/services/generic-crud.service';
import { Observable } from 'rxjs';

export interface InspectionItemDto { description: string; passed: boolean; }
export interface InspectionCreateDto { vehiclePlate: string; inspectorName: string; inspectionDate: string; notes?: string; items: InspectionItemDto[] }
export interface Inspection { id: number; vehiclePlate: string; inspectorName: string; inspectionDate: string; notes?: string; items: InspectionItemDto[] }

@Injectable({ providedIn: 'root' })
export class InspectionService extends GenericCrudService<Inspection> {
  private readonly endpoint = 'inspections';

  createInspection(dto: InspectionCreateDto): Observable<Inspection> {
    return super.create(this.endpoint, dto);
  }

  getAllInspections(): Observable<Inspection[]> {
    return super.getAll(this.endpoint);
  }

  getInspectionById(id: number): Observable<Inspection> {
    return super.getById(this.endpoint, id);
  }

  updateInspection(id: number, data: any): Observable<Inspection> {
    return super.update(this.endpoint, id, data);
  }

  deleteInspection(id: number): Observable<void> {
    return super.delete(this.endpoint, id);
  }
}

