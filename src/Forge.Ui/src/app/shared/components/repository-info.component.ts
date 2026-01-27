import { Component, ChangeDetectionStrategy, inject } from '@angular/core';
import { RepositoryStore } from '../../core/stores/repository.store';

/**
 * Simplified repository info display for the header.
 * Repository selection is now handled by the sidebar.
 * This component just shows the current repository name and branch.
 */
@Component({
  selector: 'app-repository-info',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    @if (repositoryStore.loading()) {
      <div class="flex items-center gap-2 text-sm text-gray-400">
        <svg class="h-4 w-4 animate-spin" viewBox="0 0 24 24" fill="none" aria-hidden="true">
          <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"></circle>
          <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path>
        </svg>
        <span>Loading...</span>
      </div>
    } @else if (!repositoryStore.hasRepositories()) {
      <span class="text-sm text-gray-500 dark:text-gray-400">
        No repository selected
      </span>
    } @else if (repositoryStore.info()) {
      <div class="flex items-center gap-2 text-sm">
        <!-- Repository Icon -->
        <svg
          class="h-4 w-4 text-gray-500 dark:text-gray-400"
          viewBox="0 0 16 16"
          fill="currentColor"
          aria-hidden="true"
        >
          <path d="M2 2.5A2.5 2.5 0 014.5 0h8.75a.75.75 0 01.75.75v12.5a.75.75 0 01-.75.75h-2.5a.75.75 0 110-1.5h1.75v-2h-8a1 1 0 00-.714 1.7.75.75 0 01-1.072 1.05A2.495 2.495 0 012 11.5v-9zm10.5-1V9h-8c-.356 0-.694.074-1 .208V2.5a1 1 0 011-1h8zM5 12.25v3.25a.25.25 0 00.4.2l1.45-1.087a.25.25 0 01.3 0L8.6 15.7a.25.25 0 00.4-.2v-3.25a.25.25 0 00-.25-.25h-3.5a.25.25 0 00-.25.25z" />
        </svg>

        <!-- Repository Name -->
        <span
          class="font-medium text-gray-700 dark:text-gray-300"
          [title]="repositoryStore.path()"
        >
          {{ repositoryStore.name() }}
        </span>

        @if (repositoryStore.isGitRepository()) {
          <!-- Separator -->
          <span class="text-gray-400 dark:text-gray-600">/</span>

          <!-- Branch Icon -->
          <svg
            class="h-4 w-4 text-gray-500 dark:text-gray-400"
            viewBox="0 0 16 16"
            fill="currentColor"
            aria-hidden="true"
          >
            <path fill-rule="evenodd" d="M11.75 2.5a.75.75 0 100 1.5.75.75 0 000-1.5zm-2.25.75a2.25 2.25 0 113 2.122V6A2.5 2.5 0 0110 8.5H6a1 1 0 00-1 1v1.128a2.251 2.251 0 11-1.5 0V5.372a2.25 2.25 0 111.5 0v1.836A2.492 2.492 0 016 7h4a1 1 0 001-1v-.628A2.25 2.25 0 019.5 3.25zM4.25 12a.75.75 0 100 1.5.75.75 0 000-1.5zM3.5 3.25a.75.75 0 111.5 0 .75.75 0 01-1.5 0z" clip-rule="evenodd" />
          </svg>

          <!-- Branch Name -->
          <span class="text-gray-600 dark:text-gray-400">
            {{ repositoryStore.displayBranch() }}
          </span>

          <!-- Dirty Indicator -->
          @if (repositoryStore.isDirty()) {
            <span
              class="h-2 w-2 rounded-full bg-amber-500"
              title="Uncommitted changes"
              aria-label="Repository has uncommitted changes"
            ></span>
          }
        }
      </div>
    }
  `,
  styles: `
    :host {
      display: block;
    }
  `,
})
export class RepositoryInfoComponent {
  protected readonly repositoryStore = inject(RepositoryStore);
}
