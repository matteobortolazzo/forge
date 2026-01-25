import { Component, ChangeDetectionStrategy, input, computed } from '@angular/core';
import { Task, PipelineState } from '../../shared/models';
import { TaskCardComponent } from './task-card.component';

@Component({
  selector: 'app-task-column',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [TaskCardComponent],
  template: `
    <div class="flex h-full flex-col rounded-lg bg-gray-50 dark:bg-gray-900">
      <div class="flex items-center justify-between border-b border-gray-200 px-3 py-2 dark:border-gray-700">
        <h2 class="text-sm font-semibold text-gray-700 dark:text-gray-300">
          {{ stateLabel() }}
        </h2>
        <span
          class="inline-flex h-5 min-w-5 items-center justify-center rounded-full bg-gray-200 px-1.5 text-xs font-medium text-gray-600 dark:bg-gray-700 dark:text-gray-400"
          [attr.aria-label]="tasks().length + ' tasks in ' + stateLabel()"
        >
          {{ tasks().length }}
        </span>
      </div>

      <div class="flex-1 overflow-y-auto p-2">
        <div class="space-y-2" role="list" [attr.aria-label]="stateLabel() + ' tasks'">
          @for (task of tasks(); track task.id) {
            <app-task-card [task]="task" />
          } @empty {
            <p class="py-4 text-center text-sm text-gray-400 dark:text-gray-500">
              No tasks
            </p>
          }
        </div>
      </div>
    </div>
  `,
  styles: `
    :host {
      display: block;
      height: 100%;
    }
  `,
})
export class TaskColumnComponent {
  readonly state = input.required<PipelineState>();
  readonly tasks = input.required<Task[]>();

  readonly stateLabel = computed(() => {
    switch (this.state()) {
      case 'PrReady':
        return 'PR Ready';
      default:
        return this.state();
    }
  });
}
