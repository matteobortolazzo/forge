import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, of, delay } from 'rxjs';
import { AgentStatus } from '../../shared/models';

@Injectable({ providedIn: 'root' })
export class AgentService {
  private readonly http = inject(HttpClient);
  private readonly useMocks = false;
  private readonly apiUrl = '/api/agent';

  private mockAgentStatus: AgentStatus = {
    isRunning: true,
    currentTaskId: 'task-006',
    startedAt: new Date(Date.now() - 10 * 60 * 1000),
  };

  getStatus(): Observable<AgentStatus> {
    if (this.useMocks) {
      return of({ ...this.mockAgentStatus }).pipe(delay(200));
    }
    return this.http.get<AgentStatus>(`${this.apiUrl}/status`);
  }

  updateMockStatus(status: AgentStatus): void {
    this.mockAgentStatus = { ...status };
  }
}
