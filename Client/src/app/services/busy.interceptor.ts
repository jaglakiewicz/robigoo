/*
 * ROBIGOO FIELD SPRAYER CONTROL STATION
 * Busy Interceptor - Automatically shows/hides busy indicator for HTTP requests
 * Copyright (c) 2025 Wojciech Salamon <wojciech.salamon@yahoo.com>
 * All rights reserved. Unauthorized distribution or disclosure is prohibited.
 * 
 * This interceptor tracks all HTTP requests and shows a fullscreen busy indicator
 * when requests are in flight. It supports:
 * - Request counting (multiple concurrent requests)
 * - Configurable delay before showing (avoids flicker for fast requests)
 * - Exclusion patterns for specific URLs (e.g., polling endpoints)
 */

import { Injectable } from '@angular/core';
import {
  HttpInterceptor,
  HttpRequest,
  HttpHandler,
  HttpEvent
} from '@angular/common/http';
import { Observable } from 'rxjs';
import { finalize } from 'rxjs/operators';
import { BusyIndicatorService } from './busy-indicator.service';

/** URLs that should not trigger the busy indicator */
const EXCLUDED_URLS: RegExp[] = [
  /\/api\/auth\/refresh$/,  // Token refresh (background)
];

/** Minimum time (ms) before showing indicator - only show for slow requests */
const SHOW_DELAY_MS = 800;

/** Minimum time (ms) the indicator stays visible once shown */
const MIN_DISPLAY_MS = 300;

@Injectable()
export class BusyInterceptor implements HttpInterceptor {
  private activeRequests = 0;
  private busyId: string | null = null;
  private showTimer: ReturnType<typeof setTimeout> | null = null;
  private hideTimer: ReturnType<typeof setTimeout> | null = null;
  private shownAt: number | null = null;

  constructor(private busyService: BusyIndicatorService) {}

  intercept(req: HttpRequest<any>, next: HttpHandler): Observable<HttpEvent<any>> {
    // Skip excluded URLs
    if (this.shouldExclude(req.url)) {
      return next.handle(req);
    }

    this.incrementRequests();

    return next.handle(req).pipe(
      finalize(() => {
        this.decrementRequests();
      })
    );
  }

  private shouldExclude(url: string): boolean {
    return EXCLUDED_URLS.some(pattern => pattern.test(url));
  }

  private incrementRequests(): void {
    this.activeRequests++;

    // Clear any pending hide timer
    if (this.hideTimer) {
      clearTimeout(this.hideTimer);
      this.hideTimer = null;
    }

    // If this is the first request, schedule showing the indicator
    if (this.activeRequests === 1 && !this.busyId) {
      this.showTimer = setTimeout(() => {
        // Only show if still have active requests
        if (this.activeRequests > 0 && !this.busyId) {
          this.busyId = this.busyService.showFullscreen();
          this.shownAt = Date.now();
        }
        this.showTimer = null;
      }, SHOW_DELAY_MS);
    }
  }

  private decrementRequests(): void {
    this.activeRequests = Math.max(0, this.activeRequests - 1);

    // If no more active requests, schedule hiding
    if (this.activeRequests === 0) {
      // Cancel show timer if pending
      if (this.showTimer) {
        clearTimeout(this.showTimer);
        this.showTimer = null;
      }

      // If indicator is shown, ensure minimum display time
      if (this.busyId) {
        const elapsed = this.shownAt ? Date.now() - this.shownAt : 0;
        const remaining = Math.max(0, MIN_DISPLAY_MS - elapsed);

        this.hideTimer = setTimeout(() => {
          if (this.busyId && this.activeRequests === 0) {
            this.busyService.hide(this.busyId);
            this.busyId = null;
            this.shownAt = null;
          }
          this.hideTimer = null;
        }, remaining);
      }
    }
  }
}
