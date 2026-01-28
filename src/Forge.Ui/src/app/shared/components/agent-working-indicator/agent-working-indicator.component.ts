import {
  Component,
  ChangeDetectionStrategy,
  inject,
  computed,
} from '@angular/core';
import { Router } from '@angular/router';
import { SchedulerStore } from '../../../core/stores/scheduler.store';
import { BacklogStore } from '../../../core/stores/backlog.store';
import { TaskStore } from '../../../core/stores/task.store';

@Component({
  selector: 'app-agent-working-indicator',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    @if (schedulerStore.isAgentRunning() && displayInfo()) {
      <button
        type="button"
        class="group flex items-center gap-2 rounded-md bg-green-50 px-3 py-1.5 text-sm font-medium text-green-700 transition-colors hover:bg-green-100 focus:outline-none focus:ring-2 focus:ring-green-500 focus:ring-offset-2 dark:bg-green-900/30 dark:text-green-400 dark:hover:bg-green-900/50"
        (click)="navigateToWork()"
        [attr.aria-label]="'Navigate to ' + displayInfo()!.label"
      >
        <!-- Animated ping indicator -->
        <span class="relative flex h-2.5 w-2.5">
          <span
            class="absolute inline-flex h-full w-full animate-ping rounded-full bg-green-400 opacity-75"
          ></span>
          <span
            class="relative inline-flex h-2.5 w-2.5 rounded-full bg-green-500"
          ></span>
        </span>

        <!-- Label -->
        <span class="max-w-[180px] truncate">
          {{ displayInfo()!.label }}
        </span>

        <!-- Chevron -->
        <svg
          class="h-4 w-4 text-green-500 transition-transform group-hover:translate-x-0.5 dark:text-green-400"
          fill="none"
          viewBox="0 0 24 24"
          stroke-width="1.5"
          stroke="currentColor"
          aria-hidden="true"
        >
          <path
            stroke-linecap="round"
            stroke-linejoin="round"
            d="M8.25 4.5l7.5 7.5-7.5 7.5"
          />
        </svg>
      </button>
    }
  `,
})
export class AgentWorkingIndicatorComponent {
  protected readonly schedulerStore = inject(SchedulerStore);
  private readonly backlogStore = inject(BacklogStore);
  private readonly taskStore = inject(TaskStore);
  private readonly router = inject(Router);

  // Computed: get display info based on what's currently running
  readonly displayInfo = computed(() => {
    const backlogItemId = this.schedulerStore.currentBacklogItemId();
    const taskId = this.schedulerStore.currentTaskId();

    // Check if we have a backlog item running (refining/splitting)
    if (backlogItemId && !taskId) {
      const item = this.backlogStore.getItemById(backlogItemId);
      if (item) {
        return {
          type: 'backlog' as const,
          label: `Refining: ${item.title}`,
          route: ['/backlog', backlogItemId],
        };
      }
    }

    // Check if we have a task running
    if (taskId) {
      const task = this.taskStore.getTaskById(taskId);
      if (task) {
        return {
          type: 'task' as const,
          label: `Working: ${task.title}`,
          route: ['/backlog', task.backlogItemId, 'tasks', taskId],
        };
      }
    }

    return null;
  });

  navigateToWork(): void {
    const info = this.displayInfo();
    if (info) {
      this.router.navigate(info.route);
    }
  }
}
