/*
 * ROBIGOO FIELD SPRAYER CONTROL STATION
 * Busy Indicator Component
 * Copyright (c) 2025 Wojciech Salamon <wojciech.salamon@yahoo.com>
 * All rights reserved. Unauthorized distribution or disclosure is prohibited.
 * 
 * Global busy/loading indicator component.
 * Place this component once in the app root template.
 * 
 * Usage:
 * 1. Add <app-busy-indicator></app-busy-indicator> to app.component.html
 * 2. Inject BusyIndicatorService where needed
 * 3. Call busyService.showFullscreen('Loading...') to show
 * 4. Call busyService.hide(id) to hide
 */

import { Component, OnInit, OnDestroy } from '@angular/core';
import { Subscription } from 'rxjs';
import { BusyIndicatorService, BusyIndicatorConfig } from '../../services/busy-indicator.service';

@Component({
  selector: 'app-busy-indicator',
  templateUrl: './busy-indicator.component.html',
  styleUrls: ['./busy-indicator.component.css']
})
export class BusyIndicatorComponent implements OnInit, OnDestroy {
  fullscreenIndicator: BusyIndicatorConfig | null = null;
  
  private subscription?: Subscription;

  constructor(private busyService: BusyIndicatorService) {}

  ngOnInit(): void {
    this.subscription = this.busyService.getFullscreenIndicator().subscribe(indicator => {
      this.fullscreenIndicator = indicator;
    });
  }

  ngOnDestroy(): void {
    this.subscription?.unsubscribe();
  }
}
