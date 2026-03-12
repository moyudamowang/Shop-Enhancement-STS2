using System;
using System.Collections.Generic;
using Godot;
using MegaCrit.Sts2.Core.Localization;
using ShopEnhancement.Config;

namespace ShopEnhancement.Ui;

public partial class ConfigPanel : Window
{
    private VBoxContainer _settingsContainer = null!;
    private OptionButton _presetOption = null!;
    private LineEdit _newPresetName = null!;

    public override void _Ready()
    {
        // Window Setup
        Title = "Shop Enhancement Config";
        InitialPosition = Window.WindowInitialPosition.CenterMainWindowScreen;
        Size = new Vector2I(600, 700);
        Exclusive = true;
        Transient = true;
        Unresizable = false;
        Visible = false;

        CloseRequested += HidePanel;

        // Root Container with margin
        MarginContainer rootMargin = new();
        rootMargin.AddThemeConstantOverride("margin_top", 10);
        rootMargin.AddThemeConstantOverride("margin_left", 10);
        rootMargin.AddThemeConstantOverride("margin_bottom", 10);
        rootMargin.AddThemeConstantOverride("margin_right", 10);
        rootMargin.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        AddChild(rootMargin);

        // Main Vertical Layout
        VBoxContainer mainLayout = new();
        mainLayout.AddThemeConstantOverride("separation", 15);
        rootMargin.AddChild(mainLayout);

        // --- Header Section ---
        VBoxContainer headerBox = new();
        headerBox.AddThemeConstantOverride("separation", 5);
        mainLayout.AddChild(headerBox);

        Label title = new() { 
            Text = new LocString("shop_enhancement", "config.title").GetFormattedText(), 
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };
        title.AddThemeFontSizeOverride("font_size", 28);
        title.AddThemeColorOverride("font_color", new Color("efc851")); // Gold color
        headerBox.AddChild(title);

        Label subtitle = new() { 
            Text = new LocString("shop_enhancement", "config.subtitle").GetFormattedText(), 
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };
        subtitle.AddThemeFontSizeOverride("font_size", 16);
        subtitle.AddThemeColorOverride("font_color", new Color(1, 1, 1, 0.7f));
        headerBox.AddChild(subtitle);
        
        mainLayout.AddChild(new HSeparator());

        // --- Presets Section ---
        HBoxContainer presetRow = new();
        presetRow.AddThemeConstantOverride("separation", 10);
        presetRow.Alignment = BoxContainer.AlignmentMode.Center;
        
        Label presetLabel = new() { Text = new LocString("shop_enhancement", "config.preset").GetFormattedText() };
        presetLabel.AddThemeColorOverride("font_color", Colors.LightGray);
        presetRow.AddChild(presetLabel);

        _presetOption = new OptionButton();
        _presetOption.CustomMinimumSize = new Vector2(150, 0);
        _presetOption.ItemSelected += OnPresetSelected;
        presetRow.AddChild(_presetOption);
        
        Button loadPresetBtn = new() { Text = new LocString("shop_enhancement", "config.load").GetFormattedText() };
        loadPresetBtn.Pressed += LoadSelectedPreset;
        presetRow.AddChild(loadPresetBtn);

        // Spacer
        presetRow.AddChild(new Control { CustomMinimumSize = new Vector2(20, 0) });

        _newPresetName = new LineEdit { 
            PlaceholderText = new LocString("shop_enhancement", "config.new_preset_name").GetFormattedText(), 
            CustomMinimumSize = new Vector2(120, 0),
            ExpandToTextLength = true
        };
        presetRow.AddChild(_newPresetName);

        Button savePresetBtn = new() { Text = new LocString("shop_enhancement", "config.save_as").GetFormattedText() };
        savePresetBtn.Pressed += SaveNewPreset;
        presetRow.AddChild(savePresetBtn);

        mainLayout.AddChild(presetRow);
        mainLayout.AddChild(new HSeparator());

        // --- Settings Scroll Area ---
        ScrollContainer scroll = new() { SizeFlagsVertical = Control.SizeFlags.ExpandFill };
        mainLayout.AddChild(scroll);

        _settingsContainer = new VBoxContainer();
        _settingsContainer.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        _settingsContainer.AddThemeConstantOverride("separation", 8);
        scroll.AddChild(_settingsContainer);

        // --- Footer Buttons ---
        mainLayout.AddChild(new HSeparator());

        HBoxContainer buttons = new() { Alignment = BoxContainer.AlignmentMode.End };
        buttons.AddThemeConstantOverride("separation", 20);
        mainLayout.AddChild(buttons);

        Button cancelBtn = new() { Text = new LocString("shop_enhancement", "config.cancel").GetFormattedText(), CustomMinimumSize = new Vector2(100, 40) };
        cancelBtn.Pressed += HidePanel;
        buttons.AddChild(cancelBtn);

        Button saveBtn = new() { Text = new LocString("shop_enhancement", "config.save_close").GetFormattedText(), CustomMinimumSize = new Vector2(140, 40) };
        saveBtn.AddThemeColorOverride("font_color", new Color("efc851")); // Gold color
        saveBtn.Pressed += SaveAndClose;
        buttons.AddChild(saveBtn);

        BuildSettingsUi();
    }

    private void BuildSettingsUi()
    {
        // Remove Cost
        AddHeader(L("config.header.removal"));
        AddSetting(L("config.remove_base_cost"), "RemoveBaseCost", SettingType.Int);
        AddSetting(L("config.remove_step_cost"), "RemoveStepCost", SettingType.Int);
        AddSetting(L("config.use_linear_cost"), "UseLinearCost", SettingType.Bool);
        AddSetting(L("config.remove_limit"), "RemoveLimitPerShop", SettingType.Int);

        // Refresh
        AddHeader(L("config.header.refresh"));
        AddSetting(L("config.refresh_cost"), "RefreshCost", SettingType.Int);
        AddSetting(L("config.refresh_limit"), "RefreshLimitPerShop", SettingType.Int);

        // Rewards
        AddHeader(L("config.header.rewards"));
        AddSetting(L("config.enable_no_purchase_reward"), "EnableNoPurchaseReward", SettingType.Bool);
        AddSetting(L("config.no_purchase_gold"), "NoPurchaseRewardGold", SettingType.Int);
        AddSetting(L("config.enable_skip_card_gold"), "EnableSkipCardRewardGold", SettingType.Bool);
        AddSetting(L("config.skip_card_gold"), "SkipCardRewardGoldAmount", SettingType.Int);

        // Cards
        AddHeader(L("config.header.cards"));
        AddSetting(L("config.enable_cross_class"), "EnableCrossClassCards", SettingType.Bool);
        AddSetting(L("config.cross_class_chance"), "CrossClassCardChance", SettingType.Float);
        AddActionButton(L("config.enable_unlock_all"), L("config.unlock_all_run_now"), RunUnlockAllOnce);

        // Sell Mode
        AddHeader(L("config.header.sell"));
        AddSetting(L("config.enable_sell_mode"), "EnableSellMode", SettingType.Bool);
        AddSetting(L("config.sell_relic_ratio"), "SellRelicPriceRatio", SettingType.Float);
        AddSetting(L("config.sell_potion_ratio"), "SellPotionPriceRatio", SettingType.Float);
        AddSetting(L("config.sell_relic_min"), "SellRelicMinGold", SettingType.Int);
        AddSetting(L("config.sell_potion_min"), "SellPotionMinGold", SettingType.Int);
        
        AddHeader(L("config.header.relic_prices"));
        AddSetting(L("config.ancient_relic_price"), "SellAncientRelicBasePrice", SettingType.Int);
        AddSetting(L("config.starter_relic_price"), "SellStarterRelicBasePrice", SettingType.Int);
        AddSetting(L("config.event_relic_price"), "SellEventRelicBasePrice", SettingType.Int);

        AddHeader(L("config.header.other"));
        AddSetting(L("config.enable_gift_mode"), "EnableGiftMode", SettingType.Bool);

        AddHeader(L("config.header.enchant"));
        AddSetting(L("config.enable_removal_enchant_random"), "EnableRemovalEnchantRandom", SettingType.Bool);
        AddSetting(L("config.enable_enchant_service"), "EnableEnchantService", SettingType.Bool);
        AddSetting(L("config.enchant_start_shop_visit"), "EnchantStartShopVisit", SettingType.Int);
        AddSetting(L("config.enchant_replace_chance"), "EnchantReplaceChance", SettingType.Float);
        AddSetting(L("config.enchant_cost"), "EnchantCost", SettingType.Int);
        AddRangeSetting(L("config.enchant_amount_range"), "EnchantAmountRange", 1, 9999);
        AddRangeSetting(L("config.enchant_card_count_range"), "EnchantCardCountRange", 1, 99);
        AddSetting(L("config.enable_random_teammate_gift_service"), "EnableRandomTeammateGiftService", SettingType.Bool);
        AddRangeSetting(L("config.gift_service_card_count_range"), "GiftServiceCardCountRange", 1, 99);
        AddSetting(L("config.gift_service_base_cost"), "GiftServiceBaseCost", SettingType.Int);
        AddSetting(L("config.gift_service_step_cost"), "GiftServiceStepCost", SettingType.Int);
    }

    private enum SettingType { Bool, Int, Float }
    private Dictionary<string, Control> _controls = new();
    private Dictionary<string, (SpinBox Min, SpinBox Max)> _rangeControls = new();

    private void AddHeader(string text)
    {
        // Spacer before header
        _settingsContainer.AddChild(new Control { CustomMinimumSize = new Vector2(0, 10) });

        Label label = new() { Text = text };
        label.AddThemeFontSizeOverride("font_size", 18);
        label.AddThemeColorOverride("font_color", new Color("efc851")); // Gold
        _settingsContainer.AddChild(label);
        
        HSeparator sep = new();
        sep.Modulate = new Color(1, 1, 1, 0.5f);
        _settingsContainer.AddChild(sep);
    }

    private void AddSetting(string labelText, string propertyName, SettingType type)
    {
        HBoxContainer row = new();
        row.AddThemeConstantOverride("separation", 10);
        
        Label label = new() { 
            Text = labelText, 
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            VerticalAlignment = VerticalAlignment.Center
        };
        // Light gray for label text
        label.AddThemeColorOverride("font_color", new Color(0.9f, 0.9f, 0.9f));
        row.AddChild(label);

        Control input = null!;
        switch (type)
        {
            case SettingType.Bool:
                CheckBox cb = new();
                input = cb;
                break;
            case SettingType.Int:
                SpinBox sb = new();
                sb.Rounded = true;
                sb.MinValue = 0;
                sb.MaxValue = 9999;
                sb.CustomMinimumSize = new Vector2(100, 0);
                sb.Alignment = HorizontalAlignment.Right;
                input = sb;
                break;
            case SettingType.Float:
                SpinBox sbf = new();
                sbf.Step = 0.01;
                sbf.MinValue = 0;
                sbf.MaxValue = 10;
                sbf.CustomMinimumSize = new Vector2(100, 0);
                sbf.Alignment = HorizontalAlignment.Right;
                input = sbf;
                break;
        }
        input.Name = propertyName;
        row.AddChild(input);
        
        // Add hover effect container or similar if needed, but for now simple row
        _settingsContainer.AddChild(row);
        _controls[propertyName] = input;
    }

    private void AddRangeSetting(string labelText, string propertyName, int minValue, int maxValue)
    {
        HBoxContainer row = new();
        row.AddThemeConstantOverride("separation", 10);

        Label label = new()
        {
            Text = labelText,
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            VerticalAlignment = VerticalAlignment.Center
        };
        label.AddThemeColorOverride("font_color", new Color(0.9f, 0.9f, 0.9f));
        row.AddChild(label);

        SpinBox min = new();
        min.Rounded = true;
        min.MinValue = minValue;
        min.MaxValue = maxValue;
        min.CustomMinimumSize = new Vector2(80, 0);
        min.Alignment = HorizontalAlignment.Right;
        row.AddChild(min);

        Label sep = new() { Text = "~", VerticalAlignment = VerticalAlignment.Center };
        row.AddChild(sep);

        SpinBox max = new();
        max.Rounded = true;
        max.MinValue = minValue;
        max.MaxValue = maxValue;
        max.CustomMinimumSize = new Vector2(80, 0);
        max.Alignment = HorizontalAlignment.Right;
        row.AddChild(max);

        _settingsContainer.AddChild(row);
        _rangeControls[propertyName] = (min, max);
    }

    private void AddActionButton(string labelText, string buttonText, Action onPressed)
    {
        HBoxContainer row = new();
        row.AddThemeConstantOverride("separation", 10);

        Label label = new()
        {
            Text = labelText,
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            VerticalAlignment = VerticalAlignment.Center
        };
        label.AddThemeColorOverride("font_color", new Color(0.9f, 0.9f, 0.9f));
        row.AddChild(label);

        Button button = new()
        {
            Text = buttonText,
            CustomMinimumSize = new Vector2(120, 0)
        };
        button.Pressed += onPressed;
        row.AddChild(button);

        _settingsContainer.AddChild(row);
    }

    public void ShowPanel()
    {
        RefreshPresets();
        LoadValuesFromConfig();
        Visible = true;
    }

    public void HidePanel()
    {
        Visible = false;
    }

    private void RefreshPresets()
    {
        _presetOption.Clear();
        var presets = ConfigManager.GetPresets();
        foreach (var p in presets)
        {
            _presetOption.AddItem(p);
        }
    }

    private void OnPresetSelected(long index)
    {
        // Optional: Auto-load on select? Or wait for Load button. 
        // User asked for "configure, save, switch current reading preset".
        // Let's wait for Load button.
    }

    private void LoadSelectedPreset()
    {
        if (_presetOption.Selected == -1) return;
        string name = _presetOption.GetItemText(_presetOption.Selected);
        ConfigManager.LoadPreset(name);
        LoadValuesFromConfig(); // Refresh UI
    }

    private void SaveNewPreset()
    {
        string name = _newPresetName.Text.Trim();
        if (string.IsNullOrEmpty(name)) return;
        
        ConfigData data = CollectValues();
        ConfigManager.SavePreset(name, data);
        RefreshPresets();
        _newPresetName.Text = "";
    }

    private void LoadValuesFromConfig()
    {
        var data = ConfigManager.GetCurrentConfig();
        // Reflection-like mapping via Dictionary
        SetVal("RemoveBaseCost", data.RemoveBaseCost);
        SetVal("RemoveStepCost", data.RemoveStepCost);
        SetVal("UseLinearCost", data.UseLinearCost);
        SetVal("RemoveLimitPerShop", data.RemoveLimitPerShop);
        SetVal("RefreshCost", data.RefreshCost);
        SetVal("RefreshLimitPerShop", data.RefreshLimitPerShop);
        SetVal("EnableNoPurchaseReward", data.EnableNoPurchaseReward);
        SetVal("NoPurchaseRewardGold", data.NoPurchaseRewardGold);
        SetVal("EnableSkipCardRewardGold", data.EnableSkipCardRewardGold);
        SetVal("SkipCardRewardGoldAmount", data.SkipCardRewardGoldAmount);
        SetVal("EnableCrossClassCards", data.EnableCrossClassCards);
        SetVal("CrossClassCardChance", data.CrossClassCardChance);
        SetVal("EnableUnlockAll", data.EnableUnlockAll);
        SetVal("EnableSellMode", data.EnableSellMode);
        SetVal("SellRelicPriceRatio", data.SellRelicPriceRatio);
        SetVal("SellPotionPriceRatio", data.SellPotionPriceRatio);
        SetVal("SellRelicMinGold", data.SellRelicMinGold);
        SetVal("SellPotionMinGold", data.SellPotionMinGold);
        SetVal("SellAncientRelicBasePrice", data.SellAncientRelicBasePrice);
        SetVal("SellStarterRelicBasePrice", data.SellStarterRelicBasePrice);
        SetVal("SellEventRelicBasePrice", data.SellEventRelicBasePrice);
        SetVal("EnableGiftMode", data.EnableGiftMode);
        SetVal("EnableRemovalEnchantRandom", data.EnableRemovalEnchantRandom);
        SetVal("EnableEnchantService", data.EnableEnchantService);
        SetVal("EnchantStartShopVisit", data.EnchantStartShopVisit);
        SetVal("EnchantReplaceChance", data.EnchantReplaceChance);
        SetVal("EnchantCost", data.EnchantCost);
        SetRange("EnchantAmountRange", data.EnchantAmountRange);
        SetRange("EnchantCardCountRange", data.EnchantCardCountRange);
        SetVal("EnableRandomTeammateGiftService", data.EnableRandomTeammateGiftService);
        SetRange("GiftServiceCardCountRange", data.GiftServiceCardCountRange);
        SetVal("GiftServiceBaseCost", data.GiftServiceBaseCost);
        SetVal("GiftServiceStepCost", data.GiftServiceStepCost);
    }

    private void SetVal(string key, object value)
    {
        if (!_controls.ContainsKey(key)) return;
        var ctrl = _controls[key];
        if (ctrl is CheckBox cb && value is bool b) cb.ButtonPressed = b;
        if (ctrl is SpinBox sb)
        {
             if (value is int i) sb.Value = i;
             if (value is float f) sb.Value = f;
        }
    }

    private void SetRange(string key, Vector2I value)
    {
        if (!_rangeControls.TryGetValue(key, out var controls))
        {
            return;
        }

        int min = Math.Min(value.X, value.Y);
        int max = Math.Max(value.X, value.Y);
        controls.Min.Value = min;
        controls.Max.Value = max;
    }

    private ConfigData CollectValues()
    {
        var data = new ConfigData();
        data.RemoveBaseCost = GetInt("RemoveBaseCost");
        data.RemoveStepCost = GetInt("RemoveStepCost");
        data.UseLinearCost = GetBool("UseLinearCost");
        data.RemoveLimitPerShop = GetInt("RemoveLimitPerShop");
        data.RefreshCost = GetInt("RefreshCost");
        data.RefreshLimitPerShop = GetInt("RefreshLimitPerShop");
        data.EnableNoPurchaseReward = GetBool("EnableNoPurchaseReward");
        data.NoPurchaseRewardGold = GetInt("NoPurchaseRewardGold");
        data.EnableSkipCardRewardGold = GetBool("EnableSkipCardRewardGold");
        data.SkipCardRewardGoldAmount = GetInt("SkipCardRewardGoldAmount");
        data.EnableCrossClassCards = GetBool("EnableCrossClassCards");
        data.CrossClassCardChance = GetFloat("CrossClassCardChance");
        data.EnableUnlockAll = GetBool("EnableUnlockAll");
        data.EnableSellMode = GetBool("EnableSellMode");
        data.SellRelicPriceRatio = GetFloat("SellRelicPriceRatio");
        data.SellPotionPriceRatio = GetFloat("SellPotionPriceRatio");
        data.SellRelicMinGold = GetInt("SellRelicMinGold");
        data.SellPotionMinGold = GetInt("SellPotionMinGold");
        data.SellAncientRelicBasePrice = GetInt("SellAncientRelicBasePrice");
        data.SellStarterRelicBasePrice = GetInt("SellStarterRelicBasePrice");
        data.SellEventRelicBasePrice = GetInt("SellEventRelicBasePrice");
        data.EnableGiftMode = GetBool("EnableGiftMode");
        data.EnableRemovalEnchantRandom = GetBool("EnableRemovalEnchantRandom");
        data.EnableEnchantService = GetBool("EnableEnchantService");
        data.EnchantStartShopVisit = GetInt("EnchantStartShopVisit");
        data.EnchantReplaceChance = GetFloat("EnchantReplaceChance");
        data.EnchantCost = GetInt("EnchantCost");
        data.EnchantAmountRange = GetRange("EnchantAmountRange");
        data.EnchantCardCountRange = GetRange("EnchantCardCountRange");
        data.EnableRandomTeammateGiftService = GetBool("EnableRandomTeammateGiftService");
        data.GiftServiceCardCountRange = GetRange("GiftServiceCardCountRange");
        data.GiftServiceBaseCost = GetInt("GiftServiceBaseCost");
        data.GiftServiceStepCost = GetInt("GiftServiceStepCost");
        return data;
    }

    private int GetInt(string key) => _controls.ContainsKey(key) ? (int)((SpinBox)_controls[key]).Value : 0;
    private float GetFloat(string key) => _controls.ContainsKey(key) ? (float)((SpinBox)_controls[key]).Value : 0f;
    private bool GetBool(string key) => _controls.ContainsKey(key) && ((CheckBox)_controls[key]).ButtonPressed;
    private Vector2I GetRange(string key)
    {
        if (!_rangeControls.TryGetValue(key, out var controls))
        {
            return new Vector2I(1, 1);
        }

        int min = (int)Math.Min(controls.Min.Value, controls.Max.Value);
        int max = (int)Math.Max(controls.Min.Value, controls.Max.Value);
        return new Vector2I(min, max);
    }

    private void SaveAndClose()
    {
        ConfigData data = CollectValues();
        ConfigManager.Save(data);
        HidePanel();
    }

    private void RunUnlockAllOnce()
    {
        ConfigData data = CollectValues();
        data.EnableUnlockAll = true;
        ConfigManager.Save(data);
        LoadValuesFromConfig();
    }

    private string L(string key) => new LocString("shop_enhancement", key).GetFormattedText();
}
