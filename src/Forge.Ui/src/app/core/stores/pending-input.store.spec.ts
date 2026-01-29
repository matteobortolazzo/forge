import { TestBed } from '@angular/core/testing';
import { describe, it, expect, beforeEach, vi, afterEach } from 'vitest';
import { of, throwError } from 'rxjs';
import { PendingInputStore } from './pending-input.store';
import { PendingInputService } from '../services/pending-input.service';
import { HumanGate, AgentQuestion, ResolveGateDto, SubmitAnswerDto } from '../../shared/models';

describe('PendingInputStore', () => {
  let store: PendingInputStore;
  let serviceMock: {
    getPendingGates: ReturnType<typeof vi.fn>;
    resolveGate: ReturnType<typeof vi.fn>;
    getPendingQuestion: ReturnType<typeof vi.fn>;
    answerQuestion: ReturnType<typeof vi.fn>;
  };

  const mockGate: HumanGate = {
    id: 'gate-1',
    taskId: 'task-1',
    gateType: 'planning',
    status: 'pending',
    confidenceScore: 0.65,
    reason: 'Low confidence in implementation approach',
    requestedAt: new Date('2026-01-29T10:00:00Z'),
  };

  const mockGate2: HumanGate = {
    id: 'gate-2',
    backlogItemId: 'backlog-1',
    gateType: 'refining',
    status: 'pending',
    confidenceScore: 0.55,
    reason: 'Needs clarification',
    requestedAt: new Date('2026-01-29T09:00:00Z'),
  };

  const mockQuestion: AgentQuestion = {
    id: 'question-1',
    taskId: 'task-1',
    toolUseId: 'tool-use-123',
    questions: [
      {
        question: 'Which approach do you prefer?',
        header: 'Approach',
        options: [
          { label: 'Option A', description: 'First approach' },
          { label: 'Option B', description: 'Second approach' },
        ],
        multiSelect: false,
      },
    ],
    status: 'pending',
    requestedAt: new Date('2026-01-29T10:00:00Z'),
    timeoutAt: new Date('2026-01-29T10:05:00Z'),
  };

  beforeEach(() => {
    serviceMock = {
      getPendingGates: vi.fn().mockReturnValue(of([])),
      resolveGate: vi.fn().mockReturnValue(of(mockGate)),
      getPendingQuestion: vi.fn().mockReturnValue(of(null)),
      answerQuestion: vi.fn().mockReturnValue(of(mockQuestion)),
    };

    TestBed.configureTestingModule({
      providers: [
        PendingInputStore,
        { provide: PendingInputService, useValue: serviceMock },
      ],
    });

    store = TestBed.inject(PendingInputStore);
  });

  afterEach(() => {
    store.ngOnDestroy();
  });

  describe('initial state', () => {
    it('should have empty gates array', () => {
      expect(store.gates()).toEqual([]);
    });

    it('should have null question', () => {
      expect(store.question()).toBeNull();
    });

    it('should have pendingCount of 0', () => {
      expect(store.pendingCount()).toBe(0);
    });

    it('should not be loading', () => {
      expect(store.isLoading()).toBe(false);
    });

    it('should have no error', () => {
      expect(store.errorMessage()).toBeNull();
    });
  });

  describe('loadAll', () => {
    it('should load gates and question from service', async () => {
      serviceMock.getPendingGates.mockReturnValue(of([mockGate, mockGate2]));
      serviceMock.getPendingQuestion.mockReturnValue(of(mockQuestion));

      await store.loadAll();

      expect(store.gates()).toHaveLength(2);
      expect(store.question()).not.toBeNull();
      expect(store.question()?.id).toBe('question-1');
    });

    it('should handle errors gracefully', async () => {
      serviceMock.getPendingGates.mockReturnValue(throwError(() => new Error('Network error')));

      await store.loadAll();

      expect(store.errorMessage()).toBe('Network error');
    });

    it('should handle null question response', async () => {
      serviceMock.getPendingGates.mockReturnValue(of([mockGate]));
      serviceMock.getPendingQuestion.mockReturnValue(of(null));

      await store.loadAll();

      expect(store.gates()).toHaveLength(1);
      expect(store.question()).toBeNull();
    });
  });

  describe('computed signals', () => {
    it('should compute pendingCount correctly with gates only', async () => {
      serviceMock.getPendingGates.mockReturnValue(of([mockGate, mockGate2]));
      serviceMock.getPendingQuestion.mockReturnValue(of(null));

      await store.loadAll();

      expect(store.pendingCount()).toBe(2);
    });

    it('should compute pendingCount correctly with question only', async () => {
      serviceMock.getPendingGates.mockReturnValue(of([]));
      serviceMock.getPendingQuestion.mockReturnValue(of(mockQuestion));

      await store.loadAll();

      expect(store.pendingCount()).toBe(1);
    });

    it('should compute pendingCount correctly with both', async () => {
      serviceMock.getPendingGates.mockReturnValue(of([mockGate]));
      serviceMock.getPendingQuestion.mockReturnValue(of(mockQuestion));

      await store.loadAll();

      expect(store.pendingCount()).toBe(2);
    });

    it('should compute hasUrgentInput true when question exists', async () => {
      serviceMock.getPendingGates.mockReturnValue(of([]));
      serviceMock.getPendingQuestion.mockReturnValue(of(mockQuestion));

      await store.loadAll();

      expect(store.hasUrgentInput()).toBe(true);
    });

    it('should compute hasUrgentInput false when only gates', async () => {
      serviceMock.getPendingGates.mockReturnValue(of([mockGate]));
      serviceMock.getPendingQuestion.mockReturnValue(of(null));

      await store.loadAll();

      expect(store.hasUrgentInput()).toBe(false);
    });

    it('should sort allPendingItems with question first', async () => {
      serviceMock.getPendingGates.mockReturnValue(of([mockGate, mockGate2]));
      serviceMock.getPendingQuestion.mockReturnValue(of(mockQuestion));

      await store.loadAll();

      const items = store.allPendingItems();
      expect(items).toHaveLength(3);
      expect(items[0].type).toBe('question');
      expect(items[1].type).toBe('gate');
      expect(items[2].type).toBe('gate');
    });

    it('should return null questionTimeRemaining when no question', () => {
      expect(store.questionTimeRemaining()).toBeNull();
    });

    it('should return positive questionTimeRemaining when question exists', async () => {
      const futureQuestion: AgentQuestion = {
        ...mockQuestion,
        timeoutAt: new Date(Date.now() + 120000), // 2 minutes from now
      };
      serviceMock.getPendingQuestion.mockReturnValue(of(futureQuestion));

      await store.loadAll();

      const remaining = store.questionTimeRemaining();
      expect(remaining).not.toBeNull();
      expect(remaining).toBeGreaterThan(0);
    });
  });

  describe('SSE event handlers', () => {
    it('handleGateRequested should add gate to array', () => {
      store.handleGateRequested(mockGate);

      expect(store.gates()).toHaveLength(1);
      expect(store.gates()[0].id).toBe('gate-1');
    });

    it('handleGateRequested should not duplicate existing gate', () => {
      store.handleGateRequested(mockGate);
      store.handleGateRequested(mockGate);

      expect(store.gates()).toHaveLength(1);
    });

    it('handleGateResolved should remove gate from array', () => {
      store.handleGateRequested(mockGate);
      store.handleGateRequested(mockGate2);

      store.handleGateResolved(mockGate);

      expect(store.gates()).toHaveLength(1);
      expect(store.gates()[0].id).toBe('gate-2');
    });

    it('handleQuestionRequested should set question signal', () => {
      store.handleQuestionRequested(mockQuestion);

      expect(store.question()).not.toBeNull();
      expect(store.question()?.id).toBe('question-1');
    });

    it('handleQuestionAnswered should clear question signal', () => {
      store.handleQuestionRequested(mockQuestion);
      expect(store.question()).not.toBeNull();

      store.handleQuestionAnswered(mockQuestion);

      expect(store.question()).toBeNull();
    });

    it('handleQuestionTimeout should clear question signal', () => {
      store.handleQuestionRequested(mockQuestion);
      expect(store.question()).not.toBeNull();

      store.handleQuestionTimeout(mockQuestion);

      expect(store.question()).toBeNull();
    });

    it('handleQuestionCancelled should clear question by id', () => {
      store.handleQuestionRequested(mockQuestion);
      expect(store.question()).not.toBeNull();

      store.handleQuestionCancelled({ id: 'question-1' });

      expect(store.question()).toBeNull();
    });

    it('handleQuestionCancelled should not clear question with different id', () => {
      store.handleQuestionRequested(mockQuestion);
      expect(store.question()).not.toBeNull();

      store.handleQuestionCancelled({ id: 'question-other' });

      expect(store.question()).not.toBeNull();
    });
  });

  describe('actions', () => {
    it('resolveGate should call service and remove from array', async () => {
      store.handleGateRequested(mockGate);
      expect(store.gates()).toHaveLength(1);

      const dto: ResolveGateDto = { status: 'approved' };
      const result = await store.resolveGate('gate-1', dto);

      expect(result).toBe(true);
      expect(serviceMock.resolveGate).toHaveBeenCalledWith('gate-1', dto);
      expect(store.gates()).toHaveLength(0);
    });

    it('resolveGate should return false on error', async () => {
      store.handleGateRequested(mockGate);
      serviceMock.resolveGate.mockReturnValue(throwError(() => new Error('Failed')));

      const result = await store.resolveGate('gate-1', { status: 'approved' });

      expect(result).toBe(false);
    });

    it('answerQuestion should call service and clear question', async () => {
      store.handleQuestionRequested(mockQuestion);
      expect(store.question()).not.toBeNull();

      const dto: SubmitAnswerDto = {
        answers: [{ questionIndex: 0, selectedOptionIndices: [0] }],
      };
      const result = await store.answerQuestion('question-1', dto);

      expect(result).toBe(true);
      expect(serviceMock.answerQuestion).toHaveBeenCalledWith('question-1', dto);
      expect(store.question()).toBeNull();
    });

    it('answerQuestion should return false on error', async () => {
      store.handleQuestionRequested(mockQuestion);
      serviceMock.answerQuestion.mockReturnValue(throwError(() => new Error('Failed')));

      const result = await store.answerQuestion('question-1', {
        answers: [{ questionIndex: 0, selectedOptionIndices: [0] }],
      });

      expect(result).toBe(false);
    });
  });

  describe('helper methods', () => {
    it('getGateById should return gate if exists', () => {
      store.handleGateRequested(mockGate);

      expect(store.getGateById('gate-1')).toBeDefined();
      expect(store.getGateById('gate-1')?.id).toBe('gate-1');
    });

    it('getGateById should return undefined if not exists', () => {
      expect(store.getGateById('nonexistent')).toBeUndefined();
    });

    it('clear should reset all state', async () => {
      serviceMock.getPendingGates.mockReturnValue(of([mockGate]));
      serviceMock.getPendingQuestion.mockReturnValue(of(mockQuestion));
      await store.loadAll();

      store.clear();

      expect(store.gates()).toHaveLength(0);
      expect(store.question()).toBeNull();
      expect(store.errorMessage()).toBeNull();
    });
  });
});
