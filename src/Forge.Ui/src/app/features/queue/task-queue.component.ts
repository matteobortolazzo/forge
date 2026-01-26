import {
  Component,
  ChangeDetectionStrategy,
  inject,
  signal,
  computed,
  OnInit,
  OnDestroy,
} from '@angular/core';
import { FormsModule } from '@angular/forms';
import { TaskStore } from '../../core/stores/task.store';
import { LogStore } from '../../core/stores/log.store';
import { NotificationStore } from '../../core/stores/notification.store';
import { SchedulerStore } from '../../core/stores/scheduler.store';
import { RepositoryStore } from '../../core/stores/repository.store';
import { SseService } from '../../core/services/sse.service';
import {
  Task,
  PipelineState,
  Priority,
  PIPELINE_STATES,
  PRIORITIES,
  CreateTaskDto,
  ServerEvent,
  TaskLog,
  Notification,
  AgentStatus,
  TaskSplitPayload,
  ChildAddedPayload,
} from '../../shared/models';
import { TaskRowComponent } from './task-row.component';
import { SplitTaskDialogComponent } from './split-task-dialog.component';
import { CreateTaskDialogComponent } from '../board/create-task-dialog.component';
import { NotificationPanelComponent } from '../notifications/notification-panel.component';
import { LoadingSpinnerComponent } from '../../shared/components/loading-spinner.component';
import { ErrorAlertComponent } from '../../shared/components/error-alert.component';
import { SchedulerStatusComponent } from '../../shared/components/scheduler-status.component';
import { RepositoryInfoComponent } from '../../shared/components/repository-info.component';
import { Subscription } from 'rxjs';

type SortField = 'priority' | 'state' | 'createdAt' | 'title';
type SortDirection = 'asc' | 'desc';

@Component({
  selector: 'app-task-queue',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    FormsModule,
    TaskRowComponent,
    SplitTaskDialogComponent,
    CreateTaskDialogComponent,
    NotificationPanelComponent,
    LoadingSpinnerComponent,
    ErrorAlertComponent,
    SchedulerStatusComponent,
    RepositoryInfoComponent,
  ],
  template: `
    <div class="flex h-screen flex-col bg-gray-100 dark:bg-gray-950">
      <!-- Header -->
      <header
        class="flex items-center justify-between border-b border-gray-200 bg-white px-6 py-4 dark:border-gray-800 dark:bg-gray-900"
      >
        <div class="flex items-center gap-3">
          <h1 class="text-xl font-bold text-gray-900 dark:text-gray-100">
            Forge
          </h1>
          <span class="text-sm text-gray-500 dark:text-gray-400">
            AI Agent Dashboard
          </span>
          <div
            class="h-5 w-px bg-gray-300 dark:bg-gray-700"
            aria-hidden="true"
          ></div>
          <app-repository-info />
        </div>

        <div class="flex items-center gap-4">
          <app-scheduler-status />

          <div
            class="h-5 w-px bg-gray-300 dark:bg-gray-700"
            aria-hidden="true"
          ></div>

          <button
            type="button"
            class="inline-flex items-center gap-2 rounded-md bg-blue-600 px-4 py-2 text-sm font-medium text-white hover:bg-blue-700 focus:outline-none focus:ring-2 focus:ring-blue-500 focus:ring-offset-2"
            (click)="openCreateDialog()"
          >
            <svg
              class="h-4 w-4"
              xmlns="http://www.w3.org/2000/svg"
              viewBox="0 0 20 20"
              fill="currentColor"
              aria-hidden="true"
            >
              <path
                d="M10.75 4.75a.75.75 0 00-1.5 0v4.5h-4.5a.75.75 0 000 1.5h4.5v4.5a.75.75 0 001.5 0v-4.5h4.5a.75.75 0 000-1.5h-4.5v-4.5z"
              />
            </svg>
            New Task
          </button>

          <app-notification-panel />
        </div>
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
            <option value="priority">Priority</option>
            <option value="state">State</option>
            <option value="createdAt">Created</option>
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
                Create a new task to get started
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
              <div class="w-6 flex-shrink-0"></div>
              <div class="min-w-0 flex-1">Task</div>
              <div class="w-28 flex-shrink-0">State</div>
              <div class="w-20 flex-shrink-0">Priority</div>
              <div class="w-24 flex-shrink-0">Progress</div>
              <div class="w-32 flex-shrink-0 text-right">Actions</div>
            </div>

            <!-- Task Rows -->
            @for (task of filteredTasks(); track task.id) {
              <app-task-row
                [task]="task"
                [children]="taskStore.getChildrenOf(task.id)"
                (split)="onSplitTask($event)"
              />
            }
          </div>
        }
      </main>
    </div>

    <!-- Create Task Dialog -->
    <app-create-task-dialog
      [isOpen]="isDialogOpen()"
      (create)="onCreateTask($event)"
      (cancel)="closeCreateDialog()"
    />

    <!-- Split Task Dialog -->
    @if (splitDialogTaskId()) {
      <app-split-task-dialog
        [taskId]="splitDialogTaskId()!"
        [taskTitle]="getSplitTaskTitle()"
        (split)="onConfirmSplit($event)"
        (cancel)="closeSplitDialog()"
      />
    }
  `,
  styles: `
    :host {
      display: block;
      height: 100vh;
    }
  `,
})
export class TaskQueueComponent implements OnInit, OnDestroy {
  protected readonly taskStore = inject(TaskStore);
  private readonly logStore = inject(LogStore);
  private readonly notificationStore = inject(NotificationStore);
  protected readonly schedulerStore = inject(SchedulerStore);
  private readonly repositoryStore = inject(RepositoryStore);
  private readonly sseService = inject(SseService);

  readonly pipelineStates = PIPELINE_STATES;
  readonly priorities = PRIORITIES;

  readonly isDialogOpen = signal(false);
  readonly splitDialogTaskId = signal<string | null>(null);

  // Filter and sort state
  readonly stateFilter = signal<PipelineState | ''>('');
  readonly priorityFilter = signal<Priority | ''>('');
  readonly sortField = signal<SortField>('priority');
  readonly sortDirection = signal<SortDirection>('desc');

  private sseSubscription?: Subscription;

  // Computed: filtered and sorted root tasks
  readonly filteredTasks = computed(() => {
    let tasks = this.taskStore.rootTasks();

    // Apply state filter
    const stateFilterValue = this.stateFilter();
    if (stateFilterValue) {
      tasks = tasks.filter(t => {
        const displayState = t.derivedState ?? t.state;
        return displayState === stateFilterValue;
      });
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
      Backlog: 0,
      Planning: 1,
      Implementing: 2,
      Reviewing: 3,
      Testing: 4,
      PrReady: 5,
      Done: 6,
    };

    tasks = [...tasks].sort((a, b) => {
      let comparison = 0;
      switch (field) {
        case 'priority':
          comparison = priorityOrder[b.priority] - priorityOrder[a.priority];
          break;
        case 'state': {
          const stateA = a.derivedState ?? a.state;
          const stateB = b.derivedState ?? b.state;
          comparison = stateOrder[stateA] - stateOrder[stateB];
          break;
        }
        case 'createdAt':
          comparison =
            new Date(b.createdAt).getTime() - new Date(a.createdAt).getTime();
          break;
        case 'title':
          comparison = a.title.localeCompare(b.title);
          break;
      }
      return direction === 'desc' ? comparison : -comparison;
    });

    return tasks;
  });

  ngOnInit(): void {
    this.loadTasks();
    this.loadNotifications();
    this.loadSchedulerStatus();
    this.loadRepositoryInfo();
    this.connectToSse();
  }

  ngOnDestroy(): void {
    this.sseSubscription?.unsubscribe();
    this.sseService.disconnect();
  }

  loadTasks(): void {
    this.taskStore.loadTasks();
  }

  loadSchedulerStatus(): void {
    this.schedulerStore.loadStatus();
  }

  loadRepositoryInfo(): void {
    this.repositoryStore.loadInfo();
  }

  loadNotifications(): void {
    this.notificationStore.loadNotifications();
  }

  openCreateDialog(): void {
    this.isDialogOpen.set(true);
  }

  closeCreateDialog(): void {
    this.isDialogOpen.set(false);
  }

  async onCreateTask(dto: CreateTaskDto): Promise<void> {
    const task = await this.taskStore.createTask(dto);
    if (task) {
      this.closeCreateDialog();
    }
  }

  onSplitTask(taskId: string): void {
    this.splitDialogTaskId.set(taskId);
  }

  closeSplitDialog(): void {
    this.splitDialogTaskId.set(null);
  }

  getSplitTaskTitle(): string {
    const taskId = this.splitDialogTaskId();
    if (!taskId) return '';
    const task = this.taskStore.getTaskById(taskId);
    return task?.title ?? '';
  }

  async onConfirmSplit(subtasks: { title: string; description: string; priority: Priority }[]): Promise<void> {
    const taskId = this.splitDialogTaskId();
    if (!taskId) return;

    const success = await this.taskStore.splitTask(taskId, subtasks);
    if (success) {
      this.closeSplitDialog();
    }
  }

  toggleSortDirection(): void {
    this.sortDirection.update(d => (d === 'asc' ? 'desc' : 'asc'));
  }

  formatState(state: PipelineState): string {
    return state === 'PrReady' ? 'PR Ready' : state;
  }

  private connectToSse(): void {
    this.sseSubscription = this.sseService.connect().subscribe({
      next: (event: ServerEvent) => this.handleSseEvent(event),
      error: err => console.error('SSE error:', err),
    });
  }

  private handleSseEvent(event: ServerEvent): void {
    switch (event.type) {
      case 'task:created':
      case 'task:updated':
      case 'task:paused':
      case 'task:resumed':
        this.taskStore.updateTaskFromEvent(event.payload as Task);
        break;
      case 'task:deleted':
        this.taskStore.removeTaskFromEvent(
          (event.payload as { id: string }).id
        );
        break;
      case 'task:log':
        this.logStore.addLog(event.payload as TaskLog);
        break;
      case 'task:split': {
        const splitPayload = event.payload as TaskSplitPayload;
        this.taskStore.handleTaskSplitEvent(
          splitPayload.parent,
          splitPayload.children
        );
        break;
      }
      case 'task:childAdded': {
        const childPayload = event.payload as ChildAddedPayload;
        this.taskStore.handleChildAddedEvent(
          childPayload.parentId,
          childPayload.child
        );
        break;
      }
      case 'agent:statusChanged': {
        const agentStatus = event.payload as AgentStatus;
        this.schedulerStore.updateAgentStatus(
          agentStatus.isRunning,
          agentStatus.currentTaskId
        );
        break;
      }
      case 'scheduler:taskScheduled':
        this.taskStore.updateTaskFromEvent(event.payload as Task);
        this.schedulerStore.updateFromScheduledEvent(
          (event.payload as Task).id
        );
        break;
      case 'notification:new':
        this.notificationStore.addNotificationFromEvent(
          event.payload as Notification
        );
        break;
    }
  }
}
