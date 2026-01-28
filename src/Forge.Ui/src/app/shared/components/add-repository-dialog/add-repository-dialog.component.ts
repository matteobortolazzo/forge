import { Component, ChangeDetectionStrategy, input, output, inject, signal, computed } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
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
            <!-- Path -->
            <div>
              <label
                for="repo-path"
                class="block text-sm font-medium text-gray-700 dark:text-gray-300"
              >
                Repository Path
              </label>
              <input
                id="repo-path"
                type="text"
                formControlName="path"
                class="mt-1 block w-full rounded-md border border-gray-300 px-3 py-2 text-sm font-mono shadow-sm focus:border-blue-500 focus:outline-none focus:ring-1 focus:ring-blue-500 dark:border-gray-600 dark:bg-gray-700 dark:text-gray-100"
                placeholder="/home/user/projects/my-repo"
              />
              @if (form.controls.path.invalid && form.controls.path.touched) {
                <p class="mt-1 text-sm text-red-600 dark:text-red-400">
                  Path is required
                </p>
              }
              @if (derivedName()) {
                <p class="mt-1 text-sm text-gray-500 dark:text-gray-400">
                  Will be added as: <span class="font-medium text-gray-700 dark:text-gray-300">"{{ derivedName() }}"</span>
                </p>
              }
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
                [disabled]="form.invalid || isSubmitting() || !derivedName()"
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
    path: ['', [Validators.required, Validators.minLength(1)]],
  });

  /** Signal that tracks form value changes for reactivity with computed signals */
  private readonly formValue = toSignal(this.form.valueChanges, { initialValue: this.form.value });

  /**
   * Derives the repository name from the path.
   * Extracts the last folder name from the path.
   */
  readonly derivedName = computed(() => {
    const path = this.formValue().path?.trim();
    if (!path) return null;
    // Handle both Unix and Windows paths
    const segments = path.split(/[/\\]/).filter(Boolean);
    return segments[segments.length - 1] || null;
  });

  onSubmit(): void {
    const name = this.derivedName();
    if (this.form.valid && name) {
      this.isSubmitting.set(true);
      this.errorMessage.set(null);
      const dto: CreateRepositoryDto = {
        name,
        path: this.form.value.path!,
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
      path: '',
    });
    this.isSubmitting.set(false);
    this.errorMessage.set(null);
  }
}
