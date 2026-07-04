using Arcen.Universal;
using System;
using Arcen.AIW2.Core;

namespace Arcen.AIW2.External
{
    public class ProtectionPlanning : ArcenShortTermPlanningContext, IBetweenMapGenPoolable<ProtectionPlanning>
    {
        private static ReferenceTracker RefTracker;
        private ProtectionPlanning()
            : base( "_ST.ProtectionPlanning" )
        {
            if ( RefTracker == null )
                RefTracker = new ReferenceTracker( "ProtectionPlanning" );
            RefTracker.IncrementObjectCount();
        }

        public static int Accumulator_PRB = 0;
        public static int Accumulator_PRF = 0;

        #region Pooling
        public void WipeForReuseAsNewObject()
        {
            Accumulator_PRB = 0;
            Accumulator_PRF = 0;
        }

        private static readonly BetweenMapGenPool<ProtectionPlanning> Pool = BetweenMapGenPool<ProtectionPlanning>.Create_WillNeverBeGCed( "ProtectionPlanning", 30, 10,
            PoolBehaviorDuringShutdown.BlockAllThreads, delegate { return new ProtectionPlanning(); } );

        public static ProtectionPlanning GetFromPoolOrCreate()
        {
            ProtectionPlanning context = Pool.GetFromPoolOrCreate();
            return context;
        }
        #endregion

        private ThreadingExchanger IsCurrentlyWaitingOnThread = new ThreadingExchanger( "ProtectionPlanning", 30f );

        public override void Execute()
        {
            if ( CentralVars.DEBUG_TURN_OFF_SHORT_TERM_PLANNNING_ALL )
                return;
            if ( CentralVars.DEBUG_TURN_OFF_PROTECTION_PLAN )
                return;
            if ( IsCurrentlyWaitingOnThread.IsBusy() )
                return;

            if ( !IsCurrentlyWaitingOnThread.DoNextOnlyIfNotAlreadyBusy( string.Empty ) )
                return;
            try
            {
                Interlocked.Increment( ref Accumulator_PRB );

                foreach ( Planet planet in World_AIW2.Instance.Planets( false ) )
                {
                    // for all entities
                    foreach ( GameEntity_Squad entity in planet.Squads() )
                    {
                        entity.IsUnderDamageReducingShield.ClearConstructionValueForStartingConstruction();
                        entity.ProtectingShields.ClearConstructionListForStartingConstruction();
                        
                        entity.ShouldDoShieldDecollision.ClearConstructionValueForStartingConstruction();
                        entity.ShieldDecollision_NextMovePoint.ClearConstructionValueForStartingConstruction();
                        
                    }

                    foreach ( GameEntity_Squad _pp_e in planet.Squads( EntityRollupType.ProjectsForcefield ) )
                        DelegateHelper_ProtectionPlanning_CheckShieldGenerator( _pp_e );
                    foreach ( GameEntity_Squad _pp_e in planet.Squads( EntityRollupType.DisplacesOtherShipsPlanetTier ) )
                        DelegateHelper_ProtectionPlanning_CheckDisplacesOtherShipsPlanetTier( _pp_e );
                    foreach ( GameEntity_Squad _pp_e in planet.Squads( EntityRollupType.DisplacesOtherShipsGiantObjectTier ) )
                        DelegateHelper_ProtectionPlanning_CheckDisplacesOtherShipsGiantObjectTier( _pp_e );
                    foreach ( GameEntity_Squad _pp_e in planet.Squads( EntityRollupType.DisplacesOtherImmobileObjectsAsIfWerePlanetTier ) )
                        DelegateHelper_ProtectionPlanning_CheckDisplacesOtherImmobileObjectsAsIfWerePlanetTier( _pp_e );

                    // for all entities
                    foreach ( GameEntity_Squad entity in planet.Squads() )
                    {
                        entity.IsUnderDamageReducingShield.SwitchConstructionToDisplay();
                        entity.ProtectingShields.SwitchConstructionToDisplay();
                        
                        entity.ShouldDoShieldDecollision.SwitchConstructionToDisplay();
                        entity.ShieldDecollision_NextMovePoint.SwitchConstructionToDisplay();
                        
                    }

                    // for all shots
                    foreach ( GameEntity_Shot shot in planet.Shots() )
                    {
                        //shots that fire through shields should not try to hit them
                        if ( shot.OriginOrNull != null && shot.OriginOrNull.TypeData.FiresThroughEnemyShields )
                            continue;
                        if ( shot.FramePlan_Protection_IsShotThatHasEnteredAShieldProtectingTheTarget.GetPrimaryKeyID() > 0 )
                            continue;
                        
                        var target = shot.Target.GetSquad();
                        if ( target == null )
                            continue;
                        
                        var shields = target.ProtectingShields.GetDisplayList();
                        if ( shields.Count > 0 )
                        {
                            for ( int shieldIndex = 0; shieldIndex < shields.Count; shieldIndex++ )
                            {
                                var protector = shields[shieldIndex].GetSquad();
                                if ( protector == null )
                                    continue;
                                
                                if ( !shot.GetIsWithinRangeOf( protector.WorldLocation, protector.CalculatedCurrentShieldRadius ) )
                                    continue;
                                
                                shot.FramePlan_Protection_IsShotThatHasEnteredAShieldProtectingTheTarget = LazyLoadSquadWrapper.Create( protector );
                                
                                continue;
                            }
                        }
                        
                        if ( target.TypeData.ProjectsForcefield && target.GetCurrentShieldPoints() > 0 )
                        {
                            var protector = target;
                            if ( shot.GetIsWithinRangeOf( protector.WorldLocation, protector.CalculatedCurrentShieldRadius ) )
                            {
                                shot.FramePlan_Protection_IsShotThatHasEnteredAShieldProtectingTheTarget = LazyLoadSquadWrapper.Create( protector );
                                continue;
                            }
                        }
                        
                    }
                }

                Interlocked.Increment( ref Accumulator_PRF );
                IsCurrentlyWaitingOnThread.MarkAsNoLongerBusy();
            }
            catch ( Exception e )
            {
                IsCurrentlyWaitingOnThread.MarkAsNoLongerBusy();
                ArcenDebugging.ArcenDebugLog( "Exception in ProtectionPlanning.Execute:" + e.ToString(), Verbosity.ShowAsError );
            }
        }

        private bool isProtectable;
        private bool isDisplaceable;

        private DelReturn DelegateHelper_ProtectionPlanning_CheckShieldGenerator( GameEntity_Squad shieldGenerator )
        {
            int radius = shieldGenerator.CalculatedCurrentShieldRadius;

            //ArcenDebugging.ArcenDebugLogSingleLine( shieldGenerator.TypeData.InternalName + " radius check " + radius, Verbosity.DoNotShow );

            if ( radius <= 0 )
                return DelReturn.Continue;

            FactionRelationship protectionRelationship = FactionRelationship.Self.Add( FactionRelationship.FactionsIAmFriendlyTowards );
            FactionRelationship displacementRelationship = FactionRelationship.FactionsIAmHostileTowards;
            if (shieldGenerator.TypeData.OriginalXmlData.GetBool( "custom_forcefield_is_nonblocking", false, false ))
                displacementRelationship = FactionRelationship.None;
            
            DelegateHelper_ProtectionPlanning_shieldGeneratorCenter = shieldGenerator.WorldLocation;
            DelegateHelper_ProtectionPlanning_radius = radius;
            DelegateHelper_ProtectionPlanning_shieldGenerator = shieldGenerator;
            
            for ( int i = 0; i < shieldGenerator.Planet.Factions.Count; i++ )
            {
                var pfac = shieldGenerator.Planet.Factions[i];
                if ( pfac == null )
                    continue;
                
                isProtectable = shieldGenerator.PlanetFaction.GetHasRelationshipWith( pfac, protectionRelationship );
                isDisplaceable = shieldGenerator.PlanetFaction.GetHasRelationshipWith( pfac, displacementRelationship );
                
                // Doing this centrally and only once per faction so efficient with many entities.
                if ( !isProtectable && !isDisplaceable )
                    continue;

                foreach ( GameEntity_Squad _pp_e in pfac.Entities.Squads() )
                    DelegateHelper_ProtectionPlanning_TryToProtectOrDisplace( _pp_e );
            }
            
            DelegateHelper_ProtectionPlanning_shieldGenerator = null;
            
            return DelReturn.Continue;
        }

        private DelReturn DelegateHelper_ProtectionPlanning_CheckDisplacesOtherShipsPlanetTier( GameEntity_Squad giantObject )
        {
            if ( giantObject == null )
                return DelReturn.Continue;
            if ( !giantObject.TypeData.DisplacesOtherShipsPlanetTier )
                return DelReturn.Continue;

            DelegateHelper_ProtectionPlanning_shieldGeneratorCenter = giantObject.WorldLocation;
            DelegateHelper_ProtectionPlanning_radius = giantObject.DataForMark.Radius + giantObject.TypeData.AddedDisplacementBuffer;
            DelegateHelper_ProtectionPlanning_shieldGenerator = giantObject;

            PlanetFaction fac;
            for ( int i = 0; i < giantObject.Planet.Factions.Count; i++ )
            {
                fac = giantObject.Planet.Factions[i];
                if ( fac == null )
                    continue;

                foreach ( GameEntity_Squad _pp_e in fac.Entities.Squads() )
                    DelegateHelper_ProtectionPlanning_TryToDisplacesOtherShips( _pp_e );
            }
            return DelReturn.Continue;
        }
        
        private DelReturn DelegateHelper_ProtectionPlanning_CheckDisplacesOtherShipsGiantObjectTier( GameEntity_Squad giantObject )
        {
            if ( giantObject == null )
                return DelReturn.Continue;
            if ( !giantObject.TypeData.DisplacesOtherShipsGiantObjectTier )
                return DelReturn.Continue;

            DelegateHelper_ProtectionPlanning_shieldGeneratorCenter = giantObject.WorldLocation;
            DelegateHelper_ProtectionPlanning_radius = giantObject.DataForMark.Radius + giantObject.TypeData.AddedDisplacementBuffer;
            DelegateHelper_ProtectionPlanning_shieldGenerator = giantObject;

            PlanetFaction fac;
            for ( int i = 0; i < giantObject.Planet.Factions.Count; i++ )
            {
                fac = giantObject.Planet.Factions[i];
                if ( fac == null )
                    continue;

                foreach ( GameEntity_Squad _pp_e in fac.Entities.Squads() )
                    DelegateHelper_ProtectionPlanning_TryToDisplacesOtherShips( _pp_e );
            }
            DelegateHelper_ProtectionPlanning_shieldGenerator = null;
            return DelReturn.Continue;
        }

        private DelReturn DelegateHelper_ProtectionPlanning_TryToDisplacesOtherShips( GameEntity_Squad otherEntity )
        {
            if ( otherEntity == null )
                return DelReturn.Continue;
            GameEntityTypeData.MarkLevelStats otherMarkData = otherEntity.DataForMark;
            if ( otherMarkData == null )
                return DelReturn.Continue;
            GameEntityTypeData otherTypeData = otherEntity.TypeData;
            if ( otherTypeData == null )
                return DelReturn.Continue;

            GameEntity_Squad giantObject = DelegateHelper_ProtectionPlanning_shieldGenerator;
            if ( giantObject == otherEntity )
                return DelReturn.Continue;
            if ( giantObject == null )
                return DelReturn.Continue;

            if ( otherEntity.TypeData.DisplacesOtherShipsPlanetTier )
                return DelReturn.Continue; //nothing pushes planet-tier, and planet-tier don't push each other

            bool isPlanetTier = giantObject.TypeData.DisplacesOtherShipsPlanetTier;
            if ( isPlanetTier )
            {
                if ( otherEntity.TypeData.HasSpecialImmunityToPlanetTierDisplacement )
                    return DelReturn.Continue; //planet-tier don't push those with a special immunity to it
                if ( (otherTypeData.IsMobileOrbiter || otherTypeData.IsPlanetaryOrbiter) && string.IsNullOrEmpty(otherTypeData.OrbitsParentUnlessParentSystemHasTarget) )
                    return DelReturn.Continue; //also don't push orbiters

                //beyond that?  Yeah, they push
            }
            else
            {
                //not planet tier... so what can we push?

                if ( otherEntity.TypeData.ProjectsForcefield || !otherEntity.TypeData.IsMobile || otherEntity.TypeData.PushesEnemyShields )
                    return DelReturn.Continue; //if it isn't mobile, or if it's a forcefield projector, then it can't be pushed, or if it has the norris effect can't be pushed

                //otherwise...push it?
            }

            //we know we want to push, so... are we in range?

            //first very raw check, good for weeding out
            if ( !otherEntity.WorldLocation.GetHasAnyChanceOfBeingInRange( DelegateHelper_ProtectionPlanning_shieldGeneratorCenter, DelegateHelper_ProtectionPlanning_radius ) )
                return DelReturn.Continue;

            int radius = DelegateHelper_ProtectionPlanning_radius;

            int distance = giantObject.GetDistanceTo_ExpensiveAccurate( otherEntity, RadiusCheck.IgnoreRadii, true );
            int threshold = otherMarkData.Radius + radius;
            if ( distance >= threshold ) //don't push it if we're out of range
                return DelReturn.Continue;

            int repulsionMagnitude = threshold + ExternalConstants.Instance.ShieldRepulsionExtraDistance;
            AngleDegrees repulsionAngle = giantObject.WorldLocation.GetAngleToDegrees( otherEntity.WorldLocation );

            ArcenPoint pointToRepelTo = giantObject.WorldLocation.GetPointAtAngleAndDistance( repulsionAngle, repulsionMagnitude );
            otherEntity.ShouldDoShieldDecollision.Construction = true;
            otherEntity.ShieldDecollision_NextMovePoint.Construction = pointToRepelTo;

            //ArcenDebugging.ArcenDebugLogSingleLine( giantObject.TypeData.DisplayName + " pushed " + otherEntity.TypeData.DisplayName, Verbosity.Chat );

            return DelReturn.Continue;
        }

        private ArcenPoint DelegateHelper_ProtectionPlanning_shieldGeneratorCenter;
        private GameEntity_Squad DelegateHelper_ProtectionPlanning_shieldGenerator;
        private int DelegateHelper_ProtectionPlanning_radius;
        private DelReturn DelegateHelper_ProtectionPlanning_TryToProtectOrDisplace( GameEntity_Squad otherEntity )
        {
            if ( otherEntity == null )
                return DelReturn.Continue;
            
            var otherMarkData = otherEntity.DataForMark;
            if ( otherMarkData == null )
                return DelReturn.Continue;
            
            var otherTypeData = otherEntity.TypeData;
            if ( otherTypeData == null )
                return DelReturn.Continue;

            var shieldGenerator = DelegateHelper_ProtectionPlanning_shieldGenerator;
            if ( shieldGenerator == otherEntity )
                return DelReturn.Continue;
            
            if ( shieldGenerator == null )
                return DelReturn.Continue;

            //first very raw check, good for weeding out
            if ( !otherEntity.WorldLocation.GetHasAnyChanceOfBeingInRange( DelegateHelper_ProtectionPlanning_shieldGeneratorCenter, DelegateHelper_ProtectionPlanning_radius ) )
                return DelReturn.Continue;

            int debugStage = 0;
            try
            {
                debugStage = 200;
                int radius = DelegateHelper_ProtectionPlanning_radius;

                debugStage = 500;
                bool isDisplaceableFinal = isDisplaceable && 
                                           otherTypeData.IsMobile && 
                                           !otherEntity.GetIsCrippled() && 
                                           otherEntity.CurrentStateOfMatter == shieldGenerator.CurrentStateOfMatter && 
                                           !otherTypeData.CanPassThroughEnemyForcefields;
                
                if ( !isProtectable && !isDisplaceableFinal )
                    return DelReturn.Continue;
                
                debugStage = 1000;
                int distance = shieldGenerator.GetDistanceTo_ExpensiveAccurate( otherEntity, RadiusCheck.IgnoreRadii, true );
                
                debugStage = 1010;
                int threshold = otherMarkData.Radius + radius;
                
                //ArcenDebugging.ArcenDebugLogSingleLine( shieldGenerator.TypeData.InternalName + " dist from " + otherEntity.TypeData.InternalName + ": " + distance + " compared to " + threshold, Verbosity.DoNotShow );
                debugStage = 2000;
                if ( distance >= threshold )
                    return DelReturn.Continue;
                
                debugStage = 3000;
                if ( isProtectable )
                {
                    if ( otherTypeData.ImmuneToProtectionByForcefields )
                        return DelReturn.Continue;
                    
                    if ( otherEntity.CurrentStateOfMatter != shieldGenerator.CurrentStateOfMatter )
                        return DelReturn.Continue;

                    //List<LazyLoadSquadWrapper> shields = null;
                    //if ( shieldGenerator.TypeData.MyForcefieldHasNoPenaltyForEnemiesFiringOutOfIt )
                    //    shields = otherEntity.ProtectingShields_NoDamageReduction.GetConstructionList();
                    //else
                    
                    var reducesDamage = !shieldGenerator.TypeData.MyForcefieldHasNoPenaltyForEnemiesFiringOutOfIt;
                    if (reducesDamage)
                        otherEntity.IsUnderDamageReducingShield.Construction = true;
                    
                    var shields = otherEntity.ProtectingShields.GetConstructionList();
                    if ( shields.Count < GameEntity_Squad.MaxShieldsToTrack )
                    {
                        debugStage = 4000;
                        int idx = 0;
                        for ( ; idx < shields.Count; idx++ )
                        {
                            debugStage = 4100;
                            var otherShield = shields[idx].GetSquad();
                            if ( otherShield == null )
                                continue;
                            
                            debugStage = 4200;
                            int dist = otherShield.GetDistanceTo_ExpensiveAccurate( otherEntity, RadiusCheck.IgnoreRadii, false );
                            if ( dist <= distance )
                                break;
                        }
                        
                        debugStage = 4600;
                        try
                        {
                            if (shields.Count <= idx)
                                shields.Add( LazyLoadSquadWrapper.Create( shieldGenerator ) );
                            else
                                shields.Insert( idx, LazyLoadSquadWrapper.Create( shieldGenerator ) );
                        }
                        catch { } //cross threading error if it happens
                    }
                }
                
                debugStage = 6000;
                if ( isDisplaceableFinal )
                {
                    debugStage = 6100;
                    int repulsionMagnitude = threshold + ExternalConstants.Instance.ShieldRepulsionExtraDistance;
                    debugStage = 6200;
                    AngleDegrees repulsionAngle = shieldGenerator.WorldLocation.GetAngleToDegrees( otherEntity.WorldLocation );
                    debugStage = 6300;
                    if ( otherTypeData.PushesEnemyShields && shieldGenerator.TypeData.IsMobile ) //entity is what is being pushed, so it must be mobile
                    {
                        debugStage = 6400;
                        //Normally shields push ships. But the Ark is Chuck Norris. So the Ark pushes shields.
                        repulsionAngle = repulsionAngle.GetOpposite();
                        debugStage = 6500;
                        ArcenPoint pointToRepelTo = otherEntity.WorldLocation.GetPointAtAngleAndDistance( repulsionAngle, repulsionMagnitude );
                        debugStage = 6600;
                        shieldGenerator.ShouldDoShieldDecollision.Construction = true;
                        shieldGenerator.ShieldDecollision_NextMovePoint.Construction = pointToRepelTo;
                        //if this is bumping a player shield generator, remember where it was supposed to be
                        //so it can go back afterwards.
                        RememberOriginPoint_IfNeeded(shieldGenerator);
                    }
                    else
                    {
                        if ( otherTypeData.IsMobile ) //at this point otherEntity is getting pushed, so IT must be mobile
                        {
                            //TODO: for wedging (using multiple forcefields in the middle of a planet to control attackers )
                            //try modifying how we bounce to make sure units are going around in both directions (using even/odd primary keys)
                            debugStage = 7000;
                            ArcenPoint pointToRepelTo = shieldGenerator.WorldLocation.GetPointAtAngleAndDistance( repulsionAngle, repulsionMagnitude );
                            debugStage = 7100;
                            otherEntity.ShouldDoShieldDecollision.Construction = true;
                            otherEntity.ShieldDecollision_NextMovePoint.Construction = pointToRepelTo;
                            RememberOriginPoint_IfNeeded(otherEntity);
                        }
                    }
                }
            }
            catch ( Exception e )
            {
                ArcenDebugging.ArcenDebugLog( "DelegateHelper_ProtectionPlanning_TryToProtectOrDisplace error at debug stage " + debugStage + ":\n" + e, Verbosity.ShowAsError );
            }
            return DelReturn.Continue;
        }

        /// <summary>Remember the point a forcefield should return to if pushed.</summary>
        ///
        /// The logic here for what to record coresponds to that in
        /// <c>HostOnlyJournalsAndAutosaveAndSimilarHandler.HandleForcefieldsMovingBackOnHost_PerFaction</c>
        static void RememberOriginPoint_IfNeeded( GameEntity_Squad squad ) {
            if ( squad.TypeData.MovesBackAfterBeingNorrised &&
                    squad.GetFactionTypeSafe() == FactionType.Player &&
                    squad.GetMatches_SemiSlow( EntityRollupType.ProjectsForcefield ) &&
                    squad.GuardOrPatrolOffsetPoints.Count == 0 ) //if we are being bumped repeatedly, don't update again
            {
                ArcenPoint returnPoint = squad.WorldLocation; //return to our current location
                if ( squad.HasQueuedOrders() )
                {
                    //if we were already moving somewhere, use that location as the place to return to
                    EntityOrder order = squad.Orders.GetQueuedOrderAtIndex_OrNull( 0 );
                    if ( order.TypeData != null && order.TypeData.Type == EntityOrderType.Move_Normal )
                        returnPoint = order.RelatedPoint;
                }
                //Handle the norris effect movement
                squad.GuardOrPatrolOffsetPoints.Add(returnPoint);
            }
        }

        private DelReturn DelegateHelper_ProtectionPlanning_CheckDisplacesOtherImmobileObjectsAsIfWerePlanetTier( GameEntity_Squad giantObject )
        {
            if ( giantObject == null )
                return DelReturn.Continue;
            if ( !giantObject.TypeData.DisplacesOtherImmobileObjectsAsIfWerePlanetTier )
                return DelReturn.Continue;

            DelegateHelper_ProtectionPlanning_shieldGeneratorCenter = giantObject.WorldLocation;
            DelegateHelper_ProtectionPlanning_radius = giantObject.DataForMark.Radius + giantObject.TypeData.AddedDisplacementBuffer;
            DelegateHelper_ProtectionPlanning_shieldGenerator = giantObject;

            PlanetFaction fac;
            for ( int i = 0; i < giantObject.Planet.Factions.Count; i++ )
            {
                fac = giantObject.Planet.Factions[i];
                if ( fac == null )
                    continue;

                foreach ( GameEntity_Squad _pp_e in fac.Entities.Squads() )
                    DelegateHelper_ProtectionPlanning_TryToDisplacesOtherImmobileObjectsAsIfWerePlanetTier_Squads( _pp_e );
                foreach ( GameEntity_Other _pp_o in fac.Entities.Others() )
                    DelegateHelper_ProtectionPlanning_TryToDisplacesOtherImmobileObjectsAsIfWerePlanetTier_Others( _pp_o );
            }
            return DelReturn.Continue;
        }

        private DelReturn DelegateHelper_ProtectionPlanning_TryToDisplacesOtherImmobileObjectsAsIfWerePlanetTier_Squads( GameEntity_Squad otherEntity )
        {
            return DelegateHelper_ProtectionPlanning_TryToDisplacesOtherImmobileObjectsAsIfWerePlanetTier_Inner( otherEntity, true );
        }

        private DelReturn DelegateHelper_ProtectionPlanning_TryToDisplacesOtherImmobileObjectsAsIfWerePlanetTier_Others( GameEntity_Other otherEntity )
        {
            return DelegateHelper_ProtectionPlanning_TryToDisplacesOtherImmobileObjectsAsIfWerePlanetTier_Inner( otherEntity, false );
        }

        private DelReturn DelegateHelper_ProtectionPlanning_TryToDisplacesOtherImmobileObjectsAsIfWerePlanetTier_Inner( GameEntity_Base otherEntity, bool CareAboutStats )
        {
            if ( otherEntity == null )
                return DelReturn.Continue;
            GameEntityTypeData otherTypeData = otherEntity.TypeData;
            if ( otherTypeData == null )
                return DelReturn.Continue;

            GameEntity_Squad giantObject = DelegateHelper_ProtectionPlanning_shieldGenerator;
            if ( giantObject == otherEntity )
                return DelReturn.Continue;
            if ( giantObject == null )
                return DelReturn.Continue;

            if ( CareAboutStats )
            {
                if ( otherTypeData.DisplacesOtherShipsPlanetTier )
                    return DelReturn.Continue; //nothing pushes planet-tier, and planet-tier don't push each other

                if ( otherTypeData.HasSpecialImmunityToPlanetTierDisplacement )
                    return DelReturn.Continue; //planet-tier don't push those with a special immunity to it

                if ( otherTypeData.IsMobile )
                    return DelReturn.Continue; //if it isn't immobile, then it can't be pushed

                if ( (otherTypeData.IsMobileOrbiter || otherTypeData.IsPlanetaryOrbiter) && string.IsNullOrEmpty(otherTypeData.OrbitsParentUnlessParentSystemHasTarget) )
                    return DelReturn.Continue; //if it is an orbiter, then it can't be pushed
            }

            //we know we want to push, so... are we in range?

            //first very raw check, good for weeding out
            if ( !otherEntity.WorldLocation.GetHasAnyChanceOfBeingInRange( DelegateHelper_ProtectionPlanning_shieldGeneratorCenter, DelegateHelper_ProtectionPlanning_radius ) )
                return DelReturn.Continue;

            int radius = DelegateHelper_ProtectionPlanning_radius;
            int otherRadius = otherEntity.GetRadius();

            int distance = giantObject.GetDistanceToAnyType_ExpensiveAccurate( otherEntity, RadiusCheck.IgnoreRadii, true );
            int threshold = otherRadius + radius;
            if ( distance >= threshold ) //don't push it if we're out of range
                return DelReturn.Continue;

            int repulsionMagnitude = threshold + ExternalConstants.Instance.ShieldRepulsionExtraDistance;
            AngleDegrees repulsionAngle = giantObject.WorldLocation.GetAngleToDegrees( otherEntity.WorldLocation );

            ArcenPoint pointToRepelTo = giantObject.WorldLocation.GetPointAtAngleAndDistance( repulsionAngle, repulsionMagnitude );
            //move immobile items instantly:
            otherEntity.SetWorldLocation( pointToRepelTo );

            //ArcenDebugging.ArcenDebugLogSingleLine( giantObject.TypeData.DisplayName + " pushed " + otherEntity.TypeData.DisplayName, Verbosity.Chat );

            return DelReturn.Continue;
        }
    }
}
