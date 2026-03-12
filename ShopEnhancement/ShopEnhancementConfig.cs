using Godot;

namespace ShopEnhancement;

public static class ShopEnhancementConfig
{
    // Requirement 1: Modify card removal cost
    public static int RemoveBaseCost { get; set; } = 50; // Base cost for the first removal. 50 is cheaper than vanilla (75) to encourage deck thinning, but not free.
    public static int RemoveStepCost { get; set; } = 25; // Increase per removal. Standard scaling.
    public static bool UseLinearCost { get; set; } = true; // If false, use vanilla formula (75 + 25 * count)

    // Requirement 2: Modify card removal limit
    public static int RemoveLimitPerShop { get; set; } = 3; // 3 removals allow for aggressive thinning if you have the gold (50+75+100=225g).

    // Requirement 3: Refresh shop
    public static int RefreshCost { get; set; } = 40; // 10 was too cheap. 40 makes it a tactical decision.
    public static int RefreshLimitPerShop { get; set; } = 3; // Limit to 3 to prevent infinite digging/breaking the game loop.

    // Requirement 4: No Purchase Reward
    public static bool EnableNoPurchaseReward { get; set; } = true;
    public static int NoPurchaseRewardGold { get; set; } = 15; // 25 was a bit high. 15 is a nice consolation for a bad shop.

    // Requirement 5: Skip Card Reward Gold
    public static bool EnableSkipCardRewardGold { get; set; } = true;
    public static int SkipCardRewardGoldAmount { get; set; } = 15; // 15g is a fair trade for skipping a card power spike.

    // Requirement 6: Cross Class Cards
    public static bool EnableCrossClassCards { get; set; } = true;
    public static float CrossClassCardChance { get; set; } = 0.2f; // 20% chance per card. 100% (1f) is too chaotic. 20% adds spice without diluting class identity.

    // Requirement 7: Unlock All Cards and Relics
    public static bool EnableUnlockAll { get; set; } = false;

    public static bool EnableSellMode { get; set; } = true;
    public static float SellRelicPriceRatio { get; set; } = 0.35f; // 下调至 35%，防止无脑卖遗物，强调决策成本
    public static float SellPotionPriceRatio { get; set; } = 0.25f; // 大幅下调至 25%，药水是消耗品，避免变成“炼金刷钱”流
    public static int SellRelicMinGold { get; set; } = 30; // 略微提升保底，蚊子腿也是肉
    public static int SellPotionMinGold { get; set; } = 15;
    public static bool RequireSellDoubleClick { get; set; } = true;
    public static int SellConfirmWindowMs { get; set; } = 1800;

    // Requirement 8: Custom base prices for special relics (Original game uses 999 for all of these)
    public static int SellAncientRelicBasePrice { get; set; } = 750; // Boss 遗物非常珍贵，卖掉它应该能换回一个顶级商店遗物 (750 * 0.35 ≈ 262g)
    public static int SellStarterRelicBasePrice { get; set; } = 300; // 初始遗物保持原价
    public static int SellEventRelicBasePrice { get; set; } = 200;   // 事件遗物通常免费获取，调低回收价避免滥用

    public static bool EnableGiftMode { get; set; } = true;

    public static bool EnableRemovalEnchantRandom { get; set; } = true;
    public static bool EnableEnchantService { get; set; } = true;
    public static int EnchantStartShopVisit { get; set; } = 4;
    public static float EnchantReplaceChance { get; set; } = 0.3f;
    public static int EnchantCost { get; set; } = 105;
    public static Vector2I EnchantAmountRange { get; set; } = new Vector2I(1, 2);
    public static Vector2I EnchantCardCountRange { get; set; } = new Vector2I(1, 2);
    public static bool EnableRandomTeammateGiftService { get; set; } = true;
    public static Vector2I GiftServiceCardCountRange { get; set; } = new Vector2I(1, 1);
    public static int GiftServiceBaseCost { get; set; } = 85;
    public static int GiftServiceStepCost { get; set; } = 55;
}
