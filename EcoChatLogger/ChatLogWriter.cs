using Eco.Plugins.ChatLoger.Utilities;
using Eco.Plugins.ChatLogger.Utilities;
using System;
using System.IO;
using System.Threading;

namespace Eco.Plugins.ChatLogger
{
    class ChatLogWriter
    {
        private StreamWriter Writer = null;
        private Timer FlushTimer = null;
        string Path = null;
        int FlushDelayMS = 0;
        int FlushIntervalMS = Timeout.Infinite;

        public ChatLogWriter(string path, int flushDelayMS, int flushIntervalMS) => (Path, FlushDelayMS, FlushIntervalMS) = (path, flushDelayMS, flushIntervalMS);

        public void Initialize()
        {
            try
            {
                SystemUtil.EnsurePathExists(Path);
                Writer = new StreamWriter(Path, append: true);
            }
            catch (Exception e)
            {
                Logger.Error("Error occurred while attempting to start the file writer. Error message: " + e);
            }

            FlushTimer = new Timer(innerArgs =>
            {
                Flush();
            }, null, FlushDelayMS, FlushIntervalMS);
        }

        public void Shutdown()
        {
            SystemUtil.StopAndDestroyTimer(ref FlushTimer);
            try
            {
                Writer?.Flush();
                Writer?.Close();
            }
            catch (Exception e)
            {
                Logger.Error("Error occurred while attempting to close the file writer. Error message: " + e);
            }
            Writer = null;
        }

        public void Write(string message)
        {
            Writer?.WriteLine(message);
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
    }
}
