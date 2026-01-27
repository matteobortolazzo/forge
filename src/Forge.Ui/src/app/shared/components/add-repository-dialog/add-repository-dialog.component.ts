import { Component, ChangeDetectionStrategy, input, output, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { CreateRepositoryDto } from '../../models';

@Component({
  selector: 'app-add-repository-dialog',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [ReactiveFormsModule],
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
            Add Repository
          </h2>

          <form [formGroup]="form" (ngSubmit)="onSubmit()" class="mt-4 space-y-4">
            <!-- Name -->
            <div>
              <label
                for="repo-name"
                class="block text-sm font-medium text-gray-700 dark:text-gray-300"
              >
                Name
              </label>
              <input
                id="repo-name"
                type="text"
                formControlName="name"
                class="mt-1 block w-full rounded-md border border-gray-300 px-3 py-2 text-sm shadow-sm focus:border-blue-500 focus:outline-none focus:ring-1 focus:ring-blue-500 dark:border-gray-600 dark:bg-gray-700 dark:text-gray-100"
                placeholder="My Project"
              />
              @if (form.controls.name.invalid && form.controls.name.touched) {
                <p class="mt-1 text-sm text-red-600 dark:text-red-400">
                  Name is required
                </p>
              }
            </div>

            <!-- Path -->
            <div>
              <label
                for="repo-path"
                class="block text-sm font-medium text-gray-700 dark:text-gray-300"
              >
                Path
              </label>
              <input
                id="repo-path"
                type="text"
                formControlName="path"
                class="mt-1 block w-full rounded-md border border-gray-300 px-3 py-2 text-sm font-mono shadow-sm focus:border-blue-500 focus:outline-none focus:ring-1 focus:ring-blue-500 dark:border-gray-600 dark:bg-gray-700 dark:text-gray-100"
                placeholder="/path/to/repository"
              />
              @if (form.controls.path.invalid && form.controls.path.touched) {
                <p class="mt-1 text-sm text-red-600 dark:text-red-400">
                  Path is required
                </p>
              }
            </div>

            <!-- Set as Default -->
            <div class="flex items-center gap-2">
              <input
                id="set-default"
                type="checkbox"
                formControlName="setAsDefault"
                class="h-4 w-4 rounded border-gray-300 text-blue-600 focus:ring-blue-500 dark:border-gray-600 dark:bg-gray-700"
              />
              <label
                for="set-default"
                class="text-sm text-gray-700 dark:text-gray-300"
              >
                Set as default repository
              </label>
            </div>

            <!-- Error Message -->
            @if (errorMessage()) {
              <div class="rounded-md bg-red-50 p-3 dark:bg-red-900/20">
                <p class="text-sm text-red-700 dark:text-red-400">
                  {{ errorMessage() }}
                </p>
              </div>
            }

            <!-- Actions -->
            <div class="mt-6 flex justify-end gap-3">
              <button
                type="button"
                class="rounded-md border border-gray-300 bg-white px-4 py-2 text-sm font-medium text-gray-700 hover:bg-gray-50 focus:outline-none focus:ring-2 focus:ring-blue-500 focus:ring-offset-2 dark:border-gray-600 dark:bg-gray-700 dark:text-gray-300 dark:hover:bg-gray-600"
                (click)="onCancel()"
                [disabled]="isSubmitting()"
              >
                Cancel
              </button>
              <button
                type="submit"
                [disabled]="form.invalid || isSubmitting()"
                class="rounded-md bg-blue-600 px-4 py-2 text-sm font-medium text-white hover:bg-blue-700 focus:outline-none focus:ring-2 focus:ring-blue-500 focus:ring-offset-2 disabled:cursor-not-allowed disabled:opacity-50"
              >
                @if (isSubmitting()) {
                  Adding...
                } @else {
                  Add Repository
                }
              </button>
            </div>
          </form>
        </div>
      </div>
    }
  `,
})
export class AddRepositoryDialogComponent {
  private readonly fb = inject(FormBuilder);

  readonly isOpen = input(false);
  readonly create = output<CreateRepositoryDto>();
  readonly cancel = output<void>();

  readonly isSubmitting = signal(false);
  readonly errorMessage = signal<string | null>(null);

  readonly form = this.fb.nonNullable.group({
    name: ['', [Validators.required, Validators.minLength(1)]],
    path: ['', [Validators.required, Validators.minLength(1)]],
    setAsDefault: [false],
  });

  onSubmit(): void {
    if (this.form.valid) {
      this.isSubmitting.set(true);
      this.errorMessage.set(null);
      const dto: CreateRepositoryDto = {
        name: this.form.value.name!,
        path: this.form.value.path!,
        setAsDefault: this.form.value.setAsDefault,
      };
      this.create.emit(dto);
    }
  }

  onCancel(): void {
    this.resetForm();
    this.cancel.emit();
  }

  setError(message: string): void {
    this.errorMessage.set(message);
    this.isSubmitting.set(false);
  }

  resetForm(): void {
    this.form.reset({
      name: '',
      path: '',
      setAsDefault: false,
    });
    this.isSubmitting.set(false);
    this.errorMessage.set(null);
  }
}
