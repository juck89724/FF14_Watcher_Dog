import React, { useState } from 'react';
import { generateJobCycleMacro } from '../../utils/macroUtils';

export const MacroGenerator: React.FC = () => {
    const [macroText] = useState(generateJobCycleMacro());
    const [copied, setCopied] = useState(false);

    const handleCopy = () => {
        navigator.clipboard.writeText(macroText);
        setCopied(true);
        setTimeout(() => setCopied(false), 2000);
    };

    return (
        <div className="task-group" style={{ marginBottom: '24px' }}>
            <div className="task-list-header">
                自動切換職業巨集 (Macro)
            </div>
            <div style={{ color: 'var(--text-secondary)', fontSize: '0.9rem', marginBottom: '12px' }}>
                請複製以下巨集到遊戲中執行，以便掃描器讀取您的職業等級。請確保您的套裝列表 (Gearset List) 中已儲存對應的職業。
            </div>
            <div style={{ position: 'relative' }}>
                <textarea
                    readOnly
                    value={macroText}
                    style={{
                        width: '100%',
                        height: '200px',
                        backgroundColor: 'var(--item-bg-color)',
                        color: 'var(--text-primary)',
                        border: '1px solid #404040',
                        borderRadius: '8px',
                        padding: '12px',
                        fontFamily: 'monospace',
                        resize: 'vertical',
                        outline: 'none'
                    }}
                />
                <button
                    onClick={handleCopy}
                    style={{
                        position: 'absolute',
                        top: '12px',
                        right: '12px',
                        backgroundColor: copied ? 'var(--accent-color)' : 'var(--primary-color)',
                        color: '#000',
                        border: 'none',
                        borderRadius: '4px',
                        padding: '4px 12px',
                        fontWeight: 'bold',
                        cursor: 'pointer',
                        transition: 'all 0.2s ease'
                    }}
                >
                    {copied ? '已複製！' : '複製'}
                </button>
            </div>
        </div>
    );
};
