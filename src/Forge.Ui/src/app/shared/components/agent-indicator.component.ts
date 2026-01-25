import { Component, ChangeDetectionStrategy, input } from '@angular/core';

@Component({
  selector: 'app-agent-indicator',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    @if (isRunning()) {
      <span
        class="inline-flex items-center gap-1.5"
        role="status"
        [attr.aria-label]="'Agent is running'"
      >
        <span class="relative flex h-2.5 w-2.5">
          <span
            class="absolute inline-flex h-full w-full animate-ping rounded-full bg-green-400 opacity-75"
          ></span>
          <span
            class="relative inline-flex h-2.5 w-2.5 rounded-full bg-green-500"
          ></span>
        </span>
        @if (showLabel()) {
          <span class="text-xs font-medium text-green-600 dark:text-green-400">
            Agent Active
          </span>
        }
      </span>
    }
  `,
})
export class AgentIndicatorComponent {
  readonly isRunning = input(false);
  readonly showLabel = input(false);
}
