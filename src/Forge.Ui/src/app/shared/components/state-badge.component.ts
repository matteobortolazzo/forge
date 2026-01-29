import { Component, ChangeDetectionStrategy, input, computed } from '@angular/core';
import { PipelineState, BacklogItemState } from '../models';
import { BaseBadgeComponent, BadgeVariant } from './base-badge.component';

/** Combined state type for tasks and backlog items */
type StateType = PipelineState | BacklogItemState;

/** Mapping of pipeline states to badge variants */
const STATE_VARIANTS: Record<StateType, BadgeVariant> = {
  // BacklogItem states
  New: 'slate',
  Refining: 'purple',
  Ready: 'cyan',
  Splitting: 'indigo',
  Executing: 'blue',
  Done: 'green',
  // Task (Pipeline) states
  Planning: 'purple',
  Implementing: 'blue',
  PrReady: 'green',
};

@Component({
  selector: 'app-state-badge',
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
export class StateBadgeComponent {
  readonly state = input.required<StateType>();

  readonly stateLabel = computed(() => {
    const s = this.state();
    return s === 'PrReady' ? 'PR Ready' : s;
  });

  readonly variant = computed((): BadgeVariant => {
    return STATE_VARIANTS[this.state()] ?? 'gray';
  });
}
