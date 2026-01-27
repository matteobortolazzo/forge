import { Component, ChangeDetectionStrategy, inject, output } from '@angular/core';
import { RepositoryStore } from '../../../core/stores/repository.store';
import { Repository } from '../../models';

@Component({
  selector: 'app-repository-sidebar',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <aside class="flex h-full w-[72px] flex-col items-center bg-gray-900 py-3">
      <!-- Repository List -->
      <nav class="flex flex-1 flex-col items-center gap-2" aria-label="Repositories">
        @for (repo of repositoryStore.repositoriesWithInitials(); track repo.id) {
          <div class="group relative flex items-center">
            <!-- Selection Indicator (Discord-style pill) -->
            <div
              class="absolute left-0 h-5 w-1 rounded-r-full bg-white transition-all duration-200"
              [class.h-2]="repo.id !== repositoryStore.selectedId()"
              [class.opacity-0]="repo.id !== repositoryStore.selectedId() && !isHovered(repo.id)"
              [class.group-hover:h-5]="repo.id !== repositoryStore.selectedId()"
              [class.group-hover:opacity-100]="repo.id !== repositoryStore.selectedId()"
              aria-hidden="true"
            ></div>

            <!-- Repository Circle -->
            <button
              type="button"
              class="ml-3 flex h-12 w-12 items-center justify-center rounded-[24px] bg-gray-700 text-sm font-semibold text-gray-300 transition-all duration-200 hover:rounded-[16px] hover:bg-indigo-500 hover:text-white focus:outline-none focus-visible:ring-2 focus-visible:ring-indigo-400 focus-visible:ring-offset-2 focus-visible:ring-offset-gray-900"
              [class.rounded-[16px]]="repo.id === repositoryStore.selectedId()"
              [class.bg-indigo-500]="repo.id === repositoryStore.selectedId()"
              [class.text-white]="repo.id === repositoryStore.selectedId()"
              [attr.aria-current]="repo.id === repositoryStore.selectedId() ? 'page' : null"
              [attr.aria-label]="repo.name"
              (click)="selectRepository(repo.id)"
              (contextmenu)="onRightClick($event, repo)"
              (mouseenter)="setHovered(repo.id)"
              (mouseleave)="clearHovered()"
            >
              {{ repo.initials }}
            </button>

            <!-- Tooltip -->
            <div
              class="pointer-events-none absolute left-full z-50 ml-4 rounded-md bg-gray-950 px-3 py-2 text-sm font-medium text-white opacity-0 shadow-lg transition-opacity group-hover:opacity-100"
              role="tooltip"
            >
              <div class="whitespace-nowrap">{{ repo.name }}</div>
              @if (repo.isDefault) {
                <div class="text-xs text-gray-400">Default</div>
              }
              <div class="mt-1 text-xs text-gray-500">Right-click for settings</div>
              <!-- Arrow -->
              <div class="absolute left-0 top-1/2 -ml-1 h-2 w-2 -translate-y-1/2 rotate-45 bg-gray-950" aria-hidden="true"></div>
            </div>
          </div>
        }
      </nav>

      <!-- Separator -->
      <div class="mx-auto my-2 h-0.5 w-8 rounded-full bg-gray-700" aria-hidden="true"></div>

      <!-- Add Repository Button -->
      <div class="group relative flex items-center">
        <button
          type="button"
          class="ml-3 flex h-12 w-12 items-center justify-center rounded-[24px] bg-gray-700 text-gray-300 transition-all duration-200 hover:rounded-[16px] hover:bg-emerald-500 hover:text-white focus:outline-none focus-visible:ring-2 focus-visible:ring-emerald-400 focus-visible:ring-offset-2 focus-visible:ring-offset-gray-900"
          aria-label="Add repository"
          (click)="onAddRepository()"
        >
          <svg class="h-6 w-6" viewBox="0 0 24 24" fill="currentColor" aria-hidden="true">
            <path d="M12 4.5a.75.75 0 01.75.75v6h6a.75.75 0 010 1.5h-6v6a.75.75 0 01-1.5 0v-6h-6a.75.75 0 010-1.5h6v-6A.75.75 0 0112 4.5z" />
          </svg>
        </button>

        <!-- Tooltip -->
        <div
          class="pointer-events-none absolute left-full z-50 ml-4 whitespace-nowrap rounded-md bg-gray-950 px-3 py-2 text-sm font-medium text-white opacity-0 shadow-lg transition-opacity group-hover:opacity-100"
          role="tooltip"
        >
          Add Repository
          <!-- Arrow -->
          <div class="absolute left-0 top-1/2 -ml-1 h-2 w-2 -translate-y-1/2 rotate-45 bg-gray-950" aria-hidden="true"></div>
        </div>
      </div>
    </aside>
  `,
  styles: `
    :host {
      display: block;
      height: 100%;
    }
  `,
})
export class RepositorySidebarComponent {
  protected readonly repositoryStore = inject(RepositoryStore);

  readonly addRepository = output<void>();
  readonly openSettings = output<Repository>();

  private hoveredId: string | null = null;

  selectRepository(id: string): void {
    this.repositoryStore.setSelectedRepository(id);
  }

  onAddRepository(): void {
    this.addRepository.emit();
  }

  onOpenSettings(repo: Repository): void {
    this.openSettings.emit(repo);
  }

  onRightClick(event: MouseEvent, repo: Repository): void {
    event.preventDefault();
    this.onOpenSettings(repo);
  }

  setHovered(id: string): void {
    this.hoveredId = id;
  }

  clearHovered(): void {
    this.hoveredId = null;
  }

  isHovered(id: string): boolean {
    return this.hoveredId === id;
  }
}
