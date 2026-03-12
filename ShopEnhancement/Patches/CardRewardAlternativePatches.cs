using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.ControllerInput;
using MegaCrit.Sts2.Core.Entities.CardRewardAlternatives;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Rewards;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Rewards;

namespace ShopEnhancement.Patches;

[HarmonyPatch(typeof(CardRewardAlternative), nameof(CardRewardAlternative.Generate))]
public static class CardRewardAlternativePatches
{
    [HarmonyPostfix]
    public static void Generate_Postfix(CardReward cardReward, ref IReadOnlyList<CardRewardAlternative> __result)
    {
        if (!ShopEnhancementConfig.EnableSkipCardRewardGold) return;

        // Convert to list to modify
        var list = __result.ToList();
        var skipOption = list.FirstOrDefault(x => x.OptionId == "Skip");

        if (skipOption != null)
        {
            // Create a new option with the reward logic
            // We use the same OptionId "Skip" to keep compatibility, but we intercept the OnSelect
            var newSkipOption = new CardRewardAlternative(
                "Skip", // Keep ID as "Skip"
                () => {
                    // Execute our reward logic
                    return TaskHelper.RunSafely(GiveSkipReward(cardReward.Player));
                },
                PostAlternateCardRewardAction.DismissScreenAndRemoveReward // Remove reward to prevent farming
            );

            // Force the hotkey to be "Cancel" (Esc/B) so that:
            // 1. It matches the original Skip button behavior (users expect Esc to skip)
            // 2. Our NCardRewardAlternativeButtonPatches can recognize it and add the gold hint
            AccessTools.Field(typeof(CardRewardAlternative), "<Hotkey>k__BackingField").SetValue(newSkipOption, (string)MegaInput.cancel);
            
            // Replace the old option
            int index = list.IndexOf(skipOption);
            list[index] = newSkipOption;
            
            // Assign back to result
            __result = list;
        }
    }

    private static async Task GiveSkipReward(Player player)
    {
        int gold = ShopEnhancementConfig.SkipCardRewardGoldAmount;
        if (gold > 0)
        {
            await PlayerCmd.GainGold(gold, player);
        }
    }
}
