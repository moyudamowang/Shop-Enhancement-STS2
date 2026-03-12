using System.Linq;
using System.Runtime.CompilerServices;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.addons.mega_text;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.Screens.ScreenContext;
using MegaCrit.Sts2.Core.Nodes.Screens.Shops;
using MegaCrit.Sts2.Core.Platform;
using MegaCrit.Sts2.Core.Runs;

namespace ShopEnhancement.Patches;

[HarmonyPatch(typeof(NMerchantInventory))]
public static class ShopGiftModeButtonPatches
{
    private sealed class ButtonHolder
    {
        public NButton? GiftButton;
        public MegaLabel? GiftLabel;
        public Panel? GiftPanel;
        
        public NButton? TargetButton;
        public MegaLabel? TargetLabel;
        public Panel? TargetPanel;
    }

    private static readonly ConditionalWeakTable<NMerchantInventory, ButtonHolder> Buttons = new();

    [HarmonyPatch(nameof(NMerchantInventory._Ready))]
    [HarmonyPostfix]
    public static void Ready_Postfix(NMerchantInventory __instance)
    {
        if (!ShopEnhancementConfig.EnableGiftMode)
            return;

        var holder = Buttons.GetOrCreateValue(__instance);
        if (holder.GiftButton != null && GodotObject.IsInstanceValid(holder.GiftButton))
            return;

        var backButton = __instance.GetNodeOrNull<Control>("%BackButton");
        if (backButton == null)
            return;

        // Create Gift Mode Button
        // Positioned to the right of Sell Mode Button (which is at left:20, right:180)
        // So let's put this at left:200, right:360
        var giftButton = CreateButton(backButton.GetParent(), "GiftModeButton", 200, -130, 360, -80);
        
        var giftPanel = CreatePanel(giftButton);
        var giftLabel = CreateLabel(giftButton);
        if (__instance.GetThemeFont("font", "Label") != null)
        {
            giftLabel.AddThemeFontOverride("font", __instance.GetThemeFont("font", "Label"));
        }
        else
        {
            // Fallback or log if needed, though usually Label font should exist in theme
        }

        giftButton.Connect(NClickableControl.SignalName.Released, Callable.From<NButton>(_ =>
        {
            if (__instance.Inventory == null) return;

            // Toggle Gift Mode
            bool enabled = GiftModeState.Toggle(__instance.Inventory);
            
            // If enabling, ensure we have a valid target
            if (enabled)
            {
                ulong currentTarget = GiftModeState.GetTargetPlayerId(__instance.Inventory);
                if (currentTarget == 0)
                {
                    // Default to first other player
                    var player = __instance.Inventory.Player;
                    var runState = player?.RunState;
                    if (player != null && runState != null)
                    {
                        var otherPlayer = runState.Players.FirstOrDefault(p => p.NetId != player.NetId);
                        if (otherPlayer != null)
                        {
                            GiftModeState.SetTargetPlayerId(__instance.Inventory, otherPlayer.NetId);
                        }
                    }
                }
                
                // If Sell Mode is on, turn it off
                if (SellModeState.IsEnabled(__instance))
                {
                    SellModeState.Set(__instance, false);
                    // We might need to refresh Sell Mode button visual, but we don't have easy access to it here.
                    // Ideally, SellModeState.Set should trigger an event or we check in Update.
                    // For now, let's assume user handles it or we accept both being visually "on" but logic handles priority.
                    // Actually, let's just disable it in logic if needed.
                }
            }

            ApplyGiftButtonVisual(holder, enabled);
            UpdateButtonVisibility(__instance);
        }));

        holder.GiftButton = giftButton;
        holder.GiftLabel = giftLabel;
        holder.GiftPanel = giftPanel;

        // Create Target Player Button
        // Positioned above Gift Mode Button
        var targetButton = CreateButton(backButton.GetParent(), "GiftTargetButton", 200, -190, 360, -140);
        var targetPanel = CreatePanel(targetButton);
        var targetLabel = CreateLabel(targetButton);
        if (__instance.GetThemeFont("font", "Label") != null)
        {
            targetLabel.AddThemeFontOverride("font", __instance.GetThemeFont("font", "Label"));
        }
        
        targetButton.Connect(NClickableControl.SignalName.Released, Callable.From<NButton>(_ =>
        {
            CycleTargetPlayer(__instance);
            UpdateTargetButtonVisual(holder, __instance);
        }));

        holder.TargetButton = targetButton;
        holder.TargetLabel = targetLabel;
        holder.TargetPanel = targetPanel;

        ApplyGiftButtonVisual(holder, false);
        UpdateButtonVisibility(__instance);
        UpdateTargetButtonVisual(holder, __instance);
    }

    [HarmonyPatch("Close")]
    [HarmonyPrefix]
    public static void Close_Prefix(NMerchantInventory __instance)
    {
        if (__instance.Inventory != null)
        {
            GiftModeState.Set(__instance.Inventory, false);
        }
        
        var holder = Buttons.GetOrCreateValue(__instance);
        if (holder.GiftButton != null && GodotObject.IsInstanceValid(holder.GiftButton))
        {
             ApplyGiftButtonVisual(holder, false);
             UpdateButtonVisibility(__instance);
        }
    }

    [HarmonyPatch("OnActiveScreenUpdated")]
    [HarmonyPostfix]
    public static void OnActiveScreenUpdated_Postfix(NMerchantInventory __instance)
    {
        UpdateButtonVisibility(__instance);
    }

    // Public method to force update visuals from other patches (e.g. ShopRefreshPatches)
    public static void ForceUpdateVisuals(NMerchantInventory instance)
    {
        if (Buttons.TryGetValue(instance, out var holder))
        {
            bool enabled = instance.Inventory != null && GiftModeState.IsEnabled(instance.Inventory);
            ApplyGiftButtonVisual(holder, enabled);
            UpdateButtonVisibility(instance);
        }
    }

    private static void UpdateButtonVisibility(NMerchantInventory instance)
    {
        if (!Buttons.TryGetValue(instance, out var holder) || holder.GiftButton == null || !GodotObject.IsInstanceValid(holder.GiftButton))
            return;

        bool isShopInventoryVisible = instance.IsOpen && ActiveScreenContext.Instance.IsCurrent(instance);
        
        // Check multiplayer
        var runState = instance.Inventory?.Player?.RunState;
        bool isMultiplayer = runState != null && runState.Players.Count > 1;

        bool showGiftButton = isShopInventoryVisible && isMultiplayer;
        holder.GiftButton.Visible = showGiftButton;
        
        bool giftModeEnabled = instance.Inventory != null && GiftModeState.IsEnabled(instance.Inventory);
        bool showTargetButton = showGiftButton && giftModeEnabled;
        
        if (holder.TargetButton != null)
        {
            holder.TargetButton.Visible = showTargetButton;
        }
    }
    
    // Helper methods
    private static NButton CreateButton(Node parent, string name, float left, float top, float right, float bottom)
    {
        var button = new NButton();
        button.Name = name;
        parent.CallDeferred(Node.MethodName.AddChild, button);
        button.AnchorLeft = 0;
        button.AnchorTop = 1;
        button.AnchorRight = 0;
        button.AnchorBottom = 1;
        button.OffsetLeft = left;
        button.OffsetTop = top;
        button.OffsetRight = right;
        button.OffsetBottom = bottom;
        return button;
    }

    private static Panel CreatePanel(NButton parent)
    {
        var panel = new Panel();
        panel.Name = "Background";
        panel.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        panel.MouseFilter = Control.MouseFilterEnum.Ignore;
        parent.CallDeferred(Node.MethodName.AddChild, panel);

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
        return panel;
    }

    private static MegaLabel CreateLabel(NButton parent)
    {
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
        parent.CallDeferred(Node.MethodName.AddChild, label);
        return label;
    }

    private static void ApplyGiftButtonVisual(ButtonHolder holder, bool enabled)
    {
        if (holder.GiftLabel == null || holder.GiftPanel == null) return;

        // "Gift Mode"
        string text = "Gift Mode"; // Ideally localization
        holder.GiftLabel.SetTextAutoSize(text);

        var style = (StyleBoxFlat)holder.GiftPanel.GetThemeStylebox("panel");
        
        // Match SellMode colors logic but with blue theme for Gift
        if (enabled)
        {
            // Active state (similar to Sell Mode On but Blue)
            style.BgColor = new Color(0.15f, 0.25f, 0.4f, 0.92f); 
            style.BorderColor = new Color(0.5f, 0.7f, 1f);
        }
        else
        {
            // Inactive state (similar to Sell Mode Off)
            style.BgColor = new Color(0.15f, 0.15f, 0.22f, 0.92f);
            style.BorderColor = new Color(0.8f, 0.65f, 0.3f); // Keep gold/yellow border for consistency with UI theme
        }
        holder.TargetLabel.Modulate = Colors.Yellow;      
        holder.TargetButton.Modulate = Colors.White;
    }

    private static void UpdateTargetButtonVisual(ButtonHolder holder, NMerchantInventory inventory)
    {
        if (holder.TargetLabel == null || holder.TargetPanel == null || inventory.Inventory == null) return;

        ulong targetId = GiftModeState.GetTargetPlayerId(inventory.Inventory);
        var runState = inventory.Inventory?.Player?.RunState;
        
        string targetName = "None";
        if (runState != null && targetId != 0)
        {
            var player = runState.Players.FirstOrDefault(p => p.NetId == targetId);
            if (player != null)
            {
                // Try to get character title as fallback for name
                string charTitle = player.Character.Title.GetFormattedText();
                
                // Get platform name (Steam name etc)
                string platformName = PlatformUtil.GetPlayerName(RunManager.Instance.NetService.Platform, targetId);
                
                if (!string.IsNullOrEmpty(platformName))
                {
                    targetName = $"{charTitle} ({platformName})";
                }
                else
                {
                    targetName = $"{charTitle} ({targetId})";
                }
            }
        }
        
        holder.TargetLabel.SetTextAutoSize($"To: {targetName}");
        
        // Style for target button (always "active" looking if visible, or just standard UI style)
        var style = (StyleBoxFlat)holder.TargetPanel.GetThemeStylebox("panel");
        style.BgColor = new Color(0.15f, 0.15f, 0.22f, 0.92f);
        style.BorderColor = new Color(0.8f, 0.65f, 0.3f);
        //设置字体颜色
        holder.TargetLabel.Modulate = Colors.Yellow;      
        holder.TargetButton.Modulate = Colors.White;
    }

    private static void CycleTargetPlayer(NMerchantInventory inventory)
    {
        if (inventory.Inventory == null) return;
        var currentInventory = inventory.Inventory;
        
        var runState = currentInventory.Player?.RunState;
        if (runState == null) return;

        ulong currentTargetId = GiftModeState.GetTargetPlayerId(currentInventory);
        if (currentInventory.Player == null) return;
        ulong myId = currentInventory.Player.NetId;

        // Get list of other players
        var otherPlayers = runState.Players.Where(p => p.NetId != myId).ToList();
        if (otherPlayers.Count == 0) return;

        int currentIndex = otherPlayers.FindIndex(p => p.NetId == currentTargetId);
        int nextIndex = (currentIndex + 1) % otherPlayers.Count;
        
        GiftModeState.SetTargetPlayerId(inventory.Inventory, otherPlayers[nextIndex].NetId);
    }
}
