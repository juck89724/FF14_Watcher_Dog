import { useState, useEffect } from 'react';
import type { StoredData, TaskState, UtilityTask } from '../types';
import { TASKS } from '../data/tasks';
import { getLastResetTime } from '../utils/timeUtils';

const STORAGE_KEY = 'ff14_watcher_web_v1';

const getInitialData = (): StoredData => {
    const stored = localStorage.getItem(STORAGE_KEY);
    if (stored) {
        try {
            return JSON.parse(stored);
        } catch (e) {
            console.error('Failed to parse stored data', e);
        }
    }
    return {
        lastVisit: new Date().toISOString(),
        tasks: {},
    };
};

export const useTaskStorage = () => {
    const [data, setData] = useState<StoredData>(getInitialData);
    const [now, setNow] = useState(new Date());

    // Update "now" every minute to ensure UI is fresh (though logic runs on load/action)
    useEffect(() => {
        const timer = setInterval(() => setNow(new Date()), 60000);
        return () => clearInterval(timer);
    }, []);

    const saveToStorage = (newData: StoredData) => {
        localStorage.setItem(STORAGE_KEY, JSON.stringify(newData));
        setData(newData);
    };

    const isTaskDone = (task: UtilityTask): boolean => {
        const taskState = data.tasks[task.id];
        if (!taskState || !taskState.done || !taskState.completedAt) return false;

        const completedAt = new Date(taskState.completedAt);
        const lastReset = getLastResetTime(task, now);

        // If completed BEFORE the last reset, it is effectively NOT done.
        return completedAt > lastReset;
    };

    const toggleTask = (taskId: string) => {
        const task = TASKS.find((t) => t.id === taskId);
        if (!task) return;

        const currentDone = isTaskDone(task);

        // Create a local ISO string (YYYY-MM-DDTHH:mm:ss.sss)
        // This ensures we capture the time in the user's current timezone as requested
        const nowLocal = new Date();
        const offsetMs = nowLocal.getTimezoneOffset() * 60000;
        const localISO = new Date(nowLocal.getTime() - offsetMs).toISOString().slice(0, -1);

        const newState: TaskState = {
            done: !currentDone,
            completedAt: !currentDone ? localISO : undefined,
        };

        const newData = {
            ...data,
            tasks: {
                ...data.tasks,
                [taskId]: newState,
            },
            lastVisit: new Date().toISOString(),
        };

        saveToStorage(newData);
    };

    return {
        isTaskDone,
        toggleTask,
    };
};
