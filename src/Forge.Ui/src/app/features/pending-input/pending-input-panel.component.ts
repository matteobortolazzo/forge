import { Component, ChangeDetectionStrategy, inject, signal } from '@angular/core';
import { PendingInputStore } from '../../core/stores/pending-input.store';
import { HumanGate, ResolveGateDto, SubmitAnswerDto } from '../../shared/models';
import { GateResolutionComponent } from './gate-resolution.component';
import { QuestionAnswerComponent } from './question-answer.component';
import { formatRelativeTime } from '../../shared/utils/date-utils';

/**
 * Dropdown panel for pending human input (gates and questions).
 * Follows the notification-panel pattern.
 */
@Component({
  selector: 'app-pending-input-panel',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [GateResolutionComponent, QuestionAnswerComponent],
  template: `
    <div class="relative">
      <!-- Badge Button -->
      <button
        type="button"
        [class]="getBadgeButtonClasses()"
        (click)="togglePanel()"
        [attr.aria-expanded]="isPanelOpen()"
        aria-haspopup="true"
        aria-label="Pending input"
      >
        <!-- Hand/Input icon -->
        <svg
          class="h-6 w-6"
          xmlns="http://www.w3.org/2000/svg"
          fill="none"
          viewBox="0 0 24 24"
          stroke-width="1.5"
          stroke="currentColor"
          aria-hidden="true"
        >
          <path
            stroke-linecap="round"
            stroke-linejoin="round"
            d="M10.05 4.575a1.575 1.575 0 10-3.15 0v3m3.15-3v-1.5a1.575 1.575 0 013.15 0v1.5m-3.15 0l.075 5.925m3.075.75V4.575m0 0a1.575 1.575 0 013.15 0V15M6.9 7.575a1.575 1.575 0 10-3.15 0v8.175a6.75 6.75 0 006.75 6.75h2.018a5.25 5.25 0 003.712-1.538l1.732-1.732a5.25 5.25 0 001.538-3.712l.003-2.024a.668.668 0 01.198-.471 1.575 1.575 0 10-2.228-2.228 3.818 3.818 0 00-1.12 2.687M6.9 7.575V12m6.27 4.318A4.49 4.49 0 0116.35 15"
          />
        </svg>

        <!-- Badge count -->
        @if (pendingInputStore.pendingCount() > 0) {
          <span
            [class]="getBadgeClasses()"
            [attr.aria-label]="pendingInputStore.pendingCount() + ' pending items'"
          >
            {{ pendingInputStore.pendingCount() > 99 ? '99+' : pendingInputStore.pendingCount() }}
          </span>
        }
      </button>

      <!-- Dropdown Panel -->
      @if (isPanelOpen()) {
        <div
          class="absolute right-0 z-50 mt-2 w-96 origin-top-right rounded-lg bg-white shadow-lg ring-1 ring-black ring-opacity-5 dark:bg-gray-800 dark:ring-gray-700"
          role="menu"
          aria-orientation="vertical"
        >
          <!-- Header -->
          <div class="flex items-center justify-between border-b border-gray-200 px-4 py-3 dark:border-gray-700">
            <h3 class="text-sm font-semibold text-gray-900 dark:text-gray-100">
              Pending Input
            </h3>
            @if (pendingInputStore.hasUrgentInput()) {
              <span class="inline-flex items-center gap-1 text-xs font-medium text-red-600 dark:text-red-400">
                <span class="relative flex h-2 w-2">
                  <span class="absolute inline-flex h-full w-full animate-ping rounded-full bg-red-400 opacity-75"></span>
                  <span class="relative inline-flex h-2 w-2 rounded-full bg-red-500"></span>
                </span>
                Agent waiting
              </span>
            }
          </div>

          <!-- Content -->
          <div class="max-h-[32rem] overflow-y-auto">
            @if (pendingInputStore.pendingCount() === 0) {
              <!-- Empty state -->
              <div class="px-4 py-8 text-center text-sm text-gray-500 dark:text-gray-400">
                No pending input required
              </div>
            } @else {
              <!-- Question section (if exists) -->
              @if (pendingInputStore.question(); as question) {
                <div class="border-b border-gray-200 p-4 dark:border-gray-700">
                  <app-question-answer
                    [question]="question"
                    (answered)="onQuestionAnswered(question.id, $event)"
                  />
                </div>
              }

              <!-- Gates section -->
              @if (pendingInputStore.gates().length > 0) {
                <div class="p-4">
                  <h4 class="mb-3 text-xs font-medium uppercase tracking-wide text-gray-500 dark:text-gray-400">
                    Human Gates ({{ pendingInputStore.gates().length }})
                  </h4>
                  <div class="space-y-3">
                    @for (gate of pendingInputStore.gates(); track gate.id) {
                      <div>
                        <div class="mb-1 text-xs text-gray-500 dark:text-gray-400">
                          {{ formatGateTime(gate.requestedAt) }}
                        </div>
                        <app-gate-resolution
                          [gate]="gate"
                          (resolved)="onGateResolved(gate.id, $event)"
                        />
                      </div>
                    }
                  </div>
                </div>
              }
            }
          </div>
        </div>
      }
    </div>
  `,
  host: {
    '(document:click)': 'onDocumentClick($event)',
    '(document:keydown.escape)': 'closePanel()',
  },
})
export class PendingInputPanelComponent {
  protected readonly pendingInputStore = inject(PendingInputStore);

  readonly isPanelOpen = signal(false);

  togglePanel(): void {
    this.isPanelOpen.update(open => !open);
  }

  closePanel(): void {
    this.isPanelOpen.set(false);
  }

  onDocumentClick(event: MouseEvent): void {
    const target = event.target as HTMLElement;
    if (!target.closest('app-pending-input-panel')) {
      this.closePanel();
    }
  }

  async onGateResolved(id: string, dto: ResolveGateDto): Promise<void> {
    await this.pendingInputStore.resolveGate(id, dto);
  }

  async onQuestionAnswered(id: string, dto: SubmitAnswerDto): Promise<void> {
    await this.pendingInputStore.answerQuestion(id, dto);
  }

  formatGateTime(date: Date): string {
    return formatRelativeTime(date);
  }

  getBadgeButtonClasses(): string {
    const base = 'relative rounded-full p-2 focus:outline-none focus:ring-2 focus:ring-blue-500';
    const hasQuestion = this.pendingInputStore.hasUrgentInput();
    const hasGates = this.pendingInputStore.gates().length > 0;

    if (hasQuestion) {
      // Red with pulse for urgent question
      return `${base} text-red-500 hover:bg-red-100 dark:hover:bg-red-900/30`;
    } else if (hasGates) {
      // Amber for gates only
      return `${base} text-amber-500 hover:bg-amber-100 dark:hover:bg-amber-900/30`;
    } else {
      // Default gray
      return `${base} text-gray-500 hover:bg-gray-100 hover:text-gray-700 dark:text-gray-400 dark:hover:bg-gray-800 dark:hover:text-gray-200`;
    }
  }

  getBadgeClasses(): string {
    const base = 'absolute -right-0.5 -top-0.5 flex h-5 min-w-5 items-center justify-center rounded-full px-1 text-xs font-medium text-white';
    const hasQuestion = this.pendingInputStore.hasUrgentInput();

    if (hasQuestion) {
      return `${base} bg-red-500 animate-pulse`;
    } else {
      return `${base} bg-amber-500`;
    }
  }
}
