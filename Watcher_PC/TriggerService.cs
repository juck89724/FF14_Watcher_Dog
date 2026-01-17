using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace Watcher_PC
{
    public class TriggerRule
    {
        public string Keyword { get; set; } = "";
        public double Threshold { get; set; } = 0.6;
        public string Type { get; set; } = "Standard"; // "ExtractName", "FixedName", "RegexExtract"
        public string FixedName { get; set; } = "";
        public string LogMessage { get; set; } = "";
        public string Regex { get; set; } = ""; // For RegexExtract
    }

    public class TriggerConfig
    {
        public List<TriggerRule> StartTriggers { get; set; } = new List<TriggerRule>();
        public List<TriggerRule> EndTriggers { get; set; } = new List<TriggerRule>();
    }

    public class TriggerService
    {
        private TriggerConfig _config = new TriggerConfig();

        public bool LoadConfig()
        {
            try
            {
                string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "triggers.json");
                if (File.Exists(path))
                {
                    string json = File.ReadAllText(path);
                    _config = JsonSerializer.Deserialize<TriggerConfig>(json) ?? new TriggerConfig();
                    return true;
                }
                return false;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public TriggerRule? CheckStart(string cleanedLine)
        {
            foreach (var rule in _config.StartTriggers)
            {
                if (TextProcessor.FuzzyMatch(cleanedLine, rule.Keyword, rule.Threshold))
                {
                    return rule;
                }
            }
            return null;
        }

        public TriggerRule? CheckEnd(string cleanedLine)
        {
            foreach (var rule in _config.EndTriggers)
            {
                if (TextProcessor.FuzzyMatch(cleanedLine, rule.Keyword, rule.Threshold))
                {
                    return rule;
                }
            }
            return null;
        }
    }
}
