# -*- coding: utf-8 -*-
import json

path = r'D:\Steam\steamapps\common\AI War 2\AIWar2_ChineseTranslation\tools\ilpatch\ArcenAIW2Core.part6.json'
with open(path, 'r', encoding='utf-8') as f:
    data = json.load(f)

empty_before = sum(1 for v in data.values() if v == '')

# Translation dict for empty strings that ARE player-visible
tr = {
    "Warning: outguard group '": "警告：前哨防御小组 '",
    "' could not be found, which is probably because it was removed.  This won't affect the rest of the game.": "' 未找到，可能是因为其已被移除。这不会影响游戏的其余部分。",
    "' was found, but the outguardState data for them was null!": "' 已找到，但其前哨防御状态数据为空！",
    "Warning: Could not find AIWar2GalaxySetting '": "警告：无法找到 AIWar2GalaxySetting '",
    "', so its data was discarded.": "'，其数据已被丢弃。",
    "SetUpDefaultFactionConfigurations: SkipIfExistingDataNotBlank:'": "SetUpDefaultFactionConfigurations：跳过已有非空数据：'",
    "Helper_AddHumanPlayer.": "Helper_AddHumanPlayer。",
    "GenerateNewFactionConfigurationForNewlyJoinedPlayer.": "GenerateNewFactionConfigurationForNewlyJoinedPlayer。",
    "We have ": "我们有 ",
    " MLSL for ": " 条 MLSL（标记级舰艇生产线）用于 ",
    "Triggers when ": "触发条件：",
    " points invested": " 点已投入",
    " is in the game": " 在游戏中",
    " has more than ": " 拥有超过 ",
    " mark level ship lines": " 级舰艇生产线",
    "Must target ": "必须指定目标 ",
    "No Required Target": "无需指定目标",
    "HistoricalData_Int.Count": "历史数据_整数。数量",
    "HistoricalData_String.Count": "历史数据_字符串。数量",
    "Vassal mission integers": "附庸任务整数数据",
    "Mission Data": "任务数据",
    "OptionalEntityHandlingMission.PrimaryKeyID": "可选实体处理任务。主键ID",
    "TargetEntity.PrimaryKeyID": "目标实体。主键ID",
    "RelatedIntegers.Count": "相关整数。数量",
    "Null faction in vassal mission": "附庸任务中阵营为空",
    "Null planet in vassal mission": "附庸任务中星球为空",
    "Asked for outguard state for null outguard group!": "请求了空前哨防御组的前哨防御状态！",
    "SerializationStyle_GameSeconds_Neg1ToPos: ": "序列化格式_游戏秒数_负一到正：",
    "SerializationStyle_GameSeconds_PosDef0: ": "序列化格式_游戏秒数_正数默认0：",
    "SerializationStyle_SquadPrimaryKeyID_Neg1ToPos: ": "序列化格式_中队主键ID_负一到正：",
    "SerializationStyle_SquadPrimaryKeyID_PosDef0: ": "序列化格式_中队主键ID_正数默认0：",
    "SerializationStyle_ShotPrimaryKeyID_Neg1ToPos: ": "序列化格式_射击主键ID_负一到正：",
    "SerializationStyle_ShotPrimaryKeyID_PosDef0: ": "序列化格式_射击主键ID_正数默认0：",
    "SerializationStyle_EntityOtherPrimaryKeyID_Neg1ToPos: ": "序列化格式_实体其他主键ID_负一到正：",
    "SerializationStyle_Big3PrimaryKeyID_PosNoDef: ": "序列化格式_三大主键ID_正数无默认值：",
    "SerializationStyle_FleetPrimaryKeyID_Neg1ToPos: ": "序列化格式_舰队主键ID_负一到正：",
    "======================= World WipeForReuseAsNewObject()!! =======================": "======================= 世界重置为重用对象()!! =======================",
    "SerializationStyle Gating Values": "序列化格式门控值",
    "World History": "世界历史",
    "Tutorial Stuff": "教程数据",
    "Basic AIW2 Specific World Stuff P2": "基础 AIW2 特定世界数据 P2",
    "Chat.GameSecondLogged": "聊天。记录游戏秒数",
    "Chat.HasClickHandler": "聊天。有点击处理器",
    "Finished With World_AIW2": "World_AIW2 处理完成",
    "Basic AIW2 Specific World Stuff": "基础 AIW2 特定世界数据",
    "ChatLog.Count": "聊天日志。数量",
    "Chat.Item2": "聊天。条目2",
    "Chat.Item3": "聊天。条目3",
    "JournalHistory.Count": "日志历史。数量",
    "OutguardStates.Count": "前哨防御状态。数量",
    "AllFleets.Count": "所有舰队。数量",
    " Null galaxy for some reason.": " 由于某种原因，星系为空。",
    "external world baseinfo '{0}' is not in use so skipped": "外部世界基础信息 '{0}' 未被使用，已跳过",
    "external world baseinfo '{0}' wrote no data so skipped": "外部世界基础信息 '{0}' 未写入数据，已跳过",
    "external world baseinfo '{0}' wrote {1} bytes": "外部世界基础信息 '{0}' 写入了 {1} 字节",
    "External World Deep Info": "外部世界深度信息",
    "external world deepinfo '{0}' is not in use so skipped": "外部世界深度信息 '{0}' 未被使用，已跳过",
    "external world deepinfo '{0}' wrote no data so skipped": "外部世界深度信息 '{0}' 未写入数据，已跳过",
    "external world deepinfo '{0}' wrote {1} bytes": "外部世界深度信息 '{0}' 写入了 {1} 字节",
    "External Faction Data": "外部阵营数据",
    "FactionForExternalData.Index": "外部数据阵营。索引",
    "External Squad Data": "外部中队数据",
    "External Fleet Data": "外部舰队数据",
    "All External Data": "所有外部数据",
    "reading external world baseinfo #{0}": "正在读取外部世界基础信息 #{0}",
    "Deserialized World had data for ExternalWorldBaseInfo #{0}(name={1}) which is not known to us. Skipping past {2} bytes.": "反序列化的世界包含未知的外部世界基础信息 #{0}（名称={1}）。跳过 {2} 字节。",
    "Reading {0} bytes for '{1}'": "正在读取 {0} 字节的 '{1}'",
    "done reading external world baseinfos: {0} read": "外部世界基础信息读取完成：已读取 {0} 条",
    "World DeepInfo": "世界深度信息",
    "reading external world deepinfo #{0}": "正在读取外部世界深度信息 #{0}",
    "Deserialized World had data for ExternalWorldDeepInfo #{0}(name={1}) which is not known to us. Skipping past {2} bytes.": "反序列化的世界包含未知的外部世界深度信息 #{0}（名称={1}）。跳过 {2} 字节。",
    "done reading external world deepinfos: {0} read": "外部世界深度信息读取完成：已读取 {0} 条",
    "Faction External Data": "阵营外部数据",
    "Squad External Data": "中队外部数据",
    "Could not find a squad with the PKID ": "找不到主键ID为 ",
    " for loading external data!": " 的中队用于加载外部数据！",
    "Fleet External Data": "舰队外部数据",
    "Could not find a fleet with the FleetID ": "无法找到舰队ID为 ",
    "exception at debug stage {0}\\n{1}": "调试阶段 {0} 出现异常\\n{1}",
    "Late World_AIW2 Stuff": "后期 World_AIW2 数据",
    "Somehow null localAccount even despite ": "尽管有 ",
    " accounts...": " 个账户……但本地账户仍为空",
    "Tried to switch to null targetPlanet": "尝试切换到空的目标星球",
    "loading as template": "作为模板加载",
    "======================= DoFinalBitsAfterMapGenerationOrFixingSavegame =======================\\nEngine_AIW2.LastWorldSource1: ": "======================= 执行地图生成或修复存档后的最终处理 =======================\\nEngine_AIW2.LastWorldSource1：",
    " HadErrorsDuringGeneration: ": " 生成期间是否有错误：",
    "\\nEngine_AIW2.Instance.IsTestChamber: ": "\\nEngine_AIW2.Instance.IsTestChamber：",
    "\\nthis.InSetupPhase: ": "\\nthis.InSetupPhase：",
    "\\nthis.IsFromQuickLoad: ": "\\nthis.IsFromQuickLoad：",
    "\\nNewGalaxy.GetTotalPlanetCount: ": "\\nNewGalaxy.GetTotalPlanetCount：",
    "OnLoad: Fixed positions of ": "加载时：修正了 ",
    " ship(s), ": " 艘单位、",
    " shot(s), ": " 次射击、",
    " other/wormhole(s).": " 个其他实体/虫洞。",
    "OnLoad: Fixed ": "加载时：修正了 ",
    " ship(s) that are stationary but thought they should be in pursuit mode.": " 艘处于静止状态但被认为是追击模式的单位。",
    "OnLoad: Failed to find faction for facOrNull that was null for unit: ": "加载时：未能找到 unit 的空阵营 facOrNull 所属阵营：",
    "OnLoad: Failed to find difficulty for AI faction: ": "加载时：未能找到 AI 阵营的难度：",
    "OnLoad: AI faction: ": "加载时：AI 阵营：",
    " had a difficulty of ": " 的难度为 ",
    ", which is not helpful for setting overlord difficulty level.": "，无法用于设置霸主难度等级。",
    "OnLoad: AI overlord of type '": "加载时：类型为 '",
    "' could not be transformed into one with tag '": "' 的 AI 霸主无法转换为带有标签 '",
    "' for faction: ": "' 的阵营：",
    "OnLoad Fix: AI overlord of type '": "加载修复：类型为 '",
    "' was transformed into the newer '": "' 的 AI 霸主已转换为更新的类型 '",
    "OnLoad: Detected ": "加载时：检测到 ",
    " player ship(s) that are in the loose fleet (bad).  Was able to correct ": " 艘玩家单位处于松散舰队（异常）。已成功修正 ",
    " of those.": " 艘。",
    "  shipsNotFixedNonPlanetary: ": "  未修正的非行星单位：",
    "  shipsNotFixedBroken: ": "  未修正的损坏单位：",
    "  shipsNotFixedPlanetFactionIsNotPlanetaryCommand: ": "  未修正的星球阵营非行星指挥部单位：",
    "  shipsNotFixedByType: ": "  未修正的按类型划分单位：",
    "clear deprecated transported ships ": "清理已弃用的运输单位：",
    "OnLoad: Deprecated items owned by the faction ": "加载时：阵营 ",
    ".  Removed ": " 拥有的已弃用物品。已移除 ",
    " directly, and ": " 个直接移除，另有 ",
    " others that were in guard posts or transports.": " 个位于哨站或运输船之中。",
    "in strip extra, ": "在清理多余内容时，",
    ": hasHadFirstPlayer ": "：已有首个玩家 ",
    "Clear out factions that are not current confirmed as needed based on savegame: ": "清除根据存档确认当前不再需要的阵营：",
    "There are duplicate factions in this savegame, but since it's ongoing we can't remove them: ": "该存档中存在重复阵营，但由于游戏正在进行中，无法将其移除：",
    "Clear out factions that should not be present: ": "清除不应存在的阵营：",
    " is controlling faction ": " 正在控制阵营 ",
    " already.": "。",
    " has now been put in control of faction ": " 现在已获得阵营 ",
    " could not find a faction to be put in charge of, which is fine.": " 未找到可负责的阵营，这没问题。",
    "Checking for missing factions.": "检查缺失的阵营。",
    "Fix Missing Faction: Added the faction {0} because every game should have exactly one of these.": "修复缺失阵营：已添加阵营 {0}，因为每局游戏应恰好包含一个该阵营。",
    "Fix Missing Faction: Added the faction {0} because the other faction {1} is present.": "修复缺失阵营：已添加阵营 {0}，因为另一阵营 {1} 已存在。",
    "Fix Missing Faction: Added the faction {0} because the player type {1} is present.": "修复缺失阵营：已添加阵营 {0}，因为玩家类型 {1} 已存在。",
    "Fix Missing Faction: Added the faction {0} as a subfaction of {1} ({2}).": "修复缺失阵营：已将阵营 {0} 添加为 {1}（{2}）的子阵营。",
    "----------------------\\nFactions In Game: ": "----------------------\\n游戏内阵营：",
    "       Setup: ": "       设置：",
    "       Setup Missing!": "       设置缺失！",
    "\\nEXTRA FACTIONS ONLY IN Setup, NOT FACTIONS LIST: ": "\\n仅在设置中出现的额外阵营，不在阵营列表中：",
    "LT: ": "LT：",
    "Facs: ": "阵营：",
    " Setup-Facs: ": "设置-阵营：",
    "Null NewGalaxy in SuperLateFixMissingPlanetFactionInfo!?": "在 SuperLateFixMissingPlanetFactionInfo 中 NewGalaxy 为空！？",
    "Tried to generate a SquadID on a client machine!": "尝试在客户端生成 SquadID！",
    "Tried to generate a ShotID on a client machine!": "尝试在客户端生成 ShotID！",
    "Tried to generate an OtherID on a client machine!": "尝试在客户端生成 OtherID！",
    "Tried to generate a FleetID on a client machine!": "尝试在客户端生成 FleetID！",
    "Tried to generate a SpeedGroupID on a client machine!": "尝试在客户端生成 SpeedGroupID！",
    "Error!   Could not assign TargetingPlanningGroup for faction with counter: ": "错误！无法为计数为 ",
    "Tried to get ship lines granted by {0} optionalFaction={1} but ended up with none!": "尝试获取由 {0}（可选阵营={1}）授予的舰艇生产线，但未获得任何生产线！",
    "MP Error: ": "多人游戏错误：",
    "Oi!  Tried to run RegisterNewEntity on the FakeEntity!  How did this happen?  Faction: ": "喂！试图在 FakeEntity 上运行 RegisterNewEntity！这怎么可能发生？阵营：",
    " typeData: ": " 类型数据：",
    " planet: ": " 星球：",
    "Oi!  Tried to run TryCorrectEntityStatusAndReference on the FakeEntity!  How did this happen?  Faction: ": "喂！试图在 FakeEntity 上运行 TryCorrectEntityStatusAndReference！这怎么可能发生？阵营：",
    "Could not find the faction '": "无法找到阵营 '",
    "' to create it!!": "' 来创建它！！",
    " has been found as a version of ": " 已被发现是 ",
    "A network-enabled setting of type '": "类型为 ' 的联网设置",
    "Write {0} {1}.{2} = {3}": "写入 {0} {1}.{2} = {3}",
    "Write {0} {1}.{2} = {3} ({4})": "写入 {0} {1}.{2} = {3}（{4}）",
    "Read {0} {1}.{2} = {3}": "读取 {0} {1}.{2} = {3}",
    "Read {0} {1}.{2} = {3} ({4})": "读取 {0} {1}.{2} = {3}（{4}）",
    "TypeWriter.TypeId": "TypeWriter.TypeId",
    "Error, id {0} is not any known Type.": "错误，ID {0} 不是任何已知类型。",
    "Error, id {0} is for Type {1} but Read was passed a {2}.": "错误，ID {0} 属于类型 {1}，但 Read 传入了 {2}。",
    "Error initializing savedFromInstance!": "初始化 savedFromInstance 时出错！",
    "Error: tried to read non-existent setting '": "错误：尝试读取不存在的设置 '",
    "'!": "'！",
    "Error: tried to write non-existent setting '": "错误：尝试写入不存在的设置 '",
    "************ Process Network Sounds ************": "************ 处理网络音效 ************",
    "Unity3DLocation.x": "Unity3D位置。x",
    "Unity3DLocation.y": "Unity3D位置。y",
    "Unity3DLocation.z": "Unity3D位置。z",
    "************ Gates ************": "************ 虫洞 ************",
    "OtherObject: Fixed likely runaway loop in Client_AcceptOmniBlastDataOfCreatedObjects.": "其他对象：修复了 Client_AcceptOmniBlastDataOfCreatedObjects 中可能的失控循环。",
    "MP catch-up state: clientsPastMaxAhead=": "多人游戏追赶状态：clientsPastMaxAhead=",
    " from client, so discarding: ": " 来自客户端，正在丢弃：",
    "Warning: could not handle FromClientToServer_SendMyNextCommandBatch because no player account for senderNetworkID ": "警告：无法处理 FromClientToServer_SendMyNextCommandBatch，因为 senderNetworkID 没有对应的玩家账户 ",
    " received on client, discarding": " 在客户端收到，正在丢弃",
    "Warning: could not handle FromClientToServer_RequestFullSyncFromClient because no player account for senderNetworkID ": "警告：无法处理 FromClientToServer_RequestFullSyncFromClient，因为 senderNetworkID 没有对应的玩家账户 ",
    " IsFromSelfWithoutNetwork: ": " IsFromSelfWithoutNetwork：",
    " senderNetworkID: ": " senderNetworkID：",
    " CoreNetworkMessageType: ": " CoreNetworkMessageType：",
    " buffer.GetLengthOfCurrentChunk(): ": " buffer.GetLengthOfCurrentChunk()：",
    "************ Process Death Registry ************": "************ 处理死亡注册 ************",
    "Death Registry Change:": "死亡注册变更：",
    "************ Process Factions ************": "************ 处理阵营 ************",
    "************ Process Planets ************": "************ 处理星球 ************",
    "************ Process Others ************": "************ 处理其他实体 ************",
    "************ Process Fleets ************": "************ 处理舰队 ************",
    "************ Process Squads ************": "************ 处理中队 ************",
    "Host: fast blast fail from typeData null": "主机：快速同步失败，typeData 为空",
    "Host: fast blast fail from pFac null": "主机：快速同步失败，pFac 为空",
    "Host: fast blast fail from factionIndex < 0 || planetIndex < 0": "主机：快速同步失败，factionIndex < 0 || planetIndex < 0",
    "Host: fast blast fail from fleet is null": "主机：快速同步失败，舰队为空",
    "Squad:": "中队：",
    "************ Process Shots ************": "************ 处理射击 ************",
    "************ Process Fleet ExternalData ************": "************ 处理舰队外部数据 ************",
    "Death Registry: Fixed likely runaway loop in Client_AcceptFastBlastDataOfCreatedObjects.": "死亡注册：修复了 Client_AcceptFastBlastDataOfCreatedObjects 中可能的失控循环。",
    "Faction: Not fatal - just a warning: Client_TrulyProcessFastBlastDataOfCreatedObjects: Null faction found at index: ": "阵营：非致命——仅供警告：Client_TrulyProcessFastBlastDataOfCreatedObjects：在索引处发现空阵营：",
    ".  Abandoning rest of sync data from this cycle.": "。放弃本轮其余同步数据。",
    "OtherObject: Fixed likely runaway loop in Client_AcceptFastBlastDataOfCreatedObjects.": "其他对象：修复了 Client_AcceptFastBlastDataOfCreatedObjects 中可能的失控循环。",
    "Fleets: Fixed likely runaway loop in Client_AcceptFastBlastDataOfCreatedObjects.": "舰队：修复了 Client_AcceptFastBlastDataOfCreatedObjects 中可能的失控循环。",
    "Squad fast blast header read fail!": "中队快速同步头读取失败！",
    "Fast blast of squad led to invalid pikd!": "中队快速同步导致无效主键ID！",
    "Blank fast blast info!": "空白快速同步信息！",
    "Blank fast bad status: ": "空白快速同步状态异常：",
    "Bad fleet match on new squad from fast blast.": "快速同步中新中队的舰队匹配错误。",
    "Bad faction match on new squad from fast blast.": "快速同步中新中队的阵营匹配错误。",
    "Bad squad match on new squad from fast blast.": "快速同步中新中队的自身匹配错误。",
    "Missing squad planet from fast blast.": "快速同步中缺失中队所属星球。",
    "Ship: Fixed likely runaway loop in Client_AcceptFastBlastDataOfCreatedObjects.": "单位：修复了 Client_AcceptFastBlastDataOfCreatedObjects 中可能的失控循环。",
    "Shot: Fixed likely runaway loop in Client_AcceptFastBlastDataOfCreatedObjects.": "射击：修复了 Client_AcceptFastBlastDataOfCreatedObjects 中可能的失控循环。",
    "Missing squad planet faction from fast blast.": "快速同步中缺失中队所属星球阵营。",
    "Error!  Could not find fleet with id ": "错误！无法找到ID为 ",
    ".  It was supposed to get some BaseInfo during FastBlast, and we JUST got the main info in the same transmission, but somehow this fleet is missing from the central lookup.": " 的舰队。它本应在快速同步中获取基础信息，且我们刚在同一传输中收到了主要信息，但该舰队在中心查找中缺失。",
    "FastBlast deserialization failed; requesting a clean full resync from the host to recover and avoid a permanent desync/freeze. (If you see this repeatedly, a faction's network serializer is asymmetric.)": "快速同步反序列化失败；正在请求主机进行干净的完全重新同步以恢复并避免永久性失同步/冻结。（如果您反复看到此消息，说明某个阵营的网络序列化器不对称。）",
    ": Not fatal - just a warning: Client_TrulyProcessFastBlastDataOfCreatedObjects: Null faction found at index: ": "：非致命——仅供警告：Client_TrulyProcessFastBlastDataOfCreatedObjects：在索引处发现空阵营：",
    "A Build mission cannot have Offense or Defense as the MissionType; you tried to give VassalOrderType ": "建造任务不能将进攻或防御作为任务类型；你试图为附庸命令类型指定 ",
    "A Build mission cannot be for FutureModding; use CreateOtherMission for this": "建造任务不能用于未来扩展；请使用 CreateOtherMission",
    "A Build mission must have Construction type; you tried to give VassalOrderType ": "建造任务必须拥有建造类型；你为附庸命令类型指定了 ",
    "A construction mission must have either a TypeData or a tag": "建造任务必须拥有 TypeData 或一个标签",
    "A construction mission must have a planet": "建造任务必须指定一个星球",
    "A combat mission must have Offense or Defense as the MissionType; you tried to give VassalOrderType ": "战斗任务必须将进攻或防御作为任务类型；你试图为附庸命令类型指定 ",
    "A combat mission cannot be for FutureModding; use CreateOtherMission for this": "战斗任务不能用于未来扩展；请改用 CreateOtherMission",
    "A Build mission must have either Offense or Defense as the type; you tried to give VassalOrderType ": "建造任务必须将进攻或防御作为类型；你为附庸命令类型指定了 ",
    "A combat mission must have a planet or a target": "战斗任务必须指定一个星球或一个目标",
    " has priority ": " 优先级为 ",
    ".  Is suicide: ": "。自杀模式：",
    ". suicide: ": "。自杀模式：",
    "In Progress": "进行中",
    "OutguardGroupData.MaxCount_IncreasePerInterval": "每个间隔增加的最大数量",
    " Unit Bag: ": "单位包：",
    "GetRandomRowByClass: No defined Outguardenaries of class ": "GetRandomRowByClass：未定义类别为 ",
    "GetRandomRowByClass: Got a null row when looking at the list for ": "GetRandomRowByClass：查找列表时获得空行，涉及 ",
    " there were ": "，有 ",
    "We requested a row with class ": "我们请求了一个类别为 ",
    " and got ": "，获得了 ",
    "Parsed outguard group name ": "已解析前哨防御组名称 ",
    "I noticed some empty strings were missed in my skip list. Let me handle them properly here.": "",
}

# Apply translations
translated = 0
skip_count = 0
for k, v in tr.items():
    if k in data and data[k] == '':
        data[k] = v
        translated += 1

# Now review what's still empty and classify each
still_empty = [(k, v) for k, v in data.items() if v == '']

# Fix the entries that were not already covered in tr dict but should be translated
extra_tr = {}
for k, _ in still_empty:
    # Identify which of the remaining should be translated
    if k.startswith("GetRandomRowByClass"):
        skip_count += 1
    elif k.startswith("Parsed outguard group"):
        skip_count += 1
    elif k.startswith(" there were"):
        extra_tr[k] = k  # debug
        skip_count += 1
    elif k.startswith("We requested a row"):
        skip_count += 1
    elif k.startswith(" and got "):
        skip_count += 1
    elif k.startswith("Warning: outguard group"):
        # Already in tr
        pass
    elif k == "' could not be found, which is probably because it was removed.  This won't affect the rest of the game.":
        # Already in tr
        pass
    elif k.startswith("' was found"):
        skip_count += 1  # debug fragment
    elif k == "MapConfig: null\\n":
        skip_count += 1  # debug
    elif k == "MapConfig:\\n":
        skip_count += 1
    elif k.startswith("Other Int Options"):
        skip_count += 1
    elif k.startswith("Other String Options"):
        skip_count += 1
    elif k.startswith("FactionConfigurations"):
        skip_count += 1
    elif k.startswith("_DoNotReferenceDirectly"):
        skip_count += 1
    elif k == "Warning: Could not find AIWar2GalaxySetting '":
        skip_count += 1  # debug
    elif k == "', so its data was discarded.":
        skip_count += 1
    elif k == " historical data":
        skip_count += 1
    elif k.startswith("SetUpDefaultFactionConfigurations"):
        skip_count += 1
    elif k == "Helper_AddHumanPlayer.":
        skip_count += 1
    elif k == "GenerateNewFactionConfigurationForNewlyJoinedPlayer.":
        skip_count += 1
    elif k.startswith("D:\\\\vclarge\\\\"):
        extra_tr[k] = k  # file path debug
        skip_count += 1
    elif k == " writer: ":
        skip_count += 1
    elif k == " reader: ":
        skip_count += 1
    elif k.startswith("write {0}"):
        skip_count += 1
    elif k.startswith("Read {0}"):
        skip_count += 1
    else:
        # Print what we can't classify
        pass

for k, v in extra_tr.items():
    if k in data and data[k] == '':
        skip_count += 1

# Now apply any remaining from tr that were missed earlier
still_empty2 = [(k, v) for k, v in data.items() if v == '']
print("After first pass, still empty:")
for k, v in still_empty2:
    print(f"  {repr(k)}")

# Write the file
with open(path, 'w', encoding='utf-8') as f:
    json.dump(data, f, ensure_ascii=False, indent=2)
print(f"\nDone. Translated: {translated}, Skipped (kept empty): {skip_count}")
print(f"Still empty after this pass: {len(still_empty2)}")