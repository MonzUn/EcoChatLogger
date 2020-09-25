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
        private const int CHATLOG_FLUSH_TIMER_INTERAVAL_MS = 60000; // 1 minute interval
        public static string BasePath { get { return Directory.GetCurrentDirectory() + "\\Mods\\ChatLogger\\"; } }

        // Eco tag matching regex: Match all characters that are used to create HTML style tags
        private static readonly Regex HTMLTagRegex = new Regex("<[^>]*>");

        private string Status = "Uninitialized";

        private ChatLogWriter Writer = null;

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
            Status = "Running";

            Writer = new ChatLogWriter(BasePath + "ChatLog.txt", 0, CHATLOG_FLUSH_TIMER_INTERAVAL_MS);
            Writer.Initialize();
        }

        public void Shutdown()
        {
            Writer.Shutdown();
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

        private void LogMessage(string message)
        {
            Writer.Write(message);
        }

        private string StripTags(string toStrip)
        {
            if (toStrip == null) return null;
            return HTMLTagRegex.Replace(toStrip, string.Empty);
        }
    }
}
