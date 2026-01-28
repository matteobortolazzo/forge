import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, of, delay, throwError } from 'rxjs';
import {
  BacklogItem,
  TaskLog,
  CreateBacklogItemDto,
  UpdateBacklogItemDto,
  TransitionBacklogItemDto,
  PauseBacklogItemDto,
  BACKLOG_ITEM_STATES,
  BacklogItemState,
} from '../../shared/models';

// Mock data for offline development
const MOCK_REPO_ID = 'repo-1';

const daysAgo = (days: number) => {
  const date = new Date();
  date.setDate(date.getDate() - days);
  return date;
};

const hoursAgo = (hours: number) => {
  const date = new Date();
  date.setHours(date.getHours() - hours);
  return date;
};

const minutesAgo = (minutes: number) => {
  const date = new Date();
  date.setMinutes(date.getMinutes() - minutes);
  return date;
};

const MOCK_BACKLOG_ITEMS: BacklogItem[] = [
  // New (2 items)
  {
    id: 'backlog-001',
    repositoryId: MOCK_REPO_ID,
    title: 'Add user authentication',
    description: 'Implement JWT-based authentication with login, logout, and token refresh functionality.',
    state: 'New',
    priority: 'high',
    hasError: false,
    isPaused: false,
    retryCount: 0,
    maxRetries: 3,
    taskCount: 0,
    completedTaskCount: 0,
    hasPendingGate: false,
    refiningIterations: 0,
    createdAt: daysAgo(5),
    updatedAt: daysAgo(5),
  },
  {
    id: 'backlog-002',
    repositoryId: MOCK_REPO_ID,
    title: 'Create dashboard charts',
    description: 'Add interactive charts showing task completion metrics and agent performance over time.',
    state: 'New',
    priority: 'medium',
    hasError: false,
    isPaused: false,
    retryCount: 0,
    maxRetries: 3,
    taskCount: 0,
    completedTaskCount: 0,
    hasPendingGate: false,
    refiningIterations: 0,
    createdAt: daysAgo(3),
    updatedAt: daysAgo(3),
  },

  // Refining (1 item)
  {
    id: 'backlog-003',
    repositoryId: MOCK_REPO_ID,
    title: 'Add dark mode support',
    description: 'Implement system-wide dark mode toggle with persistent user preference.',
    state: 'Refining',
    priority: 'low',
    assignedAgentId: 'agent-refining',
    hasError: false,
    isPaused: false,
    retryCount: 0,
    maxRetries: 3,
    taskCount: 0,
    completedTaskCount: 0,
    hasPendingGate: false,
    refiningIterations: 1,
    createdAt: daysAgo(2),
    updatedAt: hoursAgo(1),
  },

  // Ready (1 item)
  {
    id: 'backlog-004',
    repositoryId: MOCK_REPO_ID,
    title: 'Implement drag-and-drop',
    description: 'Allow users to drag tasks between columns on the Kanban board.',
    state: 'Ready',
    priority: 'medium',
    hasError: false,
    isPaused: false,
    retryCount: 0,
    maxRetries: 3,
    taskCount: 0,
    completedTaskCount: 0,
    hasPendingGate: false,
    refiningIterations: 2,
    createdAt: daysAgo(4),
    updatedAt: hoursAgo(12),
  },

  // Splitting (1 item)
  {
    id: 'backlog-005',
    repositoryId: MOCK_REPO_ID,
    title: 'Add task filtering',
    description: 'Implement filters for priority, assignee, and date range on the board view.',
    state: 'Splitting',
    priority: 'low',
    assignedAgentId: 'agent-split',
    hasError: false,
    isPaused: false,
    retryCount: 0,
    maxRetries: 3,
    taskCount: 0,
    completedTaskCount: 0,
    hasPendingGate: false,
    refiningIterations: 1,
    createdAt: daysAgo(3),
    updatedAt: hoursAgo(2),
  },

  // Executing (2 items)
  {
    id: 'backlog-006',
    repositoryId: MOCK_REPO_ID,
    title: 'Fix API rate limiting',
    description: 'Implement proper rate limiting on all API endpoints to prevent abuse.',
    state: 'Executing',
    priority: 'critical',
    hasError: false,
    isPaused: false,
    retryCount: 0,
    maxRetries: 3,
    taskCount: 3,
    completedTaskCount: 1,
    hasPendingGate: false,
    refiningIterations: 1,
    progress: { completed: 1, total: 3, percent: 33 },
    createdAt: daysAgo(2),
    updatedAt: minutesAgo(5),
  },
  {
    id: 'backlog-007',
    repositoryId: MOCK_REPO_ID,
    title: 'Add email notifications',
    description: 'Send email notifications when tasks are assigned or completed.',
    state: 'Executing',
    priority: 'medium',
    hasError: true,
    errorMessage: 'Task failed: SMTP connection timeout',
    isPaused: true,
    pauseReason: 'Max retries exceeded',
    pausedAt: hoursAgo(1),
    retryCount: 3,
    maxRetries: 3,
    taskCount: 4,
    completedTaskCount: 2,
    hasPendingGate: false,
    refiningIterations: 1,
    progress: { completed: 2, total: 4, percent: 50 },
    createdAt: daysAgo(3),
    updatedAt: hoursAgo(1),
  },

  // Done (2 items)
  {
    id: 'backlog-008',
    repositoryId: MOCK_REPO_ID,
    title: 'Setup CI/CD pipeline',
    description: 'Configure GitHub Actions for automated testing and deployment.',
    state: 'Done',
    priority: 'high',
    hasError: false,
    isPaused: false,
    retryCount: 0,
    maxRetries: 3,
    taskCount: 5,
    completedTaskCount: 5,
    hasPendingGate: false,
    refiningIterations: 1,
    progress: { completed: 5, total: 5, percent: 100 },
    createdAt: daysAgo(10),
    updatedAt: daysAgo(1),
  },
  {
    id: 'backlog-009',
    repositoryId: MOCK_REPO_ID,
    title: 'Initial project setup',
    description: 'Create Angular project with Tailwind CSS and configure ESLint.',
    state: 'Done',
    priority: 'critical',
    hasError: false,
    isPaused: false,
    retryCount: 0,
    maxRetries: 3,
    taskCount: 3,
    completedTaskCount: 3,
    hasPendingGate: false,
    refiningIterations: 0,
    progress: { completed: 3, total: 3, percent: 100 },
    createdAt: daysAgo(14),
    updatedAt: daysAgo(12),
  },
];

@Injectable({ providedIn: 'root' })
export class BacklogService {
  private readonly http = inject(HttpClient);
  private readonly useMocks = false;

  // In-memory store for mock mode
  private mockItems: BacklogItem[] = [...MOCK_BACKLOG_ITEMS];

  private getApiUrl(repositoryId: string): string {
    return `/api/repositories/${repositoryId}/backlog`;
  }

  getAll(repositoryId: string): Observable<BacklogItem[]> {
    if (this.useMocks) {
      const items = this.mockItems.filter(i => i.repositoryId === repositoryId);
      return of([...items]).pipe(delay(300));
    }
    return this.http.get<BacklogItem[]>(this.getApiUrl(repositoryId));
  }

  getById(repositoryId: string, id: string): Observable<BacklogItem> {
    if (this.useMocks) {
      const item = this.mockItems.find(i => i.id === id && i.repositoryId === repositoryId);
      if (item) {
        return of({ ...item }).pipe(delay(200));
      }
      return throwError(() => new Error('Backlog item not found'));
    }
    return this.http.get<BacklogItem>(`${this.getApiUrl(repositoryId)}/${id}`);
  }

  create(repositoryId: string, dto: CreateBacklogItemDto): Observable<BacklogItem> {
    if (this.useMocks) {
      const newItem: BacklogItem = {
        id: `backlog-${Date.now()}`,
        repositoryId,
        title: dto.title,
        description: dto.description,
        priority: dto.priority ?? 'medium',
        acceptanceCriteria: dto.acceptanceCriteria,
        state: 'New',
        hasError: false,
        isPaused: false,
        retryCount: 0,
        maxRetries: 3,
        taskCount: 0,
        completedTaskCount: 0,
        hasPendingGate: false,
        refiningIterations: 0,
        createdAt: new Date(),
        updatedAt: new Date(),
      };
      this.mockItems = [newItem, ...this.mockItems];
      return of({ ...newItem }).pipe(delay(300));
    }
    return this.http.post<BacklogItem>(this.getApiUrl(repositoryId), dto);
  }

  update(repositoryId: string, id: string, dto: UpdateBacklogItemDto): Observable<BacklogItem> {
    if (this.useMocks) {
      const index = this.mockItems.findIndex(i => i.id === id && i.repositoryId === repositoryId);
      if (index === -1) {
        return throwError(() => new Error('Backlog item not found'));
      }
      const updatedItem: BacklogItem = {
        ...this.mockItems[index],
        ...dto,
        updatedAt: new Date(),
      };
      this.mockItems[index] = updatedItem;
      return of({ ...updatedItem }).pipe(delay(200));
    }
    return this.http.patch<BacklogItem>(`${this.getApiUrl(repositoryId)}/${id}`, dto);
  }

  delete(repositoryId: string, id: string): Observable<void> {
    if (this.useMocks) {
      const index = this.mockItems.findIndex(i => i.id === id && i.repositoryId === repositoryId);
      if (index === -1) {
        return throwError(() => new Error('Backlog item not found'));
      }
      this.mockItems = this.mockItems.filter(i => i.id !== id);
      return of(undefined).pipe(delay(200));
    }
    return this.http.delete<void>(`${this.getApiUrl(repositoryId)}/${id}`);
  }

  transition(repositoryId: string, id: string, dto: TransitionBacklogItemDto): Observable<BacklogItem> {
    if (this.useMocks) {
      const index = this.mockItems.findIndex(i => i.id === id && i.repositoryId === repositoryId);
      if (index === -1) {
        return throwError(() => new Error('Backlog item not found'));
      }
      const updatedItem: BacklogItem = {
        ...this.mockItems[index],
        state: dto.targetState,
        updatedAt: new Date(),
      };
      this.mockItems[index] = updatedItem;
      return of({ ...updatedItem }).pipe(delay(200));
    }
    return this.http.post<BacklogItem>(`${this.getApiUrl(repositoryId)}/${id}/transition`, dto);
  }

  startAgent(repositoryId: string, id: string): Observable<BacklogItem> {
    if (this.useMocks) {
      const index = this.mockItems.findIndex(i => i.id === id && i.repositoryId === repositoryId);
      if (index === -1) {
        return throwError(() => new Error('Backlog item not found'));
      }
      const updatedItem: BacklogItem = {
        ...this.mockItems[index],
        assignedAgentId: `agent-${Date.now()}`,
        updatedAt: new Date(),
      };
      this.mockItems[index] = updatedItem;
      return of({ ...updatedItem }).pipe(delay(500));
    }
    return this.http.post<BacklogItem>(`${this.getApiUrl(repositoryId)}/${id}/start-agent`, {});
  }

  abortAgent(repositoryId: string, id: string): Observable<BacklogItem> {
    if (this.useMocks) {
      const index = this.mockItems.findIndex(i => i.id === id && i.repositoryId === repositoryId);
      if (index === -1) {
        return throwError(() => new Error('Backlog item not found'));
      }
      const updatedItem: BacklogItem = {
        ...this.mockItems[index],
        assignedAgentId: undefined,
        updatedAt: new Date(),
      };
      this.mockItems[index] = updatedItem;
      return of({ ...updatedItem }).pipe(delay(300));
    }
    return this.http.post<BacklogItem>(`${this.getApiUrl(repositoryId)}/${id}/abort`, {});
  }

  pause(repositoryId: string, id: string, dto: PauseBacklogItemDto): Observable<BacklogItem> {
    if (this.useMocks) {
      const index = this.mockItems.findIndex(i => i.id === id && i.repositoryId === repositoryId);
      if (index === -1) {
        return throwError(() => new Error('Backlog item not found'));
      }
      const updatedItem: BacklogItem = {
        ...this.mockItems[index],
        isPaused: true,
        pauseReason: dto.reason,
        pausedAt: new Date(),
        updatedAt: new Date(),
      };
      this.mockItems[index] = updatedItem;
      return of({ ...updatedItem }).pipe(delay(300));
    }
    return this.http.post<BacklogItem>(`${this.getApiUrl(repositoryId)}/${id}/pause`, dto);
  }

  resume(repositoryId: string, id: string): Observable<BacklogItem> {
    if (this.useMocks) {
      const index = this.mockItems.findIndex(i => i.id === id && i.repositoryId === repositoryId);
      if (index === -1) {
        return throwError(() => new Error('Backlog item not found'));
      }
      const updatedItem: BacklogItem = {
        ...this.mockItems[index],
        isPaused: false,
        pauseReason: undefined,
        pausedAt: undefined,
        updatedAt: new Date(),
      };
      this.mockItems[index] = updatedItem;
      return of({ ...updatedItem }).pipe(delay(300));
    }
    return this.http.post<BacklogItem>(`${this.getApiUrl(repositoryId)}/${id}/resume`, {});
  }

  getLogs(repositoryId: string, id: string): Observable<TaskLog[]> {
    if (this.useMocks) {
      // Return empty logs in mock mode
      return of([]).pipe(delay(200));
    }
    return this.http.get<TaskLog[]>(`${this.getApiUrl(repositoryId)}/${id}/logs`);
  }

  // Helper methods
  getNextState(currentState: BacklogItemState): BacklogItemState | null {
    const currentIndex = BACKLOG_ITEM_STATES.indexOf(currentState);
    if (currentIndex < BACKLOG_ITEM_STATES.length - 1) {
      return BACKLOG_ITEM_STATES[currentIndex + 1];
    }
    return null;
  }

  getPreviousState(currentState: BacklogItemState): BacklogItemState | null {
    const currentIndex = BACKLOG_ITEM_STATES.indexOf(currentState);
    if (currentIndex > 0) {
      return BACKLOG_ITEM_STATES[currentIndex - 1];
    }
    return null;
  }
}
