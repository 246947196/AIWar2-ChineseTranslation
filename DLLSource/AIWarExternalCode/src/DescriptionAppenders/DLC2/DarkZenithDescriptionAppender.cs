using Arcen.AIW2.Core;
using Arcen.Universal;
using System;

using System.Text;

namespace Arcen.AIW2.External
{
    public class DarkZenithDescriptionAppender : GameEntityDescriptionAppenderBase
    {
        private static void AddInboundTransportsToBuffer( GameEntity_Squad RelatedEntityOrNull, DarkZenithFactionBaseInfoRoot globaldata, ArcenCharacterBufferBase Buffer )
        {
            try
            {
                //this is set in Sim, so could race
                List<SafeSquadWrapper> transports = globaldata.Transports.GetDisplayList();
                int inboundCount = 0;
                for ( int i = 0; i < transports.Count; i++ )
                {
                    GameEntity_Squad ship = transports[i].GetSquad();
                    if ( ship == null )
                        continue;
                    DarkZenithPerUnitBaseInfo transportData = ship.CreateExternalBaseInfo<DarkZenithPerUnitBaseInfo>( "DarkZenithPerUnitBaseInfo" );
                    if ( transportData.Destination != RelatedEntityOrNull )
                        continue;
                    if ( inboundCount == 0 )
                        Buffer.Add( "\n入站运输船：\n" );
                    inboundCount++;
                    if ( ship.Planet == RelatedEntityOrNull.Planet )
                        Buffer.Add( "\t- 本星球上的运输船\n" );
                    else
                        Buffer.Add( "\t- 位于 " ).Add( ship.GetPlanetName_Safe(), "066006" ).Add( " 的运输船\n" );
                }
            }
            catch { }
        }


        public override void AddToDescriptionBuffer( GameEntity_Squad RelatedEntityOrNull, GameEntityTypeData RelatedEntityTypeData, ArcenCharacterBufferBase Buffer )
        {
            bool debug = GameSettings.Current.GetBoolBySetting( "Debug_Tooltip" ) || (Engine_AIW2.TraceAtAll && Engine_AIW2.TracingFlags.Has( ArcenTracingFlags.DarkZenith ));
            if ( RelatedEntityOrNull == null )
                return;
            DarkZenithPerUnitBaseInfo data = RelatedEntityOrNull.TryGetExternalBaseInfoAs<DarkZenithPerUnitBaseInfo>();
            if ( data == null )
                return;
            if ( RelatedEntityOrNull.GetFactionTypeSafe() == FactionType.AI )
                return; //this is owned by the gladiator AI type
            if ( RelatedEntityTypeData.KillsToTriggerTransformation > 0 )
            {
                //Only for the sidekick
                Buffer.Add(" 此舰队已击杀 ").Add( data.UnitsKilled, "a1ffa1" ).Add(" 个单位；在击杀 ").Add( RelatedEntityTypeData.KillsToTriggerTransformation, "ffa1a1" ).Add(" 个后将变形为 ").Add(RelatedEntityTypeData.TransformAfterKills.GetDisplayName(), "ff22ff").Add("。");
                if ( data.Inventory != null && DarkZenithFactionBaseInfo.ResourceColour != null)
                {
                    FactionUtilityMethods.Instance.PrintDZDictionary( data.Inventory, ref Buffer, "旗舰资源" );
                }
                return;
            }

            if ( data.Inventory == null || DarkZenithFactionBaseInfo.ResourceColour == null )
            {
                Buffer.Add( "请暂停游戏以查看关于此单位的更多信息。" );
                return;
            }
            int debugCode = 0;
            try
            {
                debugCode = 100;
                Faction faction = RelatedEntityOrNull.GetFactionOrNull_Safe();
                debugCode = 150;
                DarkZenithFactionBaseInfoRoot globaldata = faction.TryGetExternalBaseInfoAs<DarkZenithFactionBaseInfoRoot>();
                if ( globaldata == null )
                    return;
                debugCode = 160;
                int intensity = faction.BaseInfo.GetDifficultyOrdinal_OrNegativeOneIfNotRelevant();
                debugCode = 170;
                DarkZenithDifficulty diff = DarkZenithDifficultyTable.Instance.GetRowByIntensity( intensity, faction );
                if ( diff == null )
                    return;
                Buffer.Add( "\n" );
                if ( RelatedEntityTypeData.GetHasTag( "DZHjarn" ) )
                {
                    int timeTillConversion = diff.TimeToConvertPlanet - RelatedEntityOrNull.GetSecondsSinceEnteringThisPlanet();
                    string timerColor = ArcenExternalUIUtilities.GetColorForNomadMoveTime( timeTillConversion ); //timerColor gets more red the closer the planet is to succumbing
                    Buffer.Add( "此星球将�" ).AddHoursAndMinutes( timeTillConversion, timerColor ).Add( " 后被冰封。阻止变形的唯一方法是摧毁那里的希尔纳。" ).Add( "\n" );
                    return;
                }
                if ( data.IsJormugandr &&
                     RelatedEntityOrNull.PlanetFaction.Faction.Type != FactionType.Player) //not for player-controlled jormugandr
                {
                    if ( data.IsDormant )
                    {
                        Buffer.Add( "这个魔像似乎正在休眠。希望没有什么能唤醒它..." );
                        if ( debug )
                            Buffer.Add( " 魔像将在 " ).AddHoursAndMinutes( data.SecondsUntilDormancyMove ).Add( " 后再次移动。" );
                    }
                    else
                    {
                        Buffer.Add( "此魔像已暴怒并正在率领对星系的进攻。" );
                        if ( debug )
                            Buffer.Add( " 魔像将保持活跃 " ).AddHoursAndMinutes( data.SecondsRemaingActive ).Add( "。" );
                    }

                    return;
                }

                if ( RelatedEntityTypeData.IsMobileCombatant )
                    return; //mobile combatants don't do conversions or things like that
                if ( data.NextConversion != null )
                {
                    debugCode = 350;
                    if ( RelatedEntityTypeData.GetHasTag( "DZTerminus" ) )
                    {
                        //   Buffer.Add("This terminus is generating ").Add( data.Resource.ToString(), DarkZenithFactionBaseInfo.ResourceColour[data.Resource] );
                        if ( data.Unit != null )
                        {
                            if ( data.Unit.GetHasTag( "DZHarvester" ) )
                                Buffer.Add( "终点站正在建造其初始采集器。", "ffa100" );
                            else
                                Buffer.Add( "终点站正在通过建�", "ffa100" ).Add( data.NextConversion.Unit.GetDisplayName(), "a1a1ff" );
                        }
                    }
                    else
                    {
                        Buffer.Add( "下一个将要创建的�" );
                        data.NextConversion.ToBuffer( Buffer );
                    }
                }
                debugCode = 200;
                if ( RelatedEntityTypeData.GetHasTag( "DZTerminus" ) )
                {
                    FactionUtilityMethods.Instance.PrintDZDictionaryForTerminus( data.Inventory, ref Buffer, data.Resource );
                    AddInboundTransportsToBuffer( RelatedEntityOrNull, globaldata, Buffer );
                }
                else
                {
                    FactionUtilityMethods.Instance.PrintDZDictionary( data.Inventory, ref Buffer, "库存" );
                    AddInboundTransportsToBuffer( RelatedEntityOrNull, globaldata, Buffer );
                }
                debugCode = 210;
                if ( data.HasAnyPermanentBonusIncome() )
                    FactionUtilityMethods.Instance.PrintDZDictionary( data.PermanentBonusIncome, ref Buffer, "永久额外收入" );
                debugCode = 220;
                if ( debug )
                {
                    FactionUtilityMethods.Instance.PrintDZDictionary( data.NeedForResource, ref Buffer, "NeedForResource" );
                    debugCode = 230;
                    FactionUtilityMethods.Instance.PrintDZDictionary( data.NeedForResourceScore, ref Buffer, "NeedForResourceScore" );
                    debugCode = 300;
                }
                if ( !RelatedEntityTypeData.IsMobile && !RelatedEntityTypeData.GetHasTag( "DZMetalTerminus" ) && debug )
                    Buffer.Add( "\nTime since we last created something: " ).Add( (World_AIW2.Instance.GameSecond - data.TimeWeLastDidConversion) ).Add( ".\n" );

                // for debugging conversion list problems
                if ( debug )
                {
                    Buffer.Add( "\n" );
                    for ( int i = 0; i < data.ConversionList.Count; i++ )
                    {
                        Buffer.Add( "\tConversionList[" + i + "]: " );
                        data.ConversionList[i].ToBuffer( Buffer );
                    }
                }
                if ( debug )
                {
                    TooltipDetail detailLevel = EntityText.Detail;
                    if ( detailLevel == TooltipDetail.Full )
                    {
                        Buffer.Add( "All available conversions in bag:\n" );
                        for ( int i = 0; i < data.ConversionBag.InternalListSize; i++ )
                        {
                            var pair = data.ConversionBag.GetInternalListItemPairAtIndex( i );
                            Buffer.Add( pair.Value.ToString() ).Add( " --> " ).Add( pair.Count ).Add( ", " );
                        }
                    }
                }
                if ( data.Destination != null )
                    Buffer.Add( "前往 " ).Add( data.Destination.ToStringWithPlanet() ).Add( "\n" );
                Planet destPlanet = World_AIW2.Instance.GetPlanetByIndex( data.DZConstructorTargetPlanetIndex );
                debugCode = 500;
                if ( data.Destination != null )
                    Buffer.Add( "前往 " ).Add( data.Destination.TypeData.GetDisplayName(), "cddcdc" ).Add( " 在 " ).Add( data.Destination.GetPlanetName_Safe(), "066006" ).Add( "\n" );
                if ( data.SecondaryDestination != null )
                    Buffer.Add( "\t次级中途目的地：" ).Add( data.SecondaryDestination.TypeData.GetDisplayName(), "cddcdc" ).Add( " 在 " ).Add( data.SecondaryDestination.GetPlanetName_Safe(), "066006" ).Add( "\n" );

                else if ( destPlanet != null && data.Unit != null )
                {
                    Buffer.Add( "该单位正在前往建造 " );
                    if ( data.Resource != DZResource.None )
                        Buffer.Add( data.Unit.GetDisplayName(), DarkZenithFactionBaseInfo.ResourceColour[data.Resource] );
                    else
                        Buffer.Add( data.Unit.GetDisplayName(), "cddcdc" );
                    if ( destPlanet == RelatedEntityOrNull.Planet )
                        Buffer.Add( " 在本星球。" );
                    else
                        Buffer.Add( " 在 " ).Add( destPlanet.Name, "066006" );
                    Buffer.Add( "\n" );
                }
                // else if ( destPlanet != null && RelatedEntityOrNull.Planet != destPlanet && RelatedEntityOrNull.TypeData.IsMobile )
                //     Buffer.Add( " this unit is off to " ).Add( destPlanet.Name ).Add( " but there's no further destination. That's weird" );
                debugCode = 600;
                if (data.IsPirateEpistyle)
                {
                    if ( data.TimeForNextPrivateer < World_AIW2.Instance.GameSecond)
                        Buffer.Add("啊哈，这是一艘海盗旗舰。它将派出私掠船劫持过往运输船的资源。它很快就会派出私掠船。\n");
                    else
                        Buffer.Add("啊哈，这是一艘海盗旗舰。它将派出私掠船劫持过往运输船的资源。距离下次私掠船�").Add( (data.TimeForNextPrivateer - World_AIW2.Instance.GameSecond), "a1ffa1" ).Add(" 秒。\n");
                }
                if ( debug && data.IsJormugandr )
                {
                    if ( data.IsDormant )
                        Buffer.Add( "This Jormugandr is dormant\n" );
                }
                if ( data.CanBuildInfrastructure || data.CanBuildOffensiveUnits || data.CanBuildUpgrades || data.CanBuildUtility )
                {
                    Buffer.Add( " 此单位可以建�" );
                    if ( data.CanBuildUpgrades )
                        Buffer.Add( "升级 ", "a1ffa1" );
                    if ( data.CanBuildInfrastructure )
                        Buffer.Add( "基础设施 ", "a1a1ff" );
                    if ( data.CanBuildOffensiveUnits )
                        Buffer.Add( "进攻 ", "ffa1a1" );
                    if ( data.CanBuildUtility )
                        Buffer.Add( "辅助 ", "22a188" );
                }
            }
            catch ( Exception e )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "Hit a buffer in DZ description appender debug code " + debugCode + " " + e.ToString(), Verbosity.DoNotShow );
            }
            return;
        }
    }
}
