import { Component, ChangeDetectionStrategy, inject, signal, OnInit, OnDestroy } from '@angular/core';
import { TaskStore } from '../../core/stores/task.store';
import { LogStore } from '../../core/stores/log.store';
import { NotificationStore } from '../../core/stores/notification.store';
import { SseService } from '../../core/services/sse.service';
import { PIPELINE_STATES, CreateTaskDto, ServerEvent, TaskLog, Task, Notification } from '../../shared/models';
import { TaskColumnComponent } from './task-column.component';
import { CreateTaskDialogComponent } from './create-task-dialog.component';
import { NotificationPanelComponent } from '../notifications/notification-panel.component';
import { LoadingSpinnerComponent } from '../../shared/components/loading-spinner.component';
import { ErrorAlertComponent } from '../../shared/components/error-alert.component';
import { Subscription } from 'rxjs';

@Component({
  selector: 'app-board',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    TaskColumnComponent,
    CreateTaskDialogComponent,
    NotificationPanelComponent,
    LoadingSpinnerComponent,
    ErrorAlertComponent,
  ],
  template: `
    <div class="flex h-screen flex-col bg-gray-100 dark:bg-gray-950">
      <!-- Header -->
      <header class="flex items-center justify-between border-b border-gray-200 bg-white px-6 py-4 dark:border-gray-800 dark:bg-gray-900">
        <div class="flex items-center gap-3">
          <h1 class="text-xl font-bold text-gray-900 dark:text-gray-100">
            Forge
          </h1>
          <span class="text-sm text-gray-500 dark:text-gray-400">
            AI Agent Dashboard
          </span>
        </div>

        <div class="flex items-center gap-4">
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

      <!-- Main Content -->
      <main class="flex-1 overflow-hidden p-6">
        @if (taskStore.isLoading()) {
          <div class="flex h-full items-center justify-center">
            <app-loading-spinner size="lg" label="Loading tasks..." />
          </div>
        } @else if (taskStore.errorMessage()) {
          <div class="mx-auto max-w-md">
            <app-error-alert
              title="Failed to load tasks"
              [message]="taskStore.errorMessage()!"
              [dismissible]="true"
              (dismiss)="loadTasks()"
            />
          </div>
        } @else {
          <!-- Kanban Board -->
          <div class="grid h-full grid-cols-7 gap-4">
            @for (state of pipelineStates; track state) {
              <app-task-column
                [state]="state"
                [tasks]="taskStore.tasksByState()[state]"
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
  `,
  styles: `
    :host {
      display: block;
      height: 100vh;
    }
  `,
})
export class BoardComponent implements OnInit, OnDestroy {
  protected readonly taskStore = inject(TaskStore);
  private readonly logStore = inject(LogStore);
  private readonly notificationStore = inject(NotificationStore);
  private readonly sseService = inject(SseService);

  readonly pipelineStates = PIPELINE_STATES;
  readonly isDialogOpen = signal(false);

  private sseSubscription?: Subscription;

  ngOnInit(): void {
    this.loadTasks();
    this.loadNotifications();
    this.connectToSse();
  }

  loadNotifications(): void {
    this.notificationStore.loadNotifications();
  }

  ngOnDestroy(): void {
    this.sseSubscription?.unsubscribe();
    this.sseService.disconnect();
  }

  loadTasks(): void {
    this.taskStore.loadTasks();
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

  private connectToSse(): void {
    this.sseSubscription = this.sseService.connect().subscribe({
      next: (event: ServerEvent) => this.handleSseEvent(event),
      error: (err) => console.error('SSE error:', err),
    });
  }

  private handleSseEvent(event: ServerEvent): void {
    switch (event.type) {
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
      case 'agent:statusChanged':
        // Handle agent status changes if needed
        break;
      case 'notification:new':
        this.notificationStore.addNotificationFromEvent(event.payload as Notification);
        break;
    }
  }
}
