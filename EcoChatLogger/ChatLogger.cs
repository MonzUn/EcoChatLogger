using Eco.Core.Plugins.Interfaces;
using Eco.Core.Utils;
using Eco.Gameplay.GameActions;
using Eco.Plugins.ChatLoger.Utilities;
using Eco.Plugins.ChatLogger.Utilities;
using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;

namespace Eco.Plugins.ChatLoger
{
    public class ChatLogger : IModKitPlugin, IInitializablePlugin, IShutdownablePlugin, IGameActionAware
    {
        private const int CHATLOG_FLUSH_TIMER_INTERAVAL_MS = 60000; // 1 minute interval
        public static string BasePath { get { return Directory.GetCurrentDirectory() + "\\Mods\\ChatLogger\\"; } }

        // Eco tag matching regex: Match all characters that are used to create HTML style tags
        private static readonly Regex HTMLTagRegex = new Regex("<[^>]*>");

        private string Status = "Uninitialized";
        private StreamWriter Writer = null;
        private Timer FlushTimer = null;

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
            StartLogging(BasePath + "Logs\\", "Log.txt");
            ActionUtil.AddListener(this);

            FlushTimer = new Timer(innerArgs =>
            {
                Flush();
            }, null, 0, CHATLOG_FLUSH_TIMER_INTERAVAL_MS);

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
                    Write($"{StripTags(chatSent.Citizen.Name) + ": " + StripTags(chatSent.Message)}");
                    break;

                default:
                    break;
            }
        }

        public Result ShouldOverrideAuth(GameAction action)
        {
            return new Result(ResultType.None);
        }

        private void StartLogging(string path, string fileName)
        {
            if (Writer != null)
            {
                StopLogging();
            }

            try
            {
                SystemUtil.EnsurePathExists(path);
                Writer = new StreamWriter(path + fileName, append: true);
            }
            catch (Exception e)
            {
                Logger.Error("Error occurred while attempting to start the file writer. Error message: " + e);
            }
        }

        private void StopLogging()
        {
            if (Writer == null) return;

            SystemUtil.StopAndDestroyTimer(ref FlushTimer);
            try
            {
                Writer.Flush();
                Writer.Close();
            }
            catch (Exception e)
            {
                Logger.Error("Error occurred while attempting to close the file writer. Error message: " + e);
            }
            Writer = null;
        }

        private void Write(string message)
        {
            Writer.WriteLine(message);
        }

        private void Flush()
        {
            try
            {
                Writer.Flush();
            }
            catch (Exception e)
            {
                Logger.Error("Error occurred while attempting to write a chatlog to file. Error message: " + e);
            }
        }

        private string StripTags(string toStrip)
        {
            if (toStrip == null) return null;
            return HTMLTagRegex.Replace(toStrip, string.Empty);
        }
    }
}
