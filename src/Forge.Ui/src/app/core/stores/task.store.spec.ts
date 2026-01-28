import { TestBed } from '@angular/core/testing';
import { describe, it, expect, beforeEach, vi } from 'vitest';
import { of, throwError } from 'rxjs';
import { TaskStore } from './task.store';
import { TaskService } from '../services/task.service';
import { RepositoryStore } from './repository.store';
import { Task, PipelineState } from '../../shared/models';

describe('TaskStore', () => {
  let store: TaskStore;
  let taskServiceMock: {
    getTasks: ReturnType<typeof vi.fn>;
    updateTask: ReturnType<typeof vi.fn>;
    deleteTask: ReturnType<typeof vi.fn>;
    transitionTask: ReturnType<typeof vi.fn>;
    startAgent: ReturnType<typeof vi.fn>;
    abortAgent: ReturnType<typeof vi.fn>;
    pauseTask: ReturnType<typeof vi.fn>;
    resumeTask: ReturnType<typeof vi.fn>;
    getTaskLogs: ReturnType<typeof vi.fn>;
  };
  let repositoryStoreMock: {
    selectedId: ReturnType<typeof vi.fn>;
  };

  const mockTask: Task = {
    id: 'task-1',
    title: 'Test Task',
    description: 'Test description',
    state: 'Research' as PipelineState,
    priority: 'medium',
    createdAt: new Date(),
    updatedAt: new Date(),
    retryCount: 0,
    maxRetries: 3,
    isPaused: false,
    hasError: false,
    hasPendingGate: false,
    executionOrder: 1,
    repositoryId: 'repo-1',
    backlogItemId: 'backlog-1',
  };

  beforeEach(() => {
    taskServiceMock = {
      getTasks: vi.fn().mockReturnValue(of([mockTask])),
      updateTask: vi.fn().mockReturnValue(of(mockTask)),
      deleteTask: vi.fn().mockReturnValue(of(void 0)),
      transitionTask: vi.fn().mockReturnValue(of(mockTask)),
      startAgent: vi.fn().mockReturnValue(of(mockTask)),
      abortAgent: vi.fn().mockReturnValue(of(mockTask)),
      pauseTask: vi.fn().mockReturnValue(of({ ...mockTask, isPaused: true })),
      resumeTask: vi.fn().mockReturnValue(of(mockTask)),
      getTaskLogs: vi.fn().mockReturnValue(of([])),
    };

    repositoryStoreMock = {
      selectedId: vi.fn().mockReturnValue('repo-1'),
    };

    TestBed.configureTestingModule({
      providers: [
        TaskStore,
        { provide: TaskService, useValue: taskServiceMock },
        { provide: RepositoryStore, useValue: repositoryStoreMock },
      ],
    });

    store = TestBed.inject(TaskStore);
  });

  describe('initial state', () => {
    it('should have empty tasks', () => {
      expect(store.allTasks()).toEqual([]);
    });

    it('should not be loading', () => {
      expect(store.isLoading()).toBe(false);
    });

    it('should have no error', () => {
      expect(store.errorMessage()).toBeNull();
    });
  });

  describe('loadTasks', () => {
    it('should load tasks for a backlog item', async () => {
      await store.loadTasks('backlog-1');

      expect(taskServiceMock.getTasks).toHaveBeenCalledWith('repo-1', 'backlog-1');
      expect(store.allTasks()).toHaveLength(1);
      expect(store.allTasks()[0].id).toBe('task-1');
    });

    it('should set backlog item context when provided', async () => {
      await store.loadTasks('backlog-2');

      expect(store.currentBacklogItemId()).toBe('backlog-2');
    });

    it('should handle errors', async () => {
      taskServiceMock.getTasks.mockReturnValue(
        throwError(() => new Error('Failed to fetch'))
      );

      await store.loadTasks('backlog-1');

      expect(store.errorMessage()).toBe('Failed to fetch');
    });

    it('should not load if no repository selected', async () => {
      repositoryStoreMock.selectedId.mockReturnValue(null);

      await store.loadTasks('backlog-1');

      expect(taskServiceMock.getTasks).not.toHaveBeenCalled();
    });
  });

  describe('updateTask', () => {
    beforeEach(async () => {
      await store.loadTasks('backlog-1');
    });

    it('should update a task', async () => {
      const updatedTask = { ...mockTask, title: 'Updated Title' };
      taskServiceMock.updateTask.mockReturnValue(of(updatedTask));

      const result = await store.updateTask('task-1', { title: 'Updated Title' });

      expect(result?.title).toBe('Updated Title');
      expect(store.getTaskById('task-1')?.title).toBe('Updated Title');
    });

    it('should return null if task not found', async () => {
      const result = await store.updateTask('nonexistent', { title: 'Test' });

      expect(result).toBeNull();
      expect(store.errorMessage()).toBe('Task not found');
    });
  });

  describe('deleteTask', () => {
    beforeEach(async () => {
      await store.loadTasks('backlog-1');
    });

    it('should delete a task', async () => {
      const result = await store.deleteTask('task-1');

      expect(result).toBe(true);
      expect(store.getTaskById('task-1')).toBeUndefined();
    });

    it('should return false if task not found', async () => {
      const result = await store.deleteTask('nonexistent');

      expect(result).toBe(false);
    });
  });

  describe('transitionTask', () => {
    beforeEach(async () => {
      await store.loadTasks('backlog-1');
    });

    it('should transition a task to a new state', async () => {
      const transitionedTask = { ...mockTask, state: 'Research' as PipelineState };
      taskServiceMock.transitionTask.mockReturnValue(of(transitionedTask));

      const result = await store.transitionTask('task-1', 'Research');

      expect(result?.state).toBe('Research');
    });
  });

  describe('startAgent', () => {
    beforeEach(async () => {
      await store.loadTasks('backlog-1');
    });

    it('should start an agent for a task', async () => {
      const taskWithAgent = { ...mockTask, assignedAgentId: 'agent-1' };
      taskServiceMock.startAgent.mockReturnValue(of(taskWithAgent));

      const result = await store.startAgent('task-1');

      expect(result?.assignedAgentId).toBe('agent-1');
    });
  });

  describe('abortAgent', () => {
    beforeEach(async () => {
      await store.loadTasks('backlog-1');
    });

    it('should abort an agent for a task', async () => {
      const taskWithoutAgent = { ...mockTask, assignedAgentId: undefined };
      taskServiceMock.abortAgent.mockReturnValue(of(taskWithoutAgent));

      const result = await store.abortAgent('task-1');

      expect(result?.assignedAgentId).toBeUndefined();
    });
  });

  describe('pauseTask', () => {
    beforeEach(async () => {
      await store.loadTasks('backlog-1');
    });

    it('should pause a task', async () => {
      const pausedTask = { ...mockTask, isPaused: true, pauseReason: 'Test reason' };
      taskServiceMock.pauseTask.mockReturnValue(of(pausedTask));

      const result = await store.pauseTask('task-1', 'Test reason');

      expect(result?.isPaused).toBe(true);
    });
  });

  describe('resumeTask', () => {
    beforeEach(async () => {
      await store.loadTasks('backlog-1');
    });

    it('should resume a task', async () => {
      const resumedTask = { ...mockTask, isPaused: false };
      taskServiceMock.resumeTask.mockReturnValue(of(resumedTask));

      const result = await store.resumeTask('task-1');

      expect(result?.isPaused).toBe(false);
    });
  });

  describe('computed values', () => {
    beforeEach(async () => {
      store.setBacklogItemContext('backlog-1');
      await store.loadTasks('backlog-1');
    });

    it('should compute backlogItemTasks', () => {
      expect(store.backlogItemTasks()).toHaveLength(1);
    });

    it('should compute totalTaskCount', () => {
      expect(store.totalTaskCount()).toBe(1);
    });

    it('should compute tasksByState', () => {
      const byState = store.tasksByState();
      expect(byState['Research']).toHaveLength(1);
    });
  });

  describe('SSE event handlers', () => {
    it('should update task from event', () => {
      const eventTask = { ...mockTask, title: 'Updated via SSE' };

      store.updateTaskFromEvent(eventTask);

      expect(store.getTaskById('task-1')?.title).toBe('Updated via SSE');
    });

    it('should add new task from event if not exists', () => {
      const newTask = { ...mockTask, id: 'task-2', title: 'New Task' };

      store.updateTaskFromEvent(newTask);

      expect(store.getTaskById('task-2')).toBeDefined();
    });

    it('should remove task from event', async () => {
      await store.loadTasks('backlog-1');

      store.removeTaskFromEvent('task-1');

      expect(store.getTaskById('task-1')).toBeUndefined();
    });
  });

  describe('clearTasks', () => {
    it('should clear all tasks and context', async () => {
      await store.loadTasks('backlog-1');
      expect(store.allTasks().length).toBeGreaterThan(0);

      store.clearTasks();

      expect(store.allTasks()).toHaveLength(0);
      expect(store.currentBacklogItemId()).toBeNull();
    });
  });
});
