import { Component, ChangeDetectionStrategy, input, computed } from '@angular/core';
import { BacklogItemState } from '../../models';
import { BaseBadgeComponent, BadgeVariant } from '../base-badge.component';

/** Mapping of backlog item states to badge variants */
const STATE_VARIANTS: Record<BacklogItemState, BadgeVariant> = {
  New: 'slate',
  Refining: 'purple',
  Ready: 'blue',
  Splitting: 'cyan',
  Executing: 'amber',
  Done: 'green',
};

/** Human-readable labels for backlog item states */
const STATE_LABELS: Record<BacklogItemState, string> = {
  New: 'New',
  Refining: 'Refining',
  Ready: 'Ready',
  Splitting: 'Splitting',
  Executing: 'Executing',
  Done: 'Done',
};

@Component({
  selector: 'app-backlog-state-badge',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [BaseBadgeComponent],
  template: `
    <app-base-badge
      [label]="stateLabel()"
      [variant]="variant()"
      ariaPrefix="State: "
      size="sm"
    />
  `,
  styles: `
    :host {
      display: inline-flex;
    }
    app-base-badge {
      text-transform: uppercase;
      letter-spacing: 0.025em;
      font-weight: 600;
    }
  `,
})
export class BacklogStateBadgeComponent {
  readonly state = input.required<BacklogItemState>();

  readonly stateLabel = computed(() => {
    return STATE_LABELS[this.state()] ?? this.state();
  });

  readonly variant = computed((): BadgeVariant => {
    return STATE_VARIANTS[this.state()] ?? 'gray';
  });
}
