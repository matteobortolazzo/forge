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
import { SlicePipe } from '@angular/common';
import { TaskStore } from '../../core/stores/task.store';
import { BacklogStore } from '../../core/stores/backlog.store';
import { LogStore } from '../../core/stores/log.store';
import { ArtifactStore } from '../../core/stores/artifact.store';
import { SseService } from '../../core/services/sse.service';
import { TaskService } from '../../core/services/task.service';
import { Task, TaskLog, Artifact, ServerEvent, BacklogItem } from '../../shared/models';
import { StateBadgeComponent } from '../../shared/components/state-badge.component';
import { PriorityBadgeComponent } from '../../shared/components/priority-badge.component';
import { AgentIndicatorComponent } from '../../shared/components/agent-indicator.component';
import { PausedBadgeComponent } from '../../shared/components/paused-badge.component';
import { ErrorAlertComponent } from '../../shared/components/error-alert.component';
import { LoadingSpinnerComponent } from '../../shared/components/loading-spinner.component';
import { ArtifactPanelComponent } from '../../shared/components/artifact-panel.component';
import { AgentOutputComponent } from './agent-output.component';
import { Subscription } from 'rxjs';

@Component({
  selector: 'app-task-detail',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    RouterLink,
    SlicePipe,
    StateBadgeComponent,
    PriorityBadgeComponent,
    AgentIndicatorComponent,
    PausedBadgeComponent,
    ErrorAlertComponent,
    LoadingSpinnerComponent,
    ArtifactPanelComponent,
    AgentOutputComponent,
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
          <span class="font-medium text-gray-900 dark:text-gray-100">
            Task #{{ task()?.executionOrder }}
          </span>
        </nav>
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
                routerLink="/backlog"
                class="mt-4 inline-block text-sm font-medium text-blue-600 hover:text-blue-700 dark:text-blue-400"
              >
                Return to backlog
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

            <!-- Parent Backlog Item Link -->
            @if (backlogItem()) {
              <div class="mt-2 flex items-center gap-2 text-sm text-gray-500 dark:text-gray-400">
                <svg class="h-4 w-4" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 20 20" fill="currentColor" aria-hidden="true">
                  <path fill-rule="evenodd" d="M12.207 2.232a.75.75 0 00.025 1.06l4.146 3.958H6.375a5.375 5.375 0 000 10.75H9.25a.75.75 0 000-1.5H6.375a3.875 3.875 0 010-7.75h10.003l-4.146 3.957a.75.75 0 001.036 1.085l5.5-5.25a.75.75 0 000-1.085l-5.5-5.25a.75.75 0 00-1.06.025z" clip-rule="evenodd" />
                </svg>
                <span>Part of</span>
                <a
                  [routerLink]="['/backlog', backlogItem()!.id]"
                  class="font-medium text-blue-600 hover:text-blue-700 dark:text-blue-400 dark:hover:text-blue-300"
                >
                  {{ backlogItem()!.title }}
                </a>
              </div>
            }

            <!-- Badges -->
            <div class="mt-4 flex flex-wrap items-center gap-2">
              <app-state-badge [state]="task()!.state" />
              <app-priority-badge [priority]="task()!.priority" />
              <span class="inline-flex items-center rounded-full bg-gray-100 px-2.5 py-0.5 text-xs font-medium text-gray-800 dark:bg-gray-800 dark:text-gray-200">
                Task #{{ task()!.executionOrder }}
              </span>
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
              <p class="mt-2 whitespace-pre-wrap text-sm text-gray-600 dark:text-gray-400">
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
              @if (task()!.confidenceScore !== undefined && task()!.confidenceScore !== null) {
                <div class="flex items-center justify-between text-sm">
                  <span class="text-gray-500 dark:text-gray-400">Confidence</span>
                  <span class="text-gray-700 dark:text-gray-300">{{ (task()!.confidenceScore! * 100).toFixed(0) }}%</span>
                </div>
              }
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

              <!-- Start Agent Button -->
              @if (!task()!.assignedAgentId && !task()!.isPaused && task()!.state !== 'Done') {
                <button
                  type="button"
                  class="w-full rounded-md bg-green-600 px-4 py-2 text-sm font-medium text-white hover:bg-green-700 focus:outline-none focus:ring-2 focus:ring-green-500 focus:ring-offset-2"
                  (click)="startAgent()"
                  [disabled]="isStartingAgent()"
                >
                  @if (isStartingAgent()) {
                    Starting Agent...
                  } @else {
                    Start Agent
                  }
                </button>
              }

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
              @if (!task()!.assignedAgentId && task()!.state !== 'Done') {
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

          <!-- Right Panel: Artifacts & Agent Output -->
          <div class="flex flex-1 flex-col overflow-hidden">
            <!-- Artifacts Section -->
            @if (artifacts().length > 0) {
              <div class="h-64 shrink-0 border-b border-gray-200 bg-white dark:border-gray-800 dark:bg-gray-900">
                <app-artifact-panel [artifacts]="artifacts()" />
              </div>
            }

            <!-- Agent Output Section -->
            <div class="flex-1 overflow-hidden p-6">
              <app-agent-output [logs]="logs()" />
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
export class TaskDetailComponent implements OnInit, OnDestroy {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly taskStore = inject(TaskStore);
  private readonly backlogStore = inject(BacklogStore);
  private readonly logStore = inject(LogStore);
  private readonly artifactStore = inject(ArtifactStore);
  private readonly taskService = inject(TaskService);
  private readonly sseService = inject(SseService);

  readonly isLoading = signal(true);
  readonly isTransitioning = signal(false);
  readonly isAborting = signal(false);
  readonly isDeleting = signal(false);
  readonly isPausing = signal(false);
  readonly isStartingAgent = signal(false);

  private taskId = '';
  private backlogItemId = '';
  private sseSubscription?: Subscription;

  readonly task = computed(() => this.taskStore.getTaskById(this.taskId));
  readonly backlogItem = computed(() => this.backlogStore.getItemById(this.backlogItemId));
  readonly logs = computed(() => this.logStore.getLogsForTask(this.taskId));
  readonly artifacts = computed(() => this.artifactStore.getArtifactsForTask(this.taskId));

  readonly nextState = computed(() => {
    const t = this.task();
    return t ? this.taskService.getNextState(t.state) : null;
  });

  readonly previousState = computed(() => {
    const t = this.task();
    return t ? this.taskService.getPreviousState(t.state) : null;
  });

  readonly canMoveNext = computed(() => {
    const t = this.task();
    return t && !!this.nextState();
  });

  readonly canMovePrevious = computed(() => {
    const t = this.task();
    return t && !!this.previousState();
  });

  ngOnInit(): void {
    this.backlogItemId = this.route.snapshot.paramMap.get('backlogItemId') || '';
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

    // Set context for task store
    this.taskStore.setBacklogItemContext(this.backlogItemId);

    // Load backlog items and tasks
    await Promise.all([
      this.backlogStore.loadItems(),
      this.taskStore.loadTasks(this.backlogItemId),
    ]);

    // Get task and load logs/artifacts
    const task = this.taskStore.getTaskById(this.taskId);
    if (task) {
      await Promise.all([
        this.logStore.loadLogsForTask(this.taskId),
        this.artifactStore.loadArtifactsForTask(
          task.repositoryId,
          task.backlogItemId,
          this.taskId
        ),
      ]);
    }

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
      case 'backlogItem:updated':
        this.backlogStore.updateItemFromEvent(event.payload as BacklogItem);
        break;
      case 'artifact:created': {
        const artifact = event.payload as Artifact;
        if (artifact.taskId === this.taskId) {
          this.artifactStore.addArtifactFromEvent(artifact);
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

  async startAgent(): Promise<void> {
    this.isStartingAgent.set(true);
    await this.taskStore.startAgent(this.taskId);
    this.isStartingAgent.set(false);
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
      this.router.navigate(['/backlog', this.backlogItemId]);
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
