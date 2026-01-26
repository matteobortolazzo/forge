import {
  Component,
  ChangeDetectionStrategy,
  input,
  output,
  signal,
  effect,
} from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Priority, PRIORITIES, CreateSubtaskDto } from '../../shared/models';

interface SubtaskForm {
  title: string;
  description: string;
  priority: Priority;
}

@Component({
  selector: 'app-split-task-dialog',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [FormsModule],
  template: `
    <!-- Backdrop -->
    <div
      class="fixed inset-0 z-40 bg-black/50 transition-opacity"
      (click)="onCancel()"
      aria-hidden="true"
    ></div>

    <!-- Dialog -->
    <div
      class="fixed inset-0 z-50 flex items-center justify-center p-4"
      role="dialog"
      aria-modal="true"
      aria-labelledby="split-dialog-title"
    >
      <div
        class="w-full max-w-2xl rounded-lg bg-white shadow-xl dark:bg-gray-800"
        (click)="$event.stopPropagation()"
      >
        <!-- Header -->
        <div
          class="flex items-center justify-between border-b border-gray-200 px-6 py-4 dark:border-gray-700"
        >
          <div>
            <h2
              id="split-dialog-title"
              class="text-lg font-semibold text-gray-900 dark:text-gray-100"
            >
              Split Task into Subtasks
            </h2>
            <p class="mt-1 text-sm text-gray-500 dark:text-gray-400">
              Splitting: {{ taskTitle() }}
            </p>
          </div>
          <button
            type="button"
            class="rounded-md p-2 text-gray-400 hover:bg-gray-100 hover:text-gray-500 dark:hover:bg-gray-700"
            aria-label="Close dialog"
            (click)="onCancel()"
          >
            <svg
              class="h-5 w-5"
              xmlns="http://www.w3.org/2000/svg"
              viewBox="0 0 20 20"
              fill="currentColor"
            >
              <path
                d="M6.28 5.22a.75.75 0 00-1.06 1.06L8.94 10l-3.72 3.72a.75.75 0 101.06 1.06L10 11.06l3.72 3.72a.75.75 0 101.06-1.06L11.06 10l3.72-3.72a.75.75 0 00-1.06-1.06L10 8.94 6.28 5.22z"
              />
            </svg>
          </button>
        </div>

        <!-- Content -->
        <div class="max-h-96 overflow-y-auto px-6 py-4">
          <div class="space-y-4">
            @for (subtask of subtasks(); track $index; let i = $index) {
              <div
                class="rounded-lg border border-gray-200 p-4 dark:border-gray-700"
              >
                <div class="mb-3 flex items-center justify-between">
                  <span
                    class="text-sm font-medium text-gray-700 dark:text-gray-300"
                  >
                    Subtask {{ i + 1 }}
                  </span>
                  @if (subtasks().length > 1) {
                    <button
                      type="button"
                      class="rounded-md p-1 text-gray-400 hover:bg-gray-100 hover:text-red-500 dark:hover:bg-gray-700"
                      aria-label="Remove subtask"
                      (click)="removeSubtask(i)"
                    >
                      <svg
                        class="h-4 w-4"
                        xmlns="http://www.w3.org/2000/svg"
                        viewBox="0 0 20 20"
                        fill="currentColor"
                      >
                        <path
                          fill-rule="evenodd"
                          d="M8.75 1A2.75 2.75 0 006 3.75v.443c-.795.077-1.584.176-2.365.298a.75.75 0 10.23 1.482l.149-.022.841 10.518A2.75 2.75 0 007.596 19h4.807a2.75 2.75 0 002.742-2.53l.841-10.519.149.023a.75.75 0 00.23-1.482A41.03 41.03 0 0014 4.193V3.75A2.75 2.75 0 0011.25 1h-2.5zM10 4c.84 0 1.673.025 2.5.075V3.75c0-.69-.56-1.25-1.25-1.25h-2.5c-.69 0-1.25.56-1.25 1.25v.325C8.327 4.025 9.16 4 10 4zM8.58 7.72a.75.75 0 00-1.5.06l.3 7.5a.75.75 0 101.5-.06l-.3-7.5zm4.34.06a.75.75 0 10-1.5-.06l-.3 7.5a.75.75 0 101.5.06l.3-7.5z"
                          clip-rule="evenodd"
                        />
                      </svg>
                    </button>
                  }
                </div>

                <div class="space-y-3">
                  <!-- Title -->
                  <div>
                    <label
                      [for]="'subtask-title-' + i"
                      class="mb-1 block text-sm font-medium text-gray-700 dark:text-gray-300"
                    >
                      Title
                    </label>
                    <input
                      type="text"
                      [id]="'subtask-title-' + i"
                      class="w-full rounded-md border border-gray-300 px-3 py-2 text-sm focus:border-blue-500 focus:outline-none focus:ring-1 focus:ring-blue-500 dark:border-gray-600 dark:bg-gray-700 dark:text-gray-100"
                      placeholder="Enter subtask title"
                      [ngModel]="subtask.title"
                      (ngModelChange)="updateSubtask(i, 'title', $event)"
                    />
                  </div>

                  <!-- Description -->
                  <div>
                    <label
                      [for]="'subtask-desc-' + i"
                      class="mb-1 block text-sm font-medium text-gray-700 dark:text-gray-300"
                    >
                      Description
                    </label>
                    <textarea
                      [id]="'subtask-desc-' + i"
                      rows="2"
                      class="w-full rounded-md border border-gray-300 px-3 py-2 text-sm focus:border-blue-500 focus:outline-none focus:ring-1 focus:ring-blue-500 dark:border-gray-600 dark:bg-gray-700 dark:text-gray-100"
                      placeholder="Enter subtask description"
                      [ngModel]="subtask.description"
                      (ngModelChange)="updateSubtask(i, 'description', $event)"
                    ></textarea>
                  </div>

                  <!-- Priority -->
                  <div>
                    <label
                      [for]="'subtask-priority-' + i"
                      class="mb-1 block text-sm font-medium text-gray-700 dark:text-gray-300"
                    >
                      Priority
                    </label>
                    <select
                      [id]="'subtask-priority-' + i"
                      class="w-full rounded-md border border-gray-300 px-3 py-2 text-sm focus:border-blue-500 focus:outline-none focus:ring-1 focus:ring-blue-500 dark:border-gray-600 dark:bg-gray-700 dark:text-gray-100"
                      [ngModel]="subtask.priority"
                      (ngModelChange)="updateSubtask(i, 'priority', $event)"
                    >
                      @for (priority of priorities; track priority) {
                        <option [value]="priority">{{ priority }}</option>
                      }
                    </select>
                  </div>
                </div>
              </div>
            }
          </div>

          <!-- Add Subtask Button -->
          <button
            type="button"
            class="mt-4 flex w-full items-center justify-center gap-2 rounded-md border-2 border-dashed border-gray-300 px-4 py-3 text-sm text-gray-600 hover:border-gray-400 hover:text-gray-700 dark:border-gray-600 dark:text-gray-400 dark:hover:border-gray-500 dark:hover:text-gray-300"
            (click)="addSubtask()"
          >
            <svg
              class="h-4 w-4"
              xmlns="http://www.w3.org/2000/svg"
              viewBox="0 0 20 20"
              fill="currentColor"
            >
              <path
                d="M10.75 4.75a.75.75 0 00-1.5 0v4.5h-4.5a.75.75 0 000 1.5h4.5v4.5a.75.75 0 001.5 0v-4.5h4.5a.75.75 0 000-1.5h-4.5v-4.5z"
              />
            </svg>
            Add Another Subtask
          </button>
        </div>

        <!-- Footer -->
        <div
          class="flex items-center justify-end gap-3 border-t border-gray-200 px-6 py-4 dark:border-gray-700"
        >
          <button
            type="button"
            class="rounded-md border border-gray-300 bg-white px-4 py-2 text-sm font-medium text-gray-700 hover:bg-gray-50 focus:outline-none focus:ring-2 focus:ring-blue-500 focus:ring-offset-2 dark:border-gray-600 dark:bg-gray-700 dark:text-gray-300 dark:hover:bg-gray-600"
            (click)="onCancel()"
          >
            Cancel
          </button>
          <button
            type="button"
            class="rounded-md bg-blue-600 px-4 py-2 text-sm font-medium text-white hover:bg-blue-700 focus:outline-none focus:ring-2 focus:ring-blue-500 focus:ring-offset-2 disabled:cursor-not-allowed disabled:opacity-50"
            [disabled]="!isValid()"
            (click)="onSplit()"
          >
            Split into {{ subtasks().length }} Subtasks
          </button>
        </div>
      </div>
    </div>
  `,
})
export class SplitTaskDialogComponent {
  readonly taskId = input.required<string>();
  readonly taskTitle = input.required<string>();

  readonly split = output<CreateSubtaskDto[]>();
  readonly cancel = output<void>();

  readonly priorities = PRIORITIES;
  readonly subtasks = signal<SubtaskForm[]>([
    this.createEmptySubtask(),
    this.createEmptySubtask(),
  ]);

  constructor() {
    // Reset subtasks when dialog opens with new task
    effect(() => {
      this.taskId(); // Track taskId changes
      this.subtasks.set([this.createEmptySubtask(), this.createEmptySubtask()]);
    });
  }

  private createEmptySubtask(): SubtaskForm {
    return {
      title: '',
      description: '',
      priority: 'medium',
    };
  }

  addSubtask(): void {
    this.subtasks.update(list => [...list, this.createEmptySubtask()]);
  }

  removeSubtask(index: number): void {
    this.subtasks.update(list => list.filter((_, i) => i !== index));
  }

  updateSubtask(
    index: number,
    field: keyof SubtaskForm,
    value: string | Priority
  ): void {
    this.subtasks.update(list =>
      list.map((item, i) => (i === index ? { ...item, [field]: value } : item))
    );
  }

  isValid(): boolean {
    const list = this.subtasks();
    return (
      list.length >= 1 &&
      list.every(s => s.title.trim().length > 0 && s.description.trim().length > 0)
    );
  }

  onSplit(): void {
    if (!this.isValid()) return;

    const dtos: CreateSubtaskDto[] = this.subtasks().map(s => ({
      title: s.title.trim(),
      description: s.description.trim(),
      priority: s.priority,
    }));

    this.split.emit(dtos);
  }

  onCancel(): void {
    this.cancel.emit();
  }
}
