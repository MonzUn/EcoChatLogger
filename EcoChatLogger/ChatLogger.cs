using Eco.Core.Plugins;
using Eco.Core.Plugins.Interfaces;
using Eco.Core.Utils;
using Eco.Gameplay.GameActions;
using Eco.Gameplay.Players;
using Eco.Plugins.ChatLogger;
using Eco.Shared.Localization;
using Eco.Shared.Utils;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

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

        private enum LoginEventType
        {
            Joined,
            Login,
            Logout
        }

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

            UserManager.OnNewUserJoined.Add(u =>
            {
                if (!ChatLogConfig.Data.Enabled) return;
                LogLoginEvent(LoginEventType.Joined, u, $"--> {u.Name} joined the server.");
            });

            UserManager.OnUserLoggedOut.Add(u =>
            {
                if (!ChatLogConfig.Data.Enabled) return;
                LogLoginEvent(LoginEventType.Logout, u, $"<-- {u.Name} logged out.");
            });

            UserManager.OnUserLoggedIn.Add(u =>
            {
                if (!ChatLogConfig.Data.Enabled) return;
                LogLoginEvent(LoginEventType.Login, u, $"--> {u.Name} logged in.");
            });

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
                        LogMessage(logName, $"{StripTags(chatSent.Citizen.Name) + ": " + StripTags(chatSent.Message)}");
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

        private void LogLoginEvent(LoginEventType eventType, User user, string toLog)
        {
            ChatLogConfig.ChatLogConfigData config = ChatLogConfig.Data;
            if(eventType == LoginEventType.Joined && config.NotifyUsers <= ChatLogConfig.NotificationOption.FirstLogin
                || (eventType == LoginEventType.Login && config.NotifyUsers <= ChatLogConfig.NotificationOption.AllLogin))
            {
                _ = NotifyLogSettings(user);
            }

            LogMessage(LOGIN_LOG_DIR, toLog);
        }

        const int NOTIFICATION_DELAY_MS = 10000;
        private async Task NotifyLogSettings(User user)
        {
            await Task.Delay(NOTIFICATION_DELAY_MS);
            string loggingNotification = "Logging is enabled on this server.\nThe following information is being logged:\n- Login/Logout\n- Public Chat" + (ChatLogConfig.Data.LogDirectMessages ? "\n- Private Chat" : "");
            Gameplay.Systems.Chat.ChatManager.ServerMessageToPlayer(Localizer.DoStr(loggingNotification), user, forceTemporary: true);
        }

        private void LogMessage(string logName, string toLog)
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
            writer.Write($"[{GetTimeStamp()}] {toLog}");
        }

        private string GetTimeStamp()
        {
            double seconds = Simulation.Time.WorldTime.Seconds;
            return $"{((int)TimeUtil.SecondsToHours(seconds) % 24).ToString("00") }" +
                $":{((int)(TimeUtil.SecondsToMinutes(seconds) % 60)).ToString("00")}" +
                $":{((int)seconds % 60).ToString("00")}";
        }

        private string StripTags(string toStrip)
        {
            if (toStrip == null) return null;
            return HTMLTagRegex.Replace(toStrip, string.Empty);
        }
    }
}
