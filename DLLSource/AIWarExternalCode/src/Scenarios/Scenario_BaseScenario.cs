using Arcen.AIW2.Core;
using System;
using Arcen.Universal;
using UnityEngine;
using System.Text;

namespace Arcen.AIW2.External
{
    public abstract class BaseScenario : IScenarioImplementation
    {
        public void ClearAllMyDataForQuitToMainMenuOrBeforeNewMap() 
        {
            //not super important
            RandomOptions.Clear(); 
            //might be important
            ClearAllMyDataForQuitToMainMenuOrBeforeNewMap_Inner();
        }
        protected abstract void ClearAllMyDataForQuitToMainMenuOrBeforeNewMap_Inner();

        public int GetPlayerDifficultyToRouteToPlanet( Planet planet )
        {
            return FactionUtilityMethods.Instance.GetPlayerDifficultyToRouteToPlanet( planet );
        }

        public virtual void DoPerSimStepLogic_OnMainThreadAndPartOfSim_HostOnly( ArcenHostOnlySimContext Context ) { }
        public virtual void DoPerSimStepLogic_OnMainThreadAndPartOfSim_ClientAndHost( ArcenClientOrHostSimContextCore Context ) { }
        public virtual bool GetShouldSkipGameSetup() { return false; }
        public virtual bool WriteCurrentOngoingMessageToDisplay(ArcenDoubleCharacterBuffer Buffer )
        {
            bool wroteTutorialStuff = false;
            if ( World_AIW2.Instance.TutorialOrNull != null )
            {
                wroteTutorialStuff = true;
                WriteTutorialHeader( Buffer, World_AIW2.Instance.TutorialStageIndex, World_AIW2.Instance.TutorialOrNull.Steps.Count, false );
                if ( World_AIW2.Instance.TutorialStageIndex < World_AIW2.Instance.TutorialOrNull.Steps.Count )
                {
                    Buffer.Add( "\n" );
                    Tutorial.TutorialStep step = World_AIW2.Instance.TutorialOrNull.Steps[World_AIW2.Instance.TutorialStageIndex];
                    Buffer.Add( step.CalculateTextToShow() );

                    bool areAllConditionsComplete = true;
                    for ( int i = 0; i < step.Conditions.Count; i++ )
                    {
                        if ( !step.Conditions[i].HasConditionBeenMet() )
                        {
                            areAllConditionsComplete = false;
                            break;
                        }
                    }
                    if ( areAllConditionsComplete )
                    {
                        Buffer.Add( "\n" );
                        if ( ArcenTime.IsOn_EveryHalfSecondToggle )
                            Buffer.StartColor( ColorMath.LightYellow );
                        else
                            Buffer.StartColor( ColorMath.LighterGreen );
                        Buffer.Add( "步骤完成：点击此处继续" );
                        Buffer.EndColor();
                    }
                }
                else
                {
                    if ( ArcenTime.IsOn_EveryHalfSecondToggle )
                        Buffer.StartColor( ColorMath.IceBlue );
                    else
                        Buffer.StartColor( ColorMath.LightBlue );
                    Buffer.Add( " 完成！" );
                    Buffer.Add( "\n</color>" );
                    Buffer.Add( "此教程已完成。点击此处返回教程选择界面！" );
                }
                return true;
            }
            
            if ( Engine_AIW2.Instance.PendingTargetedAction != null && 
                 Engine_AIW2.Instance.CurrentGameViewMode == GameViewMode.MainGameView )
            {
                Engine_AIW2.Instance.PendingTargetedAction.WriteStatusMessage(Buffer);
            }
            
            // jcf: these could all become ITargetedInputAction
            if ( !Engine_AIW2.Instance.PlacingDirectBuildable.GetIsNull() && 
                 Engine_AIW2.Instance.CurrentGameViewMode == GameViewMode.MainGameView )
            {
                DirectBuildable buildable = Engine_AIW2.Instance.PlacingDirectBuildable;
                if ( wroteTutorialStuff )
                    Buffer.Add( "\n\n" );
                //we're in placement mode!
                int metalCost = buildable.TypeData.MarkStatsFor( buildable.EffectiveMark ).MetalCost;
                if ( metalCost == 0 )
                    metalCost++;
                Buffer.Add( "<color=#ffc178><b>放置 " ).Add( buildable.TypeData.DisplayName ).Add( "</b></color>。 ");
                bool printMetalCosts = true;
                Faction localPlayerFaction = World_AIW2.Instance.GetLocalPlayerFactionOrNull();
                if ( NecromancerEmpireFactionBaseInfo.GetIsThisANecromancerFaction( localPlayerFaction ) )
                    printMetalCosts = false;
                if ( printMetalCosts )
                {
                    Buffer.Add("<color=#ccccee>" ).Add( ArcenExternalUIUtilities.MetalTextColorAndIcon );
                    if ( metalCost >= 1000 )
                        ArcenExternalUIUtilities.WriteRoundedNumberWithSuffix( Buffer, metalCost, true, false );
                    else
                        Buffer.Add( metalCost );
                    Buffer.Add("</color>\n");
                }
                else
                    Buffer.Add("\n");


                Buffer.Add( "<color=#999999>点击放置。\n");

                Buffer.Add("按住 " ).Add( InputActionTypeDataTable.Instance.GetHumanReadableKeyComboForAction( "Build5xUnits" ) ).Add( " 一次建造 5 个。 " );

                if ( printMetalCosts )
                {
                    Buffer.Add("<color=#ccccee>").Add( ArcenExternalUIUtilities.MetalTextColorAndIcon );
                    if ( metalCost * 5 >= 1000 )
                        ArcenExternalUIUtilities.WriteRoundedNumberWithSuffix( Buffer, metalCost * 5, true, false );
                    else
                        Buffer.Add( (metalCost * 5) );
                    Buffer.Add("</color>\n");
                }
                else
                    Buffer.Add("\n");

                Buffer.Add("按住 ").Add( InputActionTypeDataTable.Instance.GetHumanReadableKeyComboForAction( "Build10xUnits" ) ).Add( " 一次建造 10 个。 ");
                if ( printMetalCosts )
                {
                    Buffer.Add("<color=#ccccee>").Add( ArcenExternalUIUtilities.MetalTextColorAndIcon );
                    if ( metalCost * 10 >= 1000 )
                        ArcenExternalUIUtilities.WriteRoundedNumberWithSuffix( Buffer, metalCost * 10, true, false );
                    else
                        Buffer.Add( (metalCost * 10) );

                    Buffer.Add("</color>\n");
                }
                else
                    Buffer.Add("\n");

                Buffer.Add("同时按住可一次建造 50 个。</color>");

                if ( printMetalCosts )
                {
                    Buffer.Add("<color=#ccccee>" ).Add( ArcenExternalUIUtilities.MetalTextColorAndIcon );
                    if ( metalCost * 50 >= 1000 )
                        ArcenExternalUIUtilities.WriteRoundedNumberWithSuffix( Buffer, metalCost * 50, true, false );
                    else
                        Buffer.Add( (metalCost * 50) );
                    Buffer.Add("</color>\n");
                }
                else
                    Buffer.Add("\n");

            }
            if ( Engine_AIW2.Instance.IsInPingLocationMode )
            {
                if ( wroteTutorialStuff )
                    Buffer.Add( "\n\n" );

                string headerColor = string.Empty;
                string bodyColor = string.Empty;
                string colorName = string.Empty;

                PlanetPingColor pingColor = InputCaching.CalculatePlanetPingColor();
                switch ( pingColor )
                {
                    case PlanetPingColor.Orange:
                        headerColor = "ffb142";
                        bodyColor = "ca9447";
                        colorName = "orange";
                        break;
                    case PlanetPingColor.Blue:
                        headerColor = "4292ff";
                        bodyColor = "5084ca";
                        colorName = "blue";
                        break;
                    case PlanetPingColor.Pink:
                        headerColor = "f46acb";
                        bodyColor = "c267a7";
                        colorName = "pink";
                        break;
                    case PlanetPingColor.Yellow:
                        headerColor = "fff661";
                        bodyColor = "dad46d";
                        colorName = "yellow";
                        break;
                    case PlanetPingColor.Green:
                    default:
                        headerColor = "42ff9f";
                        bodyColor = "77ae92";
                        colorName = "green";
                        break;
                }

                //we're in ping mode!
                if ( Engine_AIW2.Instance.CurrentGameViewMode == GameViewMode.MainGameView )
                {
                    Buffer.Add( "<color=#" ).Add( headerColor ).Add( "><b>标记模式</b></color>：" );
                    Buffer.Add( "<color=#" ).Add( bodyColor ).Add( ">左键点击留下 " ).Add( colorName ).Add( " 标记点，想放多少放多少（每个持续六秒，随时间缩小）。右键退出此模式。</color>" );
                }
                else
                {
                    Buffer.Add( "<color=#" ).Add( headerColor ).Add( "><b>标记模式</b></color>：" );
                    Buffer.Add( "<color=#" ).Add( bodyColor ).Add( ">左键点击留下 " ).Add( colorName ).Add( " 标记点在任意星球上。它们可见 6 秒。右键退出此模式。</color>" );
                }
                Buffer.Add( "\n点击时按住：" );
                Buffer.Add( "<color=#ffb142>" ).Add( InputActionTypeDataTable.Instance.GetHumanReadableKeyComboForAction( "HoldForOrangePings" ) ).Add( "</color>、" );
                Buffer.Add( "<color=#4292ff>" ).Add( InputActionTypeDataTable.Instance.GetHumanReadableKeyComboForAction( "HoldForBluePings" ) ).Add( "</color>、" );
                Buffer.Add( "<color=#f46acb>" ).Add( InputActionTypeDataTable.Instance.GetHumanReadableKeyComboForAction( "HoldForPinkPings" ) ).Add( "</color>、或" );
                Buffer.Add( "<color=#fff661>" ).Add( InputActionTypeDataTable.Instance.GetHumanReadableKeyComboForAction( "HoldForYellowPings" ) ).Add( "</color>。" );
            }
            
            if ( Engine_AIW2.Instance.PlacingOutguardDeployable != null && 
                 Engine_AIW2.Instance.CurrentGameViewMode == GameViewMode.MainGameView )
            {
                var outguard = Engine_AIW2.Instance.PlacingOutguardDeployable;
                if ( wroteTutorialStuff )
                    Buffer.Add( "\n\n" );

                Buffer.Add( "<color=#ffc178><b>部署 " ).Add( outguard.DisplayName ).Add( "</b></color>。 ");
                Buffer.Add("\n");
                Buffer.Add( "<color=#999999>点击部署。\n");
            }

            if ( World_AIW2.Instance.TutorialOrNull != null && World.Instance.IsPaused )
                Buffer.Add( "<color=#ffd75e>游戏已暂停！</color>\n" );

            return false;
        }

        public static void WriteTutorialHeader( ArcenDoubleCharacterBuffer Buffer, int Index, int MaxIndex, bool IncludeCompleteNotice )
        {
            if ( Index > MaxIndex )
                Index = MaxIndex;

            Buffer.Add( "<b><color=#78beff>T</color><color=#6db9ff>u</color><color=#5eb1ff>t</color><color=#5ec4ff>o</color><color=#52c0ff>r</color><color=#52d8ff>i</color><color=#42d5ff>a</color><color=#24deff>l</color><color=#78beff>" );
            if ( Index < MaxIndex )
            {
                Buffer.Add( " 步骤 " ).Add( Index + 1 ).Add( "/" ).Add( MaxIndex );
                if ( IncludeCompleteNotice )
                    Buffer.Add( "（完成）" );
                Buffer.Add( "</b>:</color>" );
            }
            else
                Buffer.Add( "</b>:</color>" );
        }

        private bool IsFriendlyTowardAnyPlayer(Faction faction)
        {
            for ( int i = 0; i < World_AIW2.Instance.Factions.Count; i++ )
            {
                Faction otherFaction = World_AIW2.Instance.Factions[i];
                if ( faction == otherFaction )
                    continue;
                if(otherFaction.Type == FactionType.Player && faction.GetIsFriendlyTowards(otherFaction))
                    return true;
            }
            return false;
        }

        public virtual bool IsTutorial()
        {
            return false;
        }

        public void DoOnSpawnsOnDeath_AfterFullDeathOrPartOfStackDeath_HostOnly( GameEntity_Squad dyingEntity, GameEntity_Squad oneOfTheSpawningEntities, ArcenHostOnlySimContext Context )
        {
            
        }
        public virtual void HandleAdditionalFactionAdds( List<ConfigurationForFaction> CurrentFactions )
        {
            bool debug = false;
            if ( debug )
                ArcenDebugging.ArcenDebugLogSingleLine("handle additional faction adds", Verbosity.DoNotShow );

            for (int i = CurrentFactions.Count - 1; i >= 0; i--) //count backwards so we can remove elements
            {
                ConfigurationForFaction config = CurrentFactions[i];
                if (config.SpecialFactionData == null)//if a quick start loaded without an expansion, for instance
                    continue;
                if (!config.SpecialFactionData.AddsCivilWarScourge)
                    continue;
                //we need to add one civil war faction per AI
                bool allowScourge = true;
                bool allowDarkZenith = false;
                int intensity = 5;
                for (int j = 0; j < config.SpecialFactionData.CustomFields.Count; j++)
                {
                    DataForFaction_CustomFieldDefinition field = config.SpecialFactionData.CustomFields[j];
                    string fieldName = field.InternalName;
                    string fieldValue = config.GetStringValueForCustomFieldOrDefaultValue(fieldName, true);
                    if ( debug )
                        ArcenDebugging.LogSingleLine("Field value for " + fieldName + ": " + fieldValue, Verbosity.DoNotShow );
                    if ( field.InternalName == "AllowDarkZenith" && fieldValue == "1" )
                        allowDarkZenith = true;
                    if ( field.InternalName == "OnlyDarkZenith" && fieldValue == "1" )
                    {
                        allowScourge = false;
                        allowDarkZenith = true;
                    }

                    if (field.InternalName == "Intensity")
                        intensity = Int32.Parse(fieldValue);
                }
                if ( debug )
                {
                    ArcenDebugging.LogSingleLine("allowed to add scourge: " + allowScourge + " dark zenith? " + allowDarkZenith, Verbosity.DoNotShow );
                }
                for (int j = CurrentFactions.Count - 1; j >= 0; j--)
                {
                    ConfigurationForFaction potentialAIConfig = CurrentFactions[j];
                    if (potentialAIConfig.SpecialFactionData == null)
                        continue;
                    if (potentialAIConfig.SpecialFactionData.DisplayName == "AI Sentinels")
                    {
                        bool useDarkZenith = false;
                        bool useScourge = false;
                        if ( allowScourge && !allowDarkZenith )
                            useScourge = true;
                        else if ( !allowScourge && allowDarkZenith )
                            useDarkZenith = true;
                        else
                        {
                            int random = Engine_Universal.PermanentQualityRandom.Next(0, 100 );
                            if ( random < 50 )
                                useScourge = true;
                            else
                                useDarkZenith = true;
                        }
                        AddScourgeCivilWarFaction(CurrentFactions, useScourge, useDarkZenith, intensity, debug);
                    }
                }
            }
            return;
        }
        private void AddScourgeCivilWarFaction( List<ConfigurationForFaction> CurrentFactions, bool useScourge, bool useDarkZenith, int intensity, bool debug )
        {
            SpecialFactionData dataToAdd = null;
            for ( int i = 0; i < SpecialFactionDataTable.Instance.Rows.Count; i++)
            {
                if ( useScourge && SpecialFactionDataTable.Instance.Rows[i].InternalName == "Scourge" )
                    dataToAdd = SpecialFactionDataTable.Instance.Rows[i];
                else if ( useDarkZenith && SpecialFactionDataTable.Instance.Rows[i].InternalName == "DarkZenithSvikari" )
                    dataToAdd = SpecialFactionDataTable.Instance.Rows[i];
            }
            if ( dataToAdd == null )
                throw new Exception("Could not find scourge civil war faction");
            ConfigurationForFaction config = ConfigurationForFaction.Create( CurrentFactions.Count, "ScourgeCivilWar", dataToAdd );
            config.ShouldNeverBeRetainedInLobby = true; //remove these "extra random" factions from the game lobby when reopened
            config.SetCustomFieldValue( "Intensity", intensity.ToString() );
            config.SetCustomFieldValue( "Allegiance", "Civil War" );

            config.FactionCenterColor = TeamColorDefinitionTable.Instance.GetRandomRow();
            if ( debug )
                ArcenDebugging.ArcenDebugLogSingleLine("\tAdding  " + dataToAdd.InternalName + " " + config.GetStringValueForCustomFieldOrDefaultValue( "Impact", false ) + ", " + config.GetStringValueForCustomFieldOrDefaultValue( "Allegiance", false ) + ", " + config.FactionCenterColor, Verbosity.DoNotShow );
            CurrentFactions.Add( config );
        }

        public static DrawBag<KeyValuePair <SpecialFactionData, int>> RandomOptions = DrawBag<KeyValuePair<SpecialFactionData, int>>.Create_WillNeverBeGCed( 200, "BaseScenario-RandomOptions" );
        public void HandleRandomFactions( List<ConfigurationForFaction> CurrentFactions )
        {
            //iterate over all requested special factions; if any of them are "Random" then replace them with
            //a randomly chosen faction, but mark that new faction as "this was randomly chosen" for later
            bool debug = false;
            if ( debug )
                ArcenDebugging.ArcenDebugLogSingleLine("handle random factions", Verbosity.DoNotShow );

            //First, we see if the player has requested "Additional Randomness"
            string extraRandomness = World_AIW2.Instance.Setup.GetStringBySetting( "ExtraFactionRandomness" );
            if ( extraRandomness != "None" && extraRandomness != null && extraRandomness.Length > 0 )
                AddExtraFactionRandomness( CurrentFactions, extraRandomness );
            for ( int i = CurrentFactions.Count - 1; i >= 0 ; i-- ) //count backwards so we can remove elements
            {
                ConfigurationForFaction config = CurrentFactions[i];
                if ( config.SpecialFactionData == null )//if a quick start loaded without an expansion, for instance
                    continue;
                if ( !config.SpecialFactionData.IsRandomFaction )
                {
                    if ( debug )
                        ArcenDebugging.ArcenDebugLogSingleLine("Skipping " + config.SpecialFactionData.InternalName, Verbosity.DoNotShow );
                    continue;
                }
                if ( debug )
                    ArcenDebugging.ArcenDebugLogSingleLine("Parsing faction config " + i, Verbosity.DoNotShow );
                //if we have a random faction, lets build some lookups in the SpecialFactionDataTable
                //that we can do random searches for
                string workingAllegiance = "";
                string allegianceToCopyToNewFaction = "";
                TypeDifficulty impact = TypeDifficulty.Unset;
                List<SpecialFactionData> availableFactions = null;
                bool allegianceOverride = false;
                for ( int j = 0; j < config.SpecialFactionData.CustomFields.Count; j++ )
                {
                    DataForFaction_CustomFieldDefinition field = config.SpecialFactionData.CustomFields[j];
                    string fieldName = field.InternalName;
                    string fieldValue = config.GetStringValueForCustomFieldOrDefaultValue( fieldName, true );
                    if ( field.InternalName == "Impact" )
                    {
                        if ( debug )
                            ArcenDebugging.ArcenDebugLogSingleLine("\t Player-requested impact " + fieldValue, Verbosity.DoNotShow );
                        if( fieldValue.Equals("any", StringComparison.OrdinalIgnoreCase) )
                            impact = TypeDifficulty.Any;
                        else if( fieldValue.Equals("easier", StringComparison.OrdinalIgnoreCase) || fieldValue.Equals("easy", StringComparison.OrdinalIgnoreCase) ) //be generous with the check
                            impact = TypeDifficulty.Easier;
                        else if ( fieldValue.Equals("moderate", StringComparison.OrdinalIgnoreCase) )
                            impact = TypeDifficulty.Moderate;
                        else if ( fieldValue.Equals("hard", StringComparison.OrdinalIgnoreCase) )
                            impact = TypeDifficulty.Hard;
                        else if ( fieldValue.Equals("brutal", StringComparison.OrdinalIgnoreCase) )
                            impact = TypeDifficulty.Brutal;
                        else
                            throw new Exception("Could not parse faction impact " + fieldValue + " for " + config.SpecialFactionData.InternalName );
                    }
                    if ( field.InternalName == "RandomFactionType" )
                    {
                        if ( debug )
                            ArcenDebugging.ArcenDebugLogSingleLine(field.InternalName + " -> " + fieldValue, Verbosity.DoNotShow );
                        RandomFactionType randomType = RandomFactionTypeTable.Instance.GetRowByName(fieldValue);
                        if ( randomType.AlwaysHostileToAll )
                        {
                            workingAllegiance = "Hostile to Player";
                            allegianceToCopyToNewFaction = "Hostile to Player";
                            allegianceOverride = true;
                        }
                        availableFactions = randomType.IncludedFactions;
                    }

                    if ( field.InternalName == "Allegiance" && !allegianceOverride )
                    {
                        if ( debug )
                            ArcenDebugging.ArcenDebugLogSingleLine("\t Player requested allegiance: " + fieldValue, Verbosity.DoNotShow );
                        workingAllegiance = fieldValue;
                        allegianceToCopyToNewFaction = fieldValue;
                    }
                }
                if ( availableFactions == null )
                    throw new Exception("Could not find any random factions?");
                RandomOptions.Clear();
                SpecialFactionData randomlyChosenFactionData = null;
                int randomlyChosenIntensity = 5;

                if ( workingAllegiance == "Random" )
                {
                    int random = Engine_Universal.PermanentQualityRandom.Next(0, 100 );
                    if ( debug )
                        ArcenDebugging.ArcenDebugLogSingleLine("Randomly choosing working allegiance with " + random, Verbosity.DoNotShow );
                    if ( random < 33 )
                        workingAllegiance = "Friendly To Players";
                    else if ( random < 66 )
                        workingAllegiance = "Minor Faction Allied";
                    else
                        workingAllegiance = "Hostile to Player";
                }

                for ( int j = 0; j < availableFactions.Count; j++ )
                {
                    //find the available factions for this combination of possibilities
                    //use the above list of possibilities
                    SpecialFactionData data = availableFactions[j];
                    if ( debug )
                        ArcenDebugging.ArcenDebugLogSingleLine("Parsing eligible random faction " + i + ": " + data.InternalName + " CanBeOnMinorFactionTeam " + data.CanBeOnMinorFactionTeam + " MustBeAtMostOne " + data.MustBeAtMostOne + " required allegiance " + workingAllegiance, Verbosity.DoNotShow );

                    if ( data.MustBeAtMostOne || data.CanNotBeRandomIfAlreadyInGame )
                    {
                        //check the current factions to make sure we don't get two of the same thing
                        bool foundThisAlready = false;
                        for ( int k = 0; k < CurrentFactions.Count; k++ )
                        {
                            if ( CurrentFactions[k].SpecialFactionData.InternalName == data.InternalName )
                            {
                                foundThisAlready = true;
                                break;
                            }
                        }
                        if ( foundThisAlready )
                        {
                            if ( debug )
                                ArcenDebugging.ArcenDebugLogSingleLine("\tWe have this already, and you're only allowed one", Verbosity.DoNotShow );
                            continue;
                        }
                    }
                    if ( data.MustBeAtMostX > 0 )
                    {
                        //check the current factions to make sure we don't get two of the same thing
                        int countFoundAlready = 0;
                        for ( int k = 0; k < CurrentFactions.Count; k++ )
                        {
                            if ( CurrentFactions[k].SpecialFactionData.InternalName == data.InternalName )
                            {
                                countFoundAlready++;
                                if ( countFoundAlready > data.MustBeAtMostX )
                                    break;
                            }
                        }
                        if ( countFoundAlready > data.MustBeAtMostX )
                        {
                            if ( debug )
                                ArcenDebugging.ArcenDebugLogSingleLine( "\tWe have " + countFoundAlready + " already, and you're only allowed " + data.MustBeAtMostX, Verbosity.DoNotShow );
                            continue;
                        }
                    }
                    if ( CurrentFactions.Count > 200 )
                    {
                        if ( debug )
                            ArcenDebugging.ArcenDebugLogSingleLine( "\tThere are too many factions in the game to add any more.", Verbosity.DoNotShow );
                        continue;
                    }

                    if ( workingAllegiance == "Friendly To Players" && ! data.CanBeFriendlyToPlayer )
                    {
                        if ( debug )
                            ArcenDebugging.ArcenDebugLogSingleLine("\tFriendly Mismatch", Verbosity.DoNotShow );
                        continue;
                    }
                    if ( workingAllegiance == "Minor Faction Team Green" ||
                         workingAllegiance == "Minor Faction Team Red" ||
                         workingAllegiance == "Minor Faction Team Blue" ||
                         workingAllegiance.Contains( "Minor Faction Allied"  ) )
                    {
                        if ( !data.CanBeOnMinorFactionTeam )
                        {
                            if ( debug )
                                ArcenDebugging.ArcenDebugLogSingleLine("\tMinor Faction Team Mistmatch", Verbosity.DoNotShow );
                            continue;
                        }
                        if ( data.RequiresMinorFactionTeammates )
                        {
                            //check if we have any minor faction teams already
                            bool foundTeammates = false;
                            for ( int k = 0; k < CurrentFactions.Count; k++ )
                            {
                                ConfigurationForFaction testConfig = CurrentFactions[k];
                                if ( testConfig == null || testConfig.GetStringValueForCustomFieldOrDefaultValue( "Allegiance", false ) == null ||
                                     testConfig.GetStringValueForCustomFieldOrDefaultValue( "Allegiance", false ) == "")
                                    continue;
                                if ( testConfig.GetStringValueForCustomFieldOrDefaultValue( "Allegiance", true ).Contains( "Minor Faction Team" ) &&
                                     !testConfig.SpecialFactionData.RequiresMinorFactionTeammates )
                                    foundTeammates = true;
                            }
                            if (!foundTeammates )
                            {
                                if ( debug )
                                    ArcenDebugging.ArcenDebugLogSingleLine("skip, unable find teammates", Verbosity.DoNotShow );
                                continue;
                            }
                        }
                    }
                    if ( workingAllegiance == "Hostile to Player" && !(data.CanBeAlliedToAI || data.CanBeHostileToAll ) )
                    {
                        if ( debug )
                            ArcenDebugging.ArcenDebugLogSingleLine("\tHostile Mismatch", Verbosity.DoNotShow );
                        continue;
                    }

                    //note there's no check for "Random" allegiance since that doesn't rule anything out

                    //At this point, we have a potentially legal random faction (it matches the allegiance criteria)
                    //Now we add the appropriate intensities based on the TypeDifficulty criteria (note this can mean "Add nothing")
                    AddIntensitiesForFaction( impact, data ) ;
                }
                if ( !RandomOptions.GetHasItems() )
                {
                    throw new Exception("No possible randomly chosen factions exist for this choice. This is almost certainly a bug");
                }
                // if ( debug )
                // {
                //     ArcenDebugging.ArcenDebugLogSingleLine("For the requested allegiance " + workingAllegiance + " and impact " + impact.ToString() + " you have " + RandomOptions.InternalListSize + " entries in the drawbag:", Verbosity.DoNotShow );
                //     for ( int j = 0; j < RandomOptions.InternalListSize; j++ )
                //     {
                //         KeyValuePair<SpecialFactionData, int> logpair = RandomOptions.GetInternalListItemAtIndex(j);
                //         ArcenDebugging.ArcenDebugLogSingleLine(j + ": " + logpair.Key.InternalName + " at intensity " + logpair.Value, Verbosity.DoNotShow );
                //     }
                // }
                //change the output parameter to be a lookup pair
                KeyValuePair<SpecialFactionData, int> pair = RandomOptions.PickRandomItemAndReplace( Engine_Universal.PermanentQualityRandom );
                randomlyChosenFactionData = pair.Key;
                randomlyChosenIntensity = pair.Value;
                //I had a report that we were getting a 0 intensity, so put in some paranoid checking.
                if ( randomlyChosenIntensity <= 0 )
                    randomlyChosenIntensity = 0;
                if ( randomlyChosenIntensity > 10 )
                    randomlyChosenIntensity = 10;
                if ( debug )
                    ArcenDebugging.ArcenDebugLogSingleLine("*** Randomly chose " + randomlyChosenFactionData.InternalName + " at " + randomlyChosenIntensity + " now update config fields.", Verbosity.DoNotShow );
                randomlyChosenFactionData.WasRandomlyChosenFaction = true;

                //now figure out the other fields
                config.SpecialFactionData = randomlyChosenFactionData;
                config.RandomlyChosenImpact = impact;
                config.RandomlyChosenAllegiance = allegianceToCopyToNewFaction;

                for ( int j = 0; j < config.SpecialFactionData.CustomFields.Count; j++ )
                {
                    DataForFaction_CustomFieldDefinition field = config.SpecialFactionData.CustomFields[j];
                    string fieldName = field.InternalName;
                    if ( debug )
                        ArcenDebugging.ArcenDebugLogSingleLine("\t\tConsidering updating customfileddefinitions for the randomly chosen faction. " + fieldName + "(previous value " + config.GetIntValueForCustomFieldOrDefaultValue( fieldName, false ) + "). workingAllegiance " + workingAllegiance + ". intensity " + randomlyChosenIntensity, Verbosity.DoNotShow );
                    if ( fieldName == "NumberToSeed" || fieldName == "Intensity" )
                        config.SetCustomFieldValue( fieldName, randomlyChosenIntensity.ToString() );
                    //update the settings on the new faction
                    if ( fieldName == "Allegiance" )
                    {
                        if (workingAllegiance == "Friendly To Players" )
                        {
                            config.SetCustomFieldValue( fieldName, "Friendly To Players" );
                        }
                        if (workingAllegiance == "Hostile to Player" ) 
                        {
                            //we want hostile to player
                            if ( config.SpecialFactionData.CanBeDarkAlliance )
                            {
                                if ( Engine_Universal.PermanentQualityRandom.Next(0, 100 ) < 65 )
                                    config.SetCustomFieldValue( fieldName, "Dark Alliance" ); //prefer to be Dark Alliance
                                else
                                    config.SetCustomFieldValue( fieldName, "Hostile To All" ); //but somtimes just hostile to all (which we know is an option)
                            }
                            else if ( config.SpecialFactionData.CanBeAlliedToAI && config.SpecialFactionData.CanBeHostileToAll )
                            {
                                if ( Engine_Universal.PermanentQualityRandom.Next(0, 100 ) < 50 )
                                    config.SetCustomFieldValue( fieldName, "Allied To AI" );
                                else
                                    config.SetCustomFieldValue( fieldName, "Hostile To All" );
                            }
                            else if ( config.SpecialFactionData.CanBeAlliedToAI )
                                config.SetCustomFieldValue( fieldName, "Allied To AI" );
                            else
                                config.SetCustomFieldValue( fieldName, "Hostile To All" );
                        }
                        if ( workingAllegiance == "Minor Faction Allied" )
                        {
                            int percentChanceRed, percentChanceGreen, percentChanceBlue;
                            GetTeamPercentages (CurrentFactions, out percentChanceRed, out percentChanceGreen, out percentChanceBlue, randomlyChosenFactionData.RequiresMinorFactionTeammates);
                            if  ( debug )
                                ArcenDebugging.ArcenDebugLogSingleLine(" percent check: Red: " + percentChanceRed + " blue: " + percentChanceBlue + " green " + percentChanceGreen, Verbosity.DoNotShow );

                            if ( percentChanceGreen == 0 && percentChanceRed == 0 && percentChanceBlue == 0 )
                                throw new Exception("Couldn't find any valid teams to join??");
                            bool foundTeam = false;
                            int retries = 100;
                            do {
                                int random = Engine_Universal.PermanentQualityRandom.Next(0, 100 );
                                if ( random <= percentChanceRed )
                                {
                                    config.SetCustomFieldValue( fieldName, "Minor Faction Team Red" );
                                    foundTeam = true;
                                }
                                else if ( random <= percentChanceBlue + percentChanceRed )
                                {
                                    config.SetCustomFieldValue( fieldName, "Minor Faction Team Blue" );
                                    foundTeam = true;
                                }
                                else
                                {
                                    config.SetCustomFieldValue( fieldName, "Minor Faction Team Green" );
                                    foundTeam = true;
                                }
                            } while (retries-- > 0 && !foundTeam );
                        }
                    }
                    if ( debug )
                        ArcenDebugging.ArcenDebugLogSingleLine("\t\tAfter update, " + fieldName + " -> " + config.GetStringValueForCustomFieldOrDefaultValue( fieldName, false ), Verbosity.DoNotShow );
                }
            }
        }
        private void GetTeamPercentages ( List<ConfigurationForFaction> CurrentFactions,out int percentChanceRed, out int percentChanceGreen, out int percentChanceBlue, bool requireTeammates )
        {
            int red, blue, green;
            GetCurrentFactionsPerTeam(CurrentFactions, out red, out blue, out green, !requireTeammates);
            percentChanceRed = percentChanceGreen = percentChanceBlue = 0;
            bool debug = false;
            if ( debug )
                ArcenDebugging.ArcenDebugLogSingleLine("red: " + red + " blue " + blue + " green " + green + " requireTeammates " + requireTeammates, Verbosity.DoNotShow );
            if ( requireTeammates )
            {
                int bonusPercent = 0;
                if ( red == 0 )
                {
                    percentChanceRed = 0;
                    bonusPercent += 33;
                }
                if ( green == 0 )
                {
                    percentChanceGreen = 0;
                    bonusPercent += 33;
                }
                if ( blue == 0 )
                {
                    percentChanceBlue = 0;
                    bonusPercent += 33;
                }
                if ( red > 0 )
                {
                    percentChanceRed = 33 + bonusPercent;
                    bonusPercent = 0;
                }
                if ( green > 0 )
                {
                    percentChanceGreen = 33 + bonusPercent;
                    bonusPercent = 0;
                }
                if ( blue > 0 )
                {
                    percentChanceBlue = 33 + bonusPercent;
                    bonusPercent = 0;
                }
                return;
            }
            else
            {
                if ( red < 2 && green == 0 && blue == 0 )
                {
                    percentChanceRed = 100;
                    percentChanceBlue = 0;
                    percentChanceGreen = 0;
                }
                else if ( red >= 2 && green < 2 && blue < 2 )
                {
                    percentChanceRed = 0;
                    percentChanceBlue = 50;
                    percentChanceGreen = 50;
                }
                else if ( red < 2 && green >= 2 && blue < 2 )
                {
                    percentChanceRed = 50;
                    percentChanceBlue = 50;
                    percentChanceGreen = 0;
                }
                else if ( red < 2 && green < 2 && blue >= 2 )
                {
                    percentChanceRed = 50;
                    percentChanceBlue = 0;
                    percentChanceGreen = 0;
                }
                else
                {
                    percentChanceRed = 33;
                    percentChanceBlue = 33;
                    percentChanceGreen = 33;
                }
            }
        }
        
        private void GetCurrentFactionsPerTeam( List<ConfigurationForFaction> CurrentFactions, out int red, out int blue, out int green, bool includeFactionsRequiringTeammates)
        {
            red = blue = green = 0;
            for ( int k = 0; k < CurrentFactions.Count; k++ )
            {
                ConfigurationForFaction testConfig = CurrentFactions[k];
                if ( testConfig.GetStringValueForCustomFieldOrDefaultValue( "Allegiance", false ) == null ||
                     testConfig.GetStringValueForCustomFieldOrDefaultValue( "Allegiance", false ) == "" )
                    continue;
                if (! testConfig.GetStringValueForCustomFieldOrDefaultValue( "Allegiance", false ).Contains( "Minor Faction Team" ) )
                    continue;
                if ( !includeFactionsRequiringTeammates && testConfig.SpecialFactionData.RequiresMinorFactionTeammates )
                    continue;
                if ( testConfig.GetStringValueForCustomFieldOrDefaultValue( "Allegiance", false ).Contains( "Minor Faction Team Red" ) )
                    red++;
                if ( testConfig.GetStringValueForCustomFieldOrDefaultValue( "Allegiance", false ).Contains( "Minor Faction Team Blue" ) )
                    blue++;
                if ( testConfig.GetStringValueForCustomFieldOrDefaultValue( "Allegiance", false ).Contains( "Minor Faction Team Green" ) )
                    green++;
            }

        }
        private void AddIntensitiesForFaction( TypeDifficulty impact, SpecialFactionData data)
        {
            //update the RandomOptions draw bag
            int lowestAllowedIntensity = 0;
            int highestAllowedIntensity = 10;
            if ( impact == TypeDifficulty.Easier )
            {
                switch ( data.Impact )
                {
                case TypeDifficulty.Easier:
                    lowestAllowedIntensity = 1;
                    highestAllowedIntensity = 7;
                    break;
                case TypeDifficulty.Moderate:
                    lowestAllowedIntensity = 1;
                    highestAllowedIntensity = 4;
                    break;
                case TypeDifficulty.Hard:
                    lowestAllowedIntensity = -1;
                    highestAllowedIntensity = -1;
                    break;
                case TypeDifficulty.Brutal:
                    lowestAllowedIntensity = -1;
                    highestAllowedIntensity = -1;
                    break;
                }
            }
            if ( impact == TypeDifficulty.Moderate )
            {
                switch ( data.Impact )
                {
                case TypeDifficulty.Easier:
                    lowestAllowedIntensity = 4;
                    highestAllowedIntensity = 9;
                    break;
                case TypeDifficulty.Moderate:
                    lowestAllowedIntensity = 3;
                    highestAllowedIntensity = 6;
                    break;
                case TypeDifficulty.Hard:
                    lowestAllowedIntensity = 1;
                    highestAllowedIntensity = 4;
                    break;
                case TypeDifficulty.Brutal:
                    lowestAllowedIntensity = 1;
                    highestAllowedIntensity = 2;
                    break;
                }
            }
            if ( impact == TypeDifficulty.Hard )
            {
                switch ( data.Impact )
                {
                case TypeDifficulty.Easier:
                    lowestAllowedIntensity = 10;
                    highestAllowedIntensity = 10;
                    break;
                case TypeDifficulty.Moderate:
                    lowestAllowedIntensity = 7;
                    highestAllowedIntensity = 10;
                    break;
                case TypeDifficulty.Hard:
                    lowestAllowedIntensity = 5;
                    highestAllowedIntensity = 10;
                    break;
                case TypeDifficulty.Brutal:
                    lowestAllowedIntensity = 1;
                    highestAllowedIntensity = 5;
                    break;
                }
            }
            if ( impact == TypeDifficulty.Brutal )
            {
                switch ( data.Impact )
                {
                case TypeDifficulty.Easier:
                    lowestAllowedIntensity = -1;
                    highestAllowedIntensity = -1;
                    break;
                case TypeDifficulty.Moderate:
                    lowestAllowedIntensity = 10;
                    highestAllowedIntensity = 10;
                    break;
                case TypeDifficulty.Hard:
                    lowestAllowedIntensity = 8;
                    highestAllowedIntensity = 10;
                    break;
                case TypeDifficulty.Brutal:
                    lowestAllowedIntensity = 6;
                    highestAllowedIntensity = 10;
                    break;
                }
            }
            if ( lowestAllowedIntensity == -1 || highestAllowedIntensity == -1 )
                return;
            for ( int i = lowestAllowedIntensity; i <= highestAllowedIntensity; i++ )
            {
                KeyValuePair<SpecialFactionData, int> entry = new KeyValuePair<SpecialFactionData, int>( data, i );
                int numCopiesToInclude = 1;
                if ( impact == data.Impact )
                    numCopiesToInclude++;
                RandomOptions.AddItem(entry, numCopiesToInclude);
            }
        }
        public void AddExtraFactionRandomness ( List<ConfigurationForFaction> CurrentFactions, string extraRandomness )
        {
            bool debug = false;
            //the extra randomness influences the impact of the factions and how many there will be added
            int minToAdd = 3;
            int maxToAdd = 8;
            if ( extraRandomness == "Easy" )
            {
                minToAdd = 2;
                maxToAdd = 4;
            }
            else if ( extraRandomness == "Medium" )
            {
                minToAdd = 3;
                maxToAdd = 6;
            }
            else if ( extraRandomness == "Hard" )
            {
                minToAdd = 4;
                maxToAdd = 7;
            }
            else if ( extraRandomness == "Brutal" )
            {
                minToAdd = 4;
                maxToAdd = 8;
            }
            else if ( extraRandomness == "Brutal" )
            {
                minToAdd = 4;
                maxToAdd = 8;
            }
            else if ( extraRandomness == "Three Hard Factions" )
            {
                minToAdd = 3;
                maxToAdd = 3;
            }

            int factionsToAdd = Engine_Universal.PermanentQualityRandom.Next(minToAdd, maxToAdd );
            SpecialFactionData randomFactionData = null;
            for ( int i = 0; i < SpecialFactionDataTable.Instance.Rows.Count; i++)
            {
                if ( SpecialFactionDataTable.Instance.Rows[i].InternalName == "RandomFaction" )
                {
                    randomFactionData = SpecialFactionDataTable.Instance.Rows[i];
                    break;
                }
            }
            if ( randomFactionData == null )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "Asked for extra faction randomness of '" + extraRandomness + 
                    "', but no Random faction was found!  Do you have Zenith Onslaught installed and enabled?", Verbosity.DoNotShow );
                return;
            }
            string bonusAllegiances = World_AIW2.Instance.Setup.GetStringBySetting( "ExtraFactionAllegiances" );
            if ( debug )
                ArcenDebugging.ArcenDebugLogSingleLine("Adding " + factionsToAdd + " extra completely random factions, with setting " + extraRandomness + " and allegiances " + bonusAllegiances, Verbosity.DoNotShow );
            for ( int i = 0; i < factionsToAdd; i++ )
            {
                ConfigurationForFaction config = ConfigurationForFaction.Create( CurrentFactions.Count, "RandomFaction", randomFactionData );
                config.ShouldNeverBeRetainedInLobby = true; //remove these "extra random" factions from the game lobby when reopened
                config.SetCustomFieldValue( "Impact", GetRandomImpact(extraRandomness) );
                if ( bonusAllegiances == "Totally Random" )
                    config.SetCustomFieldValue( "Allegiance", "Random" );
                else if ( bonusAllegiances == "Hostile" )
                    config.SetCustomFieldValue( "Allegiance", "Hostile to Player" );
                else if ( bonusAllegiances == "Player Friendly" )
                    config.SetCustomFieldValue( "Allegiance", "Friendly To Players" );
                else if ( bonusAllegiances == "Balanced" )
                {
                    if ( i % 3 == 0 )
                        config.SetCustomFieldValue( "Allegiance", "Hostile to Player" );
                    if ( i % 3 == 1 )
                        config.SetCustomFieldValue( "Allegiance", "Friendly To Players" );
                    if ( i % 3 == 2 )
                        config.SetCustomFieldValue( "Allegiance", "Random" );
                }
                else
                    throw new Exception("Could not parse bonus allegiances " + bonusAllegiances);
                config.FactionCenterColor = TeamColorDefinitionTable.Instance.GetRandomRow();
                if ( debug )
                    ArcenDebugging.ArcenDebugLogSingleLine("\tAdding " + config.GetStringValueForCustomFieldOrDefaultValue( "Impact", false ) + ", " + config.GetStringValueForCustomFieldOrDefaultValue( "Allegiance", false ) + ", " + config.FactionCenterColor, Verbosity.DoNotShow );
                CurrentFactions.Add( config );
            }
        }
        public string GetRandomImpact(string extraRandomness)
        {
            if ( extraRandomness == "Totally Random" )
                return "any";
            int percentEasy = 20;
            int percentMedium = 30;
            int percentHard = 20;
            //int percentBrutal = 10;
            if ( extraRandomness == "Easy" )
            {
                percentEasy = 40;
                percentMedium = 30;
                percentHard = 20;
                //percentBrutal = 10;
            }
            if ( extraRandomness == "Medium" )
            {
                percentEasy = 30;
                percentMedium = 55;
                percentHard = 15;
                //percentBrutal = 15;
            }
            if ( extraRandomness == "Hard" )
            {
                percentEasy = 10;
                percentMedium = 30;
                percentHard = 40;
                //percentBrutal = 20;
            }
            if ( extraRandomness == "Brutal" )
            {
                percentEasy = 10;
                percentMedium = 30;
                percentHard = 30;
                //percentBrutal = 30;
            }
            if ( extraRandomness == "Three Hard Factions" )
            {
                percentEasy = 0;
                percentMedium = 0;
                percentHard = 100;
            }
            int random = Engine_Universal.PermanentQualityRandom.Next(0, 100 );
            if ( random < percentEasy )
                return "easier";
            else if ( random < percentMedium + percentEasy )
                return "moderate";
            else if ( random < percentHard + percentMedium + percentEasy)
                return "hard";
            else 
                return "brutal";
        }           
        
        public void DoOnFirstDeathLogic_OnlyAferFullStackDeath_HostOnly( GameEntity_Squad entity, DamageSource Damage, EntitySystem FiringSystemOrNull, 
            ArcenHostOnlySimContext Context )
        {
            //NOTE that this only happens after a FULL stack dies.  Individual items off a stack that die don't call this.
            //     We can change that if we need to at some point, but for now this makes the most sense.

            //Note that we now check for AIP increase in "do on any death" logic, since command centers do not stay the same entity
            //between killings
            if ( entity.TypeData.AIPOnDeathWhenNoneLeft != 0 ) this.CheckForAIPOnDeathWhenNoneLeft( entity, Context );
            if ( entity.TypeData.TriggerCivilWarWhenNoneLeft )
            {
                //ArcenDebugging.ArcenDebugLogSingleLine( "CIVIL_WAR_TEST: TriggerCivilWarWhenNoneLeft: " + entity.TypeData.DisplayName, Verbosity.DoNotShow );
                this.CheckForCivilWarOnDeathWhenNoneLeft( entity, Context );
            }
            ExternalFactionDeepInfo deepInfo = entity.GetFactionDeepInfoOrNull_Safe();
            if ( deepInfo != null )
                deepInfo.DoOnFirstDeathLogic_OnlyAferFullStackDeath_HostOnly( entity, Damage, FiringSystemOrNull, Context );
        }

        public virtual void DoOnAnyCrippleLogic_HostOnly( GameEntity_Squad entity, EntitySystem FiringSystemOrNull, ArcenHostOnlySimContext Context )
        {
            if ( Context == null ) //client
                return;
            if ( entity.GetFactionTypeSafe() != FactionType.Player )
                return; //only for player units

            Fleet entityFleet = entity.GetFleetOrNull_Safe();
            if ( entityFleet != null )
                entityFleet.TimesCrippled_UIOnly++;

            Faction facOrNull = entity.GetFactionOrNull_Safe();
            if ( facOrNull != null )
                facOrNull.Safe_DeepInfo_DoOnAnyCrippleLogic_MyFactionUnitsOnly_HostOnly( entity, FiringSystemOrNull, Context );

            DoBailOutChecksOnCripple_HostOnly(entity, FiringSystemOrNull, Context);
            DoCounterAttackChecksOnCripple_HostOnly(entity, FiringSystemOrNull, Context);

            // Audio responses
            if ( entity.TypeData.SpecialType == SpecialEntityType.MobileOfficerCombatFleetFlagship 
                 && entity.Planet.GetControllingFactionType() != FactionType.Player )
            {
                if ( entity.TypeData.IsGolem )
                    Engine_AIW2.Instance.PresentationLayer.PlaySoundByType( SoundPropagation.HostDirectedPlay, SFXItemType_NonPositional.PlayerGolemDestroyed );
                else
                    Engine_AIW2.Instance.PresentationLayer.PlaySoundByType( SoundPropagation.HostDirectedPlay, SFXItemType_NonPositional.AnyArkDestroyed );
            }

            if ( entity.TypeData.SpecialType == SpecialEntityType.BattlestationCitadel ||
                entity.TypeData.SpecialType == SpecialEntityType.CityCenter )
                Engine_AIW2.Instance.PresentationLayer.PlaySoundByType( SoundPropagation.HostDirectedPlay, SFXItemType_NonPositional.PlayerFortressDestroyed );
        }

        void DoCounterAttackChecksOnCripple_HostOnly( GameEntity_Squad entity, EntitySystem FiringSystemOrNull, ArcenHostOnlySimContext Context )
        {
            bool debug = false;

            //if this unit was crippled on an AI world, lets give the AI a bunch of Salvage
            if (entity.Planet.GetControllingFactionType() == FactionType.Player)
                return; //not for player planets

            //neutral planets generate less salvage
            FInt planetMultiplier = FInt.One;
            if ( entity.Planet.GetControllingFactionType() == FactionType.NaturalObject )
                planetMultiplier = ExternalConstants.Instance.FlagshipCounterAttackBudgetMultiplierNeutralPlanet;

            //figure out which AI faction to give salvage to
            Faction aiFaction = null;
            if ( entity.Planet.GetControllingFactionType() == FactionType.AI )
                aiFaction = entity.Planet.GetControllingFaction();
            else if ( FiringSystemOrNull != null && FiringSystemOrNull.ParentEntity.GetFactionTypeSafe() == FactionType.AI)
                aiFaction = FiringSystemOrNull.ParentEntity.GetFactionOrNull_Safe();
            if(aiFaction == null)
            {
                if(debug)
                {
                    ArcenDebugging.ArcenDebugLogSingleLine( "Flagship " + entity.TypeData.InternalName + " on " + entity.GetPlanetName_Safe() + " was killed by a non-AI faction on a neutral planet, so no counterattack budget\n", Verbosity.DoNotShow);
                }
                return;
            }

            AISentinelsCoreData sentinels = aiFaction.TryGetAISentinelsCoreData()?.SentinelInfo;
            if ( GlobalAIWorldBaseInfo.Instance.AIProgress_Effective < sentinels.AIDifficulty.AIPUnlockCounterattacks ) //Note that this is on a per-faction AIP basis, not the highest of any faction
            {
                if(debug && sentinels != null)
                {
                    ArcenDebugging.ArcenDebugLogSingleLine( "Flagship " + entity.TypeData.InternalName + " on " + entity.GetPlanetName_Safe() + " was killed but AIP too low to generate counterattacks. " + GlobalAIWorldBaseInfo.Instance.AIProgress_Effective + " < " + sentinels.AIDifficulty.AIPUnlockCounterattacks + ".\n", Verbosity.DoNotShow);
                }
                return;
            }
            FInt updateAmount = (FInt)entity.DataForMark.StrengthPerSquad_Original_DoesNotIncreaseWithMarkLevel;

            FInt flagshipTypeMultiplier = FInt.One;
            switch(entity.TypeData.SpecialType)
            {
                case SpecialEntityType.BattlestationBasic:
                    flagshipTypeMultiplier = ExternalConstants.Instance.FlagshipCounterAttackBudgetMultiplierBattlestationBasic;
                    break;
                case SpecialEntityType.BattlestationCitadel:
                    flagshipTypeMultiplier = ExternalConstants.Instance.FlagshipCounterAttackBudgetMultiplierCitadel;
                    break;
                case SpecialEntityType.MobileOfficerCombatFleetFlagship:
                    flagshipTypeMultiplier = ExternalConstants.Instance.FlagshipCounterAttackBudgetMultiplierCombat;
                    break;
                case SpecialEntityType.MobileStrikeCombatFleetFlagship:
                case SpecialEntityType.MobileCustomUnattachedFleetFlagship:
                case SpecialEntityType.MobileCustomCityFedFleetFlagship:
                    flagshipTypeMultiplier = ExternalConstants.Instance.FlagshipCounterAttackBudgetMultiplierFleet;
                    break;
                case SpecialEntityType.MobileSupportFleetFlagship:
                    flagshipTypeMultiplier = ExternalConstants.Instance.FlagshipCounterAttackBudgetMultiplierSupport;
                    break;
            }

            updateAmount *= flagshipTypeMultiplier;
            updateAmount *= planetMultiplier;
            if ( sentinels != null )
                updateAmount *= sentinels.AIDifficulty.BaseCounterattackMultiplier;
            if (entity.TypeData.CounterAttackBudgetOverrideMultiplier != FInt.One)
                updateAmount *= entity.TypeData.CounterAttackBudgetOverrideMultiplier;
            entity.Planet.AICounterattackUnspentBudget += updateAmount;
            
            if ( debug )
                ArcenDebugging.ArcenDebugLogSingleLine( "Flagship " + entity.TypeData.InternalName + " with strength "+ entity.GetStrengthPerSquad() + " * flagshipType " + flagshipTypeMultiplier + " * planetMultiplier " + planetMultiplier + " = " + updateAmount + " just was crippled on ai planet " + entity.GetPlanetName_Safe() + " set this-planet AICounterattackUnspentBudget to " + entity.Planet.AICounterattackUnspentBudget, Verbosity.DoNotShow );
        }

        #region DoBailOutChecksIfWasJustCrippled_HostOnly
        public void DoBailOutChecksOnCripple_HostOnly( GameEntity_Squad entity, EntitySystem FiringSystemOrNull, ArcenHostOnlySimContext Context )
        {
            if ( Context == null ) //client
                return;

            string originalEntityName = entity.TypeData.DisplayName;

            Faction myFaction = entity.GetFactionOrNull_Safe();

            if ( myFaction == null || entity.Planet == null )
                return;

            int hackingPointsLost = entity.TypeData.GetHackingPointsLostWhenCrippled();

            //only lose hacking points if you have them; don't go negative
            if ( hackingPointsLost > 0 && myFaction.StoredHacking > FInt.Zero )
            {
                //if there are not enough, take as many as you can
                if ( hackingPointsLost > myFaction.StoredHacking )
                    hackingPointsLost = myFaction.StoredHacking.GetNearestIntPreferringHigher();

                if ( hackingPointsLost > 0 )
                {
                    myFaction.StoredHacking -= hackingPointsLost;

                    HackingType hackToDo = HackingTypeTable.Instance.GetRowByName( "UnitWasCrippled" );
                    HackingEvent hackEvent = HackingEvent.Create( myFaction.FactionIndex, myFaction.FactionIndex, -1,
                            hackToDo, null, false, originalEntityName + " of " + entity.GetFleetName_Safe() +
                            " on " + entity.GetPlanetName_Safe(), hackingPointsLost );
                    hackEvent.HackingPointsSpent = (FInt)hackingPointsLost;
                    myFaction.HackingHistory.Add( hackEvent );
                }
            }

            string prefix = "<color=#" + myFaction.FactionCenterColor.ColorHexBrighter + ">" + myFaction.GetDisplayName() + ": </color>";
            string postfix = (hackingPointsLost <= 0 ? "." : " (<sprite=\"Res_Hack\" color=#3DE799><color=#3DE799>" + hackingPointsLost + " lost</color>)." );

            bool transformIntoBaseTransport = false;
            if ( entity.TypeData.SpecialType == SpecialEntityType.MobileOfficerCombatFleetFlagship )  {
                transformIntoBaseTransport = AIWar2GalaxySettingTable.GetIsBoolSettingEnabledByName_DuringGame( "OfficerPermadeath" );
            }

            if (transformIntoBaseTransport) {
                GameEntityTypeData baseTransport = GameEntityTypeDataTable.Instance.GetRowByNameOrNullIfNotFound("TransportFlagship_Starter");
                if (baseTransport == null) {
                    transformIntoBaseTransport = false;
                } else {
                    entity = entity.TransformInto(Context, baseTransport, 1, true);
                }
            }

            bool bailOutToAnyAllied = AIWar2GalaxySettingTable.GetIsBoolSettingEnabledByName_DuringGame( "MobileShipsBailOutToNearestFriendlyPlanetOnCrippling" );
            bool bailOutToAlliedHome = AIWar2GalaxySettingTable.GetIsBoolSettingEnabledByName_DuringGame( "MobileShipsBailOutToNearestHomePlanetOnCrippling" );
            if ( !bailOutToAnyAllied )
            {
                if ( entity.TypeData.ForcedToBailOutOnCripple_Any )
                {
                    bailOutToAnyAllied = true;
                }
                else if ( entity.TypeData.ForcedToBailOutOnCripple_DeepstrikeOnly )
                {
                    Planet plan = entity.Planet;
                    if ( plan != null && plan.IsEligibleForDeepStrike )
                        bailOutToAnyAllied = true;
                }
            }

            if ( entity.DataForMark.Speed <= 0 || (!bailOutToAnyAllied && !bailOutToAlliedHome) )
            {
                SquadViewChatHandlerBase chatHandlerOrNull = ChatClickHandler.CreateNewAs<SquadViewChatHandlerBase>( "ShipCrippled" );
                if ( chatHandlerOrNull != null )
                    chatHandlerOrNull.SquadToView = LazyLoadSquadWrapper.Create( entity );

                if (transformIntoBaseTransport) {
                    World_AIW2.Instance.QueueChatMessageOrCommand( prefix + originalEntityName + " of fleet " + entity.GetFleetName_Safe() +
                            " was crippled on " + entity.GetPlanetName_Safe()
                            + " beyond our ability to repair. Command transfered to recovered " + entity.TypeData.DisplayName
                            + postfix, ChatType.LogToCentralChat, chatHandlerOrNull );
                } else {
                    World_AIW2.Instance.QueueChatMessageOrCommand( prefix + originalEntityName + " of fleet " + entity.GetFleetName_Safe() +
                            " is now crippled on " + entity.GetPlanetName_Safe() + postfix, ChatType.LogToCentralChat, chatHandlerOrNull );
                }
                return;
            }

            Planet nearestAlliedPlanet = null;
            if ( bailOutToAlliedHome )
                nearestAlliedPlanet = entity.Planet.FindNearestAlliedKingPlanet_FairlyExpensive( myFaction );
            if ( nearestAlliedPlanet == null )
                nearestAlliedPlanet = entity.Planet.FindNearestAlliedPlanet_FairlyExpensive( myFaction );
            if ( nearestAlliedPlanet == null )
                nearestAlliedPlanet = entity.Planet.FindNearestAlliedOrNeutralPlanet_FairlyExpensive( myFaction );

            //entity.CurrentEngineStunSeconds = ExternalConstants.Instance.MaxEngineStun;
            //entity.CurrentWeaponAddedReloadSeconds = ExternalConstants.Instance.MaxWeaponAddedReloadSeconds;
            entity.CurrentParalysisSeconds = ExternalConstants.Instance.MaxParalysisTime;
            entity.Orders.ClearOrders( ClearBehavior.DoNotClearBehaviors, ClearDecollisionOnParent.YesClear_AndAlsoClearDecollisionMoveOrders,
                ClearSource.YesClearAnyOrders_IncludingFromHumans, "DoBailOutIfWasJustCrippled" );

            if ( nearestAlliedPlanet != null )
            {
                SquadViewChatHandlerBase chatHandlerOrNull = ChatClickHandler.CreateNewAs<SquadViewChatHandlerBase>( "ShipCrippled" );
                if ( chatHandlerOrNull != null )
                    chatHandlerOrNull.SquadToView = LazyLoadSquadWrapper.Create( entity );

                if (transformIntoBaseTransport) {
                    World_AIW2.Instance.QueueChatMessageOrCommand( prefix + originalEntityName + " of fleet " + entity.GetFleetName_Safe() +
                            " was crippled on " + entity.GetPlanetName_Safe()
                            + " beyond our ability to repair. Command transfered to " + entity.TypeData.DisplayName
                            + " on " + nearestAlliedPlanet.Name
                            + postfix, ChatType.LogToCentralChat, chatHandlerOrNull );
                } else {
                    World_AIW2.Instance.QueueChatMessageOrCommand( prefix + originalEntityName + " of fleet " + entity.GetFleetName_Safe() + " on " + entity.GetPlanetName_Safe() + " bailed out to "
                            + nearestAlliedPlanet.Name + postfix, ChatType.LogToCentralChat, chatHandlerOrNull );
                }
                entity.WarpToPlanetAtSafePointNearCommandStationIfPossible( nearestAlliedPlanet, Context, "Bailed Out At Cripple-Time!" );
            }
            else
            {
                SquadViewChatHandlerBase chatHandlerOrNull = ChatClickHandler.CreateNewAs<SquadViewChatHandlerBase>( "ShipCrippled" );
                if ( chatHandlerOrNull != null )
                    chatHandlerOrNull.SquadToView = LazyLoadSquadWrapper.Create( entity );

                if (transformIntoBaseTransport) {
                    World_AIW2.Instance.QueueChatMessageOrCommand( prefix + originalEntityName + " of fleet " + entity.GetFleetName_Safe() +
                            " was crippled on " + entity.GetPlanetName_Safe()
                            + " beyond our ability to repair. Command transfered to recovered " + entity.TypeData.DisplayName
                            + ", since they have no where else to go"
                            + postfix, ChatType.LogToCentralChat, chatHandlerOrNull );
                } else {
                    World_AIW2.Instance.QueueChatMessageOrCommand( prefix + originalEntityName + " of fleet " + entity.GetFleetName_Safe() +
                            " on " + entity.GetPlanetName_Safe() + " is now crippled, but has nowhere to which to bail out" + postfix, ChatType.LogToCentralChat, chatHandlerOrNull );
                }
            }
        }
        #endregion
            
        public virtual void DoOnAnyDeathLogic_HostOnly_AfterFullDeathOrPartOfStackDeath( 
            bool IsFromOnlyPartOfStackDying, GameEntity_Squad entity, 
            DamageSource Damage, EntitySystem FiringSystemOrNull, int NumShipsDying, ArcenHostOnlySimContext Context )
        {
            if ( entity == null )
                return;
            
            if ( ArcenNetworkAuthority.DesiredStatus == DesiredMultiplayerStatus.Client )
                return;

            int debugStage = 0;
            try
            {
                bool debug = false;
                bool aipDebug = false;
                
                //FiringSystemOrNull being null indicates this is a shot, not a ship. Or that someone just called entity.Die()
                // jcf: um, a shot has a firing system..

                debugStage = 100;

                if ( entity.TypeData.IsCommandStation )
                    BaseScenario.Helper_DoAutoDeathOnCommandStationDeath( entity, Context, FiringSystemOrNull );

                debugStage = 200;
                if ( debug )
                {
                    LOG.Msg("Entering DoAnyDeathLogic for {0}x {1} killed by {2}", 
                            NumShipsDying, entity.TypeData.InternalName, FiringSystemOrNull?.TypeData.InternalName_Longer ?? "[unknown]");
                }
                
                debugStage = 400;
                
                Faction factionDying = entity.PlanetFaction?.Faction;
                Faction factionKilling = null;
                PlanetFaction planetFactionKilling = null;
                
                planetFactionKilling = FiringSystemOrNull?.ParentEntity?.PlanetFaction;
                if (planetFactionKilling != null)
                    factionKilling = planetFactionKilling.Faction;
                
                debugStage = 500;

                // Update Metal Lost
                Faction localPlayerFaction = World_AIW2.Instance.GetLocalPlayerFactionOrNull();
                if ( localPlayerFaction != null )
                {
                    if ( factionDying.GetIsFriendlyTowards( localPlayerFaction ) )
                    {
                        entity.Planet.FriendlyMetalLost += NumShipsDying * entity.DataForMark.MetalCost;
                    }
                    else
                    {
                        entity.Planet.HostileMetalLost += NumShipsDying * entity.DataForMark.MetalCost;
                    }
                }

                Faction factionForRewards = GetPlayerFactionToGiveRewardToOrNull( factionKilling, entity, Context );

                // If the dying unit grants science, metal or hacking on death to a player, handle that here
                if (factionForRewards != null)
                {
                    if (!NecromancerEmpireFactionBaseInfo.GetIsThisANecromancerFaction( factionForRewards ))
                    {
                        if ( entity.TypeData.MetalToGrantOnDeath > 0 )
                            factionForRewards.StoredMetal += NumShipsDying * entity.TypeData.MetalToGrantOnDeath;
                        if ( entity.TypeData.HackingToGrantOnDeath > 0 )
                            factionForRewards.StoredHacking += NumShipsDying * entity.TypeData.HackingToGrantOnDeath;

                        if ( (entity.TypeData.ScienceToGrantOnDeath > 0 || entity.TypeData.ScienceToGrantOnDeathPerLevel > 0) &&
                             !NecromancerEmpireFactionBaseInfo.GetIsThisANecromancerFaction( factionForRewards ) )
                            factionForRewards.StoredScience += NumShipsDying * (entity.TypeData.ScienceToGrantOnDeath + (entity.CurrentMarkLevel - 1) * entity.TypeData.ScienceToGrantOnDeathPerLevel);
                    }

                    if ( entity.TypeData.ResourceOneToGrantOnDeath > 0 )
                    {
                        factionForRewards.StoredFactionResourceOne += NumShipsDying * entity.TypeData.ResourceOneToGrantOnDeath;
                    }

                    if ( entity.TypeData.ResourceTwoToGrantOnDeath > 0 )
                    {
                        factionForRewards.StoredFactionResourceTwo += NumShipsDying * entity.TypeData.ResourceTwoToGrantOnDeath;
                    }

                    if ( entity.TypeData.ResourceThreeToGrantOnDeath > 0 )
                    {
                        factionForRewards.StoredFactionResourceThree += NumShipsDying * entity.TypeData.ResourceThreeToGrantOnDeath;
                    }

                    ExternalFactionBaseInfo factionBaseInfo = factionForRewards.BaseInfo;
                    if ( factionBaseInfo != null )
                    {
                        //This is called only when the faction itself is the killer and also determined to be the one "deserving a reward"
                        //if a faction is going to get any unique rewards based on killing a target, then in here is where that should usually happen
                        factionBaseInfo.DoWhenThisFactionIsToBeRewardedForKillingEntity( 
                            IsFromOnlyPartOfStackDying, entity,
                            Damage, FiringSystemOrNull, NumShipsDying, Context );
                    }
                }

                if ( factionKilling != null || 
                     FactionUtilityMethods.Instance.AnyNecromancerFactions() )
                {
                    foreach ( Faction fac in World_AIW2.Instance.AllPlayerFactions )
                    {
                        if ( factionKilling == null && 
                             !NecromancerEmpireFactionBaseInfo.GetIsThisANecromancerFaction( fac ) )
                        {
                            //if we don't know who killed the entity, don't run this for non-necromancer players.
                            //Necromancers should always run even if we don't know who killed the entity because things can die from corruption damage
                            continue;
                        }
                        
                        ExternalFactionBaseInfo factionBaseInfo = fac.BaseInfo;
                        if ( factionBaseInfo != null )
                        {
                            //This is called on every player faction when any unit is killed, period.  It identifies who the killing faction is, and allows for custom logic to be run.
                            // Many times, this will be utterly unrelated to anything the player faction needs to do.  But if the player faction is "the strongest faction of type X on that planet where the thing died,"
                            // for instance, then this is a method where that sort of thing can be calculated and then some reward can be granted.

                            factionBaseInfo.CheckIfPlayerFactionShouldGetRewardBasedOnAUnitDying( 
                                factionKilling, IsFromOnlyPartOfStackDying, entity,
                                Damage, FiringSystemOrNull, NumShipsDying, Context );
                        }
                    }
                }

                //Handle AIP changes
                int aipOnDeath = entity.TypeData.AIPOnDeath;
                if ( aipOnDeath != 0 && 
                     entity.SelfBuildingMetalRemaining <= FInt.Zero )
                {
                    debugStage = 600;
                    //Note: this is in its own method so that it can use return statements without skipping other logic in the main death method
                    this.HandleAIPIncrease( debug, aipDebug, ref debugStage, aipOnDeath * NumShipsDying, entity, FiringSystemOrNull, factionKilling, planetFactionKilling, Context );
                }
                
                debugStage = 4000;

                //Handle AI counterattack stuff and stats
                if ( FiringSystemOrNull != null && 
                     entity.Planet != null &&
                     !entity.TypeData.IsDrone) //drones don't generate salvage
                {
                    debugStage = 4200;
                    
                    if ( entity.Planet.GetIsControlledByFactionType( FactionType.Player ) )
                    {
                        debugStage = 4300;
                    }
                    else
                    if ( entity.Planet.GetIsControlledByFactionType( FactionType.AI ) && 
                         factionDying != null)
                    {
                        debugStage = 4400;

                        AISentinelsCoreData sentinels = entity.Planet.GetControllingFaction()?.TryGetAISentinelsCoreData()?.SentinelInfo;
                        if ( sentinels != null && 
                             //Note that this is on a per-faction AIP basis, not the highest of any faction
                             sentinels.AIDifficulty.AIPUnlockCounterattacks <= GlobalAIWorldBaseInfo.Instance.AIProgress_Effective ) 
                        {
                            debugStage = 4500;
                            
                            //if the thing killed is a player unit on an AI planet, and the AIP is high enough, give the AI counterattack points
                            //We don't give counterattack points for non-player units (otherwise the Devourer or Marauders will trigger counterattacks waves)
                            if ( factionDying.Type == FactionType.Player && 
                                 !entity.TypeData.DoesNotContributeToCounterattacks )
                            {
                                debugStage = 4600;
                                
                                FInt updateAmount = (FInt)entity.DataForMark.StrengthPerSquad_Original_DoesNotIncreaseWithMarkLevel;
                                
                                updateAmount += NumShipsDying * updateAmount;
                                
                                if ( sentinels != null )
                                    updateAmount *= sentinels.AIDifficulty.BaseCounterattackMultiplier;
                                
                                if ( entity.TypeData.CounterAttackBudgetOverrideMultiplier != FInt.One )
                                    updateAmount *= entity.TypeData.CounterAttackBudgetOverrideMultiplier;

                                entity.Planet.AICounterattackUnspentBudget += updateAmount;
                                
                                if ( debug )
                                    ArcenDebugging.ArcenDebugLogSingleLine( entity.TypeData.InternalName + " with strength "+ entity.GetStrengthPerSquad() + " = " + updateAmount + " just died on ai planet " + entity.GetPlanetName_Safe() + " set this-planet AICounterattackUnspentBudget to " + entity.Planet.AICounterattackUnspentBudget, Verbosity.DoNotShow );

                            }
                        }
                    }
                    //else //this is ok!  This happens all the time.
                    //{
                    //    ArcenDebugging.ArcenDebugLogSingleLine(entity.GetPlanetName_Safe() + " is not owned by the player or the AI: " + entity.Planet.GetControllingFactionType(), Verbosity.DoNotShow );
                    //}

                    debugStage = 5000;

                    if ( factionForRewards != null )
                    {
                        factionForRewards.TotalUnitsKilled += NumShipsDying;
                        factionForRewards.HasKilledUnitType[entity.TypeData] = true;
                    }
                    debugStage = 5100;

                    if ( factionDying != null )
                    {
                        factionDying.TotalUnitsLost += NumShipsDying;
                    }
                }

                debugStage = 5100;

                if ( FiringSystemOrNull != null )
                {
                    debugStage = 5101;
                    if ( FiringSystemOrNull.TypeData.UnitToSpawnOnInfestation.Length > 0 &&
                         entity.TypeData.ShipClass.CanBeInfested )
                    {
                        debugStage = 5102;
                        
                        GameEntityTypeData ThingToSpawnFromInfestedUnit = GameEntityTypeDataTable.Instance.GetRowByName( FiringSystemOrNull.TypeData.UnitToSpawnOnInfestation );
                        
                        debugStage = 5103;
                        
                        GameEntity_Squad SpawnedEntity = GameEntity_Squad.CreateNew_ReturnNullIfMPClient( 
                            FiringSystemOrNull.ParentEntity.PlanetFaction, ThingToSpawnFromInfestedUnit, 0, 
                            FiringSystemOrNull.ParentEntity.PlanetFaction.Faction.LooseFleet, 0, entity.WorldLocation, 
                            Context, "OnDeath-UnitToSpawnOnInfestation" );
                        
                        if ( SpawnedEntity != null )
                        {
                            debugStage = 5104;
                            
                            SpawnedEntity.SetShipCount( NumShipsDying );
                            SpawnedEntity.Orders.SetBehaviorDirectlyInSim( EntityBehaviorType.Attacker_Full );
                            SpawnedEntity.ShouldNotBeConsideredAsThreatToHumanTeam = true; //default to not counting this as threat
                        }
                    }
                }

                debugStage = 5800;

                Faction anyFaction;
                for ( int i = 0; i < World_AIW2.Instance.Factions.Count; i++ )
                {
                    anyFaction = World_AIW2.Instance.Factions[i];
                    if ( anyFaction != null )
                        anyFaction.Safe_DeepInfo_DoOnAnyDeathLogic_FromCentralLoop_NotJustMyOwnShips_HostOnly( ref debugStage, entity, Damage, FiringSystemOrNull,
                            factionKilling, factionDying, NumShipsDying, Context );
                }

                for ( int i = 0; i < ExternalWorldDeepInfoSourceTable.Instance.Rows.Count; i++ )
                {
                    ExternalWorldDeepInfo info = ExternalWorldDeepInfoSourceTable.Instance.Rows[i].Singleton;
                    if ( info == null )
                        continue;

                    if ( info.GetShouldIBeInUse() )
                        info.Safe_DoOnAnyDeathLogic( entity, Damage, FiringSystemOrNull, factionKilling, factionDying, NumShipsDying, Context );
                }

                //If this is a hacker who died in the middle of a hack, handle any bad side effects
                if (entity.ActiveHack != null && 
                    entity.ActiveHack.AIPOnFailedHack > 0)
                {
                    GlobalAIWorldBaseInfo.Instance.ChangeAIP( (FInt)entity.ActiveHack.AIPOnFailedHack, AIPChangeReason.FailedHacking, entity.TypeData, entity.GetFactionIndex_Safe(), entity.Planet.Index, -1 );
                }

                debugStage = 7000;
                Faction facOrNull = entity.GetFactionOrNull_Safe();
                if ( facOrNull != null )
                    facOrNull.Safe_DeepInfo_DoOnAnyDeathLogic_MyFactionUnitsOnly_HostOnly( entity, Damage, FiringSystemOrNull, Context );
            }
            catch ( Exception e )
            {
                ArcenDebugging.ArcenDebugLog( "Exception in BaseScenario.DoOnAnyDeathLogic_HostOnly stage " + debugStage + "\n" + e, Verbosity.ShowAsError );
            }
        }

        Faction GetPlayerFactionToGiveRewardToOrNull(Faction killingFaction, GameEntity_Squad entity, ArcenHostOnlySimContext Context )
        {
            if ( killingFaction == null )
                return null;
            
            if ( killingFaction.Type == FactionType.Player )
                return killingFaction;

            if ( IsFriendlyTowardAnyPlayer( killingFaction ) )
            {
                //if this is a player allied faction, find the strongest player on the planet and give them the resources
                int mostAnnoyingHumanFactionIdx = entity.PlanetFaction.GetIndexOfMostAnnoyingHumanFaction(Context);
                if ( mostAnnoyingHumanFactionIdx != -1 )
                    return World_AIW2.Instance.GetFactionByIndex(mostAnnoyingHumanFactionIdx);
            }
            
            return null;
        }
        
        #region HandleAIPIncrease
        //Note: this is in its own method so that it can use return statements without skipping other logic in the main death method
        private void HandleAIPIncrease( bool debug, bool aipDebug, ref int debugStage, int aipOnDeath, GameEntity_Squad entity, EntitySystem FiringSystemOrNull, 
            Faction factionThatKilledEntityOrNull, PlanetFaction pFactionThatKilledEntity, ArcenHostOnlySimContext Context )
        {
            if ( debug )
                ArcenDebugging.ArcenDebugLogSingleLine( "Computing AIP generated by the death of " + entity.ToStringWithPlanet(), Verbosity.DoNotShow );
            
            if ( entity.SelfBuildingMetalRemaining > 0 )
                return; //you don't get AIP for a partially built structure dying. That's just mean (and tends to bite new players)
            
            if ( entity.GetShouldDieToNeutral() )
                return; // you don't get AIP for something dying to neutral
            
            if ( entity.GetFactionTypeSafe() != FactionType.AI )
            {
                if ( entity.TypeData.AIPOnDeathOnlyWhenOwnedByAI )
                {
                    if ( aipDebug )
                        ArcenDebugging.ArcenDebugLogSingleLine("AIP for " + entity.ToStringWithPlanet() + " only generated when owned by the AI; no AIP here.", Verbosity.DoNotShow );
                    
                    return;
                }
                
                //this is something not owned by the AI, so increase the AIP in a not-per-faction way.
                if ( aipDebug )
                    ArcenDebugging.ArcenDebugLogSingleLine("Path A for entity death of " + entity.ToStringWithPlanet(), Verbosity.DoNotShow );
                
                Int16 factionIndex = -1;
                if ( factionThatKilledEntityOrNull != null)
                    factionIndex = factionThatKilledEntityOrNull.FactionIndex;
                
                GlobalAIWorldBaseInfo.Instance.ChangeAIP( (FInt)aipOnDeath, AIPChangeReason.EntityDeath, entity.TypeData, factionIndex, entity.Planet.Index, entity.GetFactionIndex_Safe() );
                
                return;
            }
            
            if ( FiringSystemOrNull == null )
            {
                //if we don't know who killed the unit, up the AIP. Don't try to do anything fancy here
                //Note that since we don't know the killing unit it's going to confuse the per-faction AIP tracking
                
                if ( aipDebug )
                    ArcenDebugging.ArcenDebugLogSingleLine("Path B for entity death of " + entity.ToStringWithPlanet(), Verbosity.DoNotShow );
                
                GlobalAIWorldBaseInfo.Instance.ChangeAIP( (FInt)aipOnDeath, AIPChangeReason.EntityDeath, entity.TypeData, -1, entity.Planet.Index, entity.GetFactionIndex_Safe() );
                
                return;
            }
            
            debugStage = 700;
            
            //First check that the generated AIP (from the GameEntity_Squad xml) matches the values in ExternalConstants. I don't really expect these to
            //ever change, but let's make sure to warn a future modder.
            if ( entity.TypeData.InternalName == "WarpGate" && aipOnDeath != ExternalConstants.Instance.Balance_WarpGateAIP )
                throw new Exception( "Minor Faction AI tracking will not work correctly due to mismatched WarpGate entity xml and ExternalConstants xml for the amount of AIP to generate. Please reconcile them in the XML" );
            if ( entity.TypeData.SpecialType == SpecialEntityType.AICommandStationOriginal && aipOnDeath != ExternalConstants.Instance.Balance_CommandStationAIP )
                throw new Exception( "Minor Faction AI tracking will not work correctly due to mismatched Command Station entity xml and ExternalConstants xml for the amount of AIP to generate. Please reconcile them in the XML" );

            debugStage = 800;

            //Handle the case where if player 1 takes a planet, the AI recaptures then player 2 takes a planet,
            //player 2 won't be charged
            bool shareAIPWithAllPlayers = false;
            bool noAIPFromThisMinorFactionKill = false; 
            if ( factionThatKilledEntityOrNull != null )
            {
                if ( factionThatKilledEntityOrNull.Type == FactionType.Player )
                {
                    //if a player killed the structure
                    shareAIPWithAllPlayers = true;
                }
                else 
                if ( IsFriendlyTowardAnyPlayer( factionThatKilledEntityOrNull ) && //or an allied player
                     AIWar2GalaxySettingTable.GetIsBoolSettingEnabledByName_DuringGame( "AlliedFactionsGenerateAIP"  ) ) //and share AIP with allied factions is on
                {
                    shareAIPWithAllPlayers = true;
                    
                    //special case: player-allied ZAs don't generate AIP when they destroy things in their Territory
                    if ( factionThatKilledEntityOrNull.SpecialFactionData.InternalName == "ZenithArchitrave" )
                    {
                        if ( ZenithArchitraveFactionBaseInfo.IsPlanetInAnyZATerritory( entity.Planet ) )
                        {
                            shareAIPWithAllPlayers = false;
                            noAIPFromThisMinorFactionKill = true;
                        }
                    }
                }
            }
            
            debugStage = 900;
            
            //Check if this faction has already paid the AIP price for this
            //structure. If so then bail out.
            //Note that we also check the AIPLeft values when capturing a planet
            //Actually changing the AIP is done below
            bool IncreaseAIP = true;
            {
                if ( entity.TypeData.InternalName == "WarpGate" && pFactionThatKilledEntity != null )
                {
                    debugStage = 1000;
                    if ( pFactionThatKilledEntity.AIPLeftFromWarpGate == 0 )
                        return;
                    pFactionThatKilledEntity.AIPLeftFromWarpGate = 0;
                    if ( shareAIPWithAllPlayers )
                    {
                        debugStage = 1200;
                        
                        for ( int i = 0; i < World_AIW2.Instance.Factions.Count; i++ )
                        {
                            Faction localFaction = World_AIW2.Instance.Factions[i];
                            if ( localFaction == factionThatKilledEntityOrNull ) //dont double count ourselves
                                continue;

                            if ( localFaction.Type == FactionType.Player )
                            {
                                PlanetFaction localPlanetFaction = entity.Planet.GetPlanetFactionForFaction( localFaction );
                                if ( localPlanetFaction.AIPLeftFromWarpGate != 0 )
                                {
                                    if ( aipDebug ) ArcenDebugging.ArcenDebugLogSingleLine("\tPath A: setting aip left from warp gate to 0", Verbosity.DoNotShow );
                                    localPlanetFaction.AIPLeftFromWarpGate = 0;
                                }
                                else
                                {
                                    IncreaseAIP = false; //the player has already paid the AIP price
                                    if ( aipDebug ) ArcenDebugging.ArcenDebugLogSingleLine("\tPath B: setting aip left from warp gate to 0. IncreaseAIP: " + IncreaseAIP, Verbosity.DoNotShow );
                                }
                            }
                        }
                    }
                }
                
                debugStage = 1800;
                if ( entity.TypeData.SpecialType == SpecialEntityType.AICommandStationOriginal && 
                     pFactionThatKilledEntity != null )
                {
                    if ( pFactionThatKilledEntity.AIPLeftFromCommandStation == 0 )
                        return;
                    
                    debugStage = 1900;
                    
                    pFactionThatKilledEntity.AIPLeftFromCommandStation = 0;
                    
                    if ( shareAIPWithAllPlayers )
                    {
                        debugStage = 2000;
                        for ( int i = 0; i < World_AIW2.Instance.Factions.Count; i++ )
                        {
                            Faction localFaction = World_AIW2.Instance.Factions[i];
                            if ( localFaction.Type == FactionType.Player )
                            {
                                if ( localFaction == factionThatKilledEntityOrNull ) //dont double count ourselves
                                    continue;

                                PlanetFaction localPlanetFaction = entity.Planet.GetPlanetFactionForFaction( localFaction );
                                if ( localPlanetFaction.AIPLeftFromCommandStation != 0 )
                                {
                                    if ( aipDebug ) ArcenDebugging.ArcenDebugLogSingleLine("\tPath A: setting aip left from command station to 0", Verbosity.DoNotShow );
                                    localPlanetFaction.AIPLeftFromCommandStation = 0;
                                }
                                //the player has already paid the AIP price
                                else
                                {
                                    if ( aipDebug ) ArcenDebugging.ArcenDebugLogSingleLine("\tPath B: setting aip left from command station to 0 IncreaseAIP: " + IncreaseAIP, Verbosity.DoNotShow );
                                    IncreaseAIP = false;
                                }
                            }
                        }
                    }
                }
            }
            
            if (! IncreaseAIP )
            {
                if ( aipDebug ) ArcenDebugging.ArcenDebugLogSingleLine("\tNot changing AIP; the player has already paid. IncreaseAIP " + IncreaseAIP, Verbosity.DoNotShow );
                return;
            }
            
            debugStage = 2600;
            
            //If a player or an allied faction killed this, up the AIP
            if ( factionThatKilledEntityOrNull != null &&
                 ( factionThatKilledEntityOrNull.Type == FactionType.Player || //if a player faction
                   ( IsFriendlyTowardAnyPlayer( factionThatKilledEntityOrNull ) && //or allied to players
                     !noAIPFromThisMinorFactionKill &&
                     AIWar2GalaxySettingTable.GetIsBoolSettingEnabledByName_DuringGame( "AlliedFactionsGenerateAIP"  ) ) ) ) //and "share AIP with allied factions" is on
            {
                debugStage = 2700;
                if ( aipDebug )
                    ArcenDebugging.ArcenDebugLogSingleLine( entity.GetPlanetName_Safe() + ": the " + entity.TypeData.InternalName + " has been killed by a player or ally. AIP change " + aipOnDeath + ". pFaction: warpGateAIP " + pFactionThatKilledEntity.AIPLeftFromWarpGate + " command station AIP: " + pFactionThatKilledEntity.AIPLeftFromCommandStation, Verbosity.DoNotShow );

                GlobalAIWorldBaseInfo.Instance.ChangeAIP( (FInt)aipOnDeath, AIPChangeReason.EntityDeath, entity.TypeData, factionThatKilledEntityOrNull.FactionIndex, entity.Planet.Index,  entity.GetFactionIndex_Safe() );
            }
            else 
            //some non-human-aligned faction did the killing
            if ( factionThatKilledEntityOrNull != null )
            {
                debugStage = 2800;
                if ( aipDebug )
                    ArcenDebugging.ArcenDebugLogSingleLine( entity.GetPlanetName_Safe() + ": the " + entity.TypeData.InternalName + " has been killed by a non-player-allied unit. AIP change " + aipOnDeath + ". pFaction: warpGateAIP " + pFactionThatKilledEntity.AIPLeftFromWarpGate + " command station AIP: " + pFactionThatKilledEntity.AIPLeftFromCommandStation, Verbosity.DoNotShow );
                if ( factionThatKilledEntityOrNull.DeepInfo != null )
                    factionThatKilledEntityOrNull.DeepInfo.MinorFactionAIPEquivalentIncrease( (FInt)aipOnDeath );
            }
            
            // ???
        }
        #endregion

        private static void Helper_DoAutoDeathOnCommandStationDeath( GameEntity_Squad entity, ArcenHostOnlySimContext Context, EntitySystem FiringSystemOrNull )
        {
            if ( entity == null )
                return;
            GameEntityTypeData entityTypeData = entity.TypeData;
            if ( entityTypeData == null )
                return;
            Planet entityPlanet = entity.Planet;

            Faction entityFaction = entity == null ? null : entity.GetFactionOrNull_Safe();

            GameEntity_Squad firingParent = FiringSystemOrNull == null ? null : FiringSystemOrNull.ParentEntity;
            PlanetFaction killingPlanetFaction = firingParent == null ? null : firingParent.PlanetFaction;
            Faction killingFaction = firingParent == null ? null : firingParent.GetFactionOrNull_Safe();

            if ( killingFaction == null || killingFaction.Type == FactionType.Player ||
               killingFaction.GetIsFriendlyTowards(World_AIW2.Instance.GetFirstPlayerFactionOrNull()) )
            {
                //the outer check says "If we know who killed the command station and it's a player or ally, allow scouting". The case where FiringSystemOrNull == null shouldn't really happen except with Debug commands
                //the inner check makes sure it's an Original command station or we are paying the AIP price for it (and its not a player command station)
                if ( entityFaction != null && entityFaction.Type != FactionType.Player  &&
                     (entityTypeData.SpecialType == SpecialEntityType.AICommandStationOriginal ||
                      (killingPlanetFaction != null && killingPlanetFaction.AIPLeftFromCommandStation > 0 ) ) )
                {
                    IScenarioImplementation scenarioImp = World_AIW2.Instance.GetScenarioImplementationSafe_OrNull();
                    if ( scenarioImp != null && entityPlanet != null )
                        scenarioImp.DoScoutingAfterCommandStationDeath( Context, entityPlanet );
                }
            }
            if ( killingFaction != null )
            {
                if ( killingFaction.SpecialFactionData.InternalName == "DarkZenith" )
                {
                    //play DZ journals
                    if ( killingFaction.SpecialFactionData.FullInvasionMode )
                        World_AIW2.Instance.QueueLogJournalEntryToSidebar( "ZO_DarkZenith_Conquest", string.Empty, killingFaction, null, null, OnClient.DoThisOnHostOnly_WillBeSentToClients );
                    else
                        World_AIW2.Instance.QueueLogJournalEntryToSidebar( "ZO_DarkZenith_Svikari_Conquest", string.Empty, killingFaction, null, null, OnClient.DoThisOnHostOnly_WillBeSentToClients );
                }
            }
            //It's a bit weird to do an AIP change here, but it's  got to be someplace.
            //If the player has killed an AI Reconquest command station and there's still AIP to be earned here, charge the player for it now
            if ( killingFaction != null && killingFaction.Type == FactionType.Player && killingPlanetFaction != null &&
                 entityTypeData.SpecialType == SpecialEntityType.AICommandStationReconquest && killingPlanetFaction.AIPLeftFromCommandStation > 0 )
            {
                GlobalAIWorldBaseInfo.Instance.ChangeAIP( (FInt)killingPlanetFaction.AIPLeftFromCommandStation, AIPChangeReason.EntityDeath, entityTypeData, killingFaction.FactionIndex,
                    entityPlanet == null ? (Int16)(-1) : entityPlanet.Index,  entity.GetFactionIndex_Safe() );
                killingPlanetFaction.AIPLeftFromCommandStation = 0;
            }

            if ( entityTypeData.IsCommandStation && entityPlanet != null )
            {
                foreach ( GameEntity_Squad otherEntity in entityPlanet.Squads( EntityRollupType.AutomaticallyDiesWithCommandStation ) )
                {
                    if ( entityFaction != otherEntity.GetFactionOrNull_Safe() )
                        continue; //scrapping a player command station on a reconquered AI planet can kill AI warp gates
                    otherEntity.Die( Context, false, FiringSystemOrNull );
                }

                //only destroy these things if the command station was murdered
                if ( FiringSystemOrNull != null )
                {
                    foreach ( GameEntity_Squad otherEntity in entityPlanet.Squads( EntityRollupType.DoesNotDieWithCommandStationsIfNotYetClaimed ) )
                    {
                        if ( otherEntity.HasNotYetBeenFullyClaimed )
                            continue;
                        otherEntity.Die( Context, false, FiringSystemOrNull );
                    }
                }
            }
        }

        public virtual void DoScoutingAfterCommandStationDeath( ArcenHostOnlySimContext Context, Planet planetInQuestion )
        {
            StringBuilder stringBuilder = new StringBuilder();
            if ( planetInQuestion.IntelLevel >= PlanetIntelLevel.CurrentlyWatched )
            {
                //if we are currently watching a planet when its command station dies (and it isn't a player planet), then:
                //********************************************************************************************************
                {
                    //Hey!  Galaxies can be big, and even shaped like snakes.  It should never take us more than
                    //a certain number of planet captures to explore the whole galaxy.  Some of that is based on size.
                    //We want to keep giving the player a lot of info, but not so much that it's overwhelming.
                    int countOfPlanetsNotDestroyed = World_AIW2.Instance.CurrentGalaxy.GetCountOfNonDestroyedPlanets();
                    int minPlanetsLeftToExplore = 1;
                    if ( countOfPlanetsNotDestroyed >= 500 )
                        minPlanetsLeftToExplore = 7;
                    else if ( countOfPlanetsNotDestroyed >= 300 )
                        minPlanetsLeftToExplore = 6;
                    else if ( countOfPlanetsNotDestroyed >= 100 )
                        minPlanetsLeftToExplore = 5;
                    else if ( countOfPlanetsNotDestroyed >= 80 )
                        minPlanetsLeftToExplore = 4;
                    else if ( countOfPlanetsNotDestroyed >= 50 )
                        minPlanetsLeftToExplore = 3;
                    else if ( countOfPlanetsNotDestroyed >= 30 )
                        minPlanetsLeftToExplore = 2;

                    List<Planet> planetsToExplore = Planet.GetTemporaryPlanetList( "BaseScenario-DoScoutingAfterCommandStationDeath-planetsToExplore", 10f );
                    if ( planetsToExplore == null ) //blocked for teardown/shutdown; bail
                        return;

                    int loopCount = 40;
                    while ( loopCount-- > 0 && minPlanetsLeftToExplore > 0 )
                    {
                        //1. First make a list of all the unexplored planets next to all of the naturally-explored-or-better planets
                        //   (We need to make a list first so as to not contaminate the list as we explore them).
                        //   We're getting intel on ALL these planets, not just the ones adjacent to the planet we are exploring now
                        planetsToExplore.Clear();
                        foreach ( Planet planet in World_AIW2.Instance.Planets( false ) )
                        {
                            if ( planet.IntelLevel >= PlanetIntelLevel.ExploredByNaturalMeans )
                            {
                                foreach ( Planet neighbor in planet.LinkedNeighbors( false ) )
                                {
                                    if ( neighbor.IntelLevel == PlanetIntelLevel.Unexplored ||
                                        //if it was explored by distant hacking, then we want to convert that to being "naturally explored" now
                                        neighbor.IntelLevel == PlanetIntelLevel.ExploredByDistantHacking )
                                    {
                                        if ( !planetsToExplore.Contains( neighbor ) )
                                        {
                                            if ( neighbor.IntelLevel == PlanetIntelLevel.ExploredByDistantHacking )
                                            {
                                                neighbor.GrantIntel( PlanetIntelLevel.ExploredByNaturalMeans );
                                                neighbor.GameSecondLastHadVisionBase = World_AIW2.Instance.GameSecond;
                                            }
                                            else if ( neighbor.IntelLevel == PlanetIntelLevel.Unexplored )
                                                planetsToExplore.Add( neighbor );
                                        }
                                    }
                                }
                            }
                        }
                        if ( planetsToExplore.Count == 0 )
                        {
                            //We have already explored all the planets in the galaxy (probably via a debug option)
                            Planet.ReleaseTemporaryPlanetList( planetsToExplore );
                            return;
                        }
                        int maxToExplore = AIWar2GalaxySettingTable.GetIsIntValueFromSettingByName_DuringGame( "MaxPlanetsToScoutAtOnce" );
                        while ( planetsToExplore.Count > maxToExplore && planetsToExplore.Count > 0 )
                        {
                            int indexToRemove = Context.RandomToUse.Next( 0, planetsToExplore.Count );
                            planetsToExplore.RemoveAt( indexToRemove );
                        }


                        if ( minPlanetsLeftToExplore > maxToExplore )
                            minPlanetsLeftToExplore = maxToExplore;

                        stringBuilder.Append( "<color=#999999>Notice:</color> Explored " ).Append( Mathf.Max( minPlanetsLeftToExplore, planetsToExplore.Count ) ).Append( " planets adjacent to existing explored planets: " );
                        bool isFirst = true;

                        //2. Now explore all the planets that are directly adjacent to naturally-explored space.
                        //   Depending on the map type, this might be a LOT of planets, but hey lucky you.
                        //   In most cases it will probably take 8-10 maximum planet captures to explore everything
                        for ( int i = 0; i < planetsToExplore.Count; i++ )
                        {
                            Planet planet = planetsToExplore[i];
                            //if we've never explored this at all, then explore it
                            if ( planet.IntelLevel == PlanetIntelLevel.Unexplored )
                            {
                                planet.GrantIntel( PlanetIntelLevel.ExploredByNaturalMeans );
                                planet.GameSecondLastHadVisionBase = World_AIW2.Instance.GameSecond;
                                if ( World_AIW2.Instance.GetIsHostAnyShouldPrepareToSendNewEntitiesToClients() )
                                    World_AIW2.Instance.OnServer_PlanetsToFastBlastToClients.Enqueue( planet );
                                minPlanetsLeftToExplore--;
                                if ( isFirst )
                                    isFirst = false;
                                else
                                    stringBuilder.Append( ", " );
                                stringBuilder.Append( planet.Name );
                            }
                        }

                        //3. I guess we didn't explore as many planets as we would like (snake map?)
                        //   So let's try to explore some more.
                        int extraAttempts = 100;
                        while ( minPlanetsLeftToExplore > 0 && extraAttempts-- > 0 )
                        {
                            planetsToExplore.Clear();
                            foreach ( Planet planet in World_AIW2.Instance.Planets( false ) )
                            {
                                if ( planet.IntelLevel >= PlanetIntelLevel.ExploredByNaturalMeans )
                                {
                                    foreach ( Planet neighbor in planet.LinkedNeighbors( false ) )
                                    {
                                        if ( neighbor.IntelLevel == PlanetIntelLevel.Unexplored ||
                                            //if it was explored by distant hacking, then we want to convert that to being "naturally explored" now
                                            neighbor.IntelLevel == PlanetIntelLevel.ExploredByDistantHacking )
                                        {
                                            if ( neighbor.IntelLevel == PlanetIntelLevel.ExploredByDistantHacking )
                                            {
                                                neighbor.GrantIntel( PlanetIntelLevel.ExploredByNaturalMeans );
                                                neighbor.GameSecondLastHadVisionBase = World_AIW2.Instance.GameSecond;
                                            }
                                            else if ( neighbor.IntelLevel == PlanetIntelLevel.ExploredByDistantHacking )
                                            {
                                                if ( !planetsToExplore.Contains( neighbor ) )
                                                    planetsToExplore.Add( neighbor );
                                            }
                                        }
                                    }
                                }
                            }
                            //nothing more to explore so stop trying
                            if ( planetsToExplore.Count <= 0 )
                                break;

                            //more possibilities than we want to use, so randomize the list
                            if ( planetsToExplore.Count > minPlanetsLeftToExplore )
                            {
                                ArcenArrays.Randomize( planetsToExplore, Context.RandomToUse );
                                ArcenArrays.Randomize( planetsToExplore, Context.RandomToUse );
                                ArcenArrays.Randomize( planetsToExplore, Context.RandomToUse );
                            }

                            //unlock more planets, but NOT more than we have left in minPlanetsLeftToExplore
                            for ( int i = 0; i < planetsToExplore.Count; i++ )
                            {
                                Planet planet = planetsToExplore[i];
                                //if we've never explored this at all, then explore it
                                if ( planet.IntelLevel == PlanetIntelLevel.Unexplored )
                                {
                                    planet.GrantIntel( PlanetIntelLevel.ExploredByNaturalMeans );
                                    planet.GameSecondLastHadVisionBase = World_AIW2.Instance.GameSecond;
                                    minPlanetsLeftToExplore--;
                                    if ( isFirst )
                                        isFirst = false;
                                    else
                                        stringBuilder.Append( ", " );
                                    stringBuilder.Append( planet.Name );
                                }
                                //if we previously hack-explored this, then convert it to natural exploration
                                //but DON'T count that toward the minPlanetsLeftToExplore
                                else if ( planet.IntelLevel == PlanetIntelLevel.ExploredByDistantHacking )
                                {
                                    planet.GrantIntel( PlanetIntelLevel.ExploredByNaturalMeans );
                                    planet.GameSecondLastHadVisionBase = World_AIW2.Instance.GameSecond;
                                }

                                //we've explored enough extras, so stop looking for more
                                if ( minPlanetsLeftToExplore <= 0 )
                                    break;
                            }
                        }
                    }

                    Planet.ReleaseTemporaryPlanetList( planetsToExplore );
                }
            }

            if ( stringBuilder.Length > 0 )
            {
                stringBuilder.Append("\n");
                if ( ArcenNetworkAuthority.GetIsHostMode() )
                    World_AIW2.Instance.QueueChatMessageOrCommand( stringBuilder.ToString(), ChatType.LogToCentralChat, string.Empty, null );
            }
            
        }

        public static int GetCountOfMatchingAIPOnDeathWhenNoneLeft( GameEntityTypeData entityTypeData )
        {
            int count = 0;
            foreach ( GameEntity_Squad otherEntity in World_AIW2.Instance.Squads( EntityRollupType.AIPOnDeathWhenNoneLeft ) )
            {
                if ( otherEntity.TypeData != entityTypeData )
                    continue;
                if ( otherEntity.HasAlreadyDoneOnFirstDeath )
                    continue;
                count++;
            }
            return count;
        }

        private void CheckForAIPOnDeathWhenNoneLeft( GameEntity_Squad entity, ArcenHostOnlySimContext Context )
        {
            if (entity.TypeData.AIPOnDeathWhenNoneLeft == 0)
                return;
            
            bool more_left = false;
            foreach ( GameEntity_Squad e in World_AIW2.Instance.Squads( entity.TypeData ) )
            {
                if ( e.HasAlreadyDoneOnFirstDeath )
                    continue;

                if ( e == entity )
                    continue;

                more_left = true;

                break;
            }
            
            if ( more_left )
                return;
            
            GlobalAIWorldBaseInfo.Instance.ChangeAIP( (FInt)entity.TypeData.AIPOnDeathWhenNoneLeft, AIPChangeReason.EntityDeath, entity.TypeData, -1, -1, entity.GetFactionIndex_Safe() );
        }
        private void CheckForCivilWarOnDeathWhenNoneLeft( GameEntity_Squad entity, ArcenHostOnlySimContext Context )
        {
            bool foundAnEntity = false;
            int livingAIs = 0;
            foreach ( GameEntity_Squad otherEntity in World_AIW2.Instance.Squads( EntityRollupType.CivilWarWhenNoneLeft ) )
            {
                if ( otherEntity.TypeData != entity.TypeData )
                    continue;
                if ( otherEntity.HasAlreadyDoneOnFirstDeath )
                    continue;
                if ( otherEntity == entity )
                    continue;
                foundAnEntity = true;
                break;
            }
            for ( int i = 0; i < World_AIW2.Instance.AIFactions.Count; i++ )
            {
                if ( World_AIW2.Instance.AIFactions[i].FactionIsDefeated )
                    continue;
                livingAIs++;
            }
            if ( !foundAnEntity && livingAIs > 1 )
            {
                //ArcenDebugging.ArcenDebugLogSingleLine( "CIVIL_WAR_TEST: CheckForCivilWarOnDeathWhenNoneLeft: YesStart", Verbosity.DoNotShow );
                StartCivilWar(Context);
            }
        }
        public void StartCivilWar( ArcenHostOnlySimContext Context )
        {
            if ( ArcenNetworkAuthority.GetIsHostMode() )
                World_AIW2.Instance.QueueLogJournalEntryToSidebar( "Badg_AI_Civ", string.Empty, null, null, null, OnClient.DoThisOnHostOnly_WillBeSentToClients );
            for(int i = 0; i < World_AIW2.Instance.Factions.Count; i++)
            {
                Faction faction = World_AIW2.Instance.Factions[i];
                if(faction.Type == FactionType.AI)
                {
                    //if we're the AI, we are now hostile to each other AI faction and
                    //each other faction's Warden/Hunter fleets
                    faction.InCivilWarMode = true;
                    for(int j = 0; j < World_AIW2.Instance.Factions.Count; j++)
                    {
                        Faction otherFaction = World_AIW2.Instance.Factions[j];
                        if(i == j)
                            continue;
                        if(otherFaction.Type == FactionType.AI)
                        {
                            faction.MakeHostileTo( otherFaction );
                            otherFaction.MakeHostileTo( faction );
                        }
                        else if ( FactionUtilityMethods.Instance.IsACoreAISubFaction( otherFaction ) )
                        {
                            //if this is a warden/hunter but not my warden or hunter
                            if( otherFaction.FactionIndexOfMyParentIfIHaveOne != faction.FactionIndex)
                            {
                                faction.MakeHostileTo( otherFaction );
                                otherFaction.MakeHostileTo( faction );
                            }
                        }
                    }
                }
                else if ( FactionUtilityMethods.Instance.IsACoreAISubFaction( faction ) )
                {
                    faction.InCivilWarMode = true;
                    for(int j = 0; j < World_AIW2.Instance.Factions.Count; j++)
                    {
                        Faction otherFaction = World_AIW2.Instance.Factions[j];
                        if ( i == j )
                            continue;
                        if ( otherFaction.Type == FactionType.AI && otherFaction.FactionIndex != faction.FactionIndexOfMyParentIfIHaveOne )
                        {
                            //if this isn't my AI, I'm now hostile
                            faction.MakeHostileTo( otherFaction );
                            otherFaction.MakeHostileTo( faction );
                        }
                        else if  ( FactionUtilityMethods.Instance.IsACoreAISubFaction( otherFaction ) &&
                                   otherFaction.FactionIndexOfMyParentIfIHaveOne != faction.FactionIndexOfMyParentIfIHaveOne )
                        {
                            //if this is the warden/hunter of another AI, I'm now hostile
                            faction.MakeHostileTo( otherFaction );
                            otherFaction.MakeHostileTo( faction );
                        }
                    }
                }
            }
        }
        public void DoOnFirstClaimByHumanTeamLogic_HostOnly( GameEntity_Squad entity )
        {
            if ( entity.TypeData.AIPToClaim > 0 )
                GlobalAIWorldBaseInfo.Instance.ChangeAIP( entity.TypeData.AIPToClaim, AIPChangeReason.EntityClaim, entity.TypeData, entity.GetFactionIndex_Safe(), entity.Planet.Index, entity.GetFactionIndex_Safe() );
            else if ( entity.TypeData.AIPToClaim < 0 )
                GlobalAIWorldBaseInfo.Instance.ChangeAIP( entity.TypeData.AIPToClaim, AIPChangeReason.EntityClaim, entity.TypeData, entity.GetFactionIndex_Safe(), entity.Planet.Index, entity.GetFactionIndex_Safe() );
        }

        public void DoOnAnyClaimLogic_HostOnly( GameEntity_Squad entity )
        {
            if ( ArcenNetworkAuthority.DesiredStatus == DesiredMultiplayerStatus.Client ) //client
                return;

            //ArcenDebugging.ArcenDebugLogSingleLine("OnClaim for " + entity.ToStringWithPlanet(), Verbosity.DoNotShow );
            if ( entity.GetIsFactionControlledByLocalPlayerAccount_Safe() )
            {
                if ( entity.TypeData.SpecialType == SpecialEntityType.MobileOfficerCombatFleetFlagship )
                    Engine_AIW2.Instance.PresentationLayer.PlaySoundByType( SoundPropagation.HostDirectedPlay, SFXItemType_NonPositional.PlayerClaimsGolem );
                else if ( entity.TypeData.SpecialType == SpecialEntityType.MobileStrikeCombatFleetFlagship || 
                    entity.TypeData.SpecialType == SpecialEntityType.MobileSupportFleetFlagship ) //we can't play the Golem line for lone wolf since the golem lines all refer to the zenith
                    Engine_AIW2.Instance.PresentationLayer.PlaySoundByType( SoundPropagation.HostDirectedPlay, SFXItemType_NonPositional.PlayerClaimsFlagship );
                
                foreach ( Achievement achievement in AchievementTable.Instance.Rows )
                {                    
                    if ( achievement.ConditionType == AchievementConditionType.ClaimUnit )
                    {
                        if ( achievement.GetIsAlreadyCompleteOrBlockedByCheating( AchievementOnClient.CanOnlyBeLoggedByHost ) &&
                             !GameSettings.Current.GetBoolBySetting( "AchievementDebug" ))
                            continue; //skipping these is more efficient than checking them, especially for certain kinds
                        if ( achievement.RequiresNecromancerFaction &&
                             !FactionUtilityMethods.Instance.AnyNecromancerFactions() )
                            continue; //requires a necromancer
                        if ( achievement.RequiresShowdownCompletion &&
                             !GlobalAIWorldBaseInfo.Instance.CrisisTriggered )
                            continue; //requires a finished showdown event

                        if ( entity.TypeData.GetMatches( achievement.ConditionStringMode, achievement.ConditionRelatedStrings ) )
                        {
                            if ( GameSettings.Current.GetBoolBySetting( "AchievementDebug" ))
                                ArcenDebugging.LogSingleLine("Triggering new achievement " + achievement.InternalName + " " + achievement.Description, Verbosity.DoNotShow );
                            if ( achievement.MarkCompleteAndReturnIfAnyDataChanged( false ) ) //don't save over and over again if multiple achievements trip
                            { }
                        }
                    }
                }
            }
            //If this unit was flagged with an Objective Importance (for example, a flagship that the player wanted to capture)
            //then unflag it when claimed
            entity.ImportanceUIOnly = ObjectiveImportance.Normal;
            entity.FlagForForcedFullSyncToClients_FromHost();
            //CHRIS_TODO: make the sounds here better in the future, for DoOnAnyClaimLogic
        }

        public virtual void DoHumansGetFirstIntelOf(Planet planet)
        {
            if ( ArcenNetworkAuthority.DesiredStatus == DesiredMultiplayerStatus.Client )
                return;
            if ( planet.GetFirstMatching( SpecialEntityType.AIKingCommandStation, null, false, false ) != null )
            {
                Engine_AIW2.Instance.PresentationLayer.PlaySoundByType( SoundPropagation.HostDirectedPlay, SFXItemType_NonPositional.PlayerGetsIntelOnAIOverlord );
            }
            if ( planet.GetFirstMatching( SpecialEntityType.AIKingMobile, null, false, false ) != null )
            {
                Engine_AIW2.Instance.PresentationLayer.PlaySoundByType( SoundPropagation.HostDirectedPlay, SFXItemType_NonPositional.PlayerGetsIntelOnAIOverlord );
            }
        }

        public virtual void DoHumansGetFirstVisionOf( Planet planet, ArcenHostOnlySimContext Context )
        {
        }
        public virtual void DoOnPlayerVictory( ArcenHostOnlySimContext Context )
        {
            if ( Engine_Universal.RunStatus == RunStatus.GameStart )
                return;
//            ArcenDebugging.ArcenDebugLogSingleLine( "DoOnPlayerVictory enter", Verbosity.DoNotShow );
            try
            {
                DoPostVictoryAchievementChecks();
            }
            catch ( Exception e )
            {
                ArcenDebugging.ArcenDebugLog( "Exception when trying to log achievements: " + e, Verbosity.ShowAsError );
            }
        }

        /// <summary>
        /// For achievements and so on where there is a specific number of planets that need to be met, sometimes planets get destroyed.
        /// It would be nice for planets to still count properly for achievements if things get blown up a bit.
        /// </summary>
        private static bool GetIsWithinRange( int FirstNumber, int SecondNumber, int AllowedVariance )
        {
            return Math.Abs( FirstNumber - SecondNumber ) <= AllowedVariance;
        }

        public static void DoPostVictoryAchievementChecks()
        {
            if ( ArcenNetworkAuthority.DesiredStatus == DesiredMultiplayerStatus.Client )
                return;
            bool debug = GameSettings.Current.GetBoolBySetting( "AchievementDebug" );
            // Achievement achievement = AchievementTable.Instance.GetRowByName("WinTheGameAnyDifficulty", false, null);
            // Engine_AIW2.Instance.Achievement_MarkComplete(achievement);
            int debugLogStage = 0;
            for ( int i = 0; i < AchievementTable.Instance.Rows.Count; i++ )
            {
                Achievement achievement = AchievementTable.Instance.Rows[i];
                if ( achievement.GetIsAlreadyCompleteOrBlockedByCheating( AchievementOnClient.CanOnlyBeLoggedByHost ) &&
                     !debug ) //allow for cheating achievements in debug mode (which is C# only)
                    continue; //skipping these is more efficient than checking them, especially for certain kinds
                if ( debug )
                    ArcenDebugging.ArcenDebugLogSingleLine("Checking achievement  " + achievement.InternalName, Verbosity.DoNotShow );
                try
                {
                    debugLogStage = 100;
                    if ( achievement.RequiresNecromancerFaction &&
                         !FactionUtilityMethods.Instance.AnyNecromancerFactions() )
                        continue; //requires a necromancer
                    debugLogStage = 110;
                    if ( achievement.RequiresShowdownCompletion &&
                         !GlobalAIWorldBaseInfo.Instance.CrisisTriggered )
                        continue; //requires a finished showdown event
                    debugLogStage = 120;

                    if ( achievement.IsNumberOfPlanetsBasedVictoryAchievement )
                    {
                        debugLogStage = 150;
                        Galaxy currentGalaxy =World_AIW2.Instance.CurrentGalaxy;
                        debugLogStage = 190;
                        //check that total planets or non-destroyed planets are in the range of 5, to make sure that we are mostly as permissive as possible
                        if ( GetIsWithinRange( currentGalaxy.GetCountOfNonDestroyedPlanets(), achievement.NumberOfPlanetsInGalaxy, 5 ) ||
                            GetIsWithinRange( currentGalaxy.GetCountOfTotalPlanetsDestroyedAndOtherwise(), achievement.NumberOfPlanetsInGalaxy, 5 ) )
                        {
                            if ( achievement.MarkCompleteAndReturnIfAnyDataChanged( false ) ) //don't save over and over again if multiple achievements trip
                            { }
                        }
                    }
                    debugLogStage = 200;
                    if ( achievement.IsSingleFactionBasedVictoryAchievement && !string.IsNullOrEmpty( achievement.AITypeString ) )
                    {
                        if ( debug )
                            ArcenDebugging.ArcenDebugLogSingleLine("Path A; single AI type victory for " + achievement.InternalName + " matching " + achievement.AITypeString, Verbosity.DoNotShow );
                        for ( int j = 0; j < World_AIW2.Instance.AIFactions.Count; j++ )
                        {
                            debugLogStage = 210;
                            Faction faction = World_AIW2.Instance.AIFactions[j];
                            debugLogStage = 220;
                            AISentinelsCoreData factionExternal = faction.TryGetAISentinelsCoreData()?.SentinelInfo;
                            if ( factionExternal == null )
                                continue;
                            debugLogStage = 230;
                            int difficulty = faction.BaseInfo.GetDifficultyOrdinal_OrNegativeOneIfNotRelevant();
                            debugLogStage = 240;
                            if ( debug )
                                ArcenDebugging.ArcenDebugLogSingleLine("For " + factionExternal.AIType.DisplayName + " difficulty " + difficulty, Verbosity.DoNotShow );
                            if ( factionExternal.AdaptiveAIDifficulty != TypeDifficulty.Unset )
                            {
                                //Adaptive AI types are handled differently
                                if ( factionExternal.AdaptiveAIDifficulty == achievement.AdaptiveDifficulty &&
                                    difficulty >= achievement.FactionIntensityForAchievement )
                                    if ( achievement.MarkCompleteAndReturnIfAnyDataChanged( false ) ) //don't save over and over again if multiple achievements trip
                                    { }
                            }
                            else if ( ( ArcenStrings.Equals( factionExternal.AIType.InternalName, achievement.AITypeString ) ||
                                ArcenStrings.Equals( factionExternal.AIType.DisplayName, achievement.AITypeString ) ) &&
                               difficulty >= achievement.FactionIntensityForAchievement )
                            {
                                if ( achievement.MarkCompleteAndReturnIfAnyDataChanged( false ) ) //don't save over and over again if multiple achievements trip
                                { }
                            }
                        }
                    }
                    debugLogStage = 300;
                    if ( achievement.IsSingleFactionBasedVictoryAchievement && !string.IsNullOrEmpty( achievement.MinorFactionString ) )
                    {
                        var names = achievement.MinorFactionString.Split( ',' );

                        for ( int j = 0; j < World_AIW2.Instance.Factions.Count; j++ )
                        {
                            debugLogStage = 310;
                            Faction faction = World_AIW2.Instance.Factions[j];
                            debugLogStage = 320;
                            if ( faction.InvasionTime > World_AIW2.Instance.GameSecond + 120 )
                                continue; //if this faction was set to 'delayed invasion' and hasn't had enough time to actually do anything, it doesn't count

                            bool matches = false;
                            {
                                foreach ( var n in names )
                                {
                                    if ( ArcenStrings.Equals( faction.SpecialFactionData.InternalName, n ) ||
                                         ArcenStrings.Equals( faction.SpecialFactionData.GetDisplayName(), n ) )
                                    {
                                        matches = true;
                                        break;
                                    }
                                }
                            }

                            if ( !matches )
                                continue;

                            // Check if there is required allegiance, if there is, make sure the faction's allegiance matches the required allegiance
                            if ( !string.IsNullOrEmpty(achievement.MinorFactionAllegiance )
                                && !ArcenStrings.Equals( faction.BaseInfo.Allegiance, achievement.MinorFactionAllegiance ) )
                                continue;
                            if ( debug )
                                ArcenDebugging.ArcenDebugLogSingleLine(" " + achievement.InternalName + " B. Right faction; intensity needed " + achievement.FactionIntensityForAchievement + " actual intensity " + faction.BaseInfo.GetDifficultyOrdinal_OrNegativeOneIfNotRelevant(), Verbosity.DoNotShow );
                            debugLogStage = 330;
                            if ( faction.BaseInfo.GetDifficultyOrdinal_OrNegativeOneIfNotRelevant() < achievement.FactionIntensityForAchievement &&
                                 faction.CustomData_NumberToSeed( false ) < achievement.FactionIntensityForAchievement )
                                continue;
                            debugLogStage = 340;
                            if ( achievement.MinorFactionSubType != null && achievement.MinorFactionSubType.Length > 0 )
                            {
                                if ( debug )
                                    ArcenDebugging.ArcenDebugLogSingleLine("Looking for MinorFactionSubType " + achievement.MinorFactionSubType , Verbosity.DoNotShow );
                                bool foundSubType = false;
                                {
                                    debugLogStage = 350;
                                    AIHunterCoreData externalData = faction.GetAISentinelsCoreData().HunterInfo;
                                    if ( externalData != null && ArcenStrings.Equals( externalData.SubType.InternalName,
                                             achievement.MinorFactionSubType ) )
                                        foundSubType = true;
                                }
                                {
                                    debugLogStage = 360;
                                    AIWardenCoreData externalData = faction.GetAISentinelsCoreData().WardenInfo;
                                    if ( externalData != null && externalData.SubType != null && ArcenStrings.Equals( externalData.SubType.InternalName,
                                             achievement.MinorFactionSubType ) )
                                        foundSubType = true;
                                }
                                {
                                    debugLogStage = 370;
                                    AIPraetorianGuardCoreData externalData = faction.GetAISentinelsCoreData().PraetorianInfo;
                                    if ( externalData != null && externalData.SubType != null && ArcenStrings.Equals( externalData.SubType.InternalName,
                                             achievement.MinorFactionSubType ) )
                                        foundSubType = true;
                                }
                                if ( !foundSubType )
                                    continue;
                            }
                            if ( debug )
                                ArcenDebugging.ArcenDebugLogSingleLine(" triggering", Verbosity.DoNotShow );
                            if ( achievement.MarkCompleteAndReturnIfAnyDataChanged( false ) ) //don't save over and over again if multiple achievements trip
                            { }
                        }
                    }
                    debugLogStage = 400;
                    if ( achievement.CivilWarVictory )
                    {
                        bool allInCivilWar = true;
                        debugLogStage = 410;
                        for ( int j = 0; j < World_AIW2.Instance.AIFactions.Count; j++ )
                        {
                            debugLogStage = 420;
                            if ( !World_AIW2.Instance.AIFactions[j].InCivilWarMode )
                                allInCivilWar = false;
                        }
                        debugLogStage = 430;
                        if ( allInCivilWar )
                        {
                            if ( achievement.NumberOfAIs <= World_AIW2.Instance.AIFactions.Count )
                            {
                                if ( achievement.MarkCompleteAndReturnIfAnyDataChanged( false ) ) //don't save over and over again if multiple achievements trip
                                { }
                            }
                        }
                    }
                    debugLogStage = 500;
                    if ( achievement.ConditionType == AchievementConditionType.WinWithGalaxySettingEnabled )
                    {
                        debugLogStage = 510;
                        foreach ( string settingName in achievement.ConditionRelatedStrings )
                        {
                            debugLogStage = 520;
                            if ( AIWar2GalaxySettingTable.GetIsBoolSettingEnabledByName_DuringGame( settingName ) )
                            {
                                if ( achievement.MarkCompleteAndReturnIfAnyDataChanged( false ) ) //don't save over and over again if multiple achievements trip
                                { }
                            }
                        }
                    }
                    if ( achievement.OtherVictoryType )
                    {
                        if ( achievement.MarkCompleteAndReturnIfAnyDataChanged( false ) ) //don't save over and over again if multiple achievements trip
                        { }
                    }
                    debugLogStage = 500;
                }
                catch ( Exception e )
                {
                    ArcenDebugging.ArcenDebugLog( "Exception when trying to check achievement '" + achievement.DisplayName + "', debugLogStage: " + debugLogStage + ", \n" + e, Verbosity.ShowAsError );
                }
            } //end of the for loop
        }
        public virtual void DoOnFirstUnpauseLogic_SinceLoadOrGeneration_HostOrClient( ArcenClientOrHostSimContextCore Context )
        {
        }
        public virtual void DoOnFirstUnpauseLogic_FromGameSetup_HostOnly( bool IsFromMapGen )
        {
            int AIP = World_AIW2.Instance.Setup.GetIntBySetting( "AIP_Starting" );
            if ( AIP > 0 && World_AIW2.Instance.EmpireStylePlayerFactions.Count > 0 && GlobalAIWorldBaseInfo.Instance.AIProgress_Total == FInt.Zero )
            {
                GlobalAIWorldBaseInfo.Instance.ChangeAIP( (FInt) AIP * World_AIW2.Instance.EmpireStylePlayerFactions.Count,
                    AIPChangeReason.InitialValue, null, -1, -1, -1 );
            }

            //On first unpause, make sure that no King planet has AIP associated with it.
            //Capturing an AI Homeworld or player homeworld should not generate AIP
            foreach ( GameEntity_Squad entity in World_AIW2.Instance.Squads( EntityRollupType.KingUnitsOnly ) )
            {
                if ( entity == null )
                    continue;
                if ( entity.PlanetFaction.Faction.Type == FactionType.Player && entity.PlanetFaction.Faction.PlayerTypeName_ModeratelyExpensive == "HumanArkEmpire" )
                    continue; // Don't factor in Human Ark kings.
                Planet planet = entity.Planet;
                if ( planet == null )
                    continue;
                for (int i = 0; i < World_AIW2.Instance.Factions.Count; i++)
                {
                    PlanetFaction localPlanetFaction = planet.GetPlanetFactionForFaction(World_AIW2.Instance.Factions[i]);
                    if ( localPlanetFaction == null )
                        continue;
                    
                    localPlanetFaction.AIPLeftFromCommandStation = 0;
                    localPlanetFaction.AIPLeftFromWarpGate = 0;
                }
            }
        }

        public virtual void DoOnSelfConstructionComplete(GameEntity_Squad Squad, ArcenHostOnlySimContext Context)
        { 
            if (Squad?.TypeData?.AIPToConstruct > 0 && Squad.GetIsPlayerUnit())
            {
                GlobalAIWorldBaseInfo.Instance.ChangeAIP( 
                    Squad.TypeData.AIPToConstruct, AIPChangeReason.EntityClaim, Squad.TypeData, Squad.GetFactionIndex_Safe(), Squad.Planet.Index, Squad.GetFactionIndex_Safe() );
            }
        }
    }
}
