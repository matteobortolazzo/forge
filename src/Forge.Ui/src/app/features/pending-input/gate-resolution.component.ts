import { Component, ChangeDetectionStrategy, input, output, signal } from '@angular/core';
import { HumanGate, HumanGateStatus, ResolveGateDto } from '../../shared/models';

/**
 * Component for resolving a human gate with approve/reject/skip actions.
 * Displays gate type, confidence score, and reason.
 */
@Component({
  selector: 'app-gate-resolution',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="rounded-lg border border-gray-200 bg-white p-4 dark:border-gray-700 dark:bg-gray-800">
      <!-- Header with gate type badge and confidence -->
      <div class="flex items-center justify-between">
        <div class="flex items-center gap-2">
          <span [class]="getGateTypeBadgeClasses()">
            {{ formatGateType(gate().gateType) }}
          </span>
          @if (gate().confidenceScore !== undefined) {
            <span class="text-xs text-gray-500 dark:text-gray-400">
              Confidence: {{ (gate().confidenceScore * 100).toFixed(0) }}%
            </span>
          }
        </div>
      </div>

      <!-- Reason -->
      <p class="mt-2 text-sm text-gray-700 dark:text-gray-300">
        {{ gate().reason }}
      </p>

      <!-- Resolution message input -->
      @if (showResolutionInput()) {
        <div class="mt-3">
          <label for="resolution-message" class="sr-only">Resolution message</label>
          <textarea
            id="resolution-message"
            class="w-full rounded-md border border-gray-300 px-3 py-2 text-sm focus:border-blue-500 focus:outline-none focus:ring-1 focus:ring-blue-500 dark:border-gray-600 dark:bg-gray-700 dark:text-gray-200"
            rows="2"
            placeholder="Optional: Add a resolution message..."
            [value]="resolutionMessage()"
            (input)="onResolutionInput($event)"
          ></textarea>
        </div>
      }

      <!-- Action buttons -->
      <div class="mt-3 flex items-center gap-2">
        <button
          type="button"
          class="inline-flex items-center gap-1.5 rounded-md bg-green-600 px-3 py-1.5 text-sm font-medium text-white hover:bg-green-700 focus:outline-none focus:ring-2 focus:ring-green-500 focus:ring-offset-2 disabled:opacity-50"
          [disabled]="isResolving()"
          (click)="onApprove()"
        >
          <svg class="h-4 w-4" viewBox="0 0 20 20" fill="currentColor" aria-hidden="true">
            <path fill-rule="evenodd" d="M16.704 4.153a.75.75 0 01.143 1.052l-8 10.5a.75.75 0 01-1.127.075l-4.5-4.5a.75.75 0 011.06-1.06l3.894 3.893 7.48-9.817a.75.75 0 011.05-.143z" clip-rule="evenodd" />
          </svg>
          Approve
        </button>
        <button
          type="button"
          class="inline-flex items-center gap-1.5 rounded-md bg-red-600 px-3 py-1.5 text-sm font-medium text-white hover:bg-red-700 focus:outline-none focus:ring-2 focus:ring-red-500 focus:ring-offset-2 disabled:opacity-50"
          [disabled]="isResolving()"
          (click)="onReject()"
        >
          <svg class="h-4 w-4" viewBox="0 0 20 20" fill="currentColor" aria-hidden="true">
            <path d="M6.28 5.22a.75.75 0 00-1.06 1.06L8.94 10l-3.72 3.72a.75.75 0 101.06 1.06L10 11.06l3.72 3.72a.75.75 0 101.06-1.06L11.06 10l3.72-3.72a.75.75 0 00-1.06-1.06L10 8.94 6.28 5.22z" />
          </svg>
          Reject
        </button>
        <button
          type="button"
          class="inline-flex items-center gap-1.5 rounded-md bg-gray-200 px-3 py-1.5 text-sm font-medium text-gray-700 hover:bg-gray-300 focus:outline-none focus:ring-2 focus:ring-gray-500 focus:ring-offset-2 disabled:opacity-50 dark:bg-gray-600 dark:text-gray-200 dark:hover:bg-gray-500"
          [disabled]="isResolving()"
          (click)="onSkip()"
        >
          Skip
        </button>
        <button
          type="button"
          class="ml-auto text-xs text-gray-500 hover:text-gray-700 dark:text-gray-400 dark:hover:text-gray-200"
          (click)="toggleResolutionInput()"
        >
          {{ showResolutionInput() ? 'Hide message' : 'Add message' }}
        </button>
      </div>
    </div>
  `,
})
export class GateResolutionComponent {
  readonly gate = input.required<HumanGate>();
  readonly resolved = output<ResolveGateDto>();

  readonly isResolving = signal(false);
  readonly showResolutionInput = signal(false);
  readonly resolutionMessage = signal('');

  formatGateType(type: string): string {
    switch (type) {
      case 'refining':
        return 'Refining';
      case 'split':
        return 'Split';
      case 'planning':
        return 'Planning';
      case 'pr':
        return 'PR';
      default:
        return type;
    }
  }

  getGateTypeBadgeClasses(): string {
    const base = 'inline-flex items-center rounded-full px-2 py-0.5 text-xs font-medium';
    const type = this.gate().gateType;

    switch (type) {
      case 'pr':
        return `${base} bg-purple-100 text-purple-700 dark:bg-purple-900/30 dark:text-purple-400`;
      case 'planning':
        return `${base} bg-blue-100 text-blue-700 dark:bg-blue-900/30 dark:text-blue-400`;
      case 'split':
        return `${base} bg-amber-100 text-amber-700 dark:bg-amber-900/30 dark:text-amber-400`;
      case 'refining':
        return `${base} bg-green-100 text-green-700 dark:bg-green-900/30 dark:text-green-400`;
      default:
        return `${base} bg-gray-100 text-gray-700 dark:bg-gray-900/30 dark:text-gray-400`;
    }
  }

  toggleResolutionInput(): void {
    this.showResolutionInput.update(show => !show);
  }

  onResolutionInput(event: Event): void {
    const target = event.target as HTMLTextAreaElement;
    this.resolutionMessage.set(target.value);
  }

  onApprove(): void {
    this.resolve('approved');
  }

  onReject(): void {
    this.resolve('rejected');
  }

  onSkip(): void {
    this.resolve('skipped');
  }

  private resolve(status: HumanGateStatus): void {
    this.isResolving.set(true);
    const dto: ResolveGateDto = {
      status,
      resolution: this.resolutionMessage() || undefined,
    };
    this.resolved.emit(dto);
  }
}
