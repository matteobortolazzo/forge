import {
  Component,
  ChangeDetectionStrategy,
  inject,
  signal,
  computed,
  OnInit,
} from '@angular/core';
import { Router, ActivatedRoute, RouterLink } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { SlicePipe } from '@angular/common';
import { TaskStore } from '../../core/stores/task.store';
import { BacklogStore } from '../../core/stores/backlog.store';
import {
  Task,
  PipelineState,
  Priority,
  PIPELINE_STATES,
  PRIORITIES,
} from '../../shared/models';
import { StateBadgeComponent } from '../../shared/components/state-badge.component';
import { BacklogStateBadgeComponent } from '../../shared/components/backlog-state-badge/backlog-state-badge.component';
import { PriorityBadgeComponent } from '../../shared/components/priority-badge.component';
import { AgentIndicatorComponent } from '../../shared/components/agent-indicator.component';
import { PausedBadgeComponent } from '../../shared/components/paused-badge.component';
import { LoadingSpinnerComponent } from '../../shared/components/loading-spinner.component';
import { ErrorAlertComponent } from '../../shared/components/error-alert.component';

type SortField = 'executionOrder' | 'priority' | 'state' | 'title';
type SortDirection = 'asc' | 'desc';

@Component({
  selector: 'app-task-queue',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    RouterLink,
    FormsModule,
    SlicePipe,
    StateBadgeComponent,
    BacklogStateBadgeComponent,
    PriorityBadgeComponent,
    AgentIndicatorComponent,
    PausedBadgeComponent,
    LoadingSpinnerComponent,
    ErrorAlertComponent,
  ],
  template: `
    <div class="flex h-screen flex-col bg-gray-100 dark:bg-gray-950">
      <!-- Header -->
      <header class="flex items-center gap-4 border-b border-gray-200 bg-white px-6 py-4 dark:border-gray-800 dark:bg-gray-900">
        <!-- Breadcrumb -->
        <nav class="flex items-center gap-2 text-sm" aria-label="Breadcrumb">
          <a
            routerLink="/backlog"
            class="text-gray-500 hover:text-gray-700 dark:text-gray-400 dark:hover:text-gray-200"
          >
            Backlog
          </a>
          @if (backlogItem()) {
            <svg class="h-4 w-4 text-gray-400" fill="currentColor" viewBox="0 0 20 20" aria-hidden="true">
              <path fill-rule="evenodd" d="M7.21 14.77a.75.75 0 01.02-1.06L11.168 10 7.23 6.29a.75.75 0 111.04-1.08l4.5 4.25a.75.75 0 010 1.08l-4.5 4.25a.75.75 0 01-1.06-.02z" clip-rule="evenodd" />
            </svg>
            <a
              [routerLink]="['/backlog', backlogItem()!.id]"
              class="text-gray-500 hover:text-gray-700 dark:text-gray-400 dark:hover:text-gray-200"
            >
              {{ backlogItem()!.title | slice:0:30 }}{{ backlogItem()!.title.length > 30 ? '...' : '' }}
            </a>
          }
          <svg class="h-4 w-4 text-gray-400" fill="currentColor" viewBox="0 0 20 20" aria-hidden="true">
            <path fill-rule="evenodd" d="M7.21 14.77a.75.75 0 01.02-1.06L11.168 10 7.23 6.29a.75.75 0 111.04-1.08l4.5 4.25a.75.75 0 010 1.08l-4.5 4.25a.75.75 0 01-1.06-.02z" clip-rule="evenodd" />
          </svg>
          <span class="font-medium text-gray-900 dark:text-gray-100">Tasks</span>
        </nav>

        <div class="flex-1"></div>

        @if (backlogItem()) {
          <div class="flex items-center gap-2">
            <app-backlog-state-badge [state]="backlogItem()!.state" />
            @if (backlogItem()!.progress) {
              <span class="text-sm text-gray-500 dark:text-gray-400">
                {{ backlogItem()!.progress!.completed }}/{{ backlogItem()!.progress!.total }} tasks
              </span>
            }
          </div>
        }
      </header>

      <!-- Filters & Sort Bar -->
      <div
        class="flex items-center gap-4 border-b border-gray-200 bg-white px-6 py-3 dark:border-gray-800 dark:bg-gray-900"
      >
        <!-- State Filter -->
        <div class="flex items-center gap-2">
          <label
            for="state-filter"
            class="text-sm font-medium text-gray-700 dark:text-gray-300"
          >
            State:
          </label>
          <select
            id="state-filter"
            class="rounded-md border border-gray-300 bg-white px-3 py-1.5 text-sm text-gray-900 focus:border-blue-500 focus:outline-none focus:ring-1 focus:ring-blue-500 dark:border-gray-600 dark:bg-gray-800 dark:text-gray-100"
            [ngModel]="stateFilter()"
            (ngModelChange)="stateFilter.set($event)"
          >
            <option value="">All States</option>
            @for (state of pipelineStates; track state) {
              <option [value]="state">{{ formatState(state) }}</option>
            }
          </select>
        </div>

        <!-- Priority Filter -->
        <div class="flex items-center gap-2">
          <label
            for="priority-filter"
            class="text-sm font-medium text-gray-700 dark:text-gray-300"
          >
            Priority:
          </label>
          <select
            id="priority-filter"
            class="rounded-md border border-gray-300 bg-white px-3 py-1.5 text-sm text-gray-900 focus:border-blue-500 focus:outline-none focus:ring-1 focus:ring-blue-500 dark:border-gray-600 dark:bg-gray-800 dark:text-gray-100"
            [ngModel]="priorityFilter()"
            (ngModelChange)="priorityFilter.set($event)"
          >
            <option value="">All Priorities</option>
            @for (priority of priorities; track priority) {
              <option [value]="priority">{{ priority }}</option>
            }
          </select>
        </div>

        <div class="flex-1"></div>

        <!-- Sort -->
        <div class="flex items-center gap-2">
          <label
            for="sort-field"
            class="text-sm font-medium text-gray-700 dark:text-gray-300"
          >
            Sort:
          </label>
          <select
            id="sort-field"
            class="rounded-md border border-gray-300 bg-white px-3 py-1.5 text-sm text-gray-900 focus:border-blue-500 focus:outline-none focus:ring-1 focus:ring-blue-500 dark:border-gray-600 dark:bg-gray-800 dark:text-gray-100"
            [ngModel]="sortField()"
            (ngModelChange)="sortField.set($event)"
          >
            <option value="executionOrder">Execution Order</option>
            <option value="priority">Priority</option>
            <option value="state">State</option>
            <option value="title">Title</option>
          </select>
          <button
            type="button"
            class="rounded-md p-1.5 text-gray-500 hover:bg-gray-100 dark:text-gray-400 dark:hover:bg-gray-800"
            [attr.aria-label]="
              sortDirection() === 'asc' ? 'Sort descending' : 'Sort ascending'
            "
            (click)="toggleSortDirection()"
          >
            <svg
              class="h-4 w-4 transition-transform"
              [class.rotate-180]="sortDirection() === 'desc'"
              xmlns="http://www.w3.org/2000/svg"
              viewBox="0 0 20 20"
              fill="currentColor"
              aria-hidden="true"
            >
              <path
                fill-rule="evenodd"
                d="M10 5a.75.75 0 01.75.75v6.638l1.96-2.158a.75.75 0 111.08 1.04l-3.25 3.5a.75.75 0 01-1.08 0l-3.25-3.5a.75.75 0 111.08-1.04l1.96 2.158V5.75A.75.75 0 0110 5z"
                clip-rule="evenodd"
              />
            </svg>
          </button>
        </div>

        <!-- Task Count -->
        <div class="text-sm text-gray-500 dark:text-gray-400">
          {{ filteredTasks().length }} tasks
        </div>
      </div>

      <!-- Main Content -->
      <main class="flex-1 overflow-auto">
        @if (taskStore.isLoading()) {
          <div class="flex h-full items-center justify-center">
            <app-loading-spinner size="lg" label="Loading tasks..." />
          </div>
        } @else if (taskStore.errorMessage()) {
          <div class="mx-auto max-w-md p-6">
            <app-error-alert
              title="Failed to load tasks"
              [message]="taskStore.errorMessage()!"
              [dismissible]="true"
              (dismiss)="loadTasks()"
            />
          </div>
        } @else if (filteredTasks().length === 0) {
          <div class="flex h-full flex-col items-center justify-center text-gray-500 dark:text-gray-400">
            <svg
              class="mb-4 h-12 w-12"
              xmlns="http://www.w3.org/2000/svg"
              fill="none"
              viewBox="0 0 24 24"
              stroke-width="1.5"
              stroke="currentColor"
              aria-hidden="true"
            >
              <path
                stroke-linecap="round"
                stroke-linejoin="round"
                d="M9 12h3.75M9 15h3.75M9 18h3.75m3 .75H18a2.25 2.25 0 002.25-2.25V6.108c0-1.135-.845-2.098-1.976-2.192a48.424 48.424 0 00-1.123-.08m-5.801 0c-.065.21-.1.433-.1.664 0 .414.336.75.75.75h4.5a.75.75 0 00.75-.75 2.25 2.25 0 00-.1-.664m-5.8 0A2.251 2.251 0 0113.5 2.25H15c1.012 0 1.867.668 2.15 1.586m-5.8 0c-.376.023-.75.05-1.124.08C9.095 4.01 8.25 4.973 8.25 6.108V8.25m0 0H4.875c-.621 0-1.125.504-1.125 1.125v11.25c0 .621.504 1.125 1.125 1.125h9.75c.621 0 1.125-.504 1.125-1.125V9.375c0-.621-.504-1.125-1.125-1.125H8.25zM6.75 12h.008v.008H6.75V12zm0 3h.008v.008H6.75V15zm0 3h.008v.008H6.75V18z"
              />
            </svg>
            <p class="text-lg font-medium">No tasks found</p>
            <p class="mt-1">
              @if (stateFilter() || priorityFilter()) {
                Try adjusting your filters
              } @else {
                Tasks will appear when this backlog item is split
              }
            </p>
          </div>
        } @else {
          <!-- Task List -->
          <div class="bg-white dark:bg-gray-900">
            <!-- Header Row -->
            <div
              class="sticky top-0 z-10 flex items-center gap-4 border-b border-gray-200 bg-gray-50 px-4 py-2 text-xs font-medium uppercase tracking-wider text-gray-500 dark:border-gray-700 dark:bg-gray-800 dark:text-gray-400"
            >
              <div class="w-8 flex-shrink-0 text-center">#</div>
              <div class="min-w-0 flex-1">Task</div>
              <div class="w-28 flex-shrink-0">State</div>
              <div class="w-20 flex-shrink-0">Priority</div>
              <div class="w-20 flex-shrink-0">Status</div>
            </div>

            <!-- Task Rows -->
            @for (task of filteredTasks(); track task.id) {
              <button
                type="button"
                class="flex w-full items-center gap-4 border-b border-gray-200 px-4 py-3 text-left transition-colors hover:bg-gray-50 dark:border-gray-700 dark:hover:bg-gray-800/50"
                [class.bg-blue-50]="task.assignedAgentId"
                [class.dark:bg-blue-950/30]="task.assignedAgentId"
                (click)="onTaskClick(task)"
              >
                <!-- Execution Order -->
                <div class="flex w-8 flex-shrink-0 items-center justify-center">
                  <span class="flex h-6 w-6 items-center justify-center rounded-full bg-gray-200 text-xs font-medium text-gray-600 dark:bg-gray-700 dark:text-gray-400">
                    {{ task.executionOrder }}
                  </span>
                </div>

                <!-- Title & Description -->
                <div class="min-w-0 flex-1">
                  <div class="flex items-center gap-2">
                    <span class="font-medium text-gray-900 dark:text-gray-100">
                      {{ task.title }}
                    </span>
                    @if (task.hasError) {
                      <span
                        class="inline-flex items-center rounded-full bg-red-100 px-2 py-0.5 text-xs font-medium text-red-800 dark:bg-red-900 dark:text-red-200"
                        aria-label="Task has error"
                      >
                        Error
                      </span>
                    }
                  </div>
                  @if (task.description) {
                    <p class="mt-0.5 truncate text-sm text-gray-500 dark:text-gray-400">
                      {{ task.description }}
                    </p>
                  }
                </div>

                <!-- State Badge -->
                <div class="w-28 flex-shrink-0">
                  <app-state-badge [state]="task.state" />
                </div>

                <!-- Priority Badge -->
                <div class="w-20 flex-shrink-0">
                  <app-priority-badge [priority]="task.priority" />
                </div>

                <!-- Status Indicators -->
                <div class="flex w-20 flex-shrink-0 items-center gap-1">
                  @if (task.assignedAgentId) {
                    <app-agent-indicator />
                  }
                  @if (task.isPaused) {
                    <app-paused-badge />
                  }
                </div>
              </button>
            }
          </div>
        }
      </main>
    </div>
  `,
  styles: `
    :host {
      display: block;
      height: 100vh;
    }
  `,
})
export class TaskQueueComponent implements OnInit {
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);
  protected readonly taskStore = inject(TaskStore);
  protected readonly backlogStore = inject(BacklogStore);

  readonly pipelineStates = PIPELINE_STATES;
  readonly priorities = PRIORITIES;

  private backlogItemId = '';

  // Filter and sort state
  readonly stateFilter = signal<PipelineState | ''>('');
  readonly priorityFilter = signal<Priority | ''>('');
  readonly sortField = signal<SortField>('executionOrder');
  readonly sortDirection = signal<SortDirection>('asc');

  readonly backlogItem = computed(() => {
    return this.backlogStore.getItemById(this.backlogItemId);
  });

  // Computed: filtered and sorted tasks
  readonly filteredTasks = computed(() => {
    let tasks = this.taskStore.backlogItemTasks();

    // Apply state filter
    const stateFilterValue = this.stateFilter();
    if (stateFilterValue) {
      tasks = tasks.filter(t => t.state === stateFilterValue);
    }

    // Apply priority filter
    const priorityFilterValue = this.priorityFilter();
    if (priorityFilterValue) {
      tasks = tasks.filter(t => t.priority === priorityFilterValue);
    }

    // Sort tasks
    const field = this.sortField();
    const direction = this.sortDirection();
    const priorityOrder: Record<Priority, number> = {
      critical: 4,
      high: 3,
      medium: 2,
      low: 1,
    };
    const stateOrder: Record<PipelineState, number> = {
      Research: 0,
      Planning: 1,
      Implementing: 2,
      Simplifying: 3,
      Verifying: 4,
      Reviewing: 5,
      PrReady: 6,
      Done: 7,
    };

    tasks = [...tasks].sort((a, b) => {
      let comparison = 0;
      switch (field) {
        case 'executionOrder':
          comparison = a.executionOrder - b.executionOrder;
          break;
        case 'priority':
          comparison = priorityOrder[b.priority] - priorityOrder[a.priority];
          break;
        case 'state':
          comparison = stateOrder[a.state] - stateOrder[b.state];
          break;
        case 'title':
          comparison = a.title.localeCompare(b.title);
          break;
      }
      return direction === 'asc' ? comparison : -comparison;
    });

    return tasks;
  });

  ngOnInit(): void {
    this.backlogItemId = this.route.snapshot.paramMap.get('backlogItemId') || '';
    this.loadData();
  }

  private async loadData(): Promise<void> {
    // Set context for task store
    this.taskStore.setBacklogItemContext(this.backlogItemId);

    // Load backlog items and tasks
    await Promise.all([
      this.backlogStore.loadItems(),
      this.taskStore.loadTasks(this.backlogItemId),
    ]);
  }

  loadTasks(): void {
    this.taskStore.loadTasks(this.backlogItemId);
  }

  onTaskClick(task: Task): void {
    this.router.navigate(['/backlog', this.backlogItemId, 'tasks', task.id]);
  }

  toggleSortDirection(): void {
    this.sortDirection.update(d => (d === 'asc' ? 'desc' : 'asc'));
  }

  formatState(state: PipelineState): string {
    return state === 'PrReady' ? 'PR Ready' : state;
  }
}
