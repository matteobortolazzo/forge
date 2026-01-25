import { Component, ChangeDetectionStrategy, input, computed } from '@angular/core';

@Component({
  selector: 'app-loading-spinner',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div
      [class]="containerClasses()"
      role="status"
      [attr.aria-label]="label() || 'Loading'"
    >
      <svg
        [class]="spinnerClasses()"
        xmlns="http://www.w3.org/2000/svg"
        fill="none"
        viewBox="0 0 24 24"
        aria-hidden="true"
      >
        <circle
          class="opacity-25"
          cx="12"
          cy="12"
          r="10"
          stroke="currentColor"
          stroke-width="4"
        ></circle>
        <path
          class="opacity-75"
          fill="currentColor"
          d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"
        ></path>
      </svg>
      @if (label()) {
        <span class="text-sm text-gray-600 dark:text-gray-400">
          {{ label() }}
        </span>
      }
      <span class="sr-only">{{ label() || 'Loading' }}</span>
    </div>
  `,
})
export class LoadingSpinnerComponent {
  readonly size = input<'sm' | 'md' | 'lg'>('md');
  readonly label = input<string>();
  readonly inline = input(false);

  readonly containerClasses = computed(() => {
    const base = 'flex items-center gap-2';
    return this.inline() ? base : `${base} justify-center`;
  });

  readonly spinnerClasses = computed(() => {
    const base = 'animate-spin text-blue-600 dark:text-blue-400';
    switch (this.size()) {
      case 'sm':
        return `${base} h-4 w-4`;
      case 'lg':
        return `${base} h-8 w-8`;
      default:
        return `${base} h-6 w-6`;
    }
  });
}
