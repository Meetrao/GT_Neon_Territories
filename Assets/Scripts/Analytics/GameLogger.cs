using System;
using System.Collections.Generic;
using System.IO;

namespace NeonTerritories.Analytics
{
    public class GameLogEntry
    {
        public int Round { get; set; }
        public string EventType { get; set; }
        public string Description { get; set; }
        public string Timestamp { get; set; }

        public GameLogEntry(int round, string eventType, string description)
        {
            Round = round;
            EventType = eventType;
            Description = description;
            Timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        }

        public override string ToString()
        {
            return $"[{Timestamp}] [Round {Round}] [{EventType}] {Description}";
        }
    }

    public class GameLogger
    {
        public List<GameLogEntry> Entries { get; private set; }

        public GameLogger()
        {
            Entries = new List<GameLogEntry>();
        }

        public void Log(int round, string eventType, string description)
        {
            var entry = new GameLogEntry(round, eventType, description);
            Entries.Add(entry);
            
            #if UNITY_EDITOR || UNITY_STANDALONE
            UnityEngine.Debug.Log(entry.ToString());
            #else
            Console.WriteLine(entry.ToString());
            #endif
        }

        /// <summary>
        /// Saves action logs to a local file for post-match analysis.
        /// </summary>
        public void ExportLogsToFile(string path)
        {
            try
            {
                using (StreamWriter writer = new StreamWriter(path))
                {
                    writer.WriteLine("=== NEON TERRITORIES SESSION LOGS ===");
                    foreach (var entry in Entries)
                    {
                        writer.WriteLine(entry.ToString());
                    }
                }
            }
            catch (Exception ex)
            {
                #if UNITY_EDITOR || UNITY_STANDALONE
                UnityEngine.Debug.LogError($"Failed to export logs: {ex.Message}");
                #else
                Console.WriteLine($"Failed to export logs: {ex.Message}");
                #endif
            }
        }
    }
}
