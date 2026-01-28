import { TestBed, fakeAsync, tick } from '@angular/core/testing';
import { describe, it, expect, beforeEach, vi } from 'vitest';
import { of, throwError } from 'rxjs';
import { BacklogStore } from './backlog.store';
import { BacklogService } from '../services/backlog.service';
import { RepositoryStore } from './repository.store';
import { BacklogItem, BacklogItemState } from '../../shared/models';
import { signal } from '@angular/core';

describe('BacklogStore', () => {
  let store: BacklogStore;
  let backlogServiceMock: {
    getAll: ReturnType<typeof vi.fn>;
    create: ReturnType<typeof vi.fn>;
    update: ReturnType<typeof vi.fn>;
    delete: ReturnType<typeof vi.fn>;
    transition: ReturnType<typeof vi.fn>;
    startAgent: ReturnType<typeof vi.fn>;
    abortAgent: ReturnType<typeof vi.fn>;
    pause: ReturnType<typeof vi.fn>;
    resume: ReturnType<typeof vi.fn>;
  };
  let repositoryStoreMock: {
    selectedId: ReturnType<typeof signal>;
  };

  const mockItem: BacklogItem = {
    id: 'item-1',
    title: 'Test Item',
    description: 'Test description',
    state: 'New' as BacklogItemState,
    priority: 'medium',
    createdAt: new Date(),
    updatedAt: new Date(),
    isPaused: false,
    hasError: false,
    hasPendingGate: false,
    taskCount: 0,
    completedTaskCount: 0,
    retryCount: 0,
    maxRetries: 3,
    refiningIterations: 0,
    repositoryId: 'repo-1',
  };

  beforeEach(() => {
    backlogServiceMock = {
      getAll: vi.fn().mockReturnValue(of([mockItem])),
      create: vi.fn().mockReturnValue(of(mockItem)),
      update: vi.fn().mockReturnValue(of(mockItem)),
      delete: vi.fn().mockReturnValue(of(void 0)),
      transition: vi.fn().mockReturnValue(of(mockItem)),
      startAgent: vi.fn().mockReturnValue(of(mockItem)),
      abortAgent: vi.fn().mockReturnValue(of(mockItem)),
      pause: vi.fn().mockReturnValue(of({ ...mockItem, isPaused: true })),
      resume: vi.fn().mockReturnValue(of(mockItem)),
    };

    repositoryStoreMock = {
      selectedId: signal<string | null>('repo-1'),
    };

    TestBed.configureTestingModule({
      providers: [
        BacklogStore,
        { provide: BacklogService, useValue: backlogServiceMock },
        { provide: RepositoryStore, useValue: repositoryStoreMock },
      ],
    });

    store = TestBed.inject(BacklogStore);
  });

  describe('initial state', () => {
    it('should have empty items', () => {
      expect(store.allItems()).toEqual([]);
    });

    it('should not be loading', () => {
      expect(store.isLoading()).toBe(false);
    });

    it('should have no error', () => {
      expect(store.errorMessage()).toBeNull();
    });

    it('should have no selected item', () => {
      expect(store.selectedId()).toBeNull();
    });
  });

  describe('loadItems', () => {
    it('should load items for selected repository', async () => {
      await store.loadItems();

      expect(backlogServiceMock.getAll).toHaveBeenCalledWith('repo-1');
      expect(store.allItems()).toHaveLength(1);
      expect(store.allItems()[0].id).toBe('item-1');
    });

    it('should handle errors', async () => {
      backlogServiceMock.getAll.mockReturnValue(
        throwError(() => new Error('Failed to fetch'))
      );

      await store.loadItems();

      expect(store.errorMessage()).toBe('Failed to fetch');
    });

    it('should clear items if no repository selected', async () => {
      repositoryStoreMock.selectedId.set(null);

      await store.loadItems();

      expect(backlogServiceMock.getAll).not.toHaveBeenCalled();
      expect(store.allItems()).toHaveLength(0);
    });
  });

  describe('createItem', () => {
    it('should create an item', async () => {
      const newItem = { ...mockItem, id: 'item-2', title: 'New Item' };
      backlogServiceMock.create.mockReturnValue(of(newItem));

      const result = await store.createItem({
        title: 'New Item',
        description: 'Description',
        priority: 'high',
      });

      expect(result?.id).toBe('item-2');
      expect(store.getItemById('item-2')).toBeDefined();
    });

    it('should return null if no repository selected', async () => {
      repositoryStoreMock.selectedId.set(null);

      const result = await store.createItem({
        title: 'Test',
        description: 'Test',
        priority: 'medium',
      });

      expect(result).toBeNull();
      expect(store.errorMessage()).toBe('No repository selected');
    });
  });

  describe('updateItem', () => {
    beforeEach(async () => {
      await store.loadItems();
    });

    it('should update an item', async () => {
      const updatedItem = { ...mockItem, title: 'Updated Title' };
      backlogServiceMock.update.mockReturnValue(of(updatedItem));

      const result = await store.updateItem('item-1', { title: 'Updated Title' });

      expect(result?.title).toBe('Updated Title');
      expect(store.getItemById('item-1')?.title).toBe('Updated Title');
    });

    it('should return null if item not found', async () => {
      const result = await store.updateItem('nonexistent', { title: 'Test' });

      expect(result).toBeNull();
      expect(store.errorMessage()).toBe('Backlog item not found');
    });
  });

  describe('deleteItem', () => {
    beforeEach(async () => {
      await store.loadItems();
    });

    it('should delete an item', async () => {
      const result = await store.deleteItem('item-1');

      expect(result).toBe(true);
      expect(store.getItemById('item-1')).toBeUndefined();
    });

    it('should clear selection if deleted item was selected', async () => {
      store.setSelectedItem('item-1');
      expect(store.selectedId()).toBe('item-1');

      await store.deleteItem('item-1');

      expect(store.selectedId()).toBeNull();
    });

    it('should return false if item not found', async () => {
      const result = await store.deleteItem('nonexistent');

      expect(result).toBe(false);
    });
  });

  describe('transitionItem', () => {
    beforeEach(async () => {
      await store.loadItems();
    });

    it('should transition an item to a new state', async () => {
      const transitionedItem = { ...mockItem, state: 'Ready' as BacklogItemState };
      backlogServiceMock.transition.mockReturnValue(of(transitionedItem));

      const result = await store.transitionItem('item-1', 'Ready');

      expect(result?.state).toBe('Ready');
    });
  });

  describe('startAgent', () => {
    beforeEach(async () => {
      await store.loadItems();
    });

    it('should start an agent for an item', async () => {
      const itemWithAgent = { ...mockItem, assignedAgentId: 'agent-1' };
      backlogServiceMock.startAgent.mockReturnValue(of(itemWithAgent));

      const result = await store.startAgent('item-1');

      expect(result?.assignedAgentId).toBe('agent-1');
    });
  });

  describe('abortAgent', () => {
    beforeEach(async () => {
      await store.loadItems();
    });

    it('should abort an agent for an item', async () => {
      const itemWithoutAgent = { ...mockItem, assignedAgentId: undefined };
      backlogServiceMock.abortAgent.mockReturnValue(of(itemWithoutAgent));

      const result = await store.abortAgent('item-1');

      expect(result?.assignedAgentId).toBeUndefined();
    });
  });

  describe('pauseItem', () => {
    beforeEach(async () => {
      await store.loadItems();
    });

    it('should pause an item', async () => {
      const pausedItem = { ...mockItem, isPaused: true, pauseReason: 'Test reason' };
      backlogServiceMock.pause.mockReturnValue(of(pausedItem));

      const result = await store.pauseItem('item-1', 'Test reason');

      expect(result?.isPaused).toBe(true);
    });
  });

  describe('resumeItem', () => {
    beforeEach(async () => {
      await store.loadItems();
    });

    it('should resume an item', async () => {
      const resumedItem = { ...mockItem, isPaused: false };
      backlogServiceMock.resume.mockReturnValue(of(resumedItem));

      const result = await store.resumeItem('item-1');

      expect(result?.isPaused).toBe(false);
    });
  });

  describe('computed values', () => {
    beforeEach(async () => {
      await store.loadItems();
    });

    it('should compute repositoryItems', () => {
      expect(store.repositoryItems()).toHaveLength(1);
    });

    it('should compute totalItemCount', () => {
      expect(store.totalItemCount()).toBe(1);
    });

    it('should compute itemsByState', () => {
      const byState = store.itemsByState();
      expect(byState['New']).toHaveLength(1);
    });

    it('should compute itemCountByState', () => {
      const counts = store.itemCountByState();
      expect(counts['New']).toBe(1);
      expect(counts['Done']).toBe(0);
    });

    it('should compute activeItems', () => {
      expect(store.activeItems()).toHaveLength(1);
    });

    it('should compute completedItems', () => {
      expect(store.completedItems()).toHaveLength(0);
    });

    it('should compute selectedItem', () => {
      store.setSelectedItem('item-1');
      expect(store.selectedItem()?.id).toBe('item-1');
    });
  });

  describe('SSE event handlers', () => {
    it('should update item from event', () => {
      const eventItem = { ...mockItem, title: 'Updated via SSE' };

      store.updateItemFromEvent(eventItem);

      expect(store.getItemById('item-1')?.title).toBe('Updated via SSE');
    });

    it('should add new item from event if not exists', () => {
      const newItem = { ...mockItem, id: 'item-2', title: 'New Item' };

      store.updateItemFromEvent(newItem);

      expect(store.getItemById('item-2')).toBeDefined();
    });

    it('should remove item from event', async () => {
      await store.loadItems();

      store.removeItemFromEvent('item-1');

      expect(store.getItemById('item-1')).toBeUndefined();
    });

    it('should clear selection when removing selected item via event', async () => {
      await store.loadItems();
      store.setSelectedItem('item-1');

      store.removeItemFromEvent('item-1');

      expect(store.selectedId()).toBeNull();
    });
  });

  describe('setSelectedItem', () => {
    it('should set the selected item ID', () => {
      store.setSelectedItem('item-1');
      expect(store.selectedId()).toBe('item-1');
    });

    it('should clear selection when null', () => {
      store.setSelectedItem('item-1');
      store.setSelectedItem(null);
      expect(store.selectedId()).toBeNull();
    });
  });
});
