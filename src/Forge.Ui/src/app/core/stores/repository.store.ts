import { Injectable, computed, inject, signal } from '@angular/core';
import { Repository, CreateRepositoryDto, UpdateRepositoryDto } from '../../shared/models';
import { RepositoryService } from '../services/repository.service';
import { firstValueFrom } from 'rxjs';

const SELECTED_REPO_KEY = 'forge:selectedRepositoryId';

@Injectable({ providedIn: 'root' })
export class RepositoryStore {
  private readonly repositoryService = inject(RepositoryService);

  // State
  private readonly _repositories = signal<Repository[]>([]);
  private readonly _selectedId = signal<string | null>(this.loadSelectedIdFromStorage());
  private readonly _loading = signal(false);
  private readonly _error = signal<string | null>(null);

  // Public readonly signals
  readonly repositories = this._repositories.asReadonly();
  readonly selectedId = this._selectedId.asReadonly();
  readonly loading = this._loading.asReadonly();
  readonly error = this._error.asReadonly();

  // Computed: selected repository
  readonly selectedRepository = computed(() => {
    const id = this._selectedId();
    if (!id) return null;
    return this._repositories().find(r => r.id === id) ?? null;
  });

  // Computed: default repository
  readonly defaultRepository = computed(() => {
    return this._repositories().find(r => r.isDefault) ?? this._repositories()[0] ?? null;
  });

  // Computed: active repositories only
  readonly activeRepositories = computed(() => {
    return this._repositories().filter(r => r.isActive);
  });

  // Computed: active repositories with initials for sidebar display
  readonly repositoriesWithInitials = computed(() =>
    this.activeRepositories().map(repo => ({
      ...repo,
      initials: this.getInitials(repo.name),
    }))
  );

  // Computed: whether we have any repositories
  readonly hasRepositories = computed(() => this._repositories().length > 0);

  // Computed: info from selected repository (for backward compatibility)
  readonly info = computed(() => this.selectedRepository() ?? this.defaultRepository());
  readonly name = computed(() => this.info()?.name ?? '');
  readonly path = computed(() => this.info()?.path ?? '');
  readonly branch = computed(() => this.info()?.branch);
  readonly commitHash = computed(() => this.info()?.commitHash);
  readonly remoteUrl = computed(() => this.info()?.remoteUrl);
  readonly isDirty = computed(() => this.info()?.isDirty ?? false);
  readonly isGitRepository = computed(() => this.info()?.isGitRepository ?? false);

  // Truncate branch name for display (max 30 chars)
  readonly displayBranch = computed(() => {
    const branch = this.branch();
    if (!branch) return null;
    if (branch.length <= 30) return branch;
    return branch.substring(0, 27) + '...';
  });

  // Actions
  async loadRepositories(): Promise<void> {
    this._loading.set(true);
    this._error.set(null);

    try {
      const repos = await firstValueFrom(this.repositoryService.getAll());
      this._repositories.set(repos);

      // Auto-select if no selection or selection invalid
      const currentId = this._selectedId();
      const validSelection = currentId && repos.some(r => r.id === currentId);
      if (!validSelection && repos.length > 0) {
        const defaultRepo = repos.find(r => r.isDefault) ?? repos[0];
        this.setSelectedRepository(defaultRepo.id);
      }
    } catch (err) {
      this._error.set(err instanceof Error ? err.message : 'Failed to load repositories');
    } finally {
      this._loading.set(false);
    }
  }

  // Legacy method for backward compatibility
  async loadInfo(): Promise<void> {
    await this.loadRepositories();
  }

  setSelectedRepository(id: string): void {
    this._selectedId.set(id);
    this.saveSelectedIdToStorage(id);
  }

  async createRepository(dto: CreateRepositoryDto): Promise<Repository | null> {
    this._error.set(null);

    try {
      const newRepo = await firstValueFrom(this.repositoryService.create(dto));
      this._repositories.update(repos => {
        // If new repo is default, update others
        if (newRepo.isDefault) {
          return [...repos.map(r => ({ ...r, isDefault: false })), newRepo];
        }
        return [...repos, newRepo];
      });

      // Auto-select if it's the first one or is default
      if (this._repositories().length === 1 || newRepo.isDefault) {
        this.setSelectedRepository(newRepo.id);
      }

      return newRepo;
    } catch (err) {
      this._error.set(err instanceof Error ? err.message : 'Failed to create repository');
      return null;
    }
  }

  async updateRepository(id: string, dto: UpdateRepositoryDto): Promise<Repository | null> {
    this._error.set(null);

    try {
      const updated = await firstValueFrom(this.repositoryService.update(id, dto));
      this._repositories.update(repos =>
        repos.map(r => (r.id === id ? updated : r))
      );
      return updated;
    } catch (err) {
      this._error.set(err instanceof Error ? err.message : 'Failed to update repository');
      return null;
    }
  }

  async deleteRepository(id: string): Promise<boolean> {
    this._error.set(null);

    try {
      await firstValueFrom(this.repositoryService.delete(id));
      this._repositories.update(repos => repos.filter(r => r.id !== id));

      // If deleted the selected one, select another
      if (this._selectedId() === id) {
        const remaining = this._repositories();
        if (remaining.length > 0) {
          const defaultRepo = remaining.find(r => r.isDefault) ?? remaining[0];
          this.setSelectedRepository(defaultRepo.id);
        } else {
          this._selectedId.set(null);
          this.clearSelectedIdFromStorage();
        }
      }

      return true;
    } catch (err) {
      this._error.set(err instanceof Error ? err.message : 'Failed to delete repository');
      return false;
    }
  }

  async refreshRepository(id: string): Promise<Repository | null> {
    this._error.set(null);

    try {
      const refreshed = await firstValueFrom(this.repositoryService.refresh(id));
      this._repositories.update(repos =>
        repos.map(r => (r.id === id ? refreshed : r))
      );
      return refreshed;
    } catch (err) {
      this._error.set(err instanceof Error ? err.message : 'Failed to refresh repository');
      return null;
    }
  }

  async setDefaultRepository(id: string): Promise<Repository | null> {
    this._error.set(null);

    try {
      const updated = await firstValueFrom(this.repositoryService.setDefault(id));
      this._repositories.update(repos =>
        repos.map(r => ({
          ...r,
          isDefault: r.id === id,
        }))
      );
      return updated;
    } catch (err) {
      this._error.set(err instanceof Error ? err.message : 'Failed to set default repository');
      return null;
    }
  }

  // SSE event handlers
  updateRepositoryFromEvent(repo: Repository): void {
    this._repositories.update(repos => {
      const index = repos.findIndex(r => r.id === repo.id);
      if (index === -1) {
        return [...repos, repo];
      }
      return repos.map(r => (r.id === repo.id ? repo : r));
    });
  }

  removeRepositoryFromEvent(id: string): void {
    this._repositories.update(repos => repos.filter(r => r.id !== id));

    // If deleted the selected one, select another
    if (this._selectedId() === id) {
      const remaining = this._repositories();
      if (remaining.length > 0) {
        const defaultRepo = remaining.find(r => r.isDefault) ?? remaining[0];
        this.setSelectedRepository(defaultRepo.id);
      } else {
        this._selectedId.set(null);
        this.clearSelectedIdFromStorage();
      }
    }
  }

  // Local storage helpers
  private loadSelectedIdFromStorage(): string | null {
    if (typeof localStorage === 'undefined') return null;
    return localStorage.getItem(SELECTED_REPO_KEY);
  }

  private saveSelectedIdToStorage(id: string): void {
    if (typeof localStorage === 'undefined') return;
    localStorage.setItem(SELECTED_REPO_KEY, id);
  }

  private clearSelectedIdFromStorage(): void {
    if (typeof localStorage === 'undefined') return;
    localStorage.removeItem(SELECTED_REPO_KEY);
  }

  /**
   * Get 2-character initials from repository name.
   * Single word: first letter (e.g., "Forge" → "F")
   * Multiple words: first letter of first two words (e.g., "My Project" → "MP")
   */
  getInitials(name: string): string {
    const words = name.trim().split(/\s+/);
    if (words.length === 1) {
      return words[0].charAt(0).toUpperCase();
    }
    return (words[0].charAt(0) + words[1].charAt(0)).toUpperCase();
  }
}
