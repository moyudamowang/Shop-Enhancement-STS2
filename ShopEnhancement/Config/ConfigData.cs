using System.Text.Json.Serialization;
using Godot;

namespace ShopEnhancement.Config;

public class ConfigData
{
    public int RemoveBaseCost { get; set; } = 50;
    public int RemoveStepCost { get; set; } = 25;
    public bool UseLinearCost { get; set; } = true;
    public int RemoveLimitPerShop { get; set; } = 3;

    public int RefreshCost { get; set; } = 40;
    public int RefreshLimitPerShop { get; set; } = 3;

    public bool EnableNoPurchaseReward { get; set; } = true;
    public int NoPurchaseRewardGold { get; set; } = 15;

    public bool EnableSkipCardRewardGold { get; set; } = true;
    public int SkipCardRewardGoldAmount { get; set; } = 15;

    public bool EnableCrossClassCards { get; set; } = true;
    public float CrossClassCardChance { get; set; } = 0.2f;

    public bool EnableUnlockAll { get; set; } = false;

    public bool EnableSellMode { get; set; } = true;
    public float SellRelicPriceRatio { get; set; } = 0.35f;
    public float SellPotionPriceRatio { get; set; } = 0.25f;
    public int SellRelicMinGold { get; set; } = 30;
    public int SellPotionMinGold { get; set; } = 15;
    public bool RequireSellDoubleClick { get; set; } = true;
    public int SellConfirmWindowMs { get; set; } = 1800;

    public int SellAncientRelicBasePrice { get; set; } = 750;
    public int SellStarterRelicBasePrice { get; set; } = 300;
    public int SellEventRelicBasePrice { get; set; } = 200;

    public bool EnableGiftMode { get; set; } = true;
    public bool EnableRemovalEnchantRandom { get; set; } = true;
    public bool EnableEnchantService { get; set; } = true;
    public int EnchantStartShopVisit { get; set; } = 5;
    public float EnchantReplaceChance { get; set; } = 0.35f;
    public int EnchantCost { get; set; } = 90;
    public Vector2I EnchantAmountRange { get; set; } = new Vector2I(1, 1);
    public Vector2I EnchantCardCountRange { get; set; } = new Vector2I(1, 1);
    public bool EnableRandomTeammateGiftService { get; set; } = true;
    public Vector2I GiftServiceCardCountRange { get; set; } = new Vector2I(1, 2);
    public int GiftServiceBaseCost { get; set; } = 70;
    public int GiftServiceStepCost { get; set; } = 40;
}
