import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface ChangeLog {
  id: number;
  entityName: string;
  entityId: string;
  changes: string;
  who: string;
  when: string;
  userFirstName?: string;
  userLastName?: string;
  userAvatarData?: string; // base64 encoded byte array from C#
}

@Injectable({
  providedIn: 'root'
})
export class HistoryService {

  // Ideally use environment.apiUrl, but assuming relative path works or based on existing service patterns
  private baseUrl = 'api'; 

  constructor(private http: HttpClient) { }

  getHistory(entityName: string, entityId: string): Observable<ChangeLog[]> {
    return this.http.get<ChangeLog[]>(`${this.baseUrl}/History/${entityName}/${entityId}`);
  }
}
