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
                        Buffer.Add( "\nTransports inbound:\n" );
                    inboundCount++;
                    if ( ship.Planet == RelatedEntityOrNull.Planet )
                        Buffer.Add( "\t- A transport on this planet\n" );
                    else
                        Buffer.Add( "\t- A transport on " ).Add( ship.GetPlanetName_Safe(), "066006" ).Add( "\n" );
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
                Buffer.Add(" This fleet has killed ").Add( data.UnitsKilled, "a1ffa1" ).Add(" units; after it kills ").Add( RelatedEntityTypeData.KillsToTriggerTransformation, "ffa1a1" ).Add(" then we will transform into ").Add(RelatedEntityTypeData.TransformAfterKills.GetDisplayName(), "ff22ff").Add(". ");
                if ( data.Inventory != null && DarkZenithFactionBaseInfo.ResourceColour != null)
                {
                    FactionUtilityMethods.Instance.PrintDZDictionary( data.Inventory, ref Buffer, "Flagship Resources" );
                }
                return;
            }

            if ( data.Inventory == null || DarkZenithFactionBaseInfo.ResourceColour == null )
            {
                Buffer.Add( "Please unpause the game to see further information about this unit." );
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
                    Buffer.Add( "This planet will succumb to the Fimbulwinter in " ).AddHoursAndMinutes( timeTillConversion, timerColor ).Add( ". The only way to stop the transformation is to destroy the Hjarn there." ).Add( "\n" );
                    return;
                }
                if ( data.IsJormugandr &&
                     RelatedEntityOrNull.PlanetFaction.Faction.Type != FactionType.Player) //not for player-controlled jormugandr
                {
                    if ( data.IsDormant )
                    {
                        Buffer.Add( "This golem seems to be dormant. Hopefully nothing wakes it up..." );
                        if ( debug )
                            Buffer.Add( " The golem will move again in " ).AddHoursAndMinutes( data.SecondsUntilDormancyMove ).Add( "." );
                    }
                    else
                    {
                        Buffer.Add( "This golem is enraged and is leading the charge against the galaxy." );
                        if ( debug )
                            Buffer.Add( " The golem will remain active for " ).AddHoursAndMinutes( data.SecondsRemaingActive ).Add( "." );
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
                                Buffer.Add( "The Terminus is building its initial Harvesters.", "ffa100" );
                            else
                                Buffer.Add( "The Terminus is bootrapping the DZ economy by building a ", "ffa100" ).Add( data.NextConversion.Unit.GetDisplayName(), "a1a1ff" );
                        }
                    }
                    else
                    {
                        Buffer.Add( "Next thing to be created: " );
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
                    FactionUtilityMethods.Instance.PrintDZDictionary( data.Inventory, ref Buffer, "Inventory" );
                    AddInboundTransportsToBuffer( RelatedEntityOrNull, globaldata, Buffer );
                }
                debugCode = 210;
                if ( data.HasAnyPermanentBonusIncome() )
                    FactionUtilityMethods.Instance.PrintDZDictionary( data.PermanentBonusIncome, ref Buffer, "PermanentBonusIncome" );
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
                        Buffer.Add("Avast, this is a Pirate Epistyle. It will send out Privateers to hijack resources from passing Transports. It will launch a privateer soon.\n");
                    else
                        Buffer.Add("Avast, this is a Pirate Epistyle. It will send out Privateers to hijack resources from passing Transports. Time till next privateer: ").Add( (data.TimeForNextPrivateer - World_AIW2.Instance.GameSecond), "a1ffa1" ).Add(" seconds.\n");
                }
                if ( debug && data.IsJormugandr )
                {
                    if ( data.IsDormant )
                        Buffer.Add( "This Jormugandr is dormant\n" );
                }
                if ( data.CanBuildInfrastructure || data.CanBuildOffensiveUnits || data.CanBuildUpgrades || data.CanBuildUtility )
                {
                    Buffer.Add( "This unit can build " );
                    if ( data.CanBuildUpgrades )
                        Buffer.Add( "Upgrades ", "a1ffa1" );
                    if ( data.CanBuildInfrastructure )
                        Buffer.Add( "Infrastructure ", "a1a1ff" );
                    if ( data.CanBuildOffensiveUnits )
                        Buffer.Add( "Offense ", "ffa1a1" );
                    if ( data.CanBuildUtility )
                        Buffer.Add( "Utility ", "22a188" );
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
