import { useState } from 'react';
import { useTaskStorage } from './hooks/useTaskStorage';
import { TaskList } from './components/TaskList';
import { TASKS } from './data/tasks';
import { ClockWidget } from './components/ClockWidget';
import { Scanner } from './components/Scanner/Scanner';
import { MacroGenerator } from './components/Scanner/MacroGenerator';
import './styles/index.css';
import './styles/App.css';

function App() {
  const { isTaskDone, toggleTask } = useTaskStorage();
  const [activeTab, setActiveTab] = useState<'todo' | 'scanner'>('todo');

  const dutyRouletteTasks = TASKS.filter((t) => t.category === 'duty_roulette');
  const dailyChallengeTasks = TASKS.filter((t) => t.category === 'daily_challenge');
  const missionInfoTasks = TASKS.filter((t) => t.category === 'mission_info');

  return (
    <div className="app-container">
      <h1 className="app-title">FF14 Let Me See See</h1>

      <ClockWidget />

      <div className="tab-navigation">
        <button
          className={`tab-button ${activeTab === 'todo' ? 'active' : ''}`}
          onClick={() => setActiveTab('todo')}
        >
          每日清單
        </button>
        <button
          className={`tab-button ${activeTab === 'scanner' ? 'active' : ''}`}
          onClick={() => setActiveTab('scanner')}
        >
          職業掃描
        </button>
      </div>

      {activeTab === 'scanner' && (
        <div className="task-columns" style={{ marginBottom: '24px' }}>
          <div className="task-group" style={{ flex: '2 1 600px' }}>
            <Scanner />
            <MacroGenerator />
          </div>
        </div>
      )}

      {activeTab === 'todo' && (
        <div className="task-columns">
          {/* Group 1: Duty Finder (Roulettes + Daily Challenge) */}
          <div className="task-group">
            <TaskList
              title="隨機任務"
              tasks={dutyRouletteTasks}
              checkStatus={isTaskDone}
              onToggle={toggleTask}
            />

            <TaskList
              title="每日挑戰"
              tasks={dailyChallengeTasks}
              checkStatus={isTaskDone}
              onToggle={toggleTask}
            />
          </div>

          {/* Group 2: Mission Info */}
          <div className="task-group">
            <TaskList
              title="任務情報"
              tasks={missionInfoTasks}
              checkStatus={isTaskDone}
              onToggle={toggleTask}
            />
          </div>
        </div>
      )}
    </div>
  );
}

export default App;
