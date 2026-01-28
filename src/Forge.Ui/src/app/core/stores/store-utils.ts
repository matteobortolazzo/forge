import { signal, Signal, WritableSignal } from '@angular/core';

/**
 * Async state for store operations.
 * Provides consistent loading/error state management.
 */
export interface AsyncState {
  /** Whether an async operation is in progress */
  readonly loading: Signal<boolean>;
  /** Error message from the last failed operation */
  readonly error: Signal<string | null>;
}

/**
 * Writeable async state for internal store use.
 */
export interface WritableAsyncState {
  loading: WritableSignal<boolean>;
  error: WritableSignal<string | null>;
}

/**
 * Creates loading and error signals for async state management.
 * Use this in stores to avoid duplicating signal declarations.
 *
 * @example
 * ```typescript
 * export class MyStore {
 *   private readonly asyncState = createAsyncState();
 *   readonly isLoading = this.asyncState.loading.asReadonly();
 *   readonly errorMessage = this.asyncState.error.asReadonly();
 * }
 * ```
 */
export function createAsyncState(): WritableAsyncState {
  return {
    loading: signal(false),
    error: signal<string | null>(null),
  };
}

/**
 * Options for runAsync helper.
 */
export interface RunAsyncOptions {
  /** Set loading state during operation (default: true) */
  setLoading?: boolean;
  /** Clear error before operation (default: true) */
  clearError?: boolean;
}

/**
 * Runs an async operation with automatic loading/error state management.
 * Reduces boilerplate in store actions.
 *
 * @param state - The async state signals to manage
 * @param operation - The async operation to run
 * @param options - Optional configuration for loading/error behavior
 * @param errorMessage - Custom error message prefix (default: 'An error occurred')
 * @returns The result of the operation, or null if an error occurred
 *
 * @example
 * ```typescript
 * async loadData(): Promise<void> {
 *   await runAsync(this.asyncState, async () => {
 *     const data = await firstValueFrom(this.service.getData());
 *     this.data.set(data);
 *   }, {}, 'Failed to load data');
 * }
 *
 * // Without loading state (for fire-and-forget operations)
 * async deleteItem(id: string): Promise<boolean> {
 *   return runAsync(this.asyncState, async () => {
 *     await firstValueFrom(this.service.delete(id));
 *     this.items.update(list => list.filter(i => i.id !== id));
 *     return true;
 *   }, { setLoading: false }, 'Failed to delete item') ?? false;
 * }
 * ```
 */
export async function runAsync<T>(
  state: WritableAsyncState,
  operation: () => Promise<T>,
  options: RunAsyncOptions = {},
  errorMessage = 'An error occurred'
): Promise<T | null> {
  const { setLoading = true, clearError = true } = options;

  if (setLoading) {
    state.loading.set(true);
  }
  if (clearError) {
    state.error.set(null);
  }

  try {
    return await operation();
  } catch (err) {
    state.error.set(extractErrorMessage(err, errorMessage));
    return null;
  } finally {
    if (setLoading) {
      state.loading.set(false);
    }
  }
}

/**
 * Extracts error message from various error types.
 * Handles HttpErrorResponse, Error objects, and unknown types.
 */
export function extractErrorMessage(err: unknown, defaultMessage: string): string {
  if (err instanceof Error) {
    return err.message;
  }
  // Handle HttpErrorResponse-like objects
  if (typeof err === 'object' && err !== null && 'error' in err) {
    const httpErr = err as { error?: { error?: string }; message?: string };
    return httpErr.error?.error || httpErr.message || defaultMessage;
  }
  return defaultMessage;
}
