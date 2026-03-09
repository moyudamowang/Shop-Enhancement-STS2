using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text.Json;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Localization;
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

        ShopEnhancementNetwork.Initialize();
    }

    public static void OnLocaleChanged()
    {
        LoadLocalization();
    }

    private static void LoadLocalization()
    {
        string lang = LocManager.Instance.Language;
        // Check if we have localization for this language, otherwise fallback to en
        // Note: The path depends on where the files are located in the exported project.
        // Assuming they are under ShopEnhancement/localization/
        string path = $"res://ShopEnhancement/localization/{lang}.json";
        if (!Godot.FileAccess.FileExists(path))
        {
            // Try explicit fallback to en if current lang is not found
            if (lang != "en")
            {
                path = "res://ShopEnhancement/localization/en.json";
            }
        }
        
        if (!Godot.FileAccess.FileExists(path))
        {
             // Only log error if we really can't find anything
             // It might be fine if we are in editor or running tests differently
             Logger.Info($"Could not find localization file: {path}");
             return;
        }

        using var file = Godot.FileAccess.Open(path, Godot.FileAccess.ModeFlags.Read);
        string jsonText = file.GetAsText();
        
        try 
        {
            // LocManager uses a specific serializer context, but standard Deserialize should work for Dictionary<string, string>
            var dict = JsonSerializer.Deserialize<Dictionary<string, string>>(jsonText);
            if (dict == null) return;
            
            // We'll use a custom table name "shop_enhancement"
            var tableName = "shop_enhancement";
            var table = new LocTable(tableName, dict);
            
            // Inject into LocManager._tables via reflection
            var tablesField = typeof(LocManager).GetField("_tables", BindingFlags.NonPublic | BindingFlags.Instance);
            if (tablesField != null)
            {
                var tables = tablesField.GetValue(LocManager.Instance) as Dictionary<string, LocTable>;
                if (tables != null)
                {
                    tables[tableName] = table;
                    Logger.Info($"Loaded localization table '{tableName}' for language '{lang}' from {path}");
                }
            }
            else
            {
                Logger.Error("Could not access LocManager._tables");
            }
        }
        catch (Exception ex)
        {
            Logger.Error($"Failed to load localization: {ex}");
        }
    }
}
