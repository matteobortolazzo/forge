import { Injectable, computed, inject, signal } from '@angular/core';
import {
  Task,
  PipelineState,
  PIPELINE_STATES,
  CreateTaskDto,
  UpdateTaskDto,
  TransitionTaskDto,
  CreateSubtaskDto,
  SplitTaskDto,
} from '../../shared/models';
import { TaskService } from '../services/task.service';
import { firstValueFrom } from 'rxjs';

@Injectable({ providedIn: 'root' })
export class TaskStore {
  private readonly taskService = inject(TaskService);

  // State
  private readonly tasks = signal<Task[]>([]);
  private readonly loading = signal(false);
  private readonly error = signal<string | null>(null);

  // Public readonly signals
  readonly isLoading = this.loading.asReadonly();
  readonly errorMessage = this.error.asReadonly();
  readonly allTasks = this.tasks.asReadonly();

  // Computed: tasks grouped by state
  readonly tasksByState = computed(() => {
    const grouped: Record<PipelineState, Task[]> = {} as Record<PipelineState, Task[]>;

    // Initialize all states with empty arrays
    for (const state of PIPELINE_STATES) {
      grouped[state] = [];
    }

    // Group tasks by state
    for (const task of this.tasks()) {
      grouped[task.state].push(task);
    }

    return grouped;
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

  // Computed: total task count
  readonly totalTaskCount = computed(() => this.tasks().length);

  // Computed: tasks with errors
  readonly tasksWithErrors = computed(() => this.tasks().filter(t => t.hasError));

  // Computed: tasks with active agents
  readonly tasksWithAgents = computed(() => this.tasks().filter(t => t.assignedAgentId));

  // Computed: paused tasks
  readonly pausedTasks = computed(() => this.tasks().filter(t => t.isPaused));

  // Computed: root tasks only (tasks without parents)
  readonly rootTasks = computed(() => this.tasks().filter(t => !t.parentId));

  // Computed: children grouped by parent ID
  readonly childrenByParent = computed(() => {
    const map = new Map<string, Task[]>();
    for (const task of this.tasks()) {
      if (task.parentId) {
        const children = map.get(task.parentId) ?? [];
        children.push(task);
        map.set(task.parentId, children);
      }
    }
    return map;
  });

  // Computed: leaf tasks only (tasks without children)
  readonly leafTasks = computed(() => this.tasks().filter(t => t.childCount === 0));

  // Computed: parent tasks only (tasks with children)
  readonly parentTasks = computed(() => this.tasks().filter(t => t.childCount > 0));

  // Get children for a specific parent
  getChildrenOf(parentId: string): Task[] {
    return this.childrenByParent().get(parentId) ?? [];
  }

  // Get display state (derived state for parents, regular state for leaves)
  getDisplayState(task: Task): PipelineState {
    return task.derivedState ?? task.state;
  }

  // Check if task is a leaf task
  isLeafTask(task: Task): boolean {
    return task.childCount === 0;
  }

  // Check if task is a parent task
  isParentTask(task: Task): boolean {
    return task.childCount > 0;
  }

  // Actions
  async loadTasks(): Promise<void> {
    this.loading.set(true);
    this.error.set(null);

    try {
      const tasks = await firstValueFrom(this.taskService.getTasks());
      this.tasks.set(tasks);
    } catch (err) {
      this.error.set(err instanceof Error ? err.message : 'Failed to load tasks');
    } finally {
      this.loading.set(false);
    }
  }

  async createTask(dto: CreateTaskDto): Promise<Task | null> {
    this.error.set(null);

    try {
      const newTask = await firstValueFrom(this.taskService.createTask(dto));
      this.tasks.update(tasks => [newTask, ...tasks]);
      return newTask;
    } catch (err) {
      this.error.set(err instanceof Error ? err.message : 'Failed to create task');
      return null;
    }
  }

  async updateTask(id: string, dto: UpdateTaskDto): Promise<Task | null> {
    this.error.set(null);

    try {
      const updatedTask = await firstValueFrom(this.taskService.updateTask(id, dto));
      this.tasks.update(tasks =>
        tasks.map(t => (t.id === id ? updatedTask : t))
      );
      return updatedTask;
    } catch (err) {
      this.error.set(err instanceof Error ? err.message : 'Failed to update task');
      return null;
    }
  }

  async deleteTask(id: string): Promise<boolean> {
    this.error.set(null);

    try {
      await firstValueFrom(this.taskService.deleteTask(id));
      this.tasks.update(tasks => tasks.filter(t => t.id !== id));
      return true;
    } catch (err) {
      this.error.set(err instanceof Error ? err.message : 'Failed to delete task');
      return false;
    }
  }

  async transitionTask(id: string, targetState: PipelineState): Promise<Task | null> {
    this.error.set(null);

    try {
      const dto: TransitionTaskDto = { targetState };
      const updatedTask = await firstValueFrom(this.taskService.transitionTask(id, dto));
      this.tasks.update(tasks =>
        tasks.map(t => (t.id === id ? updatedTask : t))
      );
      return updatedTask;
    } catch (err) {
      this.error.set(err instanceof Error ? err.message : 'Failed to transition task');
      return null;
    }
  }

  async abortAgent(taskId: string): Promise<Task | null> {
    this.error.set(null);

    try {
      const updatedTask = await firstValueFrom(this.taskService.abortAgent(taskId));
      this.tasks.update(tasks =>
        tasks.map(t => (t.id === taskId ? updatedTask : t))
      );
      return updatedTask;
    } catch (err) {
      this.error.set(err instanceof Error ? err.message : 'Failed to abort agent');
      return null;
    }
  }

  async startAgent(taskId: string): Promise<Task | null> {
    this.error.set(null);

    try {
      const updatedTask = await firstValueFrom(this.taskService.startAgent(taskId));
      this.tasks.update(tasks =>
        tasks.map(t => (t.id === taskId ? updatedTask : t))
      );
      return updatedTask;
    } catch (err) {
      this.error.set(err instanceof Error ? err.message : 'Failed to start agent');
      return null;
    }
  }

  async pauseTask(id: string, reason: string): Promise<Task | null> {
    this.error.set(null);

    try {
      const updatedTask = await firstValueFrom(this.taskService.pauseTask(id, { reason }));
      this.tasks.update(tasks =>
        tasks.map(t => (t.id === id ? updatedTask : t))
      );
      return updatedTask;
    } catch (err) {
      this.error.set(err instanceof Error ? err.message : 'Failed to pause task');
      return null;
    }
  }

  async resumeTask(id: string): Promise<Task | null> {
    this.error.set(null);

    try {
      const updatedTask = await firstValueFrom(this.taskService.resumeTask(id));
      this.tasks.update(tasks =>
        tasks.map(t => (t.id === id ? updatedTask : t))
      );
      return updatedTask;
    } catch (err) {
      this.error.set(err instanceof Error ? err.message : 'Failed to resume task');
      return null;
    }
  }

  // Get a single task by ID
  getTaskById(id: string): Task | undefined {
    return this.tasks().find(t => t.id === id);
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

  // Hierarchy actions
  async splitTask(taskId: string, subtasks: CreateSubtaskDto[]): Promise<boolean> {
    this.error.set(null);

    try {
      const dto: SplitTaskDto = { subtasks };
      const result = await firstValueFrom(this.taskService.splitTask(taskId, dto));

      // Update parent and add children to the store
      this.tasks.update(tasks => {
        const updatedTasks = tasks.map(t =>
          t.id === taskId ? result.parent : t
        );
        return [...updatedTasks, ...result.children];
      });

      return true;
    } catch (err) {
      this.error.set(err instanceof Error ? err.message : 'Failed to split task');
      return false;
    }
  }

  async addChild(parentId: string, dto: CreateSubtaskDto): Promise<Task | null> {
    this.error.set(null);

    try {
      const child = await firstValueFrom(this.taskService.addChild(parentId, dto));

      // Add child to store and update parent's childCount
      this.tasks.update(tasks => {
        const updatedTasks = tasks.map(t => {
          if (t.id === parentId) {
            return { ...t, childCount: t.childCount + 1, updatedAt: new Date() };
          }
          return t;
        });
        return [...updatedTasks, child];
      });

      return child;
    } catch (err) {
      this.error.set(err instanceof Error ? err.message : 'Failed to add child task');
      return null;
    }
  }

  // Handle task:split SSE event
  handleTaskSplitEvent(parent: Task, children: Task[]): void {
    this.tasks.update(tasks => {
      const updatedTasks = tasks.map(t =>
        t.id === parent.id ? parent : t
      );
      // Add children that don't already exist
      const existingIds = new Set(updatedTasks.map(t => t.id));
      const newChildren = children.filter(c => !existingIds.has(c.id));
      return [...updatedTasks, ...newChildren];
    });
  }

  // Handle task:childAdded SSE event
  handleChildAddedEvent(parentId: string, child: Task): void {
    this.tasks.update(tasks => {
      const updatedTasks = tasks.map(t => {
        if (t.id === parentId) {
          return { ...t, childCount: t.childCount + 1, updatedAt: new Date() };
        }
        return t;
      });
      // Add child if not already in store
      const exists = updatedTasks.some(t => t.id === child.id);
      if (!exists) {
        return [...updatedTasks, child];
      }
      return updatedTasks;
    });
  }
}
