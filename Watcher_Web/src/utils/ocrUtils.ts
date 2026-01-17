// @ts-ignore
import * as ocr from '@paddlejs-models/ocr';

export interface OCRResult {
    text: string;
    confidence: number;
}

// Initialize OCR model once
let isLoaded = false;
const initModel = async () => {
    if (isLoaded) return;
    try {
        // Use default CDN models as the npm package does not include model files locally
        // @ts-ignore
        await ocr.init();
        isLoaded = true;
        console.log('PaddleOCR Model Loaded');
    } catch (e) {
        console.error('Failed to load PaddleOCR:', e);
    }
};

// Trigger init immediately
initModel();

export const recognizeText = async (image: string): Promise<OCRResult> => {
    try {
        if (!isLoaded) await initModel();

        // PaddleJS handles canvas or image element
        // We have a dataURL string. We need to create an Image element.
        const img = new Image();
        img.src = image;

        await new Promise((resolve) => {
            img.onload = resolve;
        });

        const res = await ocr.recognize(img, {
            // canvas: canvasElement // Optional if you want to draw detection boxes
        });

        // PaddleOCR result structure varies, usually res.text is an array of strings
        // or res.data containing text blocks.
        // Let's assume standard output concatenation for now.
        // If 'res' is just the result object:
        console.log('PaddleOCR Result:', res);

        // Simple extraction (adjust based on actual return type of @paddlejs-models/ocr)
        if (res && res.text) {
            const text = Array.isArray(res.text) ? res.text.join('\n') : res.text;
            return {
                text: text || '',
                confidence: 0.9 // Placeholder
            };
        }

        return {
            text: '',
            confidence: 0,
        };

    } catch (error) {
        console.error('OCR Error:', error);
        return { text: '', confidence: 0 };
    }
};
