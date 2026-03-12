using System.Runtime.CompilerServices;
using MegaCrit.Sts2.Core.Nodes.Screens.Shops;

namespace ShopEnhancement.Patches;

public static class SellModeState
{
    private sealed class SellState
    {
        public bool IsEnabled;
    }

    private static readonly ConditionalWeakTable<NMerchantInventory, SellState> States = new();

    public static bool IsEnabled(NMerchantInventory inventory)
    {
        return States.GetOrCreateValue(inventory).IsEnabled;
    }

    public static bool Toggle(NMerchantInventory inventory)
    {
        var state = States.GetOrCreateValue(inventory);
        state.IsEnabled = !state.IsEnabled;
        return state.IsEnabled;
    }

    public static void Set(NMerchantInventory inventory, bool isEnabled)
    {
        States.GetOrCreateValue(inventory).IsEnabled = isEnabled;
    }
}
