using Arcen.AIW2.Core;
using Arcen.Universal;
using System;

using System.Linq;
using System.Text;

namespace Arcen.AIW2.External
{
    public abstract class BaseAIDefensePlacer : IAIDefensePlacerImplementation
    {
        public static readonly ReferenceTracker RefTracker = new ReferenceTracker( "BaseAIDefensePlacers" );
        public BaseAIDefensePlacer()
        {
            RefTracker.IncrementObjectCount();
        }

        protected bool OnlyOneShieldPerReinforceablePerPass = true;

        public abstract void DoInitialDefenseSeeding( ArcenHostOnlySimContext Context, Planet ThisPlanet, Tutorial.TutorialPlanetOwnershipAndSpawning TutorialPlanetOrNull, Faction owningFaction = null );

        public abstract void StartReconquestDefenseSeeding( ArcenHostOnlySimContext Context, Planet ThisPlanet, Faction owningFaction );

        public abstract void ProcessAfterGameLoad( ArcenHostOnlySimContext Context, Planet ThisPlanet, Faction owningFaction = null );

        public virtual int Reinforce( ArcenHostOnlySimContext Context, Planet planet, Faction faction, int budget, ReinforcementType reinforcementType, bool IsForInitialSeeding, 
            bool ComplainIfNotOnPurposeInabilityToSeed, ref int AddedStrength )
        {
            bool debug = GameSettings_AIW2.Current.GetBool( ArcenBoolSetting_AIW2.EnableReinforcementLogging );
            //AITypeData aiType = faction.GetSentinelsExternal().AIType;

            int AICostPurchaseCap = AIUtilityMethods.GetAICostPurchaseCapForBudgetType( planet, faction, reinforcementType, IsForInitialSeeding, debug ).IntValue;
            int purchaseCostPresent = AIUtilityMethods.GetAIToPurchaseCostPresentForBudgetType( planet, faction, reinforcementType );
            if(debug)
                ArcenDebugging.ArcenDebugLogSingleLine("Considering reinforcing " + planet.Name + " with " + reinforcementType + " purchaseCostPresent " + purchaseCostPresent + " AICostPurchaseCap " + AICostPurchaseCap + " budget " + budget , Verbosity.DoNotShow );
            if ( purchaseCostPresent >= AICostPurchaseCap )
                return 0;

            List<SafeSquadWrapper> upperEntitiesWeCanReinforce = GameEntity_Squad.GetTemporarySquadList( "BaseAIDefensePlacer-Reinforce-upperEntitiesWeCanReinforce", 10f );
            if ( upperEntitiesWeCanReinforce == null ) //blocked for teardown/shutdown; bail
                return 0;

            Int16 reinforcementLocationCount = 0;
            foreach ( GameEntity_Squad guardpost in planet.GetPlanetFactionForFaction( faction ).Entities.Squads( EntityRollupType.ReinforcementLocations ) )
            {
                reinforcementLocationCount++;
                upperEntitiesWeCanReinforce.Add( guardpost );
                guardpost.Working_ReinforcementsOnly_ContentsCostForAIToPurchase = guardpost.GetCostForAIToPurchaseOfContentsIfAny();
            }

            if ( reinforcementLocationCount > planet.MaxReinforcementPlacesEverSeenHere )
                planet.MaxReinforcementPlacesEverSeenHere = reinforcementLocationCount;

            if (debug)
                ArcenDebugging.ArcenDebugLogSingleLine(planet.Name + " has " + upperEntitiesWeCanReinforce.Count + " reinforcement locations", Verbosity.DoNotShow );
            if( upperEntitiesWeCanReinforce.Count == 0)
            {
                GameEntity_Squad.ReleaseTemporarySquadList( upperEntitiesWeCanReinforce );
                return 0;
            }

            PlanetFaction pFaction = planet.GetPlanetFactionForFaction( faction );
            AIDefensePlacer_Default.DefinePlanetFactionDefenseTypesIfNeeded( Context, planet, faction, false );

            int result = 0;
            AIShipGroup aiShipGroup = null;
            
            switch ( reinforcementType )
            {
                case ReinforcementType.Turret:
                    aiShipGroup = pFaction.ShipGroup_Turrets;
                    break;
                case ReinforcementType.NonTurretDefense:
                    aiShipGroup = pFaction.ShipGroup_NonTurretDefenses;
                    break;
                case ReinforcementType.Strikecraft:
                    aiShipGroup = pFaction.ShipGroup_Strikecraft;
                    break;
                case ReinforcementType.Guardian:
                    aiShipGroup = pFaction.ShipGroup_BasicGuardians;
                    break;
            }
            if (reinforcementType == ReinforcementType.Guardian && aiShipGroup != null )
            {
                DrawBag<GameEntityTypeData> guardianTypes = aiShipGroup.DrawBag;
                GameEntity_Squad.ReleaseTemporarySquadList( upperEntitiesWeCanReinforce );
                return this.Helper_SeedGuardians(Context, planet, null, faction, guardianTypes,
                    pFaction.ShipGroup_DireGuardians == null ? null : pFaction.ShipGroup_DireGuardians.DrawBag, budget, FInt.FromParts(0, 500), FInt.FromParts(0, 750), 
                    EntityBehaviorType.Guard_Guardian_Patrolling, true, AICostPurchaseCap, ref purchaseCostPresent, ref AddedStrength );
            }
            if(aiShipGroup == null)
            {
                ArcenDebugging.ArcenDebugLogSingleLine("Reinforce: Found null ShipGroup for " + reinforcementType + " for " + faction.Type + " faction " + faction.FactionIndex + " planet " + planet.Name , Verbosity.DoNotShow );
                GameEntity_Squad.ReleaseTemporarySquadList( upperEntitiesWeCanReinforce );
                return 0;
            }

            result += this.Inner_ReinforceWithFleetShipsOrTurrets( Context, planet, faction, ref budget, reinforcementType, AICostPurchaseCap, ref purchaseCostPresent, 
                aiShipGroup, upperEntitiesWeCanReinforce, ComplainIfNotOnPurposeInabilityToSeed, ref AddedStrength, IsForInitialSeeding );
            if (debug)
                ArcenDebugging.ArcenDebugLogSingleLine("Reinforced " + planet.Name + " type " + reinforcementType + " with " + result + " remaining budget " + budget, Verbosity.DoNotShow );

            GameEntity_Squad.ReleaseTemporarySquadList( upperEntitiesWeCanReinforce );
            return result;
        }

        /// <summary>
        /// returns amount spent on guardians
        /// </summary>
        protected int Helper_SeedGuardians( ArcenHostOnlySimContext Context, Planet ThisPlanet, GameEntity_Squad entityToGuardOrNull, Faction faction,
            DrawBag<GameEntityTypeData> regularGuardianTypes, DrawBag<GameEntityTypeData> direGuardianTypes, int budgetForAllTypes,
            FInt minDistanceFactor, FInt maxDistanceFactor, EntityBehaviorType behavior, Boolean isMobilePatrol, int AICostPurchaseCap, ref int costForAIToPurchasePresent, 
            ref int AddedStrength )
        {
            if ( ThisPlanet == null || ThisPlanet.MarkLevelForAIOnly == null )
                return 0; //this randomly seemed to happen to me, but only in the lobby seeding.

            bool seedDireOnly = (ThisPlanet.PopulationType == PlanetPopulationType.AIHomeworld); //don't worry about AIBastionWorld here
            int startingBudgetForDire = seedDireOnly ? budgetForAllTypes : (ThisPlanet.MarkLevelForAIOnly.PlanetDireGuardianPercentage * budgetForAllTypes).GetNearestIntPreferringHigher();

            if ( entityToGuardOrNull == null ) //probably only in lobby seeding does this happen, but let's be careful.
            {
                foreach ( GameEntity_Squad commandStation in ThisPlanet.Squads( EntityRollupType.CommandStation ) )
                {
                        try
                        {
                            if ( commandStation.GetFactionTypeSafe() != FactionType.AI )
                                continue;
                        }
                        catch { }
                        entityToGuardOrNull = commandStation;
                        break;
                    }
            }

            if ( entityToGuardOrNull == null )
            {
                foreach ( GameEntity_Squad guardPost in ThisPlanet.Squads( EntityRollupType.ReinforcementLocations ) )
                {
                    try
                    {
                        if ( guardPost.GetFactionTypeSafe() != FactionType.AI )
                            continue;
                    }
                    catch { }
                    entityToGuardOrNull = guardPost;
                    break;
                }
            }

            if ( entityToGuardOrNull == null )
                return 0; //otherwise they'll just go on the offensive!

            PlanetFaction pFaction = ThisPlanet.GetPlanetFactionForFaction( faction );

            //ArcenDebugging.ArcenDebugLogSingleLine( "Helper_SeedGuardians start on " + ThisPlanet.Name, Verbosity.DoNotShow );

            int debugCode = 0;
            int resultSpent = 0;
            for ( int k = 0; k < 2; k++ )
            {
                try
                {
                    //first seed dire guardians if we can, then seed regular ones if we can
                    DrawBag<GameEntityTypeData> guardianTypesForThisLoop = (k == 0 ? direGuardianTypes : regularGuardianTypes);
                    if ( guardianTypesForThisLoop == null || !guardianTypesForThisLoop.GetHasItems() )
                        continue;

                    int currentBudget = (k == 0 ? startingBudgetForDire : budgetForAllTypes); //whatever is left if not dire
                    //ArcenDebugging.ArcenDebugLogSingleLine( "Helper_SeedGuardians on " + ThisPlanet.Name + " k:" + k + " currentBudget: " + currentBudget +
                    //    " costForAIToPurchasePresent: " + costForAIToPurchasePresent + " AICostPurchaseCap:" + AICostPurchaseCap, Verbosity.DoNotShow );
                    if ( currentBudget <= 0 )
                        continue;

                    int seedAttempts = 100;
                    debugCode = 1000;
                    while ( currentBudget > 0 && costForAIToPurchasePresent < AICostPurchaseCap && seedAttempts > 0 )
                    {
                        debugCode = 2000;
                        seedAttempts--;
                        GameEntityTypeData guardianData = guardianTypesForThisLoop.PickRandomItemAndReplace( Context.RandomToUse );
                        if ( guardianData == null )
                            throw new Exception( "Null guardian data in Helper_SeedGuardians" );
                        GameEntityTypeData.MarkLevelStats guardianData_MarkStats = guardianData.MarkStatsFor( ThisPlanet );
                        //when seeding guardians at a planet, try to make them one level higher than they would normally be
                        guardianData_MarkStats = guardianData.MarkStatsFor( (byte)(guardianData_MarkStats.MarkLevel.Ordinal + 1) );

                        //We actually check for the guardian's cost here and finish the reinforcement if our budget is too low
                        if(currentBudget < guardianData.CostForAIToPurchase )
                        {
                            break;
                        }
                        currentBudget -= guardianData.CostForAIToPurchase;
                        budgetForAllTypes -= guardianData.CostForAIToPurchase;
                        debugCode = 2200;
                        ArcenPoint point = ThisPlanet.GetSafePlacementPointAroundPlanetCenter( Context, guardianData, minDistanceFactor, maxDistanceFactor );
                        if ( point == ArcenPoint.ZeroZeroPoint )
                            continue;
                        debugCode = 2300;
                        resultSpent += guardianData.CostForAIToPurchase;
                        costForAIToPurchasePresent += guardianData.CostForAIToPurchase;

                        debugCode = 2350;
                        GameEntity_Squad newEntity = GameEntity_Squad.CreateNew_ReturnNullIfMPClient( pFaction, guardianData, guardianData_MarkStats.MarkLevel.Ordinal,
                                                                                 pFaction.FleetUsedAtPlanet, 0, point, Context, "DefensePlacer" ); //works great!  mapgen
                        debugCode = 2400;
                        AddedStrength += newEntity.GetStrengthPerSquad();
                        debugCode = 2450;
                        newEntity.Orders.SetBehaviorDirectlyInSim( behavior ); //works great!  mapgen
                        newEntity.GuardedUnit = LazyLoadSquadWrapper.Create( entityToGuardOrNull );
                        switch ( behavior )
                        {
                            case EntityBehaviorType.Guard_Guardian_Anchored:
                                break;
                            case EntityBehaviorType.Guard_Guardian_Patrolling:
                                debugCode = 2500;
                                newEntity.GuardOrPatrolOffsetPoints.Add( newEntity.WorldLocation - entityToGuardOrNull.WorldLocation );
                                debugCode = 2600;
                                if ( isMobilePatrol )
                                {
                                    debugCode = 2700;
                                    AngleDegrees initialAngle = Engine_AIW2.Instance.CombatCenter.GetAngleToDegrees( newEntity.WorldLocation );
                                    //use the more expensive distance method to ensure correctness here
                                    int initialDistance = Engine_AIW2.Instance.CombatCenter.GetDistanceTo( newEntity.WorldLocation, false );
                                    int step = UnityEngine.Mathf.RoundToInt( AngleDegrees.MAX_VALUE / 6 );
                                    for ( int i = step; i < AngleDegrees.MAX_VALUE; i += step )
                                    {
                                        debugCode = 2800;
                                        AngleDegrees angleToThisPoint = initialAngle.Add( AngleDegrees.Create( (float)i ) );
                                        ArcenPoint thisPoint = Engine_AIW2.Instance.CombatCenter.GetPointAtAngleAndDistance( angleToThisPoint, initialDistance );
                                        newEntity.GuardOrPatrolOffsetPoints.Add( thisPoint - entityToGuardOrNull.WorldLocation );
                                    }
                                }
                                break;
                        }
                        debugCode = 2900;
                    }
                }
                catch ( Exception e )
                {
                    ArcenDebugging.ArcenDebugLogSingleLine( "Helper_SeedGuardians debug code " + debugCode + " exception " + e.ToString(), Verbosity.DoNotShow );
                }
            }
            return resultSpent;
        }

        private readonly List<SafeSquadWrapper> WorkingEntitiesWeCanReinforce = List<SafeSquadWrapper>.Create_WillNeverBeGCed( 500, "BaseAIDefensePlacer-WorkingEntitiesWeCanReinforce" );
        protected int Inner_ReinforceWithFleetShipsOrTurrets( ArcenHostOnlySimContext Context, Planet planet, Faction faction, ref int budget, ReinforcementType reinforcementType,
            int AICostPurchaseCap, ref int costForAIToPurchasePresent, AIShipGroup ShipGroup, List<SafeSquadWrapper> BaseEntitiesWeCanReinforce, bool ComplainIfNotOnPurposeInabilityToSeed,
            ref int AddedStrength, bool IsForInitialSeeding )
        {
            bool debug = GameSettings_AIW2.Current.GetBool( ArcenBoolSetting_AIW2.EnableReinforcementLogging );

            if ( BaseEntitiesWeCanReinforce.Count <= 0 )
                return 0;
            //ArcenDebugging.ArcenDebugLogSingleLine( planet.PopulationType + ": mk" + planet.MarkLevelForAIOnly.Ordinal + " reinforcementType: " + reinforcementType + " costForAIToPurchasePresent: " + costForAIToPurchasePresent + " " + AICostPurchaseCap, Verbosity.DoNotShow );

            PlanetFaction pFaction = planet.GetPlanetFactionForFaction( faction );

            DrawBag<GameEntityTypeData> bag = ShipGroup.DrawBag;

            if ( !bag.GetHasItems() )
                return 0;

            AISentinelsCoreData sentinelsExternal = faction.TryGetAISentinelsCoreData()?.SentinelInfo;
            if ( sentinelsExternal == null && (faction.Type == FactionType.SpecialFaction || faction.Type == FactionType.NaturalObject) )
            {
                faction = planet.GetFirstFactionOfType( FactionType.AI ).Faction;
                sentinelsExternal = faction.TryGetAISentinelsCoreData()?.SentinelInfo;
            }

            if ( sentinelsExternal == null )
            {
                Engine_AIW2.Instance.LogErrorDuringCurrentMapGen( "Null sentinelsExternal on faction of type " + faction.Type + " on planet " + planet.Name + " (" + planet.Index + ") in Inner_ReinforceWithFleetShipsOrTurrets. This should not be possible" );
                return 0;
            }
            AITypeData aiType = sentinelsExternal.AIType;
            if ( aiType == null )
            {
                Engine_AIW2.Instance.LogErrorDuringCurrentMapGen( "Null aiType on sentinelsExternal of faction of type " + faction.Type + " on planet " + planet.Name + " (" + planet.Index + ") in Inner_ReinforceWithFleetShipsOrTurrets. This should not be possible" );
                return 0;
            }

            WorkingEntitiesWeCanReinforce.Clear();
            switch ( reinforcementType )
            {
                case ReinforcementType.Turret:
                    for ( int i = 0; i < BaseEntitiesWeCanReinforce.Count; i++ )
                    {
                        GameEntity_Squad reinforcementPoint = BaseEntitiesWeCanReinforce[i].GetSquad();
                        if ( reinforcementPoint == null )
                            continue;
                        if ( reinforcementPoint.PrimaryKeyID % aiType.ModulusForTurretsAtReinforcementLocationLowerIsMoreFrequent == 0 || reinforcementPoint.TypeData.AlwaysIsAITurretSpawnPoint )
                        {
                            //only allow turret defenses to reinforce at every 6 point
                            WorkingEntitiesWeCanReinforce.Add( reinforcementPoint );
                        }
                    }
                    break;
                case ReinforcementType.NonTurretDefense:
                    for ( int i = 0; i < BaseEntitiesWeCanReinforce.Count; i++ )
                    {
                        GameEntity_Squad reinforcementPoint = BaseEntitiesWeCanReinforce[i].GetSquad();
                        if ( reinforcementPoint == null )
                            continue;
                        if ( reinforcementPoint.PrimaryKeyID % aiType.ModulusForNonTurretDefensesAtReinforcementLocationLowerIsMoreFrequent == 0 || reinforcementPoint.TypeData.AlwaysIsAINonTurretDefenseSpawnPoint )
                        {
                            //only allow non-turret defenses to reinforce at every 3 point
                            //more frequent than turrets, but these have a maximum cap and are fairly expensive on the budget.
                            //want them spread out a bit more as well, rather than a pile of Forcefields, Gravity, etc all on one Post.
                            WorkingEntitiesWeCanReinforce.Add( reinforcementPoint );
                        }
                    }
                    break;
                default:
                    WorkingEntitiesWeCanReinforce.AddRange( BaseEntitiesWeCanReinforce );
                    break;
            }
            if ( WorkingEntitiesWeCanReinforce.Count <= 0 )
                return 0;

            WorkingEntitiesWeCanReinforce.Sort( static delegate ( SafeSquadWrapper Left, SafeSquadWrapper Right )
            {
                return Left.Working_ReinforcementsOnly_ContentsCostForAIToPurchase.CompareTo( Right.Working_ReinforcementsOnly_ContentsCostForAIToPurchase );
            } );

            int resultSpentAmount = 0;
            int seedAttempts = 8000;
            int lastIndexOuter = 0;
            while ( budget > 0 && costForAIToPurchasePresent < AICostPurchaseCap && WorkingEntitiesWeCanReinforce.Count > 0 && seedAttempts > 0 )
            {
                seedAttempts--;
                if ( lastIndexOuter >= WorkingEntitiesWeCanReinforce.Count )
                    lastIndexOuter = 0;

                int index = lastIndexOuter;
                switch ( reinforcementType )
                {
                    case ReinforcementType.Turret:
                    case ReinforcementType.NonTurretDefense:
                    //case ReinforcementType.Shield:
                        index = Context.RandomToUse.Next( 0, WorkingEntitiesWeCanReinforce.Count );
                        break;
                }
                GameEntity_Squad entityToReinforce = WorkingEntitiesWeCanReinforce[index].GetSquad();
                if ( entityToReinforce == null )
                    continue;
                GameEntityTypeData typeToBuy = null;
                //int maxOfEachNonTurretDefensesPerGuardPost = 1;
                if(!bag.GetHasItems() )
                        return 0; //we have emptied this bag, presumably be being non-turret defenses and not being able to build more of things

                if(reinforcementType == ReinforcementType.NonTurretDefense)
                {
                    typeToBuy = bag.PickRandomItemAndReplace( Context.RandomToUse );
                    // There's a limit as to how many of something can be on the planet. This probably includes things spawned from Wormhole Sentinels, which seems okay!
                    bool HasEnough = false;
                    int AmountOfThisPresentAlready = 0;
                    foreach ( GameEntity_Squad entity in pFaction.Entities.Squads() )
                    {
                         if ( entity.TypeData == typeToBuy & typeToBuy.MaxCountSeededAsAINonTurretDefense >= 1 )
                         {
                             AmountOfThisPresentAlready = AmountOfThisPresentAlready + 1;
                             if ( AmountOfThisPresentAlready >= entity.TypeData.MaxCountSeededAsAINonTurretDefense)
                             {
                                 HasEnough = true;
                                 break;
                             }
                         }
                     }
                     if( HasEnough )
                     {
                        if (debug)
                             ArcenDebugging.ArcenDebugLogSingleLine("For budget " + reinforcementType + " we already have enough of " + typeToBuy.InternalName + " on " + entityToReinforce.GetPlanetName_Safe(), Verbosity.DoNotShow );
                         continue;
                     }
                }
                else
                    typeToBuy = bag.PickRandomItemAndReplace( Context.RandomToUse );


                GameEntityTypeData.MarkLevelStats typeToBuy_MarkStats = typeToBuy.MarkStatsFor( planet );
                //Note from Chris:  this is how the game has always worked, not breaking when there's not enough budget.
                //                  changing that in 2024 would suddenly adjust balance in unexpected ways.
                //
                //////Check for cost compared to budget and break if not enough
                ////if( budget < typeToBuy.CostForAIToPurchase )
                ////{
                ////    break;
                ////}
                budget -= typeToBuy.CostForAIToPurchase;
                resultSpentAmount += typeToBuy.CostForAIToPurchase;
                costForAIToPurchasePresent += typeToBuy.CostForAIToPurchase;
                switch ( reinforcementType )
                {
                    case ReinforcementType.NonTurretDefense:
                    case ReinforcementType.Turret:
                    case ReinforcementType.Guardian:
                        {
                            ArcenPoint point = planet.GetSafePlacementPoint_AroundEntity( Context, typeToBuy, entityToReinforce, FInt.FromParts( 0, 020 ), FInt.FromParts( 0, 040 ) );
                            if ( point == ArcenPoint.ZeroZeroPoint )
                                continue;
                            if(debug)
                                ArcenDebugging.ArcenDebugLogSingleLine("Building a " + typeToBuy.InternalName + " with budget " + reinforcementType + " near " + entityToReinforce.TypeData.InternalName + " on " + entityToReinforce.GetPlanetName_Safe() + ". It cost " + typeToBuy.CostForAIToPurchase  , Verbosity.DoNotShow );
                            GameEntity_Squad newEntity = GameEntity_Squad.CreateNew_ReturnNullIfMPClient( pFaction, typeToBuy, typeToBuy_MarkStats.MarkLevel.Ordinal, pFaction.FleetUsedAtPlanet, 0, point, Context, "DefensePlacer" );
                            AddedStrength += newEntity.GetStrengthPerSquad();
                            newEntity.Orders.SetBehaviorDirectlyInSim( EntityBehaviorType.Stationary ); //works great!  mapgen
                            lastIndexOuter++;
                        }
                        break;
                    case ReinforcementType.Strikecraft:
                        {
                            if(debug)
                                ArcenDebugging.ArcenDebugLogSingleLine("Building a " + typeToBuy.InternalName + " with budget " + reinforcementType + " near " + entityToReinforce.TypeData.InternalName + " on " + entityToReinforce.GetPlanetName_Safe()  + ". It cost " + typeToBuy.CostForAIToPurchase , Verbosity.DoNotShow );

                            entityToReinforce.AddToAIReinforcementPointContents( typeToBuy, 1, IsForInitialSeeding ? "InitialSeeding" : "RegularReinforcement", string.Empty );
                            entityToReinforce.Working_ReinforcementsOnly_ContentsCostForAIToPurchase += typeToBuy.CostForAIToPurchase;
                            AddedStrength += typeToBuy.GetForMark( entityToReinforce.CurrentMarkLevel ).StrengthPerSquad_CalculatedWithNullFleetMembership;
                            lastIndexOuter++;
                        }
                        break;
                }
            }

            return resultSpentAmount;
        }

        protected static GameEntity_Squad Helper_TryToSeedEntityWithPlanetMark_MapgenOnly_AtSpecificPoint( ArcenHostOnlySimContext Context, Planet ThisPlanet, ArcenPoint point, Faction faction, GameEntityTypeData typeToPlace, 
            int minDistance, int maxDistance)
        {
            if ( ThisPlanet == null || faction == null )
                return null;
            point = ThisPlanet.GetSafePlacementPoint_SpecificPoint( Context, typeToPlace, point, minDistance, maxDistance );
            if ( point == ArcenPoint.ZeroZeroPoint )
                return null;
            PlanetFaction pFaction = ThisPlanet.GetPlanetFactionForFaction( faction );
            if ( pFaction == null )
                return null;
            GameEntity_Squad newEntity = GameEntity_Squad.CreateNew_ReturnNullIfMPClient( pFaction, typeToPlace, typeToPlace.MarkFor( ThisPlanet.MarkLevelForAIOnly.Ordinal ), 
                pFaction.FleetUsedAtPlanet, 0, point, Context, "DefensePlacerHelper" ); //works great because mapgen
            if ( newEntity == null )
                return null;
            newEntity.Orders.SetBehaviorDirectlyInSim( EntityBehaviorType.Stationary );
            return newEntity;
        }
    }

    public class AIDefensePlacer_Default : BaseAIDefensePlacer
    {
        private bool HaveLoadedData;

        private void EnsureLoadedCustomData(ArcenDynamicTableRow Row)
        {
            if ( this.HaveLoadedData )
                return;
            this.HaveLoadedData = true;
        }

        public override void ProcessAfterGameLoad( ArcenHostOnlySimContext Context, Planet ThisPlanet, Faction owningFaction = null )
        {
            if ( owningFaction != null && owningFaction.Type == FactionType.AI )
                DefinePlanetFactionDefenseTypesIfNeeded( Context, ThisPlanet, owningFaction, false );
            else
            {
                bool foundAnAIFormerOwner = false;
                foreach ( GameEntity_Squad entity in ThisPlanet.Squads( EntityRollupType.AICounterattackEnablers ) )
                {
                    if ( entity.GetFactionTypeSafe() == FactionType.AI )
                    {
                        foundAnAIFormerOwner = true;
                        DefinePlanetFactionDefenseTypesIfNeeded( Context, ThisPlanet, entity.GetFactionOrNull_Safe(), false );
                        break;

                    }
                }
                if ( !foundAnAIFormerOwner )
                {
                    //do we need to do anything else here??  Probably not right now...
                }
            }
        }

        public override void StartReconquestDefenseSeeding( ArcenHostOnlySimContext Context, Planet ThisPlanet, Faction faction )
        {
            bool debug = GameSettings_AIW2.Current.GetBool( ArcenBoolSetting_AIW2.EnableReinforcementLogging );

            int debugCode = 0;
            try
            {
                if ( ArcenNetworkAuthority.DesiredStatus == DesiredMultiplayerStatus.Client )
                    return; //let the host take care of that!

                if ( ThisPlanet.GetControllingFactionType() != FactionType.NaturalObject )
                    return;
                if ( faction.Type != FactionType.AI )
                    return;
                debugCode = 10;
                bool isAdjacentToHumanHomeworld = ThisPlanet.OriginalHopsToHumanHomeworld <= 1;
                AISentinelsCoreData sentinelsExternal = faction.TryGetAISentinelsCoreData()?.SentinelInfo;
                if ( sentinelsExternal == null && (faction.Type == FactionType.SpecialFaction || faction.Type == FactionType.NaturalObject) )
                {
                    faction = ThisPlanet.GetFirstFactionOfType( FactionType.AI ).Faction;
                    sentinelsExternal = faction.TryGetAISentinelsCoreData()?.SentinelInfo;
                }

                if ( sentinelsExternal == null )
                {
                    ArcenDebugging.ArcenDebugLog( "Null sentinelsExternal on faction of type " + faction.Type + " on planet " + ThisPlanet.Name + " (" + ThisPlanet.Index + ") in DoInitialOrReconquestDefenseSeeding. This should not be possible", Verbosity.DoNotShow );
                    return;
                }

                //first thing!  Reset the types of ships that can spawn from here to be for the current AI:
                //it might be a new AI type doing the reconquest, or we'll just shuffle things around in general to make it fresh
                DefinePlanetFactionDefenseTypesIfNeeded( Context, ThisPlanet, faction, true );

                debugCode = 12;
                //Seed guard posts
                int numCurrentGuardPosts = 0;
                foreach ( GameEntity_Squad pEntity in ThisPlanet.GetPlanetFactionForFaction(faction).Entities.Squads( EntityRollupType.AICounterattackEnablers ) )
                {
                  numCurrentGuardPosts++;
                }
                int amountOfStrengthAdded = 0;

                if ( numCurrentGuardPosts <= 8 )
                {
                    RefPair<int, int> resultingPosts = ThisPlanet.GuardPostAndCommandPlacer.Implementation.PlaceGuardPosts( Context, ThisPlanet, faction, null, sentinelsExternal, true, ref amountOfStrengthAdded );

                    ArcenDebugging.ArcenDebugLogSingleLine( "Reconquest Seeding for " + ThisPlanet.Name + ": guard post placer: " + ThisPlanet.GuardPostAndCommandPlacer.InternalName +
                                                            " scheduled: " + resultingPosts.LeftItem + " actually seeded: " + resultingPosts.RightItem, Verbosity.DoNotShow );
                }

                if ( ThisPlanet.MarkLevelForAIOnly.Ordinal > 1 )
                {
                    FInt guardianAIToPurchaseCostBudget = AIUtilityMethods.GetAICostPurchaseCapForBudgetType( ThisPlanet, faction, ReinforcementType.Guardian, true, debug, sentinelsExternal ) * sentinelsExternal.AIType.MultiplierForStartingGuardianBudgetOnReconquest;
                    this.Reinforce( Context, ThisPlanet, faction, guardianAIToPurchaseCostBudget.GetNearestIntPreferringHigher(), ReinforcementType.Guardian, true, true, ref amountOfStrengthAdded );
                }

                //log that this was a reinforcement event
                ThisPlanet.TotalStrengthOfAllSentinelReinforcements += amountOfStrengthAdded;
                ThisPlanet.StrengthOfLastSentinelReinforcements = amountOfStrengthAdded;
                ThisPlanet.NumberOfSentinelReinforcementsEvents++;
                ThisPlanet.TimeOfLastSentinelReinforcement = World_AIW2.Instance.GameSecond;

            }
            catch ( Exception e )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "Hit debugCode " + debugCode + " exception " + e + " in StartReconquestDefenseSeeding for " + ThisPlanet.Name, Verbosity.DoNotShow );
            }
        }
        
        public override void DoInitialDefenseSeeding( ArcenHostOnlySimContext Context, Planet ThisPlanet, Tutorial.TutorialPlanetOwnershipAndSpawning TutorialPlanetOrNull, Faction owningFaction = null)
        {
            bool debug = GameSettings_AIW2.Current.GetBool( ArcenBoolSetting_AIW2.EnableReinforcementLogging );

            int debugStage = 1000;
            try
            {
                Faction faction = ThisPlanet.GetControllingFaction();
                if ( faction == null )
                {
                    Engine_AIW2.Instance.LogErrorDuringCurrentMapGen( "Null faction controlling planet " + ThisPlanet.Name + " (" + ThisPlanet.Index + ") in DoInitialOrReconquestDefenseSeeding. This should not be possible" );
                    return;
                }
                if ( faction.Type == FactionType.NaturalObject )
                {
                    //owningFaction is an override
                    if(owningFaction == null)
                        faction = ThisPlanet.GetFirstFactionOfType( FactionType.AI ).Faction;
                    else
                        faction = owningFaction;
                }

                AISentinelsCoreData sentinelsExternal = faction.TryGetAISentinelsCoreData()?.SentinelInfo;
                if ( sentinelsExternal == null && (faction.Type == FactionType.SpecialFaction || faction.Type == FactionType.NaturalObject) )
                {
                    faction = ThisPlanet.GetFirstFactionOfType( FactionType.AI ).Faction;
                    sentinelsExternal = faction.TryGetAISentinelsCoreData()?.SentinelInfo;
                }

                if ( sentinelsExternal == null )
                {
                    Engine_AIW2.Instance.LogErrorDuringCurrentMapGen( "Null sentinelsExternal on faction of type " + faction.Type + " on planet " + ThisPlanet.Name + " (" + ThisPlanet.Index + ") in DoInitialOrReconquestDefenseSeeding. This should not be possible" );
                    return;
                }
                AITypeData aiType = sentinelsExternal.AIType;
                if ( aiType == null )
                {
                    Engine_AIW2.Instance.LogErrorDuringCurrentMapGen( "Null aiType on sentinelsExternal of faction of type " + faction.Type + " on planet " + ThisPlanet.Name + " (" + ThisPlanet.Index + ") in DoInitialOrReconquestDefenseSeeding. This should not be possible" );
                    return;
                }
                AIDifficulty difficulty = sentinelsExternal.AIDifficulty;
                if ( difficulty == null )
                {
                    Engine_AIW2.Instance.LogErrorDuringCurrentMapGen( "Null difficulty on sentinelsExternal of faction of type " + faction.Type + " on planet " + ThisPlanet.Name + " (" + ThisPlanet.Index + ") in DoInitialOrReconquestDefenseSeeding. This should not be possible" );
                    return;
                }
                AIBudgetItem reinforcementBudgetItem = aiType.BudgetItems[AIBudgetType.Reinforcement];
                if ( reinforcementBudgetItem == null )
                {
                    Engine_AIW2.Instance.LogErrorDuringCurrentMapGen( "Null reinforcementBudgetItem on aiType of faction of type " + faction.Type + " on planet " + ThisPlanet.Name + " (" + ThisPlanet.Index + ") in DoInitialOrReconquestDefenseSeeding. This should not be possible" );
                    return;
                }   

                debugStage = 1100;
                bool isAdjacentToHumanHomeworld = ThisPlanet.OriginalHopsToHumanHomeworld <= 1;

                debugStage = 1800;

                PlanetFaction pFaction = ThisPlanet.GetPlanetFactionForFaction( faction );
                DefinePlanetFactionDefenseTypesIfNeeded( Context, ThisPlanet, faction, false );

                debugStage = 2000;
                #region Seed wormhole sentinels (ONLY do this on the initial seeding)
                if(!isAdjacentToHumanHomeworld)
                {
                    AIShipGroupCategory sentinelCategory = reinforcementBudgetItem.WormholeSentinelAIShipGroup;

                    debugStage = 2100;
                    if ( sentinelCategory.DrawBag.GetHasItems() && ( TutorialPlanetOrNull == null || !TutorialPlanetOrNull.SkipAIWormholeSentinels ) )
                    {
                        debugStage = 2200;
                        foreach ( GameEntity_Other entity in ThisPlanet.Others( OtherSpecialEntityType.Wormhole ) )
                        {
                            //choose a different sentinel category per wormhole, rather than being per-planet.
                            AIShipGroup sentinelsGroup = sentinelCategory.DrawBag.PickRandomItemAndReplace( Context.RandomToUse );
                            //it's quite likely the group will be empty, as that's a common case and we just want to put nothing there.  All good!
                            if ( sentinelsGroup == null || sentinelsGroup.DrawBag.InternalListSize <= 0 )
                                continue;

                            int wormholeSentinelsBudget = Context.RandomToUse.Next( difficulty.MinWormholeSentimelBudget, difficulty.MaxWormholeSentimelBudget );
                            int seedAttemptCount = 200;

                            while ( wormholeSentinelsBudget > 0 && seedAttemptCount-- > 0 )
                            {
                                GameEntityTypeData typeToPlace = sentinelsGroup.DrawBag.PickRandomItemAndReplace( Context.RandomToUse );
                                if ( typeToPlace == null )
                                    continue; //having some of these be empty is ok!

                                wormholeSentinelsBudget -= typeToPlace.CostForAIToPurchase;

                                debugStage = 2300;
                                int combinedRadii = typeToPlace.ForMark[Balance_MarkLevelTable.Instance.MaxOrdinal].Radius + entity.TypeData.BaseMark.Radius;
                                debugStage = 2400;
                                Helper_TryToSeedEntityWithPlanetMark_MapgenOnly_AtSpecificPoint( Context, ThisPlanet, entity.WorldLocation, faction, typeToPlace, combinedRadii, combinedRadii << 1 );
                            }
                            //if ( seedAttemptCount <= 0 )
                            //{
                            //    ArcenDebugging.ArcenDebugLog( sentinelsGroup.InternalName + " sentinel budget hit zero, with drawbag size of :" + sentinelsGroup.DrawBag.InternalListSize + " " +
                            //        sentinelsGroup.DrawBag.GetHasItems() + " " + ( sentinelsGroup.DrawBag.InternalListSize != 1 ? "??" :
                            //        sentinelsGroup.DrawBag.PickRandomItemAndReplace( Context.RandomToUse ).InternalName + "   " +
                            //        sentinelsGroup.DrawBag.PickRandomItemAndReplace( Context.RandomToUse ).CostForAIToPurchase ), Verbosity.Chat );
                            //}
                        }
                    }
                }
                #endregion

                int amountOfStrengthAdded = 0;

                debugStage = 4000;
                //Seed guard posts
                ThisPlanet.GuardPostAndCommandPlacer.Implementation.PlaceGuardPosts( Context, ThisPlanet, faction, TutorialPlanetOrNull, sentinelsExternal, false, ref amountOfStrengthAdded );
                if ( MapgenLogger.IsActive )
                    MapgenLogger.Log( "Guard post style for planet " + ThisPlanet.Name + " of mark" + ThisPlanet.MarkLevelForAIOnly.Ordinal + ": " + ThisPlanet.GuardPostAndCommandPlacer.InternalName );

                debugStage = 5000;

                //seed guardians
                if ( ThisPlanet.MarkLevelForAIOnly.Ordinal > 1 && (TutorialPlanetOrNull == null || !TutorialPlanetOrNull.SkipAIGuardians) )
                {
                    FInt guardianAIToPurchaseCostBudget = AIUtilityMethods.GetAICostPurchaseCapForBudgetType( ThisPlanet, faction, ReinforcementType.Guardian, true, debug, sentinelsExternal ) * 
                        sentinelsExternal.AIType.MultiplierForGameStartingGuardiansFromTotalBudget;
                    this.Reinforce( Context, ThisPlanet, faction, guardianAIToPurchaseCostBudget.GetNearestIntPreferringHigher(), ReinforcementType.Guardian, true, true, ref amountOfStrengthAdded );
                }
                if ( amountOfStrengthAdded > 0 ) { } //we don't care

                debugStage = 6000;
            }
            catch ( Exception e )
            {
                Engine_AIW2.Instance.LogErrorDuringCurrentMapGen( "Exception thrown in DoInitialDefenseSeeding in stage " + debugStage + "\n" + e.ToString() );
            }
        }

        #region DefinePlanetFactionDefenseTypesIfNeeded
        public static void DefinePlanetFactionDefenseTypesIfNeeded( ArcenHostOnlySimContext Context, Planet ThisPlanet, Faction faction, bool overrideExistingTypes  )
        {
            if ( faction == null )
                return;
            int debugStage = 0;
            try
            {
                debugStage = 100;
                AITypeData aiType = faction.TryGetAISentinelsCoreData().SentinelInfo.AIType;
                debugStage = 200;
                AIBudgetItem reinforcementBudgetItem = aiType.BudgetItems[AIBudgetType.Reinforcement];
                debugStage = 300;
                AIBudgetItem waveBudgetItem = aiType.BudgetItems[AIBudgetType.Wave];

                //bool isAdjacentToHumanHomeworld = ThisPlanet.OriginalHopsToHumanHomeworld <= 1;
                debugStage = 400;
                PlanetFaction pFaction = ThisPlanet.GetPlanetFactionForFaction( faction );

                debugStage = 1000;
                if ( pFaction.ShipGroup_BasicGuardians == null || overrideExistingTypes )
                {
                    debugStage = 1100;
                    //on AI homeworlds, it uses dire guardians in place of the basic guardians
                    if ( ThisPlanet.PopulationType == PlanetPopulationType.AIHomeworld ) //don't do this for AIBastionWorld
                    {
                        debugStage = 1200;
                        pFaction.ShipGroup_BasicGuardians = reinforcementBudgetItem.DireGuardianAIShipGroup.DrawBag.PickRandomItemAndReplace( Context.RandomToUse );
                        debugStage = 1300;
                        pFaction.ShipGroup_DireGuardians = pFaction.ShipGroup_BasicGuardians;
                    }

                    debugStage = 1400;
                    if ( (pFaction.ShipGroup_BasicGuardians == null || overrideExistingTypes) || !pFaction.ShipGroup_BasicGuardians.DrawBag.GetHasItems() )
                        pFaction.ShipGroup_BasicGuardians = reinforcementBudgetItem.GuardianAIShipGroup.DrawBag.PickRandomItemAndReplace( Context.RandomToUse );
                }

                debugStage = 2000;
                if ( pFaction.ShipGroup_DireGuardians == null || overrideExistingTypes )
                    pFaction.ShipGroup_DireGuardians = reinforcementBudgetItem.DireGuardianAIShipGroup.DrawBag.PickRandomItemAndReplace( Context.RandomToUse );

                debugStage = 3000;
                if ( pFaction.ShipGroup_Reinforcements == null || overrideExistingTypes )
                    pFaction.ShipGroup_Reinforcements = reinforcementBudgetItem.NormalAIShipGroup.DrawBag.PickRandomItemAndReplace( Context.RandomToUse );

                debugStage = 4000;
                if ( pFaction.ShipGroup_Turrets == null || overrideExistingTypes )
                    pFaction.ShipGroup_Turrets = reinforcementBudgetItem.TurretAIShipGroup.DrawBag.PickRandomItemAndReplace( Context.RandomToUse );

                debugStage = 5000;
                if ( pFaction.ShipGroup_NonTurretDefenses == null || overrideExistingTypes )
                    pFaction.ShipGroup_NonTurretDefenses = reinforcementBudgetItem.NonTurretDefenseAIShipGroup.DrawBag.PickRandomItemAndReplace( Context.RandomToUse );

                debugStage = 6000;
                if ( pFaction.ShipGroup_Strikecraft == null || overrideExistingTypes )
                    pFaction.ShipGroup_Strikecraft = reinforcementBudgetItem.NormalAIShipGroup.DrawBag.PickRandomItemAndReplace( Context.RandomToUse );

                debugStage = 6000;
                if ( ThisPlanet.ShipGroup_WavesFromHere_Normal == null || overrideExistingTypes )
                    ThisPlanet.ShipGroup_WavesFromHere_Normal = waveBudgetItem.NormalAIShipGroup.DrawBag.PickRandomItemAndReplace( Context.RandomToUse );

                debugStage = 7000; 
                if ( ThisPlanet.ShipGroup_WavesFromHere_Guardians == null || overrideExistingTypes )
                    ThisPlanet.ShipGroup_WavesFromHere_Guardians = waveBudgetItem.GuardianAIShipGroup.DrawBag.PickRandomItemAndReplace( Context.RandomToUse );

                debugStage = 8000; 
                if ( ThisPlanet.ShipGroup_WavesFromHere_DireGuardians == null || overrideExistingTypes )
                    ThisPlanet.ShipGroup_WavesFromHere_DireGuardians = waveBudgetItem.DireGuardianAIShipGroup.DrawBag.PickRandomItemAndReplace( Context.RandomToUse );
            }
            catch ( Exception e )
            {
                Engine_AIW2.Instance.LogErrorDuringCurrentMapGen( "Exception thrown in DefinePlanetFactionDefenseTypesIfNeeded in stage " + debugStage + "\n" + e.ToString() );
            }
        }
        #endregion
    }
}
