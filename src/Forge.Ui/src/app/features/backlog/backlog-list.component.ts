import {
  Component,
  ChangeDetectionStrategy,
  inject,
  signal,
  OnInit,
} from '@angular/core';
import { Router } from '@angular/router';
import { BacklogStore } from '../../core/stores/backlog.store';
import { RepositoryStore } from '../../core/stores/repository.store';
import { NotificationStore } from '../../core/stores/notification.store';
import { SchedulerStore } from '../../core/stores/scheduler.store';
import {
  BacklogItem,
  BacklogItemState,
  BACKLOG_ITEM_STATES,
  CreateBacklogItemDto,
} from '../../shared/models';
import { PriorityBadgeComponent } from '../../shared/components/priority-badge.component';
import { LoadingSpinnerComponent } from '../../shared/components/loading-spinner.component';
import { ErrorAlertComponent } from '../../shared/components/error-alert.component';
import { AppHeaderComponent } from '../../shared/components/app-header/app-header.component';
import { AgentIndicatorComponent } from '../../shared/components/agent-indicator.component';
import { PausedBadgeComponent } from '../../shared/components/paused-badge.component';
import { CreateBacklogItemDialogComponent } from './create-backlog-item-dialog.component';

/** Human-readable labels for backlog item states */
const STATE_LABELS: Record<BacklogItemState, string> = {
  New: 'New',
  Refining: 'Refining',
  Ready: 'Ready',
  Splitting: 'Splitting',
  Executing: 'In Progress',
  Done: 'Done',
};

@Component({
  selector: 'app-backlog-list',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    PriorityBadgeComponent,
    LoadingSpinnerComponent,
    ErrorAlertComponent,
    AppHeaderComponent,
    AgentIndicatorComponent,
    PausedBadgeComponent,
    CreateBacklogItemDialogComponent,
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
          New Item
        </button>
      </app-header>

      <!-- Main Content -->
      <main class="flex-1 overflow-hidden">
        @if (backlogStore.isLoading()) {
          <div class="flex h-full items-center justify-center">
            <app-loading-spinner size="lg" label="Loading backlog..." />
          </div>
        } @else if (backlogStore.errorMessage()) {
          <div class="mx-auto max-w-md p-6">
            <app-error-alert
              title="Failed to load backlog"
              [message]="backlogStore.errorMessage()!"
              [dismissible]="true"
              (dismiss)="loadItems()"
            />
          </div>
        } @else {
          <!-- Kanban Board -->
          <div class="flex h-full gap-4 overflow-x-auto p-4">
            @for (state of states; track state) {
              <div class="flex h-full w-72 flex-shrink-0 flex-col rounded-lg bg-white shadow dark:bg-gray-900">
                <!-- Column Header -->
                <div class="flex items-center justify-between border-b border-gray-200 px-4 py-3 dark:border-gray-700">
                  <div class="flex items-center gap-2">
                    <h2 class="text-sm font-semibold text-gray-700 dark:text-gray-200">
                      {{ getStateLabel(state) }}
                    </h2>
                    <span class="rounded-full bg-gray-100 px-2 py-0.5 text-xs font-medium text-gray-600 dark:bg-gray-800 dark:text-gray-400">
                      {{ backlogStore.itemCountByState()[state] }}
                    </span>
                  </div>
                </div>

                <!-- Column Content -->
                <div class="flex-1 overflow-y-auto p-2">
                  @for (item of backlogStore.itemsByState()[state]; track item.id) {
                    <button
                      type="button"
                      class="mb-2 w-full cursor-pointer rounded-lg border border-gray-200 bg-white p-3 text-left shadow-sm transition-shadow hover:shadow-md focus:outline-none focus:ring-2 focus:ring-blue-500 dark:border-gray-700 dark:bg-gray-800"
                      (click)="onItemClick(item)"
                    >
                      <!-- Card Header -->
                      <div class="mb-2 flex items-start justify-between gap-2">
                        <h3 class="line-clamp-2 text-sm font-medium text-gray-900 dark:text-gray-100">
                          {{ item.title }}
                        </h3>
                        <app-priority-badge [priority]="item.priority" />
                      </div>

                      <!-- Card Description -->
                      @if (item.description) {
                        <p class="mb-2 line-clamp-2 text-xs text-gray-500 dark:text-gray-400">
                          {{ item.description }}
                        </p>
                      }

                      <!-- Card Footer -->
                      <div class="flex flex-wrap items-center gap-2">
                        @if (item.assignedAgentId) {
                          <app-agent-indicator />
                        }
                        @if (item.isPaused) {
                          <app-paused-badge [reason]="item.pauseReason" />
                        }
                        @if (item.hasError) {
                          <span class="inline-flex items-center gap-1 rounded bg-red-100 px-1.5 py-0.5 text-xs font-medium text-red-700 dark:bg-red-900/30 dark:text-red-400">
                            <svg class="h-3 w-3" fill="currentColor" viewBox="0 0 20 20" aria-hidden="true">
                              <path fill-rule="evenodd" d="M10 18a8 8 0 100-16 8 8 0 000 16zM8.28 7.22a.75.75 0 00-1.06 1.06L8.94 10l-1.72 1.72a.75.75 0 101.06 1.06L10 11.06l1.72 1.72a.75.75 0 101.06-1.06L11.06 10l1.72-1.72a.75.75 0 00-1.06-1.06L10 8.94 8.28 7.22z" clip-rule="evenodd" />
                            </svg>
                            Error
                          </span>
                        }
                        @if (item.progress && item.taskCount > 0) {
                          <span class="ml-auto text-xs text-gray-500 dark:text-gray-400">
                            {{ item.progress.completed }}/{{ item.progress.total }} tasks
                          </span>
                        }
                      </div>

                      <!-- Progress Bar -->
                      @if (item.progress && item.taskCount > 0) {
                        <div class="mt-2 h-1 w-full overflow-hidden rounded-full bg-gray-200 dark:bg-gray-700">
                          <div
                            class="h-full rounded-full bg-blue-500 transition-all"
                            [style.width.%]="item.progress.percent"
                          ></div>
                        </div>
                      }
                    </button>
                  }

                  @if (backlogStore.itemsByState()[state].length === 0) {
                    <div class="flex h-24 items-center justify-center text-sm text-gray-400 dark:text-gray-500">
                      No items
                    </div>
                  }
                </div>
              </div>
            }
          </div>
        }
      </main>
    </div>

    <!-- Create Dialog -->
    <app-create-backlog-item-dialog
      [isOpen]="isDialogOpen()"
      (create)="onCreateItem($event)"
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
export class BacklogListComponent implements OnInit {
  private readonly router = inject(Router);
  protected readonly backlogStore = inject(BacklogStore);
  protected readonly repositoryStore = inject(RepositoryStore);
  private readonly notificationStore = inject(NotificationStore);
  protected readonly schedulerStore = inject(SchedulerStore);

  readonly states = BACKLOG_ITEM_STATES;
  readonly isDialogOpen = signal(false);

  ngOnInit(): void {
    // Backlog items load automatically via effect when repository is selected
    this.loadNotifications();
    this.loadSchedulerStatus();
  }

  loadItems(): void {
    this.backlogStore.loadItems();
  }

  loadNotifications(): void {
    this.notificationStore.loadNotifications();
  }

  loadSchedulerStatus(): void {
    this.schedulerStore.loadStatus();
  }

  getStateLabel(state: BacklogItemState): string {
    return STATE_LABELS[state] ?? state;
  }

  onItemClick(item: BacklogItem): void {
    this.router.navigate(['/backlog', item.id]);
  }

  openCreateDialog(): void {
    this.isDialogOpen.set(true);
  }

  closeCreateDialog(): void {
    this.isDialogOpen.set(false);
  }

  async onCreateItem(dto: CreateBacklogItemDto): Promise<void> {
    const item = await this.backlogStore.createItem(dto);
    if (item) {
      this.closeCreateDialog();
    }
  }
}
