import { Component, ChangeDetectionStrategy, input, output, signal, computed, inject, OnInit, OnDestroy } from '@angular/core';
import { AgentQuestion, QuestionAnswer, SubmitAnswerDto } from '../../shared/models';
import { PendingInputStore } from '../../core/stores/pending-input.store';

/**
 * Component for answering agent questions with countdown timer.
 * Supports single-select (radio) and multi-select (checkbox) questions.
 */
@Component({
  selector: 'app-question-answer',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="rounded-lg border-2 border-amber-400 bg-amber-50 p-4 dark:border-amber-600 dark:bg-amber-900/20">
      <!-- Header with countdown timer -->
      <div class="flex items-center justify-between">
        <span class="text-sm font-semibold text-amber-800 dark:text-amber-300">
          Agent Question
        </span>
        <div [class]="getTimerClasses()">
          <svg class="h-4 w-4" viewBox="0 0 20 20" fill="currentColor" aria-hidden="true">
            <path fill-rule="evenodd" d="M10 18a8 8 0 100-16 8 8 0 000 16zm.75-13a.75.75 0 00-1.5 0v5c0 .414.336.75.75.75h4a.75.75 0 000-1.5h-3.25V5z" clip-rule="evenodd" />
          </svg>
          <span>{{ formatTimeRemaining() }}</span>
        </div>
      </div>

      <!-- Questions -->
      <div class="mt-3 space-y-4">
        @for (q of question().questions; track $index; let qIdx = $index) {
          <div class="rounded-md bg-white p-3 shadow-sm dark:bg-gray-800">
            <!-- Question header chip -->
            <div class="flex items-center gap-2">
              <span class="inline-flex items-center rounded-full bg-gray-100 px-2 py-0.5 text-xs font-medium text-gray-600 dark:bg-gray-700 dark:text-gray-300">
                {{ q.header }}
              </span>
              @if (q.multiSelect) {
                <span class="text-xs text-gray-500 dark:text-gray-400">(select multiple)</span>
              }
            </div>

            <!-- Question text -->
            <p class="mt-2 text-sm font-medium text-gray-900 dark:text-gray-100">
              {{ q.question }}
            </p>

            <!-- Options -->
            <div class="mt-3 space-y-2">
              @for (opt of q.options; track $index; let optIdx = $index) {
                <label
                  class="flex cursor-pointer items-start gap-3 rounded-md border border-gray-200 p-2 hover:bg-gray-50 dark:border-gray-600 dark:hover:bg-gray-700"
                  [class.border-blue-500]="isOptionSelected(qIdx, optIdx)"
                  [class.bg-blue-50]="isOptionSelected(qIdx, optIdx)"
                  [class.dark:bg-blue-900/20]="isOptionSelected(qIdx, optIdx)"
                >
                  @if (q.multiSelect) {
                    <input
                      type="checkbox"
                      class="mt-0.5 h-4 w-4 rounded border-gray-300 text-blue-600 focus:ring-blue-500"
                      [checked]="isOptionSelected(qIdx, optIdx)"
                      (change)="toggleOption(qIdx, optIdx)"
                    />
                  } @else {
                    <input
                      type="radio"
                      class="mt-0.5 h-4 w-4 border-gray-300 text-blue-600 focus:ring-blue-500"
                      [name]="'question-' + qIdx"
                      [checked]="isOptionSelected(qIdx, optIdx)"
                      (change)="selectSingleOption(qIdx, optIdx)"
                    />
                  }
                  <div class="flex-1">
                    <span class="text-sm font-medium text-gray-900 dark:text-gray-100">
                      {{ opt.label }}
                    </span>
                    @if (opt.description) {
                      <p class="mt-0.5 text-xs text-gray-500 dark:text-gray-400">
                        {{ opt.description }}
                      </p>
                    }
                  </div>
                </label>
              }

              <!-- Other (custom answer) option -->
              <label
                class="flex cursor-pointer items-start gap-3 rounded-md border border-gray-200 p-2 hover:bg-gray-50 dark:border-gray-600 dark:hover:bg-gray-700"
                [class.border-blue-500]="isOtherSelected(qIdx)"
                [class.bg-blue-50]="isOtherSelected(qIdx)"
                [class.dark:bg-blue-900/20]="isOtherSelected(qIdx)"
              >
                @if (q.multiSelect) {
                  <input
                    type="checkbox"
                    class="mt-0.5 h-4 w-4 rounded border-gray-300 text-blue-600 focus:ring-blue-500"
                    [checked]="isOtherSelected(qIdx)"
                    (change)="toggleOther(qIdx)"
                  />
                } @else {
                  <input
                    type="radio"
                    class="mt-0.5 h-4 w-4 border-gray-300 text-blue-600 focus:ring-blue-500"
                    [name]="'question-' + qIdx"
                    [checked]="isOtherSelected(qIdx)"
                    (change)="selectOther(qIdx)"
                  />
                }
                <div class="flex-1">
                  <span class="text-sm font-medium text-gray-900 dark:text-gray-100">
                    Other
                  </span>
                  @if (isOtherSelected(qIdx)) {
                    <input
                      type="text"
                      class="mt-2 w-full rounded-md border border-gray-300 px-2 py-1 text-sm focus:border-blue-500 focus:outline-none focus:ring-1 focus:ring-blue-500 dark:border-gray-600 dark:bg-gray-700 dark:text-gray-200"
                      placeholder="Enter your answer..."
                      [value]="getCustomAnswer(qIdx)"
                      (input)="onCustomAnswerInput(qIdx, $event)"
                    />
                  }
                </div>
              </label>
            </div>
          </div>
        }
      </div>

      <!-- Submit button -->
      <div class="mt-4 flex justify-end">
        <button
          type="button"
          class="inline-flex items-center rounded-md bg-blue-600 px-4 py-2 text-sm font-medium text-white hover:bg-blue-700 focus:outline-none focus:ring-2 focus:ring-blue-500 focus:ring-offset-2 disabled:cursor-not-allowed disabled:opacity-50"
          [disabled]="!canSubmit() || isSubmitting()"
          (click)="onSubmit()"
        >
          @if (isSubmitting()) {
            <svg class="mr-2 h-4 w-4 animate-spin" viewBox="0 0 20 20" fill="currentColor">
              <path fill-rule="evenodd" d="M15.312 11.424a5.5 5.5 0 01-9.201 2.466l-.312-.311h2.433a.75.75 0 000-1.5H3.989a.75.75 0 00-.75.75v4.242a.75.75 0 001.5 0v-2.43l.31.31a7 7 0 0011.712-3.138.75.75 0 00-1.449-.39zm1.23-3.723a.75.75 0 00.219-.53V2.929a.75.75 0 00-1.5 0V5.36l-.31-.31A7 7 0 003.239 8.188a.75.75 0 101.448.389A5.5 5.5 0 0113.89 6.11l.311.31h-2.432a.75.75 0 000 1.5h4.243a.75.75 0 00.53-.219z" clip-rule="evenodd" />
            </svg>
            Submitting...
          } @else {
            Submit Answer
          }
        </button>
      </div>
    </div>
  `,
})
export class QuestionAnswerComponent implements OnInit {
  readonly question = input.required<AgentQuestion>();
  readonly answered = output<SubmitAnswerDto>();

  private readonly pendingInputStore = inject(PendingInputStore);

  readonly isSubmitting = signal(false);

  // Track answers for each question: { [questionIndex]: { selectedIndices: number[], customAnswer?: string, useOther: boolean } }
  private readonly _answers = signal<Map<number, { selectedIndices: number[]; customAnswer?: string; useOther: boolean }>>(new Map());

  ngOnInit(): void {
    // Initialize empty answers for each question
    const answers = new Map<number, { selectedIndices: number[]; customAnswer?: string; useOther: boolean }>();
    this.question().questions.forEach((_, idx) => {
      answers.set(idx, { selectedIndices: [], useOther: false });
    });
    this._answers.set(answers);
  }

  // Computed: time remaining in seconds
  readonly timeRemaining = computed(() => this.pendingInputStore.questionTimeRemaining());

  // Computed: Can submit (all questions have at least one answer)
  readonly canSubmit = computed(() => {
    const answers = this._answers();
    const questions = this.question().questions;

    for (let i = 0; i < questions.length; i++) {
      const answer = answers.get(i);
      if (!answer) return false;

      const hasSelection = answer.selectedIndices.length > 0;
      const hasOtherAnswer = answer.useOther && answer.customAnswer && answer.customAnswer.trim().length > 0;

      if (!hasSelection && !hasOtherAnswer) return false;
    }

    return true;
  });

  formatTimeRemaining(): string {
    const seconds = this.timeRemaining();
    if (seconds === null) return '--:--';

    const mins = Math.floor(seconds / 60);
    const secs = seconds % 60;
    return `${mins}:${secs.toString().padStart(2, '0')}`;
  }

  getTimerClasses(): string {
    const base = 'flex items-center gap-1 text-sm font-medium';
    const seconds = this.timeRemaining();

    if (seconds === null) return `${base} text-gray-500`;
    if (seconds < 30) return `${base} text-red-600 animate-pulse`;
    if (seconds < 120) return `${base} text-amber-600`;
    return `${base} text-green-600`;
  }

  isOptionSelected(questionIdx: number, optionIdx: number): boolean {
    const answer = this._answers().get(questionIdx);
    return answer?.selectedIndices.includes(optionIdx) ?? false;
  }

  isOtherSelected(questionIdx: number): boolean {
    const answer = this._answers().get(questionIdx);
    return answer?.useOther ?? false;
  }

  getCustomAnswer(questionIdx: number): string {
    const answer = this._answers().get(questionIdx);
    return answer?.customAnswer ?? '';
  }

  selectSingleOption(questionIdx: number, optionIdx: number): void {
    this._answers.update(answers => {
      const newAnswers = new Map(answers);
      newAnswers.set(questionIdx, {
        selectedIndices: [optionIdx],
        useOther: false,
        customAnswer: undefined,
      });
      return newAnswers;
    });
  }

  toggleOption(questionIdx: number, optionIdx: number): void {
    this._answers.update(answers => {
      const newAnswers = new Map(answers);
      const current = newAnswers.get(questionIdx) ?? { selectedIndices: [], useOther: false };
      const indices = [...current.selectedIndices];

      const existingIdx = indices.indexOf(optionIdx);
      if (existingIdx >= 0) {
        indices.splice(existingIdx, 1);
      } else {
        indices.push(optionIdx);
      }

      newAnswers.set(questionIdx, { ...current, selectedIndices: indices });
      return newAnswers;
    });
  }

  selectOther(questionIdx: number): void {
    this._answers.update(answers => {
      const newAnswers = new Map(answers);
      newAnswers.set(questionIdx, {
        selectedIndices: [],
        useOther: true,
        customAnswer: '',
      });
      return newAnswers;
    });
  }

  toggleOther(questionIdx: number): void {
    this._answers.update(answers => {
      const newAnswers = new Map(answers);
      const current = newAnswers.get(questionIdx) ?? { selectedIndices: [], useOther: false };

      if (current.useOther) {
        // Unselect other
        newAnswers.set(questionIdx, { ...current, useOther: false, customAnswer: undefined });
      } else {
        // Select other
        newAnswers.set(questionIdx, { ...current, useOther: true, customAnswer: '' });
      }
      return newAnswers;
    });
  }

  onCustomAnswerInput(questionIdx: number, event: Event): void {
    const target = event.target as HTMLInputElement;
    this._answers.update(answers => {
      const newAnswers = new Map(answers);
      const current = newAnswers.get(questionIdx) ?? { selectedIndices: [], useOther: true };
      newAnswers.set(questionIdx, { ...current, customAnswer: target.value });
      return newAnswers;
    });
  }

  onSubmit(): void {
    if (!this.canSubmit()) return;

    this.isSubmitting.set(true);

    const answers: QuestionAnswer[] = [];
    const answerMap = this._answers();

    for (let i = 0; i < this.question().questions.length; i++) {
      const answer = answerMap.get(i);
      if (answer) {
        answers.push({
          questionIndex: i,
          selectedOptionIndices: answer.selectedIndices,
          customAnswer: answer.useOther && answer.customAnswer ? answer.customAnswer : undefined,
        });
      }
    }

    const dto: SubmitAnswerDto = { answers };
    this.answered.emit(dto);
  }
}
