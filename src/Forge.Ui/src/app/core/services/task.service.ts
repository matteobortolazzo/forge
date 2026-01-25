import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, of, delay, throwError } from 'rxjs';
import {
  Task,
  TaskLog,
  CreateTaskDto,
  UpdateTaskDto,
  TransitionTaskDto,
  AgentStatus,
  PIPELINE_STATES,
  PipelineState,
} from '../../shared/models';
import { MOCK_TASKS, getLogsForTask, getTaskById } from '../mocks/mock-data';

@Injectable({ providedIn: 'root' })
export class TaskService {
  private readonly http = inject(HttpClient);
  private readonly useMocks = true;
  private readonly apiUrl = '/api/tasks';

  // In-memory task store for mock mode
  private mockTasks: Task[] = [...MOCK_TASKS];

  getTasks(): Observable<Task[]> {
    if (this.useMocks) {
      return of([...this.mockTasks]).pipe(delay(300));
    }
    return this.http.get<Task[]>(this.apiUrl);
  }

  getTask(id: string): Observable<Task> {
    if (this.useMocks) {
      const task = this.mockTasks.find(t => t.id === id);
      if (task) {
        return of({ ...task }).pipe(delay(200));
      }
      return throwError(() => new Error('Task not found'));
    }
    return this.http.get<Task>(`${this.apiUrl}/${id}`);
  }

  createTask(dto: CreateTaskDto): Observable<Task> {
    if (this.useMocks) {
      const newTask: Task = {
        id: `task-${Date.now()}`,
        title: dto.title,
        description: dto.description,
        priority: dto.priority,
        state: 'Backlog',
        hasError: false,
        createdAt: new Date(),
        updatedAt: new Date(),
      };
      this.mockTasks = [newTask, ...this.mockTasks];
      return of({ ...newTask }).pipe(delay(300));
    }
    return this.http.post<Task>(this.apiUrl, dto);
  }

  updateTask(id: string, dto: UpdateTaskDto): Observable<Task> {
    if (this.useMocks) {
      const index = this.mockTasks.findIndex(t => t.id === id);
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
    return this.http.patch<Task>(`${this.apiUrl}/${id}`, dto);
  }

  deleteTask(id: string): Observable<void> {
    if (this.useMocks) {
      const index = this.mockTasks.findIndex(t => t.id === id);
      if (index === -1) {
        return throwError(() => new Error('Task not found'));
      }
      this.mockTasks = this.mockTasks.filter(t => t.id !== id);
      return of(undefined).pipe(delay(200));
    }
    return this.http.delete<void>(`${this.apiUrl}/${id}`);
  }

  transitionTask(id: string, dto: TransitionTaskDto): Observable<Task> {
    if (this.useMocks) {
      const index = this.mockTasks.findIndex(t => t.id === id);
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
    return this.http.post<Task>(`${this.apiUrl}/${id}/transition`, dto);
  }

  getTaskLogs(taskId: string): Observable<TaskLog[]> {
    if (this.useMocks) {
      const logs = getLogsForTask(taskId);
      return of([...logs]).pipe(delay(200));
    }
    return this.http.get<TaskLog[]>(`${this.apiUrl}/${taskId}/logs`);
  }

  abortAgent(taskId: string): Observable<Task> {
    if (this.useMocks) {
      const index = this.mockTasks.findIndex(t => t.id === taskId);
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
    return this.http.post<Task>(`${this.apiUrl}/${taskId}/abort`, {});
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
