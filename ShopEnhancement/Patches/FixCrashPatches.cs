using System;
using System.Reflection;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Multiplayer;
using MegaCrit.Sts2.Core.Multiplayer.Game.PeerInput;
using MegaCrit.Sts2.Core.Nodes.Screens;
using MegaCrit.Sts2.Core.Nodes.Screens.CharacterSelect;
using MegaCrit.Sts2.Core.Nodes.Screens.Overlays;
using MegaCrit.Sts2.Core.Nodes.Screens.Shops;
using MegaCrit.Sts2.Core.Runs;

namespace ShopEnhancement.Patches;

[HarmonyPatch(typeof(NMerchantCard), "UpdateVisual")]
public static class NMerchantCardFix
{
    [HarmonyPrefix]
    [HarmonyPriority(Priority.High)]
    public static bool Prefix(NMerchantCard __instance)
    {
        if (!GodotObject.IsInstanceValid(__instance))
        {
            return false;
        }
        return true;
    }

    [HarmonyFinalizer]
    public static Exception? Finalizer(Exception? __exception)
    {
        return MerchantVisualFinalizer.Handle(__exception);
    }
}

[HarmonyPatch(typeof(NMerchantSlot), "UpdateVisual")]
public static class NMerchantSlotFix
{
    [HarmonyPrefix]
    [HarmonyPriority(Priority.High)]
    public static bool Prefix(NMerchantSlot __instance)
    {
        return GodotObject.IsInstanceValid(__instance) && __instance.IsInsideTree();
    }

    [HarmonyFinalizer]
    public static Exception? Finalizer(Exception? __exception)
    {
        return MerchantVisualFinalizer.Handle(__exception);
    }
}

[HarmonyPatch(typeof(NMerchantRelic), "UpdateVisual")]
public static class NMerchantRelicFix
{
    [HarmonyPrefix]
    [HarmonyPriority(Priority.High)]
    public static bool Prefix(NMerchantRelic __instance)
    {
        return GodotObject.IsInstanceValid(__instance) && __instance.IsInsideTree();
    }

    [HarmonyFinalizer]
    public static Exception? Finalizer(Exception? __exception)
    {
        return MerchantVisualFinalizer.Handle(__exception);
    }
}

[HarmonyPatch(typeof(NMerchantPotion), "UpdateVisual")]
public static class NMerchantPotionFix
{
    [HarmonyPrefix]
    [HarmonyPriority(Priority.High)]
    public static bool Prefix(NMerchantPotion __instance)
    {
        return GodotObject.IsInstanceValid(__instance) && __instance.IsInsideTree();
    }

    [HarmonyFinalizer]
    public static Exception? Finalizer(Exception? __exception)
    {
        return MerchantVisualFinalizer.Handle(__exception);
    }
}

[HarmonyPatch(typeof(NMerchantCardRemoval), "UpdateVisual")]
public static class NMerchantCardRemovalFix
{
    [HarmonyPrefix]
    [HarmonyPriority(Priority.High)]
    public static bool Prefix(NMerchantCardRemoval __instance)
    {
        return GodotObject.IsInstanceValid(__instance) && __instance.IsInsideTree();
    }

    [HarmonyFinalizer]
    public static Exception? Finalizer(Exception? __exception)
    {
        return MerchantVisualFinalizer.Handle(__exception);
    }
}

public static class MerchantVisualFinalizer
{
    public static Exception? Handle(Exception? exception)
    {
        if (exception is ObjectDisposedException)
        {
            return null;
        }

        return exception;
    }
}

[HarmonyPatch(typeof(ScreenStateTracker), "OnOverlayStackChanged")]
public static class ScreenStateTrackerFix
{
    private static readonly FieldInfo ConnectedRewardsScreenField = AccessTools.Field(typeof(ScreenStateTracker), "_connectedRewardsScreen");
    private static readonly FieldInfo OverlayScreenField = AccessTools.Field(typeof(ScreenStateTracker), "_overlayScreen");
    private static readonly MethodInfo SyncLocalScreenMethod = AccessTools.Method(typeof(ScreenStateTracker), "SyncLocalScreen");

    [HarmonyPrefix]
    public static bool Prefix(ScreenStateTracker __instance)
    {
        if (RunManager.Instance.IsSinglePlayerOrFakeMultiplayer)
        {
            return false;
        }

        IOverlayScreen? overlayScreen = NOverlayStack.Instance?.Peek();
        if (overlayScreen is NRewardsScreen nRewardsScreen)
        {
            var callable = CreateSyncLocalScreenCallable(__instance);
            if (!nRewardsScreen.IsConnected(NRewardsScreen.SignalName.Completed, callable))
            {
                nRewardsScreen.Connect(NRewardsScreen.SignalName.Completed, callable);
            }

            ConnectedRewardsScreenField.SetValue(__instance, nRewardsScreen);
        }
        else
        {
            ConnectedRewardsScreenField.SetValue(__instance, null);
        }

        OverlayScreenField.SetValue(__instance, overlayScreen?.ScreenType ?? NetScreenType.None);
        SyncLocalScreenMethod.Invoke(__instance, null);
        return false;
    }

    private static Callable CreateSyncLocalScreenCallable(ScreenStateTracker tracker)
    {
        var action = (Action)Delegate.CreateDelegate(typeof(Action), tracker, SyncLocalScreenMethod);
        return Callable.From(action);
    }
}

[HarmonyPatch(typeof(NCharacterSelectScreen), nameof(NCharacterSelectScreen._Process))]
public static class CharacterSelectNetworkBootstrapPatch
{
    [HarmonyPostfix]
    public static void Postfix()
    {
        ShopEnhancementNetwork.EnsureHandlersRegistered();
    }
}
