using System;
using System.Reflection;
using System.Threading.Tasks;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Merchant;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Gold;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Screens.Shops;
using MegaCrit.Sts2.Core.Runs;
using ShopEnhancement.Network;

namespace ShopEnhancement.Patches;

[HarmonyPatch(typeof(MerchantEntry), nameof(MerchantEntry.OnTryPurchaseWrapper))]
public static class GiftPurchasePatches
{
    private static readonly MethodInfo ClearAfterPurchaseMethod = AccessTools.Method(typeof(MerchantEntry), "ClearAfterPurchase");
    private static readonly MethodInfo RestockAfterPurchaseMethod = AccessTools.Method(typeof(MerchantEntry), "RestockAfterPurchase");

    [HarmonyPrefix]
    public static bool Prefix(MerchantEntry __instance, MerchantInventory? inventory, ref Task<bool> __result)
    {
        if (inventory == null) return true;
        if (!ShopEnhancementConfig.EnableGiftMode) return true;
        if (!GiftModeState.IsEnabled(inventory)) return true;

        ulong targetId = GiftModeState.GetTargetPlayerId(inventory);
        if (targetId == 0 || targetId == inventory.Player.NetId) return true; // Target invalid or self, use normal purchase

        // Handle Gift Purchase
        __result = HandleGiftPurchase(__instance, inventory, targetId);
        return false; // Skip original method
    }

    private static async Task<bool> HandleGiftPurchase(MerchantEntry entry, MerchantInventory inventory, ulong targetId)
    {
        var player = inventory.Player;
        int cost = entry.Cost;

        if (player.Gold < cost)
        {
            entry.InvokePurchaseFailed(PurchaseStatus.FailureGold);
            return false;
        }

        // Deduct Gold
        await PlayerCmd.LoseGold(cost, player, GoldLossType.Spent);

        // Send Network Message
        string itemId = "";
        string itemType = "";
        int upgradeCount = 0;
        int misc = 0;

        if (entry is MerchantCardEntry cardEntry && cardEntry.CreationResult != null)
        {
            itemId = cardEntry.CreationResult.Card.Id.ToString();
            itemType = "Card";
            
            // Try to get upgrade count and misc via reflection to avoid compilation errors if properties are missing on CardModel
            // or if CreationResult structure is different than expected.
            try 
            {
                var result = cardEntry.CreationResult;
                var type = result.GetType();
                
                // Check CreationResult for UpgradeCount/Misc
                var upgProp = type.GetProperty("UpgradeCount") ?? type.GetProperty("TimesUpgraded");
                if (upgProp != null && upgProp.PropertyType == typeof(int))
                {
                    upgradeCount = (int)(upgProp.GetValue(result) ?? 0);
                }
                
                var miscProp = type.GetProperty("Misc");
                if (miscProp != null && miscProp.PropertyType == typeof(int))
                {
                    misc = (int)(miscProp.GetValue(result) ?? 0);
                }
            }
            catch (Exception ex)
            {
                MainFile.Logger.Error($"Failed to get card properties: {ex}");
            }
        }
        else if (entry is MerchantRelicEntry relicEntry && relicEntry.Model != null)
        {
            itemId = relicEntry.Model.Id.ToString();
            itemType = "Relic";
        }
        else if (entry is MerchantPotionEntry potionEntry && potionEntry.Model != null)
        {
            itemId = potionEntry.Model.Id.ToString();
            itemType = "Potion";
        }
        
        if (!string.IsNullOrEmpty(itemId))
        {
            var msg = new GiftItemMessage(itemId, itemType, player.NetId, targetId, upgradeCount, misc);
            if (RunManager.Instance.NetService != null)
            {
                RunManager.Instance.NetService.SendMessage(msg);
                MainFile.Logger.Info($"Sent gift {itemId} to {targetId}");
            }
        }

        // Handle cleanup/restock
        bool shouldRestock = player.RunState.CurrentRoom is MegaCrit.Sts2.Core.Rooms.MerchantRoom && Hook.ShouldRefillMerchantEntry(player.RunState, entry, player);
        
        if (shouldRestock)
        {
            RestockAfterPurchaseMethod.Invoke(entry, new object[] { inventory });
        }
        else
        {
            ClearAfterPurchaseMethod.Invoke(entry, null);
        }

        // Force UI update to reflect the change (empty slot or restocked item)
        entry.OnMerchantInventoryUpdated();

        await Hook.AfterItemPurchased(player.RunState, player, entry, cost);
        // Do NOT call InvokePurchaseCompleted to avoid local visual effect of gaining item
        // entry.InvokePurchaseCompleted(entry); 
        
        // Play sound for gift sent
        SfxCmd.Play("event:/sfx/ui/shop_purchase"); // Standard shop sound

        return true;
    }
}
