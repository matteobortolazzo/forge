import { Injectable, computed, inject, signal, OnDestroy } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import {
  HumanGate,
  AgentQuestion,
  ResolveGateDto,
  SubmitAnswerDto,
  PendingInputItem,
} from '../../shared/models';
import { PendingInputService } from '../services/pending-input.service';
import { createAsyncState, runAsync } from './store-utils';

@Injectable({ providedIn: 'root' })
export class PendingInputStore implements OnDestroy {
  private readonly pendingInputService = inject(PendingInputService);

  // State
  private readonly _gates = signal<HumanGate[]>([]);
  private readonly _question = signal<AgentQuestion | null>(null);
  private readonly asyncState = createAsyncState();

  // Timer for countdown
  private countdownInterval?: ReturnType<typeof setInterval>;

  // Current timestamp for countdown calculation (updated every second)
  private readonly _now = signal<Date>(new Date());

  // Public readonly signals
  readonly gates = this._gates.asReadonly();
  readonly question = this._question.asReadonly();
  readonly isLoading = this.asyncState.loading.asReadonly();
  readonly errorMessage = this.asyncState.error.asReadonly();

  constructor() {
    // Start countdown timer for question timeout
    this.countdownInterval = setInterval(() => {
      this._now.set(new Date());
    }, 1000);
  }

  ngOnDestroy(): void {
    if (this.countdownInterval) {
      clearInterval(this.countdownInterval);
    }
  }

  // Computed: Total pending count (gates + question if exists)
  readonly pendingCount = computed(() => {
    const gateCount = this._gates().length;
    const questionCount = this._question() ? 1 : 0;
    return gateCount + questionCount;
  });

  // Computed: Unified sorted list (question first, then gates by requestedAt)
  readonly allPendingItems = computed<PendingInputItem[]>(() => {
    const items: PendingInputItem[] = [];

    // Question always first (if exists)
    const question = this._question();
    if (question) {
      items.push({ type: 'question', data: question });
    }

    // Gates sorted by requestedAt (newest first)
    const sortedGates = [...this._gates()].sort(
      (a, b) => new Date(b.requestedAt).getTime() - new Date(a.requestedAt).getTime()
    );

    for (const gate of sortedGates) {
      items.push({ type: 'gate', data: gate });
    }

    return items;
  });

  // Computed: True if question exists (agent blocked)
  readonly hasUrgentInput = computed(() => this._question() !== null);

  // Computed: Seconds until question timeout (or null if no question)
  readonly questionTimeRemaining = computed<number | null>(() => {
    const question = this._question();
    if (!question) return null;

    const now = this._now();
    const timeoutAt = new Date(question.timeoutAt);
    const remainingMs = timeoutAt.getTime() - now.getTime();

    return Math.max(0, Math.floor(remainingMs / 1000));
  });

  /**
   * Load all pending gates and the current question from API.
   */
  async loadAll(): Promise<void> {
    await runAsync(
      this.asyncState,
      async () => {
        const [gates, question] = await Promise.all([
          firstValueFrom(this.pendingInputService.getPendingGates()),
          firstValueFrom(this.pendingInputService.getPendingQuestion()),
        ]);

        // Parse dates for gates
        const parsedGates = gates.map(g => ({
          ...g,
          requestedAt: new Date(g.requestedAt),
          resolvedAt: g.resolvedAt ? new Date(g.resolvedAt) : undefined,
        }));
        this._gates.set(parsedGates);

        // Parse dates for question
        if (question) {
          this._question.set({
            ...question,
            requestedAt: new Date(question.requestedAt),
            timeoutAt: new Date(question.timeoutAt),
            answeredAt: question.answeredAt ? new Date(question.answeredAt) : undefined,
          });
        } else {
          this._question.set(null);
        }
      },
      {},
      'Failed to load pending input'
    );
  }

  /**
   * Resolve a human gate (approve, reject, or skip).
   */
  async resolveGate(id: string, dto: ResolveGateDto): Promise<boolean> {
    const result = await runAsync(
      this.asyncState,
      async () => {
        await firstValueFrom(this.pendingInputService.resolveGate(id, dto));
        // Remove gate from local state
        this._gates.update(gates => gates.filter(g => g.id !== id));
        return true;
      },
      { setLoading: false },
      'Failed to resolve gate'
    );
    return result ?? false;
  }

  /**
   * Submit an answer to the current question.
   */
  async answerQuestion(id: string, dto: SubmitAnswerDto): Promise<boolean> {
    const result = await runAsync(
      this.asyncState,
      async () => {
        await firstValueFrom(this.pendingInputService.answerQuestion(id, dto));
        // Clear the question
        this._question.set(null);
        return true;
      },
      { setLoading: false },
      'Failed to submit answer'
    );
    return result ?? false;
  }

  // SSE Event Handlers

  /**
   * Handle humanGate:requested SSE event.
   * Adds gate to the array if not already present.
   */
  handleGateRequested(gate: HumanGate): void {
    const parsed: HumanGate = {
      ...gate,
      requestedAt: new Date(gate.requestedAt),
      resolvedAt: gate.resolvedAt ? new Date(gate.resolvedAt) : undefined,
    };

    this._gates.update(gates => {
      // Check for duplicates
      if (gates.some(g => g.id === parsed.id)) {
        return gates;
      }
      return [parsed, ...gates];
    });
  }

  /**
   * Handle humanGate:resolved SSE event.
   * Removes the gate from the array.
   */
  handleGateResolved(gate: HumanGate): void {
    this._gates.update(gates => gates.filter(g => g.id !== gate.id));
  }

  /**
   * Handle agentQuestion:requested SSE event.
   * Sets the question signal.
   */
  handleQuestionRequested(question: AgentQuestion): void {
    this._question.set({
      ...question,
      requestedAt: new Date(question.requestedAt),
      timeoutAt: new Date(question.timeoutAt),
      answeredAt: question.answeredAt ? new Date(question.answeredAt) : undefined,
    });
  }

  /**
   * Handle agentQuestion:answered SSE event.
   * Clears the question signal.
   */
  handleQuestionAnswered(_question: AgentQuestion): void {
    this._question.set(null);
  }

  /**
   * Handle agentQuestion:timeout SSE event.
   * Clears the question signal.
   */
  handleQuestionTimeout(_question: AgentQuestion): void {
    this._question.set(null);
  }

  /**
   * Handle agentQuestion:cancelled SSE event.
   * Clears the question if it matches the cancelled ID.
   */
  handleQuestionCancelled(payload: { id: string }): void {
    const current = this._question();
    if (current && current.id === payload.id) {
      this._question.set(null);
    }
  }

  /**
   * Get a gate by ID.
   */
  getGateById(id: string): HumanGate | undefined {
    return this._gates().find(g => g.id === id);
  }

  /**
   * Clear all state.
   */
  clear(): void {
    this._gates.set([]);
    this._question.set(null);
    this.asyncState.error.set(null);
  }
}
