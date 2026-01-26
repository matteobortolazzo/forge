import {
  Component,
  ChangeDetectionStrategy,
  inject,
  signal,
  computed,
  OnInit,
  OnDestroy,
} from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { TaskStore } from '../../core/stores/task.store';
import { LogStore } from '../../core/stores/log.store';
import { NotificationStore } from '../../core/stores/notification.store';
import { SseService } from '../../core/services/sse.service';
import { TaskService } from '../../core/services/task.service';
import { Task, TaskLog, ServerEvent, PIPELINE_STATES } from '../../shared/models';
import { StateBadgeComponent } from '../../shared/components/state-badge.component';
import { PriorityBadgeComponent } from '../../shared/components/priority-badge.component';
import { AgentIndicatorComponent } from '../../shared/components/agent-indicator.component';
import { PausedBadgeComponent } from '../../shared/components/paused-badge.component';
import { ErrorAlertComponent } from '../../shared/components/error-alert.component';
import { LoadingSpinnerComponent } from '../../shared/components/loading-spinner.component';
import { AgentOutputComponent } from './agent-output.component';
import { Subscription } from 'rxjs';

@Component({
  selector: 'app-task-detail',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    RouterLink,
    StateBadgeComponent,
    PriorityBadgeComponent,
    AgentIndicatorComponent,
    PausedBadgeComponent,
    ErrorAlertComponent,
    LoadingSpinnerComponent,
    AgentOutputComponent,
  ],
  template: `
    <div class="flex h-screen flex-col bg-gray-100 dark:bg-gray-950">
      <!-- Header -->
      <header class="flex items-center gap-4 border-b border-gray-200 bg-white px-6 py-4 dark:border-gray-800 dark:bg-gray-900">
        <a
          routerLink="/"
          class="flex items-center gap-2 text-gray-500 hover:text-gray-700 dark:text-gray-400 dark:hover:text-gray-200"
          aria-label="Back to board"
        >
          <svg class="h-5 w-5" viewBox="0 0 20 20" fill="currentColor" aria-hidden="true">
            <path fill-rule="evenodd" d="M17 10a.75.75 0 01-.75.75H5.612l4.158 3.96a.75.75 0 11-1.04 1.08l-5.5-5.25a.75.75 0 010-1.08l5.5-5.25a.75.75 0 111.04 1.08L5.612 9.25H16.25A.75.75 0 0117 10z" clip-rule="evenodd" />
          </svg>
          <span class="text-sm font-medium">Back to Board</span>
        </a>
      </header>

      <!-- Main Content -->
      <main class="flex flex-1 overflow-hidden">
        @if (isLoading()) {
          <div class="flex flex-1 items-center justify-center">
            <app-loading-spinner size="lg" label="Loading task..." />
          </div>
        } @else if (!task()) {
          <div class="flex flex-1 items-center justify-center">
            <div class="text-center">
              <h2 class="text-lg font-semibold text-gray-900 dark:text-gray-100">Task not found</h2>
              <p class="mt-1 text-sm text-gray-500 dark:text-gray-400">
                The task you're looking for doesn't exist or has been deleted.
              </p>
              <a
                routerLink="/"
                class="mt-4 inline-block text-sm font-medium text-blue-600 hover:text-blue-700 dark:text-blue-400"
              >
                Return to board
              </a>
            </div>
          </div>
        } @else {
          <!-- Left Panel: Task Details -->
          <div class="w-1/3 min-w-80 overflow-y-auto border-r border-gray-200 bg-white p-6 dark:border-gray-800 dark:bg-gray-900">
            <!-- Title & Status -->
            <div class="flex items-start justify-between gap-4">
              <h1 class="text-xl font-semibold text-gray-900 dark:text-gray-100">
                {{ task()!.title }}
              </h1>
              <app-agent-indicator [isRunning]="!!task()!.assignedAgentId" [showLabel]="true" />
            </div>

            <!-- Badges -->
            <div class="mt-4 flex flex-wrap items-center gap-2">
              <app-state-badge [state]="task()!.state" />
              <app-priority-badge [priority]="task()!.priority" />
              @if (task()!.isPaused) {
                <app-paused-badge [reason]="task()!.pauseReason" />
              }
            </div>

            <!-- Error Alert -->
            @if (task()!.hasError && task()!.errorMessage) {
              <div class="mt-4">
                <app-error-alert
                  title="Error"
                  [message]="task()!.errorMessage!"
                />
              </div>
            }

            <!-- Description -->
            <div class="mt-6">
              <h2 class="text-sm font-semibold text-gray-700 dark:text-gray-300">Description</h2>
              <p class="mt-2 text-sm text-gray-600 dark:text-gray-400 whitespace-pre-wrap">
                {{ task()!.description || 'No description provided.' }}
              </p>
            </div>

            <!-- Metadata -->
            <div class="mt-6 space-y-3">
              <div class="flex items-center justify-between text-sm">
                <span class="text-gray-500 dark:text-gray-400">Created</span>
                <span class="text-gray-700 dark:text-gray-300">{{ formatDate(task()!.createdAt) }}</span>
              </div>
              <div class="flex items-center justify-between text-sm">
                <span class="text-gray-500 dark:text-gray-400">Updated</span>
                <span class="text-gray-700 dark:text-gray-300">{{ formatDate(task()!.updatedAt) }}</span>
              </div>
              @if (task()!.assignedAgentId) {
                <div class="flex items-center justify-between text-sm">
                  <span class="text-gray-500 dark:text-gray-400">Agent ID</span>
                  <span class="font-mono text-xs text-gray-700 dark:text-gray-300">{{ task()!.assignedAgentId }}</span>
                </div>
              }
              @if (task()!.isPaused) {
                <div class="flex items-center justify-between text-sm">
                  <span class="text-gray-500 dark:text-gray-400">Paused At</span>
                  <span class="text-gray-700 dark:text-gray-300">{{ formatDate(task()!.pausedAt!) }}</span>
                </div>
                @if (task()!.pauseReason) {
                  <div class="flex items-center justify-between text-sm">
                    <span class="text-gray-500 dark:text-gray-400">Pause Reason</span>
                    <span class="text-gray-700 dark:text-gray-300">{{ task()!.pauseReason }}</span>
                  </div>
                }
              }
              <div class="flex items-center justify-between text-sm">
                <span class="text-gray-500 dark:text-gray-400">Retry Count</span>
                <span class="text-gray-700 dark:text-gray-300">{{ task()!.retryCount }} / {{ task()!.maxRetries }}</span>
              </div>
            </div>

            <!-- Action Buttons -->
            <div class="mt-8 space-y-3">
              <!-- State Transition Buttons -->
              <div class="flex gap-2">
                @if (canMovePrevious()) {
                  <button
                    type="button"
                    class="flex-1 rounded-md border border-gray-300 bg-white px-4 py-2 text-sm font-medium text-gray-700 hover:bg-gray-50 focus:outline-none focus:ring-2 focus:ring-blue-500 focus:ring-offset-2 dark:border-gray-600 dark:bg-gray-800 dark:text-gray-300 dark:hover:bg-gray-700"
                    (click)="moveToPrevious()"
                    [disabled]="isTransitioning()"
                  >
                    Move to {{ previousState() }}
                  </button>
                }
                @if (canMoveNext()) {
                  <button
                    type="button"
                    class="flex-1 rounded-md bg-blue-600 px-4 py-2 text-sm font-medium text-white hover:bg-blue-700 focus:outline-none focus:ring-2 focus:ring-blue-500 focus:ring-offset-2 disabled:opacity-50"
                    (click)="moveToNext()"
                    [disabled]="isTransitioning()"
                  >
                    Move to {{ nextState() }}
                  </button>
                }
              </div>

              <!-- Abort Agent Button -->
              @if (task()!.assignedAgentId) {
                <button
                  type="button"
                  class="w-full rounded-md border border-amber-300 bg-amber-50 px-4 py-2 text-sm font-medium text-amber-700 hover:bg-amber-100 focus:outline-none focus:ring-2 focus:ring-amber-500 focus:ring-offset-2 dark:border-amber-700 dark:bg-amber-900/30 dark:text-amber-400 dark:hover:bg-amber-900/50"
                  (click)="abortAgent()"
                  [disabled]="isAborting()"
                >
                  @if (isAborting()) {
                    Aborting...
                  } @else {
                    Abort Agent
                  }
                </button>
              }

              <!-- Pause/Resume Button -->
              @if (!task()!.assignedAgentId) {
                @if (task()!.isPaused) {
                  <button
                    type="button"
                    class="w-full rounded-md border border-green-300 bg-green-50 px-4 py-2 text-sm font-medium text-green-700 hover:bg-green-100 focus:outline-none focus:ring-2 focus:ring-green-500 focus:ring-offset-2 dark:border-green-700 dark:bg-green-900/30 dark:text-green-400 dark:hover:bg-green-900/50"
                    (click)="resumeTask()"
                    [disabled]="isPausing()"
                  >
                    @if (isPausing()) {
                      Resuming...
                    } @else {
                      Resume Task
                    }
                  </button>
                } @else {
                  <button
                    type="button"
                    class="w-full rounded-md border border-amber-300 bg-amber-50 px-4 py-2 text-sm font-medium text-amber-700 hover:bg-amber-100 focus:outline-none focus:ring-2 focus:ring-amber-500 focus:ring-offset-2 dark:border-amber-700 dark:bg-amber-900/30 dark:text-amber-400 dark:hover:bg-amber-900/50"
                    (click)="pauseTask()"
                    [disabled]="isPausing()"
                  >
                    @if (isPausing()) {
                      Pausing...
                    } @else {
                      Pause Task
                    }
                  </button>
                }
              }

              <!-- Delete Button -->
              <button
                type="button"
                class="w-full rounded-md border border-red-300 bg-white px-4 py-2 text-sm font-medium text-red-600 hover:bg-red-50 focus:outline-none focus:ring-2 focus:ring-red-500 focus:ring-offset-2 disabled:cursor-not-allowed disabled:opacity-50 dark:border-red-700 dark:bg-transparent dark:text-red-400 dark:hover:bg-red-900/30"
                (click)="deleteTask()"
                [disabled]="!!task()!.assignedAgentId || isDeleting()"
                [title]="task()!.assignedAgentId ? 'Cannot delete while agent is running' : 'Delete task'"
              >
                @if (isDeleting()) {
                  Deleting...
                } @else {
                  Delete Task
                }
              </button>
            </div>
          </div>

          <!-- Right Panel: Agent Output -->
          <div class="flex-1 p-6">
            <app-agent-output [logs]="logs()" />
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
export class TaskDetailComponent implements OnInit, OnDestroy {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly taskStore = inject(TaskStore);
  private readonly logStore = inject(LogStore);
  private readonly notificationStore = inject(NotificationStore);
  private readonly taskService = inject(TaskService);
  private readonly sseService = inject(SseService);

  readonly isLoading = signal(true);
  readonly isTransitioning = signal(false);
  readonly isAborting = signal(false);
  readonly isDeleting = signal(false);
  readonly isPausing = signal(false);

  private taskId = '';
  private sseSubscription?: Subscription;

  readonly task = computed(() => this.taskStore.getTaskById(this.taskId));
  readonly logs = computed(() => this.logStore.getLogsForTask(this.taskId));

  readonly nextState = computed(() => {
    const t = this.task();
    return t ? this.taskService.getNextState(t.state) : null;
  });

  readonly previousState = computed(() => {
    const t = this.task();
    return t ? this.taskService.getPreviousState(t.state) : null;
  });

  readonly canMoveNext = computed(() => !!this.nextState());
  readonly canMovePrevious = computed(() => !!this.previousState());

  ngOnInit(): void {
    this.taskId = this.route.snapshot.paramMap.get('id') || '';
    this.loadData();
    this.connectToSse();
  }

  ngOnDestroy(): void {
    this.sseSubscription?.unsubscribe();
    this.sseService.disconnect();
  }

  private async loadData(): Promise<void> {
    this.isLoading.set(true);

    // Load tasks if not already loaded
    if (this.taskStore.allTasks().length === 0) {
      await this.taskStore.loadTasks();
    }

    // Load logs for this task
    await this.logStore.loadLogsForTask(this.taskId);

    this.isLoading.set(false);
  }

  private connectToSse(): void {
    this.sseSubscription = this.sseService.connect().subscribe({
      next: (event: ServerEvent) => this.handleSseEvent(event),
      error: (err) => console.error('SSE error:', err),
    });
  }

  private handleSseEvent(event: ServerEvent): void {
    switch (event.type) {
      case 'task:updated':
      case 'task:paused':
      case 'task:resumed':
      case 'scheduler:taskScheduled':
        this.taskStore.updateTaskFromEvent(event.payload as Task);
        break;
      case 'task:log': {
        const log = event.payload as TaskLog;
        if (log.taskId === this.taskId) {
          this.logStore.addLog(log);
        }
        break;
      }
    }
  }

  async moveToNext(): Promise<void> {
    const next = this.nextState();
    if (!next) return;

    this.isTransitioning.set(true);
    await this.taskStore.transitionTask(this.taskId, next);
    this.isTransitioning.set(false);
  }

  async moveToPrevious(): Promise<void> {
    const prev = this.previousState();
    if (!prev) return;

    this.isTransitioning.set(true);
    await this.taskStore.transitionTask(this.taskId, prev);
    this.isTransitioning.set(false);
  }

  async abortAgent(): Promise<void> {
    this.isAborting.set(true);
    await this.taskStore.abortAgent(this.taskId);
    this.isAborting.set(false);
  }

  async deleteTask(): Promise<void> {
    const t = this.task();
    if (!t || t.assignedAgentId) return;

    this.isDeleting.set(true);
    const success = await this.taskStore.deleteTask(this.taskId);
    this.isDeleting.set(false);

    if (success) {
      this.router.navigate(['/']);
    }
  }

  async pauseTask(): Promise<void> {
    this.isPausing.set(true);
    await this.taskStore.pauseTask(this.taskId, 'Manually paused by user');
    this.isPausing.set(false);
  }

  async resumeTask(): Promise<void> {
    this.isPausing.set(true);
    await this.taskStore.resumeTask(this.taskId);
    this.isPausing.set(false);
  }

  formatDate(date: Date): string {
    return new Date(date).toLocaleDateString(undefined, {
      year: 'numeric',
      month: 'short',
      day: 'numeric',
      hour: '2-digit',
      minute: '2-digit',
    });
  }
}
