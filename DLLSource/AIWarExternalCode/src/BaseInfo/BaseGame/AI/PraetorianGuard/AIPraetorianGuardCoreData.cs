using Arcen.AIW2.Core;
using Arcen.Universal;
using System;

using System.Text;

namespace Arcen.AIW2.External
{
    public class AIPraetorianGuardCoreData : AISubFactionCoreDataRoot
    {
        protected override string TracingNameRoot => "PraetorianCore";
        protected override bool IsHunter => false;
        protected override bool IsWarden => false;
        protected override bool IsPraetorian => true;
        protected override AIBudgetType MyAIBudgetType => AIBudgetType.PraetorianGuard;
        public override Faction MySpecificSubfaction => BaseInfoOfParentSentinels.SubFac_Praetorian;
        public override bool ShouldHuntEnemyKingUnits => this.SubType.ShouldHuntEnemyKingUnits;
        public override bool ShouldCountAsThreat => this.SubType.ShouldCountAsThreat;
        public override bool CanWaitForReinforcements => true; //Praetorian always stays smart on this item
        public override bool AllowEntryIntoHostileTerritory => this.SubType.AllowEntryIntoHostileTerritory;
        public override bool MustCampOnNinjaBases => false; //this only matters for the warden
        public override bool DistanceFromKingRestricted => this.SubType.DistanceFromKingRestricted; //only matters on Praetorian
        public override FInt HostileRatio_TooDangerousToStay => this.SubType.HostileRatio_TooDangerousToStay;
        public override FInt HostileRatio_TooDangerousToTarget => this.SubType.HostileRatio_TooDangerousToTarget;
        public override FInt HostileRatio_TooDangerousToGoThrough => this.SubType.HostileRatio_TooDangerousToGoThrough;

        public PraetorianGuardTypeData SubType;
        public AIDifficulty_PraetorianGuard AIDifficulty;
        public bool HaveCheckedForInitialDonation;
        public readonly EnumIndexedArray<PraetorianGuardBudgetType,FInt> CurrentBudgetRatios = EnumIndexedArray<PraetorianGuardBudgetType,FInt>.Create_WillNeverBeGCed( true, FInt.Zero, "AIPraetorianGuardCoreData-CurrentBudgetRatios" );
        private readonly EnumIndexedArray<PraetorianGuardBudgetType,FInt> StoredAIPurchaseCostByBudgetByBudget = EnumIndexedArray<PraetorianGuardBudgetType,FInt>.Create_WillNeverBeGCed( true, FInt.Zero, "AIPraetorianGuardCoreData-StoredAIPurchaseCostByBudgetByBudget" );

        public AIPraetorianGuardCoreData( AISentinelsFactionBaseInfo BaseInfo ) : base( BaseInfo ) { }

        protected override void SubCleanup()
        {
            this.SubType = null;
            this.AIDifficulty = null;
            this.HaveCheckedForInitialDonation = false;
            this.CurrentBudgetRatios.Clear();
            this.StoredAIPurchaseCostByBudgetByBudget.Clear();
        }

        #region Ser / Deser
        protected override void SubSerializeTo( SerMetaData MetaData, ArcenSerializationBuffer Buffer, SerializationCommandType SerializationCmdType )
        {
            Buffer.WriteHeaderStringToLogIfLoggingActive( "PraetorianGuardData" );
            PraetorianGuardTypeDataTable.Instance.SerializeByInternalName( MetaData, this.SubType, Buffer, "PraetorianGuardType" );
            AIDifficulty_PraetorianGuardTable.Instance.SerializeByInternalName( MetaData, this.AIDifficulty, Buffer, "PraetorianGuardDifficulty" );
            Buffer.AddBool( MetaData, this.HaveCheckedForInitialDonation, "HaveCheckedForInitialDonation" );
            Buffer.AddByte( MetaData, ReadStyleByte.Normal, (byte)PraetorianGuardBudgetType.Length, "PraetorianGuardBudgetTypeCount" );
            for ( PraetorianGuardBudgetType i = 0; i < PraetorianGuardBudgetType.Length; i++ )
                Buffer.AddFInt( MetaData, this.CurrentBudgetRatios[i], "PraetorianGuardBudgetTypeCurrentBudgetRatios" );
            Buffer.AddByte( MetaData, ReadStyleByte.Normal, (byte)PraetorianGuardBudgetType.Length, "PraetorianGuardBudgetTypeCount" );
            for ( PraetorianGuardBudgetType i = 0; i < PraetorianGuardBudgetType.Length; i++ )
                Buffer.AddFInt( MetaData, this.GetStoredAIPurchaseCost( i ), "PraetorianGuardBudgetTypeStoredAIPurchaseCost" );
        }

        protected override void SubDeserializeInto( SerMetaData MetaData, ArcenDeserializationBuffer Buffer, SerializationCommandType SerializationCmdType )
        {
            Buffer.WriteHeaderStringToLogIfLoggingActive( "PraetorianGuardData" );
            Buffer.ActivateOrAddTrackerByNameIfTracking( "PraetorianGuardData Ext", TrackerStyle.ByTypeOnly );
            this.SubType = PraetorianGuardTypeDataTable.Instance.DeserializeByInternalName( MetaData, Buffer, "PraetorianGuardType" );
            this.AIDifficulty = AIDifficulty_PraetorianGuardTable.Instance.DeserializeByInternalName( MetaData, Buffer, "PraetorianGuardDifficulty" );
            this.HaveCheckedForInitialDonation = Buffer.ReadBool( MetaData, "HaveCheckedForInitialDonation" );

            int countToExpect;

            countToExpect = Buffer.ReadByte( MetaData, ReadStyleByte.Normal, "PraetorianGuardBudgetTypeCount" );
            for ( int i = 0; i < countToExpect; i++ )
                this.CurrentBudgetRatios[(PraetorianGuardBudgetType)i] = Buffer.ReadFInt( MetaData, "PraetorianGuardBudgetTypeCurrentBudgetRatios" );
            countToExpect = Buffer.ReadByte( MetaData, ReadStyleByte.Normal, "PraetorianGuardBudgetTypeCount" );
            for ( int i = 0; i < countToExpect; i++ )
                this.AddStoredAIPurchaseCost( (PraetorianGuardBudgetType)i, Buffer.ReadFInt( MetaData, "PraetorianGuardBudgetTypeStoredAIPurchaseCost" ) );
            Buffer.StopTrackerByName( "PraetorianGuardData Ext" );
        }
        #endregion

        public override PlanetPathfinder GetPathfinderThatMustBeReleased()
        {
            return PlanetPathfinderWarden.Pool.GetFromPoolOrCreate( "AIPraetorianGuardCoreData-PlanetPathfinderWarden", 30f );
        }

        public FInt GetStoredAIPurchaseCost( PraetorianGuardBudgetType Budget )
        {
            return this.StoredAIPurchaseCostByBudgetByBudget[Budget];
        }

        public void AddStoredAIPurchaseCost( PraetorianGuardBudgetType Budget, FInt AmountToAdd )
        {
            this.StoredAIPurchaseCostByBudgetByBudget[Budget] += AmountToAdd;
        }

        #region ReceiveDonation
        public FInt ReceiveDonation( FInt Strength, Faction FromFaction, 
            FireteamRequiredTarget RequiredTargetOrNull ) //required target is ignored for now
        {
            if ( World_AIW2.Instance.GetIsTutorial() )
                return FInt.Zero;

            Faction myFaction = MySpecificSubfaction;
            if ( myFaction == null )
                return FInt.Zero;

            #region Tracing
            bool tracing = this.tracing_shortTerm = Engine_AIW2.TraceAtAll && Engine_AIW2.TracingFlags.Has( ArcenTracingFlags.Independents );
            ArcenCharacterBuffer tracingBuffer = tracing ? ArcenCharacterBuffer.GetFromPoolOrCreate( "Pratorian-ReceiveDonation-trace", 10f ) : null;
            if ( tracing ) tracingBuffer.Add( "PraetorianGuard ReceiveDonation trace begins for faction " ).Add( myFaction.SpecialFactionData.InternalName ).Add( " (index " ).Add( myFaction.FactionIndex ).Add( ")" );
            if ( tracing ) tracingBuffer.Add( "\n" ).Add( "receiving " ).Add( Strength.ReadableString ).Add( " from " ).Add( FromFaction.SpecialFactionData.InternalName ).Add( " (index " ).Add( FromFaction.FactionIndex ).Add( ")" );
            #endregion
            FInt totalStrengthAlreadySpawned = FInt.Zero;
            foreach ( Planet planet in World_AIW2.Instance.Planets( false ) )
            {
                PlanetFaction planetFaction = planet.GetPlanetFactionForFaction( myFaction );
                totalStrengthAlreadySpawned += planetFaction.DataByStance[FactionStance.Self].MobileStrength;
                if ( tracing && planetFaction.DataByStance[FactionStance.Self].MobileStrength > FInt.Zero ) tracingBuffer.Add( "\n" ).Add( "Praetorian Guard strength on " + planet.Name + ": " ).Add( planetFaction.DataByStance[FactionStance.Self].MobileStrength );
            }

            FInt populationCap = this.GetPopulationCap();
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
                    return Strength; // refuse donation, we have plenty
                }
            }
            #region Tracing
            if ( tracing ) tracingBuffer.Add( "\n" ).Add( "accepting donation because current SF strength (" ).Add( totalStrengthAlreadySpawned.ReadableString ).Add( ") is less than the population cap of " ).Add( populationCap.ReadableString );
            #endregion
            this.SetSpendingRatios();
            for ( PraetorianGuardBudgetType i = PraetorianGuardBudgetType.None + 1; i < PraetorianGuardBudgetType.Length; i++ )
            {
                FInt ratio = this.CurrentBudgetRatios[i];
                FInt strengthForThis = Strength * ratio;
                #region Tracing
                if ( tracing ) tracingBuffer.Add( "\n" ).Add( "allocating x" ).Add( ratio.ReadableString ).Add( " (" ).Add( strengthForThis.ReadableString ).Add( ") to " ).Add( i.ToString() ).Add( " (previously  " ).Add( this.GetStoredAIPurchaseCost( i ).ReadableString );
                #endregion
                this.AddStoredAIPurchaseCost( i, strengthForThis );
                #region Tracing
                if ( tracing ) tracingBuffer.Add( ", now " ).Add( this.GetStoredAIPurchaseCost( i ).ReadableString ).Add( ")" );
                #endregion
            }
            #region Tracing
            if ( tracing ) tracingBuffer.Add( "\n" ).Add( "Praetorian Guard ReceiveDonation trace ends" );
            if ( tracing )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( tracingBuffer.ToString(), Verbosity.DoNotShow );
                tracingBuffer.ReturnToPool();
                tracingBuffer = null;
            }
            #endregion
            return FInt.Zero;
        }
        #endregion

        #region GetPopulationCap
        public FInt GetPopulationCap()
        {
            if ( World_AIW2.Instance.GetIsTutorial() )
                return FInt.Zero;
            FInt effectiveAIP = GlobalAIWorldBaseInfo.Instance.AIProgress_Effective;
            FInt populationCap = (FInt)this.AIDifficulty.GetStrengthCapAtAIP( effectiveAIP.GetNearestIntPreferringHigher() );
            AITypeData aiType = this.BaseInfoOfParentSentinels.SentinelInfo.AIType;
            populationCap *= aiType.PraetorianPopulationCapMultiplier;

            if (this.AIDifficulty.CapScalesWithMarkLevel)
            {
                var row = MarkLevelScaleStyleTable.Instance.GetRowByName("Original");
                populationCap *= row.AttackMultipliers[this.BaseInfoOfParentSentinels.AttachedFaction.CurrentGeneralMarkLevel_Base];
            }

            return populationCap;
        }
        #endregion

        #region SetSpendingRatios
        public void SetSpendingRatios()
        {
            if ( World_AIW2.Instance.GetIsTutorial() )
                return;
            FInt AIP = FactionUtilityMethods.Instance.GetCurrentAIP();
            if ( this.AIDifficulty.CanUseTopTierUnits && AIP >= this.AIDifficulty.AIPForTopTierUnlock )
            {
                this.CurrentBudgetRatios[PraetorianGuardBudgetType.Extragalactic] = FInt.FromParts( 0, 000 );
                this.CurrentBudgetRatios[PraetorianGuardBudgetType.TopTier] = FInt.FromParts( 0, 150 );
                this.CurrentBudgetRatios[PraetorianGuardBudgetType.NonForcefieldGuardians] = FInt.FromParts( 0, 200 );
                this.CurrentBudgetRatios[PraetorianGuardBudgetType.ForcefieldGuardians] = FInt.FromParts( 0, 050 );
                this.CurrentBudgetRatios[PraetorianGuardBudgetType.Normal] = FInt.FromParts( 0, 200 );
                this.CurrentBudgetRatios[PraetorianGuardBudgetType.RapidResponse] = FInt.FromParts( 0, 200 );
                this.CurrentBudgetRatios[PraetorianGuardBudgetType.Bruisers] = FInt.FromParts( 0, 200 );
            }
            else
            {
                this.CurrentBudgetRatios[PraetorianGuardBudgetType.Extragalactic] = FInt.FromParts( 0, 000 );
                this.CurrentBudgetRatios[PraetorianGuardBudgetType.NonForcefieldGuardians] = FInt.FromParts( 0, 250 );
                this.CurrentBudgetRatios[PraetorianGuardBudgetType.ForcefieldGuardians] = FInt.FromParts( 0, 050 );
                this.CurrentBudgetRatios[PraetorianGuardBudgetType.Normal] = FInt.FromParts( 0, 250 );
                this.CurrentBudgetRatios[PraetorianGuardBudgetType.TopTier] = FInt.FromParts( 0, 000 );
                this.CurrentBudgetRatios[PraetorianGuardBudgetType.RapidResponse] = FInt.FromParts( 0, 200 );
                this.CurrentBudgetRatios[PraetorianGuardBudgetType.Bruisers] = FInt.FromParts( 0, 250 );
            }
        }
        #endregion

        public override FInt GetOverconfidenceRatio()
        {
            if ( World_AIW2.Instance.GetIsTutorial() )
                return FInt.One;
            return this.AIDifficulty.OverconfidenceRatio;
        }

        public override Int16 GetNeverCampCloserThanXHopsToHostileTerritory()
        {
            return 0; //that's what the original classes said, and there wasn't a dial for adjusting it
        }

        public override string GetBudgetName( int BudgetAsInt )
        {
            return ((HunterFleetBudgetType)BudgetAsInt).ToString();
        }

        #region DoBudgetLoop_OnMainThreadAndPartOfSim
        protected override void DoBudgetLoop_OnMainThreadAndPartOfSim( ArcenHostOnlySimContext Context )
        {
            if ( World_AIW2.Instance.GetIsTutorial() )
                return;
            bool tracing = this.tracing_shortTerm = Engine_AIW2.TraceAtAll && Engine_AIW2.TracingFlags.Has( ArcenTracingFlags.Independents );
            ArcenCharacterBuffer tracingBuffer = tracing ? ArcenCharacterBuffer.GetFromPoolOrCreate( "Pratorian-DoBudgetLoop_OnMainThreadAndPartOfSim-trace", 10f ) : null;
            for ( PraetorianGuardBudgetType budget = PraetorianGuardBudgetType.None + 1; budget < PraetorianGuardBudgetType.Length; budget++ )
            {
                FInt strength = this.GetStoredAIPurchaseCost( budget );
                bool noUnitsAvailable = false;
                FInt budgetSpent = this.DoSingleBudgetLoopIteration_OnMainThreadAndPartOfSim( Context, strength, (int)budget, out noUnitsAvailable );
                if ( noUnitsAvailable )
                {
                    //if there are no units, just waste half the budget
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
            switch ( (PraetorianGuardBudgetType)budgetTypeAsInt )
            {
                case PraetorianGuardBudgetType.NonForcefieldGuardians:
                    category = budgetItem.GuardianAIShipGroup;
                    break;
                case PraetorianGuardBudgetType.TopTier:
                    category = budgetItem.DireGuardianAIShipGroup;
                    break;
                case PraetorianGuardBudgetType.ForcefieldGuardians:
                    category = budgetItem.ForcefieldGuardianAIShipGroup;
                    break;
                case PraetorianGuardBudgetType.Normal:
                case PraetorianGuardBudgetType.RapidResponse:
                case PraetorianGuardBudgetType.Bruisers:
                case PraetorianGuardBudgetType.Extragalactic: //in case we need it later
                    category = budgetItem.NormalAIShipGroup;
                    break;
            }
            if ( category == null )
                ArcenDebugging.ArcenDebugLogSingleLine( "whoops; type " + (PraetorianGuardBudgetType)budgetTypeAsInt, Verbosity.DoNotShow );
            bagToFill.Clear();
            AIShipGroup group = category.DrawBag.PickRandomItemAndReplace( Context.RandomToUse );
            if ( group == null )
                return;
            bagToFill.CopyFrom( group.DrawBag );
        }
        #endregion

        #region DoGameStartLogic
        public void DoGameStartLogic( ArcenHostOnlySimContext Context )
        {
            Faction myFaction = MySpecificSubfaction;

            //if we are going to auto-kite, then we need to set ourselves to do so!
            myFaction.UnitsAutoKite_NeverSetDirectlyOrItBreaksEverything = this.SubType.AutoKite;
        }
        #endregion
    }
}
