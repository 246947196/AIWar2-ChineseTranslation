using Arcen.AIW2.Core;
using Arcen.Universal;
using System;

using System.Linq;
using System.Text;

namespace Arcen.AIW2.External
{
    public class AIGuardPostAndCommandPlacers_Base : IAIGuardPostAndCommandPlacerImplementation
    {
        public static readonly ReferenceTracker RefTracker = new ReferenceTracker( "AIGuardPostAndCommandPlacers_Bases" );
        public AIGuardPostAndCommandPlacers_Base()
        {
            RefTracker.IncrementObjectCount();
        }

        //human homeworlds
        public static ArcenPoint GetPointForCommandStation_HumanHome( ArcenHostOnlySimContext Context, Planet ThisPlanet )
        {
            return Helper_GetPointForCommandStation( Context, ThisPlanet, FInt.FromParts( 0, 650 ), FInt.FromParts( 0, 850 ), FInt.FromParts( 0, 150 ), 300 );
        }

        //The baseline for all AIs and such
        public virtual ArcenPoint GetPointForCommandStation( ArcenHostOnlySimContext Context, Planet ThisPlanet )
        {
            return Helper_GetPointForCommandStation( Context, ThisPlanet, FInt.FromParts( 0, 50 ), FInt.FromParts( 0, 900 ), FInt.FromParts( 0, 150 ), 300 );
        }

        #region Helper_GetPointForCommandStation
        public static ArcenPoint Helper_GetPointForCommandStation( ArcenHostOnlySimContext Context, Planet ThisPlanet, FInt minDistancePercentage, FInt maxDistancePercentage, FInt minDistancePercentageFromWormholes, int TotalTries )
        {
            int minDistanceFromWormholes = (ThisPlanet.GravWellSize.DistanceScale_GravwellRadius * minDistancePercentageFromWormholes).IntValue;

            Context.RandomToUse.ReinitializeWithSeed( ThisPlanet.Index + World_AIW2.Instance.Setup.MapConfig.Seed );


            GameEntityTypeData commandStationTypeForTestPurposes = GameEntityTypeDataTable.Instance.GetRowByName( "AICommandStation" );
            if ( commandStationTypeForTestPurposes == null )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "Sorry!  Can't generate a proper map.  We need a specific unit called AICommandStation for its name.  " +
                    "You can mark it as deprecated, so it does not show up in game, but its radius will be used for the sizing of other entities.", Verbosity.ShowAsError );
                return ArcenPoint.ZeroZeroPoint;
            }

            ArcenPoint commandStationLocation = ArcenPoint.ZeroZeroPoint;
            for ( int loopCount = 0; loopCount < TotalTries; loopCount++ )
            {
                commandStationLocation = ThisPlanet.GetSafePlacementPointAroundPlanetCenter( Context, commandStationTypeForTestPurposes, minDistancePercentage, maxDistancePercentage );
                if ( ThisPlanet.GetIsPointOutsideGravWell_SlowButCorrect( commandStationLocation ) )
                    continue; //out of grav well, try again
                bool needsToTryAgain = false;
                foreach ( GameEntity_Other wormhole in ThisPlanet.Others() )
                {
                    if ( commandStationLocation.GetDistanceTo( wormhole.WorldLocation, false ) < minDistanceFromWormholes )
                    {
                        needsToTryAgain = true;
                        break;
                    }
                }
                if ( !needsToTryAgain )
                    break; //we're good!
            }

            //if we're saying something outside the gravity well, then put it on the edge instead.
            if ( ThisPlanet.GetIsPointOutsideGravWell_SlowButCorrect( commandStationLocation ) )
                return ThisPlanet.GetPointOnRadiusOfGravWellIfOutOfRange_Slow( commandStationLocation );

            return commandStationLocation;
        }
        #endregion Helper_GetPointForCommandStation

        #region Helper_TryToSeedEntityWithPlanetMark
        protected static GameEntity_Squad Helper_TryToSeedEntityWithPlanetMark_AtSpecificPoint( ArcenHostOnlySimContext Context, Planet ThisPlanet, ArcenPoint point, 
            Faction faction, GameEntityTypeData typeToPlace,
            int minDistance, int maxDistance, bool ThrowErrorsIfWouldReturnNull )
        {
            if ( ThisPlanet == null || faction == null )
            {
                if ( ThrowErrorsIfWouldReturnNull )
                    Engine_AIW2.Instance.LogErrorDuringCurrentMapGen( "Null-seed because ThisPlanet: " + ( ThisPlanet == null ? "null" : ThisPlanet.Name ) +
                        " faction: " + ( faction == null ? "null" : faction.GetDisplayName() ) );
                return null;
            }
            point = ThisPlanet.GetSafePlacementPoint_SpecificPoint( Context, typeToPlace, point, minDistance, maxDistance );
            if ( point == ArcenPoint.ZeroZeroPoint )
            {
                if ( ThrowErrorsIfWouldReturnNull )
                    Engine_AIW2.Instance.LogErrorDuringCurrentMapGen( "Null-seed because point == ArcenPoint.ZeroZeroPoint" );
                return null;
            }
            PlanetFaction pFaction = ThisPlanet.GetPlanetFactionForFaction( faction );
            if ( pFaction == null )
            {
                if ( ThrowErrorsIfWouldReturnNull )
                    Engine_AIW2.Instance.LogErrorDuringCurrentMapGen( "Null-seed because pFaction is null!" );
                return null;
            }
            GameEntity_Squad newEntity = GameEntity_Squad.CreateNew_ReturnNullIfMPClient( pFaction, typeToPlace, typeToPlace.MarkFor( ThisPlanet.MarkLevelForAIOnly.Ordinal ),
                pFaction.FleetUsedAtPlanet, 0, point, Context, "GuardPostPlacer" );
            if ( newEntity == null )
            {
                if ( ThrowErrorsIfWouldReturnNull )
                    Engine_AIW2.Instance.LogErrorDuringCurrentMapGen( "Null-seed because newEntity is null!" );
                return null;
            }
            newEntity.Orders.SetBehaviorDirectlyInSim( EntityBehaviorType.Stationary );
            return newEntity;
        }
        #endregion

        #region Helper_PlaceGuardPostsAtPointsList
        public static RefPair<int, int> Helper_PlaceGuardPostsAtPointsList( ArcenHostOnlySimContext Context, Planet ThisPlanet, Faction faction, 
            Tutorial.TutorialPlanetOwnershipAndSpawning TutorialPlanetOrNull,
            IAISentinelsCoreData SentinelsExternalData, List<ArcenPoint> points, bool IsReconquest, ref int AddedStrength )
        {
            RefPair<int, int> result = RefPair<int, int>.Create( points.Count, 0 );
            int debugStage = 1;
            try
            {
                AISentinelsCoreData sentinelsExternal = (AISentinelsCoreData)SentinelsExternalData;
                PlanetFaction pFaction = ThisPlanet.GetPlanetFactionForFaction( faction );

                AIShipGroup guardPostsArmed = null;
                AIShipGroup guardPostsDireArmed = null;
                AIShipGroup guardPostsUnarmed = null;
                AIBudgetItem reinforcementBudgetItem = sentinelsExternal.AIType.BudgetItems[AIBudgetType.Reinforcement];

                guardPostsArmed = reinforcementBudgetItem.GuardPostAIShipGroup.DrawBag.PickRandomItemAndReplace( Context.RandomToUse );
                guardPostsUnarmed = reinforcementBudgetItem.UnarmedGuardPostAIShipGroup.DrawBag.PickRandomItemAndReplace( Context.RandomToUse );

                switch ( ThisPlanet.PopulationType )
                {
                    case PlanetPopulationType.AIHomeworld:
                    case PlanetPopulationType.AIBastionWorld:
                        if ( !IsReconquest )//only dires on initial seeding
                            guardPostsDireArmed = reinforcementBudgetItem.DireGuardPostAIShipGroup.DrawBag.PickRandomItemAndReplace( Context.RandomToUse );
                        break;
                }

                DrawBag<GameEntityTypeData> armedGuardPostBag = null;
                if ( guardPostsArmed != null )
                    armedGuardPostBag = guardPostsArmed.DrawBag;

                DrawBag<GameEntityTypeData> direArmedGuardPostBag = null;
                if ( guardPostsDireArmed != null )
                    direArmedGuardPostBag = guardPostsDireArmed.DrawBag;

                DrawBag<GameEntityTypeData> unarmedGuardPostBag = null;
                if ( guardPostsUnarmed != null )
                    unarmedGuardPostBag = guardPostsUnarmed.DrawBag;

                bool canUseUnarmed = true;
                foreach ( Faction player in World_AIW2.Instance.AllPlayerFactions )
                {
                    PlayerTypeData playerType = player.PlayerTypeDataOrNull_ModeratelyExpensive;
                    if ( playerType != null && playerType.MapGen_ShouldAllGuardPostsBeArmed )
                    {
                        canUseUnarmed = false;
                        break;
                    }
                }

                if ( !canUseUnarmed )
                {
                    if ( armedGuardPostBag != null && armedGuardPostBag.GetHasItems() )
                        unarmedGuardPostBag = armedGuardPostBag;
                }

                bool hasArmed = (armedGuardPostBag != null && armedGuardPostBag.GetHasItems());
                bool hasDireArmed = (direArmedGuardPostBag != null && direArmedGuardPostBag.GetHasItems());
                bool hasUnarmmed = (unarmedGuardPostBag != null && unarmedGuardPostBag.GetHasItems());

                //ArcenDebugging.ArcenDebugLog( "Placing guard post count: " + points.Count, Verbosity.DoNotShow );

                debugStage = 410;
                if ( TutorialPlanetOrNull != null && TutorialPlanetOrNull.SkipAIGuardPosts )
                {
                    result.RightItem = -1;
                    return result; //sometimes we don't want guard posts, even.  Seems valid to me!
                }
                if ( (!hasArmed && !hasUnarmmed && !hasDireArmed) || (TutorialPlanetOrNull != null && TutorialPlanetOrNull.SkipAIGuardPosts) )
                {
                    ArcenDebugging.ArcenDebugLog( "Could not find any armed or unarmed guard posts at planet!", Verbosity.DoNotShow );
                    result.RightItem = -2;
                    return result; //sometimes we don't want guard posts, even.  Seems valid to me!
                }
                //result

                ArcenArrays.Randomize( points, Context.RandomToUse, 3 );

                byte markLevelOfPlanet = ThisPlanet.MarkLevelForAIOnly.Ordinal;
                byte chanceForArmed = 100;
                //This also fixes a bug where if you were trying to use unarmed guard posts in reconquest 
                //after saving and loading, that would fail because unarmed guard posts are not serialized.
                if ( !IsReconquest )
                {
                    if ( ThisPlanet.PopulationType == PlanetPopulationType.AIHomeworld ||
                        ThisPlanet.PopulationType == PlanetPopulationType.AIBastionWorld || 
                        markLevelOfPlanet >= 7 )
                        chanceForArmed = 100;
                    else
                    {
                        if ( markLevelOfPlanet < 1 )
                            markLevelOfPlanet = 1;
                        switch ( markLevelOfPlanet )
                        {
                            case 1:
                                chanceForArmed = sentinelsExternal.AIType.ChanceOutOf100ForArmedGuardPostPlanetMk1;
                                break;
                            case 2:
                                chanceForArmed = sentinelsExternal.AIType.ChanceOutOf100ForArmedGuardPostPlanetMk2;
                                break;
                            case 3:
                                chanceForArmed = sentinelsExternal.AIType.ChanceOutOf100ForArmedGuardPostPlanetMk3;
                                break;
                            case 4:
                                chanceForArmed = sentinelsExternal.AIType.ChanceOutOf100ForArmedGuardPostPlanetMk4;
                                break;
                            case 5:
                                chanceForArmed = sentinelsExternal.AIType.ChanceOutOf100ForArmedGuardPostPlanetMk5;
                                break;
                            case 6:
                                chanceForArmed = sentinelsExternal.AIType.ChanceOutOf100ForArmedGuardPostPlanetMk6;
                                break;
                        }
                    }
                }

                GameEntityTypeData warpingType = GameEntityTypeDataTable.Instance.GetRowByName( "WarpingInGuardPost" );

                int numberOfDires = 0;
                if ( hasDireArmed && !IsReconquest )
                {
                    switch ( ThisPlanet.PopulationType )
                    {
                        case PlanetPopulationType.AIHomeworld:
                            numberOfDires = sentinelsExternal.AIType.NumberOfDirePostsOnHomeworld;
                            break;
                        case PlanetPopulationType.AIBastionWorld:
                            numberOfDires = sentinelsExternal.AIType.NumberOfDirePostsOnBastionWorlds;
                            break;
                    }
                }

                debugStage = 420;
                for ( int i = 0; i < points.Count; i++ )
                {
                    ArcenPoint point = points[i];

                    debugStage = 430;
                    GameEntityTypeData postType = null;
                    if ( numberOfDires > 0 && hasDireArmed && direArmedGuardPostBag != null )
                    {
                        postType = direArmedGuardPostBag.PickRandomItemAndReplace( Context.RandomToUse );
                        if ( postType != null )
                            numberOfDires--;
                    }
                    if ( postType == null && hasArmed && Context.RandomToUse.Next( 0, 100 ) <= chanceForArmed )
                        postType = armedGuardPostBag.PickRandomItemAndReplace( Context.RandomToUse );
                    if ( postType == null && hasUnarmmed )
                        postType = unarmedGuardPostBag.PickRandomItemAndReplace( Context.RandomToUse );
                    if ( postType == null && hasArmed )
                        postType = armedGuardPostBag.PickRandomItemAndReplace( Context.RandomToUse );

                    if ( postType == null )
                    {
                        Engine_AIW2.Instance.LogErrorDuringCurrentMapGen( "Null guard postType " + i );
                        //Engine_AIW2.Instance.LogErrorDuringCurrentMapGen( "postType == null on planet " + ThisPlanet.Name + " (" + ThisPlanet.Index + ")." + AIBudgetItem.GetAIShipGroupDetails( pFaction.ShipGroup_GuardPosts ) );
                        continue;
                    }
                    debugStage = 440;
                    if ( IsReconquest && warpingType != null )
                    {
                        string finalName = postType.InternalName;

                        GameEntity_Squad warpingInPost = Helper_TryToSeedEntityWithPlanetMark_AtSpecificPoint( Context, ThisPlanet, point, faction, warpingType,
                           postType.ForMark[Balance_MarkLevelTable.Instance.MaxOrdinal].Radius / 2, postType.ForMark[Balance_MarkLevelTable.Instance.MaxOrdinal].Radius * 1, true );
                        AddedStrength += postType.GetForMark( ThisPlanet.MarkLevelForAIOnly.Ordinal ).StrengthPerSquad_CalculatedWithNullFleetMembership;
                        warpingInPost.TransformsIntoAfterTime = finalName;
                        warpingInPost.SecondsTillTransformation = (Int16)Context.RandomToUse.Next( 30, 280 );
                        result.RightItem = result.RightItem + 1;
                    }
                    else
                    {
                        int minDistance = postType.ForMark[Balance_MarkLevelTable.Instance.MaxOrdinal].Radius / 2;
                        int maxDistance = postType.ForMark[Balance_MarkLevelTable.Instance.MaxOrdinal].Radius;
                        GameEntity_Squad actualPost = Helper_TryToSeedEntityWithPlanetMark_AtSpecificPoint( Context, ThisPlanet, point, faction, postType,
                            minDistance, maxDistance, true );
                        if ( actualPost != null )
                        {
                            AddedStrength += actualPost.GetStrengthPerSquad();
                            result.RightItem = result.RightItem + 1;
                        }
                        else
                        {
                            Engine_AIW2.Instance.LogErrorDuringCurrentMapGen( "Null resulting guard post after trying to seed! index: " + i +
                                " Planet: " + (ThisPlanet == null ? "null" : ThisPlanet.Name) +
                                " Faction: " + (faction == null ? "null" : faction.GetDisplayName()) +
                                " postType: " + (postType == null ? "null" : postType.DisplayName) +
                                " minDistance: " + minDistance +
                                " maxDistance: " + maxDistance +
                                " point: " + point );
                            return result;
                        }
                    }
                }

                debugStage = 500;
                Int16 reinforcementLocationCount = 0;
                foreach ( GameEntity_Squad reinforcementPoint in ThisPlanet.Squads( EntityRollupType.ReinforcementLocations ) )
                {
                    reinforcementLocationCount++;
                }
                ThisPlanet.MaxReinforcementPlacesEverSeenHere = reinforcementLocationCount;
            }
            catch ( Exception e )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "Hit debugStage " + debugStage + " exception " + e + " in Helper_PlaceGuardPostsAtPointsList for " + ThisPlanet.Name, Verbosity.ShowAsError );
            }
            return result;
        }
        #endregion

        public virtual void ProcessAfterGameLoad( ArcenHostOnlySimContext Context, Planet ThisPlanet, Faction owningFaction = null ) { }
        public virtual RefPair<int, int> PlaceGuardPosts( ArcenHostOnlySimContext Context, Planet ThisPlanet, Faction faction, Tutorial.TutorialPlanetOwnershipAndSpawning TutorialPlanetOrNull,
            IAISentinelsCoreData SentinelsExternalData, bool IsReconquest, ref int AddedStrength ) { return RefPair<int, int>.Create( -5, -5 ); }

        #region Helper_StartListOfExistingReinforcementPoints
        public static void Helper_StartListOfExistingReinforcementPoints( List<ArcenPoint> listToFill, Planet ThisPlanet )
        {
            listToFill.Clear();
            foreach ( GameEntity_Squad reinforcer in ThisPlanet.Squads( EntityRollupType.ReinforcementLocations ) )
            {
                listToFill.Add( reinforcer.WorldLocation );
            }
        }
        #endregion

        //protected static int DebuggingInfo_InitialOutsideGravWellsCount;
        //protected static int DebuggingInfo_TooCloseToWormholeCount;
        //protected static int DebuggingInfo_TotalInternalLoopTries;
        //protected static int DebuggingInfo_TooCloseToOtherAvoidPointsCount;
        //protected static int DebuggingInfo_FinalOutsideGravWellsCount;
        //protected static int DebuggingInfo_OuterFailCount;

        //public static void ResetDebuggingInfoCounts()
        //{
        //    DebuggingInfo_InitialOutsideGravWellsCount = 0;
        //    DebuggingInfo_TooCloseToWormholeCount = 0;
        //    DebuggingInfo_TotalInternalLoopTries = 0;
        //    DebuggingInfo_TooCloseToOtherAvoidPointsCount = 0;
        //    DebuggingInfo_FinalOutsideGravWellsCount = 0;
        //    DebuggingInfo_OuterFailCount = 0;
        //}

        #region Helper_GetPointAroundACenterButNotTooCloseToOtherItems
        public static ArcenPoint Helper_GetPointAroundACenterButNotTooCloseToOtherItems( ArcenHostOnlySimContext Context, Planet ThisPlanet, ArcenPoint centerLocation, 
            int minDistance, int maxDistance, bool ClampMaxDistanceToGravWell, int minDistanceFromWormholes, int minDistanceFromOtherPoints, 
            int TotalTries, List<ArcenPoint> OtherPointsToAvoid, GameEntityTypeData TypeDataForTesting, out bool wasSuccess )
        {
            wasSuccess = false;

            if ( TypeDataForTesting == null )
            {
                ArcenDebugging.ArcenDebugLog( "Cannot Helper_GetPointAroundACenterButNotTooCloseToOtherItems, because entity type passed in was null!", Verbosity.ShowAsError );
                return Engine_AIW2.Instance.CombatCenter;
            }

            if ( ClampMaxDistanceToGravWell )
            {
                if ( maxDistance > ThisPlanet.GravWellSize.DistanceScale_GravwellRadius )
                    maxDistance = UnityEngine.Mathf.RoundToInt( ThisPlanet.GravWellSize.DistanceScale_GravwellRadius * 0.95f );
            }

            Context.RandomToUse.ReinitializeWithSeed( ThisPlanet.Index + World_AIW2.Instance.Setup.MapConfig.Seed );

            //if ( MapgenLogger.IsActive )
            //    MapgenLogger.Log( "GetPoints: minDistance: " + minDistance + " maxDistance: " + maxDistance + " minDistanceFromWormholes: " + minDistanceFromWormholes
            //        + " minDistanceFromOtherPoints: " + minDistanceFromOtherPoints + " OtherPointsToAvoid: " + OtherPointsToAvoid.Count );

            ArcenPoint newPointLocation = ArcenPoint.ZeroZeroPoint;
            for ( int loopCount = 0; loopCount < TotalTries; loopCount++ )
            {
                //DebuggingInfo_TotalInternalLoopTries++;
                newPointLocation = ThisPlanet.GetSafePlacementPoint_AroundDesiredPointVicinity( Context, TypeDataForTesting, centerLocation, minDistance, maxDistance );
                //if ( MapgenLogger.IsActive )
                //    MapgenLogger.Log( "GetPoints: centerLocation: " + centerLocation + " minDistance: " + minDistance + " maxDistance: " + maxDistance
                //        + " newPointLocation: " + newPointLocation );

                if ( ThisPlanet.GetIsPointOutsideGravWell_SlowButCorrect( newPointLocation ) )
                {
                    newPointLocation = ThisPlanet.GetPointOnRadiusOfGravWellIfOutOfRange_Slow( newPointLocation );
                }
                bool needsToTryAgain = false;
                foreach ( GameEntity_Other wormhole in ThisPlanet.Others() )
                {
                    if ( newPointLocation.GetDistanceTo( wormhole.WorldLocation, false ) < minDistanceFromWormholes )
                    {
                        //if ( MapgenLogger.IsActive )
                        //    MapgenLogger.Log( "GetPoints: chosen point: " + newPointLocation + " dist from wormhole: " +
                        //        newPointLocation.GetDistanceTo( wormhole.WorldLocation, false ) + " which is less than " + minDistanceFromWormholes );
                        //DebuggingInfo_TooCloseToWormholeCount++;
                        needsToTryAgain = true;
                        break;
                    }
                }
                //it wasn't too close to other wormholes, now let's check about other points
                if ( !needsToTryAgain )
                {
                    for ( int i =  0; i < OtherPointsToAvoid.Count; i++ )
                    {
                        if ( newPointLocation.GetDistanceTo( OtherPointsToAvoid[i], false ) < minDistanceFromOtherPoints )
                        {
                            //if ( MapgenLogger.IsActive )
                            //    MapgenLogger.Log( "GetPoints: chosen point: " + newPointLocation + " dist from avoid-point: " +
                            //        newPointLocation.GetDistanceTo( OtherPointsToAvoid[i], false ) + " which is less than " + minDistanceFromOtherPoints );

                            //DebuggingInfo_TooCloseToOtherAvoidPointsCount++;
                            needsToTryAgain = true;
                            break;
                        }
                    }
                }
                if ( !needsToTryAgain )
                {
                    //if ( MapgenLogger.IsActive )
                    //    MapgenLogger.Log( "GetPoints: chosen point: " + newPointLocation + " loopCount: " + loopCount );
                    wasSuccess = true;
                    break; //we're good!  It wasn't too close to any other points or anything like that
                }
            }

            //if we're saying something outside the gravity well, then put it in the center instead.
            if ( ThisPlanet.GetIsPointOutsideGravWell_SlowButCorrect( newPointLocation ) )
            {
                //if ( MapgenLogger.IsActive )
                //    MapgenLogger.Log( "GetPoints: was outside grav well: " + newPointLocation + " so alter to: " + ThisPlanet.GetPointOnRadiusOfGravWellIfOutOfRange_Slow( newPointLocation ) );
                wasSuccess = true;
                //DebuggingInfo_FinalOutsideGravWellsCount++;
                return ThisPlanet.GetPointOnRadiusOfGravWellIfOutOfRange_Slow( newPointLocation );
            }
            
            return newPointLocation;
        }
        #endregion

        #region Helper_SeedAroundWormholes
        public static void Helper_SeedAroundWormholes( ArcenHostOnlySimContext Context, Planet ThisPlanet, List<ArcenPoint> pointsToAvoid, List<ArcenPoint> pointsToSeed,
            int CountPerWormhole, GameEntityTypeData TypeDataForTesting, out int targetCount, out int seededCount )
        {
            if ( TypeDataForTesting == null )
            {
                ArcenDebugging.ArcenDebugLog( "Cannot Helper_SeedAroundWormholes, because entity type passed in was null!", Verbosity.ShowAsError );
                seededCount = 0;
                targetCount = 0;
                return;
            }

            targetCount = 0;
            int internalTargetCount = 0;
            seededCount = 0;
            int internalSeededCount = 0;
            foreach ( GameEntity_Other wormhole in ThisPlanet.Others() )
            {
                internalTargetCount++;
                int countRemainingToAdd = CountPerWormhole;
                int outerCount = 10;
                int minDistance = 520;
                int maxDistance = 1300;
                int minDistanceFromWormholes = 520;
                int minDistanceFromOtherPoints = 1300;
                while ( countRemainingToAdd > 0 && outerCount-- > 0 )
                {
                    bool wasSuccess;
                    ArcenPoint possiblePoint = Helper_GetPointAroundACenterButNotTooCloseToOtherItems( Context, ThisPlanet, wormhole.WorldLocation,
                        minDistance, maxDistance, true, minDistanceFromWormholes, minDistanceFromOtherPoints, 300, pointsToAvoid, TypeDataForTesting, out wasSuccess );
                    if ( wasSuccess )
                    {
                        countRemainingToAdd--;
                        internalSeededCount++;
                        pointsToAvoid.Add( possiblePoint );
                        pointsToSeed.Add( possiblePoint );
                    }
                    else //failed
                    {
                        //DebuggingInfo_OuterFailCount++;
                        minDistance += 1300;
                        maxDistance += 1300;
                        minDistanceFromWormholes -= 780;
                        if ( minDistanceFromWormholes < 300 )
                            minDistanceFromWormholes = 300;
                    }
                }
            }
            targetCount = internalTargetCount;
            seededCount = internalSeededCount;
        }
        #endregion

        #region Helper_SeedAroundPoint
        public static void Helper_SeedAroundPoint( ArcenHostOnlySimContext Context, Planet ThisPlanet, List<ArcenPoint> pointsToAvoid, List<ArcenPoint> pointsToSeed,
            ArcenPoint TargetPoint, int CountToSeed, int minDistanceFromTargetPoint, int maxDistanceFromTargetPoint, bool ClampDistanceToGravWell, int minDistanceFromWormholes, int minDistanceFromOtherPoints,
            int addedToDistanceFromCenterPerFail, int addedToDistanceFromWormholesPerFail, GameEntityTypeData TypeDataForTesting, out int seededCount )
        {
            if ( TypeDataForTesting == null )
            {
                ArcenDebugging.ArcenDebugLog( "Cannot Helper_SeedAroundPoint, because entity type passed in was null!", Verbosity.ShowAsError );
                seededCount = 0;
                return;
            }

            seededCount = 0;
            int countRemainingToAdd = CountToSeed;
            int outerCount = 10;
            while ( countRemainingToAdd > 0 && outerCount-- > 0 )
            {
                bool wasSuccess;
                ArcenPoint possiblePoint = Helper_GetPointAroundACenterButNotTooCloseToOtherItems( Context, ThisPlanet, TargetPoint,
                    minDistanceFromTargetPoint, maxDistanceFromTargetPoint, ClampDistanceToGravWell, minDistanceFromWormholes, minDistanceFromOtherPoints, 300, pointsToAvoid, TypeDataForTesting, out wasSuccess );
                if ( wasSuccess )
                {
                    countRemainingToAdd--;
                    seededCount++;
                    pointsToAvoid.Add( possiblePoint );
                    pointsToSeed.Add( possiblePoint );
                }
                else //failed
                {
                    //DebuggingInfo_OuterFailCount++;
                    minDistanceFromTargetPoint -= addedToDistanceFromCenterPerFail;
                    if ( minDistanceFromTargetPoint < 780 )
                        minDistanceFromTargetPoint = 780;
                    maxDistanceFromTargetPoint += addedToDistanceFromCenterPerFail;

                    minDistanceFromWormholes -= addedToDistanceFromWormholesPerFail;
                    if ( minDistanceFromWormholes < 520 )
                        minDistanceFromWormholes = 520;
                }
            }
        }
        #endregion

        #region Helper_SeedRandomDisperse
        public static void Helper_SeedRandomDisperse( ArcenHostOnlySimContext Context, Planet ThisPlanet, List<ArcenPoint> pointsToAvoid, List<ArcenPoint> pointsToSeed,
            int NumberToSeed, int minDistanceFromCenter, int maxDistanceFromCenter, int minDistanceFromWormholes, int minDistanceFromOtherPoints,
            int addedToDistanceFromCenterPerFail, int addedToDistanceFromWormholesPerFail, GameEntityTypeData TypeDataForTesting, out int seededCount )
        {
            if ( TypeDataForTesting == null )
            {
                ArcenDebugging.ArcenDebugLog( "Cannot Helper_SeedRandomDisperse, because entity type passed in was null!", Verbosity.ShowAsError );
                seededCount = 0;
                return;
            }

            seededCount = 0;
            int countRemainingToAdd = NumberToSeed;
            int outerCount = 10;
            while ( countRemainingToAdd > 0 && outerCount-- > 0 )
            {
                bool wasSuccess;
                ArcenPoint possiblePoint = Helper_GetPointAroundACenterButNotTooCloseToOtherItems( Context, ThisPlanet, Engine_AIW2.Instance.CombatCenter,
                    minDistanceFromCenter, maxDistanceFromCenter, true, minDistanceFromWormholes, minDistanceFromOtherPoints, 300, pointsToAvoid, TypeDataForTesting, out wasSuccess );
                if ( wasSuccess )
                {
                    countRemainingToAdd--;
                    seededCount++;
                    pointsToAvoid.Add( possiblePoint );
                    pointsToSeed.Add( possiblePoint );
                }
                else //failed
                {
                    //DebuggingInfo_OuterFailCount++;
                    minDistanceFromCenter -= addedToDistanceFromCenterPerFail;
                    if ( minDistanceFromCenter < 780 )
                        minDistanceFromCenter = 780;
                    maxDistanceFromCenter += addedToDistanceFromCenterPerFail;
                    
                    minDistanceFromWormholes -= addedToDistanceFromWormholesPerFail;
                    if ( minDistanceFromWormholes < 200 )
                        minDistanceFromWormholes = 200;
                }
            }
        }
        #endregion

        #region Helper_SeedRandomDisperse_Standard
        public static void Helper_SeedRandomDisperse_Standard( ArcenHostOnlySimContext Context, Planet ThisPlanet, List<ArcenPoint> pointsToAvoid, List<ArcenPoint> pointsToSeed,
            int NumberToSeed, GameEntityTypeData TypeDataForTesting, out int seededCount )
        {
            seededCount = 0;
            Helper_SeedRandomDisperse( Context, ThisPlanet, pointsToAvoid, pointsToSeed, NumberToSeed,
                1300, 900000,
                3900, 6500,
                1300, 500, TypeDataForTesting, out seededCount );
        }
        #endregion
    }

    #region AIGuardPostAndCommandPlacer_GuardingMetalDeposits
    public class AIGuardPostAndCommandPlacer_GuardingMetalDeposits : AIGuardPostAndCommandPlacers_Base
    {
        private readonly List<ArcenPoint> points = List<ArcenPoint>.Create_WillNeverBeGCed( 60, "AIGuardPostAndCommandPlacer_GuardingMetalDeposits-points" );

        public override RefPair<int, int> PlaceGuardPosts( ArcenHostOnlySimContext Context, Planet ThisPlanet, Faction faction, Tutorial.TutorialPlanetOwnershipAndSpawning TutorialPlanetOrNull, 
            IAISentinelsCoreData SentinelsExternalData, bool IsReconquest, ref int AddedStrength )
        {
            Context.RandomToUse.ReinitializeWithSeed( ThisPlanet.Index + World_AIW2.Instance.Setup.MapConfig.Seed );

            int debugStage = 1;
            try
            {
                points.Clear();
                foreach ( GameEntity_Squad entity in ThisPlanet.Squads( "MetalGenerator" ) )
                {
                    points.Add( entity.WorldLocation );
                }

                Helper_PlaceGuardPostsAtPointsList( Context, ThisPlanet, faction, TutorialPlanetOrNull, SentinelsExternalData, points, IsReconquest, ref AddedStrength );
            }
            catch ( Exception e )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "Hit debugStage " + debugStage + " exception " + e + " in AIGuardPostAndCommandPlacer_GuardingMetalDeposits for " + ThisPlanet.Name, Verbosity.ShowAsError );
            }
            return RefPair<int, int>.Create( -8, -8 );
        }
    }
    #endregion

    public class AIGuardPostAndCommandPlacer_GuardingWormholesGenerally : AIGuardPostAndCommandPlacers_Base
    {
        private readonly List<ArcenPoint> pointsToAvoid = List<ArcenPoint>.Create_WillNeverBeGCed( 60, "AIGuardPostAndCommandPlacer_GuardingWormholesGenerally-pointsToAvoid" );
        private readonly List<ArcenPoint> pointsToSeed = List<ArcenPoint>.Create_WillNeverBeGCed( 60, "AIGuardPostAndCommandPlacer_GuardingWormholesGenerally-pointsToSeed" );

        public override RefPair<int, int> PlaceGuardPosts( ArcenHostOnlySimContext Context, Planet ThisPlanet, Faction faction, Tutorial.TutorialPlanetOwnershipAndSpawning TutorialPlanetOrNull,
            IAISentinelsCoreData SentinelsExternalData, bool IsReconquest, ref int AddedStrength )
        {
            Context.RandomToUse.ReinitializeWithSeed( ThisPlanet.Index + World_AIW2.Instance.Setup.MapConfig.Seed );

            int debugStage = 1;
            try
            {
                pointsToAvoid.Clear();
                pointsToSeed.Clear();
                Helper_StartListOfExistingReinforcementPoints( pointsToAvoid, ThisPlanet );

                int totalTarget = 2;
                int totalPerWormhole = 1;
                switch ( ThisPlanet.MarkLevelForAIOnly.Ordinal )
                {
                    case 2:
                        totalTarget = 3;
                        break;
                    case 3:
                        totalTarget = 3;
                        break;
                    case 4:
                        totalTarget = 4;
                        break;
                    case 5:
                        totalTarget = 6;
                        break;
                    case 6:
                        totalTarget = 7;
                        totalPerWormhole = 2;
                        break;
                    case 7:
                        totalTarget = 9;
                        totalPerWormhole = 3;
                        break;
                }

                GameEntityTypeData commandStationTypeForTestPurposes = GameEntityTypeDataTable.Instance.GetRowByName( "DireSabotGuardPost" );
                if ( commandStationTypeForTestPurposes == null )
                {
                    ArcenDebugging.ArcenDebugLogSingleLine( "Sorry!  Can't generate a proper map.  We need a specific unit called DireSabotGuardPost for its name.  " +
                        "You can mark it as deprecated, so it does not show up in game, but its radius will be used for the sizing of other entities.", Verbosity.ShowAsError );
                    return RefPair<int,int>.Create( 0, 0 );
                }

                int targetCount = 0;
                int seededCount = 0;
                //ResetDebuggingInfoCounts();
                Helper_SeedAroundWormholes( Context, ThisPlanet, pointsToAvoid, pointsToSeed, totalPerWormhole, commandStationTypeForTestPurposes, out targetCount, out seededCount );
                if ( seededCount < totalTarget )
                    Helper_SeedRandomDisperse( Context, ThisPlanet, pointsToAvoid, pointsToSeed, totalTarget - seededCount,
                        780, 900000,
                        3900, 6500,
                        780, 520, commandStationTypeForTestPurposes, out seededCount );

                //if ( targetCount > pointsToSeed.Count )
                //    ArcenDebugging.ArcenDebugLogSingleLine( "Only could seed " + pointsToSeed.Count + " of " + targetCount + " guard posts for " + 
                //        ThisPlanet.Name + 
                //        " InitialOutsideGravWellsCount: " + DebuggingInfo_InitialOutsideGravWellsCount +
                //        " TooCloseToWormholeCount: " + DebuggingInfo_TooCloseToWormholeCount +
                //        " TotalInternalLoopTries: " + DebuggingInfo_TotalInternalLoopTries +
                //        " TooCloseToOtherAvoidPointsCount: " + DebuggingInfo_TooCloseToOtherAvoidPointsCount +
                //        " FinalOutsideGravWellsCount: " + DebuggingInfo_FinalOutsideGravWellsCount +
                //        " OuterFailCount: " + DebuggingInfo_OuterFailCount, Verbosity.DoNotShow );

                return Helper_PlaceGuardPostsAtPointsList( Context, ThisPlanet, faction, TutorialPlanetOrNull, SentinelsExternalData, pointsToSeed, IsReconquest, ref AddedStrength );
            }
            catch ( Exception e )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "Hit debugStage " + debugStage + " exception " + e + " in AIGuardPostAndCommandPlacer_GuardingWormholesGenerally for " + ThisPlanet.Name, Verbosity.ShowAsError );
            }
            return RefPair<int, int>.Create( -8, -8 );
        }
    }

    public class AIGuardPostAndCommandPlacer_GuardingWormholesHarshly : AIGuardPostAndCommandPlacers_Base
    {
        private readonly List<ArcenPoint> pointsToAvoid = List<ArcenPoint>.Create_WillNeverBeGCed( 60, "AIGuardPostAndCommandPlacer_GuardingWormholesHarshly-pointsToAvoid" );
        private readonly List<ArcenPoint> pointsToSeed = List<ArcenPoint>.Create_WillNeverBeGCed( 60, "AIGuardPostAndCommandPlacer_GuardingWormholesHarshly-pointsToSeed" );

        public override RefPair<int, int> PlaceGuardPosts( ArcenHostOnlySimContext Context, Planet ThisPlanet, Faction faction, Tutorial.TutorialPlanetOwnershipAndSpawning TutorialPlanetOrNull,
            IAISentinelsCoreData SentinelsExternalData, bool IsReconquest, ref int AddedStrength )
        {
            Context.RandomToUse.ReinitializeWithSeed( ThisPlanet.Index + World_AIW2.Instance.Setup.MapConfig.Seed );

            int debugStage = 1;
            try
            {
                pointsToAvoid.Clear();
                pointsToSeed.Clear();
                Helper_StartListOfExistingReinforcementPoints( pointsToAvoid, ThisPlanet );
                int totalTarget = 5;
                int totalPerWormhole = 2;
                switch ( ThisPlanet.MarkLevelForAIOnly.Ordinal )
                {
                    case 4:
                        totalTarget = 6;
                        break;
                    case 5:
                        totalTarget = 7;
                        totalPerWormhole = 3;
                        break;
                    case 6:
                        totalTarget = 9;
                        totalPerWormhole = 4;
                        break;
                    case 7:
                        totalTarget = 10;
                        totalPerWormhole = 5;
                        break;
                }

                GameEntityTypeData commandStationTypeForTestPurposes = GameEntityTypeDataTable.Instance.GetRowByName( "DireSabotGuardPost" );
                if ( commandStationTypeForTestPurposes == null )
                {
                    ArcenDebugging.ArcenDebugLogSingleLine( "Sorry!  Can't generate a proper map.  We need a specific unit called DireSabotGuardPost for its name.  " +
                        "You can mark it as deprecated, so it does not show up in game, but its radius will be used for the sizing of other entities.", Verbosity.ShowAsError );
                    return RefPair<int, int>.Create( 0, 0 );
                }

                int targetCount = 0;
                int seededCount = 0;
                //ResetDebuggingInfoCounts();
                Helper_SeedAroundWormholes( Context, ThisPlanet, pointsToAvoid, pointsToSeed, totalPerWormhole, commandStationTypeForTestPurposes, out targetCount, out seededCount );
                if ( seededCount < totalTarget )
                    Helper_SeedRandomDisperse( Context, ThisPlanet, pointsToAvoid, pointsToSeed, totalTarget - seededCount,
                        780, 900000,
                        3900, 6500,
                        780, 520, commandStationTypeForTestPurposes, out seededCount );

                //if ( targetCount > pointsToSeed.Count )
                //    ArcenDebugging.ArcenDebugLogSingleLine( "Only could seed " + pointsToSeed.Count + " of " + targetCount + " guard posts for " + 
                //        ThisPlanet.Name + 
                //        " InitialOutsideGravWellsCount: " + DebuggingInfo_InitialOutsideGravWellsCount +
                //        " TooCloseToWormholeCount: " + DebuggingInfo_TooCloseToWormholeCount +
                //        " TotalInternalLoopTries: " + DebuggingInfo_TotalInternalLoopTries +
                //        " TooCloseToOtherAvoidPointsCount: " + DebuggingInfo_TooCloseToOtherAvoidPointsCount +
                //        " FinalOutsideGravWellsCount: " + DebuggingInfo_FinalOutsideGravWellsCount +
                //        " OuterFailCount: " + DebuggingInfo_OuterFailCount, Verbosity.DoNotShow );

                return Helper_PlaceGuardPostsAtPointsList( Context, ThisPlanet, faction, TutorialPlanetOrNull, SentinelsExternalData, pointsToSeed, IsReconquest, ref AddedStrength );
            }
            catch ( Exception e )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "Hit debugStage " + debugStage + " exception " + e + " in AIGuardPostAndCommandPlacer_GuardingWormholesHarshly for " + ThisPlanet.Name, Verbosity.ShowAsError );
            }
            return RefPair<int, int>.Create( -8, -8 );
        }
    }

    public class AIGuardPostAndCommandPlacer_OneSingleMasss : AIGuardPostAndCommandPlacers_Base
    {
        private readonly List<ArcenPoint> pointsToAvoid = List<ArcenPoint>.Create_WillNeverBeGCed( 60, "AIGuardPostAndCommandPlacer_OneSingleMasss-pointsToAvoid" );
        private readonly List<ArcenPoint> pointsToSeed = List<ArcenPoint>.Create_WillNeverBeGCed( 60, "AIGuardPostAndCommandPlacer_OneSingleMasss-pointsToSeed" );

        public override RefPair<int, int> PlaceGuardPosts( ArcenHostOnlySimContext Context, Planet ThisPlanet, Faction faction, Tutorial.TutorialPlanetOwnershipAndSpawning TutorialPlanetOrNull,
            IAISentinelsCoreData SentinelsExternalData, bool IsReconquest, ref int AddedStrength )
        {
            Context.RandomToUse.ReinitializeWithSeed( ThisPlanet.Index + World_AIW2.Instance.Setup.MapConfig.Seed );

            int debugStage = 1;
            try
            {
                pointsToAvoid.Clear();
                pointsToSeed.Clear();
                Helper_StartListOfExistingReinforcementPoints( pointsToAvoid, ThisPlanet );
                int totalTarget = 2;
                switch ( ThisPlanet.MarkLevelForAIOnly.Ordinal )
                {
                    case 3:
                        totalTarget = Context.RandomToUse.NextInclus( 3, 4 );
                        break;
                    case 4:
                        totalTarget = Context.RandomToUse.NextInclus( 4, 5 );
                        break;
                    case 5:
                        totalTarget = Context.RandomToUse.NextInclus( 5, 7 );
                        break;
                    case 6:
                        totalTarget = Context.RandomToUse.NextInclus( 7, 9 );
                        break;
                    case 7:
                        totalTarget = Context.RandomToUse.NextInclus( 9, 12 );
                        break;
                }

                int minDistance = 780;
                int maxDistance = UnityEngine.Mathf.RoundToInt(ThisPlanet.GravWellSize.DistanceScale_GravwellRadius * 0.95f);
                ArcenPoint randomMassLocation = Engine_AIW2.Instance.CombatCenter.GetRandomPointWithinDistance( Context.RandomToUse, minDistance, maxDistance );

                GameEntityTypeData commandStationTypeForTestPurposes = GameEntityTypeDataTable.Instance.GetRowByName( "DireSabotGuardPost" );
                if ( commandStationTypeForTestPurposes == null )
                {
                    ArcenDebugging.ArcenDebugLogSingleLine( "Sorry!  Can't generate a proper map.  We need a specific unit called DireSabotGuardPost for its name.  " +
                        "You can mark it as deprecated, so it does not show up in game, but its radius will be used for the sizing of other entities.", Verbosity.ShowAsError );
                    return RefPair<int, int>.Create( 0, 0 );
                }

                int seededCount = 0;
                //ResetDebuggingInfoCounts();
                Helper_SeedAroundPoint( Context, ThisPlanet, pointsToAvoid, pointsToSeed, randomMassLocation, totalTarget,
                        1300, 3900, true,
                        3900, 780,
                        1300, 520, commandStationTypeForTestPurposes, out seededCount );

                //Helper_SeedAroundPoint
                if ( seededCount < totalTarget )
                    Helper_SeedRandomDisperse( Context, ThisPlanet, pointsToAvoid, pointsToSeed, totalTarget - seededCount,
                        780, 900000,
                        3900, 6500,
                        780, 500, commandStationTypeForTestPurposes, out seededCount );

                //if ( targetCount > pointsToSeed.Count )
                //    ArcenDebugging.ArcenDebugLogSingleLine( "Only could seed " + pointsToSeed.Count + " of " + targetCount + " guard posts for " + 
                //        ThisPlanet.Name + 
                //        " InitialOutsideGravWellsCount: " + DebuggingInfo_InitialOutsideGravWellsCount +
                //        " TooCloseToWormholeCount: " + DebuggingInfo_TooCloseToWormholeCount +
                //        " TotalInternalLoopTries: " + DebuggingInfo_TotalInternalLoopTries +
                //        " TooCloseToOtherAvoidPointsCount: " + DebuggingInfo_TooCloseToOtherAvoidPointsCount +
                //        " FinalOutsideGravWellsCount: " + DebuggingInfo_FinalOutsideGravWellsCount +
                //        " OuterFailCount: " + DebuggingInfo_OuterFailCount, Verbosity.DoNotShow );

                return Helper_PlaceGuardPostsAtPointsList( Context, ThisPlanet, faction, TutorialPlanetOrNull, SentinelsExternalData, pointsToSeed, IsReconquest, ref AddedStrength );
            }
            catch ( Exception e )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "Hit debugStage " + debugStage + " exception " + e + " in AIGuardPostAndCommandPlacer_OneSingleMasss for " + ThisPlanet.Name, Verbosity.ShowAsError );
            }
            return RefPair<int, int>.Create( -8, -8 );
        }
    }

    public class AIGuardPostAndCommandPlacer_ThreeSpreadOutGuardPosts : AIGuardPostAndCommandPlacers_Base
    {
        private readonly List<ArcenPoint> pointsToAvoid = List<ArcenPoint>.Create_WillNeverBeGCed( 60, "AIGuardPostAndCommandPlacer_ThreeSpreadOutGuardPosts-pointsToAvoid" );
        private readonly List<ArcenPoint> pointsToSeed = List<ArcenPoint>.Create_WillNeverBeGCed( 60, "AIGuardPostAndCommandPlacer_ThreeSpreadOutGuardPosts-pointsToSeed" );

        public override RefPair<int, int> PlaceGuardPosts( ArcenHostOnlySimContext Context, Planet ThisPlanet, Faction faction, Tutorial.TutorialPlanetOwnershipAndSpawning TutorialPlanetOrNull,
               IAISentinelsCoreData SentinelsExternalData, bool IsReconquest, ref int AddedStrength )
        {
            Context.RandomToUse.ReinitializeWithSeed( ThisPlanet.Index + World_AIW2.Instance.Setup.MapConfig.Seed );

            int debugStage = 1;
            try
            {
                pointsToAvoid.Clear();
                pointsToSeed.Clear();
                Helper_StartListOfExistingReinforcementPoints( pointsToAvoid, ThisPlanet );

                GameEntityTypeData commandStationTypeForTestPurposes = GameEntityTypeDataTable.Instance.GetRowByName( "DireSabotGuardPost" );
                if ( commandStationTypeForTestPurposes == null )
                {
                    ArcenDebugging.ArcenDebugLogSingleLine( "Sorry!  Can't generate a proper map.  We need a specific unit called DireSabotGuardPost for its name.  " +
                        "You can mark it as deprecated, so it does not show up in game, but its radius will be used for the sizing of other entities.", Verbosity.ShowAsError );
                    return RefPair<int, int>.Create( 0, 0 );
                }

                int seededCount = 0;
                //ResetDebuggingInfoCounts();                
                Helper_SeedRandomDisperse_Standard( Context, ThisPlanet, pointsToAvoid, pointsToSeed, 3, commandStationTypeForTestPurposes, out seededCount );

                //if ( targetCount > pointsToSeed.Count )
                //    ArcenDebugging.ArcenDebugLogSingleLine( "Only could seed " + pointsToSeed.Count + " of " + targetCount + " guard posts for " + 
                //        ThisPlanet.Name + 
                //        " InitialOutsideGravWellsCount: " + DebuggingInfo_InitialOutsideGravWellsCount +
                //        " TooCloseToWormholeCount: " + DebuggingInfo_TooCloseToWormholeCount +
                //        " TotalInternalLoopTries: " + DebuggingInfo_TotalInternalLoopTries +
                //        " TooCloseToOtherAvoidPointsCount: " + DebuggingInfo_TooCloseToOtherAvoidPointsCount +
                //        " FinalOutsideGravWellsCount: " + DebuggingInfo_FinalOutsideGravWellsCount +
                //        " OuterFailCount: " + DebuggingInfo_OuterFailCount, Verbosity.DoNotShow );

                return Helper_PlaceGuardPostsAtPointsList( Context, ThisPlanet, faction, TutorialPlanetOrNull, SentinelsExternalData, pointsToSeed, IsReconquest, ref AddedStrength );
            }
            catch ( Exception e )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "Hit debugStage " + debugStage + " exception " + e + " in AIGuardPostAndCommandPlacer_ThreeSpreadOutGuardPosts for " + ThisPlanet.Name, Verbosity.ShowAsError );
            }
            return RefPair<int, int>.Create( -8, -8 );
        }
    }

    public class AIGuardPostAndCommandPlacer_FourSpreadOutGuardPosts : AIGuardPostAndCommandPlacers_Base
    {
        private readonly List<ArcenPoint> pointsToAvoid = List<ArcenPoint>.Create_WillNeverBeGCed( 60, "AIGuardPostAndCommandPlacer_FourSpreadOutGuardPosts-pointsToAvoid" );
        private readonly List<ArcenPoint> pointsToSeed = List<ArcenPoint>.Create_WillNeverBeGCed( 60, "AIGuardPostAndCommandPlacer_FourSpreadOutGuardPosts-pointsToSeed" );

        public override RefPair<int, int> PlaceGuardPosts( ArcenHostOnlySimContext Context, Planet ThisPlanet, Faction faction, Tutorial.TutorialPlanetOwnershipAndSpawning TutorialPlanetOrNull,
               IAISentinelsCoreData SentinelsExternalData, bool IsReconquest, ref int AddedStrength )
        {
            Context.RandomToUse.ReinitializeWithSeed( ThisPlanet.Index + World_AIW2.Instance.Setup.MapConfig.Seed );

            int debugStage = 1;
            try
            {
                pointsToAvoid.Clear();
                pointsToSeed.Clear();
                Helper_StartListOfExistingReinforcementPoints( pointsToAvoid, ThisPlanet );

                GameEntityTypeData commandStationTypeForTestPurposes = GameEntityTypeDataTable.Instance.GetRowByName( "DireSabotGuardPost" );
                if ( commandStationTypeForTestPurposes == null )
                {
                    ArcenDebugging.ArcenDebugLogSingleLine( "Sorry!  Can't generate a proper map.  We need a specific unit called DireSabotGuardPost for its name.  " +
                        "You can mark it as deprecated, so it does not show up in game, but its radius will be used for the sizing of other entities.", Verbosity.ShowAsError );
                    return RefPair<int, int>.Create( 0, 0 );
                }

                int seededCount = 0;
                //ResetDebuggingInfoCounts();                
                Helper_SeedRandomDisperse_Standard( Context, ThisPlanet, pointsToAvoid, pointsToSeed, 4, commandStationTypeForTestPurposes, out seededCount );

                //if ( targetCount > pointsToSeed.Count )
                //    ArcenDebugging.ArcenDebugLogSingleLine( "Only could seed " + pointsToSeed.Count + " of " + targetCount + " guard posts for " + 
                //        ThisPlanet.Name + 
                //        " InitialOutsideGravWellsCount: " + DebuggingInfo_InitialOutsideGravWellsCount +
                //        " TooCloseToWormholeCount: " + DebuggingInfo_TooCloseToWormholeCount +
                //        " TotalInternalLoopTries: " + DebuggingInfo_TotalInternalLoopTries +
                //        " TooCloseToOtherAvoidPointsCount: " + DebuggingInfo_TooCloseToOtherAvoidPointsCount +
                //        " FinalOutsideGravWellsCount: " + DebuggingInfo_FinalOutsideGravWellsCount +
                //        " OuterFailCount: " + DebuggingInfo_OuterFailCount, Verbosity.DoNotShow );

                return Helper_PlaceGuardPostsAtPointsList( Context, ThisPlanet, faction, TutorialPlanetOrNull, SentinelsExternalData, pointsToSeed, IsReconquest, ref AddedStrength );
            }
            catch ( Exception e )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "Hit debugStage " + debugStage + " exception " + e + " in AIGuardPostAndCommandPlacer_FourSpreadOutGuardPosts for " + ThisPlanet.Name, Verbosity.ShowAsError );
            }
            return RefPair<int, int>.Create( -8, -8 );
        }
    }

    public class AIGuardPostAndCommandPlacer_SixSpreadOutGuardPosts : AIGuardPostAndCommandPlacers_Base
    {
        private readonly List<ArcenPoint> pointsToAvoid = List<ArcenPoint>.Create_WillNeverBeGCed( 60, "AIGuardPostAndCommandPlacer_SixSpreadOutGuardPosts-pointsToAvoid" );
        private readonly List<ArcenPoint> pointsToSeed = List<ArcenPoint>.Create_WillNeverBeGCed( 60, "AIGuardPostAndCommandPlacer_SixSpreadOutGuardPosts-pointsToSeed" );

        public override RefPair<int, int> PlaceGuardPosts( ArcenHostOnlySimContext Context, Planet ThisPlanet, Faction faction, Tutorial.TutorialPlanetOwnershipAndSpawning TutorialPlanetOrNull,
               IAISentinelsCoreData SentinelsExternalData, bool IsReconquest, ref int AddedStrength )
        {
            Context.RandomToUse.ReinitializeWithSeed( ThisPlanet.Index + World_AIW2.Instance.Setup.MapConfig.Seed );

            int debugStage = 1;
            try
            {
                pointsToAvoid.Clear();
                pointsToSeed.Clear();
                Helper_StartListOfExistingReinforcementPoints( pointsToAvoid, ThisPlanet );

                GameEntityTypeData commandStationTypeForTestPurposes = GameEntityTypeDataTable.Instance.GetRowByName( "DireSabotGuardPost" );
                if ( commandStationTypeForTestPurposes == null )
                {
                    ArcenDebugging.ArcenDebugLogSingleLine( "Sorry!  Can't generate a proper map.  We need a specific unit called DireSabotGuardPost for its name.  " +
                        "You can mark it as deprecated, so it does not show up in game, but its radius will be used for the sizing of other entities.", Verbosity.ShowAsError );
                    return RefPair<int, int>.Create( 0, 0 );
                }

                int seededCount = 0;
                //ResetDebuggingInfoCounts();                
                Helper_SeedRandomDisperse_Standard( Context, ThisPlanet, pointsToAvoid, pointsToSeed, 6, commandStationTypeForTestPurposes, out seededCount );

                //if ( targetCount > pointsToSeed.Count )
                //    ArcenDebugging.ArcenDebugLogSingleLine( "Only could seed " + pointsToSeed.Count + " of " + targetCount + " guard posts for " + 
                //        ThisPlanet.Name + 
                //        " InitialOutsideGravWellsCount: " + DebuggingInfo_InitialOutsideGravWellsCount +
                //        " TooCloseToWormholeCount: " + DebuggingInfo_TooCloseToWormholeCount +
                //        " TotalInternalLoopTries: " + DebuggingInfo_TotalInternalLoopTries +
                //        " TooCloseToOtherAvoidPointsCount: " + DebuggingInfo_TooCloseToOtherAvoidPointsCount +
                //        " FinalOutsideGravWellsCount: " + DebuggingInfo_FinalOutsideGravWellsCount +
                //        " OuterFailCount: " + DebuggingInfo_OuterFailCount, Verbosity.DoNotShow );

                return Helper_PlaceGuardPostsAtPointsList( Context, ThisPlanet, faction, TutorialPlanetOrNull, SentinelsExternalData, pointsToSeed, IsReconquest, ref AddedStrength );
            }
            catch ( Exception e )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "Hit debugStage " + debugStage + " exception " + e + " in AIGuardPostAndCommandPlacer_SixSpreadOutGuardPosts for " + ThisPlanet.Name, Verbosity.ShowAsError );
            }
            return RefPair<int, int>.Create( -8, -8 );
        }
    }

    public class AIGuardPostAndCommandPlacer_SixSpreadOutGuardPosts_AndTwoByCommand : AIGuardPostAndCommandPlacers_Base
    {
        private readonly List<ArcenPoint> pointsToAvoid = List<ArcenPoint>.Create_WillNeverBeGCed( 60, "AIGuardPostAndCommandPlacer_SixSpreadOutGuardPosts_AndTwoByCommand-pointsToAvoid" );
        private readonly List<ArcenPoint> pointsToSeed = List<ArcenPoint>.Create_WillNeverBeGCed( 60, "AIGuardPostAndCommandPlacer_SixSpreadOutGuardPosts_AndTwoByCommand-pointsToSeed" );

        public override RefPair<int, int> PlaceGuardPosts( ArcenHostOnlySimContext Context, Planet ThisPlanet, Faction faction, Tutorial.TutorialPlanetOwnershipAndSpawning TutorialPlanetOrNull,
            IAISentinelsCoreData SentinelsExternalData, bool IsReconquest, ref int AddedStrength )
        {
            Context.RandomToUse.ReinitializeWithSeed( ThisPlanet.Index + World_AIW2.Instance.Setup.MapConfig.Seed );

            int debugStage = 1;
            try
            {
                pointsToAvoid.Clear();
                pointsToSeed.Clear();
                Helper_StartListOfExistingReinforcementPoints( pointsToAvoid, ThisPlanet );

                int totalDisperseToSeed = 6;
                int totalAroundCommandToSeed = 2;
                int seededCount = 0;
                GameEntity_Squad commandStationToSeedAround = null;
                foreach ( GameEntity_Squad commandStation in ThisPlanet.Squads( EntityRollupType.CommandStation ) )
                {
                    commandStationToSeedAround = commandStation;
                    break;
                }
                GameEntityTypeData commandStationTypeForTestPurposes = GameEntityTypeDataTable.Instance.GetRowByName( "DireSabotGuardPost" );
                if ( commandStationTypeForTestPurposes == null )
                {
                    ArcenDebugging.ArcenDebugLogSingleLine( "Sorry!  Can't generate a proper map.  We need a specific unit called DireSabotGuardPost for its name.  " +
                        "You can mark it as deprecated, so it does not show up in game, but its radius will be used for the sizing of other entities.", Verbosity.ShowAsError );
                    return RefPair<int, int>.Create( 0, 0 );
                }
                if ( commandStationToSeedAround != null )
                {
                    ArcenPoint commandLocation = commandStationToSeedAround.WorldLocation;
                    Helper_SeedAroundPoint( Context, ThisPlanet, pointsToAvoid, pointsToSeed, commandLocation, totalAroundCommandToSeed,
                        780, 1520, true,
                        1300, 780,
                        520, 520, commandStationTypeForTestPurposes, out seededCount );
                }
                if ( seededCount < totalAroundCommandToSeed )
                    totalDisperseToSeed += (totalAroundCommandToSeed - seededCount);

                //ResetDebuggingInfoCounts();                
                Helper_SeedRandomDisperse_Standard( Context, ThisPlanet, pointsToAvoid, pointsToSeed, totalDisperseToSeed, commandStationTypeForTestPurposes, out seededCount );

                //if ( targetCount > pointsToSeed.Count )
                //    ArcenDebugging.ArcenDebugLogSingleLine( "Only could seed " + pointsToSeed.Count + " of " + targetCount + " guard posts for " + 
                //        ThisPlanet.Name + 
                //        " InitialOutsideGravWellsCount: " + DebuggingInfo_InitialOutsideGravWellsCount +
                //        " TooCloseToWormholeCount: " + DebuggingInfo_TooCloseToWormholeCount +
                //        " TotalInternalLoopTries: " + DebuggingInfo_TotalInternalLoopTries +
                //        " TooCloseToOtherAvoidPointsCount: " + DebuggingInfo_TooCloseToOtherAvoidPointsCount +
                //        " FinalOutsideGravWellsCount: " + DebuggingInfo_FinalOutsideGravWellsCount +
                //        " OuterFailCount: " + DebuggingInfo_OuterFailCount, Verbosity.DoNotShow );

                return Helper_PlaceGuardPostsAtPointsList( Context, ThisPlanet, faction, TutorialPlanetOrNull, SentinelsExternalData, pointsToSeed, IsReconquest, ref AddedStrength );
            }
            catch ( Exception e )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "Hit debugStage " + debugStage + " exception " + e + " in AIGuardPostAndCommandPlacer_SixSpreadOutGuardPosts_AndTwoByCommand for " + ThisPlanet.Name, Verbosity.ShowAsError );
            }
            return RefPair<int, int>.Create( -8, -8 );
        }
    }

    public class AIGuardPostAndCommandPlacer_EightSpreadOutGuardPosts : AIGuardPostAndCommandPlacers_Base
    {
        private readonly List<ArcenPoint> pointsToAvoid = List<ArcenPoint>.Create_WillNeverBeGCed( 60, "AIGuardPostAndCommandPlacer_EightSpreadOutGuardPosts-pointsToAvoid" );
        private readonly List<ArcenPoint> pointsToSeed = List<ArcenPoint>.Create_WillNeverBeGCed( 60, "AIGuardPostAndCommandPlacer_EightSpreadOutGuardPosts-pointsToSeed" );

        public override RefPair<int, int> PlaceGuardPosts( ArcenHostOnlySimContext Context, Planet ThisPlanet, Faction faction, Tutorial.TutorialPlanetOwnershipAndSpawning TutorialPlanetOrNull,
               IAISentinelsCoreData SentinelsExternalData, bool IsReconquest, ref int AddedStrength )
        {
            Context.RandomToUse.ReinitializeWithSeed( ThisPlanet.Index + World_AIW2.Instance.Setup.MapConfig.Seed );

            int debugStage = 1;
            try
            {
                pointsToAvoid.Clear();
                pointsToSeed.Clear();
                Helper_StartListOfExistingReinforcementPoints( pointsToAvoid, ThisPlanet );

                GameEntityTypeData commandStationTypeForTestPurposes = GameEntityTypeDataTable.Instance.GetRowByName( "DireSabotGuardPost" );
                if ( commandStationTypeForTestPurposes == null )
                {
                    ArcenDebugging.ArcenDebugLogSingleLine( "Sorry!  Can't generate a proper map.  We need a specific unit called DireSabotGuardPost for its name.  " +
                        "You can mark it as deprecated, so it does not show up in game, but its radius will be used for the sizing of other entities.", Verbosity.ShowAsError );
                    return RefPair<int, int>.Create( 0, 0 );
                }

                int seededCount = 0;
                //ResetDebuggingInfoCounts();                
                Helper_SeedRandomDisperse_Standard( Context, ThisPlanet, pointsToAvoid, pointsToSeed, 8, commandStationTypeForTestPurposes, out seededCount );

                //if ( targetCount > pointsToSeed.Count )
                //    ArcenDebugging.ArcenDebugLogSingleLine( "Only could seed " + pointsToSeed.Count + " of " + targetCount + " guard posts for " + 
                //        ThisPlanet.Name + 
                //        " InitialOutsideGravWellsCount: " + DebuggingInfo_InitialOutsideGravWellsCount +
                //        " TooCloseToWormholeCount: " + DebuggingInfo_TooCloseToWormholeCount +
                //        " TotalInternalLoopTries: " + DebuggingInfo_TotalInternalLoopTries +
                //        " TooCloseToOtherAvoidPointsCount: " + DebuggingInfo_TooCloseToOtherAvoidPointsCount +
                //        " FinalOutsideGravWellsCount: " + DebuggingInfo_FinalOutsideGravWellsCount +
                //        " OuterFailCount: " + DebuggingInfo_OuterFailCount, Verbosity.DoNotShow );

                return Helper_PlaceGuardPostsAtPointsList( Context, ThisPlanet, faction, TutorialPlanetOrNull, SentinelsExternalData, pointsToSeed, IsReconquest, ref AddedStrength );
            }
            catch ( Exception e )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "Hit debugStage " + debugStage + " exception " + e + " in AIGuardPostAndCommandPlacer_EightSpreadOutGuardPosts for " + ThisPlanet.Name, Verbosity.ShowAsError );
            }
            return RefPair<int, int>.Create( -8, -8 );
        }
    }

    public class AIGuardPostAndCommandPlacer_EightSpreadOutGuardPosts_AndTwoByCommand : AIGuardPostAndCommandPlacers_Base
    {
        private readonly List<ArcenPoint> pointsToAvoid = List<ArcenPoint>.Create_WillNeverBeGCed( 60, "AIGuardPostAndCommandPlacer_EightSpreadOutGuardPosts_AndTwoByCommand-pointsToAvoid" );
        private readonly List<ArcenPoint> pointsToSeed = List<ArcenPoint>.Create_WillNeverBeGCed( 60, "AIGuardPostAndCommandPlacer_EightSpreadOutGuardPosts_AndTwoByCommand-pointsToSeed" );

        public override RefPair<int, int> PlaceGuardPosts( ArcenHostOnlySimContext Context, Planet ThisPlanet, Faction faction, Tutorial.TutorialPlanetOwnershipAndSpawning TutorialPlanetOrNull,
            IAISentinelsCoreData SentinelsExternalData, bool IsReconquest, ref int AddedStrength )
        {
            Context.RandomToUse.ReinitializeWithSeed( ThisPlanet.Index + World_AIW2.Instance.Setup.MapConfig.Seed );

            int debugStage = 1;
            try
            {
                pointsToAvoid.Clear();
                pointsToSeed.Clear();
                Helper_StartListOfExistingReinforcementPoints( pointsToAvoid, ThisPlanet );

                int totalDisperseToSeed = 8;
                int totalAroundCommandToSeed = 2;
                int seededCount = 0;
                GameEntity_Squad commandStationToSeedAround = null;
                foreach ( GameEntity_Squad commandStation in ThisPlanet.Squads( EntityRollupType.CommandStation ) )
                {
                    commandStationToSeedAround = commandStation;
                    break;
                }
                GameEntityTypeData commandStationTypeForTestPurposes = GameEntityTypeDataTable.Instance.GetRowByName( "DireSabotGuardPost" );
                if ( commandStationTypeForTestPurposes == null )
                {
                    ArcenDebugging.ArcenDebugLogSingleLine( "Sorry!  Can't generate a proper map.  We need a specific unit called DireSabotGuardPost for its name.  " +
                        "You can mark it as deprecated, so it does not show up in game, but its radius will be used for the sizing of other entities.", Verbosity.ShowAsError );
                    return RefPair<int, int>.Create( 0, 0 );
                }

                if ( commandStationToSeedAround != null )
                {
                    ArcenPoint commandLocation = commandStationToSeedAround.WorldLocation;
                    Helper_SeedAroundPoint( Context, ThisPlanet, pointsToAvoid, pointsToSeed, commandLocation, totalAroundCommandToSeed,
                        780, 1300, true,
                        3900, 1300,
                        780, 520, commandStationTypeForTestPurposes, out seededCount );
                }
                if ( seededCount < totalAroundCommandToSeed )
                    totalDisperseToSeed += (totalAroundCommandToSeed - seededCount);

                //ResetDebuggingInfoCounts();                
                Helper_SeedRandomDisperse_Standard( Context, ThisPlanet, pointsToAvoid, pointsToSeed, totalDisperseToSeed, commandStationTypeForTestPurposes, out seededCount );

                //if ( targetCount > pointsToSeed.Count )
                //    ArcenDebugging.ArcenDebugLogSingleLine( "Only could seed " + pointsToSeed.Count + " of " + targetCount + " guard posts for " + 
                //        ThisPlanet.Name + 
                //        " InitialOutsideGravWellsCount: " + DebuggingInfo_InitialOutsideGravWellsCount +
                //        " TooCloseToWormholeCount: " + DebuggingInfo_TooCloseToWormholeCount +
                //        " TotalInternalLoopTries: " + DebuggingInfo_TotalInternalLoopTries +
                //        " TooCloseToOtherAvoidPointsCount: " + DebuggingInfo_TooCloseToOtherAvoidPointsCount +
                //        " FinalOutsideGravWellsCount: " + DebuggingInfo_FinalOutsideGravWellsCount +
                //        " OuterFailCount: " + DebuggingInfo_OuterFailCount, Verbosity.DoNotShow );

                return Helper_PlaceGuardPostsAtPointsList( Context, ThisPlanet, faction, TutorialPlanetOrNull, SentinelsExternalData, pointsToSeed, IsReconquest, ref AddedStrength );
            }
            catch ( Exception e )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "Hit debugStage " + debugStage + " exception " + e + " in AIGuardPostAndCommandPlacer_EightSpreadOutGuardPosts_AndTwoByCommand for " + ThisPlanet.Name, Verbosity.ShowAsError );
            }
            return RefPair<int, int>.Create( -8, -8 );
        }
    }

    public class AIGuardPostAndCommandPlacer_TenSpreadOutGuardPosts : AIGuardPostAndCommandPlacers_Base
    {
        private readonly List<ArcenPoint> pointsToAvoid = List<ArcenPoint>.Create_WillNeverBeGCed( 60, "AIGuardPostAndCommandPlacer_TenSpreadOutGuardPosts-pointsToAvoid" );
        private readonly List<ArcenPoint> pointsToSeed = List<ArcenPoint>.Create_WillNeverBeGCed( 60, "AIGuardPostAndCommandPlacer_TenSpreadOutGuardPosts-pointsToSeed" );

        public override RefPair<int, int> PlaceGuardPosts( ArcenHostOnlySimContext Context, Planet ThisPlanet, Faction faction, Tutorial.TutorialPlanetOwnershipAndSpawning TutorialPlanetOrNull,
               IAISentinelsCoreData SentinelsExternalData, bool IsReconquest, ref int AddedStrength )
        {
            Context.RandomToUse.ReinitializeWithSeed( ThisPlanet.Index + World_AIW2.Instance.Setup.MapConfig.Seed );

            int debugStage = 1;
            try
            {
                pointsToAvoid.Clear();
                pointsToSeed.Clear();
                Helper_StartListOfExistingReinforcementPoints( pointsToAvoid, ThisPlanet );

                GameEntityTypeData commandStationTypeForTestPurposes = GameEntityTypeDataTable.Instance.GetRowByName( "DireSabotGuardPost" );
                if ( commandStationTypeForTestPurposes == null )
                {
                    ArcenDebugging.ArcenDebugLogSingleLine( "Sorry!  Can't generate a proper map.  We need a specific unit called DireSabotGuardPost for its name.  " +
                        "You can mark it as deprecated, so it does not show up in game, but its radius will be used for the sizing of other entities.", Verbosity.ShowAsError );
                    return RefPair<int, int>.Create( 0, 0 );
                }

                int seededCount = 0;
                //ResetDebuggingInfoCounts();                
                Helper_SeedRandomDisperse_Standard( Context, ThisPlanet, pointsToAvoid, pointsToSeed, 10, commandStationTypeForTestPurposes, out seededCount );

                //if ( targetCount > pointsToSeed.Count )
                //    ArcenDebugging.ArcenDebugLogSingleLine( "Only could seed " + pointsToSeed.Count + " of " + targetCount + " guard posts for " + 
                //        ThisPlanet.Name + 
                //        " InitialOutsideGravWellsCount: " + DebuggingInfo_InitialOutsideGravWellsCount +
                //        " TooCloseToWormholeCount: " + DebuggingInfo_TooCloseToWormholeCount +
                //        " TotalInternalLoopTries: " + DebuggingInfo_TotalInternalLoopTries +
                //        " TooCloseToOtherAvoidPointsCount: " + DebuggingInfo_TooCloseToOtherAvoidPointsCount +
                //        " FinalOutsideGravWellsCount: " + DebuggingInfo_FinalOutsideGravWellsCount +
                //        " OuterFailCount: " + DebuggingInfo_OuterFailCount, Verbosity.DoNotShow );

                return Helper_PlaceGuardPostsAtPointsList( Context, ThisPlanet, faction, TutorialPlanetOrNull, SentinelsExternalData, pointsToSeed, IsReconquest, ref AddedStrength );
            }
            catch ( Exception e )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "Hit debugStage " + debugStage + " exception " + e + " in AIGuardPostAndCommandPlacer_TenSpreadOutGuardPosts for " + ThisPlanet.Name, Verbosity.ShowAsError );
            }
            return RefPair<int, int>.Create( -8, -8 );
        }
    }

    public class AIGuardPostAndCommandPlacer_TwelveSpreadOutGuardPosts : AIGuardPostAndCommandPlacers_Base
    {
        private readonly List<ArcenPoint> pointsToAvoid = List<ArcenPoint>.Create_WillNeverBeGCed( 60, "AIGuardPostAndCommandPlacer_TwelveSpreadOutGuardPosts-pointsToAvoid" );
        private readonly List<ArcenPoint> pointsToSeed = List<ArcenPoint>.Create_WillNeverBeGCed( 60, "AIGuardPostAndCommandPlacer_TwelveSpreadOutGuardPosts-pointsToSeed" );

        public override RefPair<int, int> PlaceGuardPosts( ArcenHostOnlySimContext Context, Planet ThisPlanet, Faction faction, Tutorial.TutorialPlanetOwnershipAndSpawning TutorialPlanetOrNull,
               IAISentinelsCoreData SentinelsExternalData, bool IsReconquest, ref int AddedStrength )
        {
            Context.RandomToUse.ReinitializeWithSeed( ThisPlanet.Index + World_AIW2.Instance.Setup.MapConfig.Seed );

            int debugStage = 1;
            try
            {
                pointsToAvoid.Clear();
                pointsToSeed.Clear();
                Helper_StartListOfExistingReinforcementPoints( pointsToAvoid, ThisPlanet );

                GameEntityTypeData commandStationTypeForTestPurposes = GameEntityTypeDataTable.Instance.GetRowByName( "DireSabotGuardPost" );
                if ( commandStationTypeForTestPurposes == null )
                {
                    ArcenDebugging.ArcenDebugLogSingleLine( "Sorry!  Can't generate a proper map.  We need a specific unit called DireSabotGuardPost for its name.  " +
                        "You can mark it as deprecated, so it does not show up in game, but its radius will be used for the sizing of other entities.", Verbosity.ShowAsError );
                    return RefPair<int, int>.Create( 0, 0 );
                }

                int seededCount = 0;
                //ResetDebuggingInfoCounts();                
                Helper_SeedRandomDisperse_Standard( Context, ThisPlanet, pointsToAvoid, pointsToSeed, 12, commandStationTypeForTestPurposes, out seededCount );

                //if ( targetCount > pointsToSeed.Count )
                //    ArcenDebugging.ArcenDebugLogSingleLine( "Only could seed " + pointsToSeed.Count + " of " + targetCount + " guard posts for " + 
                //        ThisPlanet.Name + 
                //        " InitialOutsideGravWellsCount: " + DebuggingInfo_InitialOutsideGravWellsCount +
                //        " TooCloseToWormholeCount: " + DebuggingInfo_TooCloseToWormholeCount +
                //        " TotalInternalLoopTries: " + DebuggingInfo_TotalInternalLoopTries +
                //        " TooCloseToOtherAvoidPointsCount: " + DebuggingInfo_TooCloseToOtherAvoidPointsCount +
                //        " FinalOutsideGravWellsCount: " + DebuggingInfo_FinalOutsideGravWellsCount +
                //        " OuterFailCount: " + DebuggingInfo_OuterFailCount, Verbosity.DoNotShow );

                return Helper_PlaceGuardPostsAtPointsList( Context, ThisPlanet, faction, TutorialPlanetOrNull, SentinelsExternalData, pointsToSeed, IsReconquest, ref AddedStrength );
            }
            catch ( Exception e )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "Hit debugStage " + debugStage + " exception " + e + " in AIGuardPostAndCommandPlacer_TwelveSpreadOutGuardPosts for " + ThisPlanet.Name, Verbosity.ShowAsError );
            }
            return RefPair<int, int>.Create( -8, -8 );
        }
    }

    public class AIGuardPostAndCommandPlacer_AIHome_TwelveSpreadOutGuardPosts : AIGuardPostAndCommandPlacers_Base
    {
        private readonly List<ArcenPoint> pointsToAvoid = List<ArcenPoint>.Create_WillNeverBeGCed( 60, "AIGuardPostAndCommandPlacer_AIHome_TwelveSpreadOutGuardPosts-pointsToAvoid" );
        private readonly List<ArcenPoint> pointsToSeed = List<ArcenPoint>.Create_WillNeverBeGCed( 60, "AIGuardPostAndCommandPlacer_AIHome_TwelveSpreadOutGuardPosts-pointsToSeed" );

        public override RefPair<int, int> PlaceGuardPosts( ArcenHostOnlySimContext Context, Planet ThisPlanet, Faction faction, Tutorial.TutorialPlanetOwnershipAndSpawning TutorialPlanetOrNull,
               IAISentinelsCoreData SentinelsExternalData, bool IsReconquest, ref int AddedStrength )
        {
            Context.RandomToUse.ReinitializeWithSeed( ThisPlanet.Index + World_AIW2.Instance.Setup.MapConfig.Seed );

            int debugStage = 1;
            try
            {
                pointsToAvoid.Clear();
                pointsToSeed.Clear();
                Helper_StartListOfExistingReinforcementPoints( pointsToAvoid, ThisPlanet );

                GameEntityTypeData commandStationTypeForTestPurposes = GameEntityTypeDataTable.Instance.GetRowByName( "DireSabotGuardPost" );
                if ( commandStationTypeForTestPurposes == null )
                {
                    ArcenDebugging.ArcenDebugLogSingleLine( "Sorry!  Can't generate a proper map.  We need a specific unit called DireSabotGuardPost for its name.  " +
                        "You can mark it as deprecated, so it does not show up in game, but its radius will be used for the sizing of other entities.", Verbosity.ShowAsError );
                    return RefPair<int, int>.Create( 0, 0 );
                }

                int seededCount = 0;
                //ResetDebuggingInfoCounts();                
                Helper_SeedRandomDisperse_Standard( Context, ThisPlanet, pointsToAvoid, pointsToSeed, 12, commandStationTypeForTestPurposes, out seededCount );

                //if ( targetCount > pointsToSeed.Count )
                //    ArcenDebugging.ArcenDebugLogSingleLine( "Only could seed " + pointsToSeed.Count + " of " + targetCount + " guard posts for " + 
                //        ThisPlanet.Name + 
                //        " InitialOutsideGravWellsCount: " + DebuggingInfo_InitialOutsideGravWellsCount +
                //        " TooCloseToWormholeCount: " + DebuggingInfo_TooCloseToWormholeCount +
                //        " TotalInternalLoopTries: " + DebuggingInfo_TotalInternalLoopTries +
                //        " TooCloseToOtherAvoidPointsCount: " + DebuggingInfo_TooCloseToOtherAvoidPointsCount +
                //        " FinalOutsideGravWellsCount: " + DebuggingInfo_FinalOutsideGravWellsCount +
                //        " OuterFailCount: " + DebuggingInfo_OuterFailCount, Verbosity.DoNotShow );

                return Helper_PlaceGuardPostsAtPointsList( Context, ThisPlanet, faction, TutorialPlanetOrNull, SentinelsExternalData, pointsToSeed, IsReconquest, ref AddedStrength );
            }
            catch ( Exception e )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "Hit debugStage " + debugStage + " exception " + e + " in AIGuardPostAndCommandPlacer_AIHome_TwelveSpreadOutGuardPosts for " + ThisPlanet.Name, Verbosity.ShowAsError );
            }
            return RefPair<int, int>.Create( -8, -8 );
        }
    }

    public class AIGuardPostAndCommandPlacer_AIHome_TenSpreadOutGuardPostsAndTwoByCommand : AIGuardPostAndCommandPlacers_Base
    {
        private readonly List<ArcenPoint> pointsToAvoid = List<ArcenPoint>.Create_WillNeverBeGCed( 60, "AIGuardPostAndCommandPlacer_AIHome_TenSpreadOutGuardPostsAndTwoByCommand-pointsToAvoid" );
        private readonly List<ArcenPoint> pointsToSeed = List<ArcenPoint>.Create_WillNeverBeGCed( 60, "AIGuardPostAndCommandPlacer_AIHome_TenSpreadOutGuardPostsAndTwoByCommand-pointsToSeed" );

        public override RefPair<int, int> PlaceGuardPosts( ArcenHostOnlySimContext Context, Planet ThisPlanet, Faction faction, Tutorial.TutorialPlanetOwnershipAndSpawning TutorialPlanetOrNull,
            IAISentinelsCoreData SentinelsExternalData, bool IsReconquest, ref int AddedStrength )
        {
            Context.RandomToUse.ReinitializeWithSeed( ThisPlanet.Index + World_AIW2.Instance.Setup.MapConfig.Seed );

            int debugStage = 1;
            try
            {
                pointsToAvoid.Clear();
                pointsToSeed.Clear();
                Helper_StartListOfExistingReinforcementPoints( pointsToAvoid, ThisPlanet );

                int totalDisperseToSeed = 10;
                int totalAroundCommandToSeed = 2;
                int seededCount = 0;
                GameEntity_Squad commandStationToSeedAround = null;
                foreach ( GameEntity_Squad commandStation in ThisPlanet.Squads( EntityRollupType.CommandStation ) )
                {
                    commandStationToSeedAround = commandStation;
                    break;
                }
                GameEntityTypeData commandStationTypeForTestPurposes = GameEntityTypeDataTable.Instance.GetRowByName( "DireSabotGuardPost" );
                if ( commandStationTypeForTestPurposes == null )
                {
                    ArcenDebugging.ArcenDebugLogSingleLine( "Sorry!  Can't generate a proper map.  We need a specific unit called DireSabotGuardPost for its name.  " +
                        "You can mark it as deprecated, so it does not show up in game, but its radius will be used for the sizing of other entities.", Verbosity.ShowAsError );
                    return RefPair<int, int>.Create( 0, 0 );
                }
                if ( commandStationToSeedAround != null )
                {
                    ArcenPoint commandLocation = commandStationToSeedAround.WorldLocation;
                    Helper_SeedAroundPoint( Context, ThisPlanet, pointsToAvoid, pointsToSeed, commandLocation, totalAroundCommandToSeed,
                        780, 1300, true,
                        3900, 780,
                        1300, 520, commandStationTypeForTestPurposes, out seededCount );
                }
                if ( seededCount < totalAroundCommandToSeed )
                    totalDisperseToSeed += (totalAroundCommandToSeed - seededCount);

                //ResetDebuggingInfoCounts();                
                Helper_SeedRandomDisperse_Standard( Context, ThisPlanet, pointsToAvoid, pointsToSeed, totalDisperseToSeed, commandStationTypeForTestPurposes, out seededCount );

                //if ( targetCount > pointsToSeed.Count )
                //    ArcenDebugging.ArcenDebugLogSingleLine( "Only could seed " + pointsToSeed.Count + " of " + targetCount + " guard posts for " + 
                //        ThisPlanet.Name + 
                //        " InitialOutsideGravWellsCount: " + DebuggingInfo_InitialOutsideGravWellsCount +
                //        " TooCloseToWormholeCount: " + DebuggingInfo_TooCloseToWormholeCount +
                //        " TotalInternalLoopTries: " + DebuggingInfo_TotalInternalLoopTries +
                //        " TooCloseToOtherAvoidPointsCount: " + DebuggingInfo_TooCloseToOtherAvoidPointsCount +
                //        " FinalOutsideGravWellsCount: " + DebuggingInfo_FinalOutsideGravWellsCount +
                //        " OuterFailCount: " + DebuggingInfo_OuterFailCount, Verbosity.DoNotShow );

                return Helper_PlaceGuardPostsAtPointsList( Context, ThisPlanet, faction, TutorialPlanetOrNull, SentinelsExternalData, pointsToSeed, IsReconquest, ref AddedStrength );
            }
            catch ( Exception e )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "Hit debugStage " + debugStage + " exception " + e + " in AIGuardPostAndCommandPlacer_AIHome_TenSpreadOutGuardPostsAndTwoByCommand for " + ThisPlanet.Name, Verbosity.ShowAsError );
            }
            return RefPair<int, int>.Create( -8, -8 );
        }
    }

}
