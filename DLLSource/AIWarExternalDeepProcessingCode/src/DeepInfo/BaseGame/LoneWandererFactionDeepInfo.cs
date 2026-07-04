using System;

using System.Linq;
using System.Text;
using Arcen.AIW2.Core;
using Arcen.Universal;

namespace Arcen.AIW2.External
{
    public abstract class LoneWandererFactionDeepInfo : ExternalFactionDeepInfoRoot
    {
        //Let's make the devourer and the zenith trader take turns on the LRP.  Might save some thread collisions, and certainly won't hurt anything.

        protected override int MinimumSecondsBetweenLongRangePlannings => 2;
        public override void SeedStartingEntities_LaterEverythingElse( Galaxy galaxy, ArcenHostOnlySimContext Context, MapTypeData mapType )
        {
            LoneWandererFactionBaseInfo baseInfo = this.AttachedFaction.GetExternalBaseInfoAs<LoneWandererFactionBaseInfo>();
            //this is the old code path
            StandardMapPopulator.Mapgen_SeedSpecialEntities( Context, galaxy, AttachedFaction, SpecialEntityType.None, baseInfo.LoneUnitTag, SeedingType.HardcodedCount, 1,
                MapGenCountPerPlanet.One, MapGenSeedStyle.SmallBad, 3, 3, PlanetSeedingZone.MostAnywhere, SeedingExpansionType.ComplicatedOriginal );
        }

        private readonly List<SafeSquadWrapper> workingListForJustMyself = List<SafeSquadWrapper>.Create_WillNeverBeGCed( 10, "LoneWandererFactionDeepInfo-shipsPerPlanet" );
        public readonly DictionaryOfLists<Planet, SafeSquadWrapper> shipsPerPlanet = DictionaryOfLists<Planet, SafeSquadWrapper>.Create_WillNeverBeGCed( 100, 10, "LoneWandererFactionDeepInfo-shipsPerPlanet" );
        public sealed override void DoLongRangePlanning_OnBackgroundNonSimThread_Subclass( ArcenLongTermIntermittentPlanningContext Context )
        {
            #region Tracing
            bool tracing = this.tracing_longTerm = Engine_AIW2.TraceAtAll && Engine_AIW2.TracingFlags.Has( ArcenTracingFlags.Independents );
            ArcenCharacterBuffer tracingBuffer = tracing ? ArcenCharacterBuffer.GetFromPoolOrCreate( "LoneWander-DoLongRangePlanning_OnBackgroundNonSimThread_Subclass-trace", 10f ) : null;
            #endregion

            PerFactionPathCache pathingCacheData = PerFactionPathCache.GetCacheForTemporaryUse_MustReturnToPoolAfterUseOrLeaksMemory();
            try
            {
                Galaxy galaxy = World_AIW2.Instance.CurrentGalaxy;
                LoneWandererFactionBaseInfo baseInfo = this.AttachedFaction.GetExternalBaseInfoAs<LoneWandererFactionBaseInfo>();

                foreach ( GameEntity_Squad entity in AttachedFaction.Squads() )
                {
                    if ( !entity.TypeData.IsMobile )
                        continue;
                    if ( !entity.TypeData.GetHasTag( baseInfo.LoneUnitTag ) )
                    {
                        shipsPerPlanet[entity.Planet].Add( entity );
                        continue; // if not the main unit
                    }
                    #region Tracing
                    if ( tracing ) tracingBuffer.Add( "\n" ).Add( "considering entity " ).Add( entity.TypeData.InternalName ).Add( " #" ).Add( entity.PrimaryKeyID );
                    #endregion
                    Planet startingPlanet = entity.Planet;
                    if ( entity.CalculateFinalDestinationPlanetIndex_Safe() != -1 &&
                         entity.CalculateFinalDestinationPlanetIndex_Safe() != entity.GetPlanetIndexSafe() )
                    {
                        Planet nextHopPlanet = World_AIW2.Instance.GetPlanetByIndex( entity.CalculateNextHopPlanetIndex_Safe() );
                        if ( this.LongRangePlanning_GetMayPassThroughThisPlanet( AttachedFaction, nextHopPlanet ) )
                        {
                            #region Tracing
                            if ( tracing ) tracingBuffer.Add( "\n" ).Add( "entity is already heading somewhere else, so no need to change" );
                            #endregion
                            continue; // if heading somewhere else, skip
                        }
                        #region Tracing
                        if ( tracing ) tracingBuffer.Add( "\n" ).Add( "entity is heading to a different planet, but the next hop planet (" ).Add( nextHopPlanet.Name ).Add( ") is not currently passable, so time to find a new destination" );
                        #endregion
                    }
                    else
                    {
                        #region Tracing
                        if ( tracing ) tracingBuffer.Add( "\n" ).Add( "entity is not heading to a different planet, and currently on " ).Add( startingPlanet.Name ).Add( " so time to find a new destination" );
                        #endregion
                    }
                    workingListForJustMyself.Clear();
                    workingListForJustMyself.Add( entity );
                    this.Helper_FindNewDestinationAndGoThere( workingListForJustMyself, AttachedFaction, galaxy, startingPlanet, Context, pathingCacheData );

                }
                foreach ( KeyValuePair<Planet, List<SafeSquadWrapper>> pair in shipsPerPlanet )
                {
                    //steal some logic from the zombies to just have these units wander around. Note that this path will never be taken
                    //in the main game except if the devourer or zenith trader has taken over other ships
                    Planet planet = pair.Key;
                    PlanetFaction pFaction = planet.GetPlanetFactionForFaction( AttachedFaction );
                    if ( pFaction.DataByStance[FactionStance.Hostile].MobileStrength < 1000 )
                    {
                        Planet newPlanet = planet.GetRandomNeighbor( false, Context );
                        FactionUtilityMethods.Instance.Helper_RaidSpecificPlanet( pair.Value, planet, AttachedFaction, World_AIW2.Instance.CurrentGalaxy, newPlanet, false, Context, pathingCacheData, 5f );
                    }
                }
            }
            catch ( Exception e )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "Lone Wanderer LRP error: " + e, Verbosity.ShowAsError );
            }
            finally
            {
                pathingCacheData.ReturnToPool();
            }
        }

        protected abstract bool LongRangePlanning_GetMayPassThroughThisPlanet( Faction faction, Planet planet );

        //Set immediately before potentialTargets.Sort(...) so the comparison can be a non-capturing
        //static delegate.  [ThreadStatic] because long-range planning runs on a background thread.
        [ThreadStatic] private static Dictionary<Planet, int> cb_lwWorkingIntsByPlanet;

        protected void Helper_FindNewDestinationAndGoThere( List<SafeSquadWrapper> ships, Faction faction, Galaxy galaxy, Planet startingPlanet,
            ArcenLongTermIntermittentPlanningContext Context, PerFactionPathCache PathCacheData )
        {
            if ( ships.Count <= 0 )
                return;

            #region Tracing
            bool tracing = this.tracing_longTerm = Engine_AIW2.TraceAtAll && Engine_AIW2.TracingFlags.Has( ArcenTracingFlags.Independents );
            ArcenCharacterBuffer tracingBuffer = tracing ? ArcenCharacterBuffer.GetFromPoolOrCreate( "LoneWander-Helper_FindNewDestinationAndGoThere-trace", 10f ) : null;
            #endregion
            #region build list of all possible planets
            Dictionary<Planet, int> workingIntsByPlanet_Devourer = Planet.GetTemporaryPlanetDictOfInts( "LoneWander-Helper_FindNewDestinationAndGoThere-workingIntsByPlanet_Devourer", 10f );
            if ( workingIntsByPlanet_Devourer == null ) //blocked for teardown/shutdown; bail
                return;
            List<Planet> potentialTargets = Planet.GetTemporaryPlanetList( "LoneWander-potentialTargets-workingIntsByPlanet_Devourer", 10f );
            if ( potentialTargets == null ) //blocked for teardown/shutdown; bail
            {
                Planet.ReleaseTemporaryPlanetDictOfInts( workingIntsByPlanet_Devourer );
                return;
            }

            foreach ( Planet.PlanetAtHopDistance _phd in startingPlanet.PlanetsWithinXHops( -1, planet =>
            {
                if ( !this.LongRangePlanning_GetMayPassThroughThisPlanet( faction, planet ) )
                    return PropogationEvaluation.No;
                return PropogationEvaluation.Yes;
            } ) )
            {
                Planet planet = _phd.Planet;
                Int16 distance = _phd.Hops;
                potentialTargets.Add( planet );
                workingIntsByPlanet_Devourer[planet] = distance;
            }
            if ( potentialTargets.Count <= 0 )
            {
                Planet.ReleaseTemporaryPlanetList( potentialTargets );
                Planet.ReleaseTemporaryPlanetDictOfInts( workingIntsByPlanet_Devourer );

                #region Tracing
                if ( tracing ) tracingBuffer.Add( "\n" ).Add( "no potential target planets found" );
                #endregion
                return;
            }
            #region devourerAttacksNanocaust
            bool DevourerAttacksNanocaustAutomatically = false;
            if ( DevourerAttacksNanocaustAutomatically && NanocaustFactionBaseInfo.CrossFaction_AllHives.Count > 0 )
            {
                potentialTargets.Clear();
                List<SafeSquadWrapper> allHives= NanocaustFactionBaseInfo.CrossFaction_AllHives.GetDisplayList();
                for ( int i = 0; i < allHives.Count; i++ )
                {
                    Planet plan = allHives[i].Planet;
                    if ( plan != null )
                        potentialTargets.Add( plan );
                }
            }
            #endregion
            #region Tracing
            if ( tracing ) tracingBuffer.Add( "\n" ).Add( "found " ).Add( potentialTargets.Count ).Add( " potential target planets" );
            #endregion
            #endregion
            #region filter list to the top 1/4 farthest planets
            cb_lwWorkingIntsByPlanet = workingIntsByPlanet_Devourer;
            potentialTargets.Sort( static ( Left, Right ) => cb_lwWorkingIntsByPlanet[Left].CompareTo( cb_lwWorkingIntsByPlanet[Right] ) );
            int lastIndexToRetain = potentialTargets.Count / 4;
            for ( int k = lastIndexToRetain + 1; k < potentialTargets.Count; k++ )
                potentialTargets.RemoveAt( k-- );
            #endregion
            #region pick randomly, and find a path
            Planet target = potentialTargets[Context.RandomToUse.Next( 0, potentialTargets.Count )];
            PathBetweenPlanetsForFaction pathCache = PathingHelper.FindPathFreshOrFromCache( faction, "LoneWandererHelper_FindNewDestinationAndGoThere", 
                startingPlanet, target, PathingMode.Shortest, Context, PathCacheData );
            #region Tracing
            if ( tracing ) tracingBuffer.Add( "\n" ).Add( "picked " ).Add( target.Name ).Add( ", path length is " ).Add( pathCache == null ? 0 : pathCache.PathToReadOnly.Count );
            #endregion
            #endregion
            #region send command to execute path
            if ( pathCache != null && pathCache.PathToReadOnly.Count > 0 )
            {
                GameCommand command = GameCommand.Create( BaseGameCommand.CommandsByCode[BaseGameCommand.Code.SetWormholePath_NPCSingleUnit], GameCommandSource.AnythingElse );
                command.RelatedString = "DEVOUR";
                for ( int k = 0; k < ships.Count; k++ )
                    command.RelatedEntityIDs.Add( ships[k].PrimaryKeyID );
                for ( int k = 0; k < pathCache.PathToReadOnly.Count; k++ )
                    command.RelatedIntegers.Add( pathCache.PathToReadOnly[k].Index );
                World_AIW2.Instance.QueueGameCommand( this.AttachedFaction, command, false );
                #region Tracing
                if ( tracing ) tracingBuffer.Add( "\n" ).Add( "command queued" );
                #endregion
            }
            #endregion

            Planet.ReleaseTemporaryPlanetList( potentialTargets );
            Planet.ReleaseTemporaryPlanetDictOfInts( workingIntsByPlanet_Devourer );
        }

        private List<Planet> allowedSpawnPlanets = List<Planet>.Create_WillNeverBeGCed( 500, "LoneWandererFactionDeepInfo-allowedSpawnPlanets" );
        protected Planet GetRespawnPlanet( Faction faction, ArcenHostOnlySimContext Context )
        {
            //If the Devourer or Zenith Trader died for some reason, we may want to respawn the unit.
            //This shared function finds a suitable planet
            //Rules: can't spawn on a human planet. Can't spawn on a planet with a King unit

            Galaxy galaxy = World_AIW2.Instance.CurrentGalaxy;
            allowedSpawnPlanets.Clear();
            foreach ( Planet planet in World_AIW2.Instance.Planets( false ) )
            {
                PlanetFaction controllingFaction = planet.GetPlanetFactionForFaction( planet.GetControllingFaction() );
                if ( controllingFaction.Faction.Type != FactionType.Player )
                {
                    if ( !planet.GetDataByStanceForFaction( faction, FactionStance.Hostile ).HasKingUnitPresent )
                        allowedSpawnPlanets.Add( planet );
                }
            }
            if ( allowedSpawnPlanets.Count == 0 )
                return null;
            return allowedSpawnPlanets[Context.RandomToUse.Next( 0, allowedSpawnPlanets.Count )];
        }
    }
}
