using Eco.Core.Plugins.Interfaces;
using Eco.Core.Utils;
using Eco.Gameplay.GameActions;
using Eco.Plugins.ChatLogger;
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
        private ChatLogWriter Writer = null;
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
            StartLogging();
            Status = "Running";
        }

        public void Shutdown()
        {
            StopLogging();
            Status = "Shutdown";
        }

        public void ActionPerformed(GameAction action)
        {
            switch (action)
            {
                case ChatSent chatSent:
                    LogMessage($"{StripTags(chatSent.Citizen.Name) + ": " + StripTags(chatSent.Message)}");
                    break;

                default:
                    break;
            }
        }

        public Result ShouldOverrideAuth(GameAction action)
        {
            return new Result(ResultType.None);
        }

        private void StartLogging()
        {
            Writer = new ChatLogWriter(BasePath + "//Logs//Day " + CurrentDay + ".txt", 0, CHATLOG_FLUSH_TIMER_INTERAVAL_MS);
            Writer.Initialize();
        }

        private void StopLogging()
        {
            Writer.Shutdown();
        }

        private void RestartLogging()
        {
            StopLogging();
            StartLogging();
        }

        private void LogMessage(string message)
        {
            // Split log files into one per day
            int day = (int)Simulation.Time.WorldTime.Day;
            if (CurrentDay < day)
            {
                CurrentDay = day;
                RestartLogging();
            }

            Writer.Write(message);
        }

        private string StripTags(string toStrip)
        {
            if (toStrip == null) return null;
            return HTMLTagRegex.Replace(toStrip, string.Empty);
        }
    }
}
