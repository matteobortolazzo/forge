import { Injectable, computed, inject, signal, effect } from '@angular/core';
import {
  BacklogItem,
  BacklogItemState,
  BACKLOG_ITEM_STATES,
  CreateBacklogItemDto,
  UpdateBacklogItemDto,
  TransitionBacklogItemDto,
  PauseBacklogItemDto,
} from '../../shared/models';
import { BacklogService } from '../services/backlog.service';
import { RepositoryStore } from './repository.store';
import { firstValueFrom } from 'rxjs';
import { createAsyncState, runAsync } from './store-utils';

@Injectable({ providedIn: 'root' })
export class BacklogStore {
  private readonly backlogService = inject(BacklogService);
  private readonly repositoryStore = inject(RepositoryStore);

  // State
  private readonly items = signal<BacklogItem[]>([]);
  private readonly asyncState = createAsyncState();
  private readonly _selectedId = signal<string | null>(null);

  // Public readonly signals
  readonly isLoading = this.asyncState.loading.asReadonly();
  readonly errorMessage = this.asyncState.error.asReadonly();
  readonly allItems = this.items.asReadonly();
  readonly selectedId = this._selectedId.asReadonly();

  // Computed: items for the selected repository only
  readonly repositoryItems = computed(() => {
    const repoId = this.repositoryStore.selectedId();
    if (!repoId) return [];
    return this.items().filter(i => i.repositoryId === repoId);
  });

  // Computed: items grouped by state (for selected repository)
  readonly itemsByState = computed(() => {
    const grouped: Record<BacklogItemState, BacklogItem[]> = {} as Record<BacklogItemState, BacklogItem[]>;

    // Initialize all states with empty arrays
    for (const state of BACKLOG_ITEM_STATES) {
      grouped[state] = [];
    }

    // Group items by state
    for (const item of this.repositoryItems()) {
      grouped[item.state].push(item);
    }

    return grouped;
  });

  // Computed: count of items per state
  readonly itemCountByState = computed(() => {
    const counts: Record<BacklogItemState, number> = {} as Record<BacklogItemState, number>;
    const grouped = this.itemsByState();

    for (const state of BACKLOG_ITEM_STATES) {
      counts[state] = grouped[state].length;
    }

    return counts;
  });

  // Computed: total item count (for selected repository)
  readonly totalItemCount = computed(() => this.repositoryItems().length);

  // Computed: active items (not Done)
  readonly activeItems = computed(() =>
    this.repositoryItems().filter(i => i.state !== 'Done')
  );

  // Computed: completed items (Done)
  readonly completedItems = computed(() =>
    this.repositoryItems().filter(i => i.state === 'Done')
  );

  // Computed: items with errors
  readonly itemsWithErrors = computed(() =>
    this.repositoryItems().filter(i => i.hasError)
  );

  // Computed: items with active agents
  readonly itemsWithAgents = computed(() =>
    this.repositoryItems().filter(i => i.assignedAgentId)
  );

  // Computed: paused items
  readonly pausedItems = computed(() =>
    this.repositoryItems().filter(i => i.isPaused)
  );

  // Computed: items in progress (Refining, Splitting, or Executing)
  readonly inProgressItems = computed(() =>
    this.repositoryItems().filter(i =>
      i.state === 'Refining' || i.state === 'Splitting' || i.state === 'Executing'
    )
  );

  // Computed: items ready to work on (New or Ready)
  readonly readyItems = computed(() =>
    this.repositoryItems().filter(i =>
      i.state === 'New' || i.state === 'Ready'
    )
  );

  // Computed: selected item
  readonly selectedItem = computed(() => {
    const id = this._selectedId();
    if (!id) return null;
    return this.items().find(i => i.id === id) ?? null;
  });

  constructor() {
    // Auto-reload items when selected repository changes
    effect(() => {
      const repoId = this.repositoryStore.selectedId();
      if (repoId) {
        this.loadItems();
      }
    });
  }

  // Actions
  async loadItems(): Promise<void> {
    const repoId = this.repositoryStore.selectedId();
    if (!repoId) {
      this.items.set([]);
      return;
    }

    await runAsync(
      this.asyncState,
      async () => {
        const items = await firstValueFrom(this.backlogService.getAll(repoId));
        // Replace items for this repository, keep items from other repositories
        this.items.update(existing => {
          const otherRepoItems = existing.filter(i => i.repositoryId !== repoId);
          return [...otherRepoItems, ...items];
        });
      },
      {},
      'Failed to load backlog items'
    );
  }

  setSelectedItem(id: string | null): void {
    this._selectedId.set(id);
  }

  async createItem(dto: CreateBacklogItemDto): Promise<BacklogItem | null> {
    return runAsync(
      this.asyncState,
      async () => {
        const repoId = this.repositoryStore.selectedId();
        if (!repoId) {
          throw new Error('No repository selected');
        }
        const newItem = await firstValueFrom(this.backlogService.create(repoId, dto));
        // Only add if not already present (SSE event may have arrived first)
        this.items.update(items => {
          const exists = items.some(i => i.id === newItem.id);
          if (exists) {
            // SSE beat us - update instead of add
            return items.map(i => (i.id === newItem.id ? newItem : i));
          }
          return [newItem, ...items];
        });
        return newItem;
      },
      { setLoading: false },
      'Failed to create backlog item'
    );
  }

  async updateItem(id: string, dto: UpdateBacklogItemDto): Promise<BacklogItem | null> {
    return runAsync(
      this.asyncState,
      async () => {
        const item = this.items().find(i => i.id === id);
        if (!item) {
          throw new Error('Backlog item not found');
        }
        const updatedItem = await firstValueFrom(this.backlogService.update(item.repositoryId, id, dto));
        this.items.update(items =>
          items.map(i => {
            if (i.id !== id) return i;
            // Keep the newer data (SSE may have arrived with fresher state)
            return new Date(updatedItem.updatedAt) >= new Date(i.updatedAt) ? updatedItem : i;
          })
        );
        return updatedItem;
      },
      { setLoading: false },
      'Failed to update backlog item'
    );
  }

  async deleteItem(id: string): Promise<boolean> {
    return (
      (await runAsync(
        this.asyncState,
        async () => {
          const item = this.items().find(i => i.id === id);
          if (!item) {
            throw new Error('Backlog item not found');
          }
          await firstValueFrom(this.backlogService.delete(item.repositoryId, id));
          this.items.update(items => items.filter(i => i.id !== id));

          // Clear selection if we deleted the selected item
          if (this._selectedId() === id) {
            this._selectedId.set(null);
          }

          return true;
        },
        { setLoading: false },
        'Failed to delete backlog item'
      )) ?? false
    );
  }

  async transitionItem(id: string, targetState: BacklogItemState): Promise<BacklogItem | null> {
    return runAsync(
      this.asyncState,
      async () => {
        const item = this.items().find(i => i.id === id);
        if (!item) {
          throw new Error('Backlog item not found');
        }
        const dto: TransitionBacklogItemDto = { targetState };
        const updatedItem = await firstValueFrom(this.backlogService.transition(item.repositoryId, id, dto));
        this.items.update(items =>
          items.map(i => {
            if (i.id !== id) return i;
            // Keep the newer data (SSE may have arrived with fresher state)
            return new Date(updatedItem.updatedAt) >= new Date(i.updatedAt) ? updatedItem : i;
          })
        );
        return updatedItem;
      },
      { setLoading: false },
      'Failed to transition backlog item'
    );
  }

  async startAgent(id: string): Promise<BacklogItem | null> {
    return runAsync(
      this.asyncState,
      async () => {
        const item = this.items().find(i => i.id === id);
        if (!item) {
          throw new Error('Backlog item not found');
        }
        const updatedItem = await firstValueFrom(this.backlogService.startAgent(item.repositoryId, id));
        this.items.update(items =>
          items.map(i => {
            if (i.id !== id) return i;
            // Keep the newer data (SSE may have arrived with fresher state)
            return new Date(updatedItem.updatedAt) >= new Date(i.updatedAt) ? updatedItem : i;
          })
        );
        return updatedItem;
      },
      { setLoading: false },
      'Failed to start agent'
    );
  }

  async abortAgent(id: string): Promise<BacklogItem | null> {
    return runAsync(
      this.asyncState,
      async () => {
        const item = this.items().find(i => i.id === id);
        if (!item) {
          throw new Error('Backlog item not found');
        }
        const updatedItem = await firstValueFrom(this.backlogService.abortAgent(item.repositoryId, id));
        this.items.update(items =>
          items.map(i => {
            if (i.id !== id) return i;
            // Keep the newer data (SSE may have arrived with fresher state)
            return new Date(updatedItem.updatedAt) >= new Date(i.updatedAt) ? updatedItem : i;
          })
        );
        return updatedItem;
      },
      { setLoading: false },
      'Failed to abort agent'
    );
  }

  async pauseItem(id: string, reason?: string): Promise<BacklogItem | null> {
    return runAsync(
      this.asyncState,
      async () => {
        const item = this.items().find(i => i.id === id);
        if (!item) {
          throw new Error('Backlog item not found');
        }
        const dto: PauseBacklogItemDto = { reason };
        const updatedItem = await firstValueFrom(this.backlogService.pause(item.repositoryId, id, dto));
        this.items.update(items =>
          items.map(i => {
            if (i.id !== id) return i;
            // Keep the newer data (SSE may have arrived with fresher state)
            return new Date(updatedItem.updatedAt) >= new Date(i.updatedAt) ? updatedItem : i;
          })
        );
        return updatedItem;
      },
      { setLoading: false },
      'Failed to pause backlog item'
    );
  }

  async resumeItem(id: string): Promise<BacklogItem | null> {
    return runAsync(
      this.asyncState,
      async () => {
        const item = this.items().find(i => i.id === id);
        if (!item) {
          throw new Error('Backlog item not found');
        }
        const updatedItem = await firstValueFrom(this.backlogService.resume(item.repositoryId, id));
        this.items.update(items =>
          items.map(i => {
            if (i.id !== id) return i;
            // Keep the newer data (SSE may have arrived with fresher state)
            return new Date(updatedItem.updatedAt) >= new Date(i.updatedAt) ? updatedItem : i;
          })
        );
        return updatedItem;
      },
      { setLoading: false },
      'Failed to resume backlog item'
    );
  }

  // Get a single item by ID
  getItemById(id: string): BacklogItem | undefined {
    return this.items().find(i => i.id === id);
  }

  // Update item from SSE event
  updateItemFromEvent(item: BacklogItem): void {
    this.items.update(items => {
      const index = items.findIndex(i => i.id === item.id);
      if (index === -1) {
        return [item, ...items];
      }
      return items.map(i => (i.id === item.id ? item : i));
    });
  }

  // Remove item from SSE event
  removeItemFromEvent(itemId: string): void {
    this.items.update(items => items.filter(i => i.id !== itemId));

    // Clear selection if we deleted the selected item
    if (this._selectedId() === itemId) {
      this._selectedId.set(null);
    }
  }
}
