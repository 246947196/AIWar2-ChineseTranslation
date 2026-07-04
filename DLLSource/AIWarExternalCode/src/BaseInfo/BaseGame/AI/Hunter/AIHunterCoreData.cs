using Arcen.AIW2.Core;
using Arcen.Universal;
using System;

using System.Text;

namespace Arcen.AIW2.External
{
    public class AIHunterCoreData : AISubFactionCoreDataRoot
    {
        protected override string TracingNameRoot => "HunterCore";
        protected override bool IsHunter => true;
        protected override bool IsWarden => false;
        protected override bool IsPraetorian => false;
        protected override AIBudgetType MyAIBudgetType => AIBudgetType.HunterFleet;
        public override Faction MySpecificSubfaction => BaseInfoOfParentSentinels.SubFac_Hunter;
        public override bool ShouldHuntEnemyKingUnits => this.SubType.ShouldHuntEnemyKingUnits;
        public override bool ShouldCountAsThreat => this.SubType.ShouldCountAsThreat;
        public override bool CanWaitForReinforcements => this.AIDifficulty.CanWaitForReinforcements;
        public override bool AllowEntryIntoHostileTerritory => this.SubType.AllowEntryIntoHostileTerritory;
        public override bool MustCampOnNinjaBases => false; //this only matters for the warden
        public override bool DistanceFromKingRestricted => false;  //only matters on Praetorian
        public override FInt HostileRatio_TooDangerousToStay => this.SubType.HostileRatio_TooDangerousToStay;
        public override FInt HostileRatio_TooDangerousToTarget => this.SubType.HostileRatio_TooDangerousToTarget;
        public override FInt HostileRatio_TooDangerousToGoThrough => this.SubType.HostileRatio_TooDangerousToGoThrough;

        public HunterFleetType SubType;
        public AIDifficulty_HunterFleet AIDifficulty;
        public bool HaveCheckedForInitialDonation;
        public readonly ArcenLessLinkedList<Fireteam> Teams = ArcenLessLinkedList<Fireteam>.Create_WillNeverBeGCed( "AIHunterCoreData-Teams" );
        public readonly EnumIndexedArray<HunterFleetBudgetType,FInt> CurrentBudgetRatios = EnumIndexedArray<HunterFleetBudgetType,FInt>.Create_WillNeverBeGCed( true, FInt.Zero, "AIHunterCoreData-CurrentBudgetRatios" );
        private readonly EnumIndexedArray<HunterFleetBudgetType,FInt> StoredAIPurchaseCostByBudget = EnumIndexedArray<HunterFleetBudgetType,FInt>.Create_WillNeverBeGCed( true, FInt.Zero, "AIHunterCoreData-StoredAIPurchaseCostByBudget" );

        //this dictionary maps from the required target to the budget for that target (for hunter fireteams going after MDCs or things like that)
        public readonly EnumIndexedArrayOfProtectedKeyDictionaries<HunterFleetBudgetType, FireteamRequiredTarget, FInt> StoredAIPurchaseCostByBudgetForSpecificUnits = 
            EnumIndexedArrayOfProtectedKeyDictionaries<HunterFleetBudgetType, FireteamRequiredTarget, FInt>.Create_WillNeverBeGCed( 60, "AIHunterCoreData-StoredAIPurchaseCostByBudgetForSpecificUnits" );

        public AIHunterCoreData( AISentinelsFactionBaseInfo BaseInfo ) : base( BaseInfo ) { }

        protected override void SubCleanup()
        {
            SubType = null;
            AIDifficulty = null;
            HaveCheckedForInitialDonation = false;

            Teams.Clear();
            CurrentBudgetRatios.Clear();
            StoredAIPurchaseCostByBudget.Clear();
            StoredAIPurchaseCostByBudgetForSpecificUnits.Clear();
        }

        #region Ser / Deser
        protected override void SubSerializeTo( SerMetaData MetaData, ArcenSerializationBuffer Buffer, SerializationCommandType SerializationCmdType )
        {
            Buffer.WriteHeaderStringToLogIfLoggingActive( "HunterFleetData" );
            HunterFleetTypeTable.Instance.SerializeByInternalName( MetaData, this.SubType, Buffer, "HunterFleetType" );
            AIDifficulty_HunterFleetTable.Instance.SerializeByInternalName( MetaData, this.AIDifficulty, Buffer, "HunterFleetDifficulty" );
            if (!SerializationCmdType.GetIsNetworkType())
            {
                Buffer.AddBool(MetaData, this.HaveCheckedForInitialDonation, "HaveCheckedForInitialDonation");
                Buffer.AddByte(MetaData, ReadStyleByte.Normal, (byte)HunterFleetBudgetType.Length, "HunterFleetBudgetTypeCount");
                for (HunterFleetBudgetType i = 0; i < HunterFleetBudgetType.Length; i++)
                    Buffer.AddFInt(MetaData, this.CurrentBudgetRatios[i], "HunterFleetBudgetTypeCurrentBudgetRatios");
                Buffer.AddByte(MetaData, ReadStyleByte.Normal, (byte)HunterFleetBudgetType.Length, "HunterFleetBudgetTypeCount");
                for (HunterFleetBudgetType i = 0; i < HunterFleetBudgetType.Length; i++)
                    Buffer.AddFInt(MetaData, this.GetStoredAIPurchaseCost(i), "HunterFleetBudgetTypeStoredAIPurchaseCost");
            }
            FireteamBaseUtility.SerializeFireteams( MetaData, Buffer, SerializationCmdType, this.Teams );
            if (!SerializationCmdType.GetIsNetworkType())
            {
                //stuff for per-target-fireteams
                for (HunterFleetBudgetType i = 0; i < HunterFleetBudgetType.Length; i++)
                {
                    ProtectedKeyDictionary<FireteamRequiredTarget, FInt> dict = this.StoredAIPurchaseCostByBudgetForSpecificUnits[i];
                    if (dict != null && dict.Count > 0)
                    {
                        Buffer.AddByte(MetaData, ReadStyleByte.Normal, (byte)i, "HunterFleetBudgetType");
                        Buffer.AddInt16(MetaData, ReadStyle.NonNeg, (Int16)dict.Count, "dict.Count");
                        foreach ( KeyValuePair<FireteamRequiredTarget, FInt> pair in dict )
                        {
                            pair.Key.SerializeTo(MetaData, Buffer, SerializationCmdType);
                            Buffer.AddFInt(MetaData, pair.Value, "HunterFleetBudget");
                        }
                    }
                }
                Buffer.AddByte( MetaData, ReadStyleByte.Normal, (byte)HunterFleetBudgetType.Length, "HunterFleetBudgetType" ); //means we are done
            }
        }

        protected override void SubDeserializeInto( SerMetaData MetaData, ArcenDeserializationBuffer Buffer, SerializationCommandType SerializationCmdType )
        {
            Buffer.WriteHeaderStringToLogIfLoggingActive( "HunterFleetData" );
            Buffer.ActivateOrAddTrackerByNameIfTracking( "HunterFleetData Ext", TrackerStyle.ByTypeOnly );
            this.SubType = HunterFleetTypeTable.Instance.DeserializeByInternalName( MetaData, Buffer, "HunterFleetType" );
            this.AIDifficulty = AIDifficulty_HunterFleetTable.Instance.DeserializeByInternalName( MetaData, Buffer, "HunterFleetDifficulty" );

            int countToExpect;
            if (!SerializationCmdType.GetIsNetworkType())
            {
                this.HaveCheckedForInitialDonation = Buffer.ReadBool( MetaData, "HaveCheckedForInitialDonation" );
                countToExpect = Buffer.ReadByte(MetaData, ReadStyleByte.Normal, "HunterFleetBudgetTypeCount");
                for (int i = 0; i < countToExpect; i++)
                    this.CurrentBudgetRatios[(HunterFleetBudgetType)i] = Buffer.ReadFInt(MetaData, "HunterFleetBudgetTypeCurrentBudgetRatios");
                countToExpect = Buffer.ReadByte(MetaData, ReadStyleByte.Normal, "HunterFleetBudgetTypeCount");
                for (int i = 0; i < countToExpect; i++)
                    this.AddStoredAIPurchaseCost((HunterFleetBudgetType)i, Buffer.ReadFInt(MetaData, "HunterFleetBudgetTypeStoredAIPurchaseCost"));
            }
            FireteamBaseUtility.DeserializeFireteamsAndDiscardAnyExtraLeftovers( MetaData, Buffer, SerializationCmdType, this.Teams, "hunter fleet" );

            StoredAIPurchaseCostByBudgetForSpecificUnits.Clear();
            if (!SerializationCmdType.GetIsNetworkType())
            {
                while (true)
                {
                    HunterFleetBudgetType type = (HunterFleetBudgetType)Buffer.ReadByte(MetaData, ReadStyleByte.Normal, "HunterFleetBudgetType");
                    if (type == HunterFleetBudgetType.Length)
                        break; //means we are done

                    ProtectedKeyDictionary<FireteamRequiredTarget, FInt> dict = StoredAIPurchaseCostByBudgetForSpecificUnits[type];
                    Int16 dictCount = Buffer.ReadInt16(MetaData, ReadStyle.NonNeg, "dict.Count");
                    for (int i = 0; i < dictCount; i++)
                    {
                        FireteamRequiredTarget target = FireteamRequiredTarget.GetFromPoolOrCreate();
                        target.DeserializedIntoSelf(MetaData, Buffer, SerializationCmdType);
                        dict[target] = Buffer.ReadFInt(MetaData, "HunterFleetBudget");
                    }
                }
            }
            Buffer.StopTrackerByName( "HunterFleetData Ext" );
        }
        #endregion

        public override PlanetPathfinder GetPathfinderThatMustBeReleased()
        {
            return PlanetPathfinderBasic.Pool.GetFromPoolOrCreate( "AIHunterCoreData-PlanetPathfinderBasic", 30f );
        }

        public FInt GetStoredAIPurchaseCost( HunterFleetBudgetType Budget )
        {
            return this.StoredAIPurchaseCostByBudget[Budget];
        }

        public void AddStoredAIPurchaseCost( HunterFleetBudgetType Budget, FInt AmountToAdd )
        {
            this.StoredAIPurchaseCostByBudget[Budget] += AmountToAdd;
        }

        #region ReceiveDonation
        public FInt ReceiveDonation( FInt Strength, Faction FromFaction, FireteamRequiredTarget RequiredTargetOrNull )
        {
            Faction myFaction = MySpecificSubfaction;
            if ( myFaction == null )
                return FInt.Zero;

            #region Tracing
            bool tracing = this.tracing_shortTerm = Engine_AIW2.TraceAtAll && Engine_AIW2.TracingFlags.Has( ArcenTracingFlags.Independents );
            ArcenCharacterBuffer tracingBuffer = tracing ? ArcenCharacterBuffer.GetFromPoolOrCreate( "Hunter-ReceiveDonation-trace", 10f ) : null;
            if ( tracing ) tracingBuffer.Add( "HunterFleet ReceiveDonation trace begins for faction " ).Add( myFaction.SpecialFactionData.InternalName ).Add( " (index " ).Add( myFaction.FactionIndex ).Add( ")" );
            if ( tracing ) tracingBuffer.Add( "\n" ).Add( "receiving " ).Add( Strength.ReadableString ).Add( " from " ).Add( FromFaction.SpecialFactionData.InternalName ).Add( " (index " ).Add( FromFaction.FactionIndex ).Add( ")" );
            #endregion
            this.SetSpendingRatios();
            for ( HunterFleetBudgetType i = HunterFleetBudgetType.None + 1; i < HunterFleetBudgetType.Length; i++ )
            {
                FInt ratio = this.CurrentBudgetRatios[i];
                FInt strengthForThis = Strength * ratio;
                if ( RequiredTargetOrNull == null )
                {
                    //this is the usual code path, for regular hunter fleet stuff
                    #region Tracing
                    if ( tracing ) tracingBuffer.Add( "\n" ).Add( "allocating x" ).Add( ratio.ReadableString ).Add( " (" ).Add( strengthForThis.ReadableString ).Add( ") to " ).Add( i.ToString() ).Add( " (previously  " ).Add( this.GetStoredAIPurchaseCost( i ).ReadableString );
                    #endregion
                    this.AddStoredAIPurchaseCost( i, strengthForThis );
                    #region Tracing
                    if ( tracing ) tracingBuffer.Add( ", now " ).Add( this.GetStoredAIPurchaseCost( i ).ReadableString ).Add( ")" );
                    #endregion
                }
                else
                {
                    //this code path is only for fireteams that must be going after specific targets
                    FInt budgetToUpdate = FInt.Zero;
                    foreach ( KeyValuePair<FireteamRequiredTarget, FInt> pair in this.StoredAIPurchaseCostByBudgetForSpecificUnits[i] )
                    {
                        if ( !pair.Key.IsEqualTo( RequiredTargetOrNull ) )
                            continue;
                        budgetToUpdate = pair.Value;
                        break;
                    }
                    #region Tracing
                    if ( tracing ) tracingBuffer.Add( "\n" ).Add( RequiredTargetOrNull.ToString() ).Add( ": allocating x" ).Add( ratio.ReadableString ).Add( " (" ).Add( strengthForThis.ReadableString ).Add( ")" ).Add( " to " ).Add( i.ToString() ).Add( " (previously  " ).Add( budgetToUpdate.ReadableString );
                    #endregion
                    budgetToUpdate += strengthForThis;
                    this.StoredAIPurchaseCostByBudgetForSpecificUnits[i][RequiredTargetOrNull] = budgetToUpdate;
                    #region Tracing
                    if ( tracing ) tracingBuffer.Add( ", now " ).Add( budgetToUpdate.ReadableString ).Add( ")" );
                    #endregion
                }
            }
            #region Tracing
            if ( tracing ) tracingBuffer.Add( "\n" ).Add( "HunterFleet ReceiveDonation trace ends" );
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

        #region SetSpendingRatios
        public void SetSpendingRatios()
        {
            FInt AIP = FactionUtilityMethods.Instance.GetCurrentAIP();
            this.CurrentBudgetRatios[HunterFleetBudgetType.ForcefieldGuardians] = FInt.FromParts( 0, 000 );
            if ( this.AIDifficulty.CanUseDireGuardians && AIP.IntValue >= this.AIDifficulty.AIPForDireUnlock )
            {
                this.CurrentBudgetRatios[HunterFleetBudgetType.DireGuardians] = FInt.FromParts( 0, 250 );
                this.CurrentBudgetRatios[HunterFleetBudgetType.NonForcefieldGuardians] = FInt.FromParts( 0, 375 );
                this.CurrentBudgetRatios[HunterFleetBudgetType.Normal] = FInt.FromParts( 0, 350 );
                this.CurrentBudgetRatios[HunterFleetBudgetType.Decloaker] = FInt.FromParts( 0, 025 );
            }
            else
            {
                this.CurrentBudgetRatios[HunterFleetBudgetType.DireGuardians] = FInt.FromParts( 0, 000 );
                this.CurrentBudgetRatios[HunterFleetBudgetType.NonForcefieldGuardians] = FInt.FromParts( 0, 500 );
                this.CurrentBudgetRatios[HunterFleetBudgetType.Normal] = FInt.FromParts( 0, 480 );
                this.CurrentBudgetRatios[HunterFleetBudgetType.Decloaker] = FInt.FromParts( 0, 020 );
            }
        }
        #endregion

        public override FInt GetOverconfidenceRatio()
        {
            return this.AIDifficulty.OverconfidenceRatio;
        }

        public override Int16 GetNeverCampCloserThanXHopsToHostileTerritory()
        {
            return 0; //that's what the original classes said, and there wasn't a dial for adjusting it
        }

        public override string GetBudgetName( int BudgetAsInt )
        {
            return ((PraetorianGuardBudgetType)BudgetAsInt).ToString();
        }

        #region DoBudgetLoop_OnMainThreadAndPartOfSim
        protected override void DoBudgetLoop_OnMainThreadAndPartOfSim( ArcenHostOnlySimContext Context )
        {
            bool tracing = this.tracing_shortTerm = Engine_AIW2.TraceAtAll && Engine_AIW2.TracingFlags.Has( ArcenTracingFlags.Independents );
            ArcenCharacterBuffer tracingBuffer = tracing ? ArcenCharacterBuffer.GetFromPoolOrCreate( "Hunter-DoBudgetLoop_OnMainThreadAndPartOfSim-trace", 10f ) : null;

            for ( HunterFleetBudgetType budget = HunterFleetBudgetType.None + 1; budget < HunterFleetBudgetType.Length; budget++ )
            {
                //this is the "normal" code path for hunter spending
                FInt strength = this.GetStoredAIPurchaseCost( budget );
                bool noUnitsAvailable = false;
                FInt strengthSpent = this.DoSingleBudgetLoopIteration_OnMainThreadAndPartOfSim( Context, strength, (int)budget, out noUnitsAvailable );
                if ( noUnitsAvailable )
                {
                    //if there are no units, just wast half the budget
                    #region Tracing
                    if ( tracing ) tracingBuffer.Add( "\n" ).Add( "Budget " ).Add( this.GetBudgetName( (int)budget ) ).Add( " has no units it can buy, so just lose " ).Add( (strength / 2).ReadableString ).Add( " from this budget" );
                    #endregion

                    strengthSpent += (strength / 2);
                }
                this.AddStoredAIPurchaseCost( budget, -strengthSpent );
            }
            for ( HunterFleetBudgetType budget = HunterFleetBudgetType.None + 1; budget < HunterFleetBudgetType.Length; budget++ )
            {
                if ( tracing ) tracingBuffer.Add( ".\n" ).Add( "Now processing " + this.StoredAIPurchaseCostByBudgetForSpecificUnits[budget].Count + " hunter income against various Specific Units/Factions." ).Add( "\n" );
                foreach ( KeyValuePair<FireteamRequiredTarget, FInt> pair in this.StoredAIPurchaseCostByBudgetForSpecificUnits[budget] )
                {
                    if ( tracing ) tracingBuffer.Add( "\t " + pair.Key ).Add( "\n" );
                    //to make fireteams that are required to go after specific targets

                    //this is the "normal" code path for hunter spending
                    FInt strength = pair.Value;
                    if ( tracing ) tracingBuffer.Add( "\t\t " + budget.ToString() + ":  " + strength ).Add( "\n" );
                    bool noUnitsAvailable = false;
                    FInt strengthSpent = this.DoSingleBudgetLoopIteration_OnMainThreadAndPartOfSim( Context, strength, (int)budget, out noUnitsAvailable, pair.Key );
                    if ( noUnitsAvailable )
                    {
                        //if there are no units, just wast half the budget
                        #region Tracing
                        if ( tracing ) tracingBuffer.Add( "\n" ).Add( "Budget " ).Add( this.GetBudgetName( (int)budget ) ).Add( " has no units it can buy, so just lose " ).Add( (strength / 2).ReadableString ).Add( " from this budget" );
                        #endregion

                        strengthSpent += (strength / 2);
                    }
                    strength -= strengthSpent;
                    this.StoredAIPurchaseCostByBudgetForSpecificUnits[budget][pair.Key] = strength;
                    if ( tracing && strengthSpent > 0 ) tracingBuffer.Add( "\n" ).Add( "Budget " ).Add( this.GetBudgetName( (int)budget ) ).Add( " now has " ).Add( strength ).Add( " resource in it after spending " ).Add( strength.ReadableString ).Add( " strength in units.\n" );
                }
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
            switch ( (HunterFleetBudgetType)budgetTypeAsInt )
            {
                case HunterFleetBudgetType.NonForcefieldGuardians:
                    category = budgetItem.GuardianAIShipGroup;
                    break;
                case HunterFleetBudgetType.DireGuardians:
                    category = budgetItem.DireGuardianAIShipGroup;
                    break;
                case HunterFleetBudgetType.ForcefieldGuardians:
                    category = budgetItem.ForcefieldGuardianAIShipGroup;
                    break;
                case HunterFleetBudgetType.Normal:
                    category = budgetItem.NormalAIShipGroup;
                    break;
                case HunterFleetBudgetType.Decloaker:
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
