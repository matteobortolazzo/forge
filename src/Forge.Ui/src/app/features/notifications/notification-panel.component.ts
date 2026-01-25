import { Component, ChangeDetectionStrategy, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { NotificationStore } from '../../core/stores/notification.store';
import { Notification } from '../../shared/models';

@Component({
  selector: 'app-notification-panel',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="relative">
      <!-- Bell Button -->
      <button
        type="button"
        class="relative rounded-full p-2 text-gray-500 hover:bg-gray-100 hover:text-gray-700 focus:outline-none focus:ring-2 focus:ring-blue-500 dark:text-gray-400 dark:hover:bg-gray-800 dark:hover:text-gray-200"
        (click)="togglePanel()"
        [attr.aria-expanded]="isPanelOpen()"
        aria-haspopup="true"
        aria-label="Notifications"
      >
        <svg
          class="h-6 w-6"
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
            d="M14.857 17.082a23.848 23.848 0 005.454-1.31A8.967 8.967 0 0118 9.75v-.7V9A6 6 0 006 9v.75a8.967 8.967 0 01-2.312 6.022c1.733.64 3.56 1.085 5.455 1.31m5.714 0a24.255 24.255 0 01-5.714 0m5.714 0a3 3 0 11-5.714 0"
          />
        </svg>

        <!-- Unread Badge -->
        @if (notificationStore.unreadCount() > 0) {
          <span
            class="absolute -right-0.5 -top-0.5 flex h-5 min-w-5 items-center justify-center rounded-full bg-red-500 px-1 text-xs font-medium text-white"
            [attr.aria-label]="notificationStore.unreadCount() + ' unread notifications'"
          >
            {{ notificationStore.unreadCount() > 99 ? '99+' : notificationStore.unreadCount() }}
          </span>
        }
      </button>

      <!-- Dropdown Panel -->
      @if (isPanelOpen()) {
        <div
          class="absolute right-0 z-50 mt-2 w-80 origin-top-right rounded-lg bg-white shadow-lg ring-1 ring-black ring-opacity-5 dark:bg-gray-800 dark:ring-gray-700"
          role="menu"
          aria-orientation="vertical"
        >
          <!-- Header -->
          <div class="flex items-center justify-between border-b border-gray-200 px-4 py-3 dark:border-gray-700">
            <h3 class="text-sm font-semibold text-gray-900 dark:text-gray-100">
              Notifications
            </h3>
            @if (notificationStore.unreadCount() > 0) {
              <button
                type="button"
                class="text-xs font-medium text-blue-600 hover:text-blue-700 dark:text-blue-400 dark:hover:text-blue-300"
                (click)="markAllAsRead()"
              >
                Mark all as read
              </button>
            }
          </div>

          <!-- Notification List -->
          <div class="max-h-96 overflow-y-auto">
            @for (notification of notificationStore.recentNotifications(); track notification.id) {
              <div
                [class]="getNotificationClasses(notification)"
                (click)="onNotificationClick(notification)"
                (keydown.enter)="onNotificationClick(notification)"
                role="menuitem"
                tabindex="0"
              >
                <div class="flex items-start gap-3">
                  <span [class]="getIconClasses(notification)">
                    @switch (notification.type) {
                      @case ('success') {
                        <svg class="h-5 w-5" viewBox="0 0 20 20" fill="currentColor">
                          <path fill-rule="evenodd" d="M10 18a8 8 0 100-16 8 8 0 000 16zm3.857-9.809a.75.75 0 00-1.214-.882l-3.483 4.79-1.88-1.88a.75.75 0 10-1.06 1.061l2.5 2.5a.75.75 0 001.137-.089l4-5.5z" clip-rule="evenodd" />
                        </svg>
                      }
                      @case ('error') {
                        <svg class="h-5 w-5" viewBox="0 0 20 20" fill="currentColor">
                          <path fill-rule="evenodd" d="M10 18a8 8 0 100-16 8 8 0 000 16zM8.28 7.22a.75.75 0 00-1.06 1.06L8.94 10l-1.72 1.72a.75.75 0 101.06 1.06L10 11.06l1.72 1.72a.75.75 0 101.06-1.06L11.06 10l1.72-1.72a.75.75 0 00-1.06-1.06L10 8.94 8.28 7.22z" clip-rule="evenodd" />
                        </svg>
                      }
                      @case ('warning') {
                        <svg class="h-5 w-5" viewBox="0 0 20 20" fill="currentColor">
                          <path fill-rule="evenodd" d="M8.485 2.495c.673-1.167 2.357-1.167 3.03 0l6.28 10.875c.673 1.167-.17 2.625-1.516 2.625H3.72c-1.347 0-2.189-1.458-1.515-2.625L8.485 2.495zM10 5a.75.75 0 01.75.75v3.5a.75.75 0 01-1.5 0v-3.5A.75.75 0 0110 5zm0 9a1 1 0 100-2 1 1 0 000 2z" clip-rule="evenodd" />
                        </svg>
                      }
                      @default {
                        <svg class="h-5 w-5" viewBox="0 0 20 20" fill="currentColor">
                          <path fill-rule="evenodd" d="M18 10a8 8 0 11-16 0 8 8 0 0116 0zm-7-4a1 1 0 11-2 0 1 1 0 012 0zM9 9a.75.75 0 000 1.5h.253a.25.25 0 01.244.304l-.459 2.066A1.75 1.75 0 0010.747 15H11a.75.75 0 000-1.5h-.253a.25.25 0 01-.244-.304l.459-2.066A1.75 1.75 0 009.253 9H9z" clip-rule="evenodd" />
                        </svg>
                      }
                    }
                  </span>
                  <div class="flex-1 min-w-0">
                    <p class="text-sm font-medium text-gray-900 dark:text-gray-100">
                      {{ notification.title }}
                    </p>
                    <p class="mt-0.5 text-xs text-gray-500 dark:text-gray-400 line-clamp-2">
                      {{ notification.message }}
                    </p>
                    <p class="mt-1 text-xs text-gray-400 dark:text-gray-500">
                      {{ formatTime(notification.createdAt) }}
                    </p>
                  </div>
                  @if (!notification.read) {
                    <span class="mt-1 h-2 w-2 rounded-full bg-blue-500" aria-label="Unread"></span>
                  }
                </div>
              </div>
            } @empty {
              <div class="px-4 py-8 text-center text-sm text-gray-500 dark:text-gray-400">
                No notifications yet
              </div>
            }
          </div>
        </div>
      }
    </div>
  `,
  host: {
    '(document:click)': 'onDocumentClick($event)',
    '(document:keydown.escape)': 'closePanel()',
  },
})
export class NotificationPanelComponent {
  protected readonly notificationStore = inject(NotificationStore);
  private readonly router = inject(Router);

  readonly isPanelOpen = signal(false);

  togglePanel(): void {
    this.isPanelOpen.update(open => !open);
  }

  closePanel(): void {
    this.isPanelOpen.set(false);
  }

  markAllAsRead(): void {
    this.notificationStore.markAllAsRead();
  }

  onNotificationClick(notification: Notification): void {
    this.notificationStore.markAsRead(notification.id);
    if (notification.taskId) {
      this.router.navigate(['/tasks', notification.taskId]);
      this.closePanel();
    }
  }

  onDocumentClick(event: MouseEvent): void {
    const target = event.target as HTMLElement;
    if (!target.closest('app-notification-panel')) {
      this.closePanel();
    }
  }

  getNotificationClasses(notification: Notification): string {
    const base = 'cursor-pointer px-4 py-3 hover:bg-gray-50 dark:hover:bg-gray-700 transition-colors';
    return notification.read
      ? base
      : `${base} bg-blue-50 dark:bg-blue-900/20`;
  }

  getIconClasses(notification: Notification): string {
    const base = 'flex-shrink-0 mt-0.5';
    switch (notification.type) {
      case 'success':
        return `${base} text-green-500`;
      case 'error':
        return `${base} text-red-500`;
      case 'warning':
        return `${base} text-amber-500`;
      default:
        return `${base} text-blue-500`;
    }
  }

  formatTime(date: Date): string {
    const now = new Date();
    const diff = now.getTime() - date.getTime();
    const minutes = Math.floor(diff / 60000);
    const hours = Math.floor(diff / 3600000);
    const days = Math.floor(diff / 86400000);

    if (minutes < 1) return 'Just now';
    if (minutes < 60) return `${minutes}m ago`;
    if (hours < 24) return `${hours}h ago`;
    if (days < 7) return `${days}d ago`;
    return date.toLocaleDateString();
  }
}
