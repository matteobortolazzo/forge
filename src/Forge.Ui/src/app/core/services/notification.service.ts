import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, of, delay } from 'rxjs';
import { Notification } from '../../shared/models';
import { MOCK_NOTIFICATIONS } from '../mocks/mock-data';

@Injectable({ providedIn: 'root' })
export class NotificationService {
  private readonly http = inject(HttpClient);
  private readonly useMocks = false;
  private readonly apiUrl = '/api/notifications';

  // In-memory store for mock mode
  private mockNotifications: Notification[] = [...MOCK_NOTIFICATIONS];

  getNotifications(limit = 50): Observable<Notification[]> {
    if (this.useMocks) {
      return of([...this.mockNotifications].slice(0, limit)).pipe(delay(200));
    }
    return this.http.get<Notification[]>(`${this.apiUrl}?limit=${limit}`);
  }

  markAsRead(id: string): Observable<void> {
    if (this.useMocks) {
      const notification = this.mockNotifications.find(n => n.id === id);
      if (notification) {
        notification.read = true;
      }
      return of(undefined).pipe(delay(100));
    }
    return this.http.patch<void>(`${this.apiUrl}/${id}/read`, {});
  }

  markAllAsRead(): Observable<{ markedCount: number }> {
    if (this.useMocks) {
      const count = this.mockNotifications.filter(n => !n.read).length;
      this.mockNotifications.forEach(n => (n.read = true));
      return of({ markedCount: count }).pipe(delay(100));
    }
    return this.http.post<{ markedCount: number }>(`${this.apiUrl}/mark-all-read`, {});
  }

  getUnreadCount(): Observable<{ count: number }> {
    if (this.useMocks) {
      const count = this.mockNotifications.filter(n => !n.read).length;
      return of({ count }).pipe(delay(100));
    }
    return this.http.get<{ count: number }>(`${this.apiUrl}/unread-count`);
  }
}
