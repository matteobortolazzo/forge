import { BacklogItemState } from '../models';

/**
 * Human-readable labels for backlog item states.
 * Used in backlog list and detail views for display purposes.
 */
export const STATE_LABELS: Record<BacklogItemState, string> = {
  New: 'New',
  Refining: 'Refining',
  Ready: 'Ready',
  Splitting: 'Splitting',
  Executing: 'In Progress',
  Done: 'Done',
};
