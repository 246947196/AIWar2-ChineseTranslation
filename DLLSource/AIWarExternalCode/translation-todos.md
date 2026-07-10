# AI War 2 Chinese Translation - UI String Translation

## Task Status

- [ ] Group 1: Small files (StandingPlanetOrders, GiftFleet, DZTechTree, DZLogistics, TechRefunds)
- [ ] Group 2: Medium files (OngoingMessage, Notifications, DZEconomy)
- [ ] Group 3: Menu files (BottomLeftMenu, BottomLeftGalaxyMap, ModalSwapFleet, HackChoices)
- [ ] Group 4: Info files (HoverPlanet, SelectionInfo, FleetManagement)
- [x] Group 5: Window_InGameHoverEntityInfo.cs (8390 lines) — 已完成（2026-07-10 补译遗漏的 Resource Multipliers 区块）
- [x] Group 6: Window_PrototypeInGameHoverEntityInfo.cs (9824 lines) — 已完成

## Rules
1. Only translate UI strings visible to the player
2. Do NOT translate variable/method/class names, comments, debug messages, internal identifiers
3. Use Simplified Chinese (简体中文)
4. Keep code structure exactly the same
5. For string interpolation like $"{something}", keep the interpolation, translate surrounding text
6. Preserve formatting tags like <color>, <b>, etc.
7. Use Chinese punctuation (，。！？) in translated strings
