using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using OpenCvSharp;
using System.Text.RegularExpressions; // Added for Regex extraction

namespace Watcher_PC
{
    /// <summary>
    /// MainWindow.xaml 的互動邏輯
    /// </summary>
    public partial class MainWindow : System.Windows.Window
    {
        private CancellationTokenSource? _cancellationTokenSource;
        private const string TargetWindowName = "FINAL FANTASY XIV";

        public MainWindow()
        {
            InitializeComponent();
            this.Loaded += MainWindow_Loaded;
            this.Closing += MainWindow_Closing;
        }

        private DateTime _currentViewDate = DateTime.Today;

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            Log("應用程式已啟動 (WPF Mode)。");
            StartWatcher();

            // Initial UI Refresh for Tasks
            RefreshTaskList();
        }

        private void RefreshTaskList()
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                var tasks = _dailyTaskManager.GetTasksForDate(_currentViewDate);
                TaskList.ItemsSource = null;
                TaskList.ItemsSource = tasks;

                // Update Date Text
                if (TxtCurrentDate != null)
                {
                    TxtCurrentDate.Text = _currentViewDate.ToString("yyyy-MM-dd");
                }
            });
        }

        private void BtnPrevDay_Click(object sender, RoutedEventArgs e)
        {
            _currentViewDate = _currentViewDate.AddDays(-1);
            RefreshTaskList();
        }

        private void BtnNextDay_Click(object sender, RoutedEventArgs e)
        {
            _currentViewDate = _currentViewDate.AddDays(1);
            RefreshTaskList();
        }

        private void TaskCheckBox_Click(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.CheckBox cb && cb.Tag is string taskName)
            {
                bool isChecked = cb.IsChecked == true;
                _dailyTaskManager.SetTaskStatus(_currentViewDate, taskName, isChecked);

                // Refresh to ensure UI consistency (e.g. colors)
                RefreshTaskList();

                Log($"[手動] 已{(isChecked ? "勾選" : "取消")}任務: {taskName} ({_currentViewDate:MM/dd})");
            }
        }

        private void MainWindow_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
        {
            StopWatcher();
        }

        private void StartWatcher()
        {
            _cancellationTokenSource = new CancellationTokenSource();
            var token = _cancellationTokenSource.Token;

            // 在背景執行監控迴圈
            Task.Run(async () => await WatcherLoop(token), token);
        }

        private void StopWatcher()
        {
            _cancellationTokenSource?.Cancel();
        }

        private OcrService _ocrService = new OcrService();
        private TemplateMatcher _templateMatcher = new TemplateMatcher();
        private DailyTaskManager _dailyTaskManager = new DailyTaskManager(); // Initialize Manager

        // 訊息去重快取
        private HashSet<string> _messageHistory = new HashSet<string>();
        private Queue<string> _messageQueue = new Queue<string>();
        private const int MaxHistory = 50; // 保留最近 50 條訊息防止重複輸出

        // 紀錄上次的設定值，用於偵測變動
        private int _lastOffX, _lastOffY, _lastW, _lastH;
        private System.Drawing.Rectangle? _lastAnchorRect;

        private TriggerService _triggerService = new TriggerService();

        private string _deviceUuid = "";
        private string _currentDutyName = "未知副本";
        // private static readonly System.Net.Http.HttpClient _httpClient = new System.Net.Http.HttpClient(); // Removed for local mode

        private async Task WatcherLoop(CancellationToken token)
        {
            try
            {
                // 1. Init OCR
                Log("[系統] 初始化 PaddleOCR 引擎...");
                if (!await _ocrService.InitAsync())
                {
                    Log("[錯誤] OCR 初始化失敗，請檢查模型文件是否存在且支援 AVX 指令集。");
                    return;
                }
                Log("[系統] PaddleOCR 引擎就緒。");

                // 2. Init OpenCV (Check Version Only)
                Log("[系統] 正在檢查 OpenCvSharp 版本...");
                try
                {
                    var version = Cv2.GetVersionString();
                    Log($"[系統] OpenCvSharp 初始化成功，版本: {version}");
                }
                catch (Exception ex)
                {
                    Log($"[錯誤] OpenCvSharp 初始化失敗: {ex.Message}");
                    return;
                }

                // 3. Init Trigger Service
                if (_triggerService.LoadConfig())
                {
                    Log("[系統] 已載入偵測規則 (triggers.json)。");
                }
                else
                {
                    Log("[警告] 無法載入偵測規則 triggers.json，將無法偵測事件。");
                }

                // 3. Init UUID & QR Code
                _deviceUuid = GetOrCreateDeviceUuid();
                // ShowQrCode(_deviceUuid); // Hidden UI
                Log($"[系統] 裝置 UUID: {_deviceUuid}");
                Log($"[系統] 本地任務清單已載入");

                // 檢查初始設定，若未設定則自動切換到「即時預覽」分頁並提示
                Application.Current.Dispatcher.Invoke(() =>
                {
                    int.TryParse(ConfWidth.Text, out int w);
                    int.TryParse(ConfHeight.Text, out int h);
                    if (w <= 0 || h <= 0)
                    {
                        MainTabs.SelectedIndex = 1; // 切換到「即時預覽」分頁
                        Log("[提示] 請先點擊 [🔍 框選監控範圍] 按鈕來設定要監控的區域。");
                    }
                });

                Log("[系統] 開始監控迴圈...");

                while (!token.IsCancellationRequested)
                {
                    try
                    {
                        IntPtr hwnd = NativeMethods.FindWindow(null, TargetWindowName);

                        if (hwnd != IntPtr.Zero)
                        {
                            UpdateStatus($"狀態: 監控中 - 發現視窗 ({hwnd})");

                            // 1. 取得視窗客戶區座標 (Client Rect) - 去除標題列與邊框
                            // 這樣使用者移動視窗，相對座標依然準確
                            NativeMethods.GetClientRect(hwnd, out var clientRect);
                            int clientW = clientRect.Right - clientRect.Left;
                            int clientH = clientRect.Bottom - clientRect.Top;

                            // 2. 讀取設定的 ROI (Region of Interest)
                            int roiX = 0, roiY = 0, roiW = 0, roiH = 0;
                            bool hasValidSettings = false;

                            Application.Current.Dispatcher.Invoke(() =>
                            {
                                int.TryParse(ConfOffsetX.Text, out roiX); // 這裡我們將 OffSet 直接視為 X 座標
                                int.TryParse(ConfOffsetY.Text, out roiY); // 這裡我們將 OffSet 直接視為 Y 座標
                                int.TryParse(ConfWidth.Text, out roiW);
                                int.TryParse(ConfHeight.Text, out roiH);

                                // 簡單檢核設定是否有效 (寬高必須大於 0)
                                if (roiW > 0 && roiH > 0)
                                {
                                    hasValidSettings = true;
                                }
                            });

                            if (!hasValidSettings)
                            {
                                UpdateStatus("狀態: 等待設定 - 請點擊 [選取範圍] 按鈕設定監控區域");
                                // 避免空轉佔用 CPU，稍作等待
                                await Task.Delay(1000, token);
                                continue;
                            }

                            // 偵測設定變動
                            if (roiX != _lastOffX || roiY != _lastOffY || roiW != _lastW || roiH != _lastH)
                            {
                                _messageHistory.Clear();
                                _messageQueue.Clear();
                                Log($"[系統] 監控範圍已更新: ({roiX},{roiY}) {roiW}x{roiH}");
                                _lastOffX = roiX;
                                _lastOffY = roiY;
                                _lastW = roiW;
                                _lastH = roiH;
                            }

                            // 3. 擷取視窗畫面 (全視窗)
                            using var windowBitmap = ImageHelper.CaptureWindow(hwnd);

                            if (windowBitmap != null)
                            {
                                System.Drawing.Bitmap? currentProcessedBitmap = null;

                                try
                                {
                                    // 4. 根據設定裁切 (ROI)
                                    // 邊界檢查: 防止裁切出界導致崩潰
                                    int safeX = Math.Max(0, roiX);
                                    int safeY = Math.Max(0, roiY);
                                    int safeW = Math.Min(windowBitmap.Width - safeX, roiW);
                                    int safeH = Math.Min(windowBitmap.Height - safeY, roiH);

                                    if (safeW > 0 && safeH > 0)
                                    {
                                        var cropRect = new System.Drawing.Rectangle(safeX, safeY, safeW, safeH);
                                        using var cropped = windowBitmap.Clone(cropRect, windowBitmap.PixelFormat);

                                        // 顯示原始彩色截圖給使用者看，比較好確認範圍
                                        UpdatePreview(cropped);

                                        // 影像前處理 (二值化等)
                                        currentProcessedBitmap = ImageHelper.PreProcessImage(cropped);
                                    }

                                    if (currentProcessedBitmap != null)
                                    {
                                        // 執行 OCR (使用處理過的圖片)
                                        var text = await _ocrService.RecognizeTextAsync(currentProcessedBitmap);

                                        if (!string.IsNullOrWhiteSpace(text))
                                        {
                                            ProcessText(text);
                                        }
                                    }
                                }
                                finally
                                {
                                    currentProcessedBitmap?.Dispose();
                                }
                            }
                        }
                        else
                        {
                            UpdateStatus($"狀態: 待機中 - 找不到視窗 '{TargetWindowName}'");
                        }
                    }
                    catch (Exception ex)
                    {
                        Log($"[錯誤] 監控迴圈異常: {ex.Message}");
                    }

                    await Task.Delay(3000, token);
                }
            }
            catch (TaskCanceledException)
            {
                Log("[系統] 監控已停止。");
            }
        }

        private void UpdatePreview(System.Drawing.Bitmap bitmap)
        {
            // Must clone the bitmap to show it on UI thread, as the original might be disposed
            var clone = (System.Drawing.Bitmap)bitmap.Clone();
            Application.Current.Dispatcher.Invoke(() =>
            {
                try
                {
                    CapturePreview.Source = BitmapToImageSource(clone);
                }
                finally
                {
                    clone.Dispose();
                }
            });
        }


        private void ProcessText(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return;

            // 1. 調整切分行邏輯
            string splitPattern = @"(?=\[\d{1,2}[:：]\d{2}\])|(?=[\r\n])";
            var lines = System.Text.RegularExpressions.Regex.Split(text, splitPattern);

            foreach (var rawLine in lines)
            {
                if (string.IsNullOrWhiteSpace(rawLine)) continue;
                string cleanedLine = TextProcessor.CleanText(rawLine);

                if (string.IsNullOrWhiteSpace(cleanedLine) || cleanedLine.Length < 2) continue;

                if (_messageHistory.Contains(cleanedLine)) continue;

                _messageHistory.Add(cleanedLine);
                _messageQueue.Enqueue(cleanedLine);

                if (_messageQueue.Count > MaxHistory)
                {
                    var old = _messageQueue.Dequeue();
                    _messageHistory.Remove(old);
                }

                Log($"[OCR] {cleanedLine}");

                // Start Trigger
                var startRule = _triggerService.CheckStart(cleanedLine);
                if (startRule != null)
                {
                    Log($"[提醒] {startRule.LogMessage}");

                    if (startRule.Type == "ExtractName")
                    {
                        string extractedName = TextProcessor.ExtractDutyName(rawLine);
                        if (!string.IsNullOrEmpty(extractedName))
                        {
                            _currentDutyName = extractedName;
                            Log($"[情報] 副本名稱: {_currentDutyName}");
                            // 這裡可以嘗試根據副本名稱自動判斷是哪個任務 (如果有對照表)
                        }
                    }
                    else if (startRule.Type == "FixedName")
                    {
                        _currentDutyName = startRule.FixedName;
                        Log($"[情報] 副本名稱: {_currentDutyName}");
                    }
                }
                else
                {
                    // End Trigger
                    var endRule = _triggerService.CheckEnd(cleanedLine);
                    if (endRule != null)
                    {
                        Log($"[完成] {endRule.LogMessage}");

                        string? detectedTaskName = null;

                        // 1. Regex Extraction
                        if (endRule.Type == "RegexExtract" && !string.IsNullOrEmpty(endRule.Regex))
                        {
                            try
                            {
                                var match = Regex.Match(cleanedLine, endRule.Regex);
                                if (match.Success)
                                {
                                    detectedTaskName = match.Groups["name"].Value;
                                    Log($"[情報] 識別到任務關鍵字 (Regex): {detectedTaskName}");
                                }
                            }
                            catch (Exception ex)
                            {
                                Log($"[錯誤] Regex 匹配失敗: {ex.Message}");
                            }
                        }

                        // 3. Update Local Manager
                        string? completedTaskName = null;

                        // Priority 1: Use detected name from end message
                        if (!string.IsNullOrEmpty(detectedTaskName))
                        {
                            completedTaskName = _dailyTaskManager.TryCompleteTask(detectedTaskName);
                        }

                        // Priority 2: Use stored global duty name (from Start trigger)
                        if (completedTaskName == null && !string.IsNullOrEmpty(_currentDutyName) && _currentDutyName != "未知副本")
                        {
                            Log($"[情報] 嘗試使用暫存副本名稱匹配: {_currentDutyName}");
                            completedTaskName = _dailyTaskManager.TryCompleteTask(_currentDutyName);
                        }

                        // Priority 3: Try the full cleaned line
                        if (completedTaskName == null)
                        {
                            completedTaskName = _dailyTaskManager.TryCompleteTask(cleanedLine);
                        }

                        if (completedTaskName != null)
                        {
                            Log($"[紀錄] ✅ 已完成每日任務: {completedTaskName}");
                            // Clear stored duty name to avoid stale usage
                            _currentDutyName = "未知副本";
                            RefreshTaskList();
                        }
                        else
                        {
                            Log($"[紀錄] 未能自動匹配到列表中的任務，請手動確認。");
                        }
                    }
                }
            }
        }

        private string GetOrCreateDeviceUuid()
        {
            string path = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "device_id.txt");
            if (System.IO.File.Exists(path))
            {
                return System.IO.File.ReadAllText(path).Trim();
            }
            else
            {
                string newUuid = Guid.NewGuid().ToString();
                System.IO.File.WriteAllText(path, newUuid);
                return newUuid;
            }
        }

        // Helper to update UI safely
        private void Log(string message)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                string timestamp = DateTime.Now.ToString("HH:mm:ss");
                LogText.AppendText($"[{timestamp}] {message}{Environment.NewLine}");
                LogText.ScrollToEnd();
            });
        }

        private void UpdateStatus(string status)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                StatusText.Text = status;
            });
        }

        // Helper to display QR Code
        public void ShowQrCode(string content)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                try
                {
                    using var generator = new QRCoder.QRCodeGenerator();
                    using var data = generator.CreateQrCode(content, QRCoder.QRCodeGenerator.ECCLevel.Q);
                    using var code = new QRCoder.QRCode(data);
                    using var bitmap = code.GetGraphic(20);

                    QrImage.Source = BitmapToImageSource(bitmap);
                    UuidText.Text = $"UUID: {content}";
                }
                catch (Exception ex)
                {
                    Log($"[錯誤] QR Code 生成失敗: {ex.Message}");
                }
            });
        }

        private void BtnSelectRegion_Click(object sender, RoutedEventArgs e)
        {
            // Removed check for _lastAnchorRect to allow manual selection without pin
            // if (_lastAnchorRect == null) { ... }

            // Find Game Window
            IntPtr hwnd = NativeMethods.FindWindow(null, TargetWindowName);
            if (hwnd == IntPtr.Zero)
            {
                MessageBox.Show("找不到遊戲視窗，請先啟動遊戲。", "錯誤", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // 使用截圖選取模式 (更準確，不受 DPI/邊框影響)
            ShowSnapshotSelection(hwnd);
        }

        private void ShowSnapshotSelection(IntPtr hwnd)
        {
            using var bitmap = ImageHelper.CaptureWindow(hwnd);
            if (bitmap == null)
            {
                MessageBox.Show("無法擷取畫面。", "錯誤", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // Convert to WPF Image for display
            var imageSource = BitmapToImageSource(bitmap);

            // Create Selection Window
            var selectionWin = new SelectionWindow();

            // Set background to the captured image
            var brush = new System.Windows.Media.ImageBrush(imageSource);
            brush.Stretch = System.Windows.Media.Stretch.Uniform; // Ensure it scales uniformly
            selectionWin.Background = brush;

            // --- Scaling Logic ---
            // Get current screen size (Logical Units)
            double screenW = SystemParameters.PrimaryScreenWidth;
            double screenH = SystemParameters.PrimaryScreenHeight;

            // Get standard DPI scale (approximate, for initial sizing)
            var source = PresentationSource.FromVisual(this);
            double dpiX = 1.0, dpiY = 1.0;
            if (source != null && source.CompositionTarget != null)
            {
                dpiX = source.CompositionTarget.TransformToDevice.M11;
                dpiY = source.CompositionTarget.TransformToDevice.M22;
            }

            // Bitmap Original Size (Physical Pixels)
            double bmpW = bitmap.Width;
            double bmpH = bitmap.Height;

            // Convert Bitmap to Logical Size (what it would be at 100% scale)
            double bmpLogicalW = bmpW / dpiX;
            double bmpLogicalH = bmpH / dpiY;

            // Determine Target Size (Max 90% of screen)
            double maxW = screenW * 0.9;
            double maxH = screenH * 0.9;

            // Calculate Scale Ratio to fit
            double scale = 1.0;
            if (bmpLogicalW > maxW || bmpLogicalH > maxH)
            {
                double scaleW = maxW / bmpLogicalW;
                double scaleH = maxH / bmpLogicalH;
                scale = Math.Min(scaleW, scaleH);
            }

            // Apply Scale
            selectionWin.Width = bmpLogicalW * scale;
            selectionWin.Height = bmpLogicalH * scale;

            // Center the window
            selectionWin.WindowStartupLocation = WindowStartupLocation.CenterScreen;

            if (selectionWin.ShowDialog() == true && selectionWin.IsConfirmed)
            {
                var selectionVisual = selectionWin.SelectedRegion;

                // Handle missing anchor by assuming (0,0)
                System.Drawing.Rectangle anchor;
                if (_lastAnchorRect.HasValue)
                {
                    anchor = _lastAnchorRect.Value;
                }
                else
                {
                    anchor = new System.Drawing.Rectangle(0, 0, 0, 0);
                    Log("[警告] 尚未偵測到定位點，使用視窗左上角 (0,0) 作為參考點。");
                }

                // --- Restore Coordinates to Original Image Scale ---
                // Ratio = Original Physical Bitmap Width / Scaled Window Logical Width
                // Use ActualWidth to stay safe
                double ratio = bmpW / selectionWin.Width;

                int selPhysicalX = (int)(selectionVisual.X * ratio);
                int selPhysicalY = (int)(selectionVisual.Y * ratio);
                int selPhysicalW = (int)(selectionVisual.Width * ratio);
                int selPhysicalH = (int)(selectionVisual.Height * ratio);

                // --- Absolute Coordinate Logic (No Anchor) ---
                // Setup UI to reflect absolute coords
                int offX = selPhysicalX;
                int offY = selPhysicalY;
                int w = selPhysicalW;
                int h = selPhysicalH;

                ConfOffsetX.Text = offX.ToString();
                ConfOffsetY.Text = offY.ToString();
                ConfWidth.Text = w.ToString();
                ConfHeight.Text = h.ToString();

                Log($"[系統] 已設定監控範圍 (絕對座標): X={offX}, Y={offY}, W={w}, H={h}");

                // 立即觸發一次畫面更新
                _lastOffX = -1; // Force reset
            }
        }

        private System.Windows.Media.ImageSource BitmapToImageSource(System.Drawing.Bitmap bitmap)
        {
            using (System.IO.MemoryStream memory = new System.IO.MemoryStream())
            {
                bitmap.Save(memory, System.Drawing.Imaging.ImageFormat.Bmp);
                memory.Position = 0;
                System.Windows.Media.Imaging.BitmapImage bitmapimage = new System.Windows.Media.Imaging.BitmapImage();
                bitmapimage.BeginInit();
                bitmapimage.StreamSource = memory;
                bitmapimage.CacheOption = System.Windows.Media.Imaging.BitmapCacheOption.OnLoad;
                bitmapimage.EndInit();
                return bitmapimage;
            }
        }
    }
}
