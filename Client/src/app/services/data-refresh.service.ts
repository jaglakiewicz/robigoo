/*
 * ROBIGOO FIELD SPRAYER CONTROL STATION
 * Data Refresh Service
 * Copyright (c) 2025 Wojciech Salamon
 *
 * Global service for notifying components when data has been modified.
 * Components can subscribe to specific data types and refresh their data
 * when notified.
 */

import { Injectable } from '@angular/core';
import { Subject, Observable } from 'rxjs';
import { filter } from 'rxjs/operators';

/** Types of data that can be refreshed */
export type DataType = 'machines' | 'clients' | 'inspections' | 'users';

export interface DataRefreshEvent {
  dataType: DataType;
  /** Optional ID of the specific item that changed */
  itemId?: string;
  /** Type of change: create, update, delete */
  changeType: 'create' | 'update' | 'delete';
}

@Injectable({ providedIn: 'root' })
export class DataRefreshService {
  private refreshSubject = new Subject<DataRefreshEvent>();

  /** Emit a refresh event to notify listeners that data has changed */
  notifyDataChanged(event: DataRefreshEvent): void {
    this.refreshSubject.next(event);
  }

  /** Subscribe to all refresh events */
  onAnyDataChanged(): Observable<DataRefreshEvent> {
    return this.refreshSubject.asObservable();
  }

  /** Subscribe to refresh events for a specific data type */
  onDataChanged(dataType: DataType): Observable<DataRefreshEvent> {
    return this.refreshSubject.asObservable().pipe(
      filter(event => event.dataType === dataType)
    );
  }

  /** Convenience method to notify that machines data has changed */
  notifyMachinesChanged(changeType: 'create' | 'update' | 'delete', itemId?: string): void {
    this.notifyDataChanged({ dataType: 'machines', changeType, itemId });
  }

  /** Convenience method to notify that clients data has changed */
  notifyClientsChanged(changeType: 'create' | 'update' | 'delete', itemId?: string): void {
    this.notifyDataChanged({ dataType: 'clients', changeType, itemId });
  }

  /** Convenience method to notify that inspections data has changed */
  notifyInspectionsChanged(changeType: 'create' | 'update' | 'delete', itemId?: string): void {
    this.notifyDataChanged({ dataType: 'inspections', changeType, itemId });
  }

  /** Convenience method to notify that users data has changed */
  notifyUsersChanged(changeType: 'create' | 'update' | 'delete', itemId?: string): void {
    this.notifyDataChanged({ dataType: 'users', changeType, itemId });
  }
}
