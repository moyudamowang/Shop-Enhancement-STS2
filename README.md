# ShopEnhancement (STS2 Mod)

This project is a mod for *Slay the Spire 2*, designed to enhance the strategic depth of shops and reward systems, providing more choices and fun while maintaining game balance.
Base By https://github.com/Alchyr/ModTemplate-StS2

Dependency: [BaseLib-StS2](https://github.com/Alchyr/BaseLib-StS2)

## Features (Default Configuration)

This mod includes the following core functional adjustments:

### 1. Card Removal Optimization
- **Flexible Cost**: Initial removal cost adjusted to **50 Gold** (Vanilla is 75), increasing by 25 Gold each time. Encourages players to streamline their deck early.
- **Multiple Removals**: You can remove up to **3 cards** per shop visit (with increasing costs).

### 2. Shop Refresh Mechanism
- **Refresh Goods**: Spend **40 Gold** to refresh all cards, relics, and potions in the shop.
- **Limit**: Refresh is limited to **3 times** per shop to prevent excessive abuse.

### 3. Economic Compensation Mechanism
- **No Shopping Bonus**: If you leave the shop without purchasing any items, you will receive **15 Gold** as travel expenses.
- **Skip Card Reward**: After winning a battle, if you choose to skip the card reward, you will receive **15 Gold**.

### 4. Cross-Class Cards
- **More Diverse Builds**: Cards in the shop have a **20%** chance to be replaced by cards from other classes, bringing unexpected surprises and new ideas to your build.

### 5. Full Content Unlock
- **One-Click Unlock**: Automatically unlocks all cards, relics, potions, and **all characters** (by revealing all epochs) when entering the main menu. No need for tedious grinding, experience all game content directly.

## Build & Install

Execute in the project root directory:

```powershell
dotnet publish -c Release
```

After a successful build, Mod files will be automatically copied to the game's `mods` folder:
- `ShopEnhancement.dll`
- `ShopEnhancement.pck` (if resources exist)
- `mod_manifest.json`

## Direct Installation

See releases on the right.

Place the compiled `ShopEnhancement.pck` & `ShopEnhancement.dll` into the game's `mods` directory.

**Note:** You also need to install the dependency [BaseLib-StS2](https://github.com/Alchyr/BaseLib-StS2).
Download `BaseLib.dll` and `BaseLib.pck` from its releases and place them in the `mods` directory as well.

## Directory Structure

- `ShopEnhancement/`: Mod main logic and patch code
- `ShopEnhancementConfig.cs`: Core numerical configuration file
- `mod_manifest.json`: Mod metadata

## Notes

- The mod injects logic via Harmony; compatibility depends on the game version.
- If export fails or the game prompts that it is not loaded, prioritize checking if the game directory `mods` contains `ShopEnhancement.dll` and `mod_manifest.json`.
