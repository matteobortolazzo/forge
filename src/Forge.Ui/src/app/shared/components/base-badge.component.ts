import { Component, ChangeDetectionStrategy, input, computed } from '@angular/core';

/**
 * Badge color variants with light/dark mode support.
 * Each variant provides background and text colors.
 */
export type BadgeVariant =
  | 'slate'
  | 'gray'
  | 'red'
  | 'orange'
  | 'amber'
  | 'yellow'
  | 'green'
  | 'cyan'
  | 'blue'
  | 'indigo'
  | 'purple';

/**
 * Badge size options affecting padding and font size.
 */
export type BadgeSize = 'sm' | 'md';

/**
 * Badge shape options.
 */
export type BadgeShape = 'rounded' | 'pill';

/**
 * Mapping of variants to Tailwind classes for light and dark modes.
 */
const VARIANT_CLASSES: Record<BadgeVariant, string> = {
  slate: 'bg-slate-100 text-slate-700 dark:bg-slate-800 dark:text-slate-300',
  gray: 'bg-gray-100 text-gray-700 dark:bg-gray-700 dark:text-gray-300',
  red: 'bg-red-100 text-red-800 dark:bg-red-900 dark:text-red-200',
  orange: 'bg-orange-100 text-orange-800 dark:bg-orange-900 dark:text-orange-200',
  amber: 'bg-amber-100 text-amber-700 dark:bg-amber-900 dark:text-amber-300',
  yellow: 'bg-yellow-100 text-yellow-800 dark:bg-yellow-900 dark:text-yellow-200',
  green: 'bg-green-100 text-green-700 dark:bg-green-900 dark:text-green-300',
  cyan: 'bg-cyan-100 text-cyan-700 dark:bg-cyan-900 dark:text-cyan-300',
  blue: 'bg-blue-100 text-blue-700 dark:bg-blue-900 dark:text-blue-300',
  indigo: 'bg-indigo-100 text-indigo-700 dark:bg-indigo-900 dark:text-indigo-300',
  purple: 'bg-purple-100 text-purple-700 dark:bg-purple-900 dark:text-purple-300',
};

/**
 * Base badge component providing consistent styling for status indicators.
 * Use this directly or as the base for domain-specific badge components.
 *
 * @example
 * <app-base-badge label="Active" variant="green" />
 * <app-base-badge label="High" variant="orange" size="sm" shape="pill" />
 */
@Component({
  selector: 'app-base-badge',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <span [class]="badgeClasses()" [attr.aria-label]="ariaLabel()">
      {{ label() }}
    </span>
  `,
  styles: `
    span {
      display: inline-flex;
      align-items: center;
    }
  `,
})
export class BaseBadgeComponent {
  /** The text to display in the badge */
  readonly label = input.required<string>();

  /** Color variant */
  readonly variant = input<BadgeVariant>('gray');

  /** Badge size */
  readonly size = input<BadgeSize>('sm');

  /** Badge shape */
  readonly shape = input<BadgeShape>('rounded');

  /** Optional aria-label prefix (e.g., "Status: " or "Priority: ") */
  readonly ariaPrefix = input<string>('');

  /** Computed aria-label combining prefix and label */
  readonly ariaLabel = computed(() => {
    const prefix = this.ariaPrefix();
    return prefix ? `${prefix}${this.label()}` : this.label();
  });

  /** Computed CSS classes */
  readonly badgeClasses = computed(() => {
    const variantClasses = VARIANT_CLASSES[this.variant()];
    const sizeClasses = this.size() === 'sm'
      ? 'px-2 py-0.5 text-xs'
      : 'px-3 py-1 text-sm';
    const shapeClasses = this.shape() === 'pill'
      ? 'rounded-full'
      : 'rounded-md';
    const fontClasses = 'font-medium';

    return `${variantClasses} ${sizeClasses} ${shapeClasses} ${fontClasses}`;
  });
}
