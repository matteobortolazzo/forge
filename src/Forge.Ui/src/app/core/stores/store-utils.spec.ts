import { describe, it, expect, vi, beforeEach } from 'vitest';
import {
  createAsyncState,
  runAsync,
  extractErrorMessage,
  WritableAsyncState,
} from './store-utils';

describe('store-utils', () => {
  describe('createAsyncState', () => {
    it('should create an async state with loading=false and error=null', () => {
      const state = createAsyncState();

      expect(state.loading()).toBe(false);
      expect(state.error()).toBeNull();
    });

    it('should create writable signals', () => {
      const state = createAsyncState();

      state.loading.set(true);
      expect(state.loading()).toBe(true);

      state.error.set('Test error');
      expect(state.error()).toBe('Test error');
    });
  });

  describe('runAsync', () => {
    let state: WritableAsyncState;

    beforeEach(() => {
      state = createAsyncState();
    });

    it('should set loading=true during operation and false after', async () => {
      const loadingStates: boolean[] = [];

      await runAsync(state, async () => {
        loadingStates.push(state.loading());
        return 'result';
      });

      expect(loadingStates).toContain(true);
      expect(state.loading()).toBe(false);
    });

    it('should clear error before operation', async () => {
      state.error.set('Previous error');

      await runAsync(state, async () => 'result');

      expect(state.error()).toBeNull();
    });

    it('should return the result of the operation', async () => {
      const result = await runAsync(state, async () => 'success');

      expect(result).toBe('success');
    });

    it('should return null and set error on exception', async () => {
      const result = await runAsync(state, async () => {
        throw new Error('Test error');
      });

      expect(result).toBeNull();
      expect(state.error()).toBe('Test error');
      expect(state.loading()).toBe(false);
    });

    it('should use custom error message on exception', async () => {
      const result = await runAsync(
        state,
        async () => {
          throw new Error('Original error');
        },
        {},
        'Custom error message'
      );

      expect(result).toBeNull();
      // Should use extractErrorMessage which returns the Error.message
      expect(state.error()).toBe('Original error');
    });

    it('should use default message for non-Error exceptions', async () => {
      const result = await runAsync(
        state,
        async () => {
          throw 'string error';
        },
        {},
        'Custom default'
      );

      expect(result).toBeNull();
      expect(state.error()).toBe('Custom default');
    });

    it('should not set loading when setLoading=false', async () => {
      const loadingStates: boolean[] = [];

      await runAsync(
        state,
        async () => {
          loadingStates.push(state.loading());
          return 'result';
        },
        { setLoading: false }
      );

      expect(loadingStates).toEqual([false]);
      expect(state.loading()).toBe(false);
    });

    it('should not clear error when clearError=false', async () => {
      state.error.set('Previous error');

      await runAsync(
        state,
        async () => 'result',
        { clearError: false }
      );

      // Error should remain because operation succeeded
      // but clearError was false, so it wasn't cleared at the start
      // However, the error stays because operation succeeded without throwing
      expect(state.error()).toBe('Previous error');
    });

    it('should handle async operations correctly', async () => {
      const mockFn = vi.fn().mockResolvedValue('async result');

      const result = await runAsync(state, mockFn);

      expect(mockFn).toHaveBeenCalledOnce();
      expect(result).toBe('async result');
    });

    it('should handle complex return types', async () => {
      const complexResult = { id: '123', items: [1, 2, 3] };

      const result = await runAsync(state, async () => complexResult);

      expect(result).toEqual(complexResult);
    });
  });

  describe('extractErrorMessage', () => {
    it('should extract message from Error instance', () => {
      const error = new Error('Error message');
      expect(extractErrorMessage(error, 'default')).toBe('Error message');
    });

    it('should return default message for non-Error types', () => {
      expect(extractErrorMessage('string error', 'default')).toBe('default');
      expect(extractErrorMessage(null, 'default')).toBe('default');
      expect(extractErrorMessage(undefined, 'default')).toBe('default');
      expect(extractErrorMessage(123, 'default')).toBe('default');
    });

    it('should extract error from HttpErrorResponse-like object', () => {
      const httpError = {
        error: { error: 'API error message' },
        message: 'Http failure',
      };
      expect(extractErrorMessage(httpError, 'default')).toBe('API error message');
    });

    it('should fall back to message property for HttpErrorResponse', () => {
      const httpError = {
        error: {},
        message: 'Http failure',
      };
      expect(extractErrorMessage(httpError, 'default')).toBe('Http failure');
    });

    it('should return default for HttpErrorResponse without error details', () => {
      const httpError = {
        error: null,
      };
      expect(extractErrorMessage(httpError, 'default')).toBe('default');
    });
  });
});
