using Arcen.AIW2.Core;
using System;

using System.Text;
using Arcen.Universal;

/*
  Overall organization: there's a BaseInfo class (for scourge global stuff),
  a ScourgePerUnitBaseInfo (does what it says),
  a ScourgeDifficulty (which is used for balance stuff),
  the Scourge class.

  LongRangePlanning relies heavily on Fireteams, which is done in Fireteam.cs.
   */

namespace Arcen.AIW2.External
{
    public sealed class TiberiumFactionDeepInfo : ExternalFactionDeepInfoRoot
    {
        public TiberiumFactionBaseInfo BaseInfo;
        public ArmadaFactionBaseInfo ArmadaBaseInfo;
        public static TiberiumFactionDeepInfo Instance = null;
        private static readonly List<GameEntity_Squad> WorkingList = List<GameEntity_Squad>.Create_WillNeverBeGCed(100, "TiberiumFactionDeepInfo-WorkingList");
        private static readonly List<Planet> WorkingPlanetList = List<Planet>.Create_WillNeverBeGCed(100, "TiberiumFactionDeepInfo-WorkingPlanetList");
        public override void DoAnyInitializationImmediatelyAfterFactionAssigned()
        {
            this.BaseInfo = this.AttachedFaction.GetExternalBaseInfoAs<TiberiumFactionBaseInfo>();
            Faction armadaFaction = FactionUtilityMethods.Instance.GetArmadaFaction();
            if ( armadaFaction != null )
                this.ArmadaBaseInfo = armadaFaction.GetExternalBaseInfoAs<ArmadaFactionBaseInfo>();
            Instance = this;
        }
        protected override void Cleanup()
        {
            Instance = null;
            BaseInfo = null;
            ArmadaBaseInfo = null;
            AutoDefendingShipsLRP.Clear();
            WorkingPlanetsLRP.Clear();
            WorkingList.Clear();
            WorkingPlanetList.Clear();
            //probably does not matter
        }
        protected override int MinimumSecondsBetweenLongRangePlannings => 10;
        public override void DoPerSecondLogic_Stage3Main_OnMainThreadAndPartOfSim_HostOnly(ArcenHostOnlySimContext Context)
        {
            bool tracing = this.tracing_shortTerm = Engine_AIW2.TraceAtAll && Engine_AIW2.TracingFlags.Has(ArcenTracingFlags.Armada);
            ArcenCharacterBuffer tracingBuffer = tracing ? ArcenCharacterBuffer.GetFromPoolOrCreate("TiberiummD-DoPerSecondLogic_Stage3Main_OnMainThreadAndPartOfSim_HostOnly-trace", 10f) : null;

            if ( ArmadaBaseInfo == null )
            {
                //ArcenDebugging.LogSingleLine("trying to find armada base info", Verbosity.DoNotShow );
                Faction armadaFaction = FactionUtilityMethods.Instance.GetArmadaFaction();
                if ( armadaFaction != null )
                    this.ArmadaBaseInfo = armadaFaction.GetExternalBaseInfoAs<ArmadaFactionBaseInfo>();
            }

            UpdateVeins( Context );
            UpdateSummoners( Context );
            HandleGuardianDonation(Context); //this isn't used anymore, but maybe someday?
            HandleAttritioningSim(Context);

            #region Tracing
            if (tracing && !tracingBuffer.GetIsEmpty()) ArcenDebugging.ArcenDebugLogSingleLine(tracingBuffer.ToString(), Verbosity.DoNotShow);
            if (tracing)
            {
                tracingBuffer.ReturnToPool();
                tracingBuffer = null;
            }
            #endregion
        }
        private void GoToLocation( GameEntity_Squad squad, ArcenPoint location, ArcenLongTermIntermittentPlanningContext Context, PerFactionPathCache pathCacheData )
        {
            //We have the metal and the structure has the experience, so fly to it so we can upgrade it
            GameCommand moveCommand = GameCommand.Create( BaseGameCommand.CommandsByCode[BaseGameCommand.Code.MoveManyToOnePoint_NPCVisitTargetOnPlanet], GameCommandSource.AnythingElse );
            moveCommand.PlanetOrderWasIssuedFrom = squad.Planet.Index;
            moveCommand.RelatedPoints.Add( location );
            moveCommand.RelatedEntityIDs.Add( squad.PrimaryKeyID );
            World_AIW2.Instance.QueueGameCommand( this.AttachedFaction, moveCommand, false );
        }
        private void HandleAttritioningSim ( ArcenHostOnlySimContext Context )
        {
            FInt dropshipPercent = FInt.FromParts(3, 200);
            FInt locustInBattlePercent = FInt.FromParts(1, 150);
            FInt locustVictoriousPercent = FInt.FromParts(4, 550);
            //these dire guardians were spawned by Tiberium;
            //if they win the battle, give them to the AI so they can go be helpful
            foreach ( GameEntity_Squad locust in this.AttachedFaction.Squads( "AttritioningLocust" ) )
            {
                 EnumIndexedArray<FactionStance, StrengthData_PlanetFaction_Stance> myFactionData = locust.Planet.GetStanceDataForFaction( AttachedFaction );
                 int hostileStrength = myFactionData[FactionStance.Hostile].TotalStrength;
                 FInt percent = locustInBattlePercent;
                 if ( hostileStrength < 10 * 1000 )
                 {
                     percent = locustVictoriousPercent;
                 }
                 int damageToTake = ((percent * locust.GetMaxHullPoints()) / 100).IntValue;
                 locust.TakeDamageDirectly( damageToTake, null, null, DamageSource.BeingScrapped, Context);
            }
            foreach ( GameEntity_Squad dropship in this.AttachedFaction.Squads( "TiberiumDropship" ) )
            {
                 EnumIndexedArray<FactionStance, StrengthData_PlanetFaction_Stance> myFactionData = dropship.Planet.GetStanceDataForFaction( AttachedFaction );
                 int hostileStrength = myFactionData[FactionStance.Hostile].TotalStrength;
                 if ( dropship.HasQueuedOrders() || dropship.GetSecondsSinceCreation() < 20 )
                     continue; //while en route (or freshly created), don't attrition
                 int damageToTake = ((dropshipPercent * dropship.GetMaxHullPoints()) / 100).IntValue;
                 dropship.TakeDamageDirectly( damageToTake, null, null, DamageSource.BeingScrapped, Context);
            }

        }
        private void HandleGuardianDonation ( ArcenHostOnlySimContext Context )
        {
            int maxToProcess = 3;
            //these dire guardians were spawned by Tiberium;
            //if they win the battle, give them to the AI so they can go be helpful
            foreach ( GameEntity_Squad guardian in this.AttachedFaction.Squads( "DonatesToAIOnVictory" ) )
            {
                 EnumIndexedArray<FactionStance, StrengthData_PlanetFaction_Stance> myFactionData = guardian.Planet.GetStanceDataForFaction( AttachedFaction );
                 int hostileStrength = myFactionData[FactionStance.Hostile].TotalStrength;
                 int alliedStrength = myFactionData[FactionStance.Friendly].TotalStrength + myFactionData[FactionStance.Self].TotalStrength;
                 if ( hostileStrength > 0 )
                     continue;
                 //if we have won, start giving these ships to the AI
                 if ( maxToProcess-- <= 0 )
                     break;

                 Faction aiFaction = World_AIW2.GetRandomAIFaction( Context );

                 GameCommand transferToAI = GameCommand.Create( BaseGameCommand.CommandsByCode[BaseGameCommand.Code.TransferEntitiesToFaction], GameCommandSource.AnythingElse );
                 transferToAI.RelatedFactionIndex = aiFaction.FactionIndex;
                 transferToAI.RelatedEntityIDs.Add( guardian.PrimaryKeyID );
                 World_AIW2.Instance.QueueGameCommand( this.AttachedFaction, transferToAI, false );
            }
        }
        private void UpdateSummoners ( ArcenHostOnlySimContext Context )
        {
            bool debug = false;
            foreach ( GameEntity_Squad summoner in this.BaseInfo.Summoners.DisplaySquads() )
            {
                if ( summoner == null)
                    continue;
                TiberiumPerUnitBaseInfo data = summoner.TryGetExternalBaseInfoAs<TiberiumPerUnitBaseInfo>();
                if ( data == null )
                    continue;

                //Now spawn Torments if appropriate
                if ( data.TimeForNextSpawn > World_AIW2.Instance.GameSecond )
                    continue;
                if ( debug )
                    ArcenDebugging.LogSingleLine("Spawning a torment", Verbosity.DoNotShow );
                bool mustBeHunter = true;
                bool spawnedShip = SpawnAIShip( Context, summoner, "TiberiumTorment", mustBeHunter );
                data.TimeForNextSpawn = World_AIW2.Instance.GameSecond + BaseInfo.Difficulty.SummonTime;

            }
        }
        private bool CanPlanetBuildSummoner(Planet planet, ArcenHostOnlySimContext Context, bool allowSummoner = false )
        {
            bool planetMissingVein = false;
            int summonersFromAIP = ((GlobalAIWorldBaseInfo.Instance.AIProgress_Effective/50) * BaseInfo.Difficulty.AdditionalSummonersPer50AIP).IntValue;
            int summonersFromTime = World_AIW2.Instance.GameSecond / 3600;
            int allowedSummoners = BaseInfo.Difficulty.BaseSummoners + summonersFromAIP + summonersFromAIP;
            //ArcenDebugging.LogSingleLine("allowed summoners: " + allowedSummoners + " curr summoners " + BaseInfo.Summoners.Count, Verbosity.DoNotShow );
            if ( BaseInfo.Summoners.Count + BaseInfo.VeinsBuildingSummoners.Display  >= allowedSummoners )
            {
                return false;
            }
            if ( ArmadaBaseInfo == null )
                return false; //no Armada Faction
            bool foundSummoner = false;
            foreach ( GameEntity_Squad summoner in this.BaseInfo.Summoners.DisplaySquads() )
            {
                if ( allowSummoner )
                    break;
                if ( summoner.Planet == planet )
                {
                    foundSummoner = true;
                    break;
                }
            }
            if ( foundSummoner )
                return false;
            foreach ( Planet planetToCheck in planet.LinkedNeighborsAndSelf( false ) )
            {
                bool foundVeinOnPlanet = false;
                foreach ( GameEntity_Squad vein in this.BaseInfo.Veins.DisplaySquads() )
                {
                   if ( vein.Planet == planetToCheck && vein.CurrentMarkLevel == 7 )
                   {
                       foundVeinOnPlanet = true;
                       break;
                   }
                }
                if ( ! foundVeinOnPlanet )
                {
                    planetMissingVein = true;
                    break;
                }
                continue;;
            }

            if ( planetMissingVein )
                return false;
            return true;
        }
        private void UpdateVeins ( ArcenHostOnlySimContext Context )
        {
            bool debug = false;
            WorkingPlanetList.Clear();
            bool checkDuplicates = false;
            if ( World_AIW2.Instance.GameSecond % 10 == 1 )
                checkDuplicates = true;
            foreach ( GameEntity_Squad vein in this.BaseInfo.Veins.DisplaySquads() )
            {
                if ( vein == null)
                    continue;
                if ( checkDuplicates )
                {
                    //make sure we don't have multiple veins on the same planet
                    bool duplicateFound = false;
                    for ( int i = 0; i < WorkingPlanetList.Count; i++ )
                    {
                        if ( vein.Planet == WorkingPlanetList[i])
                        {
                            vein.Despawn( Context, true, InstancedRendererDeactivationReason.AFactionJustWarpedMeOut );
                            duplicateFound = true;
                            break;
                        }
                    }
                    if ( duplicateFound )
                        continue;
                    WorkingPlanetList.Add( vein.Planet );
                }
                TiberiumPerUnitBaseInfo data = vein.TryGetExternalBaseInfoAs<TiberiumPerUnitBaseInfo>();
                if ( data == null )
                    continue;
                data.Points += BaseInfo.Difficulty.PerSecondVeinIncome;
                if ( World_AIW2.Instance.GameSecond % BaseInfo.Difficulty.BonusInterval == 0 )
                    data.Points += BaseInfo.Difficulty.BonusIntervalIncome;
                data.Points += vein.CurrentMarkLevel / 2;

                if ( vein.CurrentMarkLevel == 7 )
                    data.Points += 1; //a little bonus for the  highest mark

                if ( data.NextUpgrade == TiberiumUpgrade.None )
                {
                    SetNextUpgrade( Context, vein, data);
                }
                if ( debug )
                {
                    ArcenDebugging.LogSingleLine(vein.ToStringWithPlanet() + " next upgrade " + data.NextUpgrade.ToString() + " current points " + data.Points + " and auto defense points " + data.AutoDefenseBuildPoints, Verbosity.DoNotShow );
                }
                if ( data.NextUpgrade == TiberiumUpgrade.MarkUp && data.Points >= BaseInfo.Difficulty.VeinMarkupCost )
                {
                    vein.SetCurrentMarkLevel( (byte)(vein.CurrentMarkLevel + 1));
                    data.Points -= BaseInfo.Difficulty.VeinMarkupCost;
                    data.NextUpgrade = TiberiumUpgrade.None;
                }
                if ( data.NextUpgrade == TiberiumUpgrade.NewVein && data.Points >= BaseInfo.Difficulty.NewVeinCost )
                {
                    bool spawnedVein = SpawnNewVein( Context, vein );
                    if ( !spawnedVein )
                    {
                        data.Points -= BaseInfo.Difficulty.NewVeinCost;
                    }
                    data.NextUpgrade = TiberiumUpgrade.None;
                }
                if ( data.NextUpgrade == TiberiumUpgrade.BoostNextCPA && data.Points >= BaseInfo.Difficulty.BoostCPACost )
                {
                    //CPAs are only for the armada faction, since we track the CPA strength in the Armada faction
                    if ( ArmadaBaseInfo == null )
                    {
                        data.NextUpgrade = TiberiumUpgrade.None;
                    }
                    ArmadaBaseInfo.StrengthForNextEnemyAttack += BaseInfo.Difficulty.CPABoostAmount;
                    data.Points -= BaseInfo.Difficulty.BoostCPACost;
                    data.NextUpgrade = TiberiumUpgrade.None;
                }

                if ( data.NextUpgrade == TiberiumUpgrade.SpawnDireShip && data.Points >= BaseInfo.Difficulty.NewDireCost )
                {
                    bool spawnedShip = SpawnAIShip( Context, vein, "TiberiumDireGuardian" );
                    if ( spawnedShip )
                        data.Points -= BaseInfo.Difficulty.NewDireCost;
                    data.NextUpgrade = TiberiumUpgrade.None;
                }

                if ( data.NextUpgrade == TiberiumUpgrade.SpawnTierOneShip && data.Points >= BaseInfo.Difficulty.NewTierOneShipCost )
                {
                    //spawn a ship
                    bool spawnedShip = SpawnAIShip( Context, vein, "TiberiumTierOne" );
                    if ( spawnedShip )
                        data.Points -= BaseInfo.Difficulty.NewTierOneShipCost;
                    data.NextUpgrade = TiberiumUpgrade.None;
                }
                if ( data.NextUpgrade == TiberiumUpgrade.SpawnTierTwoShip && data.Points >= BaseInfo.Difficulty.NewTierTwoShipCost )
                {
                    //spawn a ship
                    bool spawnedShip = SpawnAIShip( Context, vein, "TiberiumTierTwo" );
                    if ( spawnedShip )
                        data.Points -= BaseInfo.Difficulty.NewTierTwoShipCost;
                    data.NextUpgrade = TiberiumUpgrade.None;
                }

                if ( data.NextUpgrade == TiberiumUpgrade.SpawnDefense && data.Points >= BaseInfo.Difficulty.NewDefenseCost )
                {
                    //spawn defenses
                    bool spawnedShip = SpawnAIShip( Context, vein, "TiberiumDefense" );
                    if ( spawnedShip )
                        data.Points -= BaseInfo.Difficulty.NewDefenseCost;
                    data.NextUpgrade = TiberiumUpgrade.None;
                }
                if ( data.NextUpgrade == TiberiumUpgrade.SpawnSummoner && data.Points >= BaseInfo.Difficulty.SummonerCost )
                {
                    //spawn a Tiberium Summoner; since the Summoner is in multi-phase, only spawn it when an Armada Empire is in the game
                    SpawnTiberiumSummoner(Context, vein);
                    data.Points -= BaseInfo.Difficulty.SummonerCost;
                    data.NextUpgrade = TiberiumUpgrade.None;
                }
                if ( ShouldVeinSpawnDefenseShips( vein ) )
                {
                    //Locust related income
                    data.AutoDefenseBuildPoints += BaseInfo.Difficulty.PerSecondDropshipIncome;
                    if ( World_AIW2.Instance.GameSecond % BaseInfo.Difficulty.BonusInterval == 0 )
                        data.AutoDefenseBuildPoints += BaseInfo.Difficulty.BonusIntervalIncome;
                    if ( data.AutoDefenseBuildPoints > BaseInfo.Difficulty.DropshipCost )
                    {
                        data.AutoDefenseBuildPoints -= BaseInfo.Difficulty.DropshipCost;
                        SpawnTiberiumShip( Context, vein, "TiberiumDropship");
                    }
                }
                else
                {
                    data.AutoDefenseBuildPoints = 0;
                }

            }
        }
        public bool ShouldVeinSpawnDefenseShips( GameEntity_Squad vein )
        {
            bool tracing = this.tracing_shortTerm = Engine_AIW2.TraceAtAll && Engine_AIW2.TracingFlags.Has(ArcenTracingFlags.Armada);
            ArcenCharacterBuffer tracingBuffer = tracing ? ArcenCharacterBuffer.GetFromPoolOrCreate( "Tiberium-FindEnemiesInVeinRange-trace", 10f ) : null;

            if ( vein == null )
                return false;
            //We can spawn defense ships if we are at high enough level and our neighbors are high enough.
            if ( vein.CurrentMarkLevel < BaseInfo.Difficulty.MarkLevelForVeinNeighborsForDefense )
                return false;

            //we can help on friendly or neutral planets with enemies and friends
            Planet foundPlanet = null;
            bool verboseDebug = true;
            short range = this.BaseInfo.Difficulty.LocustRange;
            if ( tracing && verboseDebug )
                tracingBuffer.Add(vein.ToStringWithPlanet() + " Is checking for enemies within " + range + " hops").Add("\n");

            foreach ( Planet.PlanetAtHopDistance _phd in vein.Planet.PlanetsWithinXHops( range,
              delegate ( Planet secondaryPlanet )
             {
                 //don't path through planets owned by enemies
                 if ( secondaryPlanet.GetControllingOrInfluencingFaction().GetIsHostileTowards(this.AttachedFaction) )
                 {
                     return PropogationEvaluation.No;
                 }
                 //You can only pass through planets with veins (TODO: consider removing this)
                 if ( !BaseInfo.DoesPlanetHaveVein(secondaryPlanet))
                     return PropogationEvaluation.No;
                 //Don't path through planets with significant enemy strength
                 var spFaction = secondaryPlanet.GetStanceDataForFaction( AttachedFaction );
                 int hostileStrength = spFaction[FactionStance.Hostile].TotalStrength;
                 int friendlystrength = spFaction[FactionStance.Friendly].TotalStrength + spFaction[FactionStance.Self].TotalStrength;
                 if ( hostileStrength > 20 * 1000 &&
                      hostileStrength > friendlystrength / 2 )
                     return PropogationEvaluation.SelfButNotNeighbors;

                 return PropogationEvaluation.Yes;
             } ) )
            {
                Planet planet = _phd.Planet;
                if ( verboseDebug && tracing )
                    tracingBuffer.Add("\tChecking whether there are enemies on " + planet.Name).Add("\n");
                if ( planet.GetControllingOrInfluencingFaction().GetIsHostileTowards(this.AttachedFaction) )
                {
                    if ( verboseDebug && tracing )
                        tracingBuffer.Add("\t\tCan't target planets owned by our enemies\n" );

                    continue;
                }
                var pFaction = planet.GetStanceDataForFaction( AttachedFaction );
                int hostileStrength = pFaction[FactionStance.Hostile].TotalStrength;
                if ( planet == vein.Planet && hostileStrength > 0 )
                {
                    //always go for enemies on our planet
                    foundPlanet = planet;
                    if ( verboseDebug && tracing )
                        tracingBuffer.Add("\t\tfound enemies right here\n" );

                    break;
                }


                if ( hostileStrength < 10 * 1000 )
                {
                    if ( verboseDebug && tracing )
                        tracingBuffer.Add("\t\tNo enemies\n" );
                    continue;
                }
                int friendlyStrength = pFaction[FactionStance.Friendly].TotalStrength;
                if ( friendlyStrength < 2000 )
                {
                    if ( verboseDebug && tracing )
                        tracingBuffer.Add("\t\tNo friends, and we don't fight on our own\n" );

                    continue;
                }
                if ( friendlyStrength < hostileStrength / 10 )
                {
                    //if on a remote planet, never go where we are outnumbered
                    //Always come out for our own planet though
                    if ( verboseDebug && tracing )
                        tracingBuffer.Add("\t\tWe are too outnumbered\n" );
                    continue;
                }
                if ( friendlyStrength / 5 > hostileStrength )
                {
                    //We're already winning really hard
                    if ( verboseDebug && tracing )
                        tracingBuffer.Add("\t\tWe are winning too hard\n" );
                    continue;
                }

                if ( verboseDebug && tracing )
                    tracingBuffer.Add("\tFOUND " + planet.Name + " with friendly strength " + friendlyStrength + " and hostile strength " + hostileStrength ).Add("\n");
                //ArcenDebugging.LogSingleLine(vein.ToStringWithPlanet() + " is defending " + planet.Name, Verbosity.DoNotShow );

                foundPlanet = planet;
                break;
            }
            
             #region Tracing
            if ( tracing && !tracingBuffer.GetIsEmpty() ) ArcenDebugging.ArcenDebugLogSingleLine( tracingBuffer.ToString(), Verbosity.DoNotShow );
            if ( tracing )
            {
                tracingBuffer.ReturnToPool();
                tracingBuffer = null;
            }
            #endregion
            if ( foundPlanet == null )
                return false;
            //ArcenDebugging.LogSingleLine(vein.ToStringWithPlanet() + " can spawn defenses; found planet " + foundPlanet.Name, Verbosity.DoNotShow );
            return true;
        }
        private bool SpawnAIShip(  ArcenHostOnlySimContext Context, GameEntity_Squad vein, string shipTag, bool mustBeHunter = false )
        {
            Faction aiFaction = World_AIW2.GetRandomAIFaction( Context );
            if ( aiFaction == null )
                return false;
            GameEntity_Squad king = FactionUtilityMethods.Instance.findKing( aiFaction );
            if ( king == null )
                return false;
            AISentinelsFactionBaseInfo aiBaseInfo = aiFaction.GetExternalBaseInfoAs<AISentinelsFactionBaseInfo>();
            if ( aiBaseInfo == null )
                return false;
            int rand = Context.RandomToUse.Next(0, 100);
            //sometimes warden, sometimes hunter for mobile ships
            GameEntityTypeData entityData = GameEntityTypeDataTable.Instance.GetRandomRowWithTag( Context, shipTag );
            PlanetFaction pFaction = vein.Planet.GetPlanetFactionForFaction( aiFaction );
            if ( entityData.IsMobileCombatant)
            {
                //if we are a mobile ship, sometimes go to huner/warden
                if ( rand < 50 )
                    pFaction = vein.Planet.GetPlanetFactionForFaction( aiBaseInfo.SubFac_Warden );
                else if ( rand < 60 )
                    pFaction = vein.Planet.GetPlanetFactionForFaction( aiBaseInfo.SubFac_Hunter );
                if ( mustBeHunter )
                    pFaction = vein.Planet.GetPlanetFactionForFaction( aiBaseInfo.SubFac_Hunter );
            }

            ArcenPoint spawnLocation = vein.Planet.GetSafePlacementPoint_AroundEntity( Context, entityData, vein, FInt.FromParts( 0, 050 ), FInt.FromParts( 0, 250 ) );
            GameEntity_Squad newEntity = GameEntity_Squad.CreateNew_ReturnNullIfMPClient( pFaction, entityData, pFaction.Faction.CurrentGeneralMarkLevel,
                pFaction.Faction.LooseFleet, 0, spawnLocation, Context, "SpawnAIShipForTiberium" );
            if ( newEntity == null )
                return false;
            newEntity.Orders.SetBehaviorDirectlyInSim( EntityBehaviorType.Attacker_Full ); //is fine, main sim thread
            //ArcenDebugging.LogSingleLine("Spawned a " + newEntity.ToStringWithPlanet(), Verbosity.DoNotShow );
            return true;
        }
        private bool SpawnTiberiumShip(  ArcenHostOnlySimContext Context, GameEntity_Squad vein, string shipTag )
        {
            PlanetFaction pFaction = vein.PlanetFaction;
            GameEntityTypeData entityData = GameEntityTypeDataTable.Instance.GetRandomRowWithTag( Context, shipTag );
            ArcenPoint spawnLocation = vein.Planet.GetSafePlacementPoint_AroundEntity( Context, entityData, vein, FInt.FromParts( 0, 050 ), FInt.FromParts( 0, 250 ) );
            GameEntity_Squad newEntity = GameEntity_Squad.CreateNew_ReturnNullIfMPClient( pFaction, entityData, pFaction.Faction.CurrentGeneralMarkLevel,
                pFaction.Faction.LooseFleet, 0, spawnLocation, Context, "SpawnTiberiumShip" );
            if ( newEntity == null )
                return false;
            newEntity.Orders.SetBehaviorDirectlyInSim( EntityBehaviorType.Attacker_Full ); //is fine, main sim thread
            TiberiumPerUnitBaseInfo data = newEntity.CreateExternalBaseInfo<TiberiumPerUnitBaseInfo>( "TiberiumPerUnitBaseInfo" );
            data.HomeVein = LazyLoadSquadWrapper.Create(vein);
            //ArcenDebugging.LogSingleLine("Spawned a " + newEntity.ToStringWithPlanet(), Verbosity.DoNotShow );
            return true;
        }
        private bool HasAdjacentPlanetWithoutVein( ArcenHostOnlySimContext Context, Planet planet )
        {
            bool veinlessPlanetFound = false;
            foreach ( Planet neighbor in planet.LinkedNeighbors( false ) )
            {
                if ( neighbor == null || neighbor.HasPlanetBeenDestroyed )
                    continue;
                if ( !neighbor.GetControllingOrInfluencingFaction().GetIsFriendlyTowards(this.AttachedFaction))
                    continue; //must be owned by a friendly faction

                if ( !BaseInfo.DoesPlanetHaveVein( neighbor ))
                {
                    veinlessPlanetFound = true;
                    break;
                }
            }
            return veinlessPlanetFound;
        }
        private bool SpawnNewVein( ArcenHostOnlySimContext Context, GameEntity_Squad vein )
        {
            bool spawnedVein = false;
            foreach ( Planet neighbor in vein.Planet.LinkedNeighbors( false ) )
            {
                if ( neighbor == null || neighbor.HasPlanetBeenDestroyed )
                    continue;
                if ( BaseInfo.DoesPlanetHaveVein( neighbor ))
                    continue; //cant already have a vein
                if ( !neighbor.GetControllingOrInfluencingFaction().GetIsFriendlyTowards(this.AttachedFaction))
                    continue; //must be owned by a friendly faction
                //We don't have a vein here now, so spawn one
                GameEntityTypeData typeData = GameEntityTypeDataTable.Instance.GetRowByName("TiberiumVein");
                if ( typeData == null )
                    throw new Exception("Could not find tiberium vein type data");
                ArcenPoint spawnLocation = neighbor.GetSafePlacementPoint_AroundDesiredPointVicinity( Context, typeData, Engine_AIW2.Instance.CombatCenter, FInt.FromParts( 0, 150 ), FInt.FromParts( 0, 250 ) );
                PlanetFaction pFaction = neighbor.GetPlanetFactionForFaction( this.AttachedFaction );
                GameEntity_Squad newVein = GameEntity_Squad.CreateNew_ReturnNullIfMPClient( pFaction, typeData, 1, null, 0, spawnLocation, Context, "Tiberium-NewVein" );
                TiberiumPerUnitBaseInfo newData = newVein.CreateExternalBaseInfo<TiberiumPerUnitBaseInfo>("TiberiumPerUnitBaseInfo");
                spawnedVein = true;
                break;
            }
            return spawnedVein;
        }
        private void SpawnTiberiumSummoner( ArcenHostOnlySimContext Context, GameEntity_Squad vein )
        {
            GameEntityTypeData typeData = GameEntityTypeDataTable.Instance.GetRowByName("TiberiumSummoner");
            if ( typeData == null )
                throw new Exception("Could not find tiberium summoner type data");
            ArcenPoint spawnLocation = vein.Planet.GetSafePlacementPoint_AroundEntity( Context, typeData, vein, FInt.FromParts( 0, 150 ), FInt.FromParts( 0, 250 ) );
            PlanetFaction pFaction = vein.PlanetFaction;
            GameEntity_Squad summoner = GameEntity_Squad.CreateNew_ReturnNullIfMPClient( pFaction, typeData, 1, null, 0, spawnLocation, Context, "Tiberium-NewSummoner" );
            TiberiumPerUnitBaseInfo data = summoner.CreateExternalBaseInfo<TiberiumPerUnitBaseInfo>("TiberiumPerUnitBaseInfo");
            data.TimeForNextSpawn = World_AIW2.Instance.GameSecond + 1800;
        }


        private void SetNextUpgrade( ArcenHostOnlySimContext Context, GameEntity_Squad vein, TiberiumPerUnitBaseInfo data  )
        {
            //pick our next upgrade
            bool debug = false;
            if ( debug )
                ArcenDebugging.LogSingleLine(vein.ToString() + " choosing next Upgrade  " + vein.CurrentMarkLevel, Verbosity.DoNotShow );

            if ( vein.CurrentMarkLevel < 3 )
            {
                if ( debug )
                    ArcenDebugging.LogSingleLine(vein.ToString() + " is marking up; it is only level  " + vein.CurrentMarkLevel, Verbosity.DoNotShow );
                data.NextUpgrade = TiberiumUpgrade.MarkUp;
                return;
            }
            bool canSpawnVein = HasAdjacentPlanetWithoutVein( Context, vein.Planet );
            bool highEnoughMarkForSummoner = false;
            if ( vein.CurrentMarkLevel == 7 )
                highEnoughMarkForSummoner = true;

            data.NextUpgrade = TiberiumUpgrade.None;
            int rand = Context.RandomToUse.Next(0, 100);
            if ( data.PreferredUpgrade != TiberiumUpgrade.None )
            {
                if ( debug )
                    ArcenDebugging.LogSingleLine("\tHas preferred upgrade " + data.PreferredUpgrade, Verbosity.DoNotShow );

                if ( rand < 60 )
                {
                    if ( debug )
                        ArcenDebugging.LogSingleLine("\tChoosing preferred upgrade", Verbosity.DoNotShow );
                    data.NextUpgrade = data.PreferredUpgrade;
                    if ( data.PreferredUpgrade == TiberiumUpgrade.NewVein &&
                         vein.CurrentMarkLevel < 7 )
                    {
                        //when the Armada uses Empower Vein, get another markup in too
                        data.PreferredUpgrade = TiberiumUpgrade.MarkUp;
                    }
                    else //default path
                        data.PreferredUpgrade = TiberiumUpgrade.None;
                    return;
                }
            }
            if ( rand < 20 && vein.CurrentMarkLevel != 7 )
            {
                data.NextUpgrade = TiberiumUpgrade.MarkUp;
            }
            else if ( rand < 45 && canSpawnVein )
            {
                data.NextUpgrade = TiberiumUpgrade.NewVein;
            }
            else if ( rand < 55 && ArmadaBaseInfo != null )
            {
                data.NextUpgrade = TiberiumUpgrade.BoostNextCPA;
            }
            else if ( rand < 70 )
            {
                int shipRandom = Context.RandomToUse.Next(0, 100);
                data.NextUpgrade = TiberiumUpgrade.SpawnTierOneShip; //default
                if ( shipRandom < 30 )
                {
                    if ( vein.CurrentMarkLevel > 6 )
                        data.NextUpgrade = TiberiumUpgrade.SpawnDireShip;
                    else
                        data.NextUpgrade = TiberiumUpgrade.SpawnTierTwoShip;
                }
                else if ( shipRandom < 70 && vein.CurrentMarkLevel > 4 )
                {
                    data.NextUpgrade = TiberiumUpgrade.SpawnTierTwoShip;
                }
            }
            else if ( rand < 80 && highEnoughMarkForSummoner && CanPlanetBuildSummoner( vein.Planet, Context ))
            {
                data.NextUpgrade = TiberiumUpgrade.SpawnSummoner;
            }
            else
            {
                data.NextUpgrade = TiberiumUpgrade.SpawnDefense;
            }
            if ( debug )
                ArcenDebugging.LogSingleLine("\tChoosing " + data.NextUpgrade, Verbosity.DoNotShow );

        }
        private static readonly List<SafeSquadWrapper> AutoDefendingShipsLRP = List<SafeSquadWrapper>.Create_WillNeverBeGCed( 500, "TiberiumFactionDeepInfo-AutoDefendingShipsLRP" );
        private static readonly List<Planet> WorkingPlanetsLRP = List<Planet>.Create_WillNeverBeGCed( 500, "ArmadaFactionDeepInfo-WorkingPlanetsLRP" );
        public override void DoLongRangePlanning_OnBackgroundNonSimThread_Subclass( ArcenLongTermIntermittentPlanningContext Context )
        {
            PerFactionPathCache pathingCacheData = PerFactionPathCache.GetCacheForTemporaryUse_MustReturnToPoolAfterUseOrLeaksMemory();
            AutoDefendingShipsLRP.Clear();
            WorkingPlanetsLRP.Clear();
            int debugCode = 0;
            try {
                debugCode = 100;
                foreach ( GameEntity_Squad entity in this.AttachedFaction.Squads() )
                {
                    debugCode = 200;
                    if ( entity == null )
                        continue;
                    debugCode = 300;
                    if (entity.TypeData.GetHasTag("TiberiumAutoDefenseShip"))
                    {
                        AutoDefendingShipsLRP.Add( entity );
                        continue;
                    }
                }
                //Below is the armada auto-defend mode
            for (int i = 0; i < AutoDefendingShipsLRP.Count; i++ )
            {
                /* This is a very simple implementation. There's a ton of fancy things we could do, like 
                   being able to do multiple passes to find categories of battles (defensive/offensive), and prioritizing defense (or offense).
                   Another good improvement would be being able to make more informed choices that aren't "Just go to the closest battle".
                   Another good improvement would be letting fleets go to "threatened" planets (ie planets with incoming waves, or lots of hostile mobile forces nearby)

                   For that matter, there's no principled reason we couln't give a "Offensive" mode, where we could kill Instigator bases,
                   AIP reducers, or just neutering enemy planets.
                 */
                GameEntity_Squad defenseShip = AutoDefendingShipsLRP[i].GetSquad();
                if ( defenseShip == null )
                    continue;
                // if ( defenseShip.TypeData.GetHasTag("ArmadaSphereFlagship") ||
                //      defenseShip.TypeData.GetHasTag("AutoDefenseShip"))
                // {
                //     //we are always in auto-defend mode, since this is a Sphere flagship or an auto-defense ship
                // }
                // else if ( World_AIW2.Instance.Setup.GetStringBySetting("ArmadaAutoDefend") == "Disabled" )
                //           //|| this.AttachedFaction.UnderPlayerControl())
                //     continue; //auto defend is explicitly disabled, or the player is controlling
                TiberiumPerUnitBaseInfo unitData = defenseShip.TryGetExternalBaseInfoAs<TiberiumPerUnitBaseInfo>();
                GameEntity_Squad centerpiece = unitData.HomeVein.GetSquad();

                Planet dest = defenseShip.GetDestinationPlanet();
                bool goToHomeVein = false;
                debugCode = 1500;
                int myStrength;
                myStrength = defenseShip.GetStrengthPerSquad();

                debugCode = 1400;

                bool debug = false;

                if ( debug )
                    ArcenDebugging.ArcenDebugLogSingleLine("Doing LRP update for " + defenseShip.ToStringWithPlanet(), Verbosity.DoNotShow );

                debugCode = 1600;
                if (AutoDefendUtility.IsLosingBattle(defenseShip.Planet, myStrength, this.AttachedFaction ))
                {
                    debugCode = 1700;
                    if ( defenseShip.TypeData.AllowedHopsFromCenterpiece > -1 )
                        goToHomeVein = true;
                }
                GameEntity_Squad HomeVein = null;
                if ( unitData != null &&
                     defenseShip.TypeData.AllowedHopsFromCenterpiece > -1 )
                {
                    HomeVein = unitData.HomeVein.GetSquad();
                    if ( HomeVein.Planet.GetHopsTo( defenseShip.Planet ) > defenseShip.TypeData.AllowedHopsFromCenterpiece )
                        goToHomeVein = true;
                }
                debugCode = 1750;

                if (goToHomeVein && HomeVein != null )
                {
                    Planet starbasePlanet = HomeVein.Planet;
                    if (starbasePlanet != null)
                    {
                        if (debug)
                            ArcenDebugging.ArcenDebugLogSingleLine("\tHeading to " + starbasePlanet.Name + ", its home starbase", Verbosity.DoNotShow);

                        AutoDefendUtility.GoToPlanet(defenseShip, starbasePlanet, Context, pathingCacheData);
                    }
                }

                if ( defenseShip.GetIsCrippled() )
                    continue;
                debugCode = 2100;
                if ( AutoDefendUtility.IsPlanetUnderAttack(dest, this.AttachedFaction) )
                {
                    debugCode = 2200;
                    if (debug)
                    {
                        if ( dest == defenseShip.Planet )
                            ArcenDebugging.ArcenDebugLogSingleLine("\tWe are already in a battle!", Verbosity.DoNotShow);
                        else
                            ArcenDebugging.ArcenDebugLogSingleLine("\tWe are en route to a battle at " + dest.Name, Verbosity.DoNotShow);
                    }

                    continue;
                }
                debugCode = 2300;
                //Check if we have any battles to go to!
                WorkingPlanetsLRP.Clear();
                if ( HomeVein != null ) 
                    AutoDefendUtility.GetPlanetsUnderAttack( defenseShip, myStrength, HomeVein.Planet, WorkingPlanetsLRP); //we are defending around a specific starbase; Guardians
                else
                    AutoDefendUtility.GetPlanetsUnderAttack(defenseShip, myStrength, defenseShip.Planet, WorkingPlanetsLRP); //we are "generic defense"; defensive flagships
                if ( debug )
                    ArcenDebugging.ArcenDebugLogSingleLine("\tWe have found " + WorkingPlanetsLRP.Count + " planets under attack", Verbosity.DoNotShow );
                debugCode = 2400;
                if ( WorkingPlanetsLRP.Count > 0 )
                {
                    debugCode = 2500;
                    //The target's priority is chosen in GetPlanetsUnderAttack (so technically we could just pass back a single planet), but we use a List for future-proofing
                    if ( debug )
                        ArcenDebugging.ArcenDebugLogSingleLine("\tWe are heading to " + WorkingPlanetsLRP[0].Name + " to help defend", Verbosity.DoNotShow );

                    AutoDefendUtility.GoToPlanet( defenseShip, WorkingPlanetsLRP[0], Context, pathingCacheData);
                    continue;
                }
                debugCode = 2600;
                //We haven't found an ongoing battle to assist in, so see if we have a threatened planet we can defend
                WorkingPlanetsLRP.Clear();
                if ( AutoDefendUtility.IsPlanetThreatened( dest, this.AttachedFaction ))
                {
                    debugCode = 2700;
                    if ( debug )
                        ArcenDebugging.ArcenDebugLogSingleLine("\tWe are already on a threatened planet!", Verbosity.DoNotShow );
                    continue;
                }
                AutoDefendUtility.GetThreatenedPlanets( myStrength, defenseShip.Planet, WorkingPlanetsLRP, this.AttachedFaction);
                if ( debug )
                    ArcenDebugging.ArcenDebugLogSingleLine("\tWe have found " + WorkingPlanetsLRP.Count + " planets threatened", Verbosity.DoNotShow );
                debugCode = 2800;
                if ( WorkingPlanetsLRP.Count > 0 )
                {
                    Planet threatenedPlanet = WorkingPlanetsLRP[Context.RandomToUse.Next(0, WorkingPlanetsLRP.Count)];
                    if ( debug )
                        ArcenDebugging.ArcenDebugLogSingleLine("\tWe are heading to " + threatenedPlanet.Name + " because the planet is threatened", Verbosity.DoNotShow );
                    
                    AutoDefendUtility.GoToPlanet( defenseShip, threatenedPlanet, Context, pathingCacheData);
                    continue;
                }
                debugCode = 2900;
                if ( dest != defenseShip.Planet )
                    continue; //we don't have any more urgent objectives, and we are en route someplace
                if ( dest == defenseShip.Planet )
                {
                    if ( defenseShip.HasQueuedOrders() )
                        continue;
                    debugCode = 3000;
                    //Patrolling: head to a randomly chosen other shipyard, or someplace on this planet
                    if ( Context.RandomToUse.Next(0, 100) < 20 )
                    {
                        if ( debug )
                            ArcenDebugging.ArcenDebugLogSingleLine("\tWe are heading to part of of " + defenseShip.Planet.Name + " to patrol", Verbosity.DoNotShow );
                        FInt minRadius = FInt.FromParts( 0, 100 );
                        FInt maxRadius = FInt.FromParts( 0, 900 );

                        ArcenPoint patrolPoint = defenseShip.Planet.GetSafePlacementPoint_AroundDesiredPointVicinity( Context, defenseShip.TypeData, Engine_AIW2.Instance.CombatCenter, minRadius, maxRadius );
                        GoToLocation(defenseShip, patrolPoint, Context, pathingCacheData);
                        continue;
                    }
                }
                if ( debug )
                    ArcenDebugging.ArcenDebugLogSingleLine("\tNothing to do?", Verbosity.DoNotShow );
            }
            } catch(Exception e )
            {
                ArcenDebugging.LogSingleLine("Exception in tiberium LRP debugCode " + debugCode + " " + e.ToString(), Verbosity.DoNotShow );
            }
            pathingCacheData.ReturnToPool();
        }
        
        public override void SeedStartingEntities_LaterEverythingElse( Galaxy galaxy, ArcenHostOnlySimContext Context, MapTypeData mapType )
        {
            Tutorial tutorialData = World_AIW2.Instance.TutorialOrNull;
            if ( tutorialData != null ) 
                return;
            //This happens with beacons and we want to use the logic in JoinAlliesIfNecessary() to handle that case
            if ( World_AIW2.Instance.GameSecond > 1 )
                return;
            foreach ( GameEntity_Squad e in World_AIW2.Instance.Squads( EntityRollupType.KingUnitsOnly ) )
            {
                if ( !e.GetIsFriendlyTowards_Safe( AttachedFaction ) )
                    continue;

                var squad = e.Planet.Mapgen_SeedEntity( Context, AttachedFaction, "TiberiumVein", PlanetSeedingZone.MostAnywhere );
                var data = squad.CreateExternalBaseInfo<TiberiumPerUnitBaseInfo>( "TiberiumPerUnitBaseInfo" );
                data.Points = 100;
                squad.SetCurrentMarkLevel((byte)3);
            }
            int startingVeins = 7;
            if ( World_AIW2.Instance.CampaignType.HarshnessRating >= 500 )
                startingVeins += 6;
            StandardMapPopulator.Mapgen_SeedSpecialEntities( Context, galaxy, AttachedFaction, SpecialEntityType.None, "TiberiumVein", SeedingType.HardcodedCount, startingVeins,
                MapGenCountPerPlanet.One, MapGenSeedStyle.SmallBad, 2, 2, PlanetSeedingZone.MostAnywhere, SeedingExpansionType.ComplicatedOriginal );

            int superCloseVein = 1;
            var args = new SeedArgs()
            {
                FactionOrNull = AttachedFaction,
                TagOrEmpty = "TiberiumVein",
                Count = superCloseVein,
                SeedStyle = MapGenSeedStyle.NoChecks,
                MinDistanceFromHumanHomeworld = 1,
                MaxDistanceFromHumanHomeworld = 2,
                SeedingZone = PlanetSeedingZone.MostAnywhere,
                ExpansionStyle = SeedingExpansionType.ComplicatedOriginal,
                DisableDistanceRescaling = true,
            };
            StandardMapPopulator.Mapgen_SeedSpecialEntities( Context, galaxy, args );
            int nearbyVeins = 4;
            args = new SeedArgs()
            {
                FactionOrNull = AttachedFaction,
                TagOrEmpty = "TiberiumVein",
                Count = nearbyVeins,
                SeedStyle = MapGenSeedStyle.NoChecks,
                MinDistanceFromHumanHomeworld = 2,
                MaxDistanceFromHumanHomeworld = 5,
                SeedingZone = PlanetSeedingZone.MostAnywhere,
                ExpansionStyle = SeedingExpansionType.ComplicatedOriginal,
                DisableDistanceRescaling = true,
            };
            StandardMapPopulator.Mapgen_SeedSpecialEntities( Context, galaxy, args );

        }
    }
}
