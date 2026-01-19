/*
 * ROBIGOO FIELD SPRAYER CONTROL STATION
 * Busy Overlay Directive
 * Copyright (c) 2025 Wojciech Salamon <wojciech.salamon@yahoo.com>
 * All rights reserved. Unauthorized distribution or disclosure is prohibited.
 * 
 * Directive to add a loading overlay to any container.
 * 
 * Usage:
 * <div [appBusyOverlay]="isLoading" [busyMessage]="'Loading...'">
 *   Content here
 * </div>
 * 
 * Or with the service:
 * <div [appBusyOverlay]="'my-overlay-id'" [busyMessage]="'Loading...'">
 *   Content here
 * </div>
 * Then call: busyService.showOverlay('my-overlay-id')
 */

import { Directive, Input, ElementRef, Renderer2, OnChanges, SimpleChanges, OnDestroy, OnInit } from '@angular/core';
import { Subscription } from 'rxjs';
import { BusyIndicatorService } from '../../services/busy-indicator.service';

@Directive({
  selector: '[appBusyOverlay]'
})
export class BusyOverlayDirective implements OnInit, OnChanges, OnDestroy {
  /**
   * Can be:
   * - boolean: directly control the loading state
   * - string: ID to check against the BusyIndicatorService
   */
  @Input('appBusyOverlay') busyState: boolean | string = false;
  
  /** Optional message to display */
  @Input() busyMessage?: string;
  
  /** Size of the spinner: 'sm' | 'md' | 'lg' */
  @Input() busySize: 'sm' | 'md' | 'lg' = 'md';

  private overlayElement?: HTMLElement;
  private subscription?: Subscription;
  private isShowing = false;

  constructor(
    private el: ElementRef,
    private renderer: Renderer2,
    private busyService: BusyIndicatorService
  ) {}

  ngOnInit(): void {
    // If busyState is a string, subscribe to the service
    if (typeof this.busyState === 'string' && this.busyState) {
      this.subscription = this.busyService.isActive(this.busyState).subscribe(isActive => {
        this.updateOverlay(isActive);
      });
    }
  }

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['busyState']) {
      const value = changes['busyState'].currentValue;
      
      // If it's a boolean, apply directly
      if (typeof value === 'boolean') {
        this.updateOverlay(value);
      }
      // If it's a string and changed, resubscribe
      else if (typeof value === 'string') {
        this.subscription?.unsubscribe();
        this.subscription = this.busyService.isActive(value).subscribe(isActive => {
          this.updateOverlay(isActive);
        });
      }
    }
  }

  ngOnDestroy(): void {
    this.subscription?.unsubscribe();
    this.removeOverlay();
  }

  private updateOverlay(show: boolean): void {
    if (show && !this.isShowing) {
      this.showOverlay();
    } else if (!show && this.isShowing) {
      this.removeOverlay();
    }
  }

  private showOverlay(): void {
    this.isShowing = true;
    
    // Ensure the host element has relative positioning
    const currentPosition = getComputedStyle(this.el.nativeElement).position;
    if (currentPosition === 'static') {
      this.renderer.setStyle(this.el.nativeElement, 'position', 'relative');
    }

    // Create overlay element
    this.overlayElement = this.renderer.createElement('div');
    this.renderer.addClass(this.overlayElement, 'busy-container-overlay');

    // Create content container
    const content = this.renderer.createElement('div');
    this.renderer.addClass(content, 'busy-container-content');

    // Create spinner
    const spinner = this.renderer.createElement('div');
    this.renderer.addClass(spinner, 'busy-spinner');
    this.renderer.addClass(spinner, `busy-spinner--${this.busySize}`);
    this.renderer.appendChild(content, spinner);

    // Add message if provided
    if (this.busyMessage) {
      const message = this.renderer.createElement('span');
      this.renderer.addClass(message, 'busy-container-message');
      const text = this.renderer.createText(this.busyMessage);
      this.renderer.appendChild(message, text);
      this.renderer.appendChild(content, message);
    }

    this.renderer.appendChild(this.overlayElement, content);
    this.renderer.appendChild(this.el.nativeElement, this.overlayElement);
  }

  private removeOverlay(): void {
    this.isShowing = false;
    if (this.overlayElement) {
      this.renderer.removeChild(this.el.nativeElement, this.overlayElement);
      this.overlayElement = undefined;
    }
  }
}
