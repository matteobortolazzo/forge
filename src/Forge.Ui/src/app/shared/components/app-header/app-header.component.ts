import { Component, ChangeDetectionStrategy } from '@angular/core';
import { NotificationPanelComponent } from '../../../features/notifications/notification-panel.component';
import { SchedulerStatusComponent } from '../scheduler-status.component';
import { RepositoryInfoComponent } from '../repository-info.component';
import { AgentWorkingIndicatorComponent } from '../agent-working-indicator/agent-working-indicator.component';

@Component({
  selector: 'app-header',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    NotificationPanelComponent,
    SchedulerStatusComponent,
    RepositoryInfoComponent,
    AgentWorkingIndicatorComponent,
  ],
  template: `
    <header
      class="flex items-center justify-between border-b border-gray-200 bg-white px-6 py-4 dark:border-gray-800 dark:bg-gray-900"
    >
      <div class="flex items-center gap-3">
        <h1 class="text-xl font-bold text-gray-900 dark:text-gray-100">Forge</h1>
        <span class="text-sm text-gray-500 dark:text-gray-400">
          AI Agent Dashboard
        </span>
        <div
          class="h-5 w-px bg-gray-300 dark:bg-gray-700"
          aria-hidden="true"
        ></div>
        <app-repository-info />
        <app-agent-working-indicator />
      </div>

      <div class="flex items-center gap-4">
        <app-scheduler-status />

        <div
          class="h-5 w-px bg-gray-300 dark:bg-gray-700"
          aria-hidden="true"
        ></div>

        <!-- Slot for view-specific action buttons -->
        <ng-content />

        <app-notification-panel />
      </div>
    </header>
  `,
})
export class AppHeaderComponent {}
