using Eco.Shared.Localization;
using Eco.Shared.Utils;

namespace Eco.Plugins.ChatLoger.Utilities
{
    public static class Logger
    {
        public static void Warning(string message)
        {
            Log.Write(new LocString("[ChatLogger] WARNING: " + message));
        }

        public static void Info(string message)
        {
            Log.Write(new LocString("[ChatLogger] " + message));
        }

        public static void Error(string message)
        {
            Log.Write(new LocString("[ChatLogger] ERROR: " + message));
        }
    }
}
