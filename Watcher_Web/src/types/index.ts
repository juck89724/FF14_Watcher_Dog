export type Frequency = 'daily' | 'weekly';
export type TaskCategory = 'duty_roulette' | 'daily_challenge' | 'mission_info';

export interface UtilityTask {
  id: string;
  name: string;
  frequency: Frequency;
  category: TaskCategory;
  description?: string;
  resetTime?: number; // Hour of day for reset (e.g. 15 for 15:00 UTC)
  resetDay?: number; // Day of week for weekly reset (0=Sun, 2=Tue)
}

export interface TaskState {
  done: boolean;
  completedAt?: string; // ISO date string
}

export interface StoredData {
  lastVisit: string;
  tasks: Record<string, TaskState>;
}
