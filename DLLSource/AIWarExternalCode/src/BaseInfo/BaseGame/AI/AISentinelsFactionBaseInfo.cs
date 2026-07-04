using Arcen.AIW2.Core;
using Arcen.Universal;
using System;

using System.Text;

namespace Arcen.AIW2.External
{
    public partial class AISentinelsFactionBaseInfo : ExternalFactionBaseInfoRoot
    {
        /// <summary>
        /// This is a thing we usse only sparingly, but since so much here is needed for the MP client to see their top bar correctly (waves in particular, but not just that), this is a case where it is worth it.
        /// </summary>
        public override bool IsUltraFrequentSyncedInMultiplayer { get { return true; } }

        //serialized
        public readonly ProtectedList<PlannedWave> WaveList = ProtectedList<PlannedWave>.Create_WillNeverBeGCed( 300, "AISentinelsFactionBaseInfo-WaveList" );

        public readonly AISentinelsCoreData SentinelInfo;
        public readonly AIWardenCoreData WardenInfo;
        public readonly AIHunterCoreData HunterInfo;
        public readonly AIPraetorianGuardCoreData PraetorianInfo;

        //not serialized
        public Faction SubFac_Warden { get; private set; }
        public Faction SubFac_Hunter { get; private set; }
        public Faction SubFac_Praetorian { get; private set; }
        public Faction SubFac_CPA { get; private set; }
        public Faction SubFac_RelentlessWave { get; private set; }
        public Faction SubFac_BorderAggression { get; private set; }

        public AIWardenFactionBaseInfo SubFac_Warden_BaseInfo { get; private set; }
        public AIHunterFactionBaseInfo SubFac_Hunter_BaseInfo { get; private set; }
        public AIPraetorianGuardFactionBaseInfo SubFac_Praetorian_BaseInfo { get; private set; }
        public AICrossPlanetAttackerBaseInfo SubFac_CPA_BaseInfo { get; private set; }
        public AIRelentlessWaveFactionBaseInfo SubFac_RelentlessWave_BaseInfo { get; private set; }
        public AIBorderAggressionFactionBaseInfo SubFac_BorderAggression_BaseInfo { get; private set; }

        public AISentinelsFactionBaseInfo()
        {
            SentinelInfo = new AISentinelsCoreData( this );
            WardenInfo = new AIWardenCoreData( this );
            HunterInfo = new AIHunterCoreData( this );
            PraetorianInfo = new AIPraetorianGuardCoreData( this );

            Cleanup();
        }

        protected override void Cleanup()
        {            
            //serialized
            WaveList.Clear( true );

            SentinelInfo.Cleanup();
            WardenInfo.Cleanup();
            HunterInfo.Cleanup();
            PraetorianInfo.Cleanup();

            //not serialized
            SubFac_Warden = null;
            SubFac_Hunter = null;
            SubFac_Praetorian = null;
            SubFac_CPA = null;
            SubFac_RelentlessWave = null;
            SubFac_BorderAggression = null;

            SubFac_Warden_BaseInfo = null;
            SubFac_Hunter_BaseInfo = null;
            SubFac_Praetorian_BaseInfo = null;
            SubFac_CPA_BaseInfo = null;
            SubFac_RelentlessWave_BaseInfo = null;
            SubFac_BorderAggression_BaseInfo = null;

            HaveLoadedData = false; //trigger a reload of xml
        }

        protected override void DoAnySubInitializationImmediatelyAfterFactionAssigned() 
        {
        }

        #region Ser / Deser
        public override void SerializeFactionTo( SerMetaData MetaData, ArcenSerializationBuffer Buffer, SerializationCommandType SerializationCmdType )
        {
            //if there are going to be that many waves, that's a ridiculous problem and should be scrubbed anyway
            WaveList.RemoveEndEntriesWhileCountLargerThan( 250 );
            for ( int i = 0; i < WaveList.Count; i++ )
            {
                Buffer.AddBool( MetaData, true, "HasAnotherWaveListElement" );
                WaveList[i].SerializeTo( MetaData, Buffer, SerializationCmdType );
            }
            Buffer.AddBool( MetaData, false, "HasAnotherWaveListElement" );

            SentinelInfo.SerializeTo( MetaData, Buffer, SerializationCmdType );
            WardenInfo.SerializeTo( MetaData, Buffer, SerializationCmdType );
            HunterInfo.SerializeTo( MetaData, Buffer, SerializationCmdType );
            PraetorianInfo.SerializeTo( MetaData, Buffer, SerializationCmdType );
        }

        public override void DeserializeFactionIntoSelf( SerMetaData MetaData, ArcenDeserializationBuffer Buffer, SerializationCommandType SerializationCmdType )
        {
            if ( Buffer.FromGameVersion.GetGreaterThanOrEqualTo( 5, 508 ) )
            {
                WaveList.Clear( true );
                while ( Buffer.ReadBool( MetaData, "HasAnotherWaveListElement" ) )
                {
                    PlannedWave W = PlannedWave.GetFromPoolOrCreate();
                    W.DeserializeIntoSelf( MetaData, Buffer, SerializationCmdType );
                    WaveList.Add( W );
                }
            }
            else
            {
                int numElements = Buffer.ReadByte( MetaData, ReadStyleByte.Normal, "WaveList.Count" );
                WaveList.DeserializeUncertainNumberOfEntriesIntoExistingList( numElements, 
                    delegate { return PlannedWave.GetFromPoolOrCreate(); }, 
                    delegate( PlannedWave W ) { W.DeserializeIntoSelf( MetaData, Buffer, SerializationCmdType ); } );
            }
            if ( Buffer.FromGameVersion.GetLessThan( 3, 704 ) ) //3704_FastStrengthCalcs
            {
                AITypeDataTable.Instance.DeserializeByIndex( MetaData, Buffer, "AIType-OLD" );
                Buffer.ReadByte( MetaData, ReadStyleByte.Normal, "AdaptiveAIDifficulty-OLD" );
                Buffer.ReadBool( MetaData, "WasRandomAIType-OLD" );
            }

            SentinelInfo.DeserializeInto( MetaData, Buffer, SerializationCmdType );
            WardenInfo.DeserializeInto( MetaData, Buffer, SerializationCmdType );
            HunterInfo.DeserializeInto( MetaData, Buffer, SerializationCmdType );
            PraetorianInfo.DeserializeInto( MetaData, Buffer, SerializationCmdType );
        }
        #endregion

        public override int GetDifficultyOrdinal_OrNegativeOneIfNotRelevant()
        {
            return this.SentinelInfo.AIDifficulty == null ? -1 : this.SentinelInfo.AIDifficulty.Difficulty;
        }

        private bool isOkayIfHaveErrorsInFactionSettingsRefresh = false;
        public override float CalculateYourPortionOfPredictedGameLoad_Where100IsANormalAI( ArcenCharacterBufferBase OptionalExplainCalculation )
        {
            this.isOkayIfHaveErrorsInFactionSettingsRefresh = true;
            try
            {
                DoRefreshFromFactionSettings();
            }
            catch { }
            this.isOkayIfHaveErrorsInFactionSettingsRefresh = false;


            if ( OptionalExplainCalculation != null )
                OptionalExplainCalculation.Add( "AI Faction Load:" );

            int load = 80;

            string fieldValue = this.AttachedFaction.GetStringValueForCustomFieldOrDefaultValue( "AIType", false );
            AITypeData aiType = AITypeDataTable.Instance.GetRowByNameOrNullIfNotFound( fieldValue );
            if ( aiType != null )
            {
                switch ( aiType.Difficulty )
                {
                    case TypeDifficulty.Brutal:
                        load += 40;
                        if ( OptionalExplainCalculation != null )
                            OptionalExplainCalculation.Add( "\n   +40 From Brutal AI Type" );
                        break;
                    case TypeDifficulty.Hard:
                        load += 20;
                        if ( OptionalExplainCalculation != null )
                            OptionalExplainCalculation.Add( "\n   +20 From Hard AI Type" );
                        break;
                    case TypeDifficulty.Moderate:
                        load += 20;
                        if ( OptionalExplainCalculation != null )
                            OptionalExplainCalculation.Add( "\n   +20 From Moderate AI Type" );
                        break;
                }
            }

            if ( this.SentinelInfo.AIDifficulty != null && this.SentinelInfo.AIDifficulty.Difficulty > 6 )
            {
                if ( OptionalExplainCalculation != null )
                    OptionalExplainCalculation.Add( "\n   +" ).Add( ((this.SentinelInfo.AIDifficulty.Difficulty - 6) * 15) ).Add( " AI Difficulty " ).Add( this.SentinelInfo.AIDifficulty.Difficulty );
                load += ((this.SentinelInfo.AIDifficulty.Difficulty - 6) * 15);
            }
            if ( this.WardenInfo.AIDifficulty != null && this.WardenInfo.AIDifficulty.Difficulty > 6 )
            {
                if ( OptionalExplainCalculation != null )
                    OptionalExplainCalculation.Add( "\n   +" ).Add( ((this.WardenInfo.AIDifficulty.Difficulty - 6) * 6) ).Add( " AI Warden Difficulty " ).Add( this.WardenInfo.AIDifficulty.Difficulty );
                load += ((this.WardenInfo.AIDifficulty.Difficulty - 6) * 6);
            }
            if ( this.HunterInfo.AIDifficulty != null && this.HunterInfo.AIDifficulty.Difficulty > 6 )
            {
                if ( OptionalExplainCalculation != null )
                    OptionalExplainCalculation.Add( "\n   +" ).Add( ((this.HunterInfo.AIDifficulty.Difficulty - 6) * 12) ).Add( " AI Hunter Difficulty " ).Add( this.HunterInfo.AIDifficulty.Difficulty );
                load += ((this.HunterInfo.AIDifficulty.Difficulty - 6) * 12);
            }
            if ( this.PraetorianInfo.AIDifficulty != null && this.PraetorianInfo.AIDifficulty.Difficulty > 6 )
            {
                if ( OptionalExplainCalculation != null )
                    OptionalExplainCalculation.Add( "\n   +" ).Add( ((this.PraetorianInfo.AIDifficulty.Difficulty - 6) * 3) ).Add( " AI Praetorian Difficulty " ).Add( this.PraetorianInfo.AIDifficulty.Difficulty );
                load += ((this.PraetorianInfo.AIDifficulty.Difficulty - 6) * 3);
            }

            if ( OptionalExplainCalculation != null )
                OptionalExplainCalculation.Add( "\n   Total: " ).Add( load ).Add( " Load From AI Faction" );
            return load;
        }

        #region From Xml
        //this stuff is just externaldata copied in
        private bool HaveLoadedData;
        public int wavewarningnone = 0;
        public int wavewarningshort = 0;
        public int wavewarningmedium = 0;
        public int wavewarninglong = 0;

        private void LoadCustomDataIfNeeded()
        {
            if ( this.HaveLoadedData )
                return;
            this.HaveLoadedData = true;
            wavewarningnone = ExternalConstants.Instance.GetCustomInt32_Slow( "custom_int_wavewarning_wavewarningnone" );
            wavewarningshort = ExternalConstants.Instance.GetCustomInt32_Slow( "custom_int_wavewarning_wavewarningshort" );
            wavewarningmedium = ExternalConstants.Instance.GetCustomInt32_Slow( "custom_int_wavewarning_wavewarningmedium" );
            wavewarninglong = ExternalConstants.Instance.GetCustomInt32_Slow( "custom_int_wavewarning_wavewarninglong" );
        }
        #endregion

        #region DoFactionGeneralAggregationsPausedOrUnpaused
        protected override void DoFactionGeneralAggregationsPausedOrUnpaused()
        {
            LoadCustomDataIfNeeded();
        }
        #endregion

        #region DoRefreshFromFactionSettings        
        protected override void DoRefreshFromFactionSettings()
        {
            if ( this.SentinelInfo.AIDifficulty == null ||
                this.WardenInfo.AIDifficulty == null ||
                this.HunterInfo.AIDifficulty == null ||
                this.PraetorianInfo.AIDifficulty == null )
                this.CustomData_AIDifficultyAndSubFactionDifficulties( !isOkayIfHaveErrorsInFactionSettingsRefresh );

            if ( !isOkayIfHaveErrorsInFactionSettingsRefresh )
            {
                if ( this.SentinelInfo.AIDifficulty == null )
                    ArcenDebugging.ArcenDebugLogSingleLine( "Missing SentinelInfo.AIDifficulty for AI faction '" +
                        this.AttachedFaction.GetDisplayName(), Verbosity.ShowAsError );
                if ( this.WardenInfo.AIDifficulty == null )
                    ArcenDebugging.ArcenDebugLogSingleLine( "Missing WardenInfo.AIDifficulty for AI faction '" +
                        this.AttachedFaction.GetDisplayName(), Verbosity.ShowAsError );
                if ( this.HunterInfo.AIDifficulty == null )
                    ArcenDebugging.ArcenDebugLogSingleLine( "Missing HunterInfo.AIDifficulty for AI faction '" +
                        this.AttachedFaction.GetDisplayName(), Verbosity.ShowAsError );
                if ( this.PraetorianInfo.AIDifficulty == null )
                    ArcenDebugging.ArcenDebugLogSingleLine( "Missing PraetorianInfo.AIDifficulty for AI faction '" +
                        this.AttachedFaction.GetDisplayName(), Verbosity.ShowAsError );
            }

            #region AIType
            if ( this.SentinelInfo.AIType == null ) //don't set this more than once, or we'll keep getting new random values
                this.CustomData_AIType( !isOkayIfHaveErrorsInFactionSettingsRefresh );
            #endregion

            #region SubFaction Links
            if ( this.SubFac_Warden == null )
            {
                foreach ( Int16 facIndex in this.AttachedFaction.FactionIndicesOfMyChildren )
                {
                    Faction subFac = World_AIW2.Instance.Factions[facIndex];
                    switch ( subFac.SpecialFactionData.InternalName )
                    {
                        case "AIWarden":
                            SubFac_Warden = subFac;
                            break;
                        case "HunterFleet":
                            SubFac_Hunter = subFac;
                            break;
                        case "PraetorianGuard":
                            SubFac_Praetorian = subFac;
                            break;
                        case "AICrossPlanetAttacker":
                            SubFac_CPA = subFac;
                            break;
                        case "AIRelentlessWave":
                            SubFac_RelentlessWave = subFac;
                            break;
                        case "AIBorderAggression":
                            SubFac_BorderAggression = subFac;
                            break;
                        default:
                            ArcenDebugging.ArcenDebugLogSingleLine( "Unexpected subfaction '" + subFac.SpecialFactionData.InternalName + "' linked to AI faction '" +
                                this.AttachedFaction.GetDisplayName() + "'", Verbosity.ShowAsError );
                            break;
                    }
                }

                if ( SubFac_Warden == null )
                {
                    if ( !World_AIW2.Instance.GetIsTutorial() && !isOkayIfHaveErrorsInFactionSettingsRefresh )
                        ArcenDebugging.ArcenDebugLogSingleLine( "Missing AIWarden subfaction for AI faction '" +
                            this.AttachedFaction.GetDisplayName() + "', which has " + this.AttachedFaction.FactionIndicesOfMyChildren.Count +
                            " subfactions.", Verbosity.ShowAsError );
                }
                else
                    SubFac_Warden_BaseInfo = SubFac_Warden.TryGetExternalBaseInfoAs<AIWardenFactionBaseInfo>();

                if ( SubFac_Hunter == null )
                {
                    if ( !World_AIW2.Instance.GetIsTutorial() && !isOkayIfHaveErrorsInFactionSettingsRefresh )
                        ArcenDebugging.ArcenDebugLogSingleLine( "Missing HunterFleet subfaction for AI faction '" +
                            this.AttachedFaction.GetDisplayName() + "', which has " + this.AttachedFaction.FactionIndicesOfMyChildren.Count +
                            " subfactions.", Verbosity.ShowAsError );
                }
                else
                    SubFac_Hunter_BaseInfo = SubFac_Hunter.TryGetExternalBaseInfoAs<AIHunterFactionBaseInfo>();

                if ( SubFac_Praetorian == null )
                {
                    if ( !World_AIW2.Instance.GetIsTutorial() && !isOkayIfHaveErrorsInFactionSettingsRefresh )
                        ArcenDebugging.ArcenDebugLogSingleLine( "Missing PraetorianGuard subfaction for AI faction '" +
                            this.AttachedFaction.GetDisplayName() + "', which has " + this.AttachedFaction.FactionIndicesOfMyChildren.Count +
                            " subfactions.", Verbosity.ShowAsError );
                }
                else
                    SubFac_Praetorian_BaseInfo = SubFac_Praetorian.TryGetExternalBaseInfoAs<AIPraetorianGuardFactionBaseInfo>();

                if ( SubFac_CPA == null )
                {
                    if ( !World_AIW2.Instance.GetIsTutorial() && !isOkayIfHaveErrorsInFactionSettingsRefresh )
                        ArcenDebugging.ArcenDebugLogSingleLine( "Missing AICrossPlanetAttacker subfaction for AI faction '" +
                            this.AttachedFaction.GetDisplayName() + "', which has " + this.AttachedFaction.FactionIndicesOfMyChildren.Count +
                            " subfactions.", Verbosity.ShowAsError );
                }
                else
                    SubFac_CPA_BaseInfo = SubFac_CPA.TryGetExternalBaseInfoAs<AICrossPlanetAttackerBaseInfo>();

                if ( SubFac_RelentlessWave == null )
                {
                    if ( !World_AIW2.Instance.GetIsTutorial() && !isOkayIfHaveErrorsInFactionSettingsRefresh )
                        ArcenDebugging.ArcenDebugLogSingleLine( "Missing AIRelentlessWave subfaction for AI faction '" +
                            this.AttachedFaction.GetDisplayName() + "', which has " + this.AttachedFaction.FactionIndicesOfMyChildren.Count +
                            " subfactions.", Verbosity.ShowAsError );
                }
                else
                    SubFac_RelentlessWave_BaseInfo = SubFac_RelentlessWave.TryGetExternalBaseInfoAs<AIRelentlessWaveFactionBaseInfo>();

                if ( SubFac_BorderAggression == null )
                {
                    if ( !World_AIW2.Instance.GetIsTutorial() && !isOkayIfHaveErrorsInFactionSettingsRefresh )
                        ArcenDebugging.ArcenDebugLogSingleLine( "Missing AIBorderAggression subfaction for AI faction '" +
                            this.AttachedFaction.GetDisplayName() + "', which has " + this.AttachedFaction.FactionIndicesOfMyChildren.Count +
                            " subfactions.", Verbosity.ShowAsError );
                }
                else
                    SubFac_BorderAggression_BaseInfo = SubFac_BorderAggression.TryGetExternalBaseInfoAs<AIBorderAggressionFactionBaseInfo>();
            }
            #endregion

            #region WardenInfo.SubType
            if ( this.WardenInfo != null && this.WardenInfo.SubType == null )
            {
                this.WardenInfo.SubType = AIWardenTypeDataTable.Instance.GetRowByNameOrNullIfNotFound( this.AttachedFaction.GetStringValueForCustomFieldOrDefaultValue( "WardenType", !isOkayIfHaveErrorsInFactionSettingsRefresh ) );
                if ( this.WardenInfo.SubType == null )
                    this.WardenInfo.SubType = AIWardenTypeDataTable.Instance.Rows[0];
            }
            #endregion

            #region HunterInfo.SubType
            if ( this.HunterInfo != null && this.HunterInfo.SubType == null )
            {
                this.HunterInfo.SubType = HunterFleetTypeTable.Instance.GetRowByNameOrNullIfNotFound( this.AttachedFaction.GetStringValueForCustomFieldOrDefaultValue( "HunterFleetType", !isOkayIfHaveErrorsInFactionSettingsRefresh ) );
                if ( this.HunterInfo.SubType == null )
                    this.HunterInfo.SubType = HunterFleetTypeTable.Instance.Rows[0];
            }
            #endregion

            #region PraetorianInfo.SubType
            if ( this.PraetorianInfo != null && this.PraetorianInfo.SubType == null )
            {
                //there is not field for this, so just always set the default
                this.PraetorianInfo.SubType = PraetorianGuardTypeDataTable.Instance.Rows[0];
            }
            #endregion

            if ( !World_AIW2.Instance.GetIsTutorial() && !isOkayIfHaveErrorsInFactionSettingsRefresh )
            {
                if ( this.SentinelInfo.AIType == null )
                    ArcenDebugging.ArcenDebugLogSingleLine( "Missing SentinelInfo.AIType for AI faction '" +
                        this.AttachedFaction.GetDisplayName(), Verbosity.ShowAsError );
                if ( this.WardenInfo.SubType == null )
                    ArcenDebugging.ArcenDebugLogSingleLine( "Missing WardenInfo.SubType for AI faction '" +
                        this.AttachedFaction.GetDisplayName(), Verbosity.ShowAsError );
                if ( this.HunterInfo.SubType == null )
                    ArcenDebugging.ArcenDebugLogSingleLine( "Missing HunterInfo.SubType for AI faction '" +
                        this.AttachedFaction.GetDisplayName(), Verbosity.ShowAsError );
                if ( this.PraetorianInfo.SubType == null )
                    ArcenDebugging.ArcenDebugLogSingleLine( "Missing PraetorianInfo.SubType for AI faction '" +
                        this.AttachedFaction.GetDisplayName(), Verbosity.ShowAsError );
            }

            #region AIWarden_TrimColor
            {
                string fieldValue = AttachedFaction.GetStringValueForCustomFieldOrDefaultValue( "AIWarden_TrimColor", !isOkayIfHaveErrorsInFactionSettingsRefresh );
                if ( this.SubFac_Warden != null )
                {
                    this.SubFac_Warden.Config.FactionCenterColor = AttachedFaction.FactionCenterColor;
                    this.SubFac_Warden.Config.FactionTrimColor = TeamColorDefinitionTable.Instance.GetRowByNameOrNullIfNotFound( fieldValue );
                }

                ////without this set, SyncTeamColorToAndFromOthersIfNeeded will just overwrite the stuff set above
                //ConfigurationForFaction wardenConfig;
                //{
                //    wardenConfig = World_AIW2.Instance.SetupWorkingForLobbyOnly.FactionConfigurations[SubFac_Warden.FactionIndex];
                //    wardenConfig.FactionCenterColor = this.SubFac_Warden.Config.FactionCenterColor;
                //    wardenConfig.FactionTrimColor = this.SubFac_Warden.Config.FactionTrimColor;
                //}

                ////we need to set both of these, the long-term and the lobby-only.  This would normally just set long-term, but then lobby is overwriting it!
                //{
                //    wardenConfig = World_AIW2.Instance.SetupStoredLongTerm.FactionConfigurations[SubFac_Warden.FactionIndex];
                //    wardenConfig.FactionCenterColor = this.SubFac_Warden.Config.FactionCenterColor;
                //    wardenConfig.FactionTrimColor = this.SubFac_Warden.Config.FactionTrimColor;
                //}
            }
            #endregion

            #region HunterFleet_TrimColor
            {
                string fieldValue = AttachedFaction.GetStringValueForCustomFieldOrDefaultValue( "HunterFleet_TrimColor", !isOkayIfHaveErrorsInFactionSettingsRefresh );
                if ( this.SubFac_Hunter != null )
                {
                    this.SubFac_Hunter.Config.FactionCenterColor = AttachedFaction.FactionCenterColor;
                    this.SubFac_Hunter.Config.FactionTrimColor = TeamColorDefinitionTable.Instance.GetRowByNameOrNullIfNotFound( fieldValue );
                }
            }
            #endregion

            #region PraetorianGuard_TrimColor
            {
                string fieldValue = AttachedFaction.GetStringValueForCustomFieldOrDefaultValue( "PraetorianGuard_TrimColor", !isOkayIfHaveErrorsInFactionSettingsRefresh );
                if ( this.SubFac_Praetorian != null )
                {
                    this.SubFac_Praetorian.Config.FactionCenterColor = AttachedFaction.FactionCenterColor;
                    this.SubFac_Praetorian.Config.FactionTrimColor = TeamColorDefinitionTable.Instance.GetRowByNameOrNullIfNotFound( fieldValue );
                }
            }
            #endregion

            isOkayIfHaveErrorsInFactionSettingsRefresh = false;
        }
        #endregion

        #region SetNewAITypeFromAdaptiveAI
        public void SetNewAITypeFromAdaptiveAI( AITypeData NewAIType )
        {
            this.SentinelInfo.AIType = NewAIType;
        }
        #endregion
        
        public override void WriteFactionSlotStatus( ArcenCharacterBufferBase buffer )
        {
            string difficulty = AIDifficultyTable.Instance.GetRowByNameOrNullIfNotFound( 
                this.AttachedFaction.Config.GetStringValueForCustomFieldOrDefaultValue( "AIDifficulty", true ) ).DisplayName;
            buffer.Add( difficulty );

            string aiType = AITypeDataTable.Instance.GetRowByNameOrNullIfNotFound(
                this.AttachedFaction.Config.GetStringValueForCustomFieldOrDefaultValue( "AIType", true ) ).DisplayName;
            buffer.Add( "    " ).Add( aiType );
        }

        public override bool GetShouldAttackNormallyExcludedTarget( GameEntity_Squad Target )
        {
            //to allow AIs to kill eachother's command stations and warp gates in civil war mode
            if ( Target == null )
                return false;
            if ( Target.TypeData.IsCommandStation )
                return true;
            if ( Target.TypeData.ProvidesAIWarpEntryPoint )
                return true;
            if ( Target.TypeData.GetHasTag( "NormalPlanetNastyPick" ) )
                return true;
            return false;
        }

        #region CustomData_AIType
        private void CustomData_AIType( bool ErrorOnMissingField )
        {
            string fieldValue = this.AttachedFaction.GetStringValueForCustomFieldOrDefaultValue( "AIType", ErrorOnMissingField );
            AITypeData result = null;
            AITypeData typedata = AITypeDataTable.Instance.GetRowByNameOrNullIfNotFound( fieldValue );
            if ( typedata == null )
                typedata = AITypeDataTable.Instance.Rows[0];
            if ( typedata.RandomDifficulty == TypeDifficulty.Unset && typedata.AdaptiveDifficulty == TypeDifficulty.Unset )
                result = AITypeDataTable.Instance.GetRowByNameOrNullIfNotFound( fieldValue );
            else if ( typedata.AdaptiveDifficulty != TypeDifficulty.Unset )
            {
                result = AITypeDataTable.Instance.GetRandomTypeByDifficulty( typedata.AdaptiveDifficulty );
                this.SentinelInfo.AdaptiveAIDifficulty = typedata.AdaptiveDifficulty;
            }
            else if ( typedata.RandomDifficulty != TypeDifficulty.Unset )
            {
                result = AITypeDataTable.Instance.GetRandomTypeByDifficulty( typedata.RandomDifficulty );
                this.SentinelInfo.WasRandomAIType = true;
            }

            if ( MapgenLogger.IsActive )
                MapgenLogger.Log( "AI type assignment: " + result == null ? "null AIType!" : result.InternalName );
            if ( result == null )
            {
                if ( ErrorOnMissingField )
                    ArcenDebugging.ArcenDebugLog( "CustomData_AIType: Unknown field value '" + fieldValue + "'", Verbosity.ShowAsError );
            }
            this.SentinelInfo.AIType = result;
        }
        #endregion

        #region CustomData_AIDifficultyAndSubFactionDifficulties
        private void CustomData_AIDifficultyAndSubFactionDifficulties( bool ErrorOnMissingField )
        {
            string fieldValue_AIDiff = this.AttachedFaction.GetStringValueForCustomFieldOrDefaultValue( "AIDifficulty", ErrorOnMissingField );

            this.SentinelInfo.AIDifficulty = AIDifficultyTable.Instance.GetRowByNameOrNullIfNotFound( fieldValue_AIDiff );
            if ( ErrorOnMissingField && this.SentinelInfo.AIDifficulty == null )
                ArcenDebugging.ArcenDebugLog( "CustomData_AIDifficulty: Unknown field value '" + fieldValue_AIDiff + "'", Verbosity.ShowAsError );
            

            if ( fieldValue_AIDiff.Contains("ModSupport"))
                fieldValue_AIDiff = "Difficulty_" + this.SentinelInfo.AIDifficulty.Difficulty;

            if ( this.AttachedFaction.GetStringValueForCustomFieldOrDefaultValue( "UniversalDifficulty", ErrorOnMissingField ) == "Enabled" )
            {
                //set the sub-types from the universal setting
                this.WardenInfo.AIDifficulty = AIDifficulty_WardenFleetTable.Instance.GetRowByNameOrNullIfNotFound( fieldValue_AIDiff );
                this.HunterInfo.AIDifficulty = AIDifficulty_HunterFleetTable.Instance.GetRowByNameOrNullIfNotFound( fieldValue_AIDiff );
                this.PraetorianInfo.AIDifficulty = AIDifficulty_PraetorianGuardTable.Instance.GetRowByNameOrNullIfNotFound( fieldValue_AIDiff );

                if ( ErrorOnMissingField && this.WardenInfo.AIDifficulty == null )
                    ArcenDebugging.ArcenDebugLog( "CustomData_AIDifficulty: Unknown field value '" + fieldValue_AIDiff +
                        "' for sub-setting the WardenInfo difficulty via UniversalDifficulty", Verbosity.ShowAsError );

                if ( ErrorOnMissingField && this.HunterInfo.AIDifficulty == null )
                    ArcenDebugging.ArcenDebugLog( "CustomData_AIDifficulty: Unknown field value '" + fieldValue_AIDiff +
                        "' for sub-setting the HunterInfo difficulty via UniversalDifficulty", Verbosity.ShowAsError );

                if ( ErrorOnMissingField && this.PraetorianInfo.AIDifficulty == null )
                    ArcenDebugging.ArcenDebugLog( "CustomData_AIDifficulty: Unknown field value '" + fieldValue_AIDiff +
                        "' for sub-setting the PraetorianInfo difficulty via UniversalDifficulty", Verbosity.ShowAsError );
            }
            else //NOT UniversalDifficulty on
            {
                {
                    string fieldValue = this.AttachedFaction.GetStringValueForCustomFieldOrDefaultValue( "AIDifficulty_WardenFleet", ErrorOnMissingField );
                    this.WardenInfo.AIDifficulty = AIDifficulty_WardenFleetTable.Instance.GetRowByNameOrNullIfNotFound( fieldValue );

                    if ( ErrorOnMissingField && this.WardenInfo.AIDifficulty == null )
                        ArcenDebugging.ArcenDebugLog( "CustomData_AIDifficulty: Unknown field value '" + fieldValue +
                            "' for sub-setting the WardenInfo difficulty via direct AIDifficulty_WardenFleet", Verbosity.ShowAsError );
                }
                {
                    string fieldValue = this.AttachedFaction.GetStringValueForCustomFieldOrDefaultValue( "AIDifficulty_HunterFleet", ErrorOnMissingField );
                    this.HunterInfo.AIDifficulty = AIDifficulty_HunterFleetTable.Instance.GetRowByNameOrNullIfNotFound( fieldValue );

                    if ( ErrorOnMissingField && this.HunterInfo.AIDifficulty == null )
                        ArcenDebugging.ArcenDebugLog( "CustomData_AIDifficulty: Unknown field value '" + fieldValue +
                            "' for sub-setting the HunterInfo difficulty via direct AIDifficulty_HunterFleet", Verbosity.ShowAsError );
                }
                {
                    string fieldValue = this.AttachedFaction.GetStringValueForCustomFieldOrDefaultValue( "AIDifficulty_PraetorianGuard", ErrorOnMissingField );
                    this.PraetorianInfo.AIDifficulty = AIDifficulty_PraetorianGuardTable.Instance.GetRowByNameOrNullIfNotFound( fieldValue );

                    if ( ErrorOnMissingField && this.PraetorianInfo.AIDifficulty == null )
                        ArcenDebugging.ArcenDebugLog( "CustomData_AIDifficulty: Unknown field value '" + fieldValue +
                            "' for sub-setting the PraetorianInfo difficulty via direct AIDifficulty_PraetorianGuard", Verbosity.ShowAsError );
                }
            }
        }
        #endregion

        #region UpdatePowerLevel
        /* TEACHING_MOMENT: Description of the Extragalactic War Mechanic, as originally implemented by Badger, 2/16/20
           The game needed a mechanic for "The player or other minor factions are getting really OP, and the AI wants to respond in kind".

           It uses a publish/subscribe model

           Each faction can publish an 'Overall Power Level', which is updated between Stage2 sim and Stage3 of the sim code.
           For most factions, the number is between 0 and 1 (Fallen Spire can go higher). 

           For factions that want to adapt their play based on the OPness of other factions, they call "ReactToPowerLevel_HostOnly" in stage3,
           then they want to iterate over all enemy factions and sum their OverallPowerLevels, then react accordingly.

           So far the only faction that reacts to power levels is the AI. The AI computes a number between 0 and 6; each whole number determines the tier of Extragalactic War units that the AI will spawn.

           The AI then updates a specific budget, based on factors in the AIDifficulty. Once it has enough money it spawns a War unit, given to its hunter fleet.

           Relevant tunables in the AI DIfficulty:
           MinAIPForExtragalacticWar: don't bother using extragalactic units. Also, subtract this number from the actual AIP when calculating per-AIP income
           MaxExtragalacticWarTier: Don't spawn extragalactic war units above this tier; used to make the game easier at very low difficulties
           ExtragalacticWarIncomyByTier: each tier gets more income
           ExtragalacticIncomePer10AIP: the AI factors in AIP when figuring out how much income to get


           and galaxy settings allow the player to modulate this.
           don't factor in any minor factions
           Don't factor in player-allied minor factions
           Don't factor in players
         */
        public override void UpdatePowerLevel()
        {
            GlobalAIWorldBaseInfo globalAI = GlobalAIWorldBaseInfo.Instance;
            if ( globalAI == null )
                return;
            FInt newResult = FInt.Zero;
            if ( this.AttachedFaction.FactionIsDefeated )
            {
                newResult = FInt.Zero;
            }
            else
            {
                newResult = globalAI.AIProgress_Effective / 400;
            }
            this.AttachedFaction.OverallPowerLevel = newResult;
        }
        #endregion
        #region DoPerSecondLogic_Stage2Aggregating_OnMainThreadAndPartOfSim_ClientAndHost
        public override void DoPerSecondLogic_Stage2Aggregating_OnMainThreadAndPartOfSim_ClientAndHost(ArcenClientOrHostSimContextCore Context)
        {
            foreach ( GameEntity_Squad entity in AttachedFaction.Squads( "AICuendillarDrill" ) )
            {
                DysonSidekickPerUnitBaseInfo data = entity.CreateExternalBaseInfo<DysonSidekickPerUnitBaseInfo>( "DysonSidekickPerUnitBaseInfo" );
            }
        }
        #endregion
        public override void DoPerSecondLogic_Stage3Main_OnMainThreadAndPartOfSim_ClientAndHost( ArcenClientOrHostSimContextCore Context )
        {
            if ( ArcenNetworkAuthority.DesiredStatus == DesiredMultiplayerStatus.Client )
            {
                #region Clean Up Old Waves On Client
                for ( int i = this.WaveList.Count - 1; i >= 0; i-- )
                {
                    PlannedWave wave = this.WaveList[i];
                    if ( wave.gameTimeInSecondsForLaunchWave < World_AIW2.Instance.GameSecond )
                    {
                        this.WaveList.RemoveAt( i, true );
                    }
                }
                #endregion
            }
        }
        public int GetCPABunkerStrength()
        {
            AIDifficulty difficulty = this.SentinelInfo.AIDifficulty;
            //Bunker strength is "Current Wave Size" * AI Difficulty based multiplier * Game Harshness Multiplier
            int waveSize = this.GetSpecificBudgetThreshold(  AIBudgetType.Wave );
            int output = waveSize;
            int campaignMultiplier = AIWar2GalaxySettingTable.GetIsIntValueFromSettingByName_DuringGame( "DireCPAMultiplier" );
            output = (output * difficulty.BaseBunkerStrengthMultiplier * campaignMultiplier).IntValue;
            //ArcenDebugging.ArcenDebugLogSingleLine("base wave strength " + waveSize + " diff mult " + difficulty.BaseBunkerStrengthMultiplier + " harshness mult " + harshnessMultiplier + "  output: " + output + " (min 10 * 000)", Verbosity.DoNotShow );
            if ( output < 10 * 000 )
                output = 10 * 1000;  //never weaker than 10

            return output;
        }

        public Faction GetAiSubFaction( AIFactions aiSubType )
        {
            Faction result = null;

            switch(aiSubType)
            {
                case AIFactions.Sentinels:
                    result = this.AttachedFaction;
                    break;
                case AIFactions.Hunter:
                    result = SubFac_Hunter;
                    break;
                case AIFactions.Warden:
                    result = SubFac_Warden;
                    break;
                case AIFactions.Praetorian:
                    result = SubFac_Praetorian;
                    break;
                case AIFactions.BorderAggression:
                    result = SubFac_BorderAggression;
                    break;
                case AIFactions.CPA:
                    result = SubFac_CPA;
                    break;
                case AIFactions.Wave:
                    result = SubFac_RelentlessWave;
                    break;
                default:
                    throw new Exception( "Error: Value " + aiSubType.ToString() + " unknown!" );
            }

            return result;
        }

        public override FInt GetRatioOfInferiorityAtWhichAllGuardPostsDeploy()
        {
            return SentinelInfo.AIType.RatioOfInferiorityAtWhichAllGuardPostsDeploy;
        }

        public override void AppendStateForDebugDisplay( ArcenCharacterBufferBase buffer )
        {
			bool debug = false;
			if (!debug)
				return;
				
            FInt aip = GlobalAIWorldBaseInfo.Instance.AIProgress_Effective;
            FInt next_aip = aip + 20;

            // Waves
			{
	            var wave_income_persec = GetSpecificBudgetAIPurchaseCostGainPerSecond(AIBudgetType.Wave, true, true, aip);
	            var wave_interval = GetSpecificBudgetSpendingInterval(AIBudgetType.Wave, aip);
	            var wave_str = (int)(GetApproxStrengthCanPurchase((int)(wave_income_persec * wave_interval).ToFloatNonSim(), aip).ToFloatNonSim() / 1000.0f);

	            buffer.Add(string.Format("Waves: ~{0} str /{1} sec\n", wave_str, wave_interval));
	            
	            var wave_income_persec_2 = GetSpecificBudgetAIPurchaseCostGainPerSecond(AIBudgetType.Wave, true, true, next_aip);
	            var wave_str_2 = (int)(GetApproxStrengthCanPurchase((int)(wave_income_persec_2 * wave_interval).ToFloatNonSim(), next_aip).ToFloatNonSim() / 1000.0f);
	            var wave_str_inc = wave_str_2 - wave_str;

	            buffer.Add(string.Format("+20 aip would cause: +{0} str to waves\n", wave_str_inc));
			}
			
			// Warden
			{
	            int warden_time_interval = 30;
	            var warden_income_pertime = GetSpecificBudgetAIPurchaseCostGainPerSecond(AIBudgetType.Warden, true, true, aip) * FInt.FromParts(warden_time_interval, 0);
	            var warden_income_pertime_asstr = GetApproxStrengthCanPurchase(warden_income_pertime, aip).ToFloatNonSim();
	            var warden_cap = (int)WardenInfo.GetPopulationCap(aip).ToFloatNonSim() / 1000;

	            FInt tmp = FInt.Zero;
	            foreach ( Planet planet in World_AIW2.Instance.Planets( false ) )
	            {
	                var planetFaction = planet.GetPlanetFactionForFaction( this.SubFac_Warden );
	                tmp += planetFaction.DataByStance[FactionStance.Self].MobileStrength;
	            }
	            int warden_str = (int)(tmp.ToFloatNonSim() / 1000.0f);

	            var warden_cap_2 = (int)WardenInfo.GetPopulationCap(next_aip).ToFloatNonSim() / 1000;
	            var warden_cap_inc = warden_cap_2 - warden_cap;

	            var warden_income_pertime_2 = GetSpecificBudgetAIPurchaseCostGainPerSecond(AIBudgetType.Warden, true, true, next_aip) * FInt.FromParts(warden_time_interval, 0);
	            var warden_income_pertime_2_asstr = GetApproxStrengthCanPurchase(warden_income_pertime_2, next_aip).ToFloatNonSim();;
	            var warden_income_pertime__asstr_inc = warden_income_pertime_2_asstr - warden_income_pertime_asstr;

	            buffer.Add(string.Format("Warden: {0}/{1} str, building +{2} str/30sec\n", warden_str, warden_cap, (warden_income_pertime_asstr/1000.0f).ToString("0.00")));
	            buffer.Add(string.Format("+20 aip would +{0} str cap and +{1} str build/30sec\n", warden_cap_inc, (warden_income_pertime__asstr_inc/1000.0f).ToString("0.00")));
			}
        }

        public override void AppendStateForInterfaceDisplay( ArcenCharacterBufferBase buffer )
        {
            base.AppendStateForInterfaceDisplay( buffer );
        }

        public override void WriteFactionIdentityString( ArcenCharacterBufferBase buffer )
        {
            var fac = this.AttachedFaction;
            
            if (SentinelInfo.WriteAITypeDisplayString(buffer))
                buffer.Add(" ");
            
            buffer.Add( fac.GetDisplayNameInternal(true, false) );
        }
        
        public Faction GetFactionForBudget(AIBudgetType budgetType)
        {
            switch ( budgetType )
            {
                case AIBudgetType.Reinforcement:
                    return AttachedFaction;
                
                case AIBudgetType.CPA:
                    return SubFac_CPA;
                case AIBudgetType.Warden:
                    return SubFac_Warden;
                case AIBudgetType.HunterFleet:
                    return SubFac_Hunter;
                case AIBudgetType.PraetorianGuard:
                    return SubFac_Praetorian;
                
                case AIBudgetType.Reconquest: // um, actually reconquest waves are not relentless but .. wth are they?
                case AIBudgetType.Wave:
                case AIBudgetType.WormholeInvasion:
                    return SubFac_RelentlessWave;
                
                case AIBudgetType.BorderAggression:
                    return SubFac_BorderAggression;
                
                default:
                    return AttachedFaction;
            }
        }
    }
}
