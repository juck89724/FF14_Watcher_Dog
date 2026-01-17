using System;
using System.Drawing;
using System.Threading.Tasks;
using PaddleOCRSharp;

namespace Watcher_PC
{
    public class OcrService
    {
        private PaddleOCREngine? _engine;

        /// <summary>
        /// 初始化 PaddleOCR 引擎
        /// </summary>
        public async Task<bool> InitAsync()
        {
            return await Task.Run(() =>
            {
                try
                {
                    // 1. 判定模型路徑 (優先使用 BaseDirectory，適用於 Published App)
                    string baseDir = AppDomain.CurrentDomain.BaseDirectory;
                    string inferencePath = System.IO.Path.Combine(baseDir, "inference");

                    if (!System.IO.Directory.Exists(inferencePath))
                    {
                        // Fallback: 嘗試 CurrentDirectory (適用於開發環境)
                        string currentDirInference = System.IO.Path.Combine(Environment.CurrentDirectory, "inference");
                        if (System.IO.Directory.Exists(currentDirInference))
                        {
                            inferencePath = currentDirInference;
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine($"[OCR] 錯誤: 找不到模型資料夾 (Checked: {inferencePath}, {currentDirInference})");
                            Logger.LogError($"[OCR] 錯誤: 找不到模型資料夾 (Checked: {inferencePath}, {currentDirInference})");
                            // 雖然找不到，但仍嘗試讓引擎自己找找看 (或報錯)
                        }
                    }

                    System.Diagnostics.Debug.WriteLine($"[OCR] 模型根目錄: {inferencePath}");
                    Logger.Log($"[OCR] 模型根目錄: {inferencePath}");

                    // 2. 設定模型配置 (Explicitly set paths to match the folders in 'inference')
                    OCRModelConfig config = new OCRModelConfig();
                    string detPath = System.IO.Path.Combine(inferencePath, "yt_PP-OCRv5_mobile_det_infer");
                    string recPath = System.IO.Path.Combine(inferencePath, "yt_PP-OCRv5_mobile_rec_infer");
                    string clsPath = System.IO.Path.Combine(inferencePath, "yt_PP-OCRv5_mobile_cls_infer");

                    // 檢查子目錄是否存在，若存在則指定
                    if (System.IO.Directory.Exists(detPath)) config.det_infer = detPath;
                    if (System.IO.Directory.Exists(recPath)) config.rec_infer = recPath;
                    if (System.IO.Directory.Exists(clsPath)) config.cls_infer = clsPath;

                    // 注意: 若 keys 文件缺失，引擎可能使用內建預設值或報錯。
                    // 若 inference 資料夾中有 keys 文件 (例如 ppocr_keys_v1.txt)，應在此指定:
                    // config.keys = Path.Combine(inferencePath, "ppocr_keys_v1.txt");

                    // 3. OCR 參數
                    OCRParameter ocrParameter = new OCRParameter()
                    {
                        // 建議開啟方向分類以提高辨識準確度 (若有需要)
                        // Enable_cls = true, 
                        // Use_angle_cls = true 
                    };

                    // 初始化引擎
                    _engine = new PaddleOCREngine(config, ocrParameter);
                    return true;
                }
                catch (Exception ex)
                {
                    // Log 錯誤資訊
                    System.Diagnostics.Debug.WriteLine($"[OCR] 引擎初始化失敗: {ex.Message}");
                    Logger.LogError($"[OCR] 引擎初始化失敗", ex);
                    // 若是 DllNotFoundException，通常是因為缺少 VC Redist (vc_redist.x64.exe)
                    return false;
                }
            });
        }

        /// <summary>
        /// 辨識圖片中的文字
        /// </summary>
        /// <param name="bitmap">來源圖片</param>
        /// <returns>辨識出的文字內容</returns>
        public async Task<string> RecognizeTextAsync(Bitmap bitmap)
        {
            if (_engine == null) return string.Empty;

            return await Task.Run(() =>
            {
                try
                {
                    // 執行文字偵測與辨識
                    var result = _engine.DetectText(bitmap);
                    if (result != null)
                    {
                        // 傳回合併後的文字內容
                        return result.Text;
                    }
                    return string.Empty;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[OCR] 辨識過程錯誤: {ex.Message}");
                    Logger.LogError($"[OCR] 辨識過程錯誤", ex);
                    return string.Empty;
                }
            });
        }
    }
}
