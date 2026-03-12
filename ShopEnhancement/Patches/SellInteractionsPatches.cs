using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Potions;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.Potions;
using MegaCrit.Sts2.Core.Nodes.Relics;
using MegaCrit.Sts2.Core.Nodes.Screens.ScreenContext;
using MegaCrit.Sts2.Core.Nodes.Screens.Shops;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using MegaCrit.Sts2.Core.Runs;

using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Nodes.Multiplayer;

namespace ShopEnhancement.Patches;

[HarmonyPatch]
public static partial class SellInteractionsPatches
{
    [HarmonyPatch(typeof(NRelicInventoryHolder), nameof(NRelicInventoryHolder._Ready))]
    [HarmonyPostfix]
    public static void RelicHolderReady_Postfix(NRelicInventoryHolder __instance)
    {
        __instance.Connect(NClickableControl.SignalName.MouseReleased, Callable.From<InputEvent>(inputEvent =>
        {
            if (inputEvent is InputEventMouseButton { ButtonIndex: MouseButton.Right } mouse && mouse.IsReleased())
            {
                TaskHelper.RunSafely(TrySellRelic(__instance));
            }
        }));
    }

    [HarmonyPatch(typeof(NPotionHolder), nameof(NPotionHolder._Ready))]
    [HarmonyPostfix]
    public static void PotionHolderReady_Postfix(NPotionHolder __instance)
    {
        __instance.Connect(NClickableControl.SignalName.MouseReleased, Callable.From<InputEvent>(inputEvent =>
        {
            if (inputEvent is InputEventMouseButton { ButtonIndex: MouseButton.Right } mouse && mouse.IsReleased())
            {
                TaskHelper.RunSafely(TrySellPotion(__instance));
            }
        }));
    }

    private static async Task TrySellRelic(NRelicInventoryHolder holder)
    {
        if (!ShopEnhancementConfig.EnableSellMode)
            return;
        if (!TryGetCurrentInventory(out var inventory))
            return;
        if (!SellModeState.IsEnabled(inventory))
            return;

        var relic = holder.Relic?.Model;
        if (relic == null)
            return;

        int price = CalculateRelicPrice(relic);
        string relicName = relic.Title.GetFormattedText();
        
        var msgLoc = new LocString("shop_enhancement", "sell.relic_confirm");
        msgLoc.Add("0", relicName);
        msgLoc.Add("1", price);
        
        if (!await ConfirmSell(msgLoc.GetFormattedText()))
            return;

        await RelicCmd.Remove(relic);
        await PlayerCmd.GainGold(price, relic.Owner);
        RunManager.Instance.RewardSynchronizer.SyncLocalObtainedGold(price);
        ShowSellResult(price);
    }

    private static async Task TrySellPotion(NPotionHolder holder)
    {
        if (!ShopEnhancementConfig.EnableSellMode)
            return;
        if (!TryGetCurrentInventory(out var inventory))
            return;
        if (!SellModeState.IsEnabled(inventory))
            return;

        var potion = holder.Potion?.Model;
        if (potion == null)
            return;

        int price = CalculatePotionPrice(potion);
        string potionName = potion.Title.GetFormattedText();
        
        var msgLoc = new LocString("shop_enhancement", "sell.potion_confirm");
        msgLoc.Add("0", potionName);
        msgLoc.Add("1", price);

        if (!await ConfirmSell(msgLoc.GetFormattedText()))
            return;

        await PotionCmd.Discard(potion);
        await PlayerCmd.GainGold(price, potion.Owner);
        RunManager.Instance.RewardSynchronizer.SyncLocalObtainedGold(price);
        ShowSellResult(price);
    }

    private static bool TryGetCurrentInventory(out NMerchantInventory inventory)
    {
        inventory = null!;
        if (NRun.Instance?.MerchantRoom?.Inventory == null)
            return false;
        if (ActiveScreenContext.Instance.GetCurrentScreen() is not NMerchantInventory current)
            return false;
        inventory = current;
        return true;
    }

    private static async Task<bool> ConfirmSell(string message)
    {
        if (NModalContainer.Instance == null || NModalContainer.Instance.OpenModal != null)
            return false;

        var popup = NGenericPopup.Create();
        if (popup == null)
            return false;

        NModalContainer.Instance.Add(popup);

        var confirmLoc = new LocString("main_menu_ui", "GENERIC_POPUP.confirm");
        var cancelLoc = new LocString("main_menu_ui", "GENERIC_POPUP.cancel");
        
        // 使用 dummy LocString，稍后通过反射覆盖文本
        var task = popup.WaitForConfirmation(confirmLoc, confirmLoc, cancelLoc, confirmLoc);

        var verticalPopupField = AccessTools.Field(typeof(NGenericPopup), "_verticalPopup");
        if (verticalPopupField?.GetValue(popup) is NVerticalPopup verticalPopup)
        {
             var title = new LocString("shop_enhancement", "sell.confirm_title");
             verticalPopup.SetText(title.GetFormattedText(), message);
        }

        return await task;
    }

    private static void ShowSellResult(int price)
    {
        var loc = new LocString("shop_enhancement", "sell.success");
        loc.Add("0", price);
        var tip = NFullscreenTextVfx.Create(loc.GetFormattedText());
        if (tip != null && NGame.Instance != null)
        {
            NGame.Instance.AddChildSafely(tip);
        }
    }

    private static int CalculateRelicPrice(RelicModel relic)
    {
        int basePrice = relic.MerchantCost;
        if (relic.Rarity == RelicRarity.Ancient) basePrice = ShopEnhancementConfig.SellAncientRelicBasePrice;
        else if (relic.Rarity == RelicRarity.Starter) basePrice = ShopEnhancementConfig.SellStarterRelicBasePrice;
        else if (relic.Rarity == RelicRarity.Event) basePrice = ShopEnhancementConfig.SellEventRelicBasePrice;

        int value = (int)Math.Floor(basePrice * ShopEnhancementConfig.SellRelicPriceRatio);
        return Math.Max(value, ShopEnhancementConfig.SellRelicMinGold);
    }

    private static int CalculatePotionPrice(PotionModel potion)
    {
        int baseCost = potion.Rarity switch
        {
            PotionRarity.Rare => 100,
            PotionRarity.Uncommon => 75,
            _ => 50,
        };
        int value = (int)Math.Floor(baseCost * ShopEnhancementConfig.SellPotionPriceRatio);
        return Math.Max(value, ShopEnhancementConfig.SellPotionMinGold);
    }
}
