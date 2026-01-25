import { Component, ChangeDetectionStrategy, input, computed } from '@angular/core';
import { PipelineState } from '../models';

@Component({
  selector: 'app-state-badge',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <span
      [class]="badgeClasses()"
      [attr.aria-label]="'State: ' + stateLabel()"
    >
      {{ stateLabel() }}
    </span>
  `,
  styles: `
    span {
      display: inline-flex;
      align-items: center;
      padding: 0.25rem 0.75rem;
      border-radius: 0.375rem;
      font-size: 0.75rem;
      font-weight: 600;
      text-transform: uppercase;
      letter-spacing: 0.025em;
    }
  `,
})
export class StateBadgeComponent {
  readonly state = input.required<PipelineState>();

  readonly stateLabel = computed(() => {
    switch (this.state()) {
      case 'PrReady':
        return 'PR Ready';
      default:
        return this.state();
    }
  });

  readonly badgeClasses = computed(() => {
    const base = 'state-badge';
    switch (this.state()) {
      case 'Backlog':
        return `${base} bg-slate-100 text-slate-700 dark:bg-slate-800 dark:text-slate-300`;
      case 'Planning':
        return `${base} bg-purple-100 text-purple-700 dark:bg-purple-900 dark:text-purple-300`;
      case 'Implementing':
        return `${base} bg-blue-100 text-blue-700 dark:bg-blue-900 dark:text-blue-300`;
      case 'Reviewing':
        return `${base} bg-amber-100 text-amber-700 dark:bg-amber-900 dark:text-amber-300`;
      case 'Testing':
        return `${base} bg-cyan-100 text-cyan-700 dark:bg-cyan-900 dark:text-cyan-300`;
      case 'PrReady':
        return `${base} bg-indigo-100 text-indigo-700 dark:bg-indigo-900 dark:text-indigo-300`;
      case 'Done':
        return `${base} bg-green-100 text-green-700 dark:bg-green-900 dark:text-green-300`;
      default:
        return `${base} bg-gray-100 text-gray-700 dark:bg-gray-700 dark:text-gray-300`;
    }
  });
}
