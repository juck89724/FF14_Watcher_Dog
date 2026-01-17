using System.Windows;

namespace Watcher_PC
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // [DLL路徑設定] 嘗試將 DLL 搜尋路徑指向 libs 子目錄，讓根目錄更乾淨
            try
            {
                var baseDir = AppDomain.CurrentDomain.BaseDirectory;
                var libDir = System.IO.Path.Combine(baseDir, "libs");
                if (System.IO.Directory.Exists(libDir))
                {
                    NativeMethods.SetDllDirectory(libDir);
                    // 為了除錯，也可以把這個路徑加到環境變數 PATH (Optional, SetDllDirectory 通常足夠)
                    Environment.SetEnvironmentVariable("PATH", libDir + ";" + Environment.GetEnvironmentVariable("PATH"));
                }
            }
            catch (Exception ex)
            {
                // 若設定失敗，至少記錄下來，但不阻擋程式啟動 (可能還是會崩潰如果找不到DLL)
                System.Diagnostics.Debug.WriteLine($"設定 DLL 目錄失敗: {ex.Message}");
            }

            // 捕捉 UI 執行緒的未處理例外
            this.DispatcherUnhandledException += App_DispatcherUnhandledException;

            // 捕捉非 UI 執行緒的未處理例外 (Task)
            System.Threading.Tasks.TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;

            // 捕捉所有其他未處理例外 (AppDomain)
            System.AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
        }

        private void App_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            Logger.LogError("UI 執行緒發生未處理例外 (Dispatcher)", e.Exception);
            // 視情況決定是否要標記為已處理，若設為 true 則程式不會崩潰，但可能處於不穩定狀態
            // e.Handled = true; 
        }

        private void TaskScheduler_UnobservedTaskException(object? sender, System.Threading.Tasks.UnobservedTaskExceptionEventArgs e)
        {
            Logger.LogError("背景工作發生未處理例外 (Task)", e.Exception);
            e.SetObserved(); // 防止程式崩潰
        }

        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            if (e.ExceptionObject is System.Exception ex)
            {
                Logger.LogError("應用程式發生嚴重錯誤 (AppDomain)", ex);
            }
            else
            {
                Logger.LogError($"應用程式發生嚴重錯誤 (AppDomain): {e.ExceptionObject}");
            }
        }
    }
}
