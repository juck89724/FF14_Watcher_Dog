using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace Watcher_PC
{
    // 定義任務的基本資料 (不含日期狀態)
    public class TaskDefinition
    {
        public string Name { get; set; } = "";
        public List<string> Keywords { get; set; } = new List<string>();
    }

    // UI 顯示用的 ViewModel (包含狀態)
    public class DailyTaskItem
    {
        public string Name { get; set; } = "";
        public bool IsCompleted { get; set; }
        public DateTime? LastCompletedAt { get; set; } // 僅供參考，主要狀態在 History
        public List<string> Keywords { get; set; } = new List<string>();
    }

    public class DailyTaskManager
    {
        private const string SAVE_FILE = "daily_tasks_v2.json";

        // 任務定義 (不變的部分)
        private readonly List<TaskDefinition> _definitions;

        // 歷史紀錄: DateString ("yyyy-MM-dd") -> { TaskName -> IsCompleted }
        private Dictionary<string, Dictionary<string, bool>> _history = new Dictionary<string, Dictionary<string, bool>>();

        public DailyTaskManager()
        {
            // 初始化任務定義
            _definitions = new List<TaskDefinition>
            {
                new TaskDefinition { Name = "專家", Keywords = new List<string> { "專家", "Expert" } },
                new TaskDefinition { Name = "拾級迷宮", Keywords = new List<string> { "拾級迷宮", "Level 50/60/70/80", "50/60/70/80" } },
                new TaskDefinition { Name = "練級", Keywords = new List<string> { "練級", "Leveling" } },
                new TaskDefinition { Name = "討伐殲滅戰", Keywords = new List<string> { "討伐殲滅戰", "Trials" } },
                new TaskDefinition { Name = "主線任務", Keywords = new List<string> { "主線任務", "Main Scenario", "神兵要塞帝國南方堡", "最終決戰天幕魔導城", "究極武器破壞作戰" } },
                new TaskDefinition { Name = "團隊任務", Keywords = new List<string> { "團隊任務", "Alliance Raids" } },
                new TaskDefinition { Name = "大型任務", Keywords = new List<string> { "大型任務", "Normal Raids" } },
                new TaskDefinition { Name = "紛爭前線", Keywords = new List<string> { "紛爭前線", "Frontline" } }
            };

            Load();
        }

        // 取得特定日期的任務清單 (這個日期通常是 "FF14 的某一班車")
        public List<DailyTaskItem> GetTasksForDate(DateTime date)
        {
            string dateKey = date.ToString("yyyy-MM-dd");
            var result = new List<DailyTaskItem>();

            // 確保該日期有紀錄
            if (!_history.ContainsKey(dateKey))
            {
                _history[dateKey] = new Dictionary<string, bool>();
            }
            var dayRecord = _history[dateKey];

            foreach (var def in _definitions)
            {
                bool isDone = dayRecord.ContainsKey(def.Name) && dayRecord[def.Name];
                result.Add(new DailyTaskItem
                {
                    Name = def.Name,
                    Keywords = def.Keywords,
                    IsCompleted = isDone,
                    LastCompletedAt = isDone ? date : null // 這裡簡化，顯示該日期即可
                });
            }

            return result;
        }

        // 當 OCR 偵測到文字時，自動標記 "今天" (邏輯上的今天)
        public string? TryCompleteTask(string text)
        {
            // 取得 FF14 邏輯當日 (若清晨 3 點打，算前一天的任務)
            // FF14 重置時間是日本半夜 24:00 = 台灣 23:00
            // 這裡簡單抓: 若現在時間 < 23:00，算今天; 若 >= 23:00，算明天
            // 或者簡單一點：就用本地日期 (User Request: Local 的方式處理) -> 使用 DateTime.Today

            DateTime logicDate = DateTime.Today;
            // 如果要更精確配合 FF14 重置時間 (晚上11點換日):
            if (DateTime.Now.Hour >= 23) logicDate = logicDate.AddDays(1);

            return TryCompleteTaskForDate(text, logicDate);
        }

        public string? TryCompleteTaskForDate(string text, DateTime date)
        {
            string dateKey = date.ToString("yyyy-MM-dd");
            if (!_history.ContainsKey(dateKey)) _history[dateKey] = new Dictionary<string, bool>();

            foreach (var def in _definitions)
            {
                // 如果已經完成，跳過
                if (_history[dateKey].ContainsKey(def.Name) && _history[dateKey][def.Name])
                    continue;

                foreach (var keyword in def.Keywords)
                {
                    if (text.Contains(keyword))
                    {
                        // 標記完成
                        SetTaskStatus(date, def.Name, true);
                        return def.Name;
                    }
                }
            }
            return null;
        }

        // 手動切換狀態
        public void SetTaskStatus(DateTime date, string taskName, bool isCompleted)
        {
            string dateKey = date.ToString("yyyy-MM-dd");
            if (!_history.ContainsKey(dateKey)) _history[dateKey] = new Dictionary<string, bool>();

            _history[dateKey][taskName] = isCompleted;
            Save();
        }

        public void Load()
        {
            try
            {
                string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, SAVE_FILE);
                if (File.Exists(path))
                {
                    string json = File.ReadAllText(path);
                    var data = JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, bool>>>(json);
                    if (data != null)
                    {
                        _history = data;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Load Error: {ex.Message}");
            }
        }

        public void Save()
        {
            try
            {
                string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, SAVE_FILE);
                string json = JsonSerializer.Serialize(_history, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(path, json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Save Error: {ex.Message}");
            }
        }
    }
}
