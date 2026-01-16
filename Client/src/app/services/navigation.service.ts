/*
 * ROBIGOO FIELD SPRAYER CONTROL STATION
 * Copyright (c) 2025 Wojciech Salamon <wojciech.salamon@yahoo.com>
 * All rights reserved. Unauthorized distribution or disclosure is prohibited.
*/

import { Injectable } from '@angular/core';
import { Subject } from 'rxjs';

export interface NavigationRequest {
  tabType: string;
  tabTitleKey: string;
  params?: { [key: string]: string };
}

@Injectable({ providedIn: 'root' })
export class NavigationService {
  private navigationSubject = new Subject<NavigationRequest>();
  
  /** Observable for navigation requests */
  navigation$ = this.navigationSubject.asObservable();
  
  /** Pending params for the target component to pick up */
  private pendingParams: { [key: string]: string } | null = null;

  /**
   * Request navigation to a tab with optional parameters.
   * The AppComponent will listen and open the tab.
   * The target component can retrieve params via getPendingParams().
   */
  navigateTo(tabType: string, tabTitleKey: string, params?: { [key: string]: string }): void {
    this.pendingParams = params || null;
    this.navigationSubject.next({ tabType, tabTitleKey, params });
  }

  /**
   * Get pending navigation params (called by target component on init).
   * Clears the params after retrieval.
   */
  getPendingParams(): { [key: string]: string } | null {
    const params = this.pendingParams;
    this.pendingParams = null;
    return params;
  }
}
