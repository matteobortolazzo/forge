import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { ArtifactType } from '../models';

@Component({
  selector: 'app-artifact-type-badge',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <span [class]="badgeClasses()" [attr.aria-label]="'Type: ' + typeLabel()">
      {{ typeLabel() }}
    </span>
  `,
  styles: `
    span {
      display: inline-flex;
      align-items: center;
      padding: 0.125rem 0.5rem;
      border-radius: 0.25rem;
      font-size: 0.75rem;
      font-weight: 500;
      text-transform: capitalize;
    }
  `,
})
export class ArtifactTypeBadgeComponent {
  readonly type = input.required<ArtifactType>();

  readonly typeLabel = computed(() => {
    switch (this.type()) {
      case 'plan':
        return 'Plan';
      case 'implementation':
        return 'Implementation';
      case 'review':
        return 'Review';
      case 'test':
        return 'Test';
      case 'general':
        return 'General';
      default:
        return this.type();
    }
  });

  readonly badgeClasses = computed(() => {
    const base = 'artifact-badge';
    switch (this.type()) {
      case 'plan':
        return `${base} bg-blue-100 text-blue-700 dark:bg-blue-900 dark:text-blue-300`;
      case 'implementation':
        return `${base} bg-green-100 text-green-700 dark:bg-green-900 dark:text-green-300`;
      case 'review':
        return `${base} bg-purple-100 text-purple-700 dark:bg-purple-900 dark:text-purple-300`;
      case 'test':
        return `${base} bg-amber-100 text-amber-700 dark:bg-amber-900 dark:text-amber-300`;
      case 'general':
      default:
        return `${base} bg-gray-100 text-gray-700 dark:bg-gray-700 dark:text-gray-300`;
    }
  });
}
