import {
  Component,
  ChangeDetectionStrategy,
  input,
  output,
  signal,
  computed,
} from '@angular/core';
import { RouterLink } from '@angular/router';
import { Task, PipelineState } from '../../shared/models';
import { StateBadgeComponent } from '../../shared/components/state-badge.component';
import { PriorityBadgeComponent } from '../../shared/components/priority-badge.component';
import { AgentIndicatorComponent } from '../../shared/components/agent-indicator.component';
import { PausedBadgeComponent } from '../../shared/components/paused-badge.component';

@Component({
  selector: 'app-task-row',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    RouterLink,
    StateBadgeComponent,
    PriorityBadgeComponent,
    AgentIndicatorComponent,
    PausedBadgeComponent,
  ],
  template: `
    <div class="border-b border-gray-200 dark:border-gray-700">
      <!-- Main Row -->
      <div
        class="flex items-center gap-4 px-4 py-3 transition-colors hover:bg-gray-50 dark:hover:bg-gray-800/50"
        [class.bg-blue-50]="task().assignedAgentId"
        [class.dark:bg-blue-950/30]="task().assignedAgentId"
      >
        <!-- Expand/Collapse Toggle (only for parent tasks) -->
        <div class="w-6 flex-shrink-0">
          @if (isParent()) {
            <button
              type="button"
              class="flex h-6 w-6 items-center justify-center rounded text-gray-500 hover:bg-gray-200 dark:text-gray-400 dark:hover:bg-gray-700"
              [attr.aria-expanded]="isExpanded()"
              [attr.aria-label]="isExpanded() ? 'Collapse subtasks' : 'Expand subtasks'"
              (click)="toggleExpanded()"
            >
              <svg
                class="h-4 w-4 transition-transform"
                [class.rotate-90]="isExpanded()"
                xmlns="http://www.w3.org/2000/svg"
                viewBox="0 0 20 20"
                fill="currentColor"
              >
                <path
                  fill-rule="evenodd"
                  d="M7.21 14.77a.75.75 0 01.02-1.06L11.168 10 7.23 6.29a.75.75 0 111.04-1.08l4.5 4.25a.75.75 0 010 1.08l-4.5 4.25a.75.75 0 01-1.06-.02z"
                  clip-rule="evenodd"
                />
              </svg>
            </button>
          }
        </div>

        <!-- Title & Description -->
        <div class="min-w-0 flex-1">
          <div class="flex items-center gap-2">
            <a
              [routerLink]="['/tasks', task().id]"
              class="font-medium text-gray-900 hover:text-blue-600 dark:text-gray-100 dark:hover:text-blue-400"
            >
              {{ task().title }}
            </a>
            @if (task().hasError) {
              <span
                class="inline-flex items-center rounded-full bg-red-100 px-2 py-0.5 text-xs font-medium text-red-800 dark:bg-red-900 dark:text-red-200"
                aria-label="Task has error"
              >
                Error
              </span>
            }
            @if (task().isPaused) {
              <app-paused-badge [reason]="task().pauseReason" />
            }
          </div>
          @if (isParent() && task().progress) {
            <div class="mt-1 text-sm text-gray-500 dark:text-gray-400">
              {{ task().progress!.completed }}/{{ task().progress!.total }} subtasks done
            </div>
          }
        </div>

        <!-- State Badge -->
        <div class="w-28 flex-shrink-0">
          <app-state-badge [state]="displayState()" />
        </div>

        <!-- Priority Badge -->
        <div class="w-20 flex-shrink-0">
          <app-priority-badge [priority]="task().priority" />
        </div>

        <!-- Progress (for parent tasks) -->
        <div class="w-24 flex-shrink-0">
          @if (isParent() && task().progress) {
            <div class="flex items-center gap-2">
              <div class="h-2 flex-1 rounded-full bg-gray-200 dark:bg-gray-700">
                <div
                  class="h-2 rounded-full bg-green-500 transition-all"
                  [style.width.%]="task().progress!.percent"
                ></div>
              </div>
              <span class="text-xs text-gray-500 dark:text-gray-400">
                {{ task().progress!.percent }}%
              </span>
            </div>
          } @else if (task().assignedAgentId) {
            <app-agent-indicator />
          }
        </div>

        <!-- Actions -->
        <div class="w-32 flex-shrink-0 text-right">
          @if (!isParent()) {
            <button
              type="button"
              class="inline-flex items-center gap-1 rounded-md px-2 py-1 text-sm text-gray-600 hover:bg-gray-200 dark:text-gray-400 dark:hover:bg-gray-700"
              (click)="onSplit()"
            >
              <svg
                class="h-4 w-4"
                xmlns="http://www.w3.org/2000/svg"
                viewBox="0 0 20 20"
                fill="currentColor"
              >
                <path
                  fill-rule="evenodd"
                  d="M5.75 2a.75.75 0 01.75.75V7h4.5V2.75a.75.75 0 011.5 0V7h3.75a.75.75 0 010 1.5h-3.75v8.75a.75.75 0 01-1.5 0V8.5h-4.5v8.75a.75.75 0 01-1.5 0V8.5H.75a.75.75 0 010-1.5h4.25V2.75A.75.75 0 015.75 2z"
                  clip-rule="evenodd"
                />
              </svg>
              Split
            </button>
          }
        </div>
      </div>

      <!-- Children Rows -->
      @if (isParent() && isExpanded()) {
        <div class="bg-gray-50 dark:bg-gray-800/30">
          @for (child of children(); track child.id) {
            <div
              class="flex items-center gap-4 border-t border-gray-100 px-4 py-2 pl-14 transition-colors hover:bg-gray-100 dark:border-gray-700/50 dark:hover:bg-gray-800"
              [class.bg-blue-50]="child.assignedAgentId"
              [class.dark:bg-blue-950/30]="child.assignedAgentId"
            >
              <!-- Indent indicator -->
              <div class="w-4 flex-shrink-0">
                <div class="h-px w-3 bg-gray-300 dark:bg-gray-600"></div>
              </div>

              <!-- Title -->
              <div class="min-w-0 flex-1">
                <div class="flex items-center gap-2">
                  <a
                    [routerLink]="['/tasks', child.id]"
                    class="text-sm font-medium text-gray-700 hover:text-blue-600 dark:text-gray-300 dark:hover:text-blue-400"
                  >
                    {{ child.title }}
                  </a>
                  @if (child.hasError) {
                    <span
                      class="inline-flex items-center rounded-full bg-red-100 px-2 py-0.5 text-xs font-medium text-red-800 dark:bg-red-900 dark:text-red-200"
                    >
                      Error
                    </span>
                  }
                  @if (child.isPaused) {
                    <app-paused-badge [reason]="child.pauseReason" />
                  }
                </div>
              </div>

              <!-- State -->
              <div class="w-28 flex-shrink-0">
                <app-state-badge [state]="child.state" />
              </div>

              <!-- Priority -->
              <div class="w-20 flex-shrink-0">
                <app-priority-badge [priority]="child.priority" />
              </div>

              <!-- Agent indicator -->
              <div class="w-24 flex-shrink-0">
                @if (child.assignedAgentId) {
                  <app-agent-indicator />
                }
              </div>

              <!-- Spacer for actions alignment -->
              <div class="w-32 flex-shrink-0"></div>
            </div>
          }
        </div>
      }
    </div>
  `,
})
export class TaskRowComponent {
  readonly task = input.required<Task>();
  readonly children = input<Task[]>([]);
  readonly initialExpanded = input(false);

  readonly split = output<string>();

  protected readonly isExpanded = signal(false);

  protected readonly isParent = computed(() => this.task().childCount > 0);

  protected readonly displayState = computed(
    () => this.task().derivedState ?? this.task().state
  );

  constructor() {
    // Initialize expanded state from input
    if (this.initialExpanded()) {
      this.isExpanded.set(true);
    }
  }

  toggleExpanded(): void {
    this.isExpanded.update(v => !v);
  }

  onSplit(): void {
    this.split.emit(this.task().id);
  }
}
