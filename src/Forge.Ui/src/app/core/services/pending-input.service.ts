import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, of, delay } from 'rxjs';
import {
  HumanGate,
  AgentQuestion,
  ResolveGateDto,
  SubmitAnswerDto,
} from '../../shared/models';

/**
 * Mock data for testing
 */
const MOCK_GATES: HumanGate[] = [];
let mockPendingQuestion: AgentQuestion | null = null;

@Injectable({ providedIn: 'root' })
export class PendingInputService {
  private readonly http = inject(HttpClient);
  private readonly useMocks = false;

  /**
   * Get all pending human gates across all repositories.
   */
  getPendingGates(): Observable<HumanGate[]> {
    if (this.useMocks) {
      return of([...MOCK_GATES]).pipe(delay(200));
    }
    return this.http.get<HumanGate[]>('/api/gates/pending');
  }

  /**
   * Resolve a human gate with approval, rejection, or skip.
   */
  resolveGate(id: string, dto: ResolveGateDto): Observable<HumanGate> {
    if (this.useMocks) {
      const gate = MOCK_GATES.find(g => g.id === id);
      if (gate) {
        gate.status = dto.status;
        gate.resolution = dto.resolution;
        gate.resolvedBy = dto.resolvedBy;
        gate.resolvedAt = new Date();
      }
      return of(gate!).pipe(delay(200));
    }
    return this.http.post<HumanGate>(`/api/gates/${id}/resolve`, dto);
  }

  /**
   * Get the current pending agent question (if any).
   * Returns null if no question is pending.
   */
  getPendingQuestion(): Observable<AgentQuestion | null> {
    if (this.useMocks) {
      return of(mockPendingQuestion).pipe(delay(200));
    }
    return this.http.get<AgentQuestion | null>('/api/agent/questions/pending');
  }

  /**
   * Submit an answer to an agent question.
   */
  answerQuestion(id: string, dto: SubmitAnswerDto): Observable<AgentQuestion> {
    if (this.useMocks) {
      if (mockPendingQuestion && mockPendingQuestion.id === id) {
        mockPendingQuestion = {
          ...mockPendingQuestion,
          status: 'answered',
          answers: dto.answers,
          answeredAt: new Date(),
        };
        const result = mockPendingQuestion;
        mockPendingQuestion = null;
        return of(result).pipe(delay(200));
      }
      throw new Error('Question not found');
    }
    return this.http.post<AgentQuestion>(`/api/agent/questions/${id}/answer`, dto);
  }
}
