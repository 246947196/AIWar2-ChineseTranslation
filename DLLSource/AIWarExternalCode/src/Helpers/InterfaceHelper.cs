using Arcen.AIW2.Core;
using System;

using System.Text;
using Arcen.Universal;

namespace Arcen.AIW2.External
{
    public static class InterfaceHelper
    {
        public static void TakeQuitSave( bool quitAfterwards, bool QuitToOSAfter )
        {
            string campaignName = World.Instance.CampaignName;
            if ( campaignName == null || campaignName.Length == 0 )
            {
                ArcenDebugging.ArcenDebugLog( "Could not create a preset, because the campaign name is empty!", Verbosity.ShowAsError );
                return;
            }

            int debugCode = 0;
            try
            {
                debugCode = 100;
                string saveGameName = "Most Recent Quit Save"; //This overwrites every time
                GameCommand command = GameCommand.Create( BaseGameCommand.CommandsByCode[BaseGameCommand.Code.SaveGame], GameCommandSource.AnythingElse );
                debugCode = 200;
                command.RelatedString = saveGameName;
                command.RelatedBool = quitAfterwards;
                command.RelatedMagnitude = QuitToOSAfter ? 1 : 0;
                World_AIW2.Instance.QueueGameCommand( World_AIW2.Instance.GetLocalPlayerFactionOrNaturalObjectsNeverNull(), command, false );
                GameSettings.Current.LastSavegameFile = saveGameName;
                GameSettings.Current.LastSavegameCampaign = campaignName;
                GameSettings.SaveToDisk();
                debugCode = 300;
            }
            catch ( Exception e )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "Hit exception during TakeQuitSave debugCode " + debugCode + " " + e.ToString(), Verbosity.DoNotShow );
            }
        }

        public static void WriteFactionNameToBuffer( Faction faction, ArcenDoubleCharacterBuffer Buffer )
        {
            //if ( faction.SpecialFactionData.ShouldNotBeShown || faction.Type == FactionType.NaturalObject )
            //    continue;
            //DO draw those
            if ( !faction.HasBeenSeenByPlayer && !AIWar2GalaxySettingTable.GetIsBoolSettingEnabledByName_DuringGame( "AlwaysShowFactions" ) )
                Buffer.StartColor( faction.FactionCenterColor.ColorHexBrighter ).Add( "Undiscovered" ).EndColor();
            else
            {
                if ( faction.RandomImpact != TypeDifficulty.Unset && //this is a random faction
                    !faction.HasBeenSeenByPlayer ) //that hasn't been seen by a player
                {
                    if ( GameSettings.Current.GetBoolBySetting( "HideRandomFactions" ) )
                        Buffer.StartColor( faction.FactionCenterColor.ColorHexBrighter ).Add( "Undiscovered Rand" ).EndColor(); //don't show the player these factions at all
                    if ( GameSettings.Current.GetBoolBySetting( "HideRandomFactionType" ) )
                    {
                        Buffer.StartColor( faction.FactionCenterColor.ColorHexBrighter ).Add( "Random " ).Add( EnumNameCache.GetName( faction.RandomImpact ) ).EndColor();
                    }
                    else
                        Buffer.StartColor( faction.FactionCenterColor.ColorHexBrighter ).Add( faction.GetDisplayName() ).EndColor();
                }
                else
                    Buffer.StartColor( faction.FactionCenterColor.ColorHexBrighter ).Add( faction.GetDisplayName() ).EndColor();
            }
        }

        public static void WriteFactionsToBuffer( ArcenDoubleCharacterBuffer Buffer )
        {
            int debugCode = 0;
            try{
                debugCode = 100;
                bool secretFactionDetails = AIWar2GalaxySettingTable.GetIsBoolSettingEnabledByName_DuringGame( "FactionDetailsAreSecret" ) && World.Instance.ConclusionType == CampaignConclusionType.NotConcluded;
            bool hideFactions = !AIWar2GalaxySettingTable.GetIsBoolSettingEnabledByName_DuringGame( "AlwaysShowFactions" );
            bool hideRandomFactions = GameSettings.Current.GetBoolBySetting( "HideRandomFactions" );
            bool hideRandomFactionType = GameSettings.Current.GetBoolBySetting( "HideRandomFactionType" ) || secretFactionDetails;
            if ( World.Instance.ConclusionType == CampaignConclusionType.Lost )
                hideFactions = false; //show all the factions if the player is defeated
            bool showVerboseDetails = InputCaching.CalculateShouldIncreaseShipAndPlanetTooltipDetailBy1_Key1();
            bool showDebugInfoInTooltip = GameSettings.Current.GetBoolBySetting( "Debug_Tooltip" );
            bool showRandomAiType = GameSettings.Current.GetBoolBySetting( "ShowRandomAIType" ) && !secretFactionDetails;
            debugCode = 200;
            List<Faction> SortedFactionsList = FactionFilter.GetLatestSortedFactionCompleteList();
            for ( int i = 0; i < SortedFactionsList.Count; i++ )
            {
                debugCode = 300;
                if ( SortedFactionsList[i] == null )
                    continue;
                
                Faction faction = World_AIW2.Instance.GetFactionByIndex( SortedFactionsList[i].FactionIndex );
                if ( faction == null )
                    continue;
                
                bool factionDisplaysRandom = false;
                if ( faction.SpecialFactionData.ShouldNotBeShown || faction.Type == FactionType.NaturalObject )
                    continue;

                if ( hideFactions &&
                     !faction.HasBeenSeenByPlayer )
                    continue; //if this faction hasn't been found, hide it
                
                debugCode = 400;
                if ( faction.RandomImpact != TypeDifficulty.Unset && //this is a random faction
                     !faction.HasBeenSeenByPlayer ) //that hasn't been seen by a player
                {
                    if ( hideRandomFactions )
                        continue; //don't show the player these factions at all
                    
                    if ( hideRandomFactionType )
                    {
                        Buffer.StartColor( faction.FactionCenterColor.ColorHexBrighter ).Add( "Random " ).Add( EnumNameCache.GetName( faction.RandomImpact ) ).EndColor();
                        factionDisplaysRandom = false;
                    }
                    else
                        Buffer.StartColor( faction.FactionCenterColor.ColorHexBrighter ).Add( faction.GetDisplayName() ).EndColor();
                }
                else
                    Buffer.StartColor( faction.FactionCenterColor.ColorHexBrighter ).Add( faction.GetDisplayName() ).EndColor();
                
                if ( faction.GetHasHadSomeException() )
                {
                    Buffer.StartColor( ColorMath.GetTimeLerpedColor( ColorMath.Red, ColorMath.Orange, 500 ) ).Add( " FATAL ERROR IN FACTION" ).EndColor();
                    Buffer.Add( "\n" );
                    continue;
                }
                
                if ( showDebugInfoInTooltip )
                    Buffer.Add( " " + faction.FactionIndex + ". (power " + faction.OverallPowerLevel.ReadableString + ")" );
                #region AI Sentinels
                if ( faction.Type == FactionType.AI )
                {
                    debugCode = 500;
                    AISentinelsCoreData sentinelsExt = faction.TryGetAISentinelsCoreData()?.SentinelInfo;
                    if ( sentinelsExt != null )
                    {
                        if (secretFactionDetails)
                        {
                            //Buffer.Add(" Secret Type ");
                        }
                        else
                        {
                            if ( sentinelsExt.WasRandomAIType && !showRandomAiType )
                            {
                                Buffer.Add( " Random: " );
                            }
                            else if ( sentinelsExt.AdaptiveAIDifficulty != TypeDifficulty.Unset )
                            {
                                if ( showRandomAiType )
                                    Buffer.Add( " Adaptive (" + sentinelsExt.AIType.DisplayName + "): " );
                                else
                                    Buffer.Add( " Adaptive " ).Add( EnumNameCache.GetName( sentinelsExt.AdaptiveAIDifficulty ) ).Add( ": " );
                            }
                            else
                                Buffer.Add( " " + sentinelsExt.AIType.DisplayName + ": " );

                            Buffer.Add( " " ).Add( sentinelsExt.AIDifficulty.DisplayName );
                        }
                    }
                    else
                        Buffer.Add( " ??? Type " );

                    if ( faction.FactionIsDefeated )
                    {
                        Buffer.Add( ". This faction has been defeated\n" );
                        continue;
                    }
                    if ( faction.InCivilWarMode )
                        Buffer.Add( " In Civil War." );
                    if ( showVerboseDetails )
                    {
                        bool hasAddedAny = false;
                        Buffer.StartColor( QuickColors.HeaderDull ).Add( "\n\tWave Types: " ).EndColor();
                        {
                            bool threatWavesOn = World_AIW2.Instance.Setup.GetBoolBySetting( "ThreatWave" );
                            bool directWavesOn = World_AIW2.Instance.Setup.GetBoolBySetting( "DirectWave" );
                            bool crossPlanetWavesOn = World_AIW2.Instance.Setup.GetBoolBySetting( "CrossPlanetWave" );
                            bool reconquestWaveOn = World_AIW2.Instance.Setup.GetBoolBySetting( "ReconquestWave" );

                            if ( threatWavesOn )
                            {
                                AddCommaIfNeeded( ref hasAddedAny, Buffer );
                                Buffer.Add( "Threat" );
                            }
                            if ( directWavesOn )
                            {
                                AddCommaIfNeeded( ref hasAddedAny, Buffer );
                                Buffer.Add( "Direct" );
                            }
                            if ( crossPlanetWavesOn )
                            {
                                AddCommaIfNeeded( ref hasAddedAny, Buffer );
                                Buffer.Add( "Cross Planet" );
                            }
                            if ( reconquestWaveOn )
                            {
                                AddCommaIfNeeded( ref hasAddedAny, Buffer );
                                Buffer.Add( "Reconquest" );
                            }

                            if ( !hasAddedAny )
                                Buffer.Add( "None." );
                            else
                                Buffer.Add( "." );
                        }
                    }

                    bool sharkA = World_AIW2.Instance.Setup.GetBoolBySetting( "SharkA" );
                    bool sharkB = World_AIW2.Instance.Setup.GetBoolBySetting( "SharkB" );

                    if ( sharkA )
                        Buffer.Add( " Shark Plot A enabled." );
                    if ( sharkB )
                        Buffer.Add( " Shark Plot B enabled." );

                    if ( showDebugInfoInTooltip && sentinelsExt != null )
                    {
                        ProtectedList<ExtragalacticBudget> budgets = sentinelsExt.ExtragalacticBudgets;
                        if ( budgets == null )
                            ArcenDebugging.ArcenDebugLogSingleLine( "budgets is null", Verbosity.DoNotShow );
                        bool initialNewline = false;
                        for ( int j = 0; j < budgets.Count; j++ )
                        {
                            if ( budgets[j] == null )
                                continue;
                            if ( budgets[j].PowerLevel > 0 )
                            {
                                if ( !initialNewline ) { Buffer.Add( "\n" ); initialNewline = true; } //just for formatting
                                Buffer.Add( "\t" );
                                budgets[j].AppendStateForInterfaceDisplay( Buffer );
                                Buffer.Add( "\n " );
                            }
                        }
                    }

                }
                #endregion end AI Sentinels
                debugCode = 600;
                if ( faction.SpecialFactionData.InternalName == "HunterFleet" )
                {
                    AIHunterCoreData hunterExt = faction.GetAISentinelsCoreData().HunterInfo;
                    Buffer.Add( " " + (hunterExt == null ? "???" : hunterExt.SubType.DisplayName) + ". " + (hunterExt == null ? "???" : hunterExt.AIDifficulty.DisplayName) + ". " );
                    if ( faction.InCivilWarMode )
                        Buffer.Add( " In Civil War." );

                }
                debugCode = 700;
                if ( faction.SpecialFactionData.InternalName == "AIWarden" )
                {
                    AIWardenCoreData wardenExt = faction.GetAISentinelsCoreData().WardenInfo;
                    Buffer.Add( " " + (wardenExt == null ? "???" : wardenExt.SubType.DisplayName) + ". " + (wardenExt == null ? "???" : wardenExt.AIDifficulty.DisplayName) + ". " );
                    if ( faction.InCivilWarMode )
                        Buffer.Add( " In Civil War." );

                }
                debugCode = 800;
                int intensity = faction.BaseInfo.GetDifficultyOrdinal_OrNegativeOneIfNotRelevant();
                int numberToSeed = faction.CustomData_NumberToSeed( false );
                
                //don't show this for the AI, because we already showed the equivalent for them
                if ( !secretFactionDetails &&
                     intensity > 0 && 
                     faction.Type != FactionType.AI )
                {
                    Buffer.Add( " (Strength " ).Add( intensity ).Add( ")" );
                }
                if ( faction.IsVassal )
                {
                    Buffer.Add( " (Vassal) ", "a1a1ff" );
                }
                if ( showDebugInfoInTooltip &&
                     showVerboseDetails &&
                     faction.BenefitsFromFimbulwinter )
                    Buffer.Add( " Fimbul ", "235589" );
                if ( !secretFactionDetails && numberToSeed > 0 )
                {
                    debugCode = 900;
                    string textToShow = "(Strength ";
                    bool printNothing = false;
                    if ( faction.SpecialFactionData.InternalName == "ZenithArchitrave" )
                    {
                        textToShow = "(Territory ";//we reuse this value to indicate the Territory of the ZA
                        if ( factionDisplaysRandom )
                            printNothing = true; //don't display this for random factions, since it makes it obvious its a ZA
                        if ( faction.GetBoolValueForCustomFieldOrDefaultValue( "CivilWarEnabled", true ) )
                        {
                            Buffer.Add( " Civil War Enabled " );
                        }

                    }
                    if ( !printNothing )
                        Buffer.Add( textToShow ).Add( numberToSeed ).Add( ")" );

                }
                debugCode = 1000;
                string allegiance = faction.BaseInfo.Allegiance;
                if ( !String.IsNullOrEmpty( allegiance ) )
                {
                    debugCode = 1100;
                    string allegianceColor = "";
                    if ( allegiance == "Allied To AI" )
                    {
                        Faction aiFaction = World_AIW2.Instance.AIFactions[0];
                        if ( aiFaction != null )
                            allegianceColor = aiFaction.FactionCenterColor.ColorHexBrighter;
                    }
                    if ( allegiance == "Friendly To Players" )
                    {
                        allegiance = "Friendly";
                        Faction playerFaction = World_AIW2.Instance.GetLocalPlayerFactionOrNull();
                        if ( playerFaction != null )
                            allegianceColor = playerFaction.FactionCenterColor.ColorHexBrighter;
                    }
                    if ( allegiance == "Hostile To All" )
                    {
                        allegiance = "Hostile To All";
                        allegianceColor = "dd5050";
                    }
                    if ( allegiance == "Dark Alliance" )
                    {
                        allegiance = "Dark Alliance";
                        allegianceColor = "e62495";
                    }

                    if ( allegiance == "Minor Faction Team Red" )
                    {
                        allegiance = "Team Red";
                        allegianceColor = "993030";
                    }
                    if ( allegiance == "Minor Faction Team Green" )
                    {
                        allegiance = "Team Green";
                        allegianceColor = "309930";
                    }
                    if ( allegiance == "Minor Faction Team Blue" )
                    {
                        allegiance = "Team Blue";
                        allegianceColor = "303090";
                    }
                    if ( allegianceColor == "" )
                        Buffer.Add( " (" ).Add( allegiance ).Add( ")" );
                    else
                        Buffer.Add( " (" ).Add( allegiance, allegianceColor ).Add( ")" );
                }
                if ( faction.GetBoolValueForCustomFieldOrDefaultValue( "DebugMode", false ) )
                    Buffer.Add( " Debug Mode " );
                if ( faction.Stage0Ticks > 0 )
                {
                    int min = 200;
                    Buffer.Add("\nPerf for ").Add( faction.GetTotalSquadCount() ).Add(" ships:\n");
                    if ( faction.Stage0Ticks > min )
                        Buffer.Add("\t1: " + faction.Stage0Ticks ).Add("\n");
                    if ( faction.Stage0Ticks > min )
                        Buffer.Add("\t1: " + faction.Stage1Ticks ).Add("\n");
                    if ( faction.Stage2Ticks > min )
                        Buffer.Add("\t2: " + faction.Stage2Ticks ).Add("\n");
                    if ( faction.Stage3Ticks > min )
                        Buffer.Add("\t3: " + faction.Stage3Ticks ).Add("\n");
                    if ( faction.LRPTicks > min )
                    {
                        Buffer.Add("\tLRP: " + faction.LRPTicks ).Add("\n");
                        if ( faction.UpdateFireteamsTicks > min )
                            Buffer.Add("\t\tFireteamUpdate: " + faction.UpdateFireteamsTicks ).Add("\n");
                        if ( faction.UpdateRegimentsTicks > min )
                            Buffer.Add("\t\tRegimentUpdate: " + faction.UpdateRegimentsTicks ).Add("\n");
                    }

                }
                debugCode = 1200;
                Buffer.Add( "\n" );
            }
            Buffer.Add( "\n" ).Add( "<size=85%><color=#d18444>Hold <color=#996f4c>" ).Add( InputActionTypeDataTable.Instance.GetHumanReadableKeyComboForAction( "IncreaseShipAndPlanetTooltipDetailBy1_Key1" ) ).Add( "</color> to see additional data about the factions.</color></size>\n" );
            } catch ( Exception e )
            {
                ArcenDebugging.LogSingleLine("Hit exceptions in WriteFactionsToBuffer debugCode " + debugCode + " " + e.ToString(), Verbosity.DoNotShow );
            }
        }
        private static void AddCommaIfNeeded( ref bool HasAddedBefore, ArcenDoubleCharacterBuffer Buffer )
        {
            if ( HasAddedBefore )
                Buffer.Add( ", " );
            else
                HasAddedBefore = true;
        }
    }
}
