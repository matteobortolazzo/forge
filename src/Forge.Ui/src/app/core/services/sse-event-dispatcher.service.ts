import { Injectable, inject, OnDestroy } from '@angular/core';
import { Subscription } from 'rxjs';
import { SseService } from './sse.service';
import { TaskStore } from '../stores/task.store';
import { LogStore } from '../stores/log.store';
import { NotificationStore } from '../stores/notification.store';
import { SchedulerStore } from '../stores/scheduler.store';
import { RepositoryStore } from '../stores/repository.store';
import { ArtifactStore } from '../stores/artifact.store';
import {
  ServerEvent,
  Task,
  TaskLog,
  Notification,
  AgentStatus,
  TaskSplitPayload,
  ChildAddedPayload,
  Repository,
  Artifact,
} from '../../shared/models';

/**
 * Centralized SSE event dispatcher.
 *
 * This service manages the SSE connection and dispatches events to the appropriate stores.
 * It handles:
 * - Connection lifecycle (connect/disconnect)
 * - Automatic reconnection with exponential backoff
 * - Event routing to stores
 *
 * Usage:
 * - Inject this service and call `connect()` in your root component's ngOnInit
 * - Call `disconnect()` in ngOnDestroy
 * - Only one connection should be active at a time (singleton service)
 */
@Injectable({ providedIn: 'root' })
export class SseEventDispatcher implements OnDestroy {
  private readonly sseService = inject(SseService);
  private readonly taskStore = inject(TaskStore);
  private readonly logStore = inject(LogStore);
  private readonly notificationStore = inject(NotificationStore);
  private readonly schedulerStore = inject(SchedulerStore);
  private readonly repositoryStore = inject(RepositoryStore);
  private readonly artifactStore = inject(ArtifactStore);

  private subscription?: Subscription;
  private reconnectAttempts = 0;
  private reconnectTimeoutId?: ReturnType<typeof setTimeout>;
  private isConnected = false;

  /** Maximum reconnection attempts before giving up */
  private readonly maxReconnectAttempts = 10;
  /** Base delay for exponential backoff (ms) */
  private readonly baseReconnectDelay = 1000;
  /** Maximum delay between reconnection attempts (ms) */
  private readonly maxReconnectDelay = 30000;

  /**
   * Connects to SSE and starts dispatching events to stores.
   * Safe to call multiple times - will not create duplicate connections.
   */
  connect(): void {
    if (this.isConnected) {
      return;
    }

    this.isConnected = true;
    this.reconnectAttempts = 0;
    this.doConnect();
  }

  /**
   * Disconnects from SSE and stops event dispatching.
   */
  disconnect(): void {
    this.isConnected = false;
    this.clearReconnectTimeout();
    this.subscription?.unsubscribe();
    this.subscription = undefined;
    this.sseService.disconnect();
  }

  ngOnDestroy(): void {
    this.disconnect();
  }

  private doConnect(): void {
    this.subscription?.unsubscribe();

    this.subscription = this.sseService.connect().subscribe({
      next: (event: ServerEvent) => {
        // Reset reconnect attempts on successful message
        this.reconnectAttempts = 0;
        this.handleEvent(event);
      },
      error: (err) => {
        console.error('SSE connection error:', err);
        this.scheduleReconnect();
      },
      complete: () => {
        // Connection closed unexpectedly
        if (this.isConnected) {
          this.scheduleReconnect();
        }
      },
    });
  }

  private scheduleReconnect(): void {
    if (!this.isConnected) {
      return;
    }

    if (this.reconnectAttempts >= this.maxReconnectAttempts) {
      console.error(`SSE: Max reconnection attempts (${this.maxReconnectAttempts}) reached. Giving up.`);
      return;
    }

    const delay = Math.min(
      this.baseReconnectDelay * Math.pow(2, this.reconnectAttempts),
      this.maxReconnectDelay
    );

    this.reconnectAttempts++;
    console.log(`SSE: Reconnecting in ${delay}ms (attempt ${this.reconnectAttempts}/${this.maxReconnectAttempts})`);

    this.clearReconnectTimeout();
    this.reconnectTimeoutId = setTimeout(() => {
      if (this.isConnected) {
        this.doConnect();
      }
    }, delay);
  }

  private clearReconnectTimeout(): void {
    if (this.reconnectTimeoutId !== undefined) {
      clearTimeout(this.reconnectTimeoutId);
      this.reconnectTimeoutId = undefined;
    }
  }

  private handleEvent(event: ServerEvent): void {
    switch (event.type) {
      // Task events
      case 'task:created':
      case 'task:updated':
      case 'task:paused':
      case 'task:resumed':
        this.taskStore.updateTaskFromEvent(event.payload as Task);
        break;

      case 'task:deleted':
        this.taskStore.removeTaskFromEvent((event.payload as { id: string }).id);
        break;

      case 'task:log':
        this.logStore.addLog(event.payload as TaskLog);
        break;

      case 'task:split': {
        const splitPayload = event.payload as TaskSplitPayload;
        this.taskStore.handleTaskSplitEvent(splitPayload.parent, splitPayload.children);
        break;
      }

      case 'task:childAdded': {
        const childPayload = event.payload as ChildAddedPayload;
        this.taskStore.handleChildAddedEvent(childPayload.parentId, childPayload.child);
        break;
      }

      // Agent/Scheduler events
      case 'agent:statusChanged': {
        const agentStatus = event.payload as AgentStatus;
        this.schedulerStore.updateAgentStatus(agentStatus.isRunning, agentStatus.currentTaskId);
        break;
      }

      case 'scheduler:taskScheduled':
        this.taskStore.updateTaskFromEvent(event.payload as Task);
        this.schedulerStore.updateFromScheduledEvent((event.payload as Task).id);
        break;

      // Notification events
      case 'notification:new':
        this.notificationStore.addNotificationFromEvent(event.payload as Notification);
        break;

      // Repository events
      case 'repository:created':
      case 'repository:updated':
        this.repositoryStore.updateRepositoryFromEvent(event.payload as Repository);
        break;

      case 'repository:deleted':
        this.repositoryStore.removeRepositoryFromEvent((event.payload as { id: string }).id);
        break;

      // Artifact events
      case 'artifact:created':
        this.artifactStore.addArtifactFromEvent(event.payload as Artifact);
        break;

      default:
        // Log unknown events for debugging
        console.warn('Unknown SSE event type:', event.type);
    }
  }
}
