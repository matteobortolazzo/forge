import { Injectable, computed, inject, signal } from '@angular/core';
import { AgentStatus } from '../../shared/models';
import { AgentService } from '../services/agent.service';
import { TaskService } from '../services/task.service';
import { TaskStore } from './task.store';
import { firstValueFrom } from 'rxjs';

@Injectable({ providedIn: 'root' })
export class AgentStore {
  private readonly agentService = inject(AgentService);
  private readonly taskService = inject(TaskService);
  private readonly taskStore = inject(TaskStore);

  // State
  private readonly status = signal<AgentStatus>({
    isRunning: false,
    currentTaskId: undefined,
    startedAt: undefined,
  });
  private readonly loading = signal(false);
  private readonly error = signal<string | null>(null);

  // Public readonly signals
  readonly isLoading = this.loading.asReadonly();
  readonly errorMessage = this.error.asReadonly();
  readonly agentStatus = this.status.asReadonly();
  readonly isAgentRunning = computed(() => this.status().isRunning);
  readonly currentTaskId = computed(() => this.status().currentTaskId);

  // Actions
  async loadStatus(): Promise<void> {
    this.loading.set(true);
    this.error.set(null);

    try {
      const status = await firstValueFrom(this.agentService.getStatus());
      this.status.set(status);
    } catch (err) {
      this.error.set(err instanceof Error ? err.message : 'Failed to load agent status');
    } finally {
      this.loading.set(false);
    }
  }

  async startAgent(taskId: string): Promise<boolean> {
    this.error.set(null);

    try {
      const task = this.taskStore.getTaskById(taskId);
      if (!task) {
        throw new Error('Task not found');
      }
      await firstValueFrom(this.taskService.startAgent(task.repositoryId, taskId));
      this.status.set({
        isRunning: true,
        currentTaskId: taskId,
        startedAt: new Date(),
      });
      return true;
    } catch (err) {
      this.error.set(err instanceof Error ? err.message : 'Failed to start agent');
      return false;
    }
  }

  updateStatusFromEvent(status: AgentStatus): void {
    this.status.set(status);
  }

  clearStatus(): void {
    this.status.set({
      isRunning: false,
      currentTaskId: undefined,
      startedAt: undefined,
    });
  }
}
