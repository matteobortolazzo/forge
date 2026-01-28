import { Injectable, computed, inject, signal } from '@angular/core';
import {
  Task,
  PipelineState,
  PIPELINE_STATES,
  UpdateTaskDto,
  TransitionTaskDto,
} from '../../shared/models';
import { TaskService } from '../services/task.service';
import { RepositoryStore } from './repository.store';
import { firstValueFrom } from 'rxjs';
import { createAsyncState, runAsync } from './store-utils';

@Injectable({ providedIn: 'root' })
export class TaskStore {
  private readonly taskService = inject(TaskService);
  private readonly repositoryStore = inject(RepositoryStore);

  // State
  private readonly tasks = signal<Task[]>([]);
  private readonly asyncState = createAsyncState();
  private readonly _currentBacklogItemId = signal<string | null>(null);

  // Public readonly signals
  readonly isLoading = this.asyncState.loading.asReadonly();
  readonly errorMessage = this.asyncState.error.asReadonly();
  readonly allTasks = this.tasks.asReadonly();
  readonly currentBacklogItemId = this._currentBacklogItemId.asReadonly();

  // Computed: tasks for the current backlog item
  readonly backlogItemTasks = computed(() => {
    const backlogItemId = this._currentBacklogItemId();
    if (!backlogItemId) return [];
    return this.tasks().filter(t => t.backlogItemId === backlogItemId);
  });

  // Computed: tasks grouped by state (for current backlog item)
  readonly tasksByState = computed(() => {
    const grouped: Record<PipelineState, Task[]> = {} as Record<PipelineState, Task[]>;

    // Initialize all states with empty arrays
    for (const state of PIPELINE_STATES) {
      grouped[state] = [];
    }

    // Group tasks by state
    for (const task of this.backlogItemTasks()) {
      grouped[task.state].push(task);
    }

    return grouped;
  });

  // Computed: tasks sorted by execution order
  readonly tasksByExecutionOrder = computed(() => {
    return [...this.backlogItemTasks()].sort((a, b) => a.executionOrder - b.executionOrder);
  });

  // Computed: count of tasks per state
  readonly taskCountByState = computed(() => {
    const counts: Record<PipelineState, number> = {} as Record<PipelineState, number>;
    const grouped = this.tasksByState();

    for (const state of PIPELINE_STATES) {
      counts[state] = grouped[state].length;
    }

    return counts;
  });

  // Computed: total task count (for current backlog item)
  readonly totalTaskCount = computed(() => this.backlogItemTasks().length);

  // Computed: tasks with errors
  readonly tasksWithErrors = computed(() => this.backlogItemTasks().filter(t => t.hasError));

  // Computed: tasks with active agents
  readonly tasksWithAgents = computed(() => this.backlogItemTasks().filter(t => t.assignedAgentId));

  // Computed: paused tasks
  readonly pausedTasks = computed(() => this.backlogItemTasks().filter(t => t.isPaused));

  // Computed: completed tasks
  readonly completedTasks = computed(() => this.backlogItemTasks().filter(t => t.state === 'Done'));

  // Computed: active tasks (not done)
  readonly activeTasks = computed(() => this.backlogItemTasks().filter(t => t.state !== 'Done'));

  // Computed: tasks grouped by backlog item ID
  readonly tasksByBacklogItem = computed(() => {
    const map = new Map<string, Task[]>();
    for (const task of this.tasks()) {
      const backlogItemId = task.backlogItemId;
      const tasks = map.get(backlogItemId) ?? [];
      tasks.push(task);
      map.set(backlogItemId, tasks);
    }
    return map;
  });

  // Get the repository ID from context
  private getRepositoryId(): string {
    const repoId = this.repositoryStore.selectedId();
    if (!repoId) {
      throw new Error('No repository selected');
    }
    return repoId;
  }

  // Get current backlog item ID
  private getBacklogItemId(): string {
    const backlogItemId = this._currentBacklogItemId();
    if (!backlogItemId) {
      throw new Error('No backlog item selected');
    }
    return backlogItemId;
  }

  // Set the current backlog item context
  setBacklogItemContext(backlogItemId: string | null): void {
    this._currentBacklogItemId.set(backlogItemId);
  }

  // Actions
  async loadTasks(backlogItemId?: string): Promise<void> {
    const repoId = this.repositoryStore.selectedId();
    const itemId = backlogItemId ?? this._currentBacklogItemId();

    if (!repoId || !itemId) {
      return;
    }

    // Update context if a new backlog item ID was provided
    if (backlogItemId) {
      this._currentBacklogItemId.set(backlogItemId);
    }

    await runAsync(
      this.asyncState,
      async () => {
        const tasks = await firstValueFrom(this.taskService.getTasks(repoId, itemId));
        // Replace tasks for this backlog item, keep tasks from other backlog items
        this.tasks.update(existing => {
          const otherTasks = existing.filter(t => t.backlogItemId !== itemId);
          return [...otherTasks, ...tasks];
        });
      },
      {},
      'Failed to load tasks'
    );
  }

  async updateTask(id: string, dto: UpdateTaskDto): Promise<Task | null> {
    return runAsync(
      this.asyncState,
      async () => {
        const task = this.tasks().find(t => t.id === id);
        if (!task) {
          throw new Error('Task not found');
        }
        const updatedTask = await firstValueFrom(
          this.taskService.updateTask(task.repositoryId, task.backlogItemId, id, dto)
        );
        this.tasks.update(tasks =>
          tasks.map(t => (t.id === id ? updatedTask : t))
        );
        return updatedTask;
      },
      { setLoading: false },
      'Failed to update task'
    );
  }

  async deleteTask(id: string): Promise<boolean> {
    return (
      (await runAsync(
        this.asyncState,
        async () => {
          const task = this.tasks().find(t => t.id === id);
          if (!task) {
            throw new Error('Task not found');
          }
          await firstValueFrom(
            this.taskService.deleteTask(task.repositoryId, task.backlogItemId, id)
          );
          this.tasks.update(tasks => tasks.filter(t => t.id !== id));
          return true;
        },
        { setLoading: false },
        'Failed to delete task'
      )) ?? false
    );
  }

  async transitionTask(id: string, targetState: PipelineState): Promise<Task | null> {
    return runAsync(
      this.asyncState,
      async () => {
        const task = this.tasks().find(t => t.id === id);
        if (!task) {
          throw new Error('Task not found');
        }
        const dto: TransitionTaskDto = { targetState };
        const updatedTask = await firstValueFrom(
          this.taskService.transitionTask(task.repositoryId, task.backlogItemId, id, dto)
        );
        this.tasks.update(tasks =>
          tasks.map(t => (t.id === id ? updatedTask : t))
        );
        return updatedTask;
      },
      { setLoading: false },
      'Failed to transition task'
    );
  }

  async abortAgent(taskId: string): Promise<Task | null> {
    return runAsync(
      this.asyncState,
      async () => {
        const task = this.tasks().find(t => t.id === taskId);
        if (!task) {
          throw new Error('Task not found');
        }
        const updatedTask = await firstValueFrom(
          this.taskService.abortAgent(task.repositoryId, task.backlogItemId, taskId)
        );
        this.tasks.update(tasks =>
          tasks.map(t => (t.id === taskId ? updatedTask : t))
        );
        return updatedTask;
      },
      { setLoading: false },
      'Failed to abort agent'
    );
  }

  async startAgent(taskId: string): Promise<Task | null> {
    return runAsync(
      this.asyncState,
      async () => {
        const task = this.tasks().find(t => t.id === taskId);
        if (!task) {
          throw new Error('Task not found');
        }
        const updatedTask = await firstValueFrom(
          this.taskService.startAgent(task.repositoryId, task.backlogItemId, taskId)
        );
        this.tasks.update(tasks =>
          tasks.map(t => (t.id === taskId ? updatedTask : t))
        );
        return updatedTask;
      },
      { setLoading: false },
      'Failed to start agent'
    );
  }

  async pauseTask(id: string, reason?: string): Promise<Task | null> {
    return runAsync(
      this.asyncState,
      async () => {
        const task = this.tasks().find(t => t.id === id);
        if (!task) {
          throw new Error('Task not found');
        }
        const updatedTask = await firstValueFrom(
          this.taskService.pauseTask(task.repositoryId, task.backlogItemId, id, { reason })
        );
        this.tasks.update(tasks =>
          tasks.map(t => (t.id === id ? updatedTask : t))
        );
        return updatedTask;
      },
      { setLoading: false },
      'Failed to pause task'
    );
  }

  async resumeTask(id: string): Promise<Task | null> {
    return runAsync(
      this.asyncState,
      async () => {
        const task = this.tasks().find(t => t.id === id);
        if (!task) {
          throw new Error('Task not found');
        }
        const updatedTask = await firstValueFrom(
          this.taskService.resumeTask(task.repositoryId, task.backlogItemId, id)
        );
        this.tasks.update(tasks =>
          tasks.map(t => (t.id === id ? updatedTask : t))
        );
        return updatedTask;
      },
      { setLoading: false },
      'Failed to resume task'
    );
  }

  // Get a single task by ID
  getTaskById(id: string): Task | undefined {
    return this.tasks().find(t => t.id === id);
  }

  // Get tasks for a specific backlog item
  getTasksForBacklogItem(backlogItemId: string): Task[] {
    return this.tasksByBacklogItem().get(backlogItemId) ?? [];
  }

  // Update task from SSE event
  updateTaskFromEvent(task: Task): void {
    this.tasks.update(tasks => {
      const index = tasks.findIndex(t => t.id === task.id);
      if (index === -1) {
        return [task, ...tasks];
      }
      return tasks.map(t => (t.id === task.id ? task : t));
    });
  }

  // Remove task from SSE event
  removeTaskFromEvent(taskId: string): void {
    this.tasks.update(tasks => tasks.filter(t => t.id !== taskId));
  }

  // Clear all tasks (useful when switching repositories)
  clearTasks(): void {
    this.tasks.set([]);
    this._currentBacklogItemId.set(null);
  }
}
