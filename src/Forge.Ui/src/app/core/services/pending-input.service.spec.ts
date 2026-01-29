import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { describe, it, expect, beforeEach, afterEach } from 'vitest';
import { PendingInputService } from './pending-input.service';
import { HumanGate, AgentQuestion, ResolveGateDto, SubmitAnswerDto } from '../../shared/models';

describe('PendingInputService', () => {
  let service: PendingInputService;
  let httpMock: HttpTestingController;

  const mockGate: HumanGate = {
    id: 'gate-1',
    taskId: 'task-1',
    gateType: 'planning',
    status: 'pending',
    confidenceScore: 0.65,
    reason: 'Low confidence in implementation approach',
    requestedAt: new Date('2026-01-29T10:00:00Z'),
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
    TestBed.configureTestingModule({
      providers: [
        PendingInputService,
        provideHttpClient(),
        provideHttpClientTesting(),
      ],
    });

    service = TestBed.inject(PendingInputService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  describe('getPendingGates', () => {
    it('should fetch pending gates', () => {
      service.getPendingGates().subscribe(gates => {
        expect(gates).toHaveLength(1);
        expect(gates[0].id).toBe('gate-1');
      });

      const req = httpMock.expectOne('/api/gates/pending');
      expect(req.request.method).toBe('GET');
      req.flush([mockGate]);
    });

    it('should return empty array when no gates', () => {
      service.getPendingGates().subscribe(gates => {
        expect(gates).toHaveLength(0);
      });

      const req = httpMock.expectOne('/api/gates/pending');
      req.flush([]);
    });
  });

  describe('resolveGate', () => {
    it('should resolve a gate with approved status', () => {
      const dto: ResolveGateDto = {
        status: 'approved',
        resolution: 'Looks good',
      };

      const expectedResponse: HumanGate = {
        ...mockGate,
        status: 'approved',
        resolution: 'Looks good',
        resolvedAt: new Date('2026-01-29T10:01:00Z'),
      };

      service.resolveGate('gate-1', dto).subscribe(gate => {
        expect(gate.status).toBe('approved');
        expect(gate.resolution).toBe('Looks good');
      });

      const req = httpMock.expectOne('/api/gates/gate-1/resolve');
      expect(req.request.method).toBe('POST');
      expect(req.request.body).toEqual(dto);
      req.flush(expectedResponse);
    });

    it('should resolve a gate with rejected status', () => {
      const dto: ResolveGateDto = {
        status: 'rejected',
        resolution: 'Needs more work',
      };

      service.resolveGate('gate-1', dto).subscribe(gate => {
        expect(gate.status).toBe('rejected');
      });

      const req = httpMock.expectOne('/api/gates/gate-1/resolve');
      req.flush({ ...mockGate, status: 'rejected' });
    });

    it('should resolve a gate with skipped status', () => {
      const dto: ResolveGateDto = {
        status: 'skipped',
      };

      service.resolveGate('gate-1', dto).subscribe(gate => {
        expect(gate.status).toBe('skipped');
      });

      const req = httpMock.expectOne('/api/gates/gate-1/resolve');
      req.flush({ ...mockGate, status: 'skipped' });
    });
  });

  describe('getPendingQuestion', () => {
    it('should fetch pending question', () => {
      service.getPendingQuestion().subscribe(question => {
        expect(question).not.toBeNull();
        expect(question!.id).toBe('question-1');
        expect(question!.questions).toHaveLength(1);
      });

      const req = httpMock.expectOne('/api/agent/questions/pending');
      expect(req.request.method).toBe('GET');
      req.flush(mockQuestion);
    });

    it('should handle null response when no pending question', () => {
      service.getPendingQuestion().subscribe(question => {
        expect(question).toBeNull();
      });

      const req = httpMock.expectOne('/api/agent/questions/pending');
      req.flush(null);
    });
  });

  describe('answerQuestion', () => {
    it('should submit an answer', () => {
      const dto: SubmitAnswerDto = {
        answers: [
          {
            questionIndex: 0,
            selectedOptionIndices: [1],
          },
        ],
      };

      const expectedResponse: AgentQuestion = {
        ...mockQuestion,
        status: 'answered',
        answers: dto.answers,
        answeredAt: new Date('2026-01-29T10:02:00Z'),
      };

      service.answerQuestion('question-1', dto).subscribe(question => {
        expect(question.status).toBe('answered');
        expect(question.answers).toHaveLength(1);
        expect(question.answers![0].selectedOptionIndices).toEqual([1]);
      });

      const req = httpMock.expectOne('/api/agent/questions/question-1/answer');
      expect(req.request.method).toBe('POST');
      expect(req.request.body).toEqual(dto);
      req.flush(expectedResponse);
    });

    it('should submit answer with custom answer', () => {
      const dto: SubmitAnswerDto = {
        answers: [
          {
            questionIndex: 0,
            selectedOptionIndices: [],
            customAnswer: 'My custom approach',
          },
        ],
      };

      service.answerQuestion('question-1', dto).subscribe(question => {
        expect(question.answers![0].customAnswer).toBe('My custom approach');
      });

      const req = httpMock.expectOne('/api/agent/questions/question-1/answer');
      req.flush({
        ...mockQuestion,
        status: 'answered',
        answers: dto.answers,
      });
    });
  });
});
