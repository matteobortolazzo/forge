import { Injectable, NgZone, inject } from '@angular/core';
import { Observable, Subject, interval, map, takeUntil } from 'rxjs';
import { ServerEvent, TaskLog, LogType } from '../../shared/models';

const SSE_ENDPOINT = '/api/events';

@Injectable({ providedIn: 'root' })
export class SseService {
  private readonly zone = inject(NgZone);
  private readonly useMocks = false;
  private eventSource: EventSource | null = null;
  private readonly destroy$ = new Subject<void>();

  connect(): Observable<ServerEvent> {
    if (this.useMocks) {
      return this.createMockEventStream();
    }
    return this.createRealEventStream();
  }

  disconnect(): void {
    this.destroy$.next();
    if (this.eventSource) {
      this.eventSource.close();
      this.eventSource = null;
    }
  }

  private createRealEventStream(): Observable<ServerEvent> {
    return new Observable(observer => {
      this.zone.runOutsideAngular(() => {
        this.eventSource = new EventSource(SSE_ENDPOINT);

        this.eventSource.onmessage = (event) => {
          this.zone.run(() => {
            try {
              const data = JSON.parse(event.data);
              observer.next(data as ServerEvent);
            } catch {
              console.error('Failed to parse SSE event:', event.data);
            }
          });
        };

        this.eventSource.onerror = (error) => {
          this.zone.run(() => {
            console.error('SSE connection error:', error);
            // Don't complete the observable on error, allow reconnection
          });
        };
      });

      return () => {
        this.eventSource?.close();
        this.eventSource = null;
      };
    });
  }

  private createMockEventStream(): Observable<ServerEvent> {
    const mockLogMessages = [
      { type: 'thinking' as LogType, content: 'Analyzing the codebase structure to find the best approach...' },
      { type: 'toolUse' as LogType, content: 'Reading file: src/services/rate-limiter.ts', toolName: 'Read' },
      { type: 'toolResult' as LogType, content: 'File read successfully (87 lines)', toolName: 'Read' },
      { type: 'info' as LogType, content: 'Found existing rate limiter implementation, will extend it...' },
      { type: 'toolUse' as LogType, content: 'Searching for usages of RateLimiter class...', toolName: 'Grep' },
      { type: 'toolResult' as LogType, content: 'Found 3 usages across the codebase', toolName: 'Grep' },
      { type: 'thinking' as LogType, content: 'Need to update the middleware to use sliding window algorithm instead of fixed window...' },
      { type: 'toolUse' as LogType, content: 'Editing file: src/middleware/rate-limit.middleware.ts', toolName: 'Edit' },
      { type: 'toolResult' as LogType, content: 'File updated successfully', toolName: 'Edit' },
      { type: 'info' as LogType, content: 'Running tests to verify changes...' },
      { type: 'toolUse' as LogType, content: 'Executing: npm test -- --grep "rate limit"', toolName: 'Bash' },
      { type: 'toolResult' as LogType, content: 'All 12 tests passed', toolName: 'Bash' },
    ];

    let logIndex = 0;

    return interval(3000).pipe(
      takeUntil(this.destroy$),
      map(() => {
        const currentLog = mockLogMessages[logIndex % mockLogMessages.length];
        logIndex++;

        const log: TaskLog = {
          id: `mock-log-${Date.now()}`,
          taskId: 'task-002', // The active task with agent
          type: currentLog.type,
          content: currentLog.content,
          toolName: currentLog.toolName,
          timestamp: new Date(),
        };

        return {
          type: 'task:log' as const,
          payload: log,
          timestamp: new Date(),
        } as ServerEvent;
      })
    );
  }
}
