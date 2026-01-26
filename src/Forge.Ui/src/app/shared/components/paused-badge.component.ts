import { Component, ChangeDetectionStrategy, input } from '@angular/core';

@Component({
  selector: 'app-paused-badge',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <span
      class="inline-flex items-center gap-1 rounded-md bg-amber-100 px-2 py-1 text-xs font-medium text-amber-700 dark:bg-amber-900/50 dark:text-amber-400"
      [title]="reason() || 'Task is paused'"
      aria-label="Paused"
    >
      <svg
        class="h-3.5 w-3.5"
        viewBox="0 0 20 20"
        fill="currentColor"
        aria-hidden="true"
      >
        <path
          fill-rule="evenodd"
          d="M2 10a8 8 0 1116 0 8 8 0 01-16 0zm5-2.25A.75.75 0 017.75 7h.5a.75.75 0 01.75.75v4.5a.75.75 0 01-.75.75h-.5a.75.75 0 01-.75-.75v-4.5zm4 0a.75.75 0 01.75-.75h.5a.75.75 0 01.75.75v4.5a.75.75 0 01-.75.75h-.5a.75.75 0 01-.75-.75v-4.5z"
          clip-rule="evenodd"
        />
      </svg>
      Paused
    </span>
  `,
  styles: `
    :host {
      display: inline-flex;
    }
  `,
})
export class PausedBadgeComponent {
  readonly reason = input<string>();
}
