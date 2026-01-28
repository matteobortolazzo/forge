import { Injectable, computed, inject, signal } from '@angular/core';
import { SchedulerStatus } from '../../shared/models';
import { SchedulerService } from '../services/scheduler.service';
import { firstValueFrom } from 'rxjs';
import { createAsyncState, runAsync } from './store-utils';

@Injectable({ providedIn: 'root' })
export class SchedulerStore {
  private readonly schedulerService = inject(SchedulerService);

  // State
  private readonly status = signal<SchedulerStatus | null>(null);
  private readonly asyncState = createAsyncState();

  // Public readonly signals
  readonly isLoading = this.asyncState.loading.asReadonly();
  readonly errorMessage = this.asyncState.error.asReadonly();
  readonly schedulerStatus = this.status.asReadonly();

  // Computed signals
  readonly isEnabled = computed(() => this.status()?.isEnabled ?? false);
  readonly isAgentRunning = computed(() => this.status()?.isAgentRunning ?? false);
  readonly currentTaskId = computed(() => this.status()?.currentTaskId);
  readonly currentBacklogItemId = computed(() => this.status()?.currentBacklogItemId);
  readonly pendingTaskCount = computed(() => this.status()?.pendingTaskCount ?? 0);
  readonly pausedTaskCount = computed(() => this.status()?.pausedTaskCount ?? 0);

  // Actions
  async loadStatus(): Promise<void> {
    await runAsync(
      this.asyncState,
      async () => {
        const status = await firstValueFrom(this.schedulerService.getStatus());
        this.status.set(status);
      },
      {},
      'Failed to load scheduler status'
    );
  }

  async enable(): Promise<boolean> {
    return (
      (await runAsync(
        this.asyncState,
        async () => {
          await firstValueFrom(this.schedulerService.enable());
          this.status.update(s => (s ? { ...s, isEnabled: true } : null));
          return true;
        },
        { setLoading: false },
        'Failed to enable scheduler'
      )) ?? false
    );
  }

  async disable(): Promise<boolean> {
    return (
      (await runAsync(
        this.asyncState,
        async () => {
          await firstValueFrom(this.schedulerService.disable());
          this.status.update(s => (s ? { ...s, isEnabled: false } : null));
          return true;
        },
        { setLoading: false },
        'Failed to disable scheduler'
      )) ?? false
    );
  }

  // Update from SSE events
  updateFromScheduledEvent(taskId: string): void {
    this.status.update(s => s ? {
      ...s,
      isAgentRunning: true,
      currentTaskId: taskId,
    } : null);
  }

  updateAgentStatus(isRunning: boolean, taskId?: string, backlogItemId?: string): void {
    this.status.update(s => s ? {
      ...s,
      isAgentRunning: isRunning,
      currentTaskId: taskId,
      currentBacklogItemId: backlogItemId,
    } : null);
  }

  updateCounts(pendingCount?: number, pausedCount?: number): void {
    this.status.update(s => {
      if (!s) return null;
      return {
        ...s,
        pendingTaskCount: pendingCount ?? s.pendingTaskCount,
        pausedTaskCount: pausedCount ?? s.pausedTaskCount,
      };
    });
  }
}
