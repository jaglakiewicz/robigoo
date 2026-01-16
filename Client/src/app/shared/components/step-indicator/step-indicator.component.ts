/*
 * ROBIGOO FIELD SPRAYER CONTROL STATION
 * Copyright (c) 2025 Wojciech Salamon <wojciech.salamon@yahoo.com>
 * All rights reserved. Unauthorized distribution or disclosure is prohibited.
 */

import { Component, EventEmitter, Input, Output } from '@angular/core';

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
}

/**
 * Reusable step indicator component for multi-step forms and wizards.
 * 
 * Features:
 * - Horizontal scrollable step navigation
 * - Active and completed step states
 * - Click navigation between steps
 * - Automatic scroll centering on active step
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
export class StepIndicatorComponent {
  /** Array of step definitions */
  @Input() steps: Step[] = [];

  /** ID of the currently active step */
  @Input() currentStepId = 0;

  /** Whether steps can be clicked to navigate */
  @Input() clickable = true;

  /** Emitted when a step is clicked */
  @Output() stepClick = new EventEmitter<number>();

  isActive(step: Step): boolean {
    return step.id === this.currentStepId;
  }

  isCompleted(step: Step): boolean {
    return step.id < this.currentStepId;
  }

  onStepClick(step: Step): void {
    if (this.clickable) {
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
