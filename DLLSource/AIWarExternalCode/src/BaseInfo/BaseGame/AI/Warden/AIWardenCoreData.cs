using Arcen.AIW2.Core;
using Arcen.Universal;
using System;

using System.Text;

namespace Arcen.AIW2.External
{
    public class AIWardenCoreData : AISubFactionCoreDataRoot
    {
        protected override string TracingNameRoot => "WardenCore";
        protected override bool IsHunter => false;
        protected override bool IsWarden => true;
        protected override bool IsPraetorian => false;
        protected override AIBudgetType MyAIBudgetType => AIBudgetType.Warden;
        public override Faction MySpecificSubfaction => BaseInfoOfParentSentinels.SubFac_Warden;
        public override bool ShouldHuntEnemyKingUnits => this.SubType.ShouldHuntEnemyKingUnits;
        public override bool ShouldCountAsThreat => this.SubType.ShouldCountAsThreat;
        public override bool CanWaitForReinforcements => this.AIDifficulty.CanWaitForReinforcements;
        public override bool AllowEntryIntoHostileTerritory => this.SubType.AllowEntryIntoHostileTerritory;
        public override bool MustCampOnNinjaBases => this.SubType.MustCampOnNinjaBases; //only matters on the wardens
        public override bool DistanceFromKingRestricted => false; //only matters on Praetorian
        public override FInt HostileRatio_TooDangerousToStay => this.SubType.HostileRatio_TooDangerousToStay;
        public override FInt HostileRatio_TooDangerousToTarget => this.SubType.HostileRatio_TooDangerousToTarget;
        public override FInt HostileRatio_TooDangerousToGoThrough => this.SubType.HostileRatio_TooDangerousToGoThrough;

        public AIWardenTypeData SubType;
        public AIDifficulty_WardenFleet AIDifficulty;
        public bool HaveCheckedForInitialDonation;
        public readonly ArcenLessLinkedList<Fireteam> Teams = ArcenLessLinkedList<Fireteam>.Create_WillNeverBeGCed( "AIWardenCoreData-Teams" );
        public readonly EnumIndexedArray<AIWardenBudgetType,FInt> CurrentBudgetRatios = EnumIndexedArray<AIWardenBudgetType,FInt>.Create_WillNeverBeGCed( true, FInt.Zero, "AIWardenCoreData-CurrentBudgetRatios" );
        private readonly EnumIndexedArray<AIWardenBudgetType,FInt> StoredAIPurchaseCostByBudgetByBudget = EnumIndexedArray<AIWardenBudgetType,FInt>.Create_WillNeverBeGCed( true, FInt.Zero, "AIWardenCoreData-StoredAIPurchaseCostByBudgetByBudget" );
        
        public AIWardenCoreData( AISentinelsFactionBaseInfo BaseInfo ) : base( BaseInfo ) { }

        protected override void SubCleanup()
        {
            this.SubType = null;
            this.AIDifficulty = null;
            this.HaveCheckedForInitialDonation = false;
            this.Teams.Clear();
            this.CurrentBudgetRatios.Clear();
            this.StoredAIPurchaseCostByBudgetByBudget.Clear();
        }

        #region Ser / Deser
        protected override void SubSerializeTo( SerMetaData MetaData, ArcenSerializationBuffer Buffer, SerializationCommandType SerializationCmdType )
        {
            Buffer.WriteHeaderStringToLogIfLoggingActive( "AIWardenData" );
            if (!SerializationCmdType.GetIsNetworkType())
            {
                Buffer.AddBool( MetaData, this.HaveCheckedForInitialDonation, "HaveCheckedForInitialDonation" );
                Buffer.AddByte(MetaData, ReadStyleByte.Normal, (byte)AIWardenBudgetType.Length, "AIWardenBudgetTypeCount");
                for (AIWardenBudgetType i = 0; i < AIWardenBudgetType.Length; i++)
                    Buffer.AddFInt(MetaData, this.CurrentBudgetRatios[i], "AIWardenBudgetTypeRatio");
                Buffer.AddByte(MetaData, ReadStyleByte.Normal, (byte)AIWardenBudgetType.Length, "AIWardenBudgetTypeCount");
                for (AIWardenBudgetType i = 0; i < AIWardenBudgetType.Length; i++)
                    Buffer.AddFInt(MetaData, this.GetStoredAIPurchaseCost(i), "StoredAIPurchaseCost");
            }
            FireteamBaseUtility.SerializeFireteams( MetaData,  Buffer,  SerializationCmdType, this.Teams );
        }

        protected override void SubDeserializeInto( SerMetaData MetaData, ArcenDeserializationBuffer Buffer, SerializationCommandType SerializationCmdType )
        {
            Buffer.WriteHeaderStringToLogIfLoggingActive( "AIWardenData" );
            Buffer.ActivateOrAddTrackerByNameIfTracking( "AIWardenData Ext", TrackerStyle.ByTypeOnly );

            int countToExpect;
            if (!SerializationCmdType.GetIsNetworkType())
            {
                this.HaveCheckedForInitialDonation = Buffer.ReadBool( MetaData, "HaveCheckedForInitialDonation" );
                countToExpect = Buffer.ReadByte(MetaData, ReadStyleByte.Normal, "AIWardenBudgetTypeCount");
                for (int i = 0; i < countToExpect; i++)
                    this.CurrentBudgetRatios[(AIWardenBudgetType)i] = Buffer.ReadFInt(MetaData, "AIWardenBudgetTypeRatio");
                countToExpect = Buffer.ReadByte(MetaData, ReadStyleByte.Normal, "AIWardenBudgetTypeCount");
                for (int i = 0; i < countToExpect; i++)
                    this.AddStoredAIPurchaseCost((AIWardenBudgetType)i, Buffer.ReadFInt(MetaData, "StoredAIPurchaseCost"));
            }
            FireteamBaseUtility.DeserializeFireteamsAndDiscardAnyExtraLeftovers( MetaData, Buffer, SerializationCmdType, this.Teams, "warden" );
            Buffer.StopTrackerByName( "AIWardenData Ext" );
        }
        #endregion

        public override PlanetPathfinder GetPathfinderThatMustBeReleased()
        {
            return PlanetPathfinderWarden.Pool.GetFromPoolOrCreate( "AIWardenCoreData-PlanetPathfinderWarden", 30f );
        }

        public FInt GetStoredAIPurchaseCost( AIWardenBudgetType Budget )
        {
            return this.StoredAIPurchaseCostByBudgetByBudget[Budget];
        }

        public void AddStoredAIPurchaseCost( AIWardenBudgetType Budget, FInt AmountToAdd )
        {
            this.StoredAIPurchaseCostByBudgetByBudget[Budget] += AmountToAdd;
        }

        #region ReceiveDonation
        public FInt ReceiveDonation( FInt StrengthBeingDonated, Faction FromFaction, 
            FireteamRequiredTarget RequiredTargetOrNull ) //RequiredTargetOrNull is ignored for now
        {
            Faction myFaction = MySpecificSubfaction;
            if ( myFaction == null )
                return FInt.Zero;

            #region Tracing
            bool tracing = this.tracing_shortTerm = Engine_AIW2.TraceAtAll && Engine_AIW2.TracingFlags.Has( ArcenTracingFlags.Warden );
            ArcenCharacterBuffer tracingBuffer = tracing ? ArcenCharacterBuffer.GetFromPoolOrCreate( "Warden-ReceiveDonation-trace", 10f ) : null;
            if ( tracing ) tracingBuffer.Add( "WardenFleet ReceiveDonation trace begins for faction " ).Add( myFaction.GetDisplayName() ).Add( " (index " ).Add( myFaction.FactionIndex ).Add( ")" );
            if ( tracing ) tracingBuffer.Add( "\n" ).Add( "receiving " ).Add( StrengthBeingDonated.ReadableString ).Add( " from " ).Add( FromFaction.SpecialFactionData.InternalName ).Add( " (index " ).Add( FromFaction.FactionIndex ).Add( ")" );
            #endregion

            FInt totalStrengthAlreadySpawned = FInt.Zero;
            foreach ( Planet planet in World_AIW2.Instance.Planets( false ) )
            {
                PlanetFaction planetFaction = planet.GetPlanetFactionForFaction( myFaction );
                totalStrengthAlreadySpawned += planetFaction.DataByStance[FactionStance.Self].MobileStrength;
                if ( tracing && planetFaction.DataByStance[FactionStance.Self].MobileStrength > FInt.Zero ) tracingBuffer.Add( "\n" ).Add( "Warden Fleet strength on " + planet.Name + ": " ).Add( planetFaction.DataByStance[FactionStance.Self].MobileStrength );
            }

            FInt populationCap = this.GetPopulationCap(GlobalAIWorldBaseInfo.Instance.AIProgress_Effective);
            if ( totalStrengthAlreadySpawned > FInt.Zero && populationCap > FInt.Zero )
            {
                if ( totalStrengthAlreadySpawned >= populationCap )
                {
                    #region Tracing
                    if ( tracing ) tracingBuffer.Add( "\n" ).Add( "refusing donation because current SF strength (" ).Add( totalStrengthAlreadySpawned.ReadableString ).Add( ") is greater than the population cap of " ).Add( populationCap.ReadableString ).Add( ")" );
                    if ( tracing )
                    {
                        ArcenDebugging.ArcenDebugLogSingleLine( tracingBuffer.ToString(), Verbosity.DoNotShow );
                        tracingBuffer.ReturnToPool();
                        tracingBuffer = null;
                    }
                    #endregion
                    return StrengthBeingDonated; // refuse donation, we have plenty
                }
            }

            if ( World_AIW2.Instance.GameSecond > 300 ) //don't start beefing the warden for 5 minutes
            {
                FInt multiplierByPopCap = FInt.One;

                if ( totalStrengthAlreadySpawned < populationCap / 100 )
                    multiplierByPopCap = (FInt)10;
                else if ( totalStrengthAlreadySpawned < populationCap / 50 )
                    multiplierByPopCap = (FInt)8;
                else if ( totalStrengthAlreadySpawned < populationCap / 25 )
                    multiplierByPopCap = (FInt)7;
                else if ( totalStrengthAlreadySpawned < populationCap / 15 )
                    multiplierByPopCap = (FInt)6;
                else if ( totalStrengthAlreadySpawned < populationCap / 10 )
                    multiplierByPopCap = (FInt)5;
                else if ( totalStrengthAlreadySpawned < populationCap / 8 )
                    multiplierByPopCap = (FInt)3;
                else if ( totalStrengthAlreadySpawned < populationCap / 6 )
                    multiplierByPopCap = FInt.FromParts( 2, 250 );
                else if ( totalStrengthAlreadySpawned < populationCap / 4 )
                    multiplierByPopCap = FInt.FromParts( 1, 600 );
                else if ( totalStrengthAlreadySpawned < populationCap / 3 )
                    multiplierByPopCap = FInt.FromParts( 1, 250 );
                else if ( totalStrengthAlreadySpawned < populationCap / 2 )
                    multiplierByPopCap = FInt.FromParts( 1, 100 );

                if ( World_AIW2.Instance.GameSecond < 600 ) //if less than 10 minutes, make it much less severe
                    multiplierByPopCap = ((multiplierByPopCap - FInt.One) / 4) + FInt.One;
                else if ( World_AIW2.Instance.GameSecond < 1200 ) //if less than 20 minutes, make it a bit less severe
                    multiplierByPopCap = ((multiplierByPopCap - FInt.One) / 2) + FInt.One;

                StrengthBeingDonated *= multiplierByPopCap;
            }

            #region Tracing
            if ( tracing ) tracingBuffer.Add( "\n" ).Add( "accepting donation because current SF strength (" ).Add( totalStrengthAlreadySpawned.ReadableString ).Add( ") is less than the population cap of " ).Add( populationCap.ReadableString );
            #endregion
            
            this.SetSpendingRatios();
            
            for ( AIWardenBudgetType i = AIWardenBudgetType.None + 1; i < AIWardenBudgetType.Length; i++ )
            {
                FInt ratio = this.CurrentBudgetRatios[i];
                FInt strengthForThis = StrengthBeingDonated * ratio;
                
                #region Tracing
                if ( tracing ) tracingBuffer.Add( "\n" ).Add( "allocating x" ).Add( ratio.ReadableString ).Add( " (" ).Add( strengthForThis.ReadableString ).Add( ") to " ).Add( i.ToString() ).Add( " (previously  " ).Add( this.GetStoredAIPurchaseCost( i ).ReadableString );
                #endregion
                
                this.AddStoredAIPurchaseCost( i, strengthForThis );
                
                #region Tracing
                if ( tracing ) tracingBuffer.Add( ", now " ).Add( this.GetStoredAIPurchaseCost( i ).ReadableString ).Add( ")" );
                #endregion
            }
            
            #region Tracing
            if ( tracing ) tracingBuffer.Add( "\n" ).Add( "Warden Fleet ReceiveDonation trace ends" );
            if ( tracing )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( tracingBuffer.ToString(), Verbosity.DoNotShow );
                tracingBuffer.ReturnToPool();
                tracingBuffer = null;
            }
            #endregion
            
            return FInt.Zero;
        }
        #endregion end ReceiveDonation

        #region GetPopulationCap 
        public FInt GetPopulationCap(FInt Aip)
        {
            FInt populationCap = (FInt)this.AIDifficulty.GetStrengthCapAtAIP( Aip.GetNearestIntPreferringHigher() );

            if (this.AIDifficulty.CapScalesWithMarkLevel)
            {
                var row = MarkLevelScaleStyleTable.Instance.GetRowByName("Original");

                var aipformark = this.BaseInfoOfParentSentinels.SentinelInfo.AIDifficulty.AIPForMarkLevel;
                int atMarkLevel = 0;
                for (int i = 0; i < aipformark.Count; i++)
                {
                    if (aipformark[i] <= Aip)
                        atMarkLevel = i;
                }

                populationCap *= row.AttackMultipliers[atMarkLevel];
            }
            return populationCap;
        }
        #endregion

        #region SetSpendingRatios
        private void SetSpendingRatios()
        {
            FInt AIP = FactionUtilityMethods.Instance.GetCurrentAIP();
            if ( this.AIDifficulty.CanUseTopTierUnits && AIP >= this.AIDifficulty.AIPForTopTierUnlock )
            {
                this.CurrentBudgetRatios[AIWardenBudgetType.TopTier] = FInt.FromParts( 0, 150 );
                this.CurrentBudgetRatios[AIWardenBudgetType.NonForcefieldGuardians] = FInt.FromParts( 0, 200 );
                this.CurrentBudgetRatios[AIWardenBudgetType.ForcefieldGuardians] = FInt.FromParts( 0, 090 );
                this.CurrentBudgetRatios[AIWardenBudgetType.Normal] = FInt.FromParts( 0, 350 );
                this.CurrentBudgetRatios[AIWardenBudgetType.Decloakers] = FInt.FromParts( 0, 010 );
                this.CurrentBudgetRatios[AIWardenBudgetType.Bruisers] = FInt.FromParts( 0, 200 );
            }
            else
            {
                this.CurrentBudgetRatios[AIWardenBudgetType.NonForcefieldGuardians] = FInt.FromParts( 0, 250 );
                this.CurrentBudgetRatios[AIWardenBudgetType.ForcefieldGuardians] = FInt.FromParts( 0, 050 );
                this.CurrentBudgetRatios[AIWardenBudgetType.Normal] = FInt.FromParts( 0, 400 );
                this.CurrentBudgetRatios[AIWardenBudgetType.TopTier] = FInt.FromParts( 0, 000 );
                this.CurrentBudgetRatios[AIWardenBudgetType.Decloakers] = FInt.FromParts( 0, 010 );
                this.CurrentBudgetRatios[AIWardenBudgetType.Bruisers] = FInt.FromParts( 0, 290 );
            }
        }
        #endregion

        public override FInt GetOverconfidenceRatio()
        {
            return this.AIDifficulty.OverconfidenceRatio;
        }

        public override Int16 GetNeverCampCloserThanXHopsToHostileTerritory()
        {
            return this.AIDifficulty.NeverCampCloserThanXHopsToHostileTerritory;
        }

        public override string GetBudgetName( int BudgetAsInt )
        {
            return ((AIWardenBudgetType)BudgetAsInt).ToString();
        }

        #region DoBudgetLoop_OnMainThreadAndPartOfSim
        protected override void DoBudgetLoop_OnMainThreadAndPartOfSim( ArcenHostOnlySimContext Context )
        {
            bool tracing = this.tracing_shortTerm = Engine_AIW2.TraceAtAll && Engine_AIW2.TracingFlags.Has( ArcenTracingFlags.Warden );
            ArcenCharacterBuffer tracingBuffer = tracing ? ArcenCharacterBuffer.GetFromPoolOrCreate( "Warden-DoBudgetLoop_OnMainThreadAndPartOfSim-trace", 10f ) : null;
            for ( AIWardenBudgetType budget = AIWardenBudgetType.None + 1; budget < AIWardenBudgetType.Length; budget++ )
            {
                FInt strength = this.GetStoredAIPurchaseCost( budget );
                bool noUnitsAvailable = false;
                FInt budgetSpent = this.DoSingleBudgetLoopIteration_OnMainThreadAndPartOfSim( Context, strength, (int)budget, out noUnitsAvailable );
                if ( noUnitsAvailable )
                {
                    //if there are no units, just wast half the budget
                    #region Tracing
                    if ( tracing ) tracingBuffer.Add( "\n" ).Add( "Budget " ).Add( this.GetBudgetName( (int)budget ) ).Add( " has no units it can buy, so just lose " ).Add( (strength / 2).ReadableString ).Add( " from this budget" );
                    #endregion

                    budgetSpent += (strength / 2);
                }
                #region Tracing
                if ( tracing ) tracingBuffer.Add( "\n" ).Add( "Budget " ).Add( this.GetBudgetName( (int)budget ) ).Add( " Being updated by  -" ).Add( budgetSpent.ReadableString );
                #endregion

                this.AddStoredAIPurchaseCost( budget, -budgetSpent );
            }
            if ( tracing )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( tracingBuffer.ToString(), Verbosity.DoNotShow );
                tracingBuffer.ReturnToPool();
                tracingBuffer = null;
            }
        }
        #endregion

        #region GetBudgetAIShipGroupCategory
        public override void GetBudgetAIShipGroupCategory( int budgetTypeAsInt, AIBudgetItem budgetItem, ArcenHostOnlySimContext Context, ref DrawBag<GameEntityTypeData> bagToFill )
        {
            AIShipGroupCategory category = null;
            switch ( (AIWardenBudgetType)budgetTypeAsInt )
            {
                case AIWardenBudgetType.NonForcefieldGuardians:
                    category = budgetItem.GuardianAIShipGroup;
                    break;
                case AIWardenBudgetType.TopTier:
                    category = budgetItem.DireGuardianAIShipGroup;
                    break;
                case AIWardenBudgetType.ForcefieldGuardians:
                    category = budgetItem.ForcefieldGuardianAIShipGroup;
                    break;
                case AIWardenBudgetType.Normal:
                case AIWardenBudgetType.RapidResponse:
                case AIWardenBudgetType.Bruisers:
                    category = budgetItem.NormalAIShipGroup;
                    break;
                case AIWardenBudgetType.Decloakers:
                    category = budgetItem.DecloakerAIShipGroup;
                    break;
            }
            AIShipGroup group = category.DrawBag.PickRandomItemAndReplace( Context.RandomToUse );
            bagToFill.Clear();
            if ( group == null )
                return;
            bagToFill.CopyFrom( group.DrawBag );
        }
        #endregion

        #region Helper_DoTargetFindingSweep
        protected override void Helper_DoTargetFindingSweep( Planet primaryDefensePlanet, ArcenHostOnlySimContext Context, FInt fleetStrength, 
            ref Int16 BestTargetFound_Index, ref FInt BestTargetFound_Danger, DangerCheckMode dangerCheckMode )
        {
            if ( !this.MustCampOnNinjaBases )
            {
                //if we are not a "base oriented" warden, then we don't use this alternative logic
                base.Helper_DoTargetFindingSweep( primaryDefensePlanet, Context, fleetStrength,
                    ref BestTargetFound_Index, ref BestTargetFound_Danger, dangerCheckMode );
                return;
            }

            //If we ARE a base-oriented warden, then...
            //This version is exactly the same as the "regular" version, but it can't choose to have its forces camp at a planet without a warden fleet base
            Faction myFaction = MySpecificSubfaction;

            #region Tracing
            bool tracing = this.tracing_longTerm;
            ArcenCharacterBuffer tracingBuffer = tracing ? ArcenCharacterBuffer.GetFromPoolOrCreate( "Warden-Helper_DoTargetFindingSweep-trace", 10f ) : null;
            #endregion
            bool allowEntryIntoHostileTerritory = this.AllowEntryIntoHostileTerritory;
            Int16 neverCampCloserThanXHopsToHostileTerritory = this.GetNeverCampCloserThanXHopsToHostileTerritory();

            Int16 bestTargetFound_Index = BestTargetFound_Index;
            FInt bestTargetFound_Danger = BestTargetFound_Danger;

            foreach ( Planet.PlanetAtHopDistance _phd in primaryDefensePlanet.PlanetsWithinXHops( -1,
                delegate ( Planet planet )
                {
                    if ( !allowEntryIntoHostileTerritory && planet.GetIsEitherControllerOrInfluencerHostileTo( myFaction ) )
                    {
                        #region Tracing
                        if ( tracing ) tracingBuffer.Add( "\n" ).Add( "Refusing to flood into " ).Add( planet.Name ).Add( " because controlled by side hostile to me" );
                        #endregion
                        return PropogationEvaluation.No;
                    }
                    FInt danger = this.GetDanger( dangerCheckMode, planet );
                    if ( danger >= fleetStrength * this.HostileRatio_TooDangerousToTarget )
                    {
                        #region Tracing
                        if ( tracing ) tracingBuffer.Add( "\n" ).Add( "Refusing to flood into " ).Add( planet.Name ).Add( " because too much danger: " ).Add( danger.ReadableString ).Add( " >= " + fleetStrength + " * " + this.HostileRatio_TooDangerousToTarget + " == " + fleetStrength * this.HostileRatio_TooDangerousToTarget );
                        #endregion
                        return PropogationEvaluation.No;
                    }
                    if ( danger >= fleetStrength * this.HostileRatio_TooDangerousToGoThrough )
                    {
                        #region Tracing
                        if ( tracing ) tracingBuffer.Add( "\n" ).Add( "Refusing to flood through " ).Add( planet.Name ).Add( " because too much danger: " ).Add( danger.ReadableString );
                        #endregion
                        return PropogationEvaluation.SelfButNotNeighbors;
                    }
                    return PropogationEvaluation.Yes;
                } ) )
            {
                Planet planet = _phd.Planet;
                FInt friendlyStrengthTotal;
                FInt hostileStrengthActuallyOnPlanet;
                Int16 hopsFromHostileForThisPlanet = Helper_GetHopsFromHostileTerritory( planet, Context );
                Int16 hopsFromHostileForBestPlanet = Helper_GetHopsFromHostileTerritory(  World_AIW2.Instance.GetPlanetByIndex( bestTargetFound_Index ), Context );

                FInt danger = GetDanger( dangerCheckMode, planet, out friendlyStrengthTotal, out hostileStrengthActuallyOnPlanet );
                if ( dangerCheckMode == DangerCheckMode.PresentAndNearby && hostileStrengthActuallyOnPlanet <= 0 )
                {
                    #region Tracing
                    // commented out because extreme spam
                    //if ( tracing ) tracingBuffer.Add( "\n" ).Add( "rejecting target " ).Add( planet.Name ).Add( " because this is the first-try sweep and there is no hostile strength actually on planet" );
                    #endregion
                    continue;
                }
                if ( dangerCheckMode == DangerCheckMode.NearbyOnly && hostileStrengthActuallyOnPlanet > 0 )
                {
                    #region Tracing
                    if ( tracing ) tracingBuffer.Add( "\n" ).Add( "rejecting target " ).Add( planet.Name ).Add( " because this is the second-try sweep and there IS hostile strength actually on planet" );
                    #endregion
                    continue;
                }
                if ( bestTargetFound_Index >= 0 && bestTargetFound_Danger >= danger &&
                     hopsFromHostileForBestPlanet <= hopsFromHostileForThisPlanet )
                {
                    #region Tracing
                    // commented out because extreme spam
                    //if ( tracing ) tracingBuffer.Add( "\n" ).Add( "rejecting target " ).Add( planet.Name ).Add( " because danger lower than current best target: " ).Add( danger.ReadableString ).Add(", or this planet is further away from the action. danger check mode " ).Add( dangerCheckMode );
                    #endregion
                    continue;
                }
                if ( dangerCheckMode == DangerCheckMode.NearbyOnly &&
                     neverCampCloserThanXHopsToHostileTerritory > 0 &&
                     Helper_GetIsPlanetWithinXHopsOfHostileTerritory( planet, neverCampCloserThanXHopsToHostileTerritory ) )
                {
                    #region Tracing
                    if ( tracing ) tracingBuffer.Add( "\n" ).Add( "rejecting target " ).Add( planet.Name ).Add( " because is closer than " ).Add( neverCampCloserThanXHopsToHostileTerritory ).Add( " hops to hostile-owned planet" );
                    #endregion
                    continue;
                }
                if ( dangerCheckMode == DangerCheckMode.NearbyOnly && MustCampOnNinjaBases
                     && hostileStrengthActuallyOnPlanet == 0 )
                {
                    //if we are looking for camping spot, only pick ninja base planets without
                    //enemies
                    bool hasNinjaHideout = false;
                    PlanetFaction planetFaction = planet.GetPlanetFactionForFaction( myFaction );
                    foreach ( GameEntity_Squad entity in planetFaction.Entities.Squads( "WardenSecretNinjaHideout" ) )
                    {
                        hasNinjaHideout = true;
                        break;
                    }
                    if ( !hasNinjaHideout )
                    {
                        #region Tracing
                        if ( tracing ) tracingBuffer.Add( "\n" ).Add( "rejecting target " ).Add( planet.Name ).Add( " because it does not have a WardenSecretNinjaHideout and also has no enemies, so it's not a suitable place to camp " );
                        #endregion
                        continue;
                    }
                }

                #region Tracing
                if ( tracing ) tracingBuffer.Add( "\n" ).Add( "current best target = " ).Add( planet.Name ).Add( "; danger: " ).Add( danger.ReadableString ).Add( " danger check mode " ).Add( dangerCheckMode.ToString() ).Add( " (warden fleet override path)" );
                #endregion
                bestTargetFound_Index = planet.Index;
                bestTargetFound_Danger = danger;
            }
            BestTargetFound_Index = bestTargetFound_Index;
            BestTargetFound_Danger = bestTargetFound_Danger;

            if ( tracing )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( tracingBuffer.ToString(), Verbosity.DoNotShow );
                tracingBuffer.ReturnToPool();
                tracingBuffer = null;
            }
        }
        #endregion

        #region DoGameStartLogic
        public void DoGameStartLogic( ArcenHostOnlySimContext Context )
        {
            Faction myFaction = MySpecificSubfaction;
            
            //if we are going to auto-kite, then we need to set ourselves to do so!
            myFaction.UnitsAutoKite_NeverSetDirectlyOrItBreaksEverything = this.SubType.AutoKite;

            if ( !this.MustCampOnNinjaBases )
            {
                //if we are not a "base oriented" warden, then we don't need to seed any warden bases for ourselves
                return;
            }

            //If we ARE a base-oriented warden, then we need to actually seed some secret ninja hideouts for ourselves!

            Tutorial tutorialData = World_AIW2.Instance.TutorialOrNull;

            int numToSeed = World_AIW2.Instance.CurrentGalaxy.GetCountOfNonDestroyedPlanets() / 25;
            if ( numToSeed < 4 )
                numToSeed = 4;
            if ( numToSeed > 12 )
                numToSeed = 12;
            if ( tutorialData == null || !tutorialData.SkipWardenBases )
            {
                MapgenDeepLinkRoot.Instance.SeedWardenSecretNinjaHideouts( Context, myFaction, numToSeed );

                foreach ( GameEntity_Squad e in World_AIW2.Instance.Squads( EntityRollupType.KingUnitsOnly ) )
                {
                    if ( e.GetFactionTypeSafe() == FactionType.AI &&
                         (e.GetFactionIndex_Safe() == myFaction.FactionIndexOfMyParentIfIHaveOne) )
                    {
                        var entityData = GameEntityTypeDataTable.Instance.GetRandomRowWithTag( Context, "WardenSecretNinjaHideout" );
                        e.Planet.Mapgen_SeedEntity( Context, myFaction, entityData, PlanetSeedingZone.InnerSystem );
                    }
                }
            }
        }
        #endregion
    }
}
