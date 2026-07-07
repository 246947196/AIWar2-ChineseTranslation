# Task 14a Report: Window_PrototypeInGameHoverEntityInfo.cs 早期遗漏

## Status: ✅ 完成

## 翻译的字符串 (16处)

| # | 位置 | 英文 | 中文 |
|---|------|------|------|
| 1 | ~2253 | `"Increases asteroid powerplant production: "` | `"增加小行星发电站产量："` |
| 2 | ~2254 | `"x"` (metal multiplier) | `"倍"` |
| 3 | ~2262 | `"Increases asteroid powerplant production: "` | `"增加小行星发电站产量："` |
| 4 | ~2264 | `"x"` (energy multiplier) | `"倍"` |
| 5 | ~2264 | `"/s"` | `"/秒"` |
| 6 | ~2597 | `"When allied units on this planet would die, this ship instead reduces its own health to regenerate them. The efficiency is one hull point from this unit per <color=#ffdf72>"` | `"当此星球上的友方单位将要死亡时，此舰船改为减少自身生命值来复活它们。效率为每 <color=#ffdf72>"` |
| 7 | ~2614 | `"Cannot be captured by other factions. "` | `"不能被其他阵营捕获。"` |
| 8 | ~2618 | `"Cannot be supercharged. "` | `"不能超载充能。"` |
| 9 | ~2630 | `"copies of itself"` | `"自身的复制体"` |
| 10 | ~2842 | `"deg/s"` | `"度/秒"` |
| 11 | ~2874 | `"HARDENED "` | `"强化 "` |
| 12 | ~2876 | `"GREAT-FORCEFIELD"` | `"巨力场"` |
| 13 | ~2890 | `"Hardened forcefields do not shrink as their shield health goes down.  "` | `"强化力场不会随着护盾生命值下降而缩小。  "` |
| 14 | ~2892 | `"Great-forcefields do not cause allies firing out from under the shield to have any damage penalty.  "` | `"巨力场不会使护盾下的友方射击受到伤害惩罚。  "` |
| 15 | ~2894 | `"Any allies firing out from under the shield only do half damage.  "` | `"任何从护盾下向外射击的友方单位只造成一半伤害。  "` |
| 16 | ~2897 | `"This electrotoxic forcefield deals "` | `"此电毒力场造成 "` |
| 17 | ~2916 | `"The electrotoxic hull on this unit deals "` | `"此单位的电毒船体造成 "` |
| 18 | ~2921 | `"Returns damage dealt to it.  "` | `"返还所受伤害。  "` |

## 注意事项
- 保留的 Debug 字符串：`"fireteamId "`, `"ComputeDisabledReason: "`
- 大多数早期遗漏的翻译（资源生产/倍率、行星停留、巢穴/引擎、风筝/轨道等）已在之前任务中完成，本次仅补充了上述 16 处遗漏
- 未使用中文引号 `""`，使用单引号 `''` 替代

## 构建结果
- AIWarExternalCode.dll: ✅ OK (3668 KB)
- AIWarExternalDeepProcessingCode.dll: ✅ OK (1759 KB)
- AIWarExternalVisualizationCode.dll: ✅ OK (224 KB)
- ArcenUIAssetRedirect.dll: ✅ OK (6 KB)
- arcenui AssetBundle: ✅ Patched (242 patched, 214 skipped)
