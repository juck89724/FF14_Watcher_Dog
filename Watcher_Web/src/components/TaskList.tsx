import React from 'react';
import type { UtilityTask } from '../types';
import { TaskItem } from './TaskItem';

interface TaskListProps {
    title: string;
    tasks: UtilityTask[];
    checkStatus: (task: UtilityTask) => boolean;
    onToggle: (id: string) => void;
}

export const TaskList: React.FC<TaskListProps> = ({ title, tasks, checkStatus, onToggle }) => {
    if (tasks.length === 0) return null;

    return (
        <div className="task-list">
            <div className="task-list-header">
                {title}
            </div>
            <div>
                {tasks.map((task) => (
                    <TaskItem
                        key={task.id}
                        task={task}
                        isCompleted={checkStatus(task)}
                        onToggle={onToggle}
                    />
                ))}
            </div>
        </div>
    );
};
