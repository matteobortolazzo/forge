import { Component, ChangeDetectionStrategy, input, output, signal, computed } from '@angular/core';
import { Repository } from '../../models';

@Component({
  selector: 'app-repository-settings-dialog',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    @if (isOpen()) {
      <div
        class="fixed inset-0 z-50 flex items-center justify-center"
        role="dialog"
        aria-modal="true"
        aria-labelledby="settings-dialog-title"
      >
        <!-- Backdrop -->
        <div
          class="absolute inset-0 bg-black/50 transition-opacity"
          (click)="onClose()"
          aria-hidden="true"
        ></div>

        <!-- Dialog Panel -->
        <div class="relative w-full max-w-lg rounded-lg bg-white p-6 shadow-xl dark:bg-gray-800">
          @if (!showDeleteConfirm()) {
            <!-- Main Settings View -->
            <h2
              id="settings-dialog-title"
              class="text-lg font-semibold text-gray-900 dark:text-gray-100"
            >
              Repository Settings
            </h2>

            @if (repository(); as repo) {
              <div class="mt-4 space-y-4">
                <!-- Repository Info -->
                <div class="rounded-lg border border-gray-200 bg-gray-50 p-4 dark:border-gray-700 dark:bg-gray-900">
                  <div class="flex items-start gap-3">
                    <!-- Icon -->
                    <div class="flex h-10 w-10 flex-shrink-0 items-center justify-center rounded-lg bg-indigo-100 dark:bg-indigo-900">
                      <svg class="h-5 w-5 text-indigo-600 dark:text-indigo-400" viewBox="0 0 16 16" fill="currentColor" aria-hidden="true">
                        <path d="M2 2.5A2.5 2.5 0 014.5 0h8.75a.75.75 0 01.75.75v12.5a.75.75 0 01-.75.75h-2.5a.75.75 0 110-1.5h1.75v-2h-8a1 1 0 00-.714 1.7.75.75 0 01-1.072 1.05A2.495 2.495 0 012 11.5v-9zm10.5-1V9h-8c-.356 0-.694.074-1 .208V2.5a1 1 0 011-1h8zM5 12.25v3.25a.25.25 0 00.4.2l1.45-1.087a.25.25 0 01.3 0L8.6 15.7a.25.25 0 00.4-.2v-3.25a.25.25 0 00-.25-.25h-3.5a.25.25 0 00-.25.25z" />
                      </svg>
                    </div>

                    <!-- Details -->
                    <div class="min-w-0 flex-1">
                      <div class="flex items-center gap-2">
                        <h3 class="font-medium text-gray-900 dark:text-gray-100">
                          {{ repo.name }}
                        </h3>
                      </div>
                      <p class="mt-1 truncate font-mono text-sm text-gray-500 dark:text-gray-400" [title]="repo.path">
                        {{ repo.path }}
                      </p>
                    </div>
                  </div>
                </div>

                <!-- Git Info -->
                @if (repo.isGitRepository) {
                  <div class="space-y-2">
                    <h4 class="text-sm font-medium text-gray-700 dark:text-gray-300">Git Information</h4>
                    <div class="grid grid-cols-2 gap-3 text-sm">
                      <div>
                        <span class="text-gray-500 dark:text-gray-400">Branch:</span>
                        <span class="ml-2 font-medium text-gray-900 dark:text-gray-100">{{ repo.branch ?? 'Unknown' }}</span>
                        @if (repo.isDirty) {
                          <span class="ml-1 text-amber-600 dark:text-amber-400">(modified)</span>
                        }
                      </div>
                      @if (repo.commitHash) {
                        <div>
                          <span class="text-gray-500 dark:text-gray-400">Commit:</span>
                          <span class="ml-2 font-mono text-xs text-gray-900 dark:text-gray-100">{{ repo.commitHash.substring(0, 7) }}</span>
                        </div>
                      }
                    </div>
                  </div>
                } @else {
                  <div class="rounded-md bg-amber-50 p-3 dark:bg-amber-900/20">
                    <p class="text-sm text-amber-700 dark:text-amber-400">
                      This directory is not a Git repository.
                    </p>
                  </div>
                }

                <!-- Task Count -->
                <div class="flex items-center justify-between rounded-lg border border-gray-200 px-4 py-3 dark:border-gray-700">
                  <span class="text-sm text-gray-600 dark:text-gray-400">Tasks in this repository</span>
                  <span class="font-medium text-gray-900 dark:text-gray-100">{{ repo.taskCount }}</span>
                </div>
              </div>

              <!-- Actions -->
              <div class="mt-6 flex flex-wrap items-center gap-3">
                <!-- Refresh Git Info -->
                <button
                  type="button"
                  class="inline-flex items-center gap-2 rounded-md border border-gray-300 bg-white px-3 py-2 text-sm font-medium text-gray-700 hover:bg-gray-50 focus:outline-none focus:ring-2 focus:ring-blue-500 focus:ring-offset-2 disabled:opacity-50 dark:border-gray-600 dark:bg-gray-700 dark:text-gray-300 dark:hover:bg-gray-600"
                  [disabled]="isRefreshing()"
                  (click)="onRefresh()"
                >
                  <svg
                    class="h-4 w-4"
                    [class.animate-spin]="isRefreshing()"
                    viewBox="0 0 20 20"
                    fill="currentColor"
                    aria-hidden="true"
                  >
                    <path fill-rule="evenodd" d="M15.312 11.424a5.5 5.5 0 01-9.201 2.466l-.312-.311h2.433a.75.75 0 000-1.5H3.989a.75.75 0 00-.75.75v4.242a.75.75 0 001.5 0v-2.43l.31.31a7 7 0 0011.712-3.138.75.75 0 00-1.449-.39zm1.23-3.723a.75.75 0 00.219-.53V2.929a.75.75 0 00-1.5 0v2.43l-.31-.31A7 7 0 003.239 8.188a.75.75 0 101.448.389A5.5 5.5 0 0113.89 6.11l.311.31h-2.432a.75.75 0 000 1.5h4.243a.75.75 0 00.53-.219z" clip-rule="evenodd" />
                  </svg>
                  @if (isRefreshing()) {
                    Refreshing...
                  } @else {
                    Refresh Git Info
                  }
                </button>

                <div class="flex-1"></div>

                <!-- Delete -->
                <button
                  type="button"
                  class="inline-flex items-center gap-2 rounded-md border border-red-300 bg-white px-3 py-2 text-sm font-medium text-red-700 hover:bg-red-50 focus:outline-none focus:ring-2 focus:ring-red-500 focus:ring-offset-2 dark:border-red-700 dark:bg-gray-700 dark:text-red-400 dark:hover:bg-red-900/20"
                  (click)="showDeleteConfirm.set(true)"
                >
                  <svg class="h-4 w-4" viewBox="0 0 20 20" fill="currentColor" aria-hidden="true">
                    <path fill-rule="evenodd" d="M8.75 1A2.75 2.75 0 006 3.75v.443c-.795.077-1.584.176-2.365.298a.75.75 0 10.23 1.482l.149-.022.841 10.518A2.75 2.75 0 007.596 19h4.807a2.75 2.75 0 002.742-2.53l.841-10.519.149.023a.75.75 0 00.23-1.482A41.03 41.03 0 0014 4.193V3.75A2.75 2.75 0 0011.25 1h-2.5zM10 4c.84 0 1.673.025 2.5.075V3.75c0-.69-.56-1.25-1.25-1.25h-2.5c-.69 0-1.25.56-1.25 1.25v.325C8.327 4.025 9.16 4 10 4zM8.58 7.72a.75.75 0 00-1.5.06l.3 7.5a.75.75 0 101.5-.06l-.3-7.5zm4.34.06a.75.75 0 10-1.5-.06l-.3 7.5a.75.75 0 101.5.06l.3-7.5z" clip-rule="evenodd" />
                  </svg>
                  Delete
                </button>
              </div>

              <!-- Close Button -->
              <div class="mt-4 flex justify-end">
                <button
                  type="button"
                  class="rounded-md bg-gray-100 px-4 py-2 text-sm font-medium text-gray-700 hover:bg-gray-200 focus:outline-none focus:ring-2 focus:ring-blue-500 focus:ring-offset-2 dark:bg-gray-700 dark:text-gray-300 dark:hover:bg-gray-600"
                  (click)="onClose()"
                >
                  Close
                </button>
              </div>
            }
          } @else {
            <!-- Delete Confirmation View -->
            <h2
              id="settings-dialog-title"
              class="text-lg font-semibold text-red-600 dark:text-red-400"
            >
              Delete Repository
            </h2>

            @if (repository(); as repo) {
              <div class="mt-4">
                <p class="text-gray-700 dark:text-gray-300">
                  Are you sure you want to delete <strong>{{ repo.name }}</strong>?
                </p>

                @if (repo.taskCount > 0) {
                  <div class="mt-3 rounded-md bg-amber-50 p-3 dark:bg-amber-900/20">
                    <p class="text-sm text-amber-700 dark:text-amber-400">
                      This repository has <strong>{{ repo.taskCount }}</strong> task(s).
                      Deleting the repository will also remove all associated tasks.
                    </p>
                  </div>
                }

                <p class="mt-3 text-sm text-gray-500 dark:text-gray-400">
                  This action cannot be undone.
                </p>
              </div>

              <!-- Confirmation Actions -->
              <div class="mt-6 flex justify-end gap-3">
                <button
                  type="button"
                  class="rounded-md border border-gray-300 bg-white px-4 py-2 text-sm font-medium text-gray-700 hover:bg-gray-50 focus:outline-none focus:ring-2 focus:ring-blue-500 focus:ring-offset-2 dark:border-gray-600 dark:bg-gray-700 dark:text-gray-300 dark:hover:bg-gray-600"
                  (click)="showDeleteConfirm.set(false)"
                  [disabled]="isDeleting()"
                >
                  Cancel
                </button>
                <button
                  type="button"
                  class="rounded-md bg-red-600 px-4 py-2 text-sm font-medium text-white hover:bg-red-700 focus:outline-none focus:ring-2 focus:ring-red-500 focus:ring-offset-2 disabled:opacity-50"
                  [disabled]="isDeleting()"
                  (click)="onConfirmDelete()"
                >
                  @if (isDeleting()) {
                    Deleting...
                  } @else {
                    Delete Repository
                  }
                </button>
              </div>
            }
          }
        </div>
      </div>
    }
  `,
})
export class RepositorySettingsDialogComponent {
  readonly isOpen = input(false);
  readonly repository = input<Repository | null>(null);

  readonly close = output<void>();
  readonly delete = output<string>();
  readonly refresh = output<string>();

  readonly showDeleteConfirm = signal(false);
  readonly isDeleting = signal(false);
  readonly isRefreshing = signal(false);

  onClose(): void {
    this.showDeleteConfirm.set(false);
    this.close.emit();
  }

  onConfirmDelete(): void {
    const repo = this.repository();
    if (repo) {
      this.isDeleting.set(true);
      this.delete.emit(repo.id);
    }
  }

  onRefresh(): void {
    const repo = this.repository();
    if (repo) {
      this.isRefreshing.set(true);
      this.refresh.emit(repo.id);
    }
  }

  resetState(): void {
    this.showDeleteConfirm.set(false);
    this.isDeleting.set(false);
    this.isRefreshing.set(false);
  }
}
