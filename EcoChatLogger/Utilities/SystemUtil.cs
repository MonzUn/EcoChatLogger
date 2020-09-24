using Eco.Plugins.ChatLoger.Utilities;
using System;
using System.IO;
using System.Threading;

namespace Eco.Plugins.ChatLogger.Utilities
{
    class SystemUtil
    {
        public static void StopAndDestroyTimer(ref Timer timer)
        {
            if (timer != null)
            {
                timer.Change(Timeout.Infinite, Timeout.Infinite);
                timer = null;
            }
        }

        public static void EnsurePathExists(string path)
        {
            string directoryPath = Path.GetDirectoryName(path);
            try
            {
                Directory.CreateDirectory(directoryPath);
            }
            catch (Exception e)
            {
                Logger.Error("Failed to create directory at path \"" + path + "\". Error message: " + e);
            }
        }

        internal static void EnsurePathExists(object chatlogPath)
        {
            throw new NotImplementedException();
        }
    }
}
