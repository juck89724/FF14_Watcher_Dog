import { describe, it, expect } from 'vitest';
import { getLastResetTime } from './timeUtils';
import type { UtilityTask } from '../types';

describe('getLastResetTime', () => {
    // Helper to create a specific date (Year, Month sends 0-indexed, but Day is 1-indexed)
    // 2024-01-01 is a Monday
    const createDate = (day: number, hour: number, minute: number) => {
        return new Date(2024, 0, day, hour, minute, 0);
    };

    describe('Daily Task (Reset 23:00)', () => {
        const dailyTask: UtilityTask = {
            id: 'test', name: 'Test', frequency: 'daily', resetTime: 23, category: 'duty_roulette'
        };

        it('should calculate yesterday reset if before 23:00', () => {
            // Jan 2 (Tuesday) 22:59
            const now = createDate(2, 22, 59);
            const expected = createDate(1, 23, 0); // Jan 1 23:00

            const result = getLastResetTime(dailyTask, now);
            expect(result.toISOString()).toBe(expected.toISOString());
        });

        it('should calculate today reset if after 23:00', () => {
            // Jan 2 (Tuesday) 23:01
            const now = createDate(2, 23, 1);
            const expected = createDate(2, 23, 0); // Jan 2 23:00

            const result = getLastResetTime(dailyTask, now);
            expect(result.toISOString()).toBe(expected.toISOString());
        });

        it('should calculate yesterday reset if early morning next day', () => {
            // Jan 3 (Wednesday) 01:00
            const now = createDate(3, 1, 0);
            const expected = createDate(2, 23, 0); // Jan 2 23:00 (Previous logical day)

            const result = getLastResetTime(dailyTask, now);
            expect(result.toISOString()).toBe(expected.toISOString());
        });
    });

    describe('Weekly Task (Reset Tue 16:00)', () => {
        const weeklyTask: UtilityTask = {
            id: 'test_weekly', name: 'Test Weekly', frequency: 'weekly', resetDay: 2, resetTime: 16, category: 'mission_info'
        };

        // Jan 2, 2024 is a Tuesday
        it('should return last week if exactly on reset day but before time', () => {
            // Jan 2 (Tue) 15:59
            const now = createDate(2, 15, 59);
            // Expected: Dec 26 (Previous Tue) 16:00. 
            // 2024-01-02 minus 7 days = 2023-12-26
            const expected = new Date(2023, 11, 26, 16, 0, 0);

            const result = getLastResetTime(weeklyTask, now);
            expect(result.toISOString()).toBe(expected.toISOString());
        });

        it('should return today if on reset day after time', () => {
            // Jan 2 (Tue) 16:01
            const now = createDate(2, 16, 1);
            const expected = createDate(2, 16, 0); // Jan 2 16:00

            const result = getLastResetTime(weeklyTask, now);
            expect(result.toISOString()).toBe(expected.toISOString());
        });

        it('should return yesterday if on Wednesday', () => {
            // Jan 3 (Wed) 10:00
            const now = createDate(3, 10, 0);
            const expected = createDate(2, 16, 0); // Jan 2 16:00

            const result = getLastResetTime(weeklyTask, now);
            expect(result.toISOString()).toBe(expected.toISOString());
        });

        it('should return last week if on Monday', () => {
            // Jan 8 (Mon) 10:00
            const now = createDate(8, 10, 0);
            const expected = createDate(2, 16, 0); // Jan 2 16:00 (Last Week's reset)

            const result = getLastResetTime(weeklyTask, now);
            expect(result.toISOString()).toBe(expected.toISOString());
        });
    });
});
