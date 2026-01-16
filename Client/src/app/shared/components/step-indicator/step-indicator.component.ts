/*
 * ROBIGOO FIELD SPRAYER CONTROL STATION
 * Copyright (c) 2025 Wojciech Salamon <wojciech.salamon@yahoo.com>
 * All rights reserved. Unauthorized distribution or disclosure is prohibited.
 */

import { Component, EventEmitter, Input, Output, ViewChild, ElementRef, AfterViewInit, OnDestroy, NgZone } from '@angular/core';

/**
 * Step definition for the step indicator
 */
export interface Step {
  /** Unique identifier for the step */
  id: number;
  /** Key for internal reference */
  key: string;
  /** Display label for the step */
  label: string;
  /** Whether the step is disabled (optional, defaults to false) */
  disabled?: boolean;
}

/**
 * Reusable step indicator component for multi-step forms and wizards.
 * 
 * Features:
 * - Horizontal scrollable step navigation
 * - Active and completed step states
 * - Click navigation between steps
 * - Automatic scroll centering on active step
 * - Visual indicators for hidden steps (left/right arrows)
 * 
 * Usage example:
 * ```html
 * <app-step-indicator
 *   [steps]="steps"
 *   [currentStepId]="currentStep"
 *   (stepClick)="goToStep($event)">
 * </app-step-indicator>
 * ```
 */
@Component({
  selector: 'app-step-indicator',
  templateUrl: './step-indicator.component.html',
  styleUrls: ['./step-indicator.component.css']
})
export class StepIndicatorComponent implements AfterViewInit, OnDestroy {
  /** Array of step definitions */
  @Input() steps: Step[] = [];

  /** ID of the currently active step */
  @Input() currentStepId = 0;

  /** Whether steps can be clicked to navigate */
  @Input() clickable = true;

  /** Emitted when a step is clicked */
  @Output() stepClick = new EventEmitter<number>();

  /** Reference to the scrollable container */
  @ViewChild('scrollContainer') scrollContainer!: ElementRef<HTMLElement>;

  /** Whether there are more steps to the left (scrolled past) */
  hasMoreLeft = false;

  /** Whether there are more steps to the right (not yet visible) */
  hasMoreRight = false;

  private resizeObserver?: ResizeObserver;

  constructor(private ngZone: NgZone) {}

  ngAfterViewInit(): void {
    // Initial check after view is ready
    setTimeout(() => this.checkScrollPosition(), 100);

    // Listen for scroll events
    if (this.scrollContainer?.nativeElement) {
      this.scrollContainer.nativeElement.addEventListener('scroll', this.onScroll);
      
      // Observe size changes
      this.resizeObserver = new ResizeObserver(() => {
        this.ngZone.run(() => this.checkScrollPosition());
      });
      this.resizeObserver.observe(this.scrollContainer.nativeElement);
    }
  }

  ngOnDestroy(): void {
    if (this.scrollContainer?.nativeElement) {
      this.scrollContainer.nativeElement.removeEventListener('scroll', this.onScroll);
    }
    this.resizeObserver?.disconnect();
  }

  private onScroll = (): void => {
    this.ngZone.run(() => this.checkScrollPosition());
  };

  private checkScrollPosition(): void {
    if (!this.scrollContainer?.nativeElement) return;
    
    const el = this.scrollContainer.nativeElement;
    const scrollLeft = el.scrollLeft;
    const scrollWidth = el.scrollWidth;
    const clientWidth = el.clientWidth;
    
    // Check if there's content to scroll
    const hasOverflow = scrollWidth > clientWidth;
    
    // Has more to the left if scrolled past 0
    this.hasMoreLeft = hasOverflow && scrollLeft > 2;
    
    // Has more to the right if not scrolled to the end
    this.hasMoreRight = hasOverflow && (scrollLeft + clientWidth) < (scrollWidth - 2);
  }

  isActive(step: Step): boolean {
    return step.id === this.currentStepId;
  }

  isCompleted(step: Step): boolean {
    return step.id < this.currentStepId;
  }

  isDisabled(step: Step): boolean {
    return step.disabled === true;
  }

  onStepClick(step: Step): void {
    if (this.clickable && !step.disabled) {
      this.stepClick.emit(step.id);
    }
  }

  getStepNumber(index: number): number {
    return index + 1;
  }

  trackByFn(index: number, step: Step): number {
    return step.id;
  }
}
