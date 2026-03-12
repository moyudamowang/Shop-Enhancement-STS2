using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.Screens.MainMenu;
using ShopEnhancement.Ui;

namespace ShopEnhancement.Patches;

[HarmonyPatch(typeof(NMainMenu))]
public static class MainMenuPatch
{
    private static ConfigPanel? _configPanel;

    [HarmonyPatch(nameof(NMainMenu._Ready))]
    [HarmonyPostfix]
    public static void Ready_Postfix(NMainMenu __instance)
    {
        NButton configButton = new()
        {
            Name = "ShopConfigButton",
            CustomMinimumSize = new Vector2(120, 40)
        };
        
        configButton.SetAnchorsPreset(Control.LayoutPreset.BottomRight);
        configButton.GrowHorizontal = Control.GrowDirection.Begin;
        configButton.GrowVertical = Control.GrowDirection.Begin;
        configButton.OffsetRight = -20;
        configButton.OffsetBottom = -20;

        // Background Panel
        Panel panel = new();
        panel.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        panel.MouseFilter = Control.MouseFilterEnum.Ignore;
        configButton.AddChild(panel);

        // Label
        Label label = new()
        {
            Text = "Shop Config",
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };
        label.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        configButton.AddChild(label);

        __instance.AddChild(configButton);

        configButton.Connect(NClickableControl.SignalName.Released, Callable.From<NButton>(_ =>
        {
            if (_configPanel == null || !GodotObject.IsInstanceValid(_configPanel))
            {
                _configPanel = new ConfigPanel();
                __instance.AddChild(_configPanel);
            }
            _configPanel.ShowPanel();
        }));
    }
}
