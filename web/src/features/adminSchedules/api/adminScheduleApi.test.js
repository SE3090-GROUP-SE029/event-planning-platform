import { describe, expect, it } from 'vitest';
import { formatDate, formatWallClockTime } from './adminScheduleApi';

describe('admin schedule wall-clock formatters', () => {
  it('formats UTC-marked activity times without browser timezone conversion', () => {
    expect(formatWallClockTime('2026-10-06T18:00:00Z')).toBe('6:00 PM');
    expect(formatWallClockTime('2026-10-06T23:00:00Z')).toBe('11:00 PM');
    expect(formatWallClockTime('2026-10-06T00:15:00Z')).toBe('12:15 AM');
  });

  it('preserves the represented event date for midnight-adjacent times', () => {
    expect(formatDate('2026-10-06T00:15:00Z')).toBe('10/06/2026');
    expect(formatDate('2026-10-06T23:00:00Z')).toBe('10/06/2026');
  });

  it('formats time-only event windows with the same wall-clock rules', () => {
    expect(formatWallClockTime('18:00:00')).toBe('6:00 PM');
    expect(formatWallClockTime('23:00:00')).toBe('11:00 PM');
    expect(formatWallClockTime('00:15:00')).toBe('12:15 AM');
  });
});
