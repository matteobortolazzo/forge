import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, of, delay } from 'rxjs';
import { SchedulerStatus } from '../../shared/models';

@Injectable({ providedIn: 'root' })
export class SchedulerService {
  private readonly http = inject(HttpClient);
  private readonly useMocks = false;
  private readonly apiUrl = '/api/scheduler';

  // Mock state for mock mode
  private mockStatus: SchedulerStatus = {
    isEnabled: true,
    isAgentRunning: false,
    currentTaskId: undefined,
    pendingTaskCount: 5,
    pausedTaskCount: 2,
  };

  getStatus(): Observable<SchedulerStatus> {
    if (this.useMocks) {
      return of({ ...this.mockStatus }).pipe(delay(200));
    }
    return this.http.get<SchedulerStatus>(`${this.apiUrl}/status`);
  }

  enable(): Observable<{ enabled: boolean }> {
    if (this.useMocks) {
      this.mockStatus = { ...this.mockStatus, isEnabled: true };
      return of({ enabled: true }).pipe(delay(300));
    }
    return this.http.post<{ enabled: boolean }>(`${this.apiUrl}/enable`, {});
  }

  disable(): Observable<{ enabled: boolean }> {
    if (this.useMocks) {
      this.mockStatus = { ...this.mockStatus, isEnabled: false };
      return of({ enabled: false }).pipe(delay(300));
    }
    return this.http.post<{ enabled: boolean }>(`${this.apiUrl}/disable`, {});
  }
}
