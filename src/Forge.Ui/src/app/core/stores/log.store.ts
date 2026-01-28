import { Injectable, inject, signal } from '@angular/core';
import { TaskLog } from '../../shared/models';
import { TaskService } from '../services/task.service';
import { BacklogService } from '../services/backlog.service';
import { TaskStore } from './task.store';
import { firstValueFrom } from 'rxjs';
import { createAsyncState, runAsync } from './store-utils';

@Injectable({ providedIn: 'root' })
export class LogStore {
  private readonly taskService = inject(TaskService);
  private readonly backlogService = inject(BacklogService);
  private readonly taskStore = inject(TaskStore);

  // State: logs grouped by task ID
  private readonly logsByTaskId = signal<Map<string, TaskLog[]>>(new Map());
  // State: logs grouped by backlog item ID
  private readonly logsByBacklogItemId = signal<Map<string, TaskLog[]>>(new Map());
  private readonly asyncState = createAsyncState();

  // Public readonly signals
  readonly isLoading = this.asyncState.loading.asReadonly();
  readonly errorMessage = this.asyncState.error.asReadonly();

  // Get logs for a specific task
  getLogsForTask(taskId: string): TaskLog[] {
    return this.logsByTaskId().get(taskId) ?? [];
  }

  // Computed: get log count for a task
  getLogCount(taskId: string): number {
    return this.getLogsForTask(taskId).length;
  }

  // Actions
  async loadLogsForTask(taskId: string): Promise<void> {
    await runAsync(
      this.asyncState,
      async () => {
        const task = this.taskStore.getTaskById(taskId);
        if (!task) {
          throw new Error('Task not found');
        }
        const logs = await firstValueFrom(
          this.taskService.getTaskLogs(task.repositoryId, task.backlogItemId, taskId)
        );
        this.setLogsForTask(taskId, logs);
      },
      {},
      'Failed to load logs'
    );
  }

  // Add a log from SSE event (handles both task logs and backlog item logs)
  addLog(log: TaskLog): void {
    // Handle backlog item logs (no taskId but has backlogItemId)
    if (!log.taskId && log.backlogItemId) {
      const backlogItemId = log.backlogItemId;
      this.logsByBacklogItemId.update(map => {
        const existingLogs = map.get(backlogItemId) ?? [];
        // Avoid duplicates
        if (existingLogs.some(l => l.id === log.id)) {
          return map;
        }
        const newMap = new Map(map);
        newMap.set(backlogItemId, [...existingLogs, log]);
        return newMap;
      });
      return;
    }

    // Handle task logs
    if (!log.taskId) {
      return;
    }
    const taskId = log.taskId;
    this.logsByTaskId.update(map => {
      const existingLogs = map.get(taskId) ?? [];
      // Avoid duplicates
      if (existingLogs.some(l => l.id === log.id)) {
        return map; // No change needed
      }
      // Only create new Map when actually adding
      const newMap = new Map(map);
      newMap.set(taskId, [...existingLogs, log]);
      return newMap;
    });
  }

  // Clear logs for a task
  clearLogsForTask(taskId: string): void {
    this.logsByTaskId.update(map => {
      if (!map.has(taskId)) {
        return map; // No change needed
      }
      const newMap = new Map(map);
      newMap.delete(taskId);
      return newMap;
    });
  }

  // Helper to set logs for a task (avoids copy if unchanged)
  private setLogsForTask(taskId: string, logs: TaskLog[]): void {
    this.logsByTaskId.update(map => {
      const existing = map.get(taskId);
      // Skip update if same reference (shouldn't happen but safety check)
      if (existing === logs) {
        return map;
      }
      const newMap = new Map(map);
      newMap.set(taskId, logs);
      return newMap;
    });
  }

  // Clear all logs
  clearAllLogs(): void {
    this.logsByTaskId.set(new Map());
    this.logsByBacklogItemId.set(new Map());
  }

  // ========================================
  // Backlog Item Log Methods
  // ========================================

  // Get logs for a specific backlog item
  getLogsForBacklogItem(backlogItemId: string): TaskLog[] {
    return this.logsByBacklogItemId().get(backlogItemId) ?? [];
  }

  // Get log count for a backlog item
  getBacklogItemLogCount(backlogItemId: string): number {
    return this.getLogsForBacklogItem(backlogItemId).length;
  }

  // Load logs for a backlog item from backend
  async loadLogsForBacklogItem(backlogItemId: string, repositoryId: string): Promise<void> {
    await runAsync(
      this.asyncState,
      async () => {
        const logs = await firstValueFrom(
          this.backlogService.getLogs(repositoryId, backlogItemId)
        );
        this.setLogsForBacklogItem(backlogItemId, logs);
      },
      {},
      'Failed to load backlog item logs'
    );
  }

  // Clear logs for a backlog item
  clearLogsForBacklogItem(backlogItemId: string): void {
    this.logsByBacklogItemId.update(map => {
      if (!map.has(backlogItemId)) {
        return map;
      }
      const newMap = new Map(map);
      newMap.delete(backlogItemId);
      return newMap;
    });
  }

  // Helper to set logs for a backlog item
  private setLogsForBacklogItem(backlogItemId: string, logs: TaskLog[]): void {
    this.logsByBacklogItemId.update(map => {
      const existing = map.get(backlogItemId);
      if (existing === logs) {
        return map;
      }
      const newMap = new Map(map);
      newMap.set(backlogItemId, logs);
      return newMap;
    });
  }
}
