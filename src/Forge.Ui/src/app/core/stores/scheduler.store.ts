import { Injectable, computed, inject, signal } from '@angular/core';
import { SchedulerStatus } from '../../shared/models';
import { SchedulerService } from '../services/scheduler.service';
import { firstValueFrom } from 'rxjs';

@Injectable({ providedIn: 'root' })
export class SchedulerStore {
  private readonly schedulerService = inject(SchedulerService);

  // State
  private readonly status = signal<SchedulerStatus | null>(null);
  private readonly loading = signal(false);
  private readonly error = signal<string | null>(null);

  // Public readonly signals
  readonly isLoading = this.loading.asReadonly();
  readonly errorMessage = this.error.asReadonly();
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
    this.loading.set(true);
    this.error.set(null);

    try {
      const status = await firstValueFrom(this.schedulerService.getStatus());
      this.status.set(status);
    } catch (err) {
      this.error.set(err instanceof Error ? err.message : 'Failed to load scheduler status');
    } finally {
      this.loading.set(false);
    }
  }

  async enable(): Promise<boolean> {
    this.error.set(null);

    try {
      await firstValueFrom(this.schedulerService.enable());
      this.status.update(s => s ? { ...s, isEnabled: true } : null);
      return true;
    } catch (err) {
      this.error.set(err instanceof Error ? err.message : 'Failed to enable scheduler');
      return false;
    }
  }

  async disable(): Promise<boolean> {
    this.error.set(null);

    try {
      await firstValueFrom(this.schedulerService.disable());
      this.status.update(s => s ? { ...s, isEnabled: false } : null);
      return true;
    } catch (err) {
      this.error.set(err instanceof Error ? err.message : 'Failed to disable scheduler');
      return false;
    }
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
