import { Injectable, computed, inject, signal } from '@angular/core';
import { Task, PipelineState, PIPELINE_STATES, CreateTaskDto, UpdateTaskDto, TransitionTaskDto } from '../../shared/models';
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
}
