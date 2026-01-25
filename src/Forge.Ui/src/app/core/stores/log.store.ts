import { Injectable, computed, inject, signal } from '@angular/core';
import { TaskLog } from '../../shared/models';
import { TaskService } from '../services/task.service';
import { firstValueFrom } from 'rxjs';

@Injectable({ providedIn: 'root' })
export class LogStore {
  private readonly taskService = inject(TaskService);

  // State: logs grouped by task ID
  private readonly logsByTaskId = signal<Map<string, TaskLog[]>>(new Map());
  private readonly loading = signal(false);
  private readonly error = signal<string | null>(null);

  // Public readonly signals
  readonly isLoading = this.loading.asReadonly();
  readonly errorMessage = this.error.asReadonly();

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
    this.loading.set(true);
    this.error.set(null);

    try {
      const logs = await firstValueFrom(this.taskService.getTaskLogs(taskId));
      this.logsByTaskId.update(map => {
        const newMap = new Map(map);
        newMap.set(taskId, logs);
        return newMap;
      });
    } catch (err) {
      this.error.set(err instanceof Error ? err.message : 'Failed to load logs');
    } finally {
      this.loading.set(false);
    }
  }

  // Add a log from SSE event
  addLog(log: TaskLog): void {
    this.logsByTaskId.update(map => {
      const newMap = new Map(map);
      const existingLogs = newMap.get(log.taskId) ?? [];
      // Avoid duplicates
      if (!existingLogs.some(l => l.id === log.id)) {
        newMap.set(log.taskId, [...existingLogs, log]);
      }
      return newMap;
    });
  }

  // Clear logs for a task
  clearLogsForTask(taskId: string): void {
    this.logsByTaskId.update(map => {
      const newMap = new Map(map);
      newMap.delete(taskId);
      return newMap;
    });
  }

  // Clear all logs
  clearAllLogs(): void {
    this.logsByTaskId.set(new Map());
  }
}
