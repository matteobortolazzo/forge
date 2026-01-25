import { Component, ChangeDetectionStrategy, input, output, inject, signal } from '@angular/core';
import { TitleCasePipe } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { CreateTaskDto, Priority, PRIORITIES } from '../../shared/models';

@Component({
  selector: 'app-create-task-dialog',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [ReactiveFormsModule, TitleCasePipe],
  template: `
    @if (isOpen()) {
      <div
        class="fixed inset-0 z-50 flex items-center justify-center"
        role="dialog"
        aria-modal="true"
        aria-labelledby="dialog-title"
      >
        <!-- Backdrop -->
        <div
          class="absolute inset-0 bg-black/50 transition-opacity"
          (click)="onCancel()"
          aria-hidden="true"
        ></div>

        <!-- Dialog Panel -->
        <div class="relative w-full max-w-md rounded-lg bg-white p-6 shadow-xl dark:bg-gray-800">
          <h2
            id="dialog-title"
            class="text-lg font-semibold text-gray-900 dark:text-gray-100"
          >
            Create New Task
          </h2>

          <form [formGroup]="form" (ngSubmit)="onSubmit()" class="mt-4 space-y-4">
            <!-- Title -->
            <div>
              <label
                for="title"
                class="block text-sm font-medium text-gray-700 dark:text-gray-300"
              >
                Title
              </label>
              <input
                id="title"
                type="text"
                formControlName="title"
                class="mt-1 block w-full rounded-md border border-gray-300 px-3 py-2 text-sm shadow-sm focus:border-blue-500 focus:outline-none focus:ring-1 focus:ring-blue-500 dark:border-gray-600 dark:bg-gray-700 dark:text-gray-100"
                placeholder="Enter task title"
              />
              @if (form.controls.title.invalid && form.controls.title.touched) {
                <p class="mt-1 text-sm text-red-600 dark:text-red-400">
                  Title is required
                </p>
              }
            </div>

            <!-- Description -->
            <div>
              <label
                for="description"
                class="block text-sm font-medium text-gray-700 dark:text-gray-300"
              >
                Description
              </label>
              <textarea
                id="description"
                formControlName="description"
                rows="3"
                class="mt-1 block w-full rounded-md border border-gray-300 px-3 py-2 text-sm shadow-sm focus:border-blue-500 focus:outline-none focus:ring-1 focus:ring-blue-500 dark:border-gray-600 dark:bg-gray-700 dark:text-gray-100"
                placeholder="Describe the task"
              ></textarea>
            </div>

            <!-- Priority -->
            <div>
              <label
                for="priority"
                class="block text-sm font-medium text-gray-700 dark:text-gray-300"
              >
                Priority
              </label>
              <select
                id="priority"
                formControlName="priority"
                class="mt-1 block w-full rounded-md border border-gray-300 px-3 py-2 text-sm shadow-sm focus:border-blue-500 focus:outline-none focus:ring-1 focus:ring-blue-500 dark:border-gray-600 dark:bg-gray-700 dark:text-gray-100"
              >
                @for (p of priorities; track p) {
                  <option [value]="p">{{ p | titlecase }}</option>
                }
              </select>
            </div>

            <!-- Actions -->
            <div class="mt-6 flex justify-end gap-3">
              <button
                type="button"
                class="rounded-md border border-gray-300 bg-white px-4 py-2 text-sm font-medium text-gray-700 hover:bg-gray-50 focus:outline-none focus:ring-2 focus:ring-blue-500 focus:ring-offset-2 dark:border-gray-600 dark:bg-gray-700 dark:text-gray-300 dark:hover:bg-gray-600"
                (click)="onCancel()"
              >
                Cancel
              </button>
              <button
                type="submit"
                [disabled]="form.invalid || isSubmitting()"
                class="rounded-md bg-blue-600 px-4 py-2 text-sm font-medium text-white hover:bg-blue-700 focus:outline-none focus:ring-2 focus:ring-blue-500 focus:ring-offset-2 disabled:cursor-not-allowed disabled:opacity-50"
              >
                @if (isSubmitting()) {
                  Creating...
                } @else {
                  Create Task
                }
              </button>
            </div>
          </form>
        </div>
      </div>
    }
  `,
})
export class CreateTaskDialogComponent {
  private readonly fb = inject(FormBuilder);

  readonly isOpen = input(false);
  readonly create = output<CreateTaskDto>();
  readonly cancel = output<void>();

  readonly priorities: readonly Priority[] = PRIORITIES;
  readonly isSubmitting = signal(false);

  readonly form = this.fb.nonNullable.group({
    title: ['', [Validators.required, Validators.minLength(1)]],
    description: [''],
    priority: ['medium' as Priority],
  });

  onSubmit(): void {
    if (this.form.valid) {
      this.isSubmitting.set(true);
      const dto: CreateTaskDto = {
        title: this.form.value.title!,
        description: this.form.value.description!,
        priority: this.form.value.priority!,
      };
      this.create.emit(dto);
      this.resetForm();
    }
  }

  onCancel(): void {
    this.resetForm();
    this.cancel.emit();
  }

  private resetForm(): void {
    this.form.reset({
      title: '',
      description: '',
      priority: 'medium',
    });
    this.isSubmitting.set(false);
  }
}
