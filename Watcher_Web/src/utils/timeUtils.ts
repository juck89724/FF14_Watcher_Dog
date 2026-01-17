import type { UtilityTask } from '../types';

export function getLastResetTime(task: UtilityTask, now: Date): Date {
    const resetTime = task.resetTime ?? 0; // Default to midnight if not set
    const resetDate = new Date(now);
    resetDate.setHours(resetTime, 0, 0, 0);

    if (task.frequency === 'daily') {
        // If now is BEFORE today's reset time, the last reset was YESTERDAY.
        if (now < resetDate) {
            resetDate.setDate(resetDate.getDate() - 1);
        }
        return resetDate;
    } else if (task.frequency === 'weekly') {
        const resetDay = task.resetDay ?? 0; // Default Sunday
        const currentDay = now.getDay();

        // Calculate difference to target day
        // e.g. Target Tue(2), Now Wed(3) -> diff = 1. Reset was 1 day ago.
        // e.g. Target Tue(2), Now Mon(1) -> diff = -1. Reset was 6 days ago.
        let dayDiff = currentDay - resetDay;
        if (dayDiff < 0) dayDiff += 7;

        // Adjust Date
        resetDate.setDate(now.getDate() - dayDiff);

        // If we are on the reset day, but BEFORE the reset time, we need to go back another week
        if (dayDiff === 0 && now < resetDate) {
            resetDate.setDate(resetDate.getDate() - 7);
        }
        return resetDate;
    }

    return new Date(0); // Fallback
}
