using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.addons.mega_text;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Gold;
using MegaCrit.Sts2.Core.Entities.Merchant;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Enchantments;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.HoverTips;
using MegaCrit.Sts2.Core.Nodes.Screens.Shops;
using MegaCrit.Sts2.Core.Platform;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using ShopEnhancement;
using ShopEnhancement.Network;

namespace ShopEnhancement.Patches;

[HarmonyPatch(typeof(MerchantCardRemovalEntry))]
public static class MerchantCardRemovalPatches
{
    internal sealed class ShopServiceState
    {
        public bool Rolled;
        public bool IsEnchantShop;
        public bool IsGiftCardShop;
        public float Roll;
        public int ShopVisitIndex;
        public int EnchantmentIndex;
        public int EnchantmentAmount;
        public int EnchantCardCount;
        public int EnchantCost;
        public ulong GiftTargetId;
        public int GiftCardCount;
        public int GiftCost;
    }

    private static readonly ConditionalWeakTable<MerchantCardRemovalEntry, StrongBox<int>> UsageCounts = new();
    private static readonly ConditionalWeakTable<MerchantInventory, ShopServiceState> ServiceStates = new();
    private static readonly Func<EnchantmentModel>[] EnchantmentFactories =
    {
        static () => ModelDb.Enchantment<Sharp>().ToMutable(),
        static () => ModelDb.Enchantment<Nimble>().ToMutable(),
        static () => ModelDb.Enchantment<Swift>().ToMutable()
    };
    private static int _shopVisitCount;

    [HarmonyPatch(nameof(MerchantCardRemovalEntry.CalcCost))]
    [HarmonyPostfix]
    public static void CalcCost_Postfix(MerchantCardRemovalEntry __instance, Player ____player, ref int ____cost)
    {
        if (TryGetCurrentInventory(out MerchantInventory inventory)
            && inventory.CardRemovalEntry == __instance)
        {
            ShopServiceState state = EnsureServiceState(inventory);
            if (state.IsEnchantShop)
            {
                ____cost = state.EnchantCost;
                return;
            }

            if (state.IsGiftCardShop)
            {
                ____cost = state.GiftCost;
                return;
            }
        }

        if (ShopEnhancementConfig.UseLinearCost)
        {
            ____cost = ShopEnhancementConfig.RemoveBaseCost + (ShopEnhancementConfig.RemoveStepCost * ____player.ExtraFields.CardShopRemovalsUsed);
        }
    }

    [HarmonyPatch(nameof(MerchantCardRemovalEntry.OnTryPurchaseWrapper))]
    [HarmonyPrefix]
    public static bool OnTryPurchaseWrapper_Prefix(
        MerchantCardRemovalEntry __instance,
        MerchantInventory? inventory,
        bool ignoreCost,
        bool cancelable,
        ref Task<bool> __result)
    {
        if (inventory == null)
        {
            return true;
        }

        ShopServiceState state = EnsureServiceState(inventory);
        if ((!state.IsEnchantShop && !state.IsGiftCardShop) || inventory.CardRemovalEntry != __instance)
        {
            return true;
        }

        __result = state.IsGiftCardShop
            ? HandleGiftCardPurchaseAsync(__instance, inventory, state, ignoreCost, cancelable)
            : HandleEnchantPurchaseAsync(__instance, inventory, state, ignoreCost, cancelable);
        return false;
    }

    [HarmonyPatch(nameof(MerchantCardRemovalEntry.SetUsed))]
    [HarmonyPrefix]
    public static bool SetUsed_Prefix(MerchantCardRemovalEntry __instance)
    {
        if (IsCurrentEntrySpecialMode(__instance))
        {
            return true;
        }

        var countBox = UsageCounts.GetOrCreateValue(__instance);
        countBox.Value++;

        int limit = ShopEnhancementConfig.RemoveLimitPerShop;
        if (limit == -1 || countBox.Value < limit)
        {
            __instance.CalcCost();
            return false;
        }

        return true;
    }

    private static async Task<bool> HandleEnchantPurchaseAsync(
        MerchantCardRemovalEntry entry,
        MerchantInventory inventory,
        ShopServiceState state,
        bool ignoreCost,
        bool cancelable)
    {
        Player player = inventory.Player;
        EnchantmentModel enchantment = ResolveEnchantment(state.EnchantmentIndex);
        List<CardModel> availableCards = PileType.Deck.GetPile(player).Cards.Where(enchantment.CanEnchant).ToList();
        int cardCount = Math.Min(state.EnchantCardCount, availableCards.Count);
        int cost = ignoreCost ? 0 : ComputeEnchantCost(cardCount);

        if (cardCount <= 0)
        {
            entry.InvokePurchaseFailed(PurchaseStatus.FailureForbidden);
            return false;
        }

        if (!ignoreCost && player.Gold < cost)
        {
            entry.InvokePurchaseFailed(PurchaseStatus.FailureGold);
            return false;
        }

        CardSelectorPrefs prefs = new(CardSelectorPrefs.EnchantSelectionPrompt, cardCount)
        {
            Cancelable = cancelable
        };

        IEnumerable<CardModel> selectedCards = await CardSelectCmd.FromDeckGeneric(
            player,
            prefs,
            card => enchantment.CanEnchant(card));

        List<CardModel> selectedList = selectedCards.Take(cardCount).ToList();
        if (selectedList.Count == 0)
        {
            return false;
        }

        if (!ignoreCost)
        {
            await PlayerCmd.LoseGold(cost, player, GoldLossType.Spent);
        }

        foreach (CardModel selectedCard in selectedList)
        {
            CardCmd.Enchant(ResolveEnchantment(state.EnchantmentIndex), selectedCard, state.EnchantmentAmount);
            var vfx = MegaCrit.Sts2.Core.Nodes.Vfx.NCardEnchantVfx.Create(selectedCard);
            if (vfx != null)
            {
                NRun.Instance?.GlobalUi.CardPreviewContainer.AddChildSafely(vfx);
            }
        }

        entry.SetUsed();
        await Hook.AfterItemPurchased(player.RunState, player, entry, cost);
        entry.InvokePurchaseCompleted(entry);
        return true;
    }

    private static async Task<bool> HandleGiftCardPurchaseAsync(
        MerchantCardRemovalEntry entry,
        MerchantInventory inventory,
        ShopServiceState state,
        bool ignoreCost,
        bool cancelable)
    {
        Player player = inventory.Player;
        Player? targetPlayer = player.RunState.Players.FirstOrDefault(p => p.NetId == state.GiftTargetId);
        if (targetPlayer == null || targetPlayer.NetId == player.NetId || RunManager.Instance.NetService == null)
        {
            entry.InvokePurchaseFailed(PurchaseStatus.FailureForbidden);
            return false;
        }

        List<CardModel> availableCards = PileType.Deck.GetPile(player).Cards.ToList();
        int cardCount = Math.Min(state.GiftCardCount, availableCards.Count);
        if (cardCount <= 0)
        {
            entry.InvokePurchaseFailed(PurchaseStatus.FailureForbidden);
            return false;
        }

        int cost = ignoreCost ? 0 : ComputeGiftServiceCost(cardCount);
        if (!ignoreCost && player.Gold < cost)
        {
            entry.InvokePurchaseFailed(PurchaseStatus.FailureGold);
            return false;
        }

        CardSelectorPrefs prefs = new(new LocString("shop_enhancement", "gift.service_select_prompt"), cardCount)
        {
            Cancelable = cancelable
        };

        IEnumerable<CardModel> selectedCards = await CardSelectCmd.FromDeckGeneric(
            player,
            prefs,
            card => card.Pile?.Type == PileType.Deck);

        List<CardModel> selectedList = selectedCards.Take(cardCount).ToList();
        if (selectedList.Count == 0)
        {
            return false;
        }

        if (!ignoreCost)
        {
            await PlayerCmd.LoseGold(cost, player, GoldLossType.Spent);
        }

        foreach (CardModel selectedCard in selectedList)
        {
            GiftItemMessage message = BuildGiftCardMessage(player, targetPlayer.NetId, selectedCard);
            RunManager.Instance.NetService.SendMessage(message);
        }

        await CardPileCmd.RemoveFromDeck(selectedList, true);
        entry.SetUsed();
        await Hook.AfterItemPurchased(player.RunState, player, entry, cost);
        entry.InvokePurchaseCompleted(entry);
        return true;
    }

    private static GiftItemMessage BuildGiftCardMessage(Player sender, ulong targetId, CardModel card)
    {
        int upgradeCount = ReadCardInt(card, "UpgradeCount", "TimesUpgraded");
        int misc = ReadCardInt(card, "Misc");
        return new GiftItemMessage(card.Id.ToString(), "Card", sender.NetId, targetId, upgradeCount, misc);
    }

    private static int ReadCardInt(CardModel card, params string[] propertyNames)
    {
        foreach (string propertyName in propertyNames)
        {
            var prop = card.GetType().GetProperty(propertyName);
            if (prop?.PropertyType == typeof(int))
            {
                return (int)(prop.GetValue(card) ?? 0);
            }
        }

        return 0;
    }

    private static bool IsCurrentEntrySpecialMode(MerchantCardRemovalEntry entry)
    {
        if (!TryGetCurrentInventory(out MerchantInventory inventory) || inventory.CardRemovalEntry != entry)
        {
            return false;
        }

        ShopServiceState state = EnsureServiceState(inventory);
        return state.IsEnchantShop || state.IsGiftCardShop;
    }

    private static bool TryGetCurrentInventory(out MerchantInventory inventory)
    {
        inventory = null!;
        var merchantRoom = NRun.Instance?.MerchantRoom;
        if (merchantRoom?.Room?.Inventory == null)
        {
            return false;
        }

        inventory = merchantRoom.Room.Inventory;
        return true;
    }

    internal static ShopServiceState EnsureServiceState(MerchantInventory inventory)
    {
        ShopServiceState state = ServiceStates.GetOrCreateValue(inventory);
        if (state.Rolled)
        {
            return state;
        }

        state.Rolled = true;
        state.ShopVisitIndex = Math.Max(1, _shopVisitCount);
        state.IsEnchantShop = false;
        state.IsGiftCardShop = false;

        if (!ShopEnhancementConfig.EnableRemovalEnchantRandom)
        {
            return state;
        }

        if (state.ShopVisitIndex < Math.Max(1, ShopEnhancementConfig.EnchantStartShopVisit))
        {
            return state;
        }

        string seed = inventory.Player.RunState.Rng.StringSeed;
        state.Roll = ComputeRoll(seed, state.ShopVisitIndex, "shop-service-mode");
        float chance = Mathf.Clamp(ShopEnhancementConfig.EnchantReplaceChance, 0f, 1f);
        if (state.Roll >= chance)
        {
            return state;
        }

        bool canEnchant = ShopEnhancementConfig.EnableEnchantService;
        bool canGift = ShopEnhancementConfig.EnableGiftMode && ShopEnhancementConfig.EnableRandomTeammateGiftService;

        if (!canEnchant && !canGift)
        {
            return state;
        }

        if (canEnchant && canGift)
        {
            float serviceTypeRoll = ComputeDerivedRoll(state.Roll, "shop-service-type");
            if (serviceTypeRoll < 0.5f)
            {
                if (TryBuildGiftServiceState(inventory, state)) return state;
                if (TryBuildEnchantServiceState(inventory, state)) return state;
            }
            else
            {
                if (TryBuildEnchantServiceState(inventory, state)) return state;
                if (TryBuildGiftServiceState(inventory, state)) return state;
            }
        }
        else if (canGift)
        {
            if (TryBuildGiftServiceState(inventory, state)) return state;
        }
        else if (canEnchant)
        {
            if (TryBuildEnchantServiceState(inventory, state)) return state;
        }

        state.IsEnchantShop = false;
        state.IsGiftCardShop = false;
        return state;
    }

    private static bool TryBuildEnchantServiceState(MerchantInventory inventory, ShopServiceState state)
    {
        state.IsEnchantShop = true;
        state.IsGiftCardShop = false;
        float enchantRoll = ComputeDerivedRoll(state.Roll, "shop-enchant-type");
        state.EnchantmentIndex = Math.Clamp((int)(enchantRoll * EnchantmentFactories.Length), 0, EnchantmentFactories.Length - 1);
        state.EnchantmentAmount = RollIntInRange(ShopEnhancementConfig.EnchantAmountRange, ComputeDerivedRoll(state.Roll, "shop-enchant-amount"));
        int rolledCardCount = RollIntInRange(ShopEnhancementConfig.EnchantCardCountRange, ComputeDerivedRoll(state.Roll, "shop-enchant-count"));
        int maxEnchantableCount = CountEnchantableCards(inventory, state.EnchantmentIndex);
        if (maxEnchantableCount <= 0)
        {
            state.IsEnchantShop = false;
            state.EnchantCardCount = 0;
            state.EnchantCost = 0;
            return false;
        }

        state.EnchantCardCount = Math.Max(1, Math.Min(rolledCardCount, maxEnchantableCount));
        state.EnchantCost = ComputeEnchantCost(state.EnchantCardCount);
        return true;
    }

    private static bool TryBuildGiftServiceState(MerchantInventory inventory, ShopServiceState state)
    {
        if (!ShopEnhancementConfig.EnableGiftMode || !ShopEnhancementConfig.EnableRandomTeammateGiftService)
        {
            return false;
        }

        List<Player> targets = GetAvailableGiftTargets(inventory.Player);
        if (targets.Count == 0)
        {
            return false;
        }

        int maxGiftableCount = PileType.Deck.GetPile(inventory.Player).Cards.Count;
        if (maxGiftableCount <= 0)
        {
            return false;
        }

        state.IsEnchantShop = false;
        state.IsGiftCardShop = true;
        float targetRoll = ComputeDerivedRoll(state.Roll, "shop-gift-target");
        int targetIndex = Math.Clamp((int)(targetRoll * targets.Count), 0, targets.Count - 1);
        state.GiftTargetId = targets[targetIndex].NetId;
        int rolledCardCount = RollIntInRange(ShopEnhancementConfig.GiftServiceCardCountRange, ComputeDerivedRoll(state.Roll, "shop-gift-count"));
        state.GiftCardCount = Math.Max(1, Math.Min(rolledCardCount, maxGiftableCount));
        state.GiftCost = ComputeGiftServiceCost(state.GiftCardCount);
        return true;
    }

    internal static string GetCurrentEnchantName(MerchantInventory inventory)
    {
        ShopServiceState state = EnsureServiceState(inventory);
        if (!state.IsEnchantShop)
        {
            return string.Empty;
        }

        return ResolveEnchantment(state.EnchantmentIndex).Title.GetFormattedText();
    }

    internal static string GetCurrentGiftTargetName(MerchantInventory inventory)
    {
        ShopServiceState state = EnsureServiceState(inventory);
        if (!state.IsGiftCardShop || state.GiftTargetId == 0)
        {
            return string.Empty;
        }

        Player? player = inventory.Player.RunState.Players.FirstOrDefault(p => p.NetId == state.GiftTargetId);
        if (player == null)
        {
            return state.GiftTargetId.ToString();
        }

        string platformName = PlatformUtil.GetPlayerName(RunManager.Instance.NetService.Platform, state.GiftTargetId);
        if (!string.IsNullOrWhiteSpace(platformName))
        {
            return platformName;
        }

        return player.Character.Title.GetFormattedText();
    }

    private static EnchantmentModel ResolveEnchantment(int index)
    {
        int safeIndex = Math.Clamp(index, 0, EnchantmentFactories.Length - 1);
        return EnchantmentFactories[safeIndex]();
    }

    private static int CountEnchantableCards(MerchantInventory inventory, int enchantmentIndex)
    {
        EnchantmentModel enchantment = ResolveEnchantment(enchantmentIndex);
        return PileType.Deck.GetPile(inventory.Player).Cards.Count(enchantment.CanEnchant);
    }

    private static List<Player> GetAvailableGiftTargets(Player sender)
    {
        IReadOnlyCollection<ulong>? connectedIds = RunManager.Instance.RunLobby?.ConnectedPlayerIds;
        return sender.RunState.Players
            .Where(p => p.NetId != sender.NetId)
            .Where(p => connectedIds == null || connectedIds.Count == 0 || connectedIds.Contains(p.NetId))
            .OrderBy(p => p.NetId)
            .ToList();
    }

    private static int RollIntInRange(Vector2I range, float roll)
    {
        int min = Math.Max(1, Math.Min(range.X, range.Y));
        int max = Math.Max(min, Math.Max(range.X, range.Y));
        int span = max - min + 1;
        int offset = Math.Min(span - 1, (int)(Mathf.Clamp(roll, 0f, 0.999999f) * span));
        return min + offset;
    }

    private static int ComputeEnchantCost(int cardCount)
    {
        int baseCost = Math.Max(0, ShopEnhancementConfig.EnchantCost);
        return baseCost * Math.Max(1, cardCount);
    }

    private static int ComputeGiftServiceCost(int cardCount)
    {
        int baseCost = Math.Max(0, ShopEnhancementConfig.GiftServiceBaseCost);
        int stepCost = Math.Max(0, ShopEnhancementConfig.GiftServiceStepCost);
        int safeCount = Math.Max(1, cardCount);
        return baseCost + (safeCount - 1) * stepCost;
    }

    private static float ComputeRoll(string seed, int shopVisitIndex, string salt)
    {
        int hash = StringHelper.GetDeterministicHashCode($"{seed}|{shopVisitIndex}|{salt}");
        uint unsigned = unchecked((uint)hash);
        return (unsigned % 1000000) / 1000000f;
    }

    private static float ComputeDerivedRoll(float sourceRoll, string salt)
    {
        int hash = StringHelper.GetDeterministicHashCode($"{sourceRoll:F6}|{salt}");
        uint unsigned = unchecked((uint)hash);
        return (unsigned % 1000000) / 1000000f;
    }

    internal static void IncrementVisit()
    {
        _shopVisitCount++;
    }

    internal static void ResetVisit()
    {
        _shopVisitCount = 0;
    }
}

[HarmonyPatch(typeof(MerchantRoom), nameof(MerchantRoom.Enter))]
public static class MerchantRoomVisitCounterPatches
{
    [HarmonyPostfix]
    public static void Enter_Postfix()
    {
        MerchantCardRemovalPatches.IncrementVisit();
    }
}

[HarmonyPatch(typeof(RunManager), nameof(RunManager.CleanUp))]
public static class MerchantRoomVisitCounterResetPatches
{
    [HarmonyPrefix]
    public static void Prefix()
    {
        MerchantCardRemovalPatches.ResetVisit();
    }
}

[HarmonyPatch(typeof(NMerchantInventory), nameof(NMerchantInventory.Initialize))]
public static class MerchantInventoryServiceStateInitPatches
{
    [HarmonyPostfix]
    public static void Initialize_Postfix(MerchantInventory inventory)
    {
        MerchantCardRemovalPatches.EnsureServiceState(inventory);
        inventory.CardRemovalEntry?.CalcCost();
    }
}

[HarmonyPatch(typeof(NMerchantCardRemoval), "CreateHoverTip")]
public static class MerchantCardRemovalHoverTipPatches
{
    [HarmonyPrefix]
    public static bool CreateHoverTip_Prefix(NMerchantCardRemoval __instance)
    {
        if (NRun.Instance?.MerchantRoom?.Inventory == null)
        {
            return true;
        }

        MerchantInventory inventory = NRun.Instance.MerchantRoom.Room.Inventory;
        var state = MerchantCardRemovalPatches.EnsureServiceState(inventory);
        if (!state.IsEnchantShop && !state.IsGiftCardShop)
        {
            return true;
        }

        LocString title;
        LocString description;
        if (state.IsGiftCardShop)
        {
            title = new LocString("shop_enhancement", "gift.service_title");
            description = new LocString("shop_enhancement", "gift.service_desc");
            description.Add("0", MerchantCardRemovalPatches.GetCurrentGiftTargetName(inventory));
            description.Add("1", state.GiftCardCount);
            description.Add("2", state.GiftCost);
        }
        else
        {
            string enchantName = MerchantCardRemovalPatches.GetCurrentEnchantName(inventory);
            title = new LocString("shop_enhancement", "enchant.service_title");
            description = new LocString("shop_enhancement", "enchant.service_desc");
            description.Add("0", enchantName);
            description.Add("1", state.EnchantmentAmount);
            description.Add("2", state.EnchantCardCount);
            description.Add("3", state.EnchantCost);
        }

        NHoverTipSet tipSet = NHoverTipSet.CreateAndShow(__instance, new HoverTip(title, description));
        tipSet.GlobalPosition = __instance.GlobalPosition;
        if (__instance.GlobalPosition.X > __instance.GetViewport().GetVisibleRect().Size.X * 0.5f)
        {
            tipSet.SetAlignment(__instance, HoverTipAlignment.Left);
            tipSet.GlobalPosition -= __instance.Size * 0.5f * __instance.Scale;
        }
        else
        {
            tipSet.GlobalPosition += Vector2.Right * __instance.Size.X * 0.5f * __instance.Scale + Vector2.Up * __instance.Size.Y * 0.5f * __instance.Scale;
        }

        return false;
    }
}

[HarmonyPatch(typeof(NMerchantCardRemoval), "UpdateVisual")]
public static class MerchantCardRemovalCostLabelPatches
{
    [HarmonyPostfix]
    public static void UpdateVisual_Postfix(NMerchantCardRemoval __instance)
    {
        if (NRun.Instance?.MerchantRoom?.Room?.Inventory == null)
        {
            return;
        }

        MerchantInventory inventory = NRun.Instance.MerchantRoom.Room.Inventory;
        var state = MerchantCardRemovalPatches.EnsureServiceState(inventory);
        if ((!state.IsEnchantShop && !state.IsGiftCardShop) || inventory.CardRemovalEntry?.Used != false)
        {
            return;
        }

        MegaLabel? costLabel = __instance.GetNodeOrNull<MegaLabel>("%CostLabel");
        Control? costContainer = __instance.GetNodeOrNull<Control>("Cost");
        if (costLabel == null || costContainer == null)
        {
            return;
        }

        int cost = state.IsGiftCardShop ? state.GiftCost : state.EnchantCost;
        string pricePrefix = new LocString("shop_enhancement", state.IsGiftCardShop ? "gift.price_prefix" : "enchant.price_prefix").GetFormattedText();
        costLabel.Visible = true;
        costLabel.SetTextAutoSize($"{pricePrefix}{cost}");
        costLabel.Modulate = inventory.Player.Gold >= cost ? new Color(0.55f, 0.84f, 1f) : StsColors.red;
        costContainer.Visible = true;
    }
}
