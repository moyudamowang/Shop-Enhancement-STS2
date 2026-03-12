using System.Runtime.CompilerServices;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.addons.mega_text;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.Screens.ScreenContext;
using MegaCrit.Sts2.Core.Nodes.Screens.Shops;

namespace ShopEnhancement.Patches;

[HarmonyPatch(typeof(NMerchantInventory))]
public static class ShopSellModeButtonPatches
{
    private sealed class ButtonHolder
    {
        public NButton? Button;
        public MegaLabel? Label;
        public Panel? Panel;
    }

    private static readonly ConditionalWeakTable<NMerchantInventory, ButtonHolder> Buttons = new();

    [HarmonyPatch(nameof(NMerchantInventory._Ready))]
    [HarmonyPostfix]
    public static void Ready_Postfix(NMerchantInventory __instance)
    {
        if (!ShopEnhancementConfig.EnableSellMode)
            return;

        var holder = Buttons.GetOrCreateValue(__instance);
        if (holder.Button != null && GodotObject.IsInstanceValid(holder.Button))
            return;

        var backButton = __instance.GetNodeOrNull<Control>("%BackButton");
        if (backButton == null)
            return;

        var button = new NButton();
        button.Name = "SellModeButton";
        backButton.GetParent().CallDeferred(Node.MethodName.AddChild, button);
        button.AnchorLeft = 0;
        button.AnchorTop = 1;
        button.AnchorRight = 0;
        button.AnchorBottom = 1;
        button.OffsetLeft = 20;
        button.OffsetTop = -130;
        button.OffsetRight = 180;
        button.OffsetBottom = -80;

        var panel = new Panel();
        panel.Name = "Background";
        panel.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        panel.MouseFilter = Control.MouseFilterEnum.Ignore;
        button.CallDeferred(Node.MethodName.AddChild, panel);

        var style = new StyleBoxFlat();
        style.BorderWidthBottom = 2;
        style.BorderWidthLeft = 2;
        style.BorderWidthRight = 2;
        style.BorderWidthTop = 2;
        style.CornerRadiusBottomLeft = 6;
        style.CornerRadiusBottomRight = 6;
        style.CornerRadiusTopLeft = 6;
        style.CornerRadiusTopRight = 6;
        panel.AddThemeStyleboxOverride("panel", style);

        var label = new MegaLabel();
        label.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        label.OffsetLeft = 10;
        label.OffsetTop = 6;
        label.OffsetRight = -10;
        label.OffsetBottom = -6;
        label.HorizontalAlignment = HorizontalAlignment.Center;
        label.VerticalAlignment = VerticalAlignment.Center;
        label.AutowrapMode = TextServer.AutowrapMode.Off;
        label.MouseFilter = Control.MouseFilterEnum.Ignore;
        label.AddThemeFontOverride("font", __instance.GetThemeFont("font", "Label"));
        button.CallDeferred(Node.MethodName.AddChild, label);

        button.Connect(NClickableControl.SignalName.Released, Callable.From<NButton>(_ =>
        {
            bool enabled = SellModeState.Toggle(__instance);
            ApplyButtonVisual(holder, enabled);
        }));

        holder.Button = button;
        holder.Label = label;
        holder.Panel = panel;

        ApplyButtonVisual(holder, false);
        UpdateButtonVisibility(__instance);
    }

    [HarmonyPatch("Close")]
    [HarmonyPrefix]
    public static void Close_Prefix(NMerchantInventory __instance)
    {
        SellModeState.Set(__instance, false);
        var holder = Buttons.GetOrCreateValue(__instance);
        if (holder.Button != null && holder.Label != null && GodotObject.IsInstanceValid(holder.Button))
        {
            ApplyButtonVisual(holder, false);
        }
        UpdateButtonVisibility(__instance);
    }

    [HarmonyPatch("OnActiveScreenUpdated")]
    [HarmonyPostfix]
    public static void OnActiveScreenUpdated_Postfix(NMerchantInventory __instance)
    {
        UpdateButtonVisibility(__instance);
    }

    private static void UpdateButtonVisibility(NMerchantInventory instance)
    {
        if (!Buttons.TryGetValue(instance, out var holder) || holder.Button == null || !GodotObject.IsInstanceValid(holder.Button))
            return;

        bool isShopInventoryVisible = instance.IsOpen && ActiveScreenContext.Instance.IsCurrent(instance);
        holder.Button.Visible = isShopInventoryVisible;
    }

    private static void ApplyButtonVisual(ButtonHolder holder, bool enabled)
    {
        if (holder.Button == null || holder.Label == null || holder.Panel == null)
            return;

        var button = holder.Button;
        var label = holder.Label;
        var panel = holder.Panel;

        label.SetTextAutoSize(new LocString("shop_enhancement", enabled ? "sell.mode_on" : "sell.mode_off").GetFormattedText());
        var style = new StyleBoxFlat();
        style.BorderWidthBottom = 2;
        style.BorderWidthLeft = 2;
        style.BorderWidthRight = 2;
        style.BorderWidthTop = 2;
        style.CornerRadiusBottomLeft = 6;
        style.CornerRadiusBottomRight = 6;
        style.CornerRadiusTopLeft = 6;
        style.CornerRadiusTopRight = 6;
        style.BgColor = enabled ? new Color(0.15f, 0.35f, 0.2f, 0.92f) : new Color(0.15f, 0.15f, 0.22f, 0.92f);
        style.BorderColor = enabled ? new Color(0.6f, 0.85f, 0.55f) : new Color(0.8f, 0.65f, 0.3f);
        panel.AddThemeStyleboxOverride("panel", style);
        button.Modulate = Colors.White;
    }
}
