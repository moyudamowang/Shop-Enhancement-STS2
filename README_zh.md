# ShopEnhancement (STS2 Mod)

本项目为《Slay the Spire 2》模组，旨在增强商店与奖励系统的策略深度，提供更多选择与趣味性，同时保持游戏平衡。
Base By https://github.com/Alchyr/ModTemplate-StS2

前置依赖: [BaseLib-StS2](https://github.com/Alchyr/BaseLib-StS2)

## 功能特性 (默认配置)

本模组包含以下核心功能调整：

### 1. 商店卡牌移除优化
- **更灵活的费用**：初始移除费用调整为 **50 金币**（原版 75），后续每次增加 25 金币。鼓励玩家早期精简卡组。
- **多次移除**：每进入一次商店，最多可移除 **3 张** 卡牌（需支付递增费用）。

### 2. 商店刷新机制
- **刷新商品**：花费 **40 金币** 刷新商店内的所有卡牌、遗物和药水。
- **次数限制**：每家商店限刷新 **3 次**，防止过度滥用。

### 3. 经济补偿机制
- **未购物奖励**：如果离开商店时未购买任何物品，将获得 **15 金币** 的路费补偿。
- **跳过卡牌奖励**：战斗胜利后，若选择跳过卡牌奖励，将获得 **15 金币**。

### 4. 跨职业卡牌
- **更多样化的构筑**：商店中的卡牌有 **20%** 的概率被替换为其他职业的卡牌，为构筑带来意外惊喜与全新思路。

### 5. 全内容解锁
- **一键解锁**：进入主菜单时自动解锁所有卡牌、遗物、药水以及**全部角色**（通过揭示所有纪元实现）。无需繁琐的刷图过程，直接体验游戏全部内容。

## 构建与安装

在项目根目录执行：

```powershell
dotnet publish -c Release
```

构建成功后，Mod 文件将自动复制到游戏目录的 `mods` 文件夹：
- `ShopEnhancement.dll`
- `ShopEnhancement.pck` (如果有资源)
- `mod_manifest.json`
- 
## 直接安装

见右侧 releases 。

将编译后的 `ShopEnhancement.pck & dll` 放入游戏 `mods` 目录。

**注意：** 你还需要安装前置依赖 [BaseLib-StS2](https://github.com/Alchyr/BaseLib-StS2)。
请从其 Release 页面下载 `BaseLib.dll` 和 `BaseLib.pck` 并一同放入 `mods` 目录。

## 目录结构

- `ShopEnhancement/`：模组主要逻辑与补丁代码
- `ShopEnhancementConfig.cs`：核心数值配置文件
- `mod_manifest.json`：模组元数据

## 备注

- 模组通过 Harmony 注入逻辑，兼容性取决于游戏版本。
- 若导出失败或游戏提示未加载，优先检查游戏目录 `mods` 是否包含 `ShopEnhancement.dll` 与 `mod_manifest.json`。
