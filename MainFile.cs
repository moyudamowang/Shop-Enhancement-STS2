using System;
using System.Reflection;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Modding;

namespace ShopEnhancement;

[ModInitializer(nameof(Initialize))]
public partial class MainFile : Node
{
    private const string
        ModId = "ShopEnhancement"; //At the moment, this is used only for the Logger and harmony names.

    public static MegaCrit.Sts2.Core.Logging.Logger Logger { get; } =
        new(ModId, MegaCrit.Sts2.Core.Logging.LogType.Generic);

    public static void Initialize()
    {
    Harmony.DEBUG = true;
    var fileLogType = Type.GetType("HarmonyLib.HarmonyFileLog, 0Harmony") ?? Type.GetType("HarmonyLib.FileLog, 0Harmony");
    if (fileLogType != null)
    {
        var enabledProp = fileLogType.GetProperty("Enabled", BindingFlags.Public | BindingFlags.Static);
        if (enabledProp != null && enabledProp.CanWrite)
        {
            enabledProp.SetValue(null, true);
        }
        var logPathProp = fileLogType.GetProperty("LogPath", BindingFlags.Public | BindingFlags.Static);
        if (logPathProp != null && logPathProp.CanWrite)
        {
            var logPath = System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), "Harmony.log");
            logPathProp.SetValue(null, logPath);
        }
        var logMethod = fileLogType.GetMethod("Log", BindingFlags.Public | BindingFlags.Static, null, new[] { typeof(string) }, null);
        if (logMethod != null)
        {
            logMethod.Invoke(null, new object[] { "Harmony debug enabled" });
        }
    }

        Harmony harmony = new(ModId);

        harmony.PatchAll();
    }
}
