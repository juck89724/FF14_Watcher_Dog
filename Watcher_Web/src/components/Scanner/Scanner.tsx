import React, { useRef, useState, useEffect } from 'react';
// @ts-ignore
import { recognizeText } from '../../utils/ocrUtils';

interface ROI {
    id: string;
    label?: string;
    type?: 'text' | 'number';
    x: number;
    y: number;
    w: number;
    h: number;
}

interface CraftingStats {
    id: string; // Composite ID or just Job name
    job: string;
    level: number;
    cp: number;
    craftsmanship: number;
    control: number;
    timestamp: number;
}

const REQUIRED_REGIONS = [
    { id: 'job', label: '職業 (Job)', type: 'text' },
    { id: 'level', label: '等級 (Level)', type: 'number' },
    { id: 'cp', label: 'CP', type: 'number' },
    { id: 'craft', label: '作業精度', type: 'number' },
    { id: 'control', label: '加工精度', type: 'number' }
] as const;

const LOCAL_STORAGE_KEY_ROIS = 'ff14_scanner_rois';
const LOCAL_STORAGE_KEY_RECORDS = 'ff14_scanner_records';

export const Scanner: React.FC = () => {
    const videoRef = useRef<HTMLVideoElement>(null);
    const canvasRef = useRef<HTMLCanvasElement>(null);

    // 1. State Declarations
    const [isScanning, setIsScanning] = useState(false);
    const [status, setStatus] = useState<string>('等待開始...');

    // Scan Results
    const [scanResults, setScanResults] = useState<{ id: string; label: string; image: string; text: string }[]>([]);
    const [savedRecords, setSavedRecords] = useState<CraftingStats[]>(() => {
        const saved = localStorage.getItem(LOCAL_STORAGE_KEY_RECORDS);
        if (saved) {
            try {
                return JSON.parse(saved);
            } catch (e) {
                console.error("Failed to parse saved records", e);
            }
        }
        return [];
    });

    // ROI State (using ROI interface)
    const [rois, setRois] = useState<ROI[]>(() => {
        const saved = localStorage.getItem(LOCAL_STORAGE_KEY_ROIS);
        if (saved) {
            try {
                const parsed = JSON.parse(saved);
                // Simple validation to ensure it matches current structure
                if (Array.isArray(parsed) && parsed.length === REQUIRED_REGIONS.length) {
                    return parsed;
                }
            } catch (e) {
                console.error("Failed to parse saved ROIs", e);
            }
        }
        return REQUIRED_REGIONS.map(r => ({ ...r, x: 0, y: 0, w: 0, h: 0 }));
    });
    const [currentRoi, setCurrentRoi] = useState<ROI | null>(null);
    const [isSelecting, setIsSelecting] = useState(false);
    const selectionStart = useRef<{ x: number, y: number } | null>(null);

    // Active Selection State
    const [activeRoiId, setActiveRoiId] = useState<string | null>('job');

    // 2. Image Pre-processing (Helper Function)
    const preprocessImage = (ctx: CanvasRenderingContext2D, width: number, height: number) => {
        let imageData = ctx.getImageData(0, 0, width, height);
        const data = imageData.data;

        // Grayscale & Aggressive Threshold
        const threshVal = 100;
        for (let i = 0; i < data.length; i += 4) {
            const avg = 0.2126 * data[i] + 0.7152 * data[i + 1] + 0.0722 * data[i + 2];
            const val = avg > threshVal ? 0 : 255; // Black Text, White BG
            data[i] = val;
            data[i + 1] = val;
            data[i + 2] = val;
        }

        // Dilate (Thicken text)
        const copy = new Uint8ClampedArray(data);
        for (let y = 0; y < height - 1; y++) {
            for (let x = 0; x < width - 1; x++) {
                const idx = (y * width + x) * 4;
                // Check neighbors
                const neighbors = [
                    copy[idx],
                    copy[((y) * width + (x + 1)) * 4],
                    copy[((y + 1) * width + x) * 4],
                    copy[((y + 1) * width + (x + 1)) * 4]
                ];
                if (neighbors.some(p => p === 0)) {
                    data[idx] = 0;
                    data[idx + 1] = 0;
                    data[idx + 2] = 0;
                }
            }
        }
        ctx.putImageData(imageData, 0, 0);
    };

    // 3. Effects (Loop)
    // 3. Effects (Loop)
    // Save ROIs to local storage whenever they change
    useEffect(() => {
        // Only save if we have at least one valid ROI to avoid overwriting with empty defaults unnecessarily at start if logic was different
        // But since we initialize from storage, it's fine. We mainly want to save when user finishes a selection.
        if (rois.some(r => r.w > 0)) {
            localStorage.setItem(LOCAL_STORAGE_KEY_ROIS, JSON.stringify(rois));
        }
        if (rois.some(r => r.w > 0)) {
            localStorage.setItem(LOCAL_STORAGE_KEY_ROIS, JSON.stringify(rois));
        }
    }, [rois]);

    // Save Records to local storage
    useEffect(() => {
        localStorage.setItem(LOCAL_STORAGE_KEY_RECORDS, JSON.stringify(savedRecords));
    }, [savedRecords]);

    // 4. Scanning Loop
    useEffect(() => {
        let intervalId: any;
        // Check if we have valid ROIs to scan (width > 0)
        const hasValidRois = rois.some(r => r.w > 0 && r.h > 0);

        if (isScanning && hasValidRois) {
            intervalId = setInterval(async () => {
                if (videoRef.current) {
                    const video = videoRef.current;
                    const results: { id: string; label: string; image: string; text: string }[] = [];

                    if (video.videoWidth > 0) {
                        for (const r of rois) {
                            if (r.w <= 0 || r.h <= 0) continue;

                            const tempCanvas = document.createElement('canvas');
                            const tempCtx = tempCanvas.getContext('2d');
                            const SCALE = 4.0;
                            tempCanvas.width = r.w * SCALE;
                            tempCanvas.height = r.h * SCALE;

                            if (!tempCtx) continue;

                            tempCtx.fillStyle = '#ffffff';
                            tempCtx.fillRect(0, 0, tempCanvas.width, tempCanvas.height);
                            tempCtx.scale(SCALE, SCALE);
                            tempCtx.imageSmoothingEnabled = false;

                            tempCtx.drawImage(
                                video,
                                r.x, r.y, r.w, r.h,
                                0, 0, r.w, r.h
                            );

                            preprocessImage(tempCtx, tempCanvas.width, tempCanvas.height);
                            const dataUrl = tempCanvas.toDataURL('image/jpeg', 0.9);

                            const ocrRes = await recognizeText(dataUrl);

                            // Post-process
                            let cleanText = ocrRes.text.trim();
                            if (r.type === 'number') {
                                const numMatch = cleanText.match(/\d+/);
                                cleanText = numMatch ? numMatch[0] : '';
                            } else {
                                cleanText = cleanText.replace(/[^\u4e00-\u9fa5a-zA-Z\d\s]/g, '');

                                // Normalize Job Names based on user preferences and macro
                                if (r.id === 'job') {
                                    if (cleanText.includes('鍊金')) cleanText = '鍊金術士';
                                    else if (cleanText.includes('木工')) cleanText = '木工師';
                                    else if (cleanText.includes('鍛造')) cleanText = '鍛造師';
                                    else if (cleanText.includes('甲冑')) cleanText = '甲冑師';
                                    else if (cleanText.includes('金工')) cleanText = '金工師'; // Or 雕金
                                    else if (cleanText.includes('皮革')) cleanText = '皮革師';
                                    else if (cleanText.includes('裁縫')) cleanText = '裁縫師';
                                    else if (cleanText.includes('烹調')) cleanText = '烹調師';
                                    else if (cleanText.includes('採')) cleanText = '採掘師'; // Covers 採礦/採掘
                                    else if (cleanText.includes('園藝')) cleanText = '園藝師';
                                    else if (cleanText.includes('漁') || cleanText.includes('捕魚')) cleanText = '漁師';

                                    // Fallback text length limiter if no match (though mapped ones allow >3)
                                    // Only truncate if we didn't map it to a specific known job
                                    // But '鍊金術士' is 4 chars, so we accept that.
                                }
                            }

                            results.push({
                                id: r.id,
                                label: r.label || 'Unknown',
                                image: dataUrl,
                                text: cleanText
                            });
                        }

                        setScanResults(results);

                        // Parse and Save Unique Record
                        const jobVal = results.find(r => r.id === 'job')?.text;
                        const levelVal = parseInt(results.find(r => r.id === 'level')?.text || '0', 10);
                        const cpVal = parseInt(results.find(r => r.id === 'cp')?.text || '0', 10);
                        const craftVal = parseInt(results.find(r => r.id === 'craft')?.text || '0', 10);
                        const controlVal = parseInt(results.find(r => r.id === 'control')?.text || '0', 10);

                        if (jobVal && levelVal > 0 && cpVal > 0 && craftVal > 0 && controlVal > 0) {
                            setSavedRecords(prev => {
                                const newRecord = {
                                    id: Date.now().toString(),
                                    job: jobVal,
                                    level: levelVal,
                                    cp: cpVal,
                                    craftsmanship: craftVal,
                                    control: controlVal,
                                    timestamp: Date.now()
                                };

                                const existingIndex = prev.findIndex(r => r.job === jobVal);
                                if (existingIndex >= 0) {
                                    const updated = [...prev];
                                    updated[existingIndex] = newRecord;
                                    return updated;
                                }
                                return [...prev, newRecord];
                            });
                        }

                        setStatus(`監控中... (已設定 ${results.length} 個區域)`);
                    }
                }
            }, 3000);
        } else if (isScanning && !hasValidRois) {
            setStatus('請依照指示框選數值區域...');
        }
        return () => clearInterval(intervalId);
    }, [isScanning, rois]);

    const updateRecord = (id: string, field: keyof CraftingStats, value: string | number) => {
        setSavedRecords(prev => prev.map(r =>
            r.id === id ? { ...r, [field]: value } : r
        ));
    };

    const startCapture = async () => {
        try {
            const displayMediaOptions = {
                video: { cursor: "always" },
                audio: false
            } as any;
            const stream = await navigator.mediaDevices.getDisplayMedia(displayMediaOptions);

            if (videoRef.current) {
                videoRef.current.srcObject = stream;
                videoRef.current.srcObject = stream;
                videoRef.current.play();
                setIsScanning(true);

                // If we already have valid ROIs loaded from storage, we don't need to force selection mode immediately
                // unless the user clicks "Reset".
                const allSet = rois.every(r => r.w > 0 && r.h > 0);
                if (!allSet) {
                    setActiveRoiId('job');
                    setStatus('請依照下方列表框選對應區域...');
                } else {
                    setActiveRoiId(null);
                    setStatus('已載入設定，開始監控...');
                }
            }
            stream.getVideoTracks()[0].onended = () => stopCapture();
        } catch (err) {
            console.error(err);
            setStatus('無法取得畫面');
        }
    };

    const stopCapture = () => {
        if (videoRef.current && videoRef.current.srcObject) {
            const tracks = (videoRef.current.srcObject as MediaStream).getTracks();
            tracks.forEach(track => track.stop());
            videoRef.current.srcObject = null;
        }
        setIsScanning(false);
        setScanResults([]);
        setStatus('已停止');
    };

    // Drawing
    useEffect(() => {
        let animId: number;
        const draw = () => {
            if (videoRef.current && canvasRef.current) {
                const video = videoRef.current;
                const canvas = canvasRef.current;
                const ctx = canvas.getContext('2d');

                if (ctx && video.readyState === video.HAVE_ENOUGH_DATA) {
                    canvas.width = video.videoWidth;
                    canvas.height = video.videoHeight;
                    ctx.drawImage(video, 0, 0, canvas.width, canvas.height);

                    ctx.fillStyle = 'rgba(0,0,0,0.5)';
                    ctx.fillRect(0, 0, canvas.width, canvas.height);

                    rois.forEach((r) => {
                        if (r.w <= 0 || r.h <= 0) return;

                        ctx.drawImage(video, r.x, r.y, r.w, r.h, r.x, r.y, r.w, r.h);
                        const isActive = r.id === activeRoiId;
                        ctx.strokeStyle = isActive ? '#00ffff' : '#00ff00';
                        ctx.lineWidth = isActive ? 3 : 2;
                        ctx.strokeRect(r.x, r.y, r.w, r.h);
                        ctx.fillStyle = isActive ? '#00ffff' : '#00ff00';
                        ctx.font = 'bold 14px Arial';
                        ctx.fillText(r.label || '', r.x, r.y - 6);
                    });

                    if (currentRoi) {
                        ctx.drawImage(video, currentRoi.x, currentRoi.y, currentRoi.w, currentRoi.h, currentRoi.x, currentRoi.y, currentRoi.w, currentRoi.h);
                        ctx.strokeStyle = '#ffff00';
                        ctx.lineWidth = 2;
                        ctx.setLineDash([5, 5]);
                        ctx.strokeRect(currentRoi.x, currentRoi.y, currentRoi.w, currentRoi.h);
                        ctx.setLineDash([]);

                        // Show what we are currently selecting
                        if (activeRoiId) {
                            const target = REQUIRED_REGIONS.find(r => r.id === activeRoiId);
                            if (target) {
                                ctx.fillStyle = '#ffff00';
                                ctx.font = 'bold 14px Arial';
                                ctx.fillText(`正在選取: ${target.label}`, currentRoi.x, currentRoi.y - 6);
                            }
                        }
                    }
                }
            }
            animId = requestAnimationFrame(draw);
        };
        if (isScanning) draw();
        return () => cancelAnimationFrame(animId);
    }, [isScanning, rois, currentRoi, activeRoiId]);

    const handleMouseDown = (e: React.MouseEvent<HTMLCanvasElement>) => {
        if (!isScanning) return;
        const rect = canvasRef.current?.getBoundingClientRect();
        if (!rect) return;
        const scaleX = canvasRef.current!.width / rect.width;
        const scaleY = canvasRef.current!.height / rect.height;

        selectionStart.current = {
            x: (e.clientX - rect.left) * scaleX,
            y: (e.clientY - rect.top) * scaleY
        };
        setIsSelecting(true);
    };

    const handleMouseMove = (e: React.MouseEvent<HTMLCanvasElement>) => {
        if (!isSelecting || !selectionStart.current) return;
        const rect = canvasRef.current?.getBoundingClientRect();
        if (!rect) return;
        const scaleX = canvasRef.current!.width / rect.width;
        const scaleY = canvasRef.current!.height / rect.height;

        const currX = (e.clientX - rect.left) * scaleX;
        const currY = (e.clientY - rect.top) * scaleY;

        const w = currX - selectionStart.current.x;
        const h = currY - selectionStart.current.y;

        setCurrentRoi({
            id: 'temp', // Temporary ID
            label: 'Selection',
            x: w > 0 ? selectionStart.current.x : currX,
            y: h > 0 ? selectionStart.current.y : currY,
            w: Math.abs(w),
            h: Math.abs(h)
        });
    };

    const handleMouseUp = () => {
        setIsSelecting(false);
        if (currentRoi && currentRoi.w > 10 && currentRoi.h > 10 && activeRoiId) {
            // Update the active ROI
            setRois(prev => prev.map(r => {
                if (r.id === activeRoiId) {
                    return { ...r, x: currentRoi.x, y: currentRoi.y, w: currentRoi.w, h: currentRoi.h };
                }
                return r;
            }));

            // Auto-advance to next region if available
            const currentIndex = REQUIRED_REGIONS.findIndex(r => r.id === activeRoiId);
            if (currentIndex !== -1 && currentIndex < REQUIRED_REGIONS.length - 1) {
                const nextId = REQUIRED_REGIONS[currentIndex + 1].id;
                setActiveRoiId(nextId);
                setStatus(`已儲存！請繼續框選「${REQUIRED_REGIONS[currentIndex + 1].label}」`);
            } else {
                setActiveRoiId(null);
                setStatus('所有區域已設定完成！');
            }
        }
        selectionStart.current = null;
        setCurrentRoi(null);
    };

    return (
        <div className="task-group" style={{ marginBottom: '24px' }}>
            <div className="task-list-header">螢幕掃描器 (Scanner)</div>
            <div style={{ display: 'flex', flexDirection: 'column', gap: '12px' }}>
                <div style={{ color: 'var(--text-secondary)', fontSize: '0.9rem' }}>
                    1. 點擊「開始掃描」。 2. 依照指示<b>依序框選</b>各個數值區域。也可直接點擊下方按鈕重新選取特定區域。
                </div>

                <div style={{ display: 'flex', gap: '12px', alignItems: 'center' }}>
                    {!isScanning ? (
                        <button onClick={startCapture} style={{ backgroundColor: 'var(--primary-color)', color: '#000', padding: '8px 16px', border: 'none', borderRadius: '4px', fontWeight: 'bold', cursor: 'pointer' }}>開始掃描</button>
                    ) : (
                        <div style={{ display: 'flex', gap: '8px' }}>
                            <button onClick={stopCapture} style={{ backgroundColor: '#EF4444', color: '#fff', padding: '8px 16px', border: 'none', borderRadius: '4px', fontWeight: 'bold', cursor: 'pointer' }}>停止</button>
                            <button onClick={() => {
                                setRois(REQUIRED_REGIONS.map(r => ({ ...r, x: 0, y: 0, w: 0, h: 0 })));
                                setActiveRoiId('job');
                            }} style={{ backgroundColor: '#333', color: '#fff', padding: '8px 16px', border: '1px solid #666', borderRadius: '4px', fontWeight: 'bold', cursor: 'pointer' }}>全部重設</button>
                        </div>
                    )}
                    <span style={{ color: 'var(--text-secondary)' }}>{status}</span>
                </div>

                {/* Selection Indicators */}
                {isScanning && (
                    <div style={{ display: 'flex', gap: '8px', flexWrap: 'wrap' }}>
                        {rois.map(r => {
                            const isSet = r.w > 0;
                            const isActive = r.id === activeRoiId;
                            return (
                                <button
                                    key={r.id}
                                    onClick={() => setActiveRoiId(r.id)}
                                    style={{
                                        padding: '6px 12px',
                                        borderRadius: '20px',
                                        border: isActive ? '2px solid #00ffff' : '1px solid #444',
                                        backgroundColor: isSet ? '#059669' : '#333',
                                        color: '#fff',
                                        cursor: 'pointer',
                                        fontSize: '0.85rem',
                                        opacity: isActive ? 1 : 0.8
                                    }}
                                >
                                    {r.label} {isSet ? '✓' : ''}
                                </button>
                            );
                        })}
                    </div>
                )}

                <video ref={videoRef} style={{ display: 'none' }} />
                <div style={{ overflow: 'auto', maxHeight: '600px', border: '1px solid #333', position: 'relative' }}>
                    <canvas ref={canvasRef} onMouseDown={handleMouseDown} onMouseMove={handleMouseMove} onMouseUp={handleMouseUp} onMouseLeave={handleMouseUp} style={{ cursor: isScanning ? 'crosshair' : 'default', maxWidth: '100%', display: isScanning ? 'block' : 'none' }} />
                </div>

                {/* Scan Results for Debug/View */}
                {scanResults.length > 0 && (
                    <div style={{ marginTop: '16px', display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(200px, 1fr))', gap: '12px' }}>
                        {scanResults.map((res) => (
                            <div key={res.id} style={{ backgroundColor: '#1a1a1a', padding: '12px', borderRadius: '8px', border: '1px solid #333' }}>
                                <div style={{ fontSize: '0.8rem', color: '#888', marginBottom: '8px', fontWeight: 'bold' }}>{res.label}</div>
                                <div style={{ display: 'flex', gap: '12px' }}>
                                    <img src={res.image} style={{ height: '40px', border: '1px solid #444' }} alt={res.label} />
                                    <div style={{ fontSize: '1.2rem', color: 'var(--primary-color)', fontFamily: 'monospace', alignSelf: 'center' }}>
                                        {res.text || '-'}
                                    </div>
                                </div>
                            </div>
                        ))}
                    </div>
                )}

                {/* Saved Records List */}
                {savedRecords.length > 0 && (
                    <div style={{ marginTop: '24px', borderTop: '1px solid #333', paddingTop: '16px' }}>
                        <div style={{ fontSize: '1rem', fontWeight: 'bold', marginBottom: '12px' }}>已記錄職業數值 ({savedRecords.length})</div>
                        <table style={{ width: '100%', borderCollapse: 'collapse', fontSize: '0.9rem' }}>
                            <thead>
                                <tr style={{ borderBottom: '1px solid #444', textAlign: 'left', color: '#888' }}>
                                    <th style={{ padding: '8px' }}>職業</th>
                                    <th style={{ padding: '8px' }}>等級</th>
                                    <th style={{ padding: '8px' }}>CP</th>
                                    <th style={{ padding: '8px' }}>作業精度</th>
                                    <th style={{ padding: '8px' }}>加工精度</th>
                                    <th style={{ padding: '8px' }}>時間</th>
                                </tr>
                            </thead>
                            <tbody>
                                {savedRecords.map(record => (
                                    <tr key={record.id} style={{ borderBottom: '1px solid #222' }}>
                                        <td style={{ padding: '8px' }}>
                                            <input
                                                value={record.job}
                                                onChange={(e) => updateRecord(record.id, 'job', e.target.value)}
                                                disabled={isScanning}
                                                style={{ background: '#222', border: '1px solid #444', color: 'var(--primary-color)', fontWeight: 'bold', width: '80px', borderRadius: '4px', padding: '2px 4px', fontSize: '0.9rem' }}
                                            />
                                        </td>
                                        <td style={{ padding: '8px' }}>
                                            <input
                                                type="number"
                                                value={record.level}
                                                onChange={(e) => updateRecord(record.id, 'level', parseInt(e.target.value) || 0)}
                                                disabled={isScanning}
                                                style={{ background: '#222', border: '1px solid #444', color: '#fff', width: '60px', borderRadius: '4px', padding: '2px 4px' }}
                                            />
                                        </td>
                                        <td style={{ padding: '8px' }}>
                                            <input
                                                type="number"
                                                value={record.cp}
                                                onChange={(e) => updateRecord(record.id, 'cp', parseInt(e.target.value) || 0)}
                                                disabled={isScanning}
                                                style={{ background: '#222', border: '1px solid #444', color: '#fff', width: '60px', borderRadius: '4px', padding: '2px 4px' }}
                                            />
                                        </td>
                                        <td style={{ padding: '8px' }}>
                                            <input
                                                type="number"
                                                value={record.craftsmanship}
                                                onChange={(e) => updateRecord(record.id, 'craftsmanship', parseInt(e.target.value) || 0)}
                                                disabled={isScanning}
                                                style={{ background: '#222', border: '1px solid #444', color: '#fff', width: '60px', borderRadius: '4px', padding: '2px 4px' }}
                                            />
                                        </td>
                                        <td style={{ padding: '8px' }}>
                                            <input
                                                type="number"
                                                value={record.control}
                                                onChange={(e) => updateRecord(record.id, 'control', parseInt(e.target.value) || 0)}
                                                disabled={isScanning}
                                                style={{ background: '#222', border: '1px solid #444', color: '#fff', width: '60px', borderRadius: '4px', padding: '2px 4px' }}
                                            />
                                        </td>
                                        <td style={{ padding: '8px', color: '#666', fontSize: '0.8rem' }}>
                                            {new Date(record.timestamp).toLocaleTimeString()}
                                        </td>
                                    </tr>
                                ))}
                            </tbody>
                        </table>
                        <button
                            onClick={() => setSavedRecords([])}
                            style={{ marginTop: '12px', background: 'transparent', border: '1px solid #444', color: '#888', padding: '4px 12px', borderRadius: '4px', cursor: 'pointer', fontSize: '0.8rem' }}
                        >
                            清除所有記錄
                        </button>
                    </div>
                )}
            </div>
        </div>
    );
};
