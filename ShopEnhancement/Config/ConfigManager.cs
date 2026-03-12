using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Godot;
using ShopEnhancement.Patches;

namespace ShopEnhancement.Config;

public static class ConfigManager
{
    private const string ConfigDirName = "ShopEnhancement";
    private const string PresetsDirName = "Presets";
    private const string ConfigFileName = "config.json";

    private static string UserDataDir => "user://";
    private static string ModConfigDir => System.IO.Path.Combine(ProjectSettings.GlobalizePath(UserDataDir), ConfigDirName);
    private static string PresetsDir => System.IO.Path.Combine(ModConfigDir, PresetsDirName);
    private static string ConfigPath => System.IO.Path.Combine(ModConfigDir, ConfigFileName);

    public static void EnsureDirectories()
    {
        if (!Directory.Exists(ModConfigDir))
            Directory.CreateDirectory(ModConfigDir);
        if (!Directory.Exists(PresetsDir))
            Directory.CreateDirectory(PresetsDir);
    }

    public static void Load()
    {
        EnsureDirectories();
        if (!File.Exists(ConfigPath))
        {
            Save(new ConfigData()); // Save default
            return;
        }

        try
        {
            string json = File.ReadAllText(ConfigPath);
            ConfigData? data = JsonSerializer.Deserialize<ConfigData>(json);
            if (data != null)
            {
                ApplyConfig(data);
            }
        }
        catch (Exception ex)
        {
            MainFile.Logger.Error($"Failed to load config: {ex}");
        }
    }

    public static void Save(ConfigData data)
    {
        EnsureDirectories();
        try
        {
            ApplyConfig(data);
            WriteConfigFile(data);

            if (SaveUnlockPatches.TryApplyUnlockAll())
            {
                data.EnableUnlockAll = false;
                ApplyConfig(data);
                WriteConfigFile(data);
            }
        }
        catch (Exception ex)
        {
            MainFile.Logger.Error($"Failed to save config: {ex}");
        }
    }

    private static void WriteConfigFile(ConfigData data)
    {
        string json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(ConfigPath, json);
    }

    public static List<string> GetPresets()
    {
        EnsureDirectories();
        List<string> presets = new();
        try
        {
            foreach (string file in Directory.GetFiles(PresetsDir, "*.json"))
            {
                presets.Add(Path.GetFileNameWithoutExtension(file));
            }
        }
        catch (Exception ex)
        {
            MainFile.Logger.Error($"Failed to list presets: {ex}");
        }
        return presets;
    }

    public static void SavePreset(string name, ConfigData data)
    {
        EnsureDirectories();
        try
        {
            string path = Path.Combine(PresetsDir, $"{name}.json");
            string json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(path, json);
        }
        catch (Exception ex)
        {
            MainFile.Logger.Error($"Failed to save preset '{name}': {ex}");
        }
    }

    public static void LoadPreset(string name)
    {
        EnsureDirectories();
        try
        {
            string path = Path.Combine(PresetsDir, $"{name}.json");
            if (!File.Exists(path)) return;

            string json = File.ReadAllText(path);
            ConfigData? data = JsonSerializer.Deserialize<ConfigData>(json);
            if (data != null)
            {
                Save(data); // Save as current config
            }
        }
        catch (Exception ex)
        {
            MainFile.Logger.Error($"Failed to load preset '{name}': {ex}");
        }
    }

    public static ConfigData GetCurrentConfig()
    {
        return new ConfigData
        {
            RemoveBaseCost = ShopEnhancementConfig.RemoveBaseCost,
            RemoveStepCost = ShopEnhancementConfig.RemoveStepCost,
            UseLinearCost = ShopEnhancementConfig.UseLinearCost,
            RemoveLimitPerShop = ShopEnhancementConfig.RemoveLimitPerShop,
            RefreshCost = ShopEnhancementConfig.RefreshCost,
            RefreshLimitPerShop = ShopEnhancementConfig.RefreshLimitPerShop,
            EnableNoPurchaseReward = ShopEnhancementConfig.EnableNoPurchaseReward,
            NoPurchaseRewardGold = ShopEnhancementConfig.NoPurchaseRewardGold,
            EnableSkipCardRewardGold = ShopEnhancementConfig.EnableSkipCardRewardGold,
            SkipCardRewardGoldAmount = ShopEnhancementConfig.SkipCardRewardGoldAmount,
            EnableCrossClassCards = ShopEnhancementConfig.EnableCrossClassCards,
            CrossClassCardChance = ShopEnhancementConfig.CrossClassCardChance,
            EnableUnlockAll = ShopEnhancementConfig.EnableUnlockAll,
            EnableSellMode = ShopEnhancementConfig.EnableSellMode,
            SellRelicPriceRatio = ShopEnhancementConfig.SellRelicPriceRatio,
            SellPotionPriceRatio = ShopEnhancementConfig.SellPotionPriceRatio,
            SellRelicMinGold = ShopEnhancementConfig.SellRelicMinGold,
            SellPotionMinGold = ShopEnhancementConfig.SellPotionMinGold,
            RequireSellDoubleClick = ShopEnhancementConfig.RequireSellDoubleClick,
            SellConfirmWindowMs = ShopEnhancementConfig.SellConfirmWindowMs,
            SellAncientRelicBasePrice = ShopEnhancementConfig.SellAncientRelicBasePrice,
            SellStarterRelicBasePrice = ShopEnhancementConfig.SellStarterRelicBasePrice,
            SellEventRelicBasePrice = ShopEnhancementConfig.SellEventRelicBasePrice,
            EnableGiftMode = ShopEnhancementConfig.EnableGiftMode,
            EnableRemovalEnchantRandom = ShopEnhancementConfig.EnableRemovalEnchantRandom,
            EnableEnchantService = ShopEnhancementConfig.EnableEnchantService,
            EnchantStartShopVisit = ShopEnhancementConfig.EnchantStartShopVisit,
            EnchantReplaceChance = ShopEnhancementConfig.EnchantReplaceChance,
            EnchantCost = ShopEnhancementConfig.EnchantCost,
            EnchantAmountRange = ShopEnhancementConfig.EnchantAmountRange,
            EnchantCardCountRange = ShopEnhancementConfig.EnchantCardCountRange,
            EnableRandomTeammateGiftService = ShopEnhancementConfig.EnableRandomTeammateGiftService,
            GiftServiceCardCountRange = ShopEnhancementConfig.GiftServiceCardCountRange,
            GiftServiceBaseCost = ShopEnhancementConfig.GiftServiceBaseCost,
            GiftServiceStepCost = ShopEnhancementConfig.GiftServiceStepCost
        };
    }

    public static void ApplyConfig(ConfigData data)
    {
        ShopEnhancementConfig.RemoveBaseCost = data.RemoveBaseCost;
        ShopEnhancementConfig.RemoveStepCost = data.RemoveStepCost;
        ShopEnhancementConfig.UseLinearCost = data.UseLinearCost;
        ShopEnhancementConfig.RemoveLimitPerShop = data.RemoveLimitPerShop;
        ShopEnhancementConfig.RefreshCost = data.RefreshCost;
        ShopEnhancementConfig.RefreshLimitPerShop = data.RefreshLimitPerShop;
        ShopEnhancementConfig.EnableNoPurchaseReward = data.EnableNoPurchaseReward;
        ShopEnhancementConfig.NoPurchaseRewardGold = data.NoPurchaseRewardGold;
        ShopEnhancementConfig.EnableSkipCardRewardGold = data.EnableSkipCardRewardGold;
        ShopEnhancementConfig.SkipCardRewardGoldAmount = data.SkipCardRewardGoldAmount;
        ShopEnhancementConfig.EnableCrossClassCards = data.EnableCrossClassCards;
        ShopEnhancementConfig.CrossClassCardChance = data.CrossClassCardChance;
        ShopEnhancementConfig.EnableUnlockAll = data.EnableUnlockAll;
        ShopEnhancementConfig.EnableSellMode = data.EnableSellMode;
        ShopEnhancementConfig.SellRelicPriceRatio = data.SellRelicPriceRatio;
        ShopEnhancementConfig.SellPotionPriceRatio = data.SellPotionPriceRatio;
        ShopEnhancementConfig.SellRelicMinGold = data.SellRelicMinGold;
        ShopEnhancementConfig.SellPotionMinGold = data.SellPotionMinGold;
        ShopEnhancementConfig.RequireSellDoubleClick = data.RequireSellDoubleClick;
        ShopEnhancementConfig.SellConfirmWindowMs = data.SellConfirmWindowMs;
        ShopEnhancementConfig.SellAncientRelicBasePrice = data.SellAncientRelicBasePrice;
        ShopEnhancementConfig.SellStarterRelicBasePrice = data.SellStarterRelicBasePrice;
        ShopEnhancementConfig.SellEventRelicBasePrice = data.SellEventRelicBasePrice;
        ShopEnhancementConfig.EnableGiftMode = data.EnableGiftMode;
        ShopEnhancementConfig.EnableRemovalEnchantRandom = data.EnableRemovalEnchantRandom;
        ShopEnhancementConfig.EnableEnchantService = data.EnableEnchantService;
        ShopEnhancementConfig.EnchantStartShopVisit = data.EnchantStartShopVisit;
        ShopEnhancementConfig.EnchantReplaceChance = data.EnchantReplaceChance;
        ShopEnhancementConfig.EnchantCost = data.EnchantCost;
        ShopEnhancementConfig.EnchantAmountRange = NormalizeRange(data.EnchantAmountRange, 1, 999);
        ShopEnhancementConfig.EnchantCardCountRange = NormalizeRange(data.EnchantCardCountRange, 1, 20);
        ShopEnhancementConfig.EnableRandomTeammateGiftService = data.EnableRandomTeammateGiftService;
        ShopEnhancementConfig.GiftServiceCardCountRange = NormalizeRange(data.GiftServiceCardCountRange, 1, 20);
        ShopEnhancementConfig.GiftServiceBaseCost = Math.Max(0, data.GiftServiceBaseCost);
        ShopEnhancementConfig.GiftServiceStepCost = Math.Max(0, data.GiftServiceStepCost);
    }

    private static Vector2I NormalizeRange(Vector2I range, int minClamp, int maxClamp)
    {
        int min = Math.Clamp(Math.Min(range.X, range.Y), minClamp, maxClamp);
        int max = Math.Clamp(Math.Max(range.X, range.Y), minClamp, maxClamp);
        return new Vector2I(min, Math.Max(min, max));
    }
}
