import { Component, ChangeDetectionStrategy, inject, signal } from '@angular/core';
import { RepositoryStore } from '../../core/stores/repository.store';

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
      <button
        type="button"
        class="flex items-center gap-2 text-sm text-amber-600 hover:text-amber-700 dark:text-amber-400 dark:hover:text-amber-300"
        (click)="showAddRepoPrompt()"
      >
        <svg class="h-4 w-4" viewBox="0 0 20 20" fill="currentColor" aria-hidden="true">
          <path d="M10.75 4.75a.75.75 0 00-1.5 0v4.5h-4.5a.75.75 0 000 1.5h4.5v4.5a.75.75 0 001.5 0v-4.5h4.5a.75.75 0 000-1.5h-4.5v-4.5z" />
        </svg>
        Add Repository
      </button>
    } @else if (repositoryStore.info()) {
      <div class="relative">
        <!-- Repository Selector Button -->
        <button
          type="button"
          class="flex items-center gap-2 rounded-md px-2 py-1 text-sm hover:bg-gray-100 dark:hover:bg-gray-800"
          [class.bg-gray-100]="isDropdownOpen()"
          [class.dark:bg-gray-800]="isDropdownOpen()"
          (click)="toggleDropdown()"
          [attr.aria-expanded]="isDropdownOpen()"
          aria-haspopup="listbox"
        >
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

          <!-- Dropdown Arrow (only show if multiple repos) -->
          @if (repositoryStore.activeRepositories().length > 1) {
            <svg
              class="h-4 w-4 text-gray-400 transition-transform"
              [class.rotate-180]="isDropdownOpen()"
              viewBox="0 0 20 20"
              fill="currentColor"
              aria-hidden="true"
            >
              <path fill-rule="evenodd" d="M5.23 7.21a.75.75 0 011.06.02L10 11.168l3.71-3.938a.75.75 0 111.08 1.04l-4.25 4.5a.75.75 0 01-1.08 0l-4.25-4.5a.75.75 0 01.02-1.06z" clip-rule="evenodd" />
            </svg>
          }
        </button>

        <!-- Dropdown Menu -->
        @if (isDropdownOpen() && repositoryStore.activeRepositories().length > 1) {
          <div
            class="absolute left-0 top-full z-50 mt-1 w-64 rounded-md border border-gray-200 bg-white py-1 shadow-lg dark:border-gray-700 dark:bg-gray-800"
            role="listbox"
          >
            @for (repo of repositoryStore.activeRepositories(); track repo.id) {
              <button
                type="button"
                class="flex w-full items-center gap-2 px-3 py-2 text-left text-sm hover:bg-gray-100 dark:hover:bg-gray-700"
                [class.bg-blue-50]="repo.id === repositoryStore.selectedId()"
                [class.dark:bg-blue-900]="repo.id === repositoryStore.selectedId()"
                role="option"
                [attr.aria-selected]="repo.id === repositoryStore.selectedId()"
                (click)="selectRepository(repo.id)"
              >
                <svg
                  class="h-4 w-4 flex-shrink-0 text-gray-400"
                  viewBox="0 0 16 16"
                  fill="currentColor"
                  aria-hidden="true"
                >
                  <path d="M2 2.5A2.5 2.5 0 014.5 0h8.75a.75.75 0 01.75.75v12.5a.75.75 0 01-.75.75h-2.5a.75.75 0 110-1.5h1.75v-2h-8a1 1 0 00-.714 1.7.75.75 0 01-1.072 1.05A2.495 2.495 0 012 11.5v-9zm10.5-1V9h-8c-.356 0-.694.074-1 .208V2.5a1 1 0 011-1h8zM5 12.25v3.25a.25.25 0 00.4.2l1.45-1.087a.25.25 0 01.3 0L8.6 15.7a.25.25 0 00.4-.2v-3.25a.25.25 0 00-.25-.25h-3.5a.25.25 0 00-.25.25z" />
                </svg>
                <div class="min-w-0 flex-1">
                  <div class="flex items-center gap-2">
                    <span class="truncate font-medium text-gray-700 dark:text-gray-300">
                      {{ repo.name }}
                    </span>
                    @if (repo.isDefault) {
                      <span class="rounded bg-blue-100 px-1.5 py-0.5 text-xs text-blue-700 dark:bg-blue-900 dark:text-blue-300">
                        default
                      </span>
                    }
                  </div>
                  <div class="truncate text-xs text-gray-500 dark:text-gray-400">
                    {{ repo.path }}
                  </div>
                </div>
                @if (repo.id === repositoryStore.selectedId()) {
                  <svg
                    class="h-4 w-4 flex-shrink-0 text-blue-600 dark:text-blue-400"
                    viewBox="0 0 20 20"
                    fill="currentColor"
                    aria-hidden="true"
                  >
                    <path fill-rule="evenodd" d="M16.704 4.153a.75.75 0 01.143 1.052l-8 10.5a.75.75 0 01-1.127.075l-4.5-4.5a.75.75 0 011.06-1.06l3.894 3.893 7.48-9.817a.75.75 0 011.05-.143z" clip-rule="evenodd" />
                  </svg>
                }
              </button>
            }
          </div>
        }
      </div>
    }
  `,
  styles: `
    :host {
      display: block;
    }
  `,
  host: {
    '(document:click)': 'onDocumentClick($event)',
  },
})
export class RepositoryInfoComponent {
  protected readonly repositoryStore = inject(RepositoryStore);
  protected readonly isDropdownOpen = signal(false);

  toggleDropdown(): void {
    // Only toggle if there are multiple repositories
    if (this.repositoryStore.activeRepositories().length > 1) {
      this.isDropdownOpen.update(open => !open);
    }
  }

  selectRepository(id: string): void {
    this.repositoryStore.setSelectedRepository(id);
    this.isDropdownOpen.set(false);
  }

  showAddRepoPrompt(): void {
    // TODO: Show add repository dialog
    alert('Repository management coming soon. Please use the API to add repositories.');
  }

  onDocumentClick(event: MouseEvent): void {
    // Close dropdown when clicking outside
    const target = event.target as HTMLElement;
    if (!target.closest('app-repository-info')) {
      this.isDropdownOpen.set(false);
    }
  }
}
