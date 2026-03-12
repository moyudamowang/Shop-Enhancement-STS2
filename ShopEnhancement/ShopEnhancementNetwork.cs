using System;
using System.Linq;
using Godot;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Runs;
using ShopEnhancement.Network;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Nodes.Vfx;

using System.Text.Json;
using ShopEnhancement.Config;

namespace ShopEnhancement;

public static class ShopEnhancementNetwork
{
    private static INetGameService? _registeredNetService;
    private static INetGameService? _broadcastedNetService;

    public static void Initialize()
    {
        RunManager.Instance.RunStarted += OnRunStarted;
    }

    public static void EnsureHandlersRegistered()
    {
        var netService = RunManager.Instance.NetService;
        if (netService == null || ReferenceEquals(netService, _registeredNetService))
        {
            return;
        }

        if (_registeredNetService != null)
        {
            try
            {
                _registeredNetService.UnregisterMessageHandler<GiftItemMessage>(HandleGiftItemMessage);
                _registeredNetService.UnregisterMessageHandler<SyncConfigMessage>(HandleSyncConfigMessage);
            }
            catch
            {
            }
        }

        netService.RegisterMessageHandler<GiftItemMessage>(HandleGiftItemMessage);
        netService.RegisterMessageHandler<SyncConfigMessage>(HandleSyncConfigMessage);
        _registeredNetService = netService;
        MainFile.Logger.Info("Registered GiftItemMessage and SyncConfigMessage handler.");
    }

    private static void OnRunStarted(RunState state)
    {
        EnsureHandlersRegistered();

        var netService = RunManager.Instance.NetService;
        if (netService != null && netService.Type == NetGameType.Host && !ReferenceEquals(netService, _broadcastedNetService))
        {
            BroadcastConfig(netService);
            _broadcastedNetService = netService;
        }
    }

    private static void BroadcastConfig(INetGameService netService)
    {
        try
        {
            var configData = ConfigManager.GetCurrentConfig();
            string json = JsonSerializer.Serialize(configData);
            var msg = new SyncConfigMessage { ConfigJson = json };
            netService.SendMessage(msg);
            MainFile.Logger.Info("Broadcasted ShopEnhancement config to clients.");
        }
        catch (Exception ex)
        {
            MainFile.Logger.Error($"Failed to broadcast config: {ex}");
        }
    }

    private static void HandleSyncConfigMessage(SyncConfigMessage msg, ulong senderId)
    {
        try
        {
            // Only accept from host? 
            // Assuming senderId check if needed, but usually only host sends this.
            // If we are host, ignore our own broadcast if loopback happens (though loopback usually filtered or handled)
            if (RunManager.Instance.NetService.Type == NetGameType.Host) return;

            MainFile.Logger.Info($"Received config sync from {senderId}");
            var data = JsonSerializer.Deserialize<ConfigData>(msg.ConfigJson);
            if (data != null)
            {
                ConfigManager.ApplyConfig(data);
                MainFile.Logger.Info("Applied host config.");
            }
        }
        catch (Exception ex)
        {
            MainFile.Logger.Error($"Failed to handle config sync: {ex}");
        }
    }

    private static void HandleGiftItemMessage(GiftItemMessage msg, ulong senderId)
    {
        // Verify target is us
        if (RunManager.Instance.NetService.NetId != msg.TargetId)
            return;

        // Try to get player object
        var player = GetLocalPlayer();
        if (player == null) return;

        MainFile.Logger.Info($"Received gift {msg.ItemId} ({msg.ItemType}) from {senderId}");

        // Find sender name
        string senderName = "Unknown";
        // We need to find the sender in the player list.
        // We can reuse the reflection logic or just iterate if we have the list.
        // Let's assume GetLocalPlayer() gives us a player that has access to RunState which has Players.
        
        var sender = player.RunState.Players.FirstOrDefault(p => p.NetId == senderId);
        if (sender != null) 
        {
             senderName = sender.Character.Title.GetFormattedText(); // Fallback
             senderName = $"{senderName} ({senderId})";
        }

        // Give item
        switch (msg.ItemType)
        {
            case "Card":
                GiveCard(player, msg.ItemId, msg.UpgradeCount, msg.Misc);
                break;
            case "Relic":
                GiveRelic(player, msg.ItemId);
                break;
            case "Potion":
                GivePotion(player, msg.ItemId);
                break;
        }

        // Show notification
        if (player.Creature != null)
        {
            try
            {
                var loc = new LocString("shop_enhancement", "gift_received_bubble");
                loc.Add("0", senderName);
                string text = loc.GetFormattedText();
                NSpeechBubbleVfx.Create(text, player.Creature, 3.0);
            }
            catch (Exception ex)
            {
                MainFile.Logger.Warn($"Failed to create gift speech bubble: {ex.Message}");
            }
        }
        
        SfxCmd.Play("event:/sfx/ui/reward_screen_open");
    }

    private static Player? GetLocalPlayer()
    {
        try
        {
            if (RunManager.Instance.NetService != null)
            {
                var runStateProp = typeof(RunManager).GetProperty("State", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
                var runState = runStateProp?.GetValue(RunManager.Instance) as RunState;
                
                if (runState != null)
                {
                    return runState.Players.FirstOrDefault(p => p.NetId == RunManager.Instance.NetService.NetId);
                }
            }
        }
        catch (Exception ex)
        {
            MainFile.Logger.Error($"Failed to get local player: {ex}");
        }
        return null;
    }

    private static void GiveCard(Player player, string cardId, int upgradeCount, int misc)
    {
        MainFile.Logger.Info($"GiveCard: {cardId}, upgrade: {upgradeCount}");
        var modelId = ModelId.Deserialize(cardId);
        var cardModel = ModelDb.AllCards.FirstOrDefault(c => c.Id == modelId); 
        
        if (cardModel != null)
        {
            // Try to use ICardScope.CreateCard
            if (player.RunState is ICardScope scope)
            {
                try 
                {
                    var card = scope.CreateCard(cardModel, player);
                    MainFile.Logger.Info($"Created card: {card?.Title}");
                    
                    if (card != null)
                    {
                        for (int i = 0; i < upgradeCount; i++)
                        {
                            // Use CardCmd.Upgrade(card)
                            CardCmd.Upgrade(card);
                        }
                        
                        // Try to set Misc via reflection if it exists
                        try {
                            var miscProp = card.GetType().GetProperty("Misc");
                            if (miscProp != null && miscProp.CanWrite)
                            {
                                miscProp.SetValue(card, misc);
                            }
                        } catch { /* Ignore */ }

                        MainFile.Logger.Info($"Adding card to deck...");
                        Func<Task> addCardAction = async () => {
                            try {
                                var result = await CardPileCmd.Add(card, PileType.Deck);
                                if (result.success)
                                {
                                    MainFile.Logger.Info($"Card added successfully.");
                                    CardCmd.PreviewCardPileAdd(result);
                                }
                                else
                                {
                                    MainFile.Logger.Error($"CardPileCmd.Add failed. OldPile: {result.oldPile?.Type}, Success: {result.success}");
                                }
                            } catch (Exception ex) {
                                MainFile.Logger.Error($"Failed to add card to deck: {ex}");
                            }
                        };
                        TaskHelper.RunSafely(addCardAction());
                    }
                }
                catch (Exception ex)
                {
                    MainFile.Logger.Error($"Error in GiveCard: {ex}");
                }
            }
            else
            {
                MainFile.Logger.Error("Player RunState is not ICardScope");
            }
        }
        else
        {
            MainFile.Logger.Error($"Card model not found for {cardId}");
        }
    }

    private static void GiveRelic(Player player, string relicId)
    {
        var modelId = ModelId.Deserialize(relicId);
        var relicModel = ModelDb.AllRelics.FirstOrDefault(r => r.Id == modelId);
        if (relicModel != null)
        {
            TaskHelper.RunSafely(RelicCmd.Obtain(relicModel.ToMutable(), player));
        }
    }

    private static void GivePotion(Player player, string potionId)
    {
        var modelId = ModelId.Deserialize(potionId);
        var potionModel = ModelDb.AllPotions.FirstOrDefault(p => p.Id == modelId);
        if (potionModel != null)
        {
            TaskHelper.RunSafely(PotionCmd.TryToProcure(potionModel.ToMutable(), player));
        }
    }
}
