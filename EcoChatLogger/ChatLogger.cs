using Eco.Core.Plugins.Interfaces;
using Eco.Core.Utils;
using Eco.Gameplay.GameActions;
using Eco.Plugins.ChatLogger;
using Eco.Shared.Utils;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace Eco.Plugins.ChatLoger
{
    public class ChatLogger : IModKitPlugin, IInitializablePlugin, IShutdownablePlugin, IGameActionAware
    {
        public static string BasePath { get { return Directory.GetCurrentDirectory() + "\\Mods\\ChatLogger\\"; } }

        // Eco tag matching regex: Match all characters that are used to create HTML style tags
        private static readonly Regex HTMLTagRegex = new Regex("<[^>]*>");
        private const int CHATLOG_FLUSH_TIMER_INTERAVAL_MS = 60000; // 1 minute interval

        private string Status = "Uninitialized";
        private Dictionary<string, ChatLogWriter> ChatChannelWriters = new Dictionary<string, ChatLogWriter>();
        private int CurrentDay = -1;

        public override string ToString()
        {
            return "ChatLogger";
        }

        public string GetStatus()
        {
            return Status;
        }

        public void Initialize(TimedTask timer)
        {
            ActionUtil.AddListener(this);
            CurrentDay = (int)Simulation.Time.WorldTime.Day;
            Status = "Running";
        }

        public void Shutdown()
        {
            ClearActiveLogs();
            Status = "Shutdown";
        }

        public void ActionPerformed(GameAction action)
        {
            switch (action)
            {
                case ChatSent chatSent:
                    string logName = string.Empty;
                    if(chatSent.Tag.StartsWith('#'))
                    {
                        logName = chatSent.Tag.Substring(1);
                    }

                    if (logName != string.Empty)
                    {
                        double seconds = Simulation.Time.WorldTime.Seconds;
                        string time = $"{((int)TimeUtil.SecondsToHours(seconds) % 24).ToString("00") }:{((int)(TimeUtil.SecondsToMinutes(seconds) % 60)).ToString("00")}";
                        LogMessage(logName, $"[{time}] {StripTags(chatSent.Citizen.Name) + ": " + StripTags(chatSent.Message)}");
                    }
                    break;

                default:
                    break;
            }
        }

        public Result ShouldOverrideAuth(GameAction action)
        {
            return new Result(ResultType.None);
        }

        private void ClearActiveLogs()
        {
            foreach (ChatLogWriter writer in ChatChannelWriters.Values)
            {
                writer.Shutdown();
            }
            ChatChannelWriters.Clear();
        }

        private void LogMessage(string logName, string message)
        {
            // Split log files into one per day
            int day = (int)Simulation.Time.WorldTime.Day;
            if (CurrentDay < day)
            {
                CurrentDay = day;
                ClearActiveLogs();
            }

            ChatLogWriter writer = null;
            ChatChannelWriters.TryGetValue(logName, out writer);
            if(writer == null)
            {
                writer = new ChatLogWriter(BasePath + "//Logs//" + logName + "//" + " Day " + CurrentDay + ".txt", 0, CHATLOG_FLUSH_TIMER_INTERAVAL_MS);
                writer.Initialize();
                ChatChannelWriters.Add(logName, writer);
            }
            writer.Write(message);
        }

        private string StripTags(string toStrip)
        {
            if (toStrip == null) return null;
            return HTMLTagRegex.Replace(toStrip, string.Empty);
        }
    }
}
