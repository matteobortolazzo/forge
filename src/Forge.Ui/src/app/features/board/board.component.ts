import { Component, ChangeDetectionStrategy, inject, signal, OnInit } from '@angular/core';
import { TaskStore } from '../../core/stores/task.store';
import { NotificationStore } from '../../core/stores/notification.store';
import { SchedulerStore } from '../../core/stores/scheduler.store';
import { PIPELINE_STATES, CreateTaskDto } from '../../shared/models';
import { TaskColumnComponent } from './task-column.component';
import { CreateTaskDialogComponent } from './create-task-dialog.component';
import { LoadingSpinnerComponent } from '../../shared/components/loading-spinner.component';
import { ErrorAlertComponent } from '../../shared/components/error-alert.component';
import { AppHeaderComponent } from '../../shared/components/app-header/app-header.component';

@Component({
  selector: 'app-board',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    TaskColumnComponent,
    CreateTaskDialogComponent,
    LoadingSpinnerComponent,
    ErrorAlertComponent,
    AppHeaderComponent,
  ],
  template: `
    <div class="flex h-screen flex-col bg-gray-100 dark:bg-gray-950">
      <!-- Header -->
      <app-header>
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
      </app-header>

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
export class BoardComponent implements OnInit {
  protected readonly taskStore = inject(TaskStore);
  private readonly notificationStore = inject(NotificationStore);
  protected readonly schedulerStore = inject(SchedulerStore);

  readonly pipelineStates = PIPELINE_STATES;
  readonly isDialogOpen = signal(false);

  ngOnInit(): void {
    // SSE connection is managed by App component
    // Repositories are loaded in App component (sidebar), tasks load when repository is selected
    this.loadNotifications();
    this.loadSchedulerStatus();
  }

  loadSchedulerStatus(): void {
    this.schedulerStore.loadStatus();
  }

  loadNotifications(): void {
    this.notificationStore.loadNotifications();
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
}
