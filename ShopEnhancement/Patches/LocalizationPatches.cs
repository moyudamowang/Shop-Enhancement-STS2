using HarmonyLib;
using MegaCrit.Sts2.Core.Localization;
using ShopEnhancement;

namespace ShopEnhancement.Patches;

[HarmonyPatch(typeof(LocManager), nameof(LocManager.Initialize))]
public static class LocalizationPatches
{
    [HarmonyPostfix]
    public static void Initialize_Postfix()
    {
        MainFile.Logger.Info("LocManager initialized. Loading ShopEnhancement localization.");
        
        // Subscribe to locale change
        LocManager.Instance.SubscribeToLocaleChange(MainFile.OnLocaleChanged);
        
        // Load initially
        MainFile.OnLocaleChanged();
    }
}
