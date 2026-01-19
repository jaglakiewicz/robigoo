/*
 * ROBIGOO FIELD SPRAYER CONTROL STATION
 * Busy Indicator Service
 * Copyright (c) 2025 Wojciech Salamon <wojciech.salamon@yahoo.com>
 * All rights reserved. Unauthorized distribution or disclosure is prohibited.
 * 
 * Global service for managing loading/busy states across the application.
 * Supports different indicator types:
 * - 'fullscreen': Covers the entire main content area with a large spinner
 * - 'overlay': Semi-transparent overlay for specific containers
 * - 'inline': Small inline spinner for buttons or small areas
 */

import { Injectable } from '@angular/core';
import { BehaviorSubject, Observable } from 'rxjs';
import { map } from 'rxjs/operators';

/** Types of busy indicators */
export type BusyIndicatorType = 'fullscreen' | 'overlay' | 'inline';

/** Size variants for the spinner */
export type BusyIndicatorSize = 'sm' | 'md' | 'lg';

/** Configuration for a busy indicator */
export interface BusyIndicatorConfig {
  /** Unique identifier for this busy state */
  id: string;
  /** Type of indicator to show */
  type: BusyIndicatorType;
  /** Optional message to display */
  message?: string;
  /** Size of the spinner (default: 'md') */
  size?: BusyIndicatorSize;
}

/** Internal state for tracking busy indicators */
interface BusyState {
  indicators: Map<string, BusyIndicatorConfig>;
}

@Injectable({
  providedIn: 'root'
})
export class BusyIndicatorService {
  private state$ = new BehaviorSubject<BusyState>({
    indicators: new Map()
  });

  private idCounter = 0;

  constructor() {}

  /**
   * Get observable of all active indicators
   */
  getIndicators(): Observable<BusyIndicatorConfig[]> {
    return this.state$.pipe(
      map(state => Array.from(state.indicators.values()))
    );
  }

  /**
   * Check if there's any fullscreen indicator active
   */
  hasFullscreenIndicator(): Observable<boolean> {
    return this.state$.pipe(
      map(state => {
        for (const indicator of state.indicators.values()) {
          if (indicator.type === 'fullscreen') return true;
        }
        return false;
      })
    );
  }

  /**
   * Get the fullscreen indicator config if active
   */
  getFullscreenIndicator(): Observable<BusyIndicatorConfig | null> {
    return this.state$.pipe(
      map(state => {
        for (const indicator of state.indicators.values()) {
          if (indicator.type === 'fullscreen') return indicator;
        }
        return null;
      })
    );
  }

  /**
   * Check if a specific indicator is active
   */
  isActive(id: string): Observable<boolean> {
    return this.state$.pipe(
      map(state => state.indicators.has(id))
    );
  }

  /**
   * Check if any indicator is active
   */
  isBusy(): Observable<boolean> {
    return this.state$.pipe(
      map(state => state.indicators.size > 0)
    );
  }

  /**
   * Show a fullscreen busy indicator
   * @param message Optional message to display
   * @returns The ID of the created indicator (use to hide it later)
   */
  showFullscreen(message?: string): string {
    return this.show({
      id: this.generateId(),
      type: 'fullscreen',
      message,
      size: 'lg'
    });
  }

  /**
   * Show an overlay busy indicator (for specific areas)
   * @param id Unique identifier for this indicator
   * @param message Optional message to display
   */
  showOverlay(id: string, message?: string): string {
    return this.show({
      id,
      type: 'overlay',
      message,
      size: 'md'
    });
  }

  /**
   * Show an inline busy indicator (small, for buttons etc.)
   * @param id Unique identifier for this indicator
   */
  showInline(id: string): string {
    return this.show({
      id,
      type: 'inline',
      size: 'sm'
    });
  }

  /**
   * Show a busy indicator with full configuration
   * @param config The indicator configuration
   * @returns The ID of the created indicator
   */
  show(config: BusyIndicatorConfig): string {
    const currentState = this.state$.value;
    const newIndicators = new Map(currentState.indicators);
    newIndicators.set(config.id, config);
    
    this.state$.next({
      indicators: newIndicators
    });

    return config.id;
  }

  /**
   * Hide a specific busy indicator
   * @param id The ID of the indicator to hide
   */
  hide(id: string): void {
    const currentState = this.state$.value;
    if (!currentState.indicators.has(id)) return;

    const newIndicators = new Map(currentState.indicators);
    newIndicators.delete(id);
    
    this.state$.next({
      indicators: newIndicators
    });
  }

  /**
   * Hide all busy indicators
   */
  hideAll(): void {
    this.state$.next({
      indicators: new Map()
    });
  }

  /**
   * Hide all fullscreen indicators
   */
  hideFullscreen(): void {
    const currentState = this.state$.value;
    const newIndicators = new Map<string, BusyIndicatorConfig>();
    
    for (const [id, indicator] of currentState.indicators) {
      if (indicator.type !== 'fullscreen') {
        newIndicators.set(id, indicator);
      }
    }
    
    this.state$.next({
      indicators: newIndicators
    });
  }

  /**
   * Execute an async operation with a fullscreen indicator
   * @param operation The async operation to execute
   * @param message Optional message to display
   * @returns The result of the operation
   */
  async withFullscreen<T>(operation: () => Promise<T>, message?: string): Promise<T> {
    const id = this.showFullscreen(message);
    try {
      return await operation();
    } finally {
      this.hide(id);
    }
  }

  /**
   * Execute an async operation with an overlay indicator
   * @param overlayId The ID for the overlay
   * @param operation The async operation to execute
   * @param message Optional message to display
   * @returns The result of the operation
   */
  async withOverlay<T>(overlayId: string, operation: () => Promise<T>, message?: string): Promise<T> {
    this.showOverlay(overlayId, message);
    try {
      return await operation();
    } finally {
      this.hide(overlayId);
    }
  }

  /**
   * Execute an async operation with an inline indicator
   * @param inlineId The ID for the inline indicator
   * @param operation The async operation to execute
   * @returns The result of the operation
   */
  async withInline<T>(inlineId: string, operation: () => Promise<T>): Promise<T> {
    this.showInline(inlineId);
    try {
      return await operation();
    } finally {
      this.hide(inlineId);
    }
  }

  private generateId(): string {
    return `busy-${++this.idCounter}-${Date.now()}`;
  }
}
