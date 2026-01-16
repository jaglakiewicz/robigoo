/*
 * ROBIGOO FIELD SPRAYER CONTROL STATION
 * Copyright (c) 2025 Wojciech Salamon <wojciech.salamon@yahoo.com>
 * All rights reserved. Unauthorized distribution or disclosure is prohibited.
*/

import { Injectable } from '@angular/core';
import { GenericCrudService } from './shared/services/generic-crud.service';
import { Observable, of } from 'rxjs';
import { delay } from 'rxjs/operators';

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

// Mock data for development - will be replaced with actual API calls
const MOCK_CLIENTS: ClientListItem[] = [
  {
    id: '1',
    clientType: 'person',
    displayName: 'Jan Kowalski',
    firstName: 'Jan',
    lastName: 'Kowalski',
    pesel: '85010112345',
    voivodeship: 'mazowieckie',
    city: 'Warszawa',
    street: 'ul. Marszałkowska',
    buildingNumber: '10',
    apartmentNumber: '5',
    zipCode: '00-001',
    createdAt: '2025-01-10T10:00:00Z'
  },
  {
    id: '2',
    clientType: 'company',
    displayName: 'Agro-Tech Sp. z o.o.',
    companyName: 'Agro-Tech Sp. z o.o.',
    nip: '1234567890',
    regon: '123456789',
    voivodeship: 'wielkopolskie',
    city: 'Poznań',
    street: 'ul. Rolnicza',
    buildingNumber: '25',
    zipCode: '60-001',
    createdAt: '2025-01-08T14:30:00Z'
  },
  {
    id: '3',
    clientType: 'person',
    displayName: 'Anna Nowak',
    firstName: 'Anna',
    lastName: 'Nowak',
    pesel: '90050567890',
    nip: '9876543210',
    voivodeship: 'małopolskie',
    city: 'Kraków',
    street: 'ul. Główna',
    buildingNumber: '15',
    apartmentNumber: '3',
    zipCode: '30-001',
    createdAt: '2025-01-05T09:15:00Z'
  },
  {
    id: '4',
    clientType: 'company',
    displayName: 'Firma Rolna ABC',
    companyName: 'Firma Rolna ABC',
    nip: '5551234567',
    regon: '987654321',
    voivodeship: 'dolnośląskie',
    city: 'Wrocław',
    street: 'ul. Polna',
    buildingNumber: '100',
    zipCode: '50-001',
    createdAt: '2025-01-03T16:45:00Z'
  }
];

@Injectable({ providedIn: 'root' })
export class ClientService extends GenericCrudService<any> {
  private readonly endpoint = 'clients';
  private mockData = [...MOCK_CLIENTS];
  private nextId = 5;

  getClientsList(params?: {
    q?: string;
    clientType?: string;
    city?: string;
  }): Observable<ClientListItem[]> {
    // Mock implementation - filter clients based on params
    let filtered = [...this.mockData];
    
    if (params?.q) {
      const query = params.q.toLowerCase();
      filtered = filtered.filter(c => 
        c.displayName.toLowerCase().includes(query) ||
        c.city?.toLowerCase().includes(query) ||
        c.nip?.includes(query) ||
        c.pesel?.includes(query)
      );
    }
    
    if (params?.clientType) {
      filtered = filtered.filter(c => c.clientType === params.clientType);
    }
    
    if (params?.city) {
      const city = params.city.toLowerCase();
      filtered = filtered.filter(c => c.city?.toLowerCase().includes(city));
    }
    
    return of(filtered).pipe(delay(300));
  }

  getClient(id: string): Observable<ClientDetail> {
    const client = this.mockData.find(c => c.id === id);
    if (!client) {
      throw new Error('Client not found');
    }
    
    const detail: ClientDetail = {
      id: client.id,
      clientType: client.clientType,
      firstName: client.firstName || '',
      lastName: client.lastName || '',
      pesel: client.pesel || '',
      companyName: client.companyName || '',
      nip: client.nip || '',
      regon: client.regon || '',
      voivodeship: client.voivodeship || '',
      city: client.city || '',
      street: client.street || '',
      buildingNumber: client.buildingNumber || '',
      apartmentNumber: client.apartmentNumber || '',
      zipCode: client.zipCode || '',
      createdAt: client.createdAt,
      updatedAt: null
    };
    
    return of(detail).pipe(delay(200));
  }

  createClient(req: ClientCreateUpdateRequest): Observable<ClientDetail> {
    const newId = String(this.nextId++);
    const now = new Date().toISOString();
    
    const displayName = req.clientType === 'person'
      ? `${req.firstName} ${req.lastName}`
      : req.companyName;
    
    const newClient: ClientListItem = {
      id: newId,
      clientType: req.clientType,
      displayName,
      firstName: req.firstName,
      lastName: req.lastName,
      pesel: req.pesel,
      companyName: req.companyName,
      nip: req.nip,
      regon: req.regon,
      voivodeship: req.voivodeship,
      city: req.city,
      street: req.street,
      buildingNumber: req.buildingNumber,
      apartmentNumber: req.apartmentNumber,
      zipCode: req.zipCode,
      createdAt: now
    };
    
    this.mockData.unshift(newClient);
    
    const detail: ClientDetail = {
      id: newId,
      clientType: req.clientType,
      firstName: req.firstName,
      lastName: req.lastName,
      pesel: req.pesel,
      companyName: req.companyName,
      nip: req.nip,
      regon: req.regon,
      voivodeship: req.voivodeship,
      city: req.city,
      street: req.street,
      buildingNumber: req.buildingNumber,
      apartmentNumber: req.apartmentNumber,
      zipCode: req.zipCode,
      createdAt: now,
      updatedAt: null
    };
    
    return of(detail).pipe(delay(300));
  }

  updateClient(id: string, req: ClientCreateUpdateRequest): Observable<ClientDetail> {
    const index = this.mockData.findIndex(c => c.id === id);
    if (index === -1) {
      throw new Error('Client not found');
    }
    
    const now = new Date().toISOString();
    const displayName = req.clientType === 'person'
      ? `${req.firstName} ${req.lastName}`
      : req.companyName;
    
    this.mockData[index] = {
      ...this.mockData[index],
      clientType: req.clientType,
      displayName,
      firstName: req.firstName,
      lastName: req.lastName,
      pesel: req.pesel,
      companyName: req.companyName,
      nip: req.nip,
      regon: req.regon,
      voivodeship: req.voivodeship,
      city: req.city,
      street: req.street,
      buildingNumber: req.buildingNumber,
      apartmentNumber: req.apartmentNumber,
      zipCode: req.zipCode
    };
    
    const detail: ClientDetail = {
      id,
      clientType: req.clientType,
      firstName: req.firstName,
      lastName: req.lastName,
      pesel: req.pesel,
      companyName: req.companyName,
      nip: req.nip,
      regon: req.regon,
      voivodeship: req.voivodeship,
      city: req.city,
      street: req.street,
      buildingNumber: req.buildingNumber,
      apartmentNumber: req.apartmentNumber,
      zipCode: req.zipCode,
      createdAt: this.mockData[index].createdAt,
      updatedAt: now
    };
    
    return of(detail).pipe(delay(300));
  }

  deleteClient(id: string): Observable<void> {
    const index = this.mockData.findIndex(c => c.id === id);
    if (index !== -1) {
      this.mockData.splice(index, 1);
    }
    return of(undefined).pipe(delay(300));
  }
}
