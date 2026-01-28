import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { formatDateTime, formatTime, formatRelativeTime } from './date-utils';

describe('date-utils', () => {
  describe('formatDateTime', () => {
    it('should format a Date object', () => {
      const date = new Date(2026, 0, 28, 10, 30);
      const result = formatDateTime(date);
      // Format varies by locale, but should contain key parts
      expect(result).toContain('2026');
      expect(result).toContain('28');
    });

    it('should format an ISO string', () => {
      const isoString = '2026-01-28T10:30:00Z';
      const result = formatDateTime(isoString);
      expect(result).toContain('2026');
    });

    it('should handle different date values', () => {
      const dates = [
        new Date(2025, 5, 15, 14, 45),
        new Date(2024, 11, 31, 23, 59),
        '2023-03-01T09:00:00Z',
      ];

      dates.forEach(date => {
        expect(() => formatDateTime(date)).not.toThrow();
        expect(formatDateTime(date)).toBeTruthy();
      });
    });
  });

  describe('formatTime', () => {
    it('should format a Date object with time only', () => {
      const date = new Date(2026, 0, 28, 10, 30, 45);
      const result = formatTime(date);
      // Should contain time components
      expect(result).toMatch(/\d{1,2}:\d{2}:\d{2}/);
    });

    it('should format an ISO string', () => {
      const isoString = '2026-01-28T14:25:30Z';
      const result = formatTime(isoString);
      expect(result).toMatch(/\d{1,2}:\d{2}:\d{2}/);
    });

    it('should include seconds', () => {
      const date = new Date(2026, 0, 28, 10, 30, 45);
      const result = formatTime(date);
      // Should have seconds component
      expect(result.split(':').length).toBe(3);
    });
  });

  describe('formatRelativeTime', () => {
    let originalDate: typeof Date;

    beforeEach(() => {
      // Mock Date.now to return a fixed time
      originalDate = globalThis.Date;
      const mockNow = new Date(2026, 0, 28, 12, 0, 0).getTime();
      vi.useFakeTimers();
      vi.setSystemTime(mockNow);
    });

    afterEach(() => {
      vi.useRealTimers();
    });

    it('should return "Just now" for times less than a minute ago', () => {
      const thirtySecondsAgo = new Date(2026, 0, 28, 11, 59, 30);
      expect(formatRelativeTime(thirtySecondsAgo)).toBe('Just now');
    });

    it('should return minutes ago for times within an hour', () => {
      const fiveMinutesAgo = new Date(2026, 0, 28, 11, 55, 0);
      expect(formatRelativeTime(fiveMinutesAgo)).toBe('5m ago');

      const thirtyMinutesAgo = new Date(2026, 0, 28, 11, 30, 0);
      expect(formatRelativeTime(thirtyMinutesAgo)).toBe('30m ago');
    });

    it('should return hours ago for times within a day', () => {
      const twoHoursAgo = new Date(2026, 0, 28, 10, 0, 0);
      expect(formatRelativeTime(twoHoursAgo)).toBe('2h ago');

      const twentyThreeHoursAgo = new Date(2026, 0, 27, 13, 0, 0);
      expect(formatRelativeTime(twentyThreeHoursAgo)).toBe('23h ago');
    });

    it('should return days ago for times within a week', () => {
      const oneDayAgo = new Date(2026, 0, 27, 12, 0, 0);
      expect(formatRelativeTime(oneDayAgo)).toBe('1d ago');

      const sixDaysAgo = new Date(2026, 0, 22, 12, 0, 0);
      expect(formatRelativeTime(sixDaysAgo)).toBe('6d ago');
    });

    it('should return formatted date for times older than a week', () => {
      const twoWeeksAgo = new Date(2026, 0, 14, 12, 0, 0);
      const result = formatRelativeTime(twoWeeksAgo);
      // Should return a locale date string, not relative time
      expect(result).not.toContain('ago');
    });

    it('should handle ISO string input', () => {
      const fiveMinutesAgo = new Date(2026, 0, 28, 11, 55, 0).toISOString();
      expect(formatRelativeTime(fiveMinutesAgo)).toBe('5m ago');
    });
  });
});
