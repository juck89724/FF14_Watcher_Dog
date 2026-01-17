import React from 'react';
import type { UtilityTask } from '../types';

interface TaskItemProps {
    task: UtilityTask;
    isCompleted: boolean;
    onToggle: (id: string) => void;
}

export const TaskItem: React.FC<TaskItemProps> = ({ task, isCompleted, onToggle }) => {
    return (
        <div
            className={`task-item ${isCompleted ? 'completed' : ''}`}
            onClick={() => onToggle(task.id)}
        >
            <div className="task-content">
                <span className="task-name">{task.name}</span>
                {task.description && <span className="task-desc">{task.description}</span>}
            </div>

            <div className="checkbox-wrapper">
                <div className="custom-checkbox">
                    <span className="checkmark">✔</span>
                </div>
            </div>
        </div>
    );
};
