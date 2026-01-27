import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, of, delay, throwError } from 'rxjs';
import {
  Task,
  TaskLog,
  CreateTaskDto,
  UpdateTaskDto,
  TransitionTaskDto,
  PauseTaskDto,
  PIPELINE_STATES,
  PipelineState,
  CreateSubtaskDto,
  SplitTaskDto,
  SplitTaskResultDto,
} from '../../shared/models';
import { MOCK_TASKS, getLogsForTask } from '../mocks/mock-data';

@Injectable({ providedIn: 'root' })
export class TaskService {
  private readonly http = inject(HttpClient);
  private readonly useMocks = false;

  // In-memory task store for mock mode
  private mockTasks: Task[] = [...MOCK_TASKS];

  private getApiUrl(repositoryId: string): string {
    return `/api/repositories/${repositoryId}/tasks`;
  }

  getTasks(repositoryId: string, rootOnly = false): Observable<Task[]> {
    if (this.useMocks) {
      const tasks = rootOnly
        ? this.mockTasks.filter(t => !t.parentId && t.repositoryId === repositoryId)
        : this.mockTasks.filter(t => t.repositoryId === repositoryId);
      return of([...tasks]).pipe(delay(300));
    }
    const params = rootOnly ? '?rootOnly=true' : '';
    return this.http.get<Task[]>(`${this.getApiUrl(repositoryId)}${params}`);
  }

  getTask(repositoryId: string, id: string): Observable<Task> {
    if (this.useMocks) {
      const task = this.mockTasks.find(t => t.id === id && t.repositoryId === repositoryId);
      if (task) {
        return of({ ...task }).pipe(delay(200));
      }
      return throwError(() => new Error('Task not found'));
    }
    return this.http.get<Task>(`${this.getApiUrl(repositoryId)}/${id}`);
  }

  createTask(repositoryId: string, dto: CreateTaskDto): Observable<Task> {
    if (this.useMocks) {
      const newTask: Task = {
        id: `task-${Date.now()}`,
        repositoryId,
        title: dto.title,
        description: dto.description,
        priority: dto.priority,
        state: 'Backlog',
        hasError: false,
        isPaused: false,
        retryCount: 0,
        maxRetries: 3,
        childCount: 0,
        createdAt: new Date(),
        updatedAt: new Date(),
      };
      this.mockTasks = [newTask, ...this.mockTasks];
      return of({ ...newTask }).pipe(delay(300));
    }
    return this.http.post<Task>(this.getApiUrl(repositoryId), dto);
  }

  updateTask(repositoryId: string, id: string, dto: UpdateTaskDto): Observable<Task> {
    if (this.useMocks) {
      const index = this.mockTasks.findIndex(t => t.id === id && t.repositoryId === repositoryId);
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
    return this.http.patch<Task>(`${this.getApiUrl(repositoryId)}/${id}`, dto);
  }

  deleteTask(repositoryId: string, id: string): Observable<void> {
    if (this.useMocks) {
      const index = this.mockTasks.findIndex(t => t.id === id && t.repositoryId === repositoryId);
      if (index === -1) {
        return throwError(() => new Error('Task not found'));
      }
      this.mockTasks = this.mockTasks.filter(t => t.id !== id);
      return of(undefined).pipe(delay(200));
    }
    return this.http.delete<void>(`${this.getApiUrl(repositoryId)}/${id}`);
  }

  transitionTask(repositoryId: string, id: string, dto: TransitionTaskDto): Observable<Task> {
    if (this.useMocks) {
      const index = this.mockTasks.findIndex(t => t.id === id && t.repositoryId === repositoryId);
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
    return this.http.post<Task>(`${this.getApiUrl(repositoryId)}/${id}/transition`, dto);
  }

  getTaskLogs(repositoryId: string, taskId: string): Observable<TaskLog[]> {
    if (this.useMocks) {
      const logs = getLogsForTask(taskId);
      return of([...logs]).pipe(delay(200));
    }
    return this.http.get<TaskLog[]>(`${this.getApiUrl(repositoryId)}/${taskId}/logs`);
  }

  abortAgent(repositoryId: string, taskId: string): Observable<Task> {
    if (this.useMocks) {
      const index = this.mockTasks.findIndex(t => t.id === taskId && t.repositoryId === repositoryId);
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
    return this.http.post<Task>(`${this.getApiUrl(repositoryId)}/${taskId}/abort`, {});
  }

  startAgent(repositoryId: string, taskId: string): Observable<Task> {
    if (this.useMocks) {
      const index = this.mockTasks.findIndex(t => t.id === taskId && t.repositoryId === repositoryId);
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
    return this.http.post<Task>(`${this.getApiUrl(repositoryId)}/${taskId}/start-agent`, {});
  }

  pauseTask(repositoryId: string, taskId: string, dto: PauseTaskDto): Observable<Task> {
    if (this.useMocks) {
      const index = this.mockTasks.findIndex(t => t.id === taskId && t.repositoryId === repositoryId);
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
    return this.http.post<Task>(`${this.getApiUrl(repositoryId)}/${taskId}/pause`, dto);
  }

  resumeTask(repositoryId: string, taskId: string): Observable<Task> {
    if (this.useMocks) {
      const index = this.mockTasks.findIndex(t => t.id === taskId && t.repositoryId === repositoryId);
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
    return this.http.post<Task>(`${this.getApiUrl(repositoryId)}/${taskId}/resume`, {});
  }

  // Hierarchy methods
  splitTask(repositoryId: string, taskId: string, dto: SplitTaskDto): Observable<SplitTaskResultDto> {
    if (this.useMocks) {
      const index = this.mockTasks.findIndex(t => t.id === taskId && t.repositoryId === repositoryId);
      if (index === -1) {
        return throwError(() => new Error('Task not found'));
      }
      const parent = this.mockTasks[index];
      const children: Task[] = dto.subtasks.map((subtask, i) => ({
        id: `${taskId}-child-${i}`,
        repositoryId,
        title: subtask.title,
        description: subtask.description,
        priority: subtask.priority,
        state: 'Backlog' as PipelineState,
        parentId: taskId,
        childCount: 0,
        hasError: false,
        isPaused: false,
        retryCount: 0,
        maxRetries: 3,
        createdAt: new Date(),
        updatedAt: new Date(),
      }));
      const updatedParent: Task = {
        ...parent,
        childCount: children.length,
        derivedState: 'Backlog',
        children,
        progress: { completed: 0, total: children.length, percent: 0 },
        updatedAt: new Date(),
      };
      this.mockTasks[index] = updatedParent;
      this.mockTasks.push(...children);
      return of({ parent: updatedParent, children }).pipe(delay(300));
    }
    return this.http.post<SplitTaskResultDto>(`${this.getApiUrl(repositoryId)}/${taskId}/split`, dto);
  }

  addChild(repositoryId: string, parentId: string, dto: CreateSubtaskDto): Observable<Task> {
    if (this.useMocks) {
      const parentIndex = this.mockTasks.findIndex(t => t.id === parentId && t.repositoryId === repositoryId);
      if (parentIndex === -1) {
        return throwError(() => new Error('Parent task not found'));
      }
      const child: Task = {
        id: `${parentId}-child-${Date.now()}`,
        repositoryId,
        title: dto.title,
        description: dto.description,
        priority: dto.priority,
        state: 'Backlog',
        parentId,
        childCount: 0,
        hasError: false,
        isPaused: false,
        retryCount: 0,
        maxRetries: 3,
        createdAt: new Date(),
        updatedAt: new Date(),
      };
      this.mockTasks.push(child);
      const parent = this.mockTasks[parentIndex];
      parent.childCount++;
      parent.updatedAt = new Date();
      return of(child).pipe(delay(300));
    }
    return this.http.post<Task>(`${this.getApiUrl(repositoryId)}/${parentId}/children`, dto);
  }

  getChildren(repositoryId: string, parentId: string): Observable<Task[]> {
    if (this.useMocks) {
      const children = this.mockTasks.filter(t => t.parentId === parentId && t.repositoryId === repositoryId);
      return of([...children]).pipe(delay(200));
    }
    return this.http.get<Task[]>(`${this.getApiUrl(repositoryId)}/${parentId}/children`);
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

  isLeafTask(task: Task): boolean {
    return task.childCount === 0;
  }

  isParentTask(task: Task): boolean {
    return task.childCount > 0;
  }

  getDisplayState(task: Task): PipelineState {
    return task.derivedState ?? task.state;
  }
}
