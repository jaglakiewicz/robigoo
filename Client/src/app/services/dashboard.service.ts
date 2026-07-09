import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface RecentInspection {
  id: number;
  protocolNumber: string;
  inspectionDate: string;
  inspectorName: string;
  clientName?: string;
  cropSprayerName?: string;
  cropSprayerSerialNumber?: string;
  cropSprayerType?: string;
  finalResult?: boolean;
  validUntil?: string;
}

export interface RecentCropSprayer {
  serialNumber: string;
  sprayerName: string;
  manufacturer: string;
  productionYear: string;
  type: string;
  kind: string;
  ownerName?: string;
  ownerId?: string;
  createdAt: string;
}

export interface UpcomingInspection {
  cropSprayerSerialNumber: string;
  cropSprayerName: string;
  cropSprayerType?: string;
  ownerName?: string;
  ownerId?: string;
  lastInspectionDate?: string;
  validUntil?: string;
  remainingDays: number;
}

export interface MonthlyStatDto {
  month: number;
  monthName: string;
  count: number;
}

export interface DashboardStatistics {
  year: number;
  totalThisYear: number;
  months: MonthlyStatDto[];
}

@Injectable({ providedIn: 'root' })
export class DashboardService {
  constructor(private http: HttpClient) {}

  getRecentInspections(count = 5): Observable<RecentInspection[]> {
    return this.http.get<RecentInspection[]>(`/api/dashboard/recent-inspections?count=${count}`);
  }

  getRecentCropSprayers(count = 5): Observable<RecentCropSprayer[]> {
    return this.http.get<RecentCropSprayer[]>(`/api/dashboard/recent-crop-sprayers?count=${count}`);
  }

  getUpcomingInspections(limit = 10): Observable<UpcomingInspection[]> {
    return this.http.get<UpcomingInspection[]>(`/api/dashboard/upcoming-inspections?limit=${limit}`);
  }

  getStatistics(year?: number): Observable<DashboardStatistics> {
    const param = year ? `?year=${year}` : '';
    return this.http.get<DashboardStatistics>(`/api/dashboard/statistics${param}`);
  }
}
