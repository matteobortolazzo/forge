import { Component, ChangeDetectionStrategy, inject, signal } from '@angular/core';
import { SchedulerStore } from '../../core/stores/scheduler.store';

@Component({
  selector: 'app-scheduler-status',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="flex items-center gap-4">
      <!-- Scheduler Toggle -->
      <div class="flex items-center gap-2">
        <span class="text-sm text-gray-600 dark:text-gray-400">Auto-Scheduler</span>
        <button
          type="button"
          role="switch"
          [attr.aria-checked]="schedulerStore.isEnabled()"
          [class]="toggleClasses()"
          [disabled]="isToggling()"
          (click)="toggleScheduler()"
          aria-label="Toggle automatic task scheduling"
        >
          <span
            [class]="knobClasses()"
            aria-hidden="true"
          ></span>
        </button>
      </div>

      <!-- Divider -->
      <div class="h-5 w-px bg-gray-300 dark:bg-gray-700" aria-hidden="true"></div>

      <!-- Status Indicators -->
      <div class="flex items-center gap-3 text-sm">
        <!-- Current Task -->
        @if (schedulerStore.isAgentRunning() && schedulerStore.currentTaskId()) {
          <div class="flex items-center gap-1.5 text-green-600 dark:text-green-400">
            <span class="relative flex h-2 w-2">
              <span class="absolute inline-flex h-full w-full animate-ping rounded-full bg-green-400 opacity-75"></span>
              <span class="relative inline-flex h-2 w-2 rounded-full bg-green-500"></span>
            </span>
            <span>Processing</span>
          </div>
        } @else {
          <div class="flex items-center gap-1.5 text-gray-500 dark:text-gray-400">
            <span class="h-2 w-2 rounded-full bg-gray-400"></span>
            <span>Idle</span>
          </div>
        }

        <!-- Pending Count -->
        <div
          class="flex items-center gap-1 text-blue-600 dark:text-blue-400"
          title="Tasks pending scheduling"
        >
          <svg class="h-4 w-4" viewBox="0 0 20 20" fill="currentColor" aria-hidden="true">
            <path d="M10 2a8 8 0 100 16 8 8 0 000-16zm.75 4.75a.75.75 0 00-1.5 0v3.5c0 .414.336.75.75.75h2.5a.75.75 0 000-1.5h-1.75v-2.75z" />
          </svg>
          <span>{{ schedulerStore.pendingTaskCount() }}</span>
        </div>

        <!-- Paused Count -->
        @if (schedulerStore.pausedTaskCount() > 0) {
          <div
            class="flex items-center gap-1 text-amber-600 dark:text-amber-400"
            title="Tasks paused"
          >
            <svg class="h-4 w-4" viewBox="0 0 20 20" fill="currentColor" aria-hidden="true">
              <path fill-rule="evenodd" d="M2 10a8 8 0 1116 0 8 8 0 01-16 0zm5-2.25A.75.75 0 017.75 7h.5a.75.75 0 01.75.75v4.5a.75.75 0 01-.75.75h-.5a.75.75 0 01-.75-.75v-4.5zm4 0a.75.75 0 01.75-.75h.5a.75.75 0 01.75.75v4.5a.75.75 0 01-.75.75h-.5a.75.75 0 01-.75-.75v-4.5z" clip-rule="evenodd" />
            </svg>
            <span>{{ schedulerStore.pausedTaskCount() }}</span>
          </div>
        }
      </div>
    </div>
  `,
  styles: `
    :host {
      display: block;
    }
  `,
})
export class SchedulerStatusComponent {
  protected readonly schedulerStore = inject(SchedulerStore);
  protected readonly isToggling = signal(false);

  protected toggleClasses(): string {
    const base = 'relative inline-flex h-6 w-11 shrink-0 cursor-pointer rounded-full border-2 border-transparent transition-colors duration-200 ease-in-out focus:outline-none focus:ring-2 focus:ring-blue-500 focus:ring-offset-2 disabled:cursor-not-allowed disabled:opacity-50';
    return this.schedulerStore.isEnabled()
      ? `${base} bg-blue-600`
      : `${base} bg-gray-200 dark:bg-gray-700`;
  }

  protected knobClasses(): string {
    const base = 'pointer-events-none inline-block h-5 w-5 transform rounded-full bg-white shadow ring-0 transition duration-200 ease-in-out';
    return this.schedulerStore.isEnabled()
      ? `${base} translate-x-5`
      : `${base} translate-x-0`;
  }

  protected async toggleScheduler(): Promise<void> {
    this.isToggling.set(true);
    if (this.schedulerStore.isEnabled()) {
      await this.schedulerStore.disable();
    } else {
      await this.schedulerStore.enable();
    }
    this.isToggling.set(false);
  }
}
