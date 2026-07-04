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
                        Buffer.Add( "\n鍏ョ珯杩愯緭鑸癸細\n" );
                    inboundCount++;
                    if ( ship.Planet == RelatedEntityOrNull.Planet )
                        Buffer.Add( "\t- 鏈槦鐞冧笂鐨勮繍杈撹埞\n" );
                    else
                        Buffer.Add( "\t- 浣嶄簬 " ).Add( ship.GetPlanetName_Safe(), "066006" ).Add( " 鐨勮繍杈撹埞\n" );
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
                Buffer.Add(" 姝よ埌闃熷凡鍑绘潃 ").Add( data.UnitsKilled, "a1ffa1" ).Add(" 涓崟浣嶏紱鍦ㄥ嚮鏉€ ").Add( RelatedEntityTypeData.KillsToTriggerTransformation, "ffa1a1" ).Add(" 涓悗灏嗗彉褰负 ").Add(RelatedEntityTypeData.TransformAfterKills.GetDisplayName(), "ff22ff").Add("銆?);
                if ( data.Inventory != null && DarkZenithFactionBaseInfo.ResourceColour != null)
                {
                    FactionUtilityMethods.Instance.PrintDZDictionary( data.Inventory, ref Buffer, "Flagship Resources" );
                }
                return;
            }

            if ( data.Inventory == null || DarkZenithFactionBaseInfo.ResourceColour == null )
            {
                Buffer.Add( "璇锋殏鍋滄父鎴忎互鏌ョ湅鍏充簬姝ゅ崟浣嶇殑鏇村淇℃伅銆? );
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
                    Buffer.Add( "姝ゆ槦鐞冨皢鍦?" ).AddHoursAndMinutes( timeTillConversion, timerColor ).Add( " 鍚庤鍐板皝銆傞樆姝㈠彉褰㈢殑鍞竴鏂规硶鏄懅姣侀偅閲岀殑甯屽皵绾炽€? ).Add( "\n" );
                    return;
                }
                if ( data.IsJormugandr &&
                     RelatedEntityOrNull.PlanetFaction.Faction.Type != FactionType.Player) //not for player-controlled jormugandr
                {
                    if ( data.IsDormant )
                    {
                        Buffer.Add( "杩欎釜榄斿儚浼间箮姝ｅ湪浼戠湢銆傚笇鏈涙病鏈変粈涔堣兘鍞ら啋瀹?.." );
                        if ( debug )
                            Buffer.Add( " 榄斿儚灏嗗湪 " ).AddHoursAndMinutes( data.SecondsUntilDormancyMove ).Add( " 鍚庡啀娆＄Щ鍔ㄣ€? );
                    }
                    else
                    {
                        Buffer.Add( "姝ら瓟鍍忓凡鏆存€掑苟姝ｅ湪鐜囬瀵规槦绯荤殑杩涙敾銆? );
                        if ( debug )
                            Buffer.Add( " 榄斿儚灏嗕繚鎸佹椿璺?" ).AddHoursAndMinutes( data.SecondsRemaingActive ).Add( "銆? );
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
                                Buffer.Add( "缁堢偣绔欐鍦ㄥ缓閫犲叾鍒濆閲囬泦鍣ㄣ€?, "ffa100" );
                            else
                                Buffer.Add( "缁堢偣绔欐鍦ㄩ€氳繃寤洪€?", "ffa100" ).Add( data.NextConversion.Unit.GetDisplayName(), "a1a1ff" );
                        }
                    }
                    else
                    {
                        Buffer.Add( "涓嬩竴涓皢瑕佸垱寤虹殑锛? );
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
                    FactionUtilityMethods.Instance.PrintDZDictionary( data.Inventory, ref Buffer, "搴撳瓨" );
                    AddInboundTransportsToBuffer( RelatedEntityOrNull, globaldata, Buffer );
                }
                debugCode = 210;
                if ( data.HasAnyPermanentBonusIncome() )
                    FactionUtilityMethods.Instance.PrintDZDictionary( data.PermanentBonusIncome, ref Buffer, "姘镐箙棰濆鏀跺叆" );
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
                    Buffer.Add( "En route to " ).Add( data.Destination.ToStringWithPlanet() ).Add( "\n" );
                Planet destPlanet = World_AIW2.Instance.GetPlanetByIndex( data.DZConstructorTargetPlanetIndex );
                debugCode = 500;
                if ( data.Destination != null )
                    Buffer.Add( "Off to " ).Add( data.Destination.TypeData.GetDisplayName(), "cddcdc" ).Add( " on " ).Add( data.Destination.GetPlanetName_Safe(), "066006" ).Add( "\n" );
                if ( data.SecondaryDestination != null )
                    Buffer.Add( "\tSecondary stopoff destination: " ).Add( data.SecondaryDestination.TypeData.GetDisplayName(), "cddcdc" ).Add( " on " ).Add( data.SecondaryDestination.GetPlanetName_Safe(), "066006" ).Add( "\n" );

                else if ( destPlanet != null && data.Unit != null )
                {
                    string article = "a ";
                    if ( ArcenStrings.DoesStringStartWithVowel( data.Unit.GetDisplayName() ) )
                        article = "an ";
                    Buffer.Add( "This unit is en route to build " ).Add( article );
                    if ( data.Resource != DZResource.None )
                        Buffer.Add( data.Unit.GetDisplayName(), DarkZenithFactionBaseInfo.ResourceColour[data.Resource] );
                    else
                        Buffer.Add( data.Unit.GetDisplayName(), "cddcdc" );
                    if ( destPlanet == RelatedEntityOrNull.Planet )
                        Buffer.Add( " on this planet." );
                    else
                        Buffer.Add( " on " ).Add( destPlanet.Name, "066006" );
                    Buffer.Add( "\n" );
                }
                // else if ( destPlanet != null && RelatedEntityOrNull.Planet != destPlanet && RelatedEntityOrNull.TypeData.IsMobile )
                //     Buffer.Add( " this unit is off to " ).Add( destPlanet.Name ).Add( " but there's no further destination. That's weird" );
                debugCode = 600;
                if (data.IsPirateEpistyle)
                {
                    if ( data.TimeForNextPrivateer < World_AIW2.Instance.GameSecond)
                        Buffer.Add("鍟婂搱锛岃繖鏄竴鑹樻捣鐩楁棗鑸般€傚畠灏嗘淳鍑虹鎺犺埞鍔寔杩囧線杩愯緭鑸圭殑璧勬簮銆傚畠寰堝揩灏变細娲惧嚭绉佹帬鑸广€俓n");
                    else
                        Buffer.Add("鍟婂搱锛岃繖鏄竴鑹樻捣鐩楁棗鑸般€傚畠灏嗘淳鍑虹鎺犺埞鍔寔杩囧線杩愯緭鑸圭殑璧勬簮銆傝窛绂讳笅娆＄鎺犺埞锛?).Add( (data.TimeForNextPrivateer - World_AIW2.Instance.GameSecond), "a1ffa1" ).Add(" 绉掋€俓n");
                }
                if ( debug && data.IsJormugandr )
                {
                    if ( data.IsDormant )
                        Buffer.Add( "This Jormugandr is dormant\n" );
                }
                if ( data.CanBuildInfrastructure || data.CanBuildOffensiveUnits || data.CanBuildUpgrades || data.CanBuildUtility )
                {
                    Buffer.Add( " 姝ゅ崟浣嶅彲浠ュ缓閫?" );
                    if ( data.CanBuildUpgrades )
                        Buffer.Add( "鍗囩骇 ", "a1ffa1" );
                    if ( data.CanBuildInfrastructure )
                        Buffer.Add( "鍩虹璁炬柦 ", "a1a1ff" );
                    if ( data.CanBuildOffensiveUnits )
                        Buffer.Add( "杩涙敾 ", "ffa1a1" );
                    if ( data.CanBuildUtility )
                        Buffer.Add( "杈呭姪 ", "22a188" );
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
