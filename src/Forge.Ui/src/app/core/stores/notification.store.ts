import { Injectable, computed, signal } from '@angular/core';
import { Notification } from '../../shared/models';
import { MOCK_NOTIFICATIONS } from '../mocks/mock-data';

@Injectable({ providedIn: 'root' })
export class NotificationStore {
  // State
  private readonly notifications = signal<Notification[]>([...MOCK_NOTIFICATIONS]);

  // Public readonly signals
  readonly allNotifications = this.notifications.asReadonly();

  // Computed: unread notifications
  readonly unreadNotifications = computed(() =>
    this.notifications().filter(n => !n.read)
  );

  // Computed: unread count
  readonly unreadCount = computed(() => this.unreadNotifications().length);

  // Computed: recent notifications (last 10)
  readonly recentNotifications = computed(() =>
    [...this.notifications()]
      .sort((a, b) => b.createdAt.getTime() - a.createdAt.getTime())
      .slice(0, 10)
  );

  // Actions
  addNotification(notification: Omit<Notification, 'id' | 'createdAt' | 'read'>): void {
    const newNotification: Notification = {
      ...notification,
      id: `notif-${Date.now()}`,
      read: false,
      createdAt: new Date(),
    };
    this.notifications.update(list => [newNotification, ...list]);
  }

  markAsRead(id: string): void {
    this.notifications.update(list =>
      list.map(n => (n.id === id ? { ...n, read: true } : n))
    );
  }

  markAllAsRead(): void {
    this.notifications.update(list =>
      list.map(n => ({ ...n, read: true }))
    );
  }

  removeNotification(id: string): void {
    this.notifications.update(list => list.filter(n => n.id !== id));
  }

  clearAll(): void {
    this.notifications.set([]);
  }

  // Create notification helpers
  notifyTaskCreated(taskTitle: string, taskId: string): void {
    this.addNotification({
      title: 'Task Created',
      message: `"${taskTitle}" has been added to the backlog.`,
      type: 'info',
      taskId,
    });
  }

  notifyTaskCompleted(taskTitle: string, taskId: string): void {
    this.addNotification({
      title: 'Task Completed',
      message: `"${taskTitle}" has been completed.`,
      type: 'success',
      taskId,
    });
  }

  notifyAgentStarted(taskTitle: string, taskId: string): void {
    this.addNotification({
      title: 'Agent Started',
      message: `Agent assigned to "${taskTitle}"`,
      type: 'info',
      taskId,
    });
  }

  notifyAgentError(taskTitle: string, taskId: string, errorMessage: string): void {
    this.addNotification({
      title: 'Agent Error',
      message: `Error on "${taskTitle}": ${errorMessage}`,
      type: 'error',
      taskId,
    });
  }
}
