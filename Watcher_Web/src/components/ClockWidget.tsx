import React, { useState, useEffect } from 'react';
import { getEorzeaTime, formatTime } from '../utils/clockUtils';

export const ClockWidget: React.FC = () => {
    const [time, setTime] = useState(new Date());

    useEffect(() => {
        const timer = setInterval(() => setTime(new Date()), 1000); // 1-second update for smoothness logic, though UI only needs minute
        return () => clearInterval(timer);
    }, []);

    const localTimeStr = formatTime(time);
    const etTime = getEorzeaTime(time);
    const etTimeStr = formatTime(etTime);

    return (
        <div className="clock-container">
            <div className="clock-pill">
                <span className="clock-label">本</span>
                <span className="clock-time">{localTimeStr}</span>
            </div>
            <div className="clock-pill">
                <span className="clock-label">艾</span>
                <span className="clock-time">{etTimeStr}</span>
            </div>
        </div>
    );
};
