import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, of, delay, throwError } from 'rxjs';
import {
  Task,
  TaskLog,
  UpdateTaskDto,
  TransitionTaskDto,
  PauseTaskDto,
  PIPELINE_STATES,
  PipelineState,
} from '../../shared/models';

// Mock data for offline development
const MOCK_REPO_ID = 'repo-1';
const MOCK_BACKLOG_ID = 'backlog-006'; // The "Fix API rate limiting" backlog item

const minutesAgo = (minutes: number) => {
  const date = new Date();
  date.setMinutes(date.getMinutes() - minutes);
  return date;
};

const hoursAgo = (hours: number) => {
  const date = new Date();
  date.setHours(date.getHours() - hours);
  return date;
};

const MOCK_TASKS: Task[] = [
  // Tasks for backlog-006 (Fix API rate limiting)
  {
    id: 'task-001',
    repositoryId: MOCK_REPO_ID,
    backlogItemId: MOCK_BACKLOG_ID,
    title: 'Research existing rate limiting solutions',
    description: 'Analyze current API structure and research rate limiting approaches.',
    state: 'PrReady',
    priority: 'critical',
    hasError: false,
    isPaused: false,
    retryCount: 0,
    maxRetries: 3,
    executionOrder: 1,
    hasPendingGate: false,
    createdAt: hoursAgo(4),
    updatedAt: hoursAgo(2),
  },
  {
    id: 'task-002',
    repositoryId: MOCK_REPO_ID,
    backlogItemId: MOCK_BACKLOG_ID,
    title: 'Implement rate limiting middleware',
    description: 'Add AspNetCoreRateLimit package and configure rate limiting.',
    state: 'Implementing',
    priority: 'critical',
    assignedAgentId: 'agent-001',
    hasError: false,
    isPaused: false,
    retryCount: 0,
    maxRetries: 3,
    executionOrder: 2,
    hasPendingGate: false,
    createdAt: hoursAgo(3),
    updatedAt: minutesAgo(5),
  },
  {
    id: 'task-003',
    repositoryId: MOCK_REPO_ID,
    backlogItemId: MOCK_BACKLOG_ID,
    title: 'Add rate limit tests and documentation',
    description: 'Write integration tests and update API documentation.',
    state: 'Planning',
    priority: 'high',
    hasError: false,
    isPaused: false,
    retryCount: 0,
    maxRetries: 3,
    executionOrder: 3,
    hasPendingGate: false,
    createdAt: hoursAgo(2),
    updatedAt: hoursAgo(2),
  },
];

const MOCK_LOGS: TaskLog[] = [
  {
    id: 'log-001',
    taskId: 'task-002',
    type: 'info',
    content: 'Starting implementation of API rate limiting...',
    timestamp: minutesAgo(10),
  },
  {
    id: 'log-002',
    taskId: 'task-002',
    type: 'thinking',
    content: 'Analyzing current API structure to determine best approach for rate limiting.',
    timestamp: minutesAgo(9),
  },
  {
    id: 'log-003',
    taskId: 'task-002',
    type: 'toolUse',
    content: 'Reading file: src/Forge.Api/Program.cs',
    toolName: 'Read',
    timestamp: minutesAgo(8),
  },
  {
    id: 'log-004',
    taskId: 'task-002',
    type: 'toolResult',
    content: 'File contents retrieved successfully (142 lines)',
    toolName: 'Read',
    timestamp: minutesAgo(8),
  },
  {
    id: 'log-005',
    taskId: 'task-002',
    type: 'toolUse',
    content: 'Adding rate limiting package to project...',
    toolName: 'Bash',
    timestamp: minutesAgo(5),
  },
  {
    id: 'log-006',
    taskId: 'task-002',
    type: 'toolResult',
    content: 'dotnet add package AspNetCoreRateLimit --version 5.0.0\nPackage installed successfully',
    toolName: 'Bash',
    timestamp: minutesAgo(4),
  },
];

@Injectable({ providedIn: 'root' })
export class TaskService {
  private readonly http = inject(HttpClient);
  private readonly useMocks = false;

  // In-memory task store for mock mode
  private mockTasks: Task[] = [...MOCK_TASKS];

  private getApiUrl(repositoryId: string, backlogItemId: string): string {
    return `/api/repositories/${repositoryId}/backlog/${backlogItemId}/tasks`;
  }

  getTasks(repositoryId: string, backlogItemId: string): Observable<Task[]> {
    if (this.useMocks) {
      const tasks = this.mockTasks.filter(
        t => t.repositoryId === repositoryId && t.backlogItemId === backlogItemId
      );
      return of([...tasks]).pipe(delay(300));
    }
    return this.http.get<Task[]>(this.getApiUrl(repositoryId, backlogItemId));
  }

  getTask(repositoryId: string, backlogItemId: string, id: string): Observable<Task> {
    if (this.useMocks) {
      const task = this.mockTasks.find(
        t => t.id === id && t.repositoryId === repositoryId && t.backlogItemId === backlogItemId
      );
      if (task) {
        return of({ ...task }).pipe(delay(200));
      }
      return throwError(() => new Error('Task not found'));
    }
    return this.http.get<Task>(`${this.getApiUrl(repositoryId, backlogItemId)}/${id}`);
  }

  updateTask(repositoryId: string, backlogItemId: string, id: string, dto: UpdateTaskDto): Observable<Task> {
    if (this.useMocks) {
      const index = this.mockTasks.findIndex(
        t => t.id === id && t.repositoryId === repositoryId && t.backlogItemId === backlogItemId
      );
      if (index === -1) {
        return throwError(() => new Error('Task not found'));
      }
      const updatedTask: Task = {
        ...this.mockTasks[index],
        ...dto,
        updatedAt: new Date(),
      };
      this.mockTasks[index] = updatedTask;
      return of({ ...updatedTask }).pipe(delay(200));
    }
    return this.http.patch<Task>(`${this.getApiUrl(repositoryId, backlogItemId)}/${id}`, dto);
  }

  deleteTask(repositoryId: string, backlogItemId: string, id: string): Observable<void> {
    if (this.useMocks) {
      const index = this.mockTasks.findIndex(
        t => t.id === id && t.repositoryId === repositoryId && t.backlogItemId === backlogItemId
      );
      if (index === -1) {
        return throwError(() => new Error('Task not found'));
      }
      this.mockTasks = this.mockTasks.filter(t => t.id !== id);
      return of(undefined).pipe(delay(200));
    }
    return this.http.delete<void>(`${this.getApiUrl(repositoryId, backlogItemId)}/${id}`);
  }

  transitionTask(repositoryId: string, backlogItemId: string, id: string, dto: TransitionTaskDto): Observable<Task> {
    if (this.useMocks) {
      const index = this.mockTasks.findIndex(
        t => t.id === id && t.repositoryId === repositoryId && t.backlogItemId === backlogItemId
      );
      if (index === -1) {
        return throwError(() => new Error('Task not found'));
      }
      const updatedTask: Task = {
        ...this.mockTasks[index],
        state: dto.targetState,
        updatedAt: new Date(),
      };
      this.mockTasks[index] = updatedTask;
      return of({ ...updatedTask }).pipe(delay(200));
    }
    return this.http.post<Task>(`${this.getApiUrl(repositoryId, backlogItemId)}/${id}/transition`, dto);
  }

  getTaskLogs(repositoryId: string, backlogItemId: string, taskId: string): Observable<TaskLog[]> {
    if (this.useMocks) {
      const logs = MOCK_LOGS.filter(log => log.taskId === taskId);
      return of([...logs]).pipe(delay(200));
    }
    return this.http.get<TaskLog[]>(`${this.getApiUrl(repositoryId, backlogItemId)}/${taskId}/logs`);
  }

  abortAgent(repositoryId: string, backlogItemId: string, taskId: string): Observable<Task> {
    if (this.useMocks) {
      const index = this.mockTasks.findIndex(
        t => t.id === taskId && t.repositoryId === repositoryId && t.backlogItemId === backlogItemId
      );
      if (index === -1) {
        return throwError(() => new Error('Task not found'));
      }
      const updatedTask: Task = {
        ...this.mockTasks[index],
        assignedAgentId: undefined,
        updatedAt: new Date(),
      };
      this.mockTasks[index] = updatedTask;
      return of({ ...updatedTask }).pipe(delay(300));
    }
    return this.http.post<Task>(`${this.getApiUrl(repositoryId, backlogItemId)}/${taskId}/abort`, {});
  }

  startAgent(repositoryId: string, backlogItemId: string, taskId: string): Observable<Task> {
    if (this.useMocks) {
      const index = this.mockTasks.findIndex(
        t => t.id === taskId && t.repositoryId === repositoryId && t.backlogItemId === backlogItemId
      );
      if (index === -1) {
        return throwError(() => new Error('Task not found'));
      }
      const updatedTask: Task = {
        ...this.mockTasks[index],
        assignedAgentId: `agent-${Date.now()}`,
        updatedAt: new Date(),
      };
      this.mockTasks[index] = updatedTask;
      return of({ ...updatedTask }).pipe(delay(500));
    }
    return this.http.post<Task>(`${this.getApiUrl(repositoryId, backlogItemId)}/${taskId}/start-agent`, {});
  }

  pauseTask(repositoryId: string, backlogItemId: string, taskId: string, dto: PauseTaskDto): Observable<Task> {
    if (this.useMocks) {
      const index = this.mockTasks.findIndex(
        t => t.id === taskId && t.repositoryId === repositoryId && t.backlogItemId === backlogItemId
      );
      if (index === -1) {
        return throwError(() => new Error('Task not found'));
      }
      const updatedTask: Task = {
        ...this.mockTasks[index],
        isPaused: true,
        pauseReason: dto.reason,
        pausedAt: new Date(),
        updatedAt: new Date(),
      };
      this.mockTasks[index] = updatedTask;
      return of({ ...updatedTask }).pipe(delay(300));
    }
    return this.http.post<Task>(`${this.getApiUrl(repositoryId, backlogItemId)}/${taskId}/pause`, dto);
  }

  resumeTask(repositoryId: string, backlogItemId: string, taskId: string): Observable<Task> {
    if (this.useMocks) {
      const index = this.mockTasks.findIndex(
        t => t.id === taskId && t.repositoryId === repositoryId && t.backlogItemId === backlogItemId
      );
      if (index === -1) {
        return throwError(() => new Error('Task not found'));
      }
      const updatedTask: Task = {
        ...this.mockTasks[index],
        isPaused: false,
        pauseReason: undefined,
        pausedAt: undefined,
        updatedAt: new Date(),
      };
      this.mockTasks[index] = updatedTask;
      return of({ ...updatedTask }).pipe(delay(300));
    }
    return this.http.post<Task>(`${this.getApiUrl(repositoryId, backlogItemId)}/${taskId}/resume`, {});
  }

  // Helper methods
  getNextState(currentState: PipelineState): PipelineState | null {
    const currentIndex = PIPELINE_STATES.indexOf(currentState);
    if (currentIndex < PIPELINE_STATES.length - 1) {
      return PIPELINE_STATES[currentIndex + 1];
    }
    return null;
  }

  getPreviousState(currentState: PipelineState): PipelineState | null {
    const currentIndex = PIPELINE_STATES.indexOf(currentState);
    if (currentIndex > 0) {
      return PIPELINE_STATES[currentIndex - 1];
    }
    return null;
  }
}
