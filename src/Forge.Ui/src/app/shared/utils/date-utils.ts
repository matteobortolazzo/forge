/**
 * Shared date formatting utilities.
 * Centralizes date/time formatting logic used across the application.
 */

/**
 * Formats a date for display with full date and time.
 * Example: "Jan 28, 2026, 10:30 AM"
 *
 * @param date - The date to format (Date object or ISO string)
 * @returns Formatted date string
 */
export function formatDateTime(date: Date | string): string {
  return new Date(date).toLocaleDateString(undefined, {
    year: 'numeric',
    month: 'short',
    day: 'numeric',
    hour: '2-digit',
    minute: '2-digit',
  });
}

/**
 * Formats a date to show only time with seconds.
 * Example: "10:30:45"
 *
 * @param date - The date to format (Date object or ISO string)
 * @returns Formatted time string
 */
export function formatTime(date: Date | string): string {
  return new Date(date).toLocaleTimeString([], {
    hour: '2-digit',
    minute: '2-digit',
    second: '2-digit',
  });
}

/**
 * Formats a date as relative time from now.
 * Examples: "Just now", "5m ago", "2h ago", "3d ago", "Jan 15"
 *
 * @param date - The date to format (Date object or ISO string)
 * @returns Relative time string
 */
export function formatRelativeTime(date: Date | string): string {
  const now = new Date();
  const target = new Date(date);
  const diff = now.getTime() - target.getTime();
  const minutes = Math.floor(diff / 60000);
  const hours = Math.floor(diff / 3600000);
  const days = Math.floor(diff / 86400000);

  if (minutes < 1) return 'Just now';
  if (minutes < 60) return `${minutes}m ago`;
  if (hours < 24) return `${hours}h ago`;
  if (days < 7) return `${days}d ago`;
  return target.toLocaleDateString();
}
