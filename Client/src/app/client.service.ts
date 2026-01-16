/*
 * ROBIGOO FIELD SPRAYER CONTROL STATION
 * Client Service - Manages customer/client data via backend API
 * Copyright (c) 2025 Wojciech Salamon <wojciech.salamon@yahoo.com>
 * All rights reserved. Unauthorized distribution or disclosure is prohibited.
*/

import { Injectable } from '@angular/core';
import { GenericCrudService } from './shared/services/generic-crud.service';
import { Observable } from 'rxjs';

export interface ClientListItem {
  id: string;
  clientType: 'person' | 'company';
  displayName: string;
  // Person fields
  firstName?: string;
  lastName?: string;
  pesel?: string;
  // Company fields
  companyName?: string;
  nip?: string;
  regon?: string;
  // Address
  voivodeship?: string;
  city?: string;
  street?: string;
  buildingNumber?: string;
  apartmentNumber?: string;
  zipCode?: string;
  createdAt: string;
}

export interface ClientDetail {
  id: string;
  clientType: 'person' | 'company';
  // Person fields
  firstName: string;
  lastName: string;
  pesel: string;
  // Company fields
  companyName: string;
  nip: string;
  regon: string;
  // Address
  voivodeship: string;
  city: string;
  street: string;
  buildingNumber: string;
  apartmentNumber: string;
  zipCode: string;
  // Timestamps
  createdAt: string;
  updatedAt?: string | null;
}

export interface ClientCreateUpdateRequest {
  clientType: 'person' | 'company';
  // Person fields
  firstName: string;
  lastName: string;
  pesel: string;
  // Company fields
  companyName: string;
  nip: string;
  regon: string;
  // Address
  voivodeship: string;
  city: string;
  street: string;
  buildingNumber: string;
  apartmentNumber: string;
  zipCode: string;
}

/** Search parameters for clients list */
export interface ClientSearchParams {
  q?: string;
  clientType?: string;
  city?: string;
}

@Injectable({ providedIn: 'root' })
export class ClientService extends GenericCrudService<any> {
  private readonly endpoint = 'clients';

  /** Get list of clients with optional filters */
  getClientsList(params?: ClientSearchParams): Observable<ClientListItem[]> {
    return this.search(this.endpoint, params || {}) as unknown as Observable<ClientListItem[]>;
  }

  /** Get single client by ID */
  getClient(id: string): Observable<ClientDetail> {
    return this.getById(this.endpoint, id) as unknown as Observable<ClientDetail>;
  }

  /** Create a new client */
  createClient(req: ClientCreateUpdateRequest): Observable<ClientDetail> {
    return this.create(this.endpoint, req) as unknown as Observable<ClientDetail>;
  }

  /** Update an existing client */
  updateClient(id: string, req: ClientCreateUpdateRequest): Observable<ClientDetail> {
    return this.update(this.endpoint, id, req) as unknown as Observable<ClientDetail>;
  }

  /** Delete a client */
  deleteClient(id: string): Observable<void> {
    return this.delete(this.endpoint, id);
  }
}
