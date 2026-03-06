using Dalamud.Configuration;
using Dalamud.Plugin;
using System;

namespace UkrChatSupport;

[Serializable]
public class Configuration : IPluginConfiguration
{
    public int Version { get; set; } = 0;

    public void Save()
    {
        Plugin.PluginInterface.SavePluginConfig(this);
    }
}
