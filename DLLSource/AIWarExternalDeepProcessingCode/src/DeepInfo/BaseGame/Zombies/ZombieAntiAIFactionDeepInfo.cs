using Arcen.AIW2.Core;
using System;
using System.Text;
using Arcen.Universal;

namespace Arcen.AIW2.External
{
    public sealed class ZombieAntiAIFactionDeepInfo : ZombieFactionDeepInfoBase, IExternalDeepInfo_Singleton
    {
        public ZombieAntiAIFactionBaseInfo BaseInfo;
        public static ZombieAntiAIFactionDeepInfo Instance = null;
        public override void DoAnyInitializationImmediatelyAfterFactionAssigned()
        {
            this.BaseInfo = this.AttachedFaction.GetExternalBaseInfoAs<ZombieAntiAIFactionBaseInfo>();
            Instance = this;
        }

        //for patrolling
        public readonly DictionaryOfLists<Planet, SafeSquadWrapper> shipsPerNonHostilePlanet = DictionaryOfLists<Planet, SafeSquadWrapper>.Create_WillNeverBeGCed( 60, 90, "ZombieAntiAIFactionDeepInfo-shipsPerNonHostilePlanet" ); 
        
        protected override void Cleanup()
        {
            Instance = null;
            BaseInfo = null;

            shipsPerNonHostilePlanet.Clear();
        }

        protected override int MinimumSecondsBetweenLongRangePlannings => 20; //doesn't need to run often

        private readonly List<Planet> allowedPlanets = List<Planet>.Create_WillNeverBeGCed( 60, "ZombieAntiAIFactionDeepInfo-allowedPlanets" );
        private readonly List<Planet> allPlanets = List<Planet>.Create_WillNeverBeGCed( 500, "ZombieAntiAIFactionDeepInfo-allPlanets" );
        private readonly List<Planet> friendlyNeighbors = List<Planet>.Create_WillNeverBeGCed( 60, "ZombieAntiAIFactionDeepInfo-friendlyNeighbors" );
        private readonly List<Planet> smartTargets = List<Planet>.Create_WillNeverBeGCed( 90, "ZombieAntiAIFactionDeepInfo-smartTargets" );

        private readonly List<SafeSquadWrapper> listToSend = List<SafeSquadWrapper>.Create_WillNeverBeGCed( 40, "ZombieAntiAIFactionDeepInfo-listToSend" );
        private readonly List<SafeSquadWrapper> listToPatrol = List<SafeSquadWrapper>.Create_WillNeverBeGCed( 40, "ZombieAntiAIFactionDeepInfo-listToPatrol" );

        public override void DoLongRangePlanning_OnBackgroundNonSimThread_Subclass( ArcenLongTermIntermittentPlanningContext Context )
        {
            if ( AIWar2GalaxySettingTable.GetIsBoolSettingEnabledByName_DuringGame("ZombiesGoToNecromancer") )
            {
                var necro = NecromancerEmpireFactionBaseInfo.GetRandomNecromancerFaction( Context );
                if (necro != null)
                {
                    var cmd = GameCommand.Create( BaseGameCommand.CommandsByCode[BaseGameCommand.Code.TransferEntitiesToFaction], GameCommandSource.AnythingElse );
                    cmd.RelatedFactionIndex = necro.FactionIndex;
                    
                    foreach ( GameEntity_Squad e in this.AttachedFaction.Squads() )
                    {
                            cmd.RelatedEntityIDs.Add(e.PrimaryKeyID);
                        }

                    if (cmd.RelatedEntityIDs.Count == 0)
                        cmd.ReturnToPool();
                    else
                        World_AIW2.Instance.QueueGameCommand( this.AttachedFaction, cmd, false );
                }
            }
            
            if(!this.BaseInfo.AllowedToLeaveSpawnPlanet)
                return;
            
            this.shipsPerNonHostilePlanet.Clear();
            foreach ( GameEntity_Squad entity in this.AttachedFaction.Squads() )
            {
                int overrunFactor = 5;
                int friendlyForces = entity.Planet.GetPlanetFactionForFaction( this.AttachedFaction ).DataByStance[FactionStance.Self].TotalStrength +
                    entity.Planet.GetPlanetFactionForFaction( this.AttachedFaction ).DataByStance[FactionStance.Friendly].TotalStrength;
                if ( entity.Planet.GetPlanetFactionForFaction( this.AttachedFaction ).DataByStance[FactionStance.Hostile].TotalStrength > friendlyForces / overrunFactor )
                    continue; // if there's only a command station and warp gate left, consider the planet dead and move onto patrol/raid
                if ( entity.CalculateNextHopPlanetIndex_Safe() != -1 && entity.GetPlanetIndexSafe() != entity.CalculateNextHopPlanetIndex_Safe() )
                    continue; // if the ship has a wormhole move order ( and double check its not a wormhole to the same planet its on ), don't give the ship new orders

                //This planet has few enough hostile units that it the units can leave
                shipsPerNonHostilePlanet[entity.Planet].Add(entity);
            }


            PerFactionPathCache pathingCacheData = PerFactionPathCache.GetCacheForTemporaryUse_MustReturnToPoolAfterUseOrLeaksMemory();
            try
            {

                foreach ( KeyValuePair<Planet, List<SafeSquadWrapper>> pair in shipsPerNonHostilePlanet )
                {
                    Planet planet = pair.Key;
                    Planet newPlanet = planet.GetRandomNeighbor( false, Context );

                    allowedPlanets.Clear();
                    allPlanets.Clear();
                    friendlyNeighbors.Clear();
                    smartTargets.Clear();

                    bool SmartZombies = true;
                    int zombieStrength = 0;
                    List<SafeSquadWrapper> list = pair.Value;
                    for ( int idx = 0; idx < list.Count; idx++ )
                    {
                        //we need to check each Raider in case it is stacked
                        zombieStrength += list[idx].GetStrengthOfStack();
                    }

                    int shipsToSend = Context.RandomToUse.NextWithInclusiveUpperBound( this.BaseInfo.MinShipsAllowedToLeavePerPlanetPerIteration, this.BaseInfo.MaxShipsAllowedToLeavePerPlanetPerIteration );

                    foreach ( Planet neighbor in planet.LinkedNeighbors( false ) )
                    {
                        if ( neighbor == null )
                            continue;
                        PlanetFaction pFaction = neighbor.GetPlanetFactionForFaction( this.AttachedFaction );
                        if ( neighbor.GetControllingFaction().GetIsFriendlyTowards( this.AttachedFaction ) ||
                             neighbor.GetFactionWithSpecialInfluenceHere().GetIsFriendlyTowards( this.AttachedFaction ) )
                            friendlyNeighbors.Add( neighbor );
                        else if ( neighbor.GetControllingFaction().GetIsNeutralTowards( this.AttachedFaction ) &&
                                  pFaction.DataByStance[FactionStance.Hostile].TotalStrength == 0 )
                            friendlyNeighbors.Add( neighbor );
                        if ( SmartZombies )
                        {
                            if ( pFaction.DataByStance[FactionStance.Hostile].TotalStrength > 0 && pFaction.DataByStance[FactionStance.Hostile].TotalStrength <
                                 (pFaction.DataByStance[FactionStance.Friendly].TotalStrength + pFaction.DataByStance[FactionStance.Self].TotalStrength + zombieStrength) )
                                smartTargets.Add( neighbor ); //if the enemy forces are weaker than allied forces on that planet plus our strength
                        }
                        allPlanets.Add( neighbor );
                    }
                    if ( AIWar2GalaxySettingTable.GetIsBoolSettingEnabledByName_DuringGame( "DefensiveZombies" ) )
                    {
                        allowedPlanets.AddRange( friendlyNeighbors );
                    }
                    else if ( SmartZombies )
                    {
                        if ( smartTargets.Count == 0 )
                            allowedPlanets.AddRange( friendlyNeighbors );
                        else
                        {
                            allowedPlanets.AddRange( smartTargets );
                            shipsToSend = pair.Value.Count; //we are attacking; send everyone
                        }
                    }
                    else
                        allowedPlanets.AddRange( allPlanets );

                    if ( allowedPlanets.Count > 0 )
                        newPlanet = allowedPlanets[Context.RandomToUse.Next( 0, allowedPlanets.Count )];

                    if ( newPlanet == null )
                        continue; //no safe planet to send ships to

                    listToSend.Clear();
                    listToPatrol.Clear();

                    if ( pair.Value.Count > shipsToSend )
                    {
                        int sendIndex = 0;
                        int patrolIndex = 0;
                        bool sendWrap = false;

                        sendIndex = Context.RandomToUse.NextWithInclusiveUpperBound( 0, pair.Value.Count - 1 );
                        if ( sendIndex + shipsToSend > pair.Value.Count )
                        {
                            patrolIndex = sendIndex + shipsToSend - pair.Value.Count;
                            sendWrap = true;
                        }
                        if ( !sendWrap ) //Send is grouped together, remove Send from the whole list to produce Patrol
                        {
                            pair.Value.GetRange( listToSend, sendIndex, shipsToSend );
                            listToPatrol.AddRange( pair.Value );
                            listToPatrol.RemoveRange( sendIndex, listToSend.Count );
                        }
                        else //Patrol is grouped together, remove Patrol from the whole list to produce Send
                        {
                            pair.Value.GetRange( listToPatrol, patrolIndex, pair.Value.Count - shipsToSend );
                            listToSend.AddRange( pair.Value );
                            listToSend.RemoveRange( patrolIndex, listToPatrol.Count );
                        }
                    }
                    else
                        listToSend.AddRange( pair.Value );

                    FactionUtilityMethods.Instance.Helper_RaidSpecificPlanet( listToSend, planet, this.AttachedFaction, World_AIW2.Instance.CurrentGalaxy, newPlanet, false, Context, pathingCacheData, 5f );
                    if ( listToPatrol.Count > 0 )
                        FactionUtilityMethods.Instance.patrolPlanet( this.AttachedFaction, listToPatrol, Context, planet.GravWellSize.DistanceScale_GravwellRadius / 2, 10f );
                }
            }
            catch ( Exception e )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "Zombie Anti-AI LRP Pathing error: " + e, Verbosity.ShowAsError );
            }
            finally
            {
                pathingCacheData.ReturnToPool();
            }
        }
    }
}
