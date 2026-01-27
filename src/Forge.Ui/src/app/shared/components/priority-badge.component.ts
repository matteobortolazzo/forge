import { Component, ChangeDetectionStrategy, input, computed } from '@angular/core';
import { Priority } from '../models';
import { BaseBadgeComponent, BadgeVariant } from './base-badge.component';

/** Mapping of priorities to badge variants */
const PRIORITY_VARIANTS: Record<Priority, BadgeVariant> = {
  critical: 'red',
  high: 'orange',
  medium: 'yellow',
  low: 'green',
};

@Component({
  selector: 'app-priority-badge',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [BaseBadgeComponent],
  template: `
    <app-base-badge
      [label]="priority()"
      [variant]="variant()"
      ariaPrefix="Priority: "
      size="sm"
      shape="pill"
    />
  `,
  styles: `
    :host {
      display: inline-flex;
    }
    app-base-badge {
      text-transform: capitalize;
    }
  `,
})
export class PriorityBadgeComponent {
  readonly priority = input.required<Priority>();

  readonly variant = computed((): BadgeVariant => {
    return PRIORITY_VARIANTS[this.priority()] ?? 'gray';
  });
}
