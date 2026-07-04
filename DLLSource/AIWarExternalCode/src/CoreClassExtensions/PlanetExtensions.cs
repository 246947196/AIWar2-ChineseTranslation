using System;

using System.Text;
using Arcen.AIW2.Core;
using Arcen.Universal;

namespace Arcen.AIW2.External
{
    public static class PlanetExtensions
    {       
        public static GameEntity_Squad Mapgen_SeedAIEntity( this Planet planet, ArcenHostOnlySimContext Context, GameEntityTypeData entityData, PlanetSeedingZone SeedingZone )
        {
            return planet.Mapgen_SeedEntity( Context, planet.GetControllingFaction(), entityData, SeedingZone );
        }

        public static GameEntity_Squad Mapgen_SeedEntity( this Planet planet, ArcenHostOnlySimContext Context, Faction Faction, string entityTag, PlanetSeedingZone SeedingZone )
        {
            return Mapgen_SeedEntity( planet, Context, Faction, GameEntityTypeDataTable.Instance.GetRandomRowWithTag(Context, entityTag), SeedingZone );
        }
        
        public static GameEntity_Squad Mapgen_SeedEntity( this Planet planet, ArcenHostOnlySimContext Context, Faction Faction, GameEntityTypeData entityData, PlanetSeedingZone SeedingZone )
        {
            if ( Faction == null )
                throw new Exception( "Null faction passed to Mapgen_SeedEntity!" );
            if ( entityData == null )
                throw new Exception( "Null GameEntityTypeData passed to Mapgen_SeedEntity!" );
            if ( planet == null )
                throw new Exception( "Null planet passed to Mapgen_SeedEntity!" );

            FInt minRadius;
            FInt maxRadius;
            switch ( SeedingZone )
            {
                case PlanetSeedingZone.InnerSystem:
                    minRadius = FInt.FromParts( 0, 150 );
                    maxRadius = FInt.FromParts( 0, 300 );
                    break;
                case PlanetSeedingZone.OuterSystem:
                    minRadius = FInt.FromParts( 0, 750 );
                    maxRadius = FInt.FromParts( 0, 900 );
                    break;
                case PlanetSeedingZone.GravityWell:
                    minRadius = FInt.FromParts( 0, 950 );
                    maxRadius = FInt.FromParts( 0, 990 );
                    break;
                case PlanetSeedingZone.MostAnywhere:
                    minRadius = FInt.FromParts( 0, 50 );
                    maxRadius = FInt.FromParts( 0, 900 );
                    break;
                default:
                    throw new Exception( "Mapgen_SeedEntity Error: unsupported SeedingZone " + SeedingZone );
            }
            ArcenPoint point = planet.GetSafePlacementPointAroundPlanetCenter( Context, entityData, minRadius, maxRadius );
            if ( point == ArcenPoint.ZeroZeroPoint )
                return null;
            PlanetFaction pFaction = planet.GetPlanetFactionForFaction( Faction );
            if ( pFaction == null )
                throw new Exception( "Null pFaction found in Mapgen_SeedEntity for planet " + planet.Name + " and faction " + Faction.GetDisplayName() + "!" );

            GameEntity_Squad result = GameEntity_Squad.CreateNew_ReturnNullIfMPClient( pFaction, entityData,
                Faction.Type == FactionType.AI && planet.MarkLevelForAIOnly != null ? planet.MarkLevelForAIOnly.Ordinal : entityData.MarkFor( Faction.CurrentGeneralMarkLevel ),
                pFaction.FleetUsedAtPlanet, 0, point, Context, "MapgenSeed" );
            return result;
        }

        public static ArcenPoint GetSafePlacementPoint_AroundZone( this Planet planet, ArcenHostOnlySimContext Context, GameEntityTypeData entityData, PlanetSeedingZone SeedingZone )
        {
            FInt minRadius;
            FInt maxRadius;
            switch ( SeedingZone )
            {
                case PlanetSeedingZone.InnerSystem:
                    minRadius = FInt.FromParts( 0, 150 );
                    maxRadius = FInt.FromParts( 0, 300 );
                    break;
                case PlanetSeedingZone.OuterSystem:
                    minRadius = FInt.FromParts( 0, 750 );
                    maxRadius = FInt.FromParts( 0, 900 );
                    break;
                case PlanetSeedingZone.GravityWell:
                    minRadius = FInt.FromParts( 0, 950 );
                    maxRadius = FInt.FromParts( 0, 990 );
                    break;
                case PlanetSeedingZone.MostAnywhere:
                    minRadius = FInt.FromParts( 0, 50 );
                    maxRadius = FInt.FromParts( 0, 900 );
                    break;
                default:
                    throw new Exception( "Mapgen_SeedEntity Error: unsupported SeedingZone " + SeedingZone );
            }
            return planet.GetSafePlacementPointAroundPlanetCenter( Context, entityData, minRadius, maxRadius );
        }

        public static ArcenPoint GetSafePlacementPointAroundPlanetCenter( this Planet planet, ArcenHostOnlySimContext Context, GameEntityTypeData EntityTypeToPlace,
            FInt MinDistancePreferred_Multiplier, FInt MaxDistancePreferred_Multiplier )
        {
            int minDistancePreferred = (planet.GravWellSize.DistanceScale_GravwellRadius * MinDistancePreferred_Multiplier).IntValue;
            int maxDistancePreferred = (planet.GravWellSize.DistanceScale_GravwellRadius * MaxDistancePreferred_Multiplier).IntValue;

            int absoluteMax = (planet.GravWellSize.DistanceScale_GravwellRadius * FInt.FromParts( 0, 950 )).IntValue;
            return GetSafePlacementPoint_Internal( planet, Context, EntityTypeToPlace, Engine_AIW2.Instance.CombatCenter, minDistancePreferred, maxDistancePreferred, 0, absoluteMax,
                (minDistancePreferred >> 2 ), //divide by 4
                (maxDistancePreferred >> 1 ), //divide by 2
                5 );
        }

        public static ArcenPoint GetSafePlacementPoint_SpecificPoint( this Planet planet, ArcenHostOnlySimContext Context, GameEntityTypeData EntityTypeToPlace, ArcenPoint PlaceAround,
            int MinDistancePreferred, int MaxDistancePreferred )
        {
            int absoluteMax = (planet.GravWellSize.DistanceScale_GravwellRadius * FInt.FromParts( 0, 950 )).IntValue;
            return GetSafePlacementPoint_Internal( planet, Context, EntityTypeToPlace, PlaceAround, MinDistancePreferred, MaxDistancePreferred, 0, absoluteMax,
                (MinDistancePreferred >> 2), //divide by 4
                (MaxDistancePreferred >> 1), //divide by 2
                5 );
        }

        public static ArcenPoint GetSafePlacementPoint_AroundDesiredPointVicinity( this Planet planet, ArcenHostOnlySimContext Context, GameEntityTypeData EntityTypeToPlace, ArcenPoint PlaceAround,
            int MinDistanceDirect, int MaxDistanceDirect )
        {
            int absoluteMax = (planet.GravWellSize.DistanceScale_GravwellRadius * FInt.FromParts( 0, 950 )).IntValue;
            int shrinkOrGrowBy = (planet.GravWellSize.DistanceScale_GravwellRadius * FInt.FromParts( 0, 20 )).IntValue;
            return GetSafePlacementPoint_Internal( planet, Context, EntityTypeToPlace, PlaceAround, MinDistanceDirect, MaxDistanceDirect, 0, absoluteMax,
                (MinDistanceDirect >> 2), //divide by 4
                (MaxDistanceDirect >> 1), //divide by 2
                5 );
        }

        public static ArcenPoint GetSafePlacementPoint_AroundDesiredPointVicinity( this Planet planet, ArcenHostOnlySimContext Context, GameEntityTypeData EntityTypeToPlace, ArcenPoint PlaceAround,
            FInt MinOldStyleDistanceFactor_Multiplier, FInt MaxOldStyleDistanceFactor_Multiplier )
        {
            int minDistancePreferred = (ExternalConstants.Instance.MultiplierBase_GravwellRadius_ForUnitsSeedingNearAnother * MinOldStyleDistanceFactor_Multiplier).IntValue;
            int maxDistancePreferred = (ExternalConstants.Instance.MultiplierBase_GravwellRadius_ForUnitsSeedingNearAnother * MaxOldStyleDistanceFactor_Multiplier).IntValue;

            int absoluteMax = (planet.GravWellSize.DistanceScale_GravwellRadius * FInt.FromParts( 0, 950 )).IntValue;
            return GetSafePlacementPoint_Internal( planet, Context, EntityTypeToPlace, PlaceAround, minDistancePreferred, maxDistancePreferred, 0, absoluteMax,
                (minDistancePreferred >> 2), //divide by 4
                (maxDistancePreferred >> 1), //divide by 2
                5 );
        }

        public static ArcenPoint GetSafePlacementPoint_AroundEntity( this Planet planet, ArcenHostOnlySimContext Context, GameEntityTypeData EntityTypeToPlace, GameEntity_Base PlaceAround,
            FInt MinOldStyleDistanceFactor_Multiplier, FInt MaxOldStyleDistanceFactor_Multiplier )
        {
            ArcenPoint aroundPoint = PlaceAround == null ? Engine_AIW2.Instance.CombatCenter : PlaceAround.WorldLocation;

            int minDistancePreferred = ( ExternalConstants.Instance.MultiplierBase_GravwellRadius_ForUnitsSeedingNearAnother * MinOldStyleDistanceFactor_Multiplier).IntValue;
            int maxDistancePreferred = (ExternalConstants.Instance.MultiplierBase_GravwellRadius_ForUnitsSeedingNearAnother * MaxOldStyleDistanceFactor_Multiplier).IntValue;

            int absoluteMax = (planet.GravWellSize.DistanceScale_GravwellRadius * FInt.FromParts( 0, 950 )).IntValue;
            return GetSafePlacementPoint_Internal( planet, Context, EntityTypeToPlace, aroundPoint, minDistancePreferred, maxDistancePreferred, 0, absoluteMax,
                (minDistancePreferred >> 2), //divide by 4
                (maxDistancePreferred >> 1), //divide by 2
                5 );
        }

        private static ArcenPoint GetSafePlacementPoint_Internal( this Planet planet, ArcenHostOnlySimContext Context, GameEntityTypeData EntityTypeToPlace, ArcenPoint BasePoint,
            int MinDistancePreferred, int MaxDistancePreferred, int AbsoluteMinDistance, int AbsoluteMaxDistance,
            int AmountToDecreaseMinDistancePerOuterLoop, int AmountToIncreaseMaxDistancePerOuterLoop, int MaxExtraOuterLoops )
        {
            if ( EntityTypeToPlace == null )
            {
               ArcenDebugging.ArcenDebugLog( "Cannot GetSafePlacementPoint_Internal, because entity type passed in was null!", Verbosity.ShowAsError );
               return Engine_AIW2.Instance.CombatCenter;
            }

            int outerLoopsRemaining = MaxExtraOuterLoops;
            int currentMinDistance = MinDistancePreferred;
            int currentMaxDistance = MaxDistancePreferred;
            ArcenPoint finalResult = Engine_AIW2.Instance.CombatCenter;
            do
            {
                if ( Helper_GetSafePlacementPoint_Inner( planet, Context, EntityTypeToPlace, BasePoint, currentMinDistance, currentMaxDistance, out finalResult ) )
                    return finalResult; //yay, we found a spot!

                //if ( MapgenLogger.IsActive )
                //    MapgenLogger.Log( "GetSafePointOuter: currentMinDistance: " + currentMinDistance + " AbsoluteMinDistance: " + 
                //        AbsoluteMinDistance + " currentMaxDistance: " + AbsoluteMaxDistance + 
                //        " AmountToDecreaseMinDistancePerOuterLoop: " + AmountToDecreaseMinDistancePerOuterLoop +
                //        " AmountToIncreaseMaxDistancePerOuterLoop: " + AmountToIncreaseMaxDistancePerOuterLoop );

                if ( currentMinDistance <= AbsoluteMinDistance && currentMaxDistance >= AbsoluteMaxDistance )
                    return Engine_AIW2.Instance.CombatCenter; //dang, we found nothing and had already expanded as wide as was allowed!
                    
                //blah, no spot, so start widening our search to something less-restrictive
                outerLoopsRemaining--;
                currentMinDistance += AmountToDecreaseMinDistancePerOuterLoop;
                currentMaxDistance += AmountToIncreaseMaxDistancePerOuterLoop;
                if ( currentMinDistance > AbsoluteMinDistance )
                    currentMinDistance = AbsoluteMinDistance;
                if ( currentMaxDistance > AbsoluteMaxDistance )
                    currentMaxDistance = AbsoluteMaxDistance;
            }
            while ( outerLoopsRemaining > 0 );
            return finalResult;
        }

        private static bool Helper_GetSafePlacementPoint_Inner( Planet planet, ArcenHostOnlySimContext Context, GameEntityTypeData EntityTypeToPlace, ArcenPoint BasePoint,
            int MinDistance, int MaxDistance, out ArcenPoint FinalResult )
        {
            ArcenPoint result = BasePoint.GetRandomPointWithinDistance( Context.RandomToUse, MinDistance, MaxDistance );

            //if ( MapgenLogger.IsActive )
            //    MapgenLogger.Log( "GetSafePointInn: BasePoint: " + BasePoint + " MinDistance: " +
            //        MinDistance + " MaxDistance: " + MaxDistance +
            //        " result: " + result +
            //        " IsSafe: " + planet.GetIsPlacementPointSafe( EntityTypeToPlace, result, true ) +
            //        " EntityTypeToPlace: " + (EntityTypeToPlace == null ? "null" : EntityTypeToPlace.InternalName ) );

            if ( EntityTypeToPlace == null )
            {
                ArcenDebugging.ArcenDebugLog( "Cannot tell if placement point is safe, because entity type passed in was null!", Verbosity.ShowAsError );
                FinalResult = result;
                return true;
            }

            int rechecks = 100;
            while ( !planet.GetIsPlacementPointSafe( EntityTypeToPlace, result, true ) )
            {
                rechecks--;
                if ( rechecks <= 0 )
                {
                    FinalResult = Engine_AIW2.Instance.CombatCenter;
                    return false;
                }
                result = BasePoint.GetRandomPointWithinDistance( Context.RandomToUse, MinDistance, MaxDistance );
            }
            FinalResult = result;
            return true;
        }

        public static bool GetIsPlacementPointSafe( this Planet planet, GameEntityTypeData EntityTypeToPlace, ArcenPoint Point, bool OutrightIgnoreMobileUnits )
        {
            if ( planet == null )
                return false;
            if ( planet.GetIsPointOutsideGravWell_SlowButCorrect( Point ) )
                return false;
            if ( EntityTypeToPlace == null )
                return false;
            GameEntityTypeData.MarkLevelStats markStats = EntityTypeToPlace.ForMark[Balance_MarkLevelTable.Instance.MaxOrdinal];
            if ( markStats == null )
                return false;

            int radiusOfThingBeingPlaced = markStats.Radius;
            
            // this might be appropriate?
            // radiusOfThingBeingPlaced += EntityTypeToPlace.AddedDisplacementBuffer;
            
            int collisionPriorityOfThingBeingPlaced = EntityTypeToPlace.CollisionPriority;
            
            return PlanetExtensions.GetIsPlacementPointSafe( planet, Point, collisionPriorityOfThingBeingPlaced, radiusOfThingBeingPlaced, OutrightIgnoreMobileUnits);
        }

        public static bool GetIsPlacementPointSafe( this Planet planet, ArcenPoint Point, int CollisionPriority, int Radius, bool OutrightIgnoreMobileUnits )
        {
            if ( planet == null )
                return false;
            if ( planet.GetIsPointOutsideGravWell_SlowButCorrect( Point ) )
                return false;
            
            bool hadHit = false;
            foreach ( GameEntity_Squad entity in planet.Squads() )
            {
                if ( entity == null )
                    continue;
                StateOfMatterTypeData stateOfMatter = entity.CurrentStateOfMatter;
                if ( stateOfMatter != null )
                {
                    //we are assuming placement in the normal state of matter
                    if ( stateOfMatter.HasSeparateViewMode )
                        continue;
                }

                if ( !entity.GetIsWithinRangeOf( Point, Radius ) ) //use the full radius of ourselves, or things will overlap!
                    continue;

                GameEntityTypeData typeData = entity.TypeData;
                if ( typeData == null )
                    continue;

                GameEntityTypeData.MarkLevelStats otherMarkStats = entity.DataForMark;
                if ( otherMarkStats == null )
                {
                    entity.DataForMark = typeData.ForMark[entity.CurrentMarkLevel];
                    otherMarkStats = entity.DataForMark;
                }
                if ( otherMarkStats == null )
                    continue;

                //for "mobile units", we mean mobile but not fleet leaders or forcefields.
                if ( typeData.IsMobile && !typeData.IsFleetLeader && otherMarkStats.ShieldRadius <= 0 )
                {
                    //if we don't care at all about mobile units, then ignore them.
                    if ( OutrightIgnoreMobileUnits )
                        continue;
                    //if the entity we're hitting is mobile and a lower priority than us, then ignore them!
                    if ( typeData.CollisionPriority < CollisionPriority )
                        continue;
                }
                hadHit = true;
                break;
            }
            if ( hadHit )
                return !hadHit;
            foreach ( GameEntity_Other entity in planet.Others() )
            {
                if ( entity == null )
                    continue;
                if ( !entity.GetIsWithinRangeOf( Point, Radius ) )
                    continue;
                hadHit = true;
                break;
            }
            return !hadHit;
        }
    }
}
