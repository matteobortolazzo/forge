import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { ArtifactType } from '../models';
import { BaseBadgeComponent, BadgeVariant } from './base-badge.component';

/** Mapping of artifact types to badge variants */
const ARTIFACT_VARIANTS: Record<ArtifactType, BadgeVariant> = {
  task_split: 'purple',
  research_findings: 'blue',
  plan: 'blue',
  implementation: 'green',
  simplification_review: 'amber',
  verification_report: 'green',
  review: 'purple',
  documentation_update: 'gray',
  test: 'amber',
  general: 'gray',
};

/** Mapping of artifact types to display labels */
const ARTIFACT_LABELS: Record<ArtifactType, string> = {
  task_split: 'Task Split',
  research_findings: 'Research',
  plan: 'Plan',
  implementation: 'Implementation',
  simplification_review: 'Simplification',
  verification_report: 'Verification',
  review: 'Review',
  documentation_update: 'Docs',
  test: 'Test',
  general: 'General',
};

@Component({
  selector: 'app-artifact-type-badge',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [BaseBadgeComponent],
  template: `
    <app-base-badge
      [label]="typeLabel()"
      [variant]="variant()"
      ariaPrefix="Type: "
      size="sm"
    />
  `,
  styles: `
    :host {
      display: inline-flex;
    }
  `,
})
export class ArtifactTypeBadgeComponent {
  readonly type = input.required<ArtifactType>();

  readonly typeLabel = computed(() => {
    return ARTIFACT_LABELS[this.type()] ?? this.type();
  });

  readonly variant = computed((): BadgeVariant => {
    return ARTIFACT_VARIANTS[this.type()] ?? 'gray';
  });
}
