using System.Runtime.CompilerServices;
using MegaCrit.Sts2.Core.Entities.Merchant;

namespace ShopEnhancement.Patches;

public static class GiftModeState
{
    private sealed class GiftState
    {
        public bool IsEnabled;
        public ulong TargetPlayerId;
    }

    private static readonly ConditionalWeakTable<MerchantInventory, GiftState> States = new();

    private static GiftState GetState(MerchantInventory inventory)
    {
        return States.GetOrCreateValue(inventory);
    }

    public static bool IsEnabled(MerchantInventory inventory)
    {
        return GetState(inventory).IsEnabled;
    }

    public static ulong GetTargetPlayerId(MerchantInventory inventory)
    {
        return GetState(inventory).TargetPlayerId;
    }

    public static void SetTargetPlayerId(MerchantInventory inventory, ulong id)
    {
        GetState(inventory).TargetPlayerId = id;
    }

    public static bool Toggle(MerchantInventory inventory)
    {
        var state = GetState(inventory);
        state.IsEnabled = !state.IsEnabled;
        return state.IsEnabled;
    }

    public static void Set(MerchantInventory inventory, bool isEnabled)
    {
        GetState(inventory).IsEnabled = isEnabled;
    }
}
