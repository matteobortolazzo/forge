import { Component, ChangeDetectionStrategy, input, output, inject, signal } from '@angular/core';
import { TitleCasePipe } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { CreateBacklogItemDto, Priority, PRIORITIES } from '../../shared/models';
import {
  FormFieldComponent,
  INPUT_CLASSES,
  TEXTAREA_CLASSES,
  SELECT_CLASSES,
  PRIMARY_BUTTON_CLASSES,
  SECONDARY_BUTTON_CLASSES,
} from '../../shared/components/form';

@Component({
  selector: 'app-create-backlog-item-dialog',
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
        <div class="relative w-full max-w-lg rounded-lg bg-white p-6 shadow-xl dark:bg-gray-800">
          <h2
            id="dialog-title"
            class="text-lg font-semibold text-gray-900 dark:text-gray-100"
          >
            Create New Backlog Item
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
                placeholder="Enter a clear, concise title"
              />
            </app-form-field>

            <!-- Description -->
            <app-form-field
              id="description"
              label="Description"
              [error]="form.controls.description.invalid && form.controls.description.touched ? 'Description is required' : ''"
            >
              <textarea
                id="description"
                formControlName="description"
                rows="4"
                [class]="textareaClasses"
                placeholder="Describe what needs to be done and why"
              ></textarea>
            </app-form-field>

            <!-- Acceptance Criteria -->
            <app-form-field id="acceptanceCriteria" label="Acceptance Criteria (Optional)">
              <textarea
                id="acceptanceCriteria"
                formControlName="acceptanceCriteria"
                rows="3"
                [class]="textareaClasses"
                placeholder="What are the conditions for this to be considered complete?"
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
                  Create Item
                }
              </button>
            </div>
          </form>
        </div>
      </div>
    }
  `,
})
export class CreateBacklogItemDialogComponent {
  private readonly fb = inject(FormBuilder);

  readonly isOpen = input(false);
  readonly create = output<CreateBacklogItemDto>();
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
    description: ['', [Validators.required, Validators.minLength(1)]],
    acceptanceCriteria: [''],
    priority: ['medium' as Priority],
  });

  onSubmit(): void {
    if (this.form.valid) {
      this.isSubmitting.set(true);
      const dto: CreateBacklogItemDto = {
        title: this.form.value.title!,
        description: this.form.value.description!,
        priority: this.form.value.priority!,
        acceptanceCriteria: this.form.value.acceptanceCriteria || undefined,
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
      acceptanceCriteria: '',
      priority: 'medium',
    });
    this.isSubmitting.set(false);
  }
}
