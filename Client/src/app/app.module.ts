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
import { TypesOfVehiclesComponent } from './types-of-vehicles/types-of-vehicles.component';
import { StatisticsComponent } from './statistics/statistics.component';
import { InspectionsComponent } from './inspections/inspections.component';
import { SettingsComponent } from './settings/settings.component';
import { TextPipe } from './shared/text.pipe';
import { ClientsComponent } from './clients/clients.component';
import { InspectionMarksComponent } from './inspection-marks/inspection-marks.component';
import { NotificationsComponent } from './notifications/notifications.component';
import { ToastNotificationComponent } from './shared/toast-notification/toast-notification.component';
import { GenericListComponent } from './shared/components/generic-list/generic-list.component';
import { HistoryDialogComponent } from './shared/components/history-dialog/history-dialog.component';
import { AuthInterceptor } from './services/auth.interceptor';

@NgModule({
  declarations: [AppComponent, LoginComponent, NewInspectionComponent, TypesOfVehiclesComponent, StatisticsComponent, InspectionsComponent, SettingsComponent, TextPipe, ClientsComponent, InspectionMarksComponent, NotificationsComponent, ToastNotificationComponent, GenericListComponent, HistoryDialogComponent],
  imports: [BrowserModule, BrowserAnimationsModule, FormsModule, HttpClientModule, CommonModule, DragDropModule],
  providers: [
    { provide: HTTP_INTERCEPTORS, useClass: AuthInterceptor, multi: true }
  ],
  bootstrap: [AppComponent]
})
export class AppModule { }
