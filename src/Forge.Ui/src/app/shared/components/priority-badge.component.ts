import { Component, ChangeDetectionStrategy, input, computed } from '@angular/core';
import { Priority } from '../models';

@Component({
  selector: 'app-priority-badge',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <span
      [class]="badgeClasses()"
      [attr.aria-label]="'Priority: ' + priority()"
    >
      {{ priorityLabel() }}
    </span>
  `,
  styles: `
    span {
      display: inline-flex;
      align-items: center;
      padding: 0.125rem 0.5rem;
      border-radius: 9999px;
      font-size: 0.75rem;
      font-weight: 500;
      text-transform: capitalize;
    }
  `,
})
export class PriorityBadgeComponent {
  readonly priority = input.required<Priority>();

  readonly priorityLabel = computed(() => this.priority());

  readonly badgeClasses = computed(() => {
    const base = 'priority-badge';
    switch (this.priority()) {
      case 'critical':
        return `${base} bg-red-100 text-red-800 dark:bg-red-900 dark:text-red-200`;
      case 'high':
        return `${base} bg-orange-100 text-orange-800 dark:bg-orange-900 dark:text-orange-200`;
      case 'medium':
        return `${base} bg-yellow-100 text-yellow-800 dark:bg-yellow-900 dark:text-yellow-200`;
      case 'low':
        return `${base} bg-green-100 text-green-800 dark:bg-green-900 dark:text-green-200`;
      default:
        return `${base} bg-gray-100 text-gray-800 dark:bg-gray-700 dark:text-gray-200`;
    }
  });
}
