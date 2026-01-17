using System;

namespace Watcher_PC
{
    /// <summary>
    /// 負責文字的清洗與模糊比對邏輯
    /// </summary>
    public static class TextProcessor
    {
        /// <summary>
        /// 優化文字：移除時間戳記、修正全形、移除贅餘空格
        /// </summary>
        public static string CleanText(string input)
        {
            if (string.IsNullOrEmpty(input)) return "";

            // 1. 轉半形方便過濾
            char[] c = input.ToCharArray();
            for (int i = 0; i < c.Length; i++)
            {
                if (c[i] == 12288) c[i] = (char)32; // 全形空格
                else if (c[i] > 65280 && c[i] < 65375) c[i] = (char)(c[i] - 65248);
            }
            string res = new string(c);

            // 2. 統一常見符號與錯字字典 (FF14 特有 OCR 修正)
            res = res.Replace("〔", "[").Replace("〕", "]")
                     .Replace("【", "[").Replace("】", "]")
                     .Replace("•", ":").Replace("·", ":")
                     .Replace("訁", "詐").Replace("進彳", "進行").Replace("壬何乍", "任何操作");

            // 3. 移除時間戳記 (更強大的匹配，包含誤判的尾字如 8:2叼)
            // 匹配模式： [? 數字 : 數字+任何一個字 ? ]?
            res = System.Text.RegularExpressions.Regex.Replace(res, @"\[?\s*\d{1,2}\s*[:：]\s*\d[\d\w]\s*\]?", "");

            // 4. 移除所有空格
            res = res.Replace(" ", "");

            return res.Trim();
        }

        /// <summary>
        /// 模糊比對演算法
        /// </summary>
        public static bool FuzzyMatch(string input, string target, double threshold)
        {
            if (input == null || target == null) return false;

            if (input.Contains(target)) return true;

            // 計算相似度 (1 - 距離/長度)
            int distance = LevenshteinDistance(input, target);
            int maxLength = Math.Max(input.Length, target.Length);
            double similarity = 1.0 - (double)distance / maxLength;

            // 特殊處理：只要目標關鍵字大部分出現在輸入中即可
            if (similarity >= threshold) return true;

            // 嘗試關鍵字子字串匹配 (例如 "任務開始" 只要包含 "任務" 和 "始" 也很可能是)
            if (target.Length >= 4)
            {
                int matchCount = 0;
                foreach (char c in target)
                {
                    if (input.Contains(c)) matchCount++;
                }
                if ((double)matchCount / target.Length >= 0.75) return true;
            }

            return false;
        }

        /// <summary>
        /// 編輯距離演算法
        /// </summary>
        private static int LevenshteinDistance(string s, string t)
        {
            int n = s.Length;
            int m = t.Length;
            int[,] d = new int[n + 1, m + 1];
            if (n == 0) return m;
            if (m == 0) return n;
            for (int i = 0; i <= n; d[i, 0] = i++) ;
            for (int j = 0; j <= m; d[0, j] = j++) ;
            for (int i = 1; i <= n; i++)
            {
                for (int j = 1; j <= m; j++)
                {
                    int cost = (t[j - 1] == s[i - 1]) ? 0 : 1;
                    d[i, j] = Math.Min(Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1), d[i - 1, j - 1] + cost);
                }
            }
            return d[n, m];
        }

        public static string ExtractDutyName(string rawLine)
        {
            // 嘗試從該行提取副本名稱 (支援 「」 或 “” 格式)
            var match = System.Text.RegularExpressions.Regex.Match(rawLine, @"[「“](.+?)[」”]");
            if (match.Success)
            {
                return match.Groups[1].Value;
            }
            return string.Empty;
        }
    }
}
