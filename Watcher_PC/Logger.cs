using System;
using System.IO;

namespace Watcher_PC
{
    /// <summary>
    /// 簡易日誌記錄器，用於將錯誤輸出到文字檔以便除錯
    /// </summary>
    public static class Logger
    {
        private static readonly string LogFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "error_log.txt");
        private static readonly object _lock = new object();

        /// <summary>
        /// 記錄一般訊息
        /// </summary>
        public static void Log(string message)
        {
            lock (_lock)
            {
                try
                {
                    File.AppendAllText(LogFilePath, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [INFO] {message}{Environment.NewLine}");
                }
                catch { }
            }
        }

        /// <summary>
        /// 記錄錯誤訊息與例外狀況
        /// </summary>
        public static void LogError(string message, Exception? ex = null)
        {
            lock (_lock)
            {
                try
                {
                    using (StreamWriter writer = new StreamWriter(LogFilePath, true))
                    {
                        writer.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [ERROR] {message}");
                        if (ex != null)
                        {
                            writer.WriteLine($"Exception: {ex.GetType().Name}: {ex.Message}");
                            if (ex.InnerException != null)
                            {
                                writer.WriteLine($"Inner Exception: {ex.InnerException.Message}");
                            }
                            writer.WriteLine($"StackTrace: {ex.StackTrace}");
                        }
                        writer.WriteLine(new string('-', 80));
                    }
                }
                catch
                {
                    // 忽略日誌寫入錯誤，避免循環崩潰
                }
            }
        }
    }
}
