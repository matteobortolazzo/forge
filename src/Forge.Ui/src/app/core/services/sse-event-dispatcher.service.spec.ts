import { TestBed } from '@angular/core/testing';
import { describe, it, expect, beforeEach, vi } from 'vitest';
import { Subject } from 'rxjs';
import { SseEventDispatcher } from './sse-event-dispatcher.service';
import { SseService } from './sse.service';
import { TaskStore } from '../stores/task.store';
import { BacklogStore } from '../stores/backlog.store';
import { LogStore } from '../stores/log.store';
import { NotificationStore } from '../stores/notification.store';
import { SchedulerStore } from '../stores/scheduler.store';
import { RepositoryStore } from '../stores/repository.store';
import { ArtifactStore } from '../stores/artifact.store';
import { PendingInputStore } from '../stores/pending-input.store';
import { ServerEvent, HumanGate, AgentQuestion } from '../../shared/models';

describe('SseEventDispatcher', () => {
  let dispatcher: SseEventDispatcher;
  let sseServiceMock: { connect: ReturnType<typeof vi.fn>; disconnect: ReturnType<typeof vi.fn> };
  let pendingInputStoreMock: {
    handleGateRequested: ReturnType<typeof vi.fn>;
    handleGateResolved: ReturnType<typeof vi.fn>;
    handleQuestionRequested: ReturnType<typeof vi.fn>;
    handleQuestionAnswered: ReturnType<typeof vi.fn>;
    handleQuestionTimeout: ReturnType<typeof vi.fn>;
    handleQuestionCancelled: ReturnType<typeof vi.fn>;
  };
  let eventSubject: Subject<ServerEvent>;

  const mockGate: HumanGate = {
    id: 'gate-1',
    taskId: 'task-1',
    gateType: 'planning',
    status: 'pending',
    confidenceScore: 0.65,
    reason: 'Low confidence',
    requestedAt: new Date(),
  };

  const mockQuestion: AgentQuestion = {
    id: 'question-1',
    taskId: 'task-1',
    toolUseId: 'tool-use-123',
    questions: [],
    status: 'pending',
    requestedAt: new Date(),
    timeoutAt: new Date(),
  };

  beforeEach(() => {
    eventSubject = new Subject<ServerEvent>();

    sseServiceMock = {
      connect: vi.fn().mockReturnValue(eventSubject.asObservable()),
      disconnect: vi.fn(),
    };

    pendingInputStoreMock = {
      handleGateRequested: vi.fn(),
      handleGateResolved: vi.fn(),
      handleQuestionRequested: vi.fn(),
      handleQuestionAnswered: vi.fn(),
      handleQuestionTimeout: vi.fn(),
      handleQuestionCancelled: vi.fn(),
    };

    // Create minimal mocks for other stores
    const taskStoreMock = { updateTaskFromEvent: vi.fn(), removeTaskFromEvent: vi.fn() };
    const backlogStoreMock = { updateItemFromEvent: vi.fn(), removeItemFromEvent: vi.fn() };
    const logStoreMock = { addLog: vi.fn() };
    const notificationStoreMock = { addNotificationFromEvent: vi.fn() };
    const schedulerStoreMock = { updateAgentStatus: vi.fn(), updateFromScheduledEvent: vi.fn() };
    const repositoryStoreMock = { updateRepositoryFromEvent: vi.fn(), removeRepositoryFromEvent: vi.fn() };
    const artifactStoreMock = { addArtifactFromEvent: vi.fn() };

    TestBed.configureTestingModule({
      providers: [
        SseEventDispatcher,
        { provide: SseService, useValue: sseServiceMock },
        { provide: TaskStore, useValue: taskStoreMock },
        { provide: BacklogStore, useValue: backlogStoreMock },
        { provide: LogStore, useValue: logStoreMock },
        { provide: NotificationStore, useValue: notificationStoreMock },
        { provide: SchedulerStore, useValue: schedulerStoreMock },
        { provide: RepositoryStore, useValue: repositoryStoreMock },
        { provide: ArtifactStore, useValue: artifactStoreMock },
        { provide: PendingInputStore, useValue: pendingInputStoreMock },
      ],
    });

    dispatcher = TestBed.inject(SseEventDispatcher);
  });

  describe('pending input events', () => {
    beforeEach(() => {
      dispatcher.connect();
    });

    it('should route humanGate:requested to store.handleGateRequested', () => {
      const event: ServerEvent = {
        type: 'humanGate:requested',
        payload: mockGate,
        timestamp: new Date(),
      };

      eventSubject.next(event);

      expect(pendingInputStoreMock.handleGateRequested).toHaveBeenCalledWith(mockGate);
    });

    it('should route humanGate:resolved to store.handleGateResolved', () => {
      const resolvedGate = { ...mockGate, status: 'approved' as const };
      const event: ServerEvent = {
        type: 'humanGate:resolved',
        payload: resolvedGate,
        timestamp: new Date(),
      };

      eventSubject.next(event);

      expect(pendingInputStoreMock.handleGateResolved).toHaveBeenCalledWith(resolvedGate);
    });

    it('should route agentQuestion:requested to store.handleQuestionRequested', () => {
      const event: ServerEvent = {
        type: 'agentQuestion:requested',
        payload: mockQuestion,
        timestamp: new Date(),
      };

      eventSubject.next(event);

      expect(pendingInputStoreMock.handleQuestionRequested).toHaveBeenCalledWith(mockQuestion);
    });

    it('should route agentQuestion:answered to store.handleQuestionAnswered', () => {
      const answeredQuestion = { ...mockQuestion, status: 'answered' as const };
      const event: ServerEvent = {
        type: 'agentQuestion:answered',
        payload: answeredQuestion,
        timestamp: new Date(),
      };

      eventSubject.next(event);

      expect(pendingInputStoreMock.handleQuestionAnswered).toHaveBeenCalledWith(answeredQuestion);
    });

    it('should route agentQuestion:timeout to store.handleQuestionTimeout', () => {
      const timedOutQuestion = { ...mockQuestion, status: 'timeout' as const };
      const event: ServerEvent = {
        type: 'agentQuestion:timeout',
        payload: timedOutQuestion,
        timestamp: new Date(),
      };

      eventSubject.next(event);

      expect(pendingInputStoreMock.handleQuestionTimeout).toHaveBeenCalledWith(timedOutQuestion);
    });

    it('should route agentQuestion:cancelled to store.handleQuestionCancelled', () => {
      const cancelledPayload = { id: 'question-1' };
      const event: ServerEvent = {
        type: 'agentQuestion:cancelled',
        payload: cancelledPayload,
        timestamp: new Date(),
      };

      eventSubject.next(event);

      expect(pendingInputStoreMock.handleQuestionCancelled).toHaveBeenCalledWith(cancelledPayload);
    });
  });

  describe('connection lifecycle', () => {
    it('should connect to SSE service', () => {
      dispatcher.connect();

      expect(sseServiceMock.connect).toHaveBeenCalled();
    });

    it('should not create duplicate connections', () => {
      dispatcher.connect();
      dispatcher.connect();

      expect(sseServiceMock.connect).toHaveBeenCalledTimes(1);
    });

    it('should disconnect from SSE service', () => {
      dispatcher.connect();
      dispatcher.disconnect();

      expect(sseServiceMock.disconnect).toHaveBeenCalled();
    });
  });
});
