using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using Godot;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Timeline;
using MegaCrit.Sts2.Core.Timeline.Epochs;

namespace ShopEnhancement.Patches;

public static class SaveUnlockPatches
{
    public static bool TryApplyUnlockAll()
    {
        if (!ShopEnhancementConfig.EnableUnlockAll) return false;

        try
        {
            UnlockAllInSave();
            return true;
        }
        catch (Exception ex)
        {
            GD.PrintErr($"[ShopEnhancement] Failed to unlock items in save: {ex}");
            return false;
        }
    }

    private static void UnlockAllInSave()
    {
        var progress = SaveManager.Instance.Progress;
        if (progress == null) return;

        bool changed = false;

        // 1. Unlock all Epochs (Story Progress) including Character Unlocks
        // This unlocks content gated by story progression
        var epochsField = AccessTools.Field(typeof(ProgressState), "_epochs");
        if (epochsField != null)
        {
            var epochsList = epochsField.GetValue(progress) as List<SerializableEpoch>;
            if (epochsList != null)
            {
                // Get existing IDs for faster lookup
                var existingEpochIds = new HashSet<string>(epochsList.Select(e => e.Id));

                // Check all possible epochs
                foreach (var epochId in EpochModel.AllEpochIds)
                {
                    if (!existingEpochIds.Contains(epochId))
                    {
                        // Add missing epoch as Revealed
                        epochsList.Add(new SerializableEpoch(epochId, EpochState.Revealed));
                        changed = true;
                    }
                }

                // Ensure all existing epochs are Revealed
                foreach (var epoch in epochsList)
                {
                    if (epoch.State != EpochState.Revealed)
                    {
                        epoch.State = EpochState.Revealed;
                        changed = true;
                    }
                }
            }
        }

        // 2. Mark all Cards as Discovered
        // This populates the Compendium and ensures they are seen
        var discoveredCardsField = AccessTools.Field(typeof(ProgressState), "_discoveredCards");
        if (discoveredCardsField != null)
        {
            var discoveredCards = discoveredCardsField.GetValue(progress) as HashSet<ModelId>;
            if (discoveredCards != null)
            {
                foreach (var card in ModelDb.AllCards)
                {
                    if (discoveredCards.Add(card.Id))
                    {
                        changed = true;
                    }
                }
            }
        }

        // 3. Mark all Relics as Discovered
        var discoveredRelicsField = AccessTools.Field(typeof(ProgressState), "_discoveredRelics");
        if (discoveredRelicsField != null)
        {
            var discoveredRelics = discoveredRelicsField.GetValue(progress) as HashSet<ModelId>;
            if (discoveredRelics != null)
            {
                foreach (var relic in ModelDb.AllRelics)
                {
                    if (discoveredRelics.Add(relic.Id))
                    {
                        changed = true;
                    }
                }
            }
        }
        
        // 4. Mark all Potions as Discovered (Optional but good for completeness)
        var discoveredPotionsField = AccessTools.Field(typeof(ProgressState), "_discoveredPotions");
        if (discoveredPotionsField != null)
        {
            var discoveredPotions = discoveredPotionsField.GetValue(progress) as HashSet<ModelId>;
            if (discoveredPotions != null)
            {
                foreach (var potion in ModelDb.AllPotions)
                {
                    if (discoveredPotions.Add(potion.Id))
                    {
                        changed = true;
                    }
                }
            }
        }

        if (changed)
        {
            GD.Print("[ShopEnhancement] Unlocked all items in save file.");
            SaveManager.Instance.SaveProgressFile();
        }
    }
}
