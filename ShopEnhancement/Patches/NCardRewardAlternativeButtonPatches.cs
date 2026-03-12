using HarmonyLib;
using MegaCrit.Sts2.Core.ControllerInput;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Nodes.Screens.CardSelection;

namespace ShopEnhancement.Patches;

[HarmonyPatch(typeof(NCardRewardAlternativeButton), nameof(NCardRewardAlternativeButton.Create))]
public static class NCardRewardAlternativeButtonPatches
{
    [HarmonyPrefix]
    public static void Create_Prefix(ref string optionName, string hotkey)
    {
        if (!ShopEnhancementConfig.EnableSkipCardRewardGold) return;

        // Heuristic: Identify the "Skip" button by its hotkey (MegaInput.cancel)
        // This is safer than relying on the localized text string.
        // CardRewardAlternative sets Hotkey to MegaInput.cancel ONLY for DismissScreenAndKeepReward actions (like Skip).
        if (hotkey == MegaInput.cancel)
        {
            // Append gold amount to the label
            int gold = ShopEnhancementConfig.SkipCardRewardGoldAmount;
            if (gold > 0)
            {
                var loc = new LocString("shop_enhancement", "reward.skip_gold");
                loc.Add("0", gold);
                optionName += loc.GetFormattedText();
            }
        }
    }
}
