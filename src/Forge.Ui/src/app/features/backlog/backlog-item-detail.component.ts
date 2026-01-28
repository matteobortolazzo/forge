import {
  Component,
  ChangeDetectionStrategy,
  inject,
  signal,
  computed,
  OnInit,
  OnDestroy,
} from '@angular/core';
import { Router, ActivatedRoute, RouterLink } from '@angular/router';
import { BacklogStore } from '../../core/stores/backlog.store';
import { TaskStore } from '../../core/stores/task.store';
import { RepositoryStore } from '../../core/stores/repository.store';
import { LogStore } from '../../core/stores/log.store';
import { SseService } from '../../core/services/sse.service';
import {
  BacklogItem,
  Task,
  TaskLog,
} from '../../shared/models';
import { BacklogStateBadgeComponent } from '../../shared/components/backlog-state-badge/backlog-state-badge.component';
import { StateBadgeComponent } from '../../shared/components/state-badge.component';
import { PriorityBadgeComponent } from '../../shared/components/priority-badge.component';
import { LoadingSpinnerComponent } from '../../shared/components/loading-spinner.component';
import { ErrorAlertComponent } from '../../shared/components/error-alert.component';
import { AgentIndicatorComponent } from '../../shared/components/agent-indicator.component';
import { PausedBadgeComponent } from '../../shared/components/paused-badge.component';
import { AgentOutputComponent } from '../task-detail/agent-output.component';
import { Subscription } from 'rxjs';

@Component({
  selector: 'app-backlog-item-detail',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    RouterLink,
    BacklogStateBadgeComponent,
    StateBadgeComponent,
    PriorityBadgeComponent,
    LoadingSpinnerComponent,
    ErrorAlertComponent,
    AgentIndicatorComponent,
    PausedBadgeComponent,
    AgentOutputComponent,
  ],
  template: `
    <div class="flex h-screen flex-col bg-gray-100 dark:bg-gray-950">
      <!-- Header -->
      <header class="flex items-center gap-4 border-b border-gray-200 bg-white px-6 py-4 dark:border-gray-800 dark:bg-gray-900">
        <a
          routerLink="/backlog"
          class="flex items-center gap-1 text-sm text-gray-500 hover:text-gray-700 dark:text-gray-400 dark:hover:text-gray-200"
        >
          <svg class="h-4 w-4" fill="none" viewBox="0 0 24 24" stroke-width="1.5" stroke="currentColor" aria-hidden="true">
            <path stroke-linecap="round" stroke-linejoin="round" d="M10.5 19.5L3 12m0 0l7.5-7.5M3 12h18" />
          </svg>
          Backlog
        </a>

        @if (item()) {
          <h1 class="flex-1 truncate text-lg font-semibold text-gray-900 dark:text-gray-100">
            {{ item()!.title }}
          </h1>

          <div class="flex items-center gap-3">
            <app-backlog-state-badge [state]="item()!.state" />
            <app-priority-badge [priority]="item()!.priority" />
            @if (item()!.assignedAgentId) {
              <app-agent-indicator />
            }
            @if (item()!.isPaused) {
              <app-paused-badge [reason]="item()!.pauseReason" />
            }
          </div>
        }
      </header>

      <!-- Main Content -->
      <main class="flex flex-1 overflow-hidden">
        @if (isLoading()) {
          <div class="flex flex-1 items-center justify-center">
            <app-loading-spinner size="lg" label="Loading..." />
          </div>
        } @else if (error()) {
          <div class="mx-auto flex max-w-md flex-col items-center justify-center p-6">
            <app-error-alert
              title="Failed to load item"
              [message]="error()!"
              [dismissible]="true"
            />
            <a routerLink="/backlog" class="mt-4 text-sm text-blue-600 hover:underline dark:text-blue-400">
              Return to Backlog
            </a>
          </div>
        } @else if (item()) {
          <!-- Left Panel: Details & Tasks -->
          <div class="flex flex-1 flex-col overflow-hidden border-r border-gray-200 dark:border-gray-800">
            <!-- Tabs -->
            <div class="flex border-b border-gray-200 bg-white dark:border-gray-700 dark:bg-gray-900">
              <button
                type="button"
                class="px-4 py-3 text-sm font-medium transition-colors"
                [class]="activeTab() === 'overview' ? 'border-b-2 border-blue-500 text-blue-600 dark:text-blue-400' : 'text-gray-500 hover:text-gray-700 dark:text-gray-400 dark:hover:text-gray-200'"
                (click)="activeTab.set('overview')"
              >
                Overview
              </button>
              <button
                type="button"
                class="px-4 py-3 text-sm font-medium transition-colors"
                [class]="activeTab() === 'tasks' ? 'border-b-2 border-blue-500 text-blue-600 dark:text-blue-400' : 'text-gray-500 hover:text-gray-700 dark:text-gray-400 dark:hover:text-gray-200'"
                (click)="activeTab.set('tasks')"
              >
                Tasks ({{ taskStore.backlogItemTasks().length }})
              </button>
            </div>

            <!-- Tab Content -->
            <div class="flex-1 overflow-y-auto bg-white p-6 dark:bg-gray-900">
              @if (activeTab() === 'overview') {
                <!-- Description -->
                <section class="mb-6">
                  <h2 class="mb-2 text-sm font-semibold uppercase tracking-wider text-gray-500 dark:text-gray-400">
                    Description
                  </h2>
                  <p class="whitespace-pre-wrap text-gray-700 dark:text-gray-300">
                    {{ item()!.description || 'No description provided.' }}
                  </p>
                </section>

                <!-- Acceptance Criteria -->
                @if (item()!.acceptanceCriteria) {
                  <section class="mb-6">
                    <h2 class="mb-2 text-sm font-semibold uppercase tracking-wider text-gray-500 dark:text-gray-400">
                      Acceptance Criteria
                    </h2>
                    <p class="whitespace-pre-wrap text-gray-700 dark:text-gray-300">
                      {{ item()!.acceptanceCriteria }}
                    </p>
                  </section>
                }

                <!-- Progress -->
                @if (item()!.progress) {
                  <section class="mb-6">
                    <h2 class="mb-2 text-sm font-semibold uppercase tracking-wider text-gray-500 dark:text-gray-400">
                      Progress
                    </h2>
                    <div class="flex items-center gap-4">
                      <div class="h-2 flex-1 overflow-hidden rounded-full bg-gray-200 dark:bg-gray-700">
                        <div
                          class="h-full rounded-full bg-blue-500 transition-all"
                          [style.width.%]="item()!.progress!.percent"
                        ></div>
                      </div>
                      <span class="text-sm font-medium text-gray-600 dark:text-gray-400">
                        {{ item()!.progress!.percent }}%
                      </span>
                    </div>
                    <p class="mt-1 text-sm text-gray-500 dark:text-gray-400">
                      {{ item()!.progress!.completed }} of {{ item()!.progress!.total }} tasks completed
                    </p>
                  </section>
                }

                <!-- Error -->
                @if (item()!.hasError && item()!.errorMessage) {
                  <section class="mb-6">
                    <h2 class="mb-2 text-sm font-semibold uppercase tracking-wider text-red-500">
                      Error
                    </h2>
                    <div class="rounded-lg border border-red-200 bg-red-50 p-4 dark:border-red-900 dark:bg-red-900/20">
                      <p class="text-sm text-red-700 dark:text-red-400">
                        {{ item()!.errorMessage }}
                      </p>
                    </div>
                  </section>
                }

                <!-- Actions -->
                <section class="mt-8">
                  <h2 class="mb-3 text-sm font-semibold uppercase tracking-wider text-gray-500 dark:text-gray-400">
                    Actions
                  </h2>
                  <div class="flex flex-wrap gap-2">
                    @if (canStartAgent()) {
                      <button
                        type="button"
                        class="inline-flex items-center gap-2 rounded-md bg-blue-600 px-4 py-2 text-sm font-medium text-white hover:bg-blue-700 focus:outline-none focus:ring-2 focus:ring-blue-500 focus:ring-offset-2"
                        (click)="onStartAgent()"
                      >
                        <svg class="h-4 w-4" fill="none" viewBox="0 0 24 24" stroke-width="1.5" stroke="currentColor" aria-hidden="true">
                          <path stroke-linecap="round" stroke-linejoin="round" d="M5.25 5.653c0-.856.917-1.398 1.667-.986l11.54 6.348a1.125 1.125 0 010 1.971l-11.54 6.347a1.125 1.125 0 01-1.667-.985V5.653z" />
                        </svg>
                        {{ startAgentButtonText() }}
                      </button>
                    }
                    @if (item()!.assignedAgentId) {
                      <button
                        type="button"
                        class="inline-flex items-center gap-2 rounded-md bg-red-600 px-4 py-2 text-sm font-medium text-white hover:bg-red-700 focus:outline-none focus:ring-2 focus:ring-red-500 focus:ring-offset-2"
                        (click)="onAbortAgent()"
                      >
                        <svg class="h-4 w-4" fill="none" viewBox="0 0 24 24" stroke-width="1.5" stroke="currentColor" aria-hidden="true">
                          <path stroke-linecap="round" stroke-linejoin="round" d="M5.25 7.5A2.25 2.25 0 017.5 5.25h9a2.25 2.25 0 012.25 2.25v9a2.25 2.25 0 01-2.25 2.25h-9a2.25 2.25 0 01-2.25-2.25v-9z" />
                        </svg>
                        Abort Agent
                      </button>
                    }
                    @if (item()!.isPaused) {
                      <button
                        type="button"
                        class="inline-flex items-center gap-2 rounded-md bg-green-600 px-4 py-2 text-sm font-medium text-white hover:bg-green-700 focus:outline-none focus:ring-2 focus:ring-green-500 focus:ring-offset-2"
                        (click)="onResume()"
                      >
                        Resume
                      </button>
                    } @else if (canPause()) {
                      <button
                        type="button"
                        class="inline-flex items-center gap-2 rounded-md border border-gray-300 bg-white px-4 py-2 text-sm font-medium text-gray-700 hover:bg-gray-50 focus:outline-none focus:ring-2 focus:ring-blue-500 focus:ring-offset-2 dark:border-gray-600 dark:bg-gray-800 dark:text-gray-200 dark:hover:bg-gray-700"
                        (click)="onPause()"
                      >
                        Pause
                      </button>
                    }
                  </div>
                </section>
              } @else if (activeTab() === 'tasks') {
                <!-- Task List -->
                @if (taskStore.isLoading()) {
                  <div class="flex items-center justify-center py-8">
                    <app-loading-spinner label="Loading tasks..." />
                  </div>
                } @else if (taskStore.backlogItemTasks().length === 0) {
                  <div class="flex flex-col items-center justify-center py-12 text-gray-500 dark:text-gray-400">
                    <svg class="mb-4 h-12 w-12" fill="none" viewBox="0 0 24 24" stroke-width="1.5" stroke="currentColor" aria-hidden="true">
                      <path stroke-linecap="round" stroke-linejoin="round" d="M9 12h3.75M9 15h3.75M9 18h3.75m3 .75H18a2.25 2.25 0 002.25-2.25V6.108c0-1.135-.845-2.098-1.976-2.192a48.424 48.424 0 00-1.123-.08m-5.801 0c-.065.21-.1.433-.1.664 0 .414.336.75.75.75h4.5a.75.75 0 00.75-.75 2.25 2.25 0 00-.1-.664m-5.8 0A2.251 2.251 0 0113.5 2.25H15c1.012 0 1.867.668 2.15 1.586m-5.8 0c-.376.023-.75.05-1.124.08C9.095 4.01 8.25 4.973 8.25 6.108V8.25m0 0H4.875c-.621 0-1.125.504-1.125 1.125v11.25c0 .621.504 1.125 1.125 1.125h9.75c.621 0 1.125-.504 1.125-1.125V9.375c0-.621-.504-1.125-1.125-1.125H8.25z" />
                    </svg>
                    <p class="text-lg font-medium">No tasks yet</p>
                    <p class="mt-1 text-sm">Tasks will be created when this item is split</p>
                  </div>
                } @else {
                  <div class="space-y-2">
                    @for (task of taskStore.tasksByExecutionOrder(); track task.id) {
                      <button
                        type="button"
                        class="flex w-full items-center gap-4 rounded-lg border border-gray-200 bg-gray-50 p-4 text-left transition-colors hover:bg-gray-100 focus:outline-none focus:ring-2 focus:ring-blue-500 dark:border-gray-700 dark:bg-gray-800 dark:hover:bg-gray-700"
                        (click)="onTaskClick(task)"
                      >
                        <span class="flex h-6 w-6 flex-shrink-0 items-center justify-center rounded-full bg-gray-200 text-xs font-medium text-gray-600 dark:bg-gray-700 dark:text-gray-400">
                          {{ task.executionOrder }}
                        </span>
                        <div class="min-w-0 flex-1">
                          <p class="truncate font-medium text-gray-900 dark:text-gray-100">
                            {{ task.title }}
                          </p>
                          @if (task.description) {
                            <p class="mt-0.5 truncate text-sm text-gray-500 dark:text-gray-400">
                              {{ task.description }}
                            </p>
                          }
                        </div>
                        <app-state-badge [state]="task.state" />
                        @if (task.assignedAgentId) {
                          <app-agent-indicator />
                        }
                        @if (task.isPaused) {
                          <app-paused-badge />
                        }
                        @if (task.hasError) {
                          <span class="inline-flex items-center gap-1 rounded bg-red-100 px-1.5 py-0.5 text-xs font-medium text-red-700 dark:bg-red-900/30 dark:text-red-400">
                            Error
                          </span>
                        }
                      </button>
                    }
                  </div>
                }
              }
            </div>
          </div>

          <!-- Right Panel: Agent Output -->
          <div class="hidden w-96 flex-col overflow-hidden lg:flex">
            <div class="flex items-center border-b border-gray-200 bg-white px-4 py-3 dark:border-gray-700 dark:bg-gray-900">
              <h2 class="text-sm font-semibold text-gray-700 dark:text-gray-200">Agent Output</h2>
            </div>
            <div class="flex-1 overflow-hidden bg-gray-900">
              <app-agent-output [logs]="backlogItemLogs()" />
            </div>
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
export class BacklogItemDetailComponent implements OnInit, OnDestroy {
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);
  protected readonly backlogStore = inject(BacklogStore);
  protected readonly taskStore = inject(TaskStore);
  protected readonly repositoryStore = inject(RepositoryStore);
  private readonly logStore = inject(LogStore);
  private readonly sseService = inject(SseService);

  private sseSubscription: Subscription | null = null;

  readonly activeTab = signal<'overview' | 'tasks'>('overview');
  readonly isLoading = signal(true);
  readonly error = signal<string | null>(null);

  readonly item = computed(() => {
    const id = this.route.snapshot.params['id'];
    return this.backlogStore.getItemById(id) ?? null;
  });

  readonly canStartAgent = computed(() => {
    const item = this.item();
    if (!item) return false;
    // Can start agent if not already assigned and in a state that allows it
    return !item.assignedAgentId && !item.isPaused &&
           (item.state === 'New' || item.state === 'Refining' || item.state === 'Ready' || item.state === 'Splitting');
  });

  readonly startAgentButtonText = computed(() => {
    const item = this.item();
    if (!item) return 'Start Agent';
    return item.state === 'New' ? 'Start Refinement' : 'Start Agent';
  });

  readonly canPause = computed(() => {
    const item = this.item();
    if (!item) return false;
    return !item.isPaused && item.state !== 'Done';
  });

  // Logs for backlog item - connects to LogStore
  readonly backlogItemLogs = computed(() => {
    const item = this.item();
    if (!item) return [];
    return this.logStore.getLogsForBacklogItem(item.id);
  });

  async ngOnInit(): Promise<void> {
    const itemId = this.route.snapshot.params['id'];

    // Set the task context for this backlog item
    this.taskStore.setBacklogItemContext(itemId);

    // Load data
    try {
      await this.backlogStore.loadItems();
      await this.taskStore.loadTasks(itemId);
      this.isLoading.set(false);

      // If item not found after loading, show error
      const item = this.item();
      if (!item) {
        this.error.set('Backlog item not found');
      } else {
        // Load historical logs for this backlog item
        await this.logStore.loadLogsForBacklogItem(itemId, item.repositoryId);
      }
    } catch (err) {
      this.error.set(err instanceof Error ? err.message : 'Failed to load item');
      this.isLoading.set(false);
    }

    // Connect to SSE for real-time updates
    this.sseSubscription = this.sseService.connect().subscribe(event => {
      this.handleSseEvent(event);
    });
  }

  ngOnDestroy(): void {
    this.sseSubscription?.unsubscribe();
    this.taskStore.setBacklogItemContext(null);
    // Clear logs for this backlog item to free memory
    const item = this.item();
    if (item) {
      this.logStore.clearLogsForBacklogItem(item.id);
    }
  }

  private handleSseEvent(event: { type: string; payload: unknown }): void {
    switch (event.type) {
      case 'backlogItem:updated':
        this.backlogStore.updateItemFromEvent(event.payload as BacklogItem);
        break;
      case 'task:created':
      case 'task:updated':
        this.taskStore.updateTaskFromEvent(event.payload as Task);
        break;
      case 'task:deleted':
        this.taskStore.removeTaskFromEvent((event.payload as { id: string }).id);
        break;
      case 'task:log':
        this.logStore.addLog(event.payload as TaskLog);
        break;
      case 'backlogItem:log':
        this.logStore.addLog(event.payload as TaskLog);
        break;
    }
  }

  async onStartAgent(): Promise<void> {
    const item = this.item();
    if (item) {
      await this.backlogStore.startAgent(item.id);
    }
  }

  async onAbortAgent(): Promise<void> {
    const item = this.item();
    if (item) {
      await this.backlogStore.abortAgent(item.id);
    }
  }

  async onPause(): Promise<void> {
    const item = this.item();
    if (item) {
      await this.backlogStore.pauseItem(item.id, 'Manual pause');
    }
  }

  async onResume(): Promise<void> {
    const item = this.item();
    if (item) {
      await this.backlogStore.resumeItem(item.id);
    }
  }

  onTaskClick(task: Task): void {
    this.router.navigate(['/backlog', task.backlogItemId, 'tasks', task.id]);
  }
}
