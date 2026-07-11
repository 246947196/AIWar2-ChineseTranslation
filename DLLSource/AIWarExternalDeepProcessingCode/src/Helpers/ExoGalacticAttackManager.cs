using Arcen.AIW2.Core;
using System;

using System.Text;
using Arcen.Universal;

namespace Arcen.AIW2.External
{    
    public static class ExoGalacticAttackManager
    {
        //This is the primary interface function
        public static void SendExoGalacticAttack(ExoOptions options, ArcenHostOnlySimContext Context )
        {
            if ( Context == null ) //client
            {
                options.ReturnToPool();
                return;
            }

            //In AIWC the whole Exo attack is called an Armada, which is split into multiple BattleGroups.
            //A BattleGroup is an AI force starting at a particular planet, and it has a particular Target.
            //Each BattleGroup runs basically independently
            //The goal is to allow the Exo to do multilpe things to mess with the player
            //Also it allows for attacks on multiple fronts

            //The basic structure is "Populate the Armada with multiple battlegroups".
            //Then launch the Armada, which launches each Battlegroup separately.

            //Per Keith, for a Given BattleGroup we need a Lead Ship,
            //some Escort (medium) ships and a bunch of pickets (light ships).

            //So we need a function that takes a given amount of Strength,
            //then finds an appropriate Lead, escort and picket ships for a given Battlegroup.

            //Once the battlegroup knows its Target, starting location and contents it can be launched

            //int numRetries = 100;
            bool debug = false;

            //make sure that we never spawn exos with relentless waves!
            if ( options.spawningFaction != null &&
                 options.spawningFaction.SpecialFactionData.InternalName == "AIRelentlessWave" )
            {
                options.spawningFaction = null;
            }

            if ( options.spawningFaction == null )
                options.spawningFaction = World_AIW2.GetRandomAIFaction ( Context );
            
            if ( options.spawningFaction == null )
            {
                if ( debug ) ArcenDebugging.ArcenDebugLogSingleLine("Could not find a living AI faction to send this exo. Have you won the game?", Verbosity.DoNotShow );
                
                return;
            }
            
            int defaultDistanceFromTarget = 3;
            if ( options.distanceFromTargetOverride != -1 )
                defaultDistanceFromTarget = options.distanceFromTargetOverride;
            
            bool GiveAnnouncement = false;
            if ( options.Targets == null )
                throw new Exception("You tried to send an exo without giving targets. Bad juju\n");

            if ( debug )
            {
                string targetString = "";
                for ( int i = 0; i < options.Targets.Count; i++ )
                    targetString += options.Targets[i].ToStringWithPlanet() + ", ";
                ArcenDebugging.ArcenDebugLogSingleLine( "Sending an Exo with strength " + options.AttackStrength + " toward " + options.Targets.Count + " targets: " + targetString, Verbosity.DoNotShow );
                if ( options.AttackStrength == 0 )
                    ArcenDebugging.ArcenDebugLogSingleLine("THIS EXO WAS 0 STRENGTH, PROBABLY NOT WHAT YOU WANTED", Verbosity.DoNotShow );
            }
            
            Planet firstTargetPlanet = null;
            //int totalStrength = 0;
            for ( int i = 0; i < options.Targets.Count; i++ )
            {
                GameEntity_Squad target = options.Targets[i].GetSquad();
                if ( firstTargetPlanet == null && target != null )
                    firstTargetPlanet = target.Planet;

                //totalStrength += options.AttackStrength;
                Helper_SendExoGalacticAttack_SingleExoTarget( target, ref GiveAnnouncement, options.Targets.Count, debug,
                                                              options.AttackStrength, options.ForceOrigin, options.spawningFaction, options.invokingFaction, Context,
                                                              options.type, options.distanceFromTargetOverride, defaultDistanceFromTarget, options );
            }
            
            if ( debug ) ArcenDebugging.ArcenDebugLogSingleLine("\tGiveAnnouncement is " + GiveAnnouncement, Verbosity.DoNotShow );
            
            if (GiveAnnouncement && ArcenNetworkAuthority.GetIsHostMode() )
            {
                string voiceLine = "ArkChiefOfStaff_ExoGalacticAttackDetectedOtherPlanet"; //for non-homworld targets
                for ( int i = 0; i < options.Targets.Count; i++ )
                {
                    if ( options.Targets[i].TypeData.IsKingUnit )
                    {
                        voiceLine = "ArkChiefOfStaff_ExoGalacticAttackDetectedHomePlanet"; //voiceline for targeting homeworld
                        break;
                    }
                }

                PlanetViewChatHandlerBase chatHandlerOrNull = null;
                if ( firstTargetPlanet != null )
                {
                    chatHandlerOrNull = ChatClickHandler.CreateNewAs<PlanetViewChatHandlerBase>( "PlanetGeneralFocus" );
                    if ( chatHandlerOrNull != null )
                        chatHandlerOrNull.PlanetToView = firstTargetPlanet;
                }

                if ( string.IsNullOrEmpty( options.exoText ) )
                    World_AIW2.Instance.QueueChatMessageOrCommand( "<size=75%>" + Engine_Universal.ToHoursAndMinutesString( World_AIW2.Instance.GameSecond ) + ":</size> " + 
                        options.spawningFaction.StartFactionColourForLog() + "外域打击来袭</color> "/* +
                        ArcenExternalUIUtilities.GUI_StrengthTextColorAndIcon +
                        ( totalStrength/ 1000f ).ToString( "0.#" )*/, ChatType.LogToCentralChat, voiceLine, chatHandlerOrNull );
                else
                    World_AIW2.Instance.QueueChatMessageOrCommand( Engine_Universal.ToHoursAndMinutesString( World_AIW2.Instance.GameSecond ) + ": " + 
                        options.exoText, ChatType.LogToCentralChat, voiceLine, chatHandlerOrNull );
            }

            options.ReturnToPool();
        }

        private static void Helper_SendExoGalacticAttack_SingleExoTarget( GameEntity_Squad Target, ref bool GiveAnnouncement, int TargetCount, bool debug,
             int AttackStrength, Planet ForceOrigin, Faction spawningFaction, Faction invokingFaction, ArcenHostOnlySimContext Context,
                                                                          ExoGalacticAttackType type, int distanceFromTargetOverride, int defaultDistanceFromTarget, ExoOptions options )
        {
            if ( Target == null || AttackStrength == 0 )
            {
                if ( debug )
                {
                    if ( Target == null )
                        ArcenDebugging.ArcenDebugLogSingleLine("\tearly bailout, no target", Verbosity.DoNotShow );
                    if ( AttackStrength == 0 )
                        ArcenDebugging.ArcenDebugLogSingleLine("\tearly bailout, no attack strength", Verbosity.DoNotShow );
                }
                
                return;
            }
            
            if ( Target.GetFactionTypeSafe() == FactionType.Player )
                GiveAnnouncement = true;

            List<SafeSquadWrapper> spawnLocations = GameEntity_Squad.GetTemporarySquadList( "ExoAttackManager-Helper_SendExoGalacticAttack_SingleExoTarget-spawnLocations", 10f );
            if ( spawnLocations == null ) //blocked for teardown/shutdown; bail
                return;
            GetSpawnLocationsForBattleGroup( spawnLocations, defaultDistanceFromTarget, Target, Context, spawningFaction, ForceOrigin );
            if ( debug )
                ArcenDebugging.ArcenDebugLogSingleLine("\tWe have " + spawnLocations.Count + " spawn locations for this exo", Verbosity.DoNotShow );
            if ( spawnLocations.Count == 0 )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "Could not find a spawn location for exo", Verbosity.DoNotShow );
                GameEntity_Squad.ReleaseTemporarySquadList( spawnLocations );
                return;
            }
            
            if ( TargetCount == 0 )
            {
                GameEntity_Squad.ReleaseTemporarySquadList( spawnLocations );
                throw new Exception( "Had no targets\n" );
            }
            
            int debugCode = 0;
            try
            {
                debugCode = 100;
                int totalStrengthAgainstThisTarget = AttackStrength / TargetCount;
                debugCode = 200;
                int minStrengthForSplitBattlegroup = 40000;
                int numAllowedBattleGroups = Math.Max( 1, totalStrengthAgainstThisTarget / minStrengthForSplitBattlegroup );
                debugCode = 300;
                if ( numAllowedBattleGroups > spawnLocations.Count )
                    numAllowedBattleGroups = spawnLocations.Count;
                debugCode = 400;
                int battleGroupsToUse = Context.RandomToUse.Next( 1, numAllowedBattleGroups );
                int strengthPerBattlegroup = totalStrengthAgainstThisTarget / battleGroupsToUse;
                
                if ( debug ) ArcenDebugging.ArcenDebugLogSingleLine( "Sending exo with total strength " + totalStrengthAgainstThisTarget + " against " + Target.ToStringWithPlanet() + ", split into " + battleGroupsToUse + " different battlegroups, each of strength " + strengthPerBattlegroup + ". There were " + spawnLocations.Count + " possible spawn locations.", Verbosity.DoNotShow );
                
                debugCode = 500;

                List<SafeSquadWrapper> entitiesDeployedForBattleGroup = GameEntity_Squad.GetTemporarySquadList( "ExoAttackManager-Helper_SendExoGalacticAttack_SingleExoTarget-entitiesDeployedForBattleGroup", 10f );
                if ( entitiesDeployedForBattleGroup == null ) //blocked for teardown/shutdown; bail
                {
                    GameEntity_Squad.ReleaseTemporarySquadList( spawnLocations );
                    return;
                }
                Dictionary<GameEntityTypeData, int> compositionForBattleGroup = GameEntityTypeData.GetTemporaryGameEntityTypeDataIntDict( "ExoGalacticAttackManager-Helper_SendExoGalacticAttack_SingleExoTarget-compositionForBattleGroup", 10f );
                if ( compositionForBattleGroup == null ) //blocked for teardown/shutdown; bail
                {
                    GameEntity_Squad.ReleaseTemporarySquadList( entitiesDeployedForBattleGroup );
                    GameEntity_Squad.ReleaseTemporarySquadList( spawnLocations );
                    return;
                }

                for ( int i = 0; i < battleGroupsToUse; i++ )
                {
                    debugCode = 600;
                    GameEntity_Squad spawnLocation = spawnLocations[i].GetSquad();
                    if ( spawnLocation == null )
                        continue;

                    //Figure out the composition of ships to send
                    compositionForBattleGroup.Clear();
                    getCompositionForBattleGroup( compositionForBattleGroup, Context, strengthPerBattlegroup, spawningFaction, type, options );
                    
                    if ( debug )
                    {
                        int j = 0;
                        foreach ( var pair in compositionForBattleGroup )
                        {
                            ArcenDebugging.ArcenDebugLogSingleLine( "Battlegroup " + i + " unit type " + j + " is spawning " + pair.Value + " " + pair.Key.InternalName, Verbosity.DoNotShow );
                            j++;
                        }
                    }
                    if ( debug )
                        ArcenDebugging.ArcenDebugLogSingleLine( "Deploying ships on " + spawnLocation.GetPlanetName_Safe() + " to attack " + Target.GetPlanetName_Safe() +".", Verbosity.DoNotShow );

                    //Spawn the ships
                    entitiesDeployedForBattleGroup.Clear();
                    WavesHelper.Instance.DeployComposition( Context, spawningFaction, spawnLocation, Target.GetFactionIndex_Safe(), compositionForBattleGroup,
                                                            entitiesDeployedForBattleGroup, ArcenPoint.ZeroZeroPoint, null, RelentlessWaveFactionAllowed: false, debug: debug, deploySpreadOut: true );
                    if ( debug )
                        ArcenDebugging.ArcenDebugLogSingleLine( "I have spawned " + entitiesDeployedForBattleGroup.Count + " units", Verbosity.DoNotShow );
                    //we don't need to give any orders; the LongRangePlanning code for the AI will do that.
                    //Note that if we want non-AI factions to be able to send exos, that faction will need to handle the movement code too
                    debugCode = 700;
                    for ( int shipCount = 0; shipCount < entitiesDeployedForBattleGroup.Count; shipCount++ )
                    {
                        GameEntity_Squad ship = entitiesDeployedForBattleGroup[shipCount].GetSquad();
                        if ( ship == null )
                            continue;
                        ship.ExoGalacticAttackTarget = LazyLoadSquadWrapper.Create( Target );
                        //Chris notes: these were safety checks that really don't seem needed now.  When a target dies, then this would be hit for no reason, for example.
                        //if ( ship.ExoGalacticAttackTarget.GetSquad() == null )
                        //{
                        //    if ( Target == null )
                        //        throw new Exception( "No ships were spawned for exogalactic strikeforce. No target found" );
                        //    throw new Exception( "No ships were  spawned for exogalactic strikeforce against " + Target.ToStringWithPlanetAndOwner() );
                        //}

                        ship.ExoGalacticAttackPlanetIdx = Target.Planet.Index; //set the planet index so that AI long term planning knows we are in an Exo
                    }
                    
                    debugCode = 800;
                     //this is handy debugging code, and the only reason we actually pass in invoking faction for now
                    if ( debug ) ArcenDebugging.ArcenDebugLogSingleLine( "\t For exo response " + i + ", " + strengthPerBattlegroup + " strength being spawned on " + spawnLocation.GetPlanetName_Safe() + " by " + invokingFaction.GetDisplayName() + " and to attack " + entitiesDeployedForBattleGroup[0].ExoGalacticAttackTarget.GetSquad().ToStringWithPlanetAndOwner(), Verbosity.DoNotShow );

                    GameCommand speedCommand = GameCommand.Create( BaseGameCommand.CommandsByCode[BaseGameCommand.Code.CreateSpeedGroup_ExoAttack], GameCommandSource.AnythingElse );
                    int totalSpeed = 0;
                    for ( int shipCount = 0; shipCount < entitiesDeployedForBattleGroup.Count; shipCount++ )
                    {
                        speedCommand.RelatedEntityIDs.Add( entitiesDeployedForBattleGroup[shipCount].PrimaryKeyID );
                        totalSpeed += entitiesDeployedForBattleGroup[shipCount].CalculatedSpeed;
                    }
                    
                    debugCode = 900;
                    
                    //exos travel "A bit faster than the average speed of all its units"
                    int exoGroupSpeed = entitiesDeployedForBattleGroup.Count <= 0 ? 0 : totalSpeed / entitiesDeployedForBattleGroup.Count;
                    exoGroupSpeed += exoGroupSpeed / 5;

                    int minExoSpeed = 800;
                    int maxExoSpeed = 1800;
                    if ( exoGroupSpeed < minExoSpeed )
                        exoGroupSpeed = minExoSpeed;
                    if ( exoGroupSpeed > maxExoSpeed )
                        exoGroupSpeed = maxExoSpeed;

                    speedCommand.RelatedBool = true;
                    speedCommand.RelatedIntegers.Add( exoGroupSpeed );
                    
                    World_AIW2.Instance.QueueGameCommand( spawningFaction, speedCommand, false );
                }

                GameEntity_Squad.ReleaseTemporarySquadList( entitiesDeployedForBattleGroup );
                GameEntityTypeData.ReleaseTemporaryGameEntityTypeDataIntDict( compositionForBattleGroup );
            }
            catch ( System.Threading.ThreadAbortException ) { } //simply return
            catch ( Exception e )
            {
                if ( !ArcenThreading.IsCurrentlyBlockedForShutdown.IsBusy() ) //only log if not tearing things down
                    ArcenDebugging.ArcenDebugLogSingleLine( "Hit exception in Helper_SendExoGalacticAttack_SingleExoTarget debugCode " + debugCode + " " + e.ToString(), Verbosity.ShowAsError );
            }

            GameEntity_Squad.ReleaseTemporarySquadList( spawnLocations );
        }

        private static void getCompositionForBattleGroup( Dictionary<GameEntityTypeData, int> CompositionToFill, ArcenHostOnlySimContext Context, int strength, Faction spawningFaction, ExoGalacticAttackType attackType, ExoOptions options )
        {
            CompositionToFill.Clear();

            ThrowawayDrawBagCanMemLeak<GameEntityTypeData> fleetshipBagToFill = ThrowawayDrawBagCanMemLeak<GameEntityTypeData>.Create_WillActuallyBeGCed( 30 );
            ThrowawayDrawBagCanMemLeak<GameEntityTypeData> guardianBagToFill = ThrowawayDrawBagCanMemLeak<GameEntityTypeData>.Create_WillActuallyBeGCed( 30 );
            ThrowawayDrawBagCanMemLeak<GameEntityTypeData> direGuardianBagToFill = ThrowawayDrawBagCanMemLeak<GameEntityTypeData>.Create_WillActuallyBeGCed( 30 );
            ThrowawayDrawBagCanMemLeak<GameEntityTypeData> exoleaderBagToFill = ThrowawayDrawBagCanMemLeak<GameEntityTypeData>.Create_WillActuallyBeGCed( 30 );

            //AITypeData aiType = spawningFaction.GetSentinelsExternal().AIType;            

            bool debug = false;
            if ( debug ) { }
            //I need to ask the faction for their guardians, dire_guardians and normal menus
            for ( int i = 0; i < World_AIW2.Instance.Factions.Count; i++ )
            {
                Faction otherFaction = World_AIW2.Instance.Factions[i];
                if ( !otherFaction.GetIsFriendlyTowards( spawningFaction ) )
                    continue;
                if ( !( otherFaction.SpecialFactionData.InternalName == "AI" ) )
                    continue;
                AITypeData otherFactionAIType = otherFaction.TryGetAISentinelsCoreData().SentinelInfo.AIType;
                AIShipGroupCategory shipGroupCat = null;
                AIShipGroup shipGroup = null;
                shipGroupCat = otherFactionAIType.BudgetItems[AIBudgetType.Wave].NormalAIShipGroup;
                if ( shipGroupCat != null )
                {
                    shipGroup = shipGroupCat.DrawBag.PickRandomItemAndReplace( Context.RandomToUse );
                    if ( shipGroup != null )
                        fleetshipBagToFill.CopyFrom( shipGroup.DrawBag );
                }
                shipGroupCat = otherFactionAIType.BudgetItems[AIBudgetType.Reinforcement].GuardianAIShipGroup;
                if ( shipGroupCat != null )
                {
                    shipGroup = shipGroupCat.DrawBag.PickRandomItemAndReplace( Context.RandomToUse );
                    if ( shipGroup != null )
                        guardianBagToFill.CopyFrom( shipGroup.DrawBag );
                }
                shipGroupCat = otherFactionAIType.BudgetItems[AIBudgetType.Reinforcement].DireGuardianAIShipGroup;
                if ( shipGroupCat != null )
                {
                    shipGroup = shipGroupCat.DrawBag.PickRandomItemAndReplace( Context.RandomToUse );
                    if ( shipGroup != null )
                        direGuardianBagToFill.CopyFrom( shipGroup.DrawBag );
                }
                shipGroupCat = otherFactionAIType.BudgetItems[AIBudgetType.Reinforcement].ExoLeaderAIShipGroup;
                if ( shipGroupCat != null )
                {
                    shipGroup = shipGroupCat.DrawBag.PickRandomItemAndReplace( Context.RandomToUse );
                    if ( shipGroup != null )
                    {
                        exoleaderBagToFill.CopyFrom( shipGroup.DrawBag );
                        if ( options.newExoLeaderTag == "ExtragalacticWar" && AIWar2GalaxySettingTable.GetIsBoolSettingEnabledByName_DuringGame( "ExoWarUnitsInExos" ))
                        {
                            //There were a number of places where the code would set the newExoLeaderTag that was supposed to allow
                            //ExoWar units to spawn as Exo Leaders. This seems to have broken at some point, but we're now balanced
                            //reasonbably. So now allow ExoWar units to spawn as Leaders if the code has requested it and a Setting is enabled

                            //Give a 50-50 chance between a conventional ExoLeader and an ExoWar unit
                            int numToAdd = exoleaderBagToFill.InternalListSize;
                            for ( int j = 0; j < numToAdd; j++ )
                            {
                                GameEntityTypeData typedata = GameEntityTypeDataTable.Instance.GetRandomRowWithTag(Context, "ExtragalacticWarTier1");
                                exoleaderBagToFill.AddItem(typedata, 1);
                            }

                        }
                    }
                }

            }
            if(debug)
            {
                for(int i = 0; i < fleetshipBagToFill.InternalListSize; i++)
                {
                    ArcenDebugging.ArcenDebugLogSingleLine("Considering fleetship " + i + ": " + fleetshipBagToFill.GetInternalListItemAtIndex(i).InternalName + " cost " + fleetshipBagToFill.GetInternalListItemAtIndex(i).CostForAIToPurchase + " in getCompositionForBattleGroup", Verbosity.DoNotShow );
                }
                for(int i = 0; i < guardianBagToFill.InternalListSize; i++)
                {
                    ArcenDebugging.ArcenDebugLogSingleLine("Considering guardian " + i + ": " + guardianBagToFill.GetInternalListItemAtIndex(i).InternalName + " cost " + guardianBagToFill.GetInternalListItemAtIndex(i).CostForAIToPurchase, Verbosity.DoNotShow );
                }
                for(int i = 0; i < direGuardianBagToFill.InternalListSize; i++)
                {
                    ArcenDebugging.ArcenDebugLogSingleLine("Considering dire guardian " + i + ": " + direGuardianBagToFill.GetInternalListItemAtIndex(i).InternalName + " cost " + direGuardianBagToFill.GetInternalListItemAtIndex(i).CostForAIToPurchase, Verbosity.DoNotShow );
                }

                for(int i = 0; i < exoleaderBagToFill.InternalListSize; i++)
                {
                    ArcenDebugging.ArcenDebugLogSingleLine("Considering exoleader " + i + ": " + exoleaderBagToFill.GetInternalListItemAtIndex(i).InternalName + " cost " + exoleaderBagToFill.GetInternalListItemAtIndex(i).CostForAIToPurchase, Verbosity.DoNotShow );
                }
            }

            //Now that we've filled the various draw bags of categories, we're going to
            //check the options.UnitBlocksToUse and add the correct units. Strength is divided evenly
            //between the c
            int strengthPerBlock = strength / options.UnitBlocksToUse.Count;
            if(debug)
            {
                ArcenDebugging.ArcenDebugLogSingleLine("There are " + options.UnitBlocksToUse.Count + " different types of units, and the strength for each is " + strengthPerBlock, Verbosity.DoNotShow );
            }
            int unusedParameter = -1;
            int strengthSpent = 0;
            int strengthCarryover = 0;
            int strengthToSpend = 0;
            for ( int i = 0; i < options.UnitBlocksToUse.Count; i++ )
            {
                strengthToSpend = strengthPerBlock + strengthCarryover;
                if ( debug )
                    ArcenDebugging.ArcenDebugLogSingleLine("Filling block " + options.UnitBlocksToUse[i].ToString() + " with strength " + strengthPerBlock, Verbosity.DoNotShow );
                if ( options.UnitBlocksToUse[i] == ExoUnitType.ExoLeaders )
                    strengthSpent = spawningFaction.FillComposition(Context, strengthToSpend, unusedParameter, CompositionToFill, exoleaderBagToFill,
                                                                    spawningFaction.CurrentGeneralMarkLevel, 0 );
                else if ( options.UnitBlocksToUse[i] == ExoUnitType.Strikecraft )
                    strengthSpent = spawningFaction.FillComposition( Context, strengthToSpend, unusedParameter, CompositionToFill, fleetshipBagToFill,
                                                //this is using spawningFaction.CurrentGeneralMarkLevel in order to have these pop out at the mark level of the spawning faction
                                                //should be correct since this is apparently generally used for exogalactic strike forces
                                                spawningFaction.CurrentGeneralMarkLevel, 0 );
                else if ( options.UnitBlocksToUse[i] == ExoUnitType.Guardians )
                    strengthSpent = spawningFaction.FillComposition( Context, strengthToSpend, unusedParameter, CompositionToFill, guardianBagToFill,
                                             //this is using spawningFaction.CurrentGeneralMarkLevel in order to have these pop out at the mark level of the spawning faction
                                             //should be correct since this is apparently generally used for exogalactic strike forces
                                             spawningFaction.CurrentGeneralMarkLevel, 0 );
                else if ( options.UnitBlocksToUse[i] == ExoUnitType.DireGuardians )
                    strengthSpent = spawningFaction.FillComposition( Context, strengthToSpend, unusedParameter, CompositionToFill, direGuardianBagToFill,
                                             //this is using spawningFaction.CurrentGeneralMarkLevel in order to have these pop out at the mark level of the spawning faction
                                             //should be correct since this is apparently generally used for exogalactic strike forces
                                             spawningFaction.CurrentGeneralMarkLevel, 0 );
                if ( debug )
                {
                    foreach ( KeyValuePair<GameEntityTypeData, int> pair in CompositionToFill )
                    {
                        ArcenDebugging.ArcenDebugLogSingleLine("\t" + pair.Key.GetDisplayName() + ": " + pair.Value, Verbosity.DoNotShow );
                    }
                }
                strengthCarryover = strengthToSpend - strengthSpent;
                if ( strengthCarryover < 0 )
                    strengthCarryover = 0;
            }
        }

        public static void GetSpawnLocationsForBattleGroup( List<SafeSquadWrapper> ListToFill, int PreferredDistanceFromTarget, GameEntity_Squad Target, ArcenHostOnlySimContext Context, Faction spawningFaction, Planet ForceOriginOrNull )
        {
            ListToFill.Clear();

            //GameEntity SpawnLocation = null;
            int numRetries = 0;
            int maxRetries = 120;
            GameEntity_Squad warpEntryOnTargetPlanet = null;
            do
            {
                foreach ( GameEntity_Squad entity in spawningFaction.Squads( EntityRollupType.WarpEntryPoints ) )
                {
                    if ( entity.TypeData.GetHasTag("ExogalacticWormhole") )
                    {
                        //exogalatic wormholes aren't allowable exo sources until they're eligible to send other forces through.
                        //This decreases the RNG
                        if ( (World_AIW2.Instance.GameSecond + 1) - entity.GameSecondCreated  < ExternalConstants.Instance.WormholeInvasionWarningTime )
                            continue;
                    }
                    
                    //If we can't find it exactly, search +/- the preferred distance
                    if ( entity.Planet.GetHopsTo( Target.Planet ) == PreferredDistanceFromTarget + numRetries )
                    {
                        ListToFill.Add( entity );
                    }
                    else if ( entity.Planet.GetHopsTo( Target.Planet ) == PreferredDistanceFromTarget - numRetries )
                    {
                        ListToFill.Add( entity );
                    }
                    if(ForceOriginOrNull != null &&
                       entity.Planet == ForceOriginOrNull )
                        warpEntryOnTargetPlanet = entity;
                }
                numRetries++;
                if(numRetries > maxRetries)
                    break;
            }while( ListToFill.Count == 0 );

            if ( ForceOriginOrNull != null && warpEntryOnTargetPlanet != null )
            {
                ListToFill.Clear();
                ListToFill.Add( warpEntryOnTargetPlanet );
                return;
            }

            if ( ListToFill.Count == 0)
            {
                //If we can't find any warp gates (for example, if this isn't a main AI but is a Hunter/Warden fleet)
                //then pick an AI home command station
                foreach ( GameEntity_Squad entity in World_AIW2.Instance.Squads( EntityRollupType.KingUnitsOnly ) )
                {
                    if ( entity.GetFactionTypeSafe() == FactionType.AI &&
                         entity.GetIsFriendlyTowards_Safe(spawningFaction) )
                    {
                        ListToFill.Add( entity );
                    }
                }
            }
            if( ListToFill.Count == 0 )
            {
                ArcenDebugging.ArcenDebugLogSingleLine("Could not find valid warp gate for exo wave", Verbosity.DoNotShow );
                return;
            }
        }
                
        public static Faction GetRandomHunterFleet( ArcenHostOnlySimContext Context )
        {
            List<Faction> workingFactionList = Faction.GetTemporaryFactionList( "ExoGalacticAttackManager-workingFactionList", 10f );
            if ( workingFactionList == null ) //blocked for teardown/shutdown; bail
                return null;

            Faction firstFaction = null; //there is very often only one hunter, so don't make a list if there is only one.

            for ( Int16 factionIndex = 0; factionIndex < World_AIW2.Instance.Factions.Count; factionIndex++ )
            {
                Faction otherFaction = World_AIW2.Instance.Factions[factionIndex];
                if ( !( otherFaction.SpecialFactionData.InternalName == "HunterFleet" ) )
                    continue;
                if ( firstFaction == null )
                    firstFaction = otherFaction;
                else //we have more than one, so make a list now
                {
                    if ( workingFactionList.Count == 0 )
                        workingFactionList.Add( firstFaction ); //don't forget to put that first one in now!
                    workingFactionList.Add( otherFaction );
                }
            }
            if ( workingFactionList.Count <= 1 )
            {
                Faction.ReleaseTemporaryFactionList( workingFactionList );
                return firstFaction; //handles the zero or one count
            }

            Faction ret = workingFactionList[Context.RandomToUse.Next(0, workingFactionList.Count)];
            Faction.ReleaseTemporaryFactionList( workingFactionList );
            return ret;
        }
        public static void GetAllHumanHomeCommandStations( List<SafeSquadWrapper> ListToFill )
        {
            ListToFill.Clear();
            foreach ( GameEntity_Squad entity in World_AIW2.Instance.Squads( EntityRollupType.KingUnitsOnly ) )
            {
                if ( entity.GetFactionTypeSafe() == FactionType.Player )
                    ListToFill.Add(entity);
            }
        }

        public static void GetAllHumanHomeHomeworlds( List<Planet> ListToFill )
        {
            ListToFill.Clear();
            foreach ( GameEntity_Squad entity in World_AIW2.Instance.Squads( EntityRollupType.KingUnitsOnly ) )
            {
                if ( entity.GetFactionTypeSafe() == FactionType.Player )
                    ListToFill.Add(entity.Planet);
            }
        }

        public static void GetTastyPlayerTargets( List<SafeSquadWrapper> ListToFill, bool omitHomeworlds )
        {
            ListToFill.Clear();
            //find all economic command stations, GCAs and so on owned by players.
            //This might be intended to be used with the Imperial Spire hacking response where we are already
            //sending lots of ships against the player homeworld, so we can optionally omit all such planets
            //This list is then sorted from "easiest target" to "strongest target"
            List<Planet> homeworldsToOmit = Planet.GetTemporaryPlanetList( "ExoMgr-GetTastyPlayerTargets-homeworldsToOmit", 10f );
            if ( homeworldsToOmit == null ) //blocked for teardown/shutdown; bail
                return;
            if ( omitHomeworlds )
            {
                GetAllHumanHomeHomeworlds( homeworldsToOmit );
            }
            for ( int i = 0; i < World_AIW2.Instance.Factions.Count; i++ )
            {
                Faction otherFaction = World_AIW2.Instance.Factions[i];
                if ( otherFaction == null )
                    continue;
                if( otherFaction.Type != FactionType.Player )
                    continue;

                foreach ( GameEntity_Squad entity in otherFaction.Squads( EntityRollupType.EnergyProducers ) )
                {
                    if ( entity.TypeData.IsMobile )
                        continue;
                    if ( omitHomeworlds && homeworldsToOmit.Contains(entity.Planet ) )
                         continue;
                    ListToFill.Add(entity);
                }
                foreach ( GameEntity_Squad entity in otherFaction.Squads( EntityRollupType.AIPOnDeath ) )
                {
                    if ( omitHomeworlds && homeworldsToOmit.Contains(entity.Planet ) )
                         continue;
                    if ( entity.TypeData.IsMobile )
                        continue;
                    ListToFill.Add(entity);
                }
            }

            Planet.ReleaseTemporaryPlanetList( homeworldsToOmit );

            ListToFill.Sort(static delegate ( SafeSquadWrapper L, SafeSquadWrapper R )
            {
                int lDefenses = L.PlanetFaction.DataByStance[FactionStance.Self].TotalStrength + L.PlanetFaction.DataByStance[FactionStance.Friendly].TotalStrength;
                int rDefenses = R.PlanetFaction.DataByStance[FactionStance.Self].TotalStrength + R.PlanetFaction.DataByStance[FactionStance.Friendly].TotalStrength;
                return lDefenses.CompareTo(rDefenses);
            } );
        }

        public static GameEntity_Squad GetRandomHumanHomeCommandStation( ArcenHostOnlySimContext Context )
        {
            List<SafeSquadWrapper> humanHomeCommandStationsForRand = GameEntity_Squad.GetTemporarySquadList( "ExoAttackManager-GetRandomHumanHomeCommandStation-humanHomeCommandStationsForRand", 10f );
            if ( humanHomeCommandStationsForRand == null ) //blocked for teardown/shutdown; bail
                return null;
            GetAllHumanHomeCommandStations( humanHomeCommandStationsForRand );
            if ( humanHomeCommandStationsForRand.Count <= 0 )
            {
                GameEntity_Squad.ReleaseTemporarySquadList( humanHomeCommandStationsForRand );
                return null;
            }
            GameEntity_Squad ret = humanHomeCommandStationsForRand[Context.RandomToUse.Next( 0, humanHomeCommandStationsForRand.Count )].GetSquad();
            GameEntity_Squad.ReleaseTemporarySquadList( humanHomeCommandStationsForRand );
            return ret;
        }
        public static GameEntity_Squad GetHumanHomeCommandStation( Int16 factionIndexThatIsPreferredButNotRequired )
        {
            Faction faction = World_AIW2.Instance.GetFactionByIndex( factionIndexThatIsPreferredButNotRequired );
            return GetHumanHomeCommandStation(faction);
        }
        public static GameEntity_Squad GetHumanHomeCommandStation( Faction factionThatIsPreferredButNotRequired )
        {
            GameEntity_Squad king = null;
            if ( factionThatIsPreferredButNotRequired != null )
            {
                foreach ( GameEntity_Squad entity in factionThatIsPreferredButNotRequired.Squads( EntityRollupType.KingUnitsOnly ) )
                {
                    king = entity;
                    break;
                }
            }
            if ( king == null )
            {
                foreach ( Faction fac in World_AIW2.Instance.EmpireStylePlayerFactions )
                {
                    foreach ( GameEntity_Squad entity in fac.Squads( EntityRollupType.KingUnitsOnly ) )
                    {
                        king = entity;
                        break;
                    }
                    if ( king != null )
                        return king;
                }
            }

            return king;
        }
    }
}
