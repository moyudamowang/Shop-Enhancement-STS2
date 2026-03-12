using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.addons.mega_text;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Gold;
using MegaCrit.Sts2.Core.Entities.Merchant;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.Screens.ScreenContext;
using MegaCrit.Sts2.Core.Nodes.Screens.Shops;
using MegaCrit.Sts2.Core.Rooms;
using ShopEnhancement;

namespace ShopEnhancement.Patches;

[HarmonyPatch(typeof(NMerchantInventory))]
public static class ShopRefreshPatches
{
    private static NButton? _refreshButton;
    private static int _refreshCount = 0;

    [HarmonyPatch(nameof(NMerchantInventory._Ready))]
    [HarmonyPostfix]
    public static void Ready_Postfix(NMerchantInventory __instance)
    {
        // Reset refresh count when entering a new shop (or re-entering?)
        // _Ready is called when the node is added to the scene.
        // For a new shop visit, this is correct.
        _refreshCount = 0;

        CreateRefreshButton(__instance);
    }

    private static void CreateRefreshButton(NMerchantInventory instance)
    {
        // Get the back button to use as a template/anchor
        var backButton = instance.GetNode<NBackButton>("%BackButton");
        if (backButton == null) return;

        // Create new button from scratch
        _refreshButton = new NButton();
        _refreshButton.Name = "RefreshButton";
        
        // Add to tree first
        backButton.GetParent().CallDeferred(Node.MethodName.AddChild, _refreshButton);

        // Position and Size
        // Using SetAnchorsPreset to align relative to the parent (likely full screen or a large container)
        // Preset: BottomLeft
        _refreshButton.SetAnchorsPreset(Control.LayoutPreset.BottomLeft);
        
        // After setting preset, Position is relative to the anchor.
        // We want it near the bottom left, but with some padding.
        // Assuming the parent is the screen or a full-rect container.
        
        // Reset offsets first to avoid weird stretching
        _refreshButton.OffsetLeft = 0;
        _refreshButton.OffsetTop = 0;
        _refreshButton.OffsetRight = 0;
        _refreshButton.OffsetBottom = 0;

        _refreshButton.Size = new Vector2(160, 50); 
        _refreshButton.Scale = Vector2.One;

        // Manually adjust position from the bottom-left corner
        // Positive X moves right, Negative Y moves up (Godot coordinate system)
        // The screenshot shows the back button is near bottom left.
        // We want to be below or near it? The user said "move to bottom left corner (red box)".
        // The red box in the screenshot is effectively at the very bottom left corner.
        // Let's set a fixed margin from bottom-left.
        _refreshButton.Position = new Vector2(30, -80); // 30px from left, 80px from bottom (since anchor is bottom-left, y=0 is bottom?) 
        // Wait, if anchor is BottomLeft, (0,0) is the bottom-left corner of the parent?
        // No, in Godot, (0,0) of the Control is top-left of the Control itself.
        // The Position property is relative to the parent's top-left if anchors are top-left.
        // If anchors are BottomLeft, the position behavior depends on how offsets are calculated.
        
        // Safer approach: Use SetPosition with respect to viewport or use anchors with margins.
        // Let's use SetAnchorsAndOffsetsPreset for cleaner logic if available, or just set margins.
        
        // Let's try: Anchor to Bottom-Left (0, 1)
        _refreshButton.AnchorLeft = 0;
        _refreshButton.AnchorTop = 1;
        _refreshButton.AnchorRight = 0;
        _refreshButton.AnchorBottom = 1;
        
        // Set position relative to that anchor
        // Position (x, y) where y is negative to be above the bottom edge?
        // Actually, let's just use absolute positioning logic relative to the parent size if we can't trust the parent's layout behavior immediately.
        // But the parent might resize.
        
        // Let's stick to standard Godot anchoring:
        // Anchor Bottom Left: (0, 1)
        // Grow Horizontal: Begin (Right)
        // Grow Vertical: End (Up? No, usually Down. We want to grow Up? No, size is fixed.)
        
        // Set Position:
        // x = 30 (padding from left)
        // y = -80 (padding from bottom, moving up)
        _refreshButton.SetPosition(new Vector2(30, -80) + new Vector2(0, instance.GetViewportRect().Size.Y)); 
        // Wait, if we use anchors, we shouldn't use absolute viewport coordinates.
        
        // Let's try simpler:
        // Don't mess with anchors too much if we don't know the parent's behavior perfectly.
        // The BackButton is likely anchored to Bottom-Left.
        // Let's just place ours relative to the BackButton's *position* but ensure we update it if needed?
        // No, user wants it in the corner.
        
        // Correct Godot 4 / Standard Godot way for Bottom-Left fixed element:
        _refreshButton.AnchorLeft = 0;
        _refreshButton.AnchorTop = 1;
        _refreshButton.AnchorRight = 0;
        _refreshButton.AnchorBottom = 1;
        _refreshButton.GrowVertical = Control.GrowDirection.Begin; // Grow Up
        _refreshButton.GrowHorizontal = Control.GrowDirection.End; // Grow Right
        
        // PositionOffset (margin from anchor)
        // We want it 30px from left, 30px from bottom.
        _refreshButton.Position = new Vector2(30, -80); // Relative to anchor point (0, Height)? 
        // Actually, if AnchorTop/Bottom is 1, the anchor point is at Y=ParentHeight.
        // So Position.Y = -80 means 80px above the bottom.
        // BUT, we need to ensure the parent is actually full screen sized.
        // The parent of BackButton is likely the screen container.
        
        // Let's revert to a safer calculation relative to BackButton but with a fixed offset that makes sense visually
        // IF the previous attempt failed (hidden), it might be because the parent isn't FullRect or anchors put it off screen.
        
        // BackButton is visible. Let's trust BackButton's anchor settings but just offset visually.
        // Previous error: `_refreshButton.Position = backButton.Position + new Vector2(10, 160);`
        // If BackButton is at bottom-left, +160 Y might push it OFF SCREEN (below bottom).
        // The screenshot shows BackButton is already near the bottom. +160 is definitely off screen.
        
        // User said "move to bottom left corner (red box)".
        // The red box is BELOW the back button in the screenshot? 
        // No, the red box is to the LEFT/BELOW the card area?
        // Looking at the screenshot, the BackButton is the arrow on the left.
        // The red box is in the empty space at the very bottom left corner.
        // The BackButton is slightly above that corner.
        
        // So we want Y to be HIGHER (smaller value) or LOWER (larger value)?
        // Godot Y increases downwards.
        // If BackButton is at (x, y), and we want to be below it, we add to Y.
        // BUT if BackButton is already near the bottom edge, adding 160 will push it out.
        // The BackButton in screenshot looks like it has some space below it.
        // Maybe not 160px though. 
        // Actually, look at the screenshot again. The red box is basically aligned with the bottom edge of the screen.
        // The BackButton is above it.
        
        // So we want:
        // X: similar to BackButton or slightly indented? Red box is left-aligned.
        // Y: Below BackButton.
        
        // Let's use BackButton position as reference but calculate a safe on-screen position.
        // Or just hardcode to Bottom-Left of screen using anchors.
        
        _refreshButton.AnchorLeft = 0;
        _refreshButton.AnchorTop = 1;
        _refreshButton.AnchorRight = 0;
        _refreshButton.AnchorBottom = 1;
        _refreshButton.OffsetLeft = 20;
        _refreshButton.OffsetTop = -70; // -20 - 50 height
        _refreshButton.OffsetRight = 180; // 20 + 160 width
        _refreshButton.OffsetBottom = -20; // 20px from bottom
        
        // This sets the margins directly relative to the bottom-left corner of the PARENT.
        // Assuming Parent is full screen (which it should be if it holds the BackButton for the shop).
        
        // Custom Style - Dark background with Gold border
        var panel = new Panel();
        panel.Name = "Background";
        panel.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        panel.MouseFilter = Control.MouseFilterEnum.Ignore;
        
        var style = new StyleBoxFlat();
        style.BorderWidthBottom = 2;
        style.BorderWidthLeft = 2;
        style.BorderWidthRight = 2;
        style.BorderWidthTop = 2;
        style.CornerRadiusBottomLeft = 6;
        style.CornerRadiusBottomRight = 6;
        style.CornerRadiusTopLeft = 6;
        style.CornerRadiusTopRight = 6;
        style.BgColor = new Color(0.1f, 0.1f, 0.2f, 0.9f); // Dark blue-ish
        style.BorderColor = new Color(0.8f, 0.6f, 0.2f); // Gold border
        
        panel.AddThemeStyleboxOverride("panel", style);
        _refreshButton.CallDeferred(Node.MethodName.AddChild, panel);

        // Hover effect
        _refreshButton.MouseEntered += () => 
        {
            style.BgColor = new Color(0.2f, 0.2f, 0.35f, 0.9f); // Lighter
        };
        _refreshButton.MouseExited += () => 
        {
             style.BgColor = new Color(0.1f, 0.1f, 0.2f, 0.9f); // Original
        };

        // Connect signal
        _refreshButton.Released += (btn) => TaskHelper.RunSafely(OnRefreshClicked(instance));
        
        // Try to set text (will create label if needed)
        // SetButtonText(_refreshButton, new LocString("shop_enhancement", "refresh.button_text").GetFormattedText());
        UpdateRefreshButtonState(instance);
    }

    private static void SetButtonText(NButton button, string text, Control context)
    {
        // Try to find a label
        var label = button.GetNodeOrNull<MegaLabel>("Label") ?? button.GetNodeOrNull<MegaLabel>("Text");
        
        if (label == null)
        {
            label = new MegaLabel();
            label.Name = "Label";

            if (context != null)
            {
                label.AddThemeFontOverride("font", context.GetThemeFont("font", "Label"));
            }
            
            button.CallDeferred(Node.MethodName.AddChild, label);
            
            // Use FullRect to fill the button, ensuring the label has the same size as the button
            label.SetAnchorsPreset(Control.LayoutPreset.FullRect);
            
            // Center text alignment within the label
            label.HorizontalAlignment = HorizontalAlignment.Center;
            label.VerticalAlignment = VerticalAlignment.Center;
            label.AutowrapMode = TextServer.AutowrapMode.Off;
            
            label.MouseFilter = Control.MouseFilterEnum.Ignore;
        }

        label.OffsetLeft = 10;
        label.OffsetTop = 6;
        label.OffsetRight = -10;
        label.OffsetBottom = -6;
        
        label.SetTextAutoSize(text);
    }

    private static async Task OnRefreshClicked(NMerchantInventory instance)
    {
        if (instance.Inventory == null) return;

        Player player = instance.Inventory.Player;
        int cost = ShopEnhancementConfig.RefreshCost;

        // Check Gold
        if (player.Gold < cost)
        {
            // TODO: Show "Not enough gold" feedback
            return;
        }

        // Check Limit
        if (ShopEnhancementConfig.RefreshLimitPerShop != -1 && _refreshCount >= ShopEnhancementConfig.RefreshLimitPerShop)
        {
            // Limit reached
            return;
        }

        // Deduct Gold
        await PlayerCmd.LoseGold(cost, player, GoldLossType.Spent);
        _refreshCount++;

        // Perform Refresh
        await RefreshInventory(instance, player);
        
        UpdateRefreshButtonState(instance);
    }


    private static async Task RefreshInventory(NMerchantInventory instance, Player player)
    {
        // 1. Unsubscribe from old entries
        UnsubscribeFromOldEntries(instance);

        // 2. Create new Inventory
        MerchantInventory newInventory = MerchantInventory.CreateForNormalMerchant(player);

        // 2.1 Try to inject cross-class cards
        TryReplaceWithCrossClassCards(newInventory, player);

        // 3. Set Inventory field to null (to bypass Initialize check)
        PropertyInfo inventoryProp = AccessTools.Property(typeof(NMerchantInventory), "Inventory");
        // Inventory property has a private setter.
        // We can set the backing field directly if we know the name, 
        // or use the property setter via reflection.
        inventoryProp.SetValue(instance, null);

        // Fix for "Signal 'Hovered' is already connected" error
        DisconnectSlots(instance);

        // 4. Call Initialize
        // We need the dialogue set. MerchantRoom.Dialogue is static.
        MerchantDialogueSet dialogue = MerchantRoom.Dialogue;
        
        instance.Initialize(newInventory, dialogue);

        // 5. Update UI
        // Initialize refills the slots.
        
        // Disable Gift Mode on refresh
        if (GiftModeState.IsEnabled(newInventory))
        {
            GiftModeState.Set(newInventory, false);
        }
        // Force update Gift Mode button UI
        ShopGiftModeButtonPatches.ForceUpdateVisuals(instance);

        // 6. Optional: Play sound
        // SfxCmd.Play("event:/sfx/npcs/merchant/merchant_welcome");
    }

    private static void DisconnectSlots(Node node)
    {
        string typeName = node.GetType().Name;

        // Only disconnect signals from actual merchant slots (Cards, Relics, Potions)
        // This avoids breaking persistent UI elements like the Back Button or Sell Toggle
        if (typeName.Contains("MerchantCard") || 
            typeName.Contains("MerchantRelic") || 
            typeName.Contains("MerchantPotion") || 
            typeName.Contains("MerchantSlot"))
        {
            DisconnectSignal(node, "Hovered");
            DisconnectSignal(node, "Unhovered");
            DisconnectSignal(node, "Clicked");
            DisconnectSignal(node, "Released");
        }

        foreach (var child in node.GetChildren())
        {
            DisconnectSlots(child);
        }
    }

    private static void DisconnectSignal(Node node, string signalName)
    {
        if (node.HasSignal(signalName))
        {
            // Create a copy of the connection list to avoid modification issues during iteration
            var connections = node.GetSignalConnectionList(signalName);
            foreach (var conn in connections)
            {
                var callable = conn["callable"].AsCallable();
                if (node.IsConnected(signalName, callable))
                {
                    node.Disconnect(signalName, callable);
                }
            }
        }
    }

    private static void UnsubscribeFromOldEntries(NMerchantInventory instance)
    {
        if (instance.Inventory == null) return;

        // Get the OnPurchaseCompleted method delegate
        MethodInfo onPurchaseCompletedMethod = AccessTools.Method(typeof(NMerchantInventory), "OnPurchaseCompleted");
        // We can't easily create a delegate from a method info for a specific instance generic-ally without knowing the type at compile time easily?
        // Wait, we can just use the same logic as _ExitTree
        // Iterate entries and remove the event handler.
        // But we need the EXACT delegate instance to remove it?
        // Or we can use reflection to clear the event list on the entry? Too invasive.
        
        // Actually, creating a new delegate instance for the same method/target *should* work for removal in C#?
        // Yes, `new Action(...)` with same target/method matches.
        
        // Delegate for PurchaseCompleted
        // It's Action<PurchaseStatus, MerchantEntry>
        
        // We need to access _merchantDialogue to remove ShowForPurchaseAttempt
        FieldInfo? dialogueField = AccessTools.Field(typeof(NMerchantInventory), "_merchantDialogue");
        if (dialogueField == null) return;
        
        NMerchantDialogue? dialogue = dialogueField.GetValue(instance) as NMerchantDialogue;
        if (dialogue == null) return;
        
        foreach (var entry in instance.Inventory.AllEntries)
        {
            // Remove OnPurchaseCompleted
            // We can use EventInfo.RemoveEventHandler, but we need the delegate.
            // Let's try constructing it.
            /*
            var onPurchaseCompleted = Delegate.CreateDelegate(
                typeof(Action<PurchaseStatus, MerchantEntry>), 
                instance, 
                onPurchaseCompletedMethod);
             // This might fail if method is private.
            */
            
            // Simpler approach: Just don't worry about it too much as discussed in thought process?
            // The old inventory is garbage collected.
            // But let's try to be clean.
            
            // Actually, we can just iterate the invocation list if we could access the event backing field.
            // But events in interfaces/abstract classes might be tricky.
            
            // Let's skip explicit unsubscription for now to avoid reflection complexity/errors, 
            // assuming GC handles the cycle (OldInv -> Entry -> Delegate -> Instance -> NewInv).
            // Wait, Instance -> NewInv. Instance is NOT holding OldInv (we set it to null).
            // So OldInv is unreachable (unless held by something else).
            // So OldInv and its Entries are GC'd.
            // The Entries hold a reference to Instance (via delegate).
            // This is a memory leak if OldInv is NOT GC'd.
            // But who holds OldInv? Nothing.
            // So it should be fine.
        }
    }

    private static void UpdateRefreshButtonState(NMerchantInventory instance)
    {
        if (_refreshButton == null || instance.Inventory == null) return;
        
        bool isShopInventoryVisible = instance.IsOpen && ActiveScreenContext.Instance.IsCurrent(instance);
        _refreshButton.Visible = isShopInventoryVisible;
        if (!isShopInventoryVisible) return;

        Player player = instance.Inventory.Player;
        int cost = ShopEnhancementConfig.RefreshCost;

        // Check Limit
        bool limitReached = ShopEnhancementConfig.RefreshLimitPerShop != -1 && _refreshCount >= ShopEnhancementConfig.RefreshLimitPerShop;
        
        if (limitReached)
        {
            _refreshButton.Disable();
            _refreshButton.Modulate = new Color(0.5f, 0.5f, 0.5f, 0.8f);
        }
        else
        {
            // Check Gold
            bool canAfford = player.Gold >= cost;

            if (canAfford)
            {
                 _refreshButton.Enable();
                 _refreshButton.Modulate = Colors.White;
            }
            else
            {
                 _refreshButton.Disable();
                 _refreshButton.Modulate = new Color(0.5f, 0.5f, 0.5f, 0.8f);
            }
        }

        // Update Text with Cost
        string baseText = new LocString("shop_enhancement", "refresh.button_text").GetFormattedText();
        SetButtonText(_refreshButton, $"{baseText} ({cost}g)", instance);
    }
    
    // Patch OnActiveScreenUpdated to keep button state correct
    [HarmonyPatch("OnActiveScreenUpdated")] // This method name might be different in compiled code if it's an event handler?
    // In NMerchantInventory.cs it is `private void OnActiveScreenUpdated()`.
    // Harmony can patch private methods.
    [HarmonyPostfix]
    public static void OnActiveScreenUpdated_Postfix(NMerchantInventory __instance)
    {
        UpdateRefreshButtonState(__instance);
    }

    private static void TryReplaceWithCrossClassCards(MerchantInventory inventory, Player player)
    {
        if (!ShopEnhancementConfig.EnableCrossClassCards) return;
        if (ShopEnhancementConfig.CrossClassCardChance <= 0f) return;

        var creationResultField = AccessTools.Field(typeof(MerchantCardEntry), "<CreationResult>k__BackingField");
        var cardTypeField = AccessTools.Field(typeof(MerchantCardEntry), "_cardType");
        if (creationResultField == null || cardTypeField == null) return;

        var otherCharacters = ModelDb.AllCharacters.Where(c => c.Id != player.Character.Id).ToList();
        if (otherCharacters.Count == 0) return;

        var usedCanonicalCards = inventory.CardEntries
            .Select(e => e.CreationResult?.Card.CanonicalInstance)
            .OfType<CardModel>()
            .ToHashSet();

        foreach (var entry in inventory.CharacterCardEntries)
        {
            if (GD.Randf() >= ShopEnhancementConfig.CrossClassCardChance) continue;

            var currentCanonical = entry.CreationResult?.Card.CanonicalInstance;
            if (currentCanonical != null)
            {
                usedCanonicalCards.Remove(currentCanonical);
            }

            bool replaced = false;
            var cardType = (CardType?)cardTypeField.GetValue(entry);
            if (cardType == null)
            {
                if (currentCanonical != null) usedCanonicalCards.Add(currentCanonical);
                continue;
            }

            for (int attempt = 0; attempt < 6 && !replaced; attempt++)
            {
                CharacterModel randomCharacter = otherCharacters[GD.RandRange(0, otherCharacters.Count - 1)];
                IEnumerable<CardModel> pool = randomCharacter.CardPool.GetUnlockedCards(player.UnlockState, player.RunState.CardMultiplayerConstraint);

                try
                {
                    CardCreationResult newResult = CardFactory.CreateForMerchant(player, pool.Except(usedCanonicalCards), cardType.Value);
                    creationResultField.SetValue(entry, newResult);
                    entry.CalcCost();
                    usedCanonicalCards.Add(newResult.Card.CanonicalInstance);
                    replaced = true;
                }
                catch
                {
                }
            }

            if (!replaced && currentCanonical != null)
            {
                usedCanonicalCards.Add(currentCanonical);
            }
        }
    }
}

[HarmonyPatch(typeof(NMerchantCard))]
public static class NMerchantCardDisposedGuardPatches
{
    [HarmonyPatch("UpdateVisual")]
    [HarmonyFinalizer]
    public static Exception? UpdateVisual_Finalizer(Exception? __exception)
    {
        if (__exception is ObjectDisposedException objectDisposedException &&
            objectDisposedException.ObjectName == "MegaCrit.Sts2.addons.mega_text.MegaLabel")
        {
            return null;
        }

        return __exception;
    }
}
