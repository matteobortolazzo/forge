import { Injectable, computed, inject, signal } from '@angular/core';
import { Notification } from '../../shared/models';
import { NotificationService } from '../services/notification.service';
import { firstValueFrom } from 'rxjs';
import { createAsyncState, runAsync } from './store-utils';

@Injectable({ providedIn: 'root' })
export class NotificationStore {
  private readonly notificationService = inject(NotificationService);

  // State
  private readonly notifications = signal<Notification[]>([]);
  private readonly asyncState = createAsyncState();

  // Public readonly signals
  readonly allNotifications = this.notifications.asReadonly();
  readonly isLoading = this.asyncState.loading.asReadonly();
  readonly errorMessage = this.asyncState.error.asReadonly();

  // Computed: unread notifications
  readonly unreadNotifications = computed(() =>
    this.notifications().filter(n => !n.read)
  );

  // Computed: unread count
  readonly unreadCount = computed(() => this.unreadNotifications().length);

  // Computed: recent notifications (last 10)
  // Note: notifications are maintained in sorted order (newest first),
  // so we only need to slice, not sort.
  readonly recentNotifications = computed(() =>
    this.notifications().slice(0, 10)
  );

  // Load notifications from API
  async loadNotifications(limit = 50): Promise<void> {
    await runAsync(
      this.asyncState,
      async () => {
        const notifications = await firstValueFrom(
          this.notificationService.getNotifications(limit)
        );
        // Convert date strings to Date objects and sort by createdAt descending
        // (API may not return sorted, so we sort once here)
        const parsed = notifications
          .map(n => ({
            ...n,
            createdAt: new Date(n.createdAt),
          }))
          .sort((a, b) => b.createdAt.getTime() - a.createdAt.getTime());
        this.notifications.set(parsed);
      },
      {},
      'Failed to load notifications'
    );
  }

  // Add notification from SSE event
  addNotificationFromEvent(notification: Notification): void {
    const parsed: Notification = {
      ...notification,
      createdAt: new Date(notification.createdAt),
    };
    this.notifications.update(list => [parsed, ...list]);
  }

  // Mark notification as read (optimistic update + API call)
  async markAsRead(id: string): Promise<void> {
    // Optimistic update
    this.notifications.update(list =>
      list.map(n => (n.id === id ? { ...n, read: true } : n))
    );

    // Call API in background
    try {
      await firstValueFrom(this.notificationService.markAsRead(id));
    } catch (err) {
      console.error('Failed to mark notification as read:', err);
      // Revert optimistic update on error
      this.notifications.update(list =>
        list.map(n => (n.id === id ? { ...n, read: false } : n))
      );
    }
  }

  // Mark all notifications as read (optimistic update + API call)
  async markAllAsRead(): Promise<void> {
    // Store previous state for potential revert
    const previousState = this.notifications();

    // Optimistic update
    this.notifications.update(list =>
      list.map(n => ({ ...n, read: true }))
    );

    // Call API in background
    try {
      await firstValueFrom(this.notificationService.markAllAsRead());
    } catch (err) {
      console.error('Failed to mark all notifications as read:', err);
      // Revert optimistic update on error
      this.notifications.set(previousState);
    }
  }

  removeNotification(id: string): void {
    this.notifications.update(list => list.filter(n => n.id !== id));
  }

  clearAll(): void {
    this.notifications.set([]);
  }
}
