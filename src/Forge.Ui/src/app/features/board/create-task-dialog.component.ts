import { Component, ChangeDetectionStrategy, input, output, inject, signal } from '@angular/core';
import { TitleCasePipe } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { CreateTaskDto, Priority, PRIORITIES } from '../../shared/models';
import {
  FormFieldComponent,
  INPUT_CLASSES,
  TEXTAREA_CLASSES,
  SELECT_CLASSES,
  PRIMARY_BUTTON_CLASSES,
  SECONDARY_BUTTON_CLASSES,
} from '../../shared/components/form';

@Component({
  selector: 'app-create-task-dialog',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [ReactiveFormsModule, TitleCasePipe, FormFieldComponent],
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
            <app-form-field
              id="title"
              label="Title"
              [error]="form.controls.title.invalid && form.controls.title.touched ? 'Title is required' : ''"
            >
              <input
                id="title"
                type="text"
                formControlName="title"
                [class]="inputClasses"
                placeholder="Enter task title"
              />
            </app-form-field>

            <!-- Description -->
            <app-form-field id="description" label="Description">
              <textarea
                id="description"
                formControlName="description"
                rows="3"
                [class]="textareaClasses"
                placeholder="Describe the task"
              ></textarea>
            </app-form-field>

            <!-- Priority -->
            <app-form-field id="priority" label="Priority">
              <select
                id="priority"
                formControlName="priority"
                [class]="selectClasses"
              >
                @for (p of priorities; track p) {
                  <option [value]="p">{{ p | titlecase }}</option>
                }
              </select>
            </app-form-field>

            <!-- Actions -->
            <div class="mt-6 flex justify-end gap-3">
              <button
                type="button"
                [class]="secondaryButtonClasses"
                (click)="onCancel()"
              >
                Cancel
              </button>
              <button
                type="submit"
                [disabled]="form.invalid || isSubmitting()"
                [class]="primaryButtonClasses"
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

  // Shared form classes
  readonly inputClasses = INPUT_CLASSES;
  readonly textareaClasses = TEXTAREA_CLASSES;
  readonly selectClasses = SELECT_CLASSES;
  readonly primaryButtonClasses = PRIMARY_BUTTON_CLASSES;
  readonly secondaryButtonClasses = SECONDARY_BUTTON_CLASSES;

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
