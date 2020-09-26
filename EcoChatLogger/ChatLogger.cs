using Eco.Core.Plugins;
using Eco.Core.Plugins.Interfaces;
using Eco.Core.Utils;
using Eco.Gameplay.GameActions;
using Eco.Gameplay.Players;
using Eco.Plugins.ChatLogger;
using Eco.Shared.Utils;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Eco.Plugins.ChatLoger
{
    public class ChatLogger : IModKitPlugin, IInitializablePlugin, IShutdownablePlugin, IConfigurablePlugin, IGameActionAware
    {
        public IPluginConfig PluginConfig { get { return ChatLogConfig.Instance.PluginConfig; } }
        public ThreadSafeAction<object, string> ParamChanged { get; set; }

        // Eco tag matching regex: Match all characters that are used to create HTML style tags
        private static readonly Regex HTMLTagRegex = new Regex("<[^>]*>");
        private const int CHATLOG_FLUSH_TIMER_INTERAVAL_MS = 60000; // 1 minute interval
        private const string LOGIN_LOG_DIR = "Login";

        private string Status = "Uninitialized";
        private readonly Dictionary<string, ChatLogWriter> ChatLogWriters = new Dictionary<string, ChatLogWriter>();
        private int CurrentDay = -1;

        public override string ToString()
        {
            return "Chat log";
        }

        public string GetStatus()
        {
            return Status;
        }

        public object GetEditObject()
        {
            return ChatLogConfig.Data;
        }

        public void OnEditObjectChanged(object o, string param)
        {
            ChatLogConfig.Instance.HandleConfigChanged();
        }

        public void Initialize(TimedTask timer)
        {
            ActionUtil.AddListener(this);
            CurrentDay = (int)Simulation.Time.WorldTime.Day;
            ChatLogConfig.Instance.OnConfigChanged += (obj, args) =>
            {
                ClearActiveLogs();
            };

            UserManager.OnNewUserJoined.Add(u => LogMessage(LOGIN_LOG_DIR, $"--> {u.Name} joined the server."));
            UserManager.OnUserLoggedIn.Add(u => LogMessage(LOGIN_LOG_DIR, $"--> {u.Name} logged in."));
            UserManager.OnUserLoggedOut.Add(u => LogMessage(LOGIN_LOG_DIR, $"<-- {u.Name} logged out."));

            Status = "Running";
        }

        public void Shutdown()
        {
            ClearActiveLogs();
            Status = "Shutdown";
        }

        public void ActionPerformed(GameAction action)
        {
            if (!ChatLogConfig.Data.Enabled) return;

            switch (action)
            {
                case ChatSent chatSent:
                    string logName = string.Empty;
                    if (chatSent.Tag.StartsWith('#')) // Channel
                    {
                        logName = "Channel//" + chatSent.Tag.Substring(1); // Remove the #
                    }
                    else if (ChatLogConfig.Data.LogDirectMessages && chatSent.Tag.StartsWith('@')) // DM
                    {
                        string recipientName = chatSent.Tag.Substring(1); // Remove the @
                        string senderName = chatSent.Citizen.Name;

                        // Make sure that the names are always in the same order
                        logName = "DM//" + (senderName.Length < recipientName.Length
                            ? $"{senderName}-{recipientName}"
                            : $"{recipientName}-{senderName}");
                    }
                    else
                    {
                        return;
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
            foreach (ChatLogWriter writer in ChatLogWriters.Values)
            {
                writer.Shutdown();
            }
            ChatLogWriters.Clear();
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
            ChatLogWriters.TryGetValue(logName, out writer);
            if (writer == null)
            {
                writer = new ChatLogWriter(ChatLogConfig.Data.ChatlogPath + logName + "\\" + "Day " + CurrentDay + ".txt", 0, CHATLOG_FLUSH_TIMER_INTERAVAL_MS);
                writer.Initialize();
                ChatLogWriters.Add(logName, writer);
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
