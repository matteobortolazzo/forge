import { Component, ChangeDetectionStrategy, input, output, computed } from '@angular/core';
import { RouterLink } from '@angular/router';
import { Task } from '../../shared/models';
import { PriorityBadgeComponent } from '../../shared/components/priority-badge.component';
import { AgentIndicatorComponent } from '../../shared/components/agent-indicator.component';

@Component({
  selector: 'app-task-card',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterLink, PriorityBadgeComponent, AgentIndicatorComponent],
  template: `
    <a
      [routerLink]="['/tasks', task().id]"
      [class]="cardClasses()"
      [attr.aria-label]="'View task: ' + task().title"
    >
      <div class="flex items-start justify-between gap-2">
        <h3 class="text-sm font-medium text-gray-900 dark:text-gray-100 line-clamp-2">
          {{ task().title }}
        </h3>
        @if (task().assignedAgentId) {
          <app-agent-indicator [isRunning]="true" />
        }
      </div>

      @if (task().description) {
        <p class="mt-1 text-xs text-gray-500 dark:text-gray-400 line-clamp-2">
          {{ task().description }}
        </p>
      }

      <div class="mt-3 flex items-center justify-between">
        <app-priority-badge [priority]="task().priority" />

        @if (task().hasError) {
          <span
            class="inline-flex items-center gap-1 text-xs font-medium text-red-600 dark:text-red-400"
            aria-label="Task has error"
          >
            <svg
              class="h-4 w-4"
              xmlns="http://www.w3.org/2000/svg"
              viewBox="0 0 20 20"
              fill="currentColor"
              aria-hidden="true"
            >
              <path
                fill-rule="evenodd"
                d="M18 10a8 8 0 11-16 0 8 8 0 0116 0zm-8-5a.75.75 0 01.75.75v4.5a.75.75 0 01-1.5 0v-4.5A.75.75 0 0110 5zm0 10a1 1 0 100-2 1 1 0 000 2z"
                clip-rule="evenodd"
              />
            </svg>
            Error
          </span>
        }
      </div>
    </a>
  `,
  styles: `
    :host {
      display: block;
    }
  `,
})
export class TaskCardComponent {
  readonly task = input.required<Task>();

  readonly cardClasses = computed(() => {
    const base =
      'block rounded-lg border bg-white p-3 shadow-sm transition-all hover:shadow-md focus:outline-none focus:ring-2 focus:ring-blue-500 focus:ring-offset-2 dark:bg-gray-800';

    if (this.task().hasError) {
      return `${base} border-red-300 dark:border-red-700`;
    }

    if (this.task().assignedAgentId) {
      return `${base} border-green-300 dark:border-green-700`;
    }

    return `${base} border-gray-200 dark:border-gray-700`;
  });
}
