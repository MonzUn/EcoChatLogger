using Eco.Core.Plugins;
using System;
using System.ComponentModel;
using System.IO;

namespace Eco.Plugins.ChatLogger
{
    public sealed class ChatLogConfig
    {
        public static class DefaultValues
        {
            public static string ChatLogPath = Directory.GetCurrentDirectory() + "\\Mods\\ChatLogger\\Logs\\";
        }

        public event EventHandler OnConfigChanged;

        public static readonly ChatLogConfig Instance = new ChatLogConfig();
        public static ChatLogConfigData Data { get { return Instance._config.Config; } }
        public PluginConfig<ChatLogConfigData> PluginConfig { get { return Instance._config; } }

        private readonly PluginConfig<ChatLogConfigData> _config = new PluginConfig<ChatLogConfigData>("ChatLog");

        // Explicit static constructor to tell C# compiler not to mark type as beforefieldinit
        static ChatLogConfig()
        {
        }

        private ChatLogConfig()
        {
        }

        public void HandleConfigChanged()
        {
            // Reset to default if the user inputs bad data
            if (string.IsNullOrWhiteSpace(_config.Config.ChatlogPath) || !Path.IsPathRooted(_config.Config.ChatlogPath) || Path.HasExtension(_config.Config.ChatlogPath))
            {
                _config.Config.ChatlogPath = DefaultValues.ChatLogPath;
            }
            else if (!_config.Config.ChatlogPath.EndsWith("\\"))
            {
                _config.Config.ChatlogPath += "\\";
            }

            OnConfigChanged?.Invoke(this, EventArgs.Empty);
        }

        public class ChatLogConfigData
        {
            [Description("Enable/Disable chat logging. This setting can be changed while the server is running."), Category("Misc")]
            public bool Enabled { get; set; } = false;

            [Description("When to notify players about what is being logged. This setting can be changed while the server is running."), Category("Misc")]
            public NotificationOption NotifyUsers { get; set; } = NotificationOption.FirstLogin;

            [Description("Enable/Disable logging of direct (Player to Player) messages. This setting can be changed while the server is running."), Category("Misc")]
            public bool LogDirectMessages { get; set; } = false;

            [Description("The directory path where the chat logs will be stored. This setting can be changed while the server is running, but the existing logs will not transfer."), Category("Misc")]
            public string ChatlogPath { get; set; } = DefaultValues.ChatLogPath;
        }

        public enum NotificationOption
        {
            AllLogin,
            FirstLogin,
            Never
        }
    }
}
