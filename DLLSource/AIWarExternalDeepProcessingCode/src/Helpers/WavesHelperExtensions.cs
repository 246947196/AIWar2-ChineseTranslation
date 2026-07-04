using Arcen.AIW2.Core;
using System;

using System.Text;
using Arcen.Universal;

namespace Arcen.AIW2.External
{
    public static class WavesHelperExtensions
    {
        #region DeployComposition
        public static void DeployComposition( this WavesHelper Me, ArcenHostOnlySimContext Context, Faction baseFaction, GameEntity_Base entityToSpawnAtOrNull, Int16 TargetFactionIndex,
            Dictionary<GameEntityTypeData, int> composition, List<SafeSquadWrapper> ResultingEntitiesDeployedOrNull, ArcenPoint OverrideSpawnLocation, Planet OverridePlanet = null )
        {
            if ( Context == null ) //client
                return;
            Me.DeployComposition( Context, baseFaction, entityToSpawnAtOrNull, TargetFactionIndex,
                composition, ResultingEntitiesDeployedOrNull, OverrideSpawnLocation, OverridePlanet, true, false );
        }

        public static void DeployComposition( this WavesHelper Me, ArcenHostOnlySimContext Context, Faction baseFaction, GameEntity_Base entityToSpawnAtOrNull, Int16 TargetFactionIndex,
            Dictionary<GameEntityTypeData, int> composition, List<SafeSquadWrapper> ResultingEntitiesDeployedOrNull, ArcenPoint OverrideSpawnLocation, Planet OverridePlanet,
                                              bool RelentlessWaveFactionAllowed, bool debug, bool deploySpreadOut = false )
        {
            int debugstage = 0;
            try
            {
                if ( Context == null ) //client
                    return;

                debugstage = 1000;
                
                int pairCount = composition.Count;

                if ( entityToSpawnAtOrNull == null && OverrideSpawnLocation == ArcenPoint.ZeroZeroPoint )
                    throw new Exception( "Attempted to deploy a composition but we don't know where..." );
                if ( OverrideSpawnLocation != ArcenPoint.ZeroZeroPoint && OverridePlanet == null )
                    throw new Exception( "Attempted to deploy a composition to an override point but not an override planet." );

                //The base faction might be an AI, or it might be something else like the AI reserves.
                Faction spawnFaction = baseFaction;
                bool shouldSetSpecification = false;
                
                debugstage = 1010;
                
                //only if this is an AI faction should we try to use the relentless wave faction
                if ( baseFaction.Type == FactionType.AI && RelentlessWaveFactionAllowed )
                {
                    AISentinelsFactionBaseInfo sentinelsBaseInfo = baseFaction.GetAISentinelsCoreData();

                    debugstage = 1020;
                    
                    //if the AI is targeting a faction, find out what kind
                    if ( TargetFactionIndex >= 0 )
                    {
                        debugstage = 1030;
                        
                        Faction targetFaction = World_AIW2.Instance.Factions[TargetFactionIndex];
                        //if the AI is targeting a player faction, then use the relentless AI faction
                        if ( targetFaction.Type == FactionType.Player )
                        {
                            if ( AIWar2GalaxySettingTable.GetIsBoolSettingEnabledByName_DuringGame( "AIWavesAreRelentless" ) )
                                spawnFaction = sentinelsBaseInfo.SubFac_RelentlessWave;

                            if ( debug )
                                ArcenDebugging.ArcenDebugLogSingleLine( "DeployComposition: GetRelentlessWaveLogicForThisAI: Faction Found: " + spawnFaction.GetDisplayName(), Verbosity.DoNotShow );
                        }
                        else 
                        if ( !targetFaction.SpecialFactionData.AIShouldNeverHaveHunterAgainstThis && sentinelsBaseInfo.HunterInfo.SubType.UseFireteams )
                        {
                            //if the AI is targeting an NPC faction of any kind -- player-allied or otherwise -- then feed the hunter for this AI instead if allowed
                            spawnFaction = sentinelsBaseInfo.SubFac_Hunter;
                            shouldSetSpecification = true;
                        }
                        
                        debugstage = 1040;
                    }
                    else //if the AI is not targeting a specific faction, then definitely use the relentless AI faction
                        spawnFaction = sentinelsBaseInfo.SubFac_RelentlessWave;
                    
                    debugstage = 1050;
                }
                
                debugstage = 2000;
                
                //if we had a failure for some reason, then just use the original faction
                if ( spawnFaction == null )
                    spawnFaction = baseFaction;

                debugstage = 2010;
                
                debugstage = 2020;
                
                FInt minRadius = FInt.FromParts( 0, 005 );
                FInt maxRadius = FInt.FromParts( 0, 060 );

                debugstage = 2030;
                
                ArcenPoint spawnPoint = ArcenPoint.ZeroZeroPoint;
                Planet spawnPlanet = null;
                if ( OverrideSpawnLocation != ArcenPoint.ZeroZeroPoint )
                {
                    debugstage = 2040;
                    
                    spawnPoint = OverrideSpawnLocation;
                    spawnPlanet = OverridePlanet;
                }
                else 
                if ( entityToSpawnAtOrNull != null )
                {

                    debugstage = 2050;
                    spawnPoint = entityToSpawnAtOrNull.WorldLocation;
                    spawnPlanet = entityToSpawnAtOrNull.Planet;
                }
            
                debugstage = 2060;
                
                PlanetFaction spawnPlanetFaction = spawnPlanet.GetPlanetFactionForFaction(spawnFaction);
                
                debugstage = 3000;
                
                int stacks_spawned = 0;
                
                foreach ( var pair in composition )
                {
                    debugstage = 3010;
                    
                    GameEntityTypeData currentType = pair.Key;
                    int totalToSpawn = pair.Value;

                    debugstage = 3020;
                    
                    bool canStack = !currentType.CannotBeStacked && 
                                    currentType.IsMobile && 
                                    !currentType.IsFleetLeader;
                    
                    int remainingStacks = 1;

                    int StackingCutoff = AIWar2GalaxySettingQuickAccess.StackingCutoffNPCs;
                    if ( baseFaction.Type == FactionType.Player )
                        StackingCutoff = AIWar2GalaxySettingQuickAccess.StackingCutoffPlayers;
                    if ( StackingCutoff == 0 )
                        StackingCutoff = 60;

                    debugstage = 3030;
                    
                    if ( canStack )
                    {
                        // note that im specifying '3' here as the min number of stacks
                        // so that a single ship-type doesn't spawn as one 200x stack
                        // even if that would follow the normal rules, its weird when
                        // spawning in a wave and only one thing is in a huge stack
                        remainingStacks = Math.Max( 3, StackingCutoff - spawnPlanetFaction.Entities.GetCountFromListOfEntitiesByEntityType( currentType ) );
                    }

                    debugstage = 3040;
                    
                    // clamp the amount we are spawning to be under the global pop cap
                    if ( spawnFaction != null && 
                         spawnFaction.Type != FactionType.Player && 
                         currentType.NPCShipCap != null &&
                         spawnFaction.NextNPCShipCountsByCapType != null )
                    {
                        debugstage = 3050;
                        
                        int currentTypeCount = spawnFaction.NPCShipCountsByCapType[currentType.NPCShipCap.RowIndexNonSim];
                        int globalLimit = spawnFaction.SpecialFactionData.NPCShipCapsByType[currentType.NPCShipCap.RowIndexNonSim];
                        int rem = globalLimit - currentTypeCount;
                        if ( totalToSpawn > rem )
                            totalToSpawn = rem;
                        
                        debugstage = 3060;
                    }
                    
                    debugstage = 4000;

                    int stackSizeToSpawn;
                    while ( totalToSpawn > 0 )
                    {
                        debugstage = 4010;
                        
                        if ( canStack )
                        {
                            stackSizeToSpawn = Math.Max( 1, totalToSpawn / remainingStacks );
                            totalToSpawn -= stackSizeToSpawn;
                            remainingStacks--;
                        }
                        else
                        {
                            stackSizeToSpawn = 1;
                            totalToSpawn--;
                        }

                        debugstage = 4020;
                        
                        if (debug) LOG.Msg( "Spawning a stack of {0} {1} (rem: {2}).", stackSizeToSpawn, currentType.InternalName, totalToSpawn );
                        
                        ArcenPoint spawnLocation = spawnPlanet.GetSafePlacementPoint_AroundDesiredPointVicinity( Context, currentType, spawnPoint, minRadius, maxRadius );

                        debugstage = 4030;
                        
                        GameEntity_Squad entity = spawnFaction.SpawnNewUnit_ReturnNullIfMPClient(
                            Context, spawnPlanet, spawnLocation, 
                            currentType, currentType.MarkFor( spawnFaction.CurrentGeneralMarkLevel ),
                            spawnFaction.LooseFleet, 0,
                            EntityBehaviorType.Attacker_Full, TargetFactionIndex, null, "WaveDeployment" );

                        debugstage = 4040;
                        
                        stacks_spawned++;
                        if (stacks_spawned > 60)
                            maxRadius = FInt.FromParts( 0, 100 );
                        
                        if ( entity == null )
                            continue;

                        debugstage = 4050;
                        
                        entity.SetShipCount(stackSizeToSpawn);
                        
                        if ( shouldSetSpecification )
                        {
                            debugstage = 4060;
                            
                            //The spawned ships can belong to the Hunter faction. In order to focus fireteams against a specific faction,
                            //one sets a FireteamSpecification.
                            if ( entity.FireteamSpecificationOrNull != null )
                                entity.FireteamSpecificationOrNull.ReturnToPool();
                            
                            entity.FireteamSpecificationOrNull = FireteamRequiredTarget.GetFromPoolOrCreate();
                            entity.FireteamSpecificationOrNull.AgainstFaction = TargetFactionIndex > 0 ? World_AIW2.Instance.Factions[TargetFactionIndex] : null;
                            
                            debugstage = 4070;
                        }
                        
                        debugstage = 4080;
                        
                        entity.GetEffectiveOrders().SetBehaviorDirectlyInSim( EntityBehaviorType.Attacker_Full );
                            
                        if ( ResultingEntitiesDeployedOrNull != null )
                            ResultingEntitiesDeployedOrNull.Add( entity );
                        
                        debugstage = 4090;
                        
                        //ArcenPoint entranceAnimationTargetPoint = entity.WorldLocation;
                        //AngleDegrees entranceAnimationAngle = angleToWormhole;
                        //int entranceAnimationDistance = distanceToWormhole * 1000;
                        //ArcenPoint entranceAnimationStartingPoint = Engine_AIW2.Instance.CombatCenter.GetPointAtAngleAndDistance( entranceAnimationAngle, entranceAnimationDistance );
                        //CHRIS_TODO: animation of ship zipping in from the distance from entranceAnimationStartingPoint to entranceAnimationTargetPoint (to get those, uncomment the four lines above)
                    }
                }
                
                debugstage = 5000;
            }
            catch( Exception e)
            {
                LOG.Err("error at debugstage {0}\n{1}", debugstage, e);
            }
        }
        #endregion
    }
}
