import { Injectable, computed, inject, signal } from '@angular/core';
import { RepositoryInfo } from '../../shared/models';
import { RepositoryService } from '../services/repository.service';
import { firstValueFrom } from 'rxjs';

@Injectable({ providedIn: 'root' })
export class RepositoryStore {
  private readonly repositoryService = inject(RepositoryService);

  // State
  private readonly _info = signal<RepositoryInfo | null>(null);
  private readonly _loading = signal(false);
  private readonly _error = signal<string | null>(null);

  // Public readonly signals
  readonly info = this._info.asReadonly();
  readonly loading = this._loading.asReadonly();
  readonly error = this._error.asReadonly();

  // Computed signals
  readonly name = computed(() => this._info()?.name ?? '');
  readonly path = computed(() => this._info()?.path ?? '');
  readonly branch = computed(() => this._info()?.branch);
  readonly commitHash = computed(() => this._info()?.commitHash);
  readonly remoteUrl = computed(() => this._info()?.remoteUrl);
  readonly isDirty = computed(() => this._info()?.isDirty ?? false);
  readonly isGitRepository = computed(() => this._info()?.isGitRepository ?? false);

  // Truncate branch name for display (max 30 chars)
  readonly displayBranch = computed(() => {
    const branch = this.branch();
    if (!branch) return null;
    if (branch.length <= 30) return branch;
    return branch.substring(0, 27) + '...';
  });

  // Actions
  async loadInfo(): Promise<void> {
    this._loading.set(true);
    this._error.set(null);

    try {
      const info = await firstValueFrom(this.repositoryService.getInfo());
      this._info.set(info);
    } catch (err) {
      this._error.set(err instanceof Error ? err.message : 'Failed to load repository info');
    } finally {
      this._loading.set(false);
    }
  }
}
