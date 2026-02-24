/*
 * ROBIGOO FIELD SPRAYER CONTROL STATION
 * Copyright (c) 2025 Wojciech Salamon <wojciech.salamon@yahoo.com>
 * All rights reserved. Unauthorized distribution or disclosure is prohibited.
*/

import { NgModule } from '@angular/core';
import { BrowserModule } from '@angular/platform-browser';
import { BrowserAnimationsModule } from '@angular/platform-browser/animations';
import { FormsModule } from '@angular/forms';
import { HttpClientModule, HTTP_INTERCEPTORS } from '@angular/common/http';
import { CommonModule } from '@angular/common';
import { DragDropModule } from '@angular/cdk/drag-drop';

import { AppComponent } from './app.component';
import { LoginComponent } from './login/login.component';
import { NewInspectionComponent } from './new-inspection/new-inspection.component';
import { CropSprayersComponent } from './crop-sprayers/crop-sprayers.component';
import { InspectionsComponent } from './inspections/inspections.component';
import { SettingsComponent } from './settings/settings.component';
import { TextPipe } from './shared/text.pipe';
import { ClientsComponent } from './clients/clients.component';
import { InspectionMarksComponent } from './inspection-marks/inspection-marks.component';
import { NotificationsComponent } from './notifications/notifications.component';
import { ToastNotificationComponent } from './shared/toast-notification/toast-notification.component';
import { GenericListComponent } from './shared/components/generic-list/generic-list.component';
import { HistoryDialogComponent } from './shared/components/history-dialog/history-dialog.component';
import { CustomSelectComponent } from './shared/components/custom-select/custom-select.component';
import { NumericInputComponent } from './shared/components/numeric-input/numeric-input.component';
import { DatePickerComponent } from './shared/components/date-picker/date-picker.component';
import { AuthInterceptor } from './services/auth.interceptor';
import { BusyInterceptor } from './services/busy.interceptor';
import { ScrollFadeDirective } from './shared/directives/scroll-fade.directive';
import { ScrollCenterDirective } from './shared/directives/scroll-center.directive';
import { BusyOverlayDirective } from './shared/directives/busy-overlay.directive';
import { FilterPanelComponent } from './shared/components/filter-panel/filter-panel.component';
import { EntityListPanelComponent } from './shared/components/entity-list-panel/entity-list-panel.component';
import { EntityDetailPanelComponent } from './shared/components/entity-detail-panel/entity-detail-panel.component';
import { StepIndicatorComponent } from './shared/components/step-indicator/step-indicator.component';
import { MasterDetailLayoutComponent } from './shared/components/master-detail-layout/master-detail-layout.component';
import { EntityToolbarComponent } from './shared/components/entity-toolbar/entity-toolbar.component';
import { BusyIndicatorComponent } from './shared/busy-indicator/busy-indicator.component';
import { ScrollSpyDirective } from './shared/directives/scroll-spy.directive';
import { ClientAutocompleteComponent } from './shared/components/client-autocomplete/client-autocomplete.component';

@NgModule({
  declarations: [AppComponent, LoginComponent, NewInspectionComponent, CropSprayersComponent, InspectionsComponent, SettingsComponent, TextPipe, ClientsComponent, InspectionMarksComponent, NotificationsComponent, ToastNotificationComponent, GenericListComponent, HistoryDialogComponent, CustomSelectComponent, NumericInputComponent, DatePickerComponent, ScrollFadeDirective, ScrollCenterDirective, BusyOverlayDirective, FilterPanelComponent, EntityListPanelComponent, EntityDetailPanelComponent, StepIndicatorComponent, MasterDetailLayoutComponent, EntityToolbarComponent, BusyIndicatorComponent, ClientAutocompleteComponent, ScrollSpyDirective],
  imports: [BrowserModule, BrowserAnimationsModule, FormsModule, HttpClientModule, CommonModule, DragDropModule],
  providers: [
    { provide: HTTP_INTERCEPTORS, useClass: AuthInterceptor, multi: true },
    { provide: HTTP_INTERCEPTORS, useClass: BusyInterceptor, multi: true }
  ],
  bootstrap: [AppComponent]
})
export class AppModule { }
