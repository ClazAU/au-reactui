using BepInEx.Logging;

namespace ReactUI.Plugin;

/// <summary>stands in for the plugin in embedded builds so the library keeps its log sink</summary>
public static class ReactUIPlugin
{
    public const string Id = "com.reactui.core";

    internal static ManualLogSource Logger = BepInEx.Logging.Logger.CreateLogSource("ReactUI");
}
