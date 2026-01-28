import { Injectable, inject, signal } from '@angular/core';
import { TaskLog } from '../../shared/models';
import { TaskService } from '../services/task.service';
import { TaskStore } from './task.store';
import { firstValueFrom } from 'rxjs';
import { createAsyncState, runAsync } from './store-utils';

@Injectable({ providedIn: 'root' })
export class LogStore {
  private readonly taskService = inject(TaskService);
  private readonly taskStore = inject(TaskStore);

  // State: logs grouped by task ID
  private readonly logsByTaskId = signal<Map<string, TaskLog[]>>(new Map());
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

  // Add a log from SSE event
  addLog(log: TaskLog): void {
    // Skip if no taskId (might be a backlog item log)
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
  }
}
