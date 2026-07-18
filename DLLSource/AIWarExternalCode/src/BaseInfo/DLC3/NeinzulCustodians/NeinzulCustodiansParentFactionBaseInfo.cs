
using Arcen.AIW2.Core;
using Arcen.Universal;

namespace Arcen.AIW2.External
{
    public class NeinzulCustodiansParentFactionBaseInfo : ExternalFactionBaseInfoRoot, IExternalBaseInfo_Singleton
    {
        #region (De)Serialization
        public override void SerializeFactionTo( SerMetaData MetaData, ArcenSerializationBuffer Buffer, SerializationCommandType SerializationCmdType )
        {

        }

        public override void DeserializeFactionIntoSelf( SerMetaData MetaData, ArcenDeserializationBuffer Buffer, SerializationCommandType SerializationCmdType )
        {

        }
        #endregion

        public NeinzulCustodiansParentFactionBaseInfo() => Cleanup();

        protected override void Cleanup()
        {
            Intensity = 0;
            Difficulty = null;

            enclaveTypes.Clear();

            Behavior_Player_HopsFromTerritoryToAttack = 3;
            Behavior_Player_HullRatioToRetreatAt = FInt.One / 3;
            Behavior_Player_EveryXFireteamAsDefense = 4;
            Behavior_Player_OverkillRatio = 3;

            Behavior_NPC_HopsFromTerritoryToAttack = 3;
            Behavior_NPC_HullRatioToRetreatAt = FInt.One / 3;
            Behavior_NPC_EveryXFireteamAsDefense = 4;
            Behavior_NPC_OverkillRatio = 3;
        }

        public override int GetDifficultyOrdinal_OrNegativeOneIfNotRelevant()
        {
            return Intensity;
        }

        public override float CalculateYourPortionOfPredictedGameLoad_Where100IsANormalAI( ArcenCharacterBufferBase OptionalExplainCalculation )
        {
            if ( OptionalExplainCalculation != null )
                OptionalExplainCalculation.Add( "90 来自奈因祖尔守护者的负载" );
            return 90;
        }

        protected override void DoFactionGeneralAggregationsPausedOrUnpaused()
        {
            Instance = this;
        }

        public int Intensity;
        public static NeinzulCustodiansParentFactionBaseInfo Instance;

        #region Seeding
        // Used in seeding to give out units.
        // Could theoretically be done in a static, but this feels better here?
        public readonly List<GameEntityTypeData> enclaveTypes = List<GameEntityTypeData>.Create_WillNeverBeGCed( 6, "NeinzulCustodians-enclaveTypes", 1 );
        #endregion

        #region Behavior
        public int Behavior_Player_HopsFromTerritoryToAttack;
        public FInt Behavior_Player_HullRatioToRetreatAt;
        public int Behavior_Player_EveryXFireteamAsDefense;
        public int Behavior_Player_OverkillRatio;

        public int Behavior_NPC_HopsFromTerritoryToAttack;
        public FInt Behavior_NPC_HullRatioToRetreatAt;
        public int Behavior_NPC_EveryXFireteamAsDefense;
        public int Behavior_NPC_OverkillRatio;
        #endregion

        public NeinzulCustodiansDifficulty Difficulty { get; private set; }

        #region Lobby Settings
        private const string LogicPreset = "LogicPreset";
        private const string UseCustomLogicForHumanAllies = "UseCustomLogicForHumanAllies";

        private const string HopsFromTerritoryToAttack = "HopsFromTerritoryToAttack";
        private const string HullRatioToRetreatAt = "HullRatioToRetreatAt";
        private const string EveryXFireteamsAsDefense = "EveryXFireteamsAsDefense";
        private const string OverkillRatio = "OverkillRatio";
        #endregion

        protected override void DoRefreshFromFactionSettings()
        {
            var config = this.AttachedFaction.Config;
            Intensity = config.GetIntValueForCustomFieldOrDefaultValue( "Intensity", true );
            if ( Difficulty == null )
                Difficulty = NeinzulCustodiansDifficultyTable.Instance.GetRowForFaction( AttachedFaction );

            #region LogicPresets
            switch ( config.GetStringValueForCustomFieldOrDefaultValue(LogicPreset, true) )
            {
                case "Hunter":
                    Behavior_Player_HopsFromTerritoryToAttack = 4;
                    Behavior_NPC_HopsFromTerritoryToAttack = 4;
                    Behavior_Player_HullRatioToRetreatAt = FInt.One / 4;
                    Behavior_NPC_HullRatioToRetreatAt = FInt.One / 4;
                    Behavior_Player_EveryXFireteamAsDefense = 5;
                    Behavior_NPC_EveryXFireteamAsDefense = 5;
                    Behavior_Player_OverkillRatio = 4;
                    Behavior_NPC_OverkillRatio = 4;
                    break;
                case "Warden":
                    Behavior_Player_HopsFromTerritoryToAttack = 2;
                    Behavior_NPC_HopsFromTerritoryToAttack = 2;
                    Behavior_Player_HullRatioToRetreatAt = FInt.One / 2;
                    Behavior_NPC_HullRatioToRetreatAt = FInt.One / 2;
                    Behavior_Player_EveryXFireteamAsDefense = 3;
                    Behavior_NPC_EveryXFireteamAsDefense = 3;
                    Behavior_Player_OverkillRatio = 2;
                    Behavior_NPC_OverkillRatio = 2;
                    break;
                case "Berserk":
                    Behavior_Player_HopsFromTerritoryToAttack = 6;
                    Behavior_NPC_HopsFromTerritoryToAttack = 6;
                    Behavior_Player_HullRatioToRetreatAt = FInt.One / 20;
                    Behavior_NPC_HullRatioToRetreatAt = FInt.One / 20;
                    Behavior_Player_EveryXFireteamAsDefense = 99999;
                    Behavior_NPC_EveryXFireteamAsDefense = 99999;
                    Behavior_Player_OverkillRatio = 1;
                    Behavior_NPC_OverkillRatio = 1;
                    break;
                case "Coward":
                    Behavior_Player_HopsFromTerritoryToAttack = 2;
                    Behavior_NPC_HopsFromTerritoryToAttack = 2;
                    Behavior_Player_HullRatioToRetreatAt = FInt.FromParts( 0, 900 );
                    Behavior_NPC_HullRatioToRetreatAt = FInt.FromParts(0, 900);
                    Behavior_Player_EveryXFireteamAsDefense = 2;
                    Behavior_NPC_EveryXFireteamAsDefense = 2;
                    Behavior_Player_OverkillRatio = 10;
                    Behavior_NPC_OverkillRatio = 10;
                    break;
                default:
                    Behavior_Player_HopsFromTerritoryToAttack = 3;
                    Behavior_NPC_HopsFromTerritoryToAttack = 3;
                    Behavior_Player_HullRatioToRetreatAt = FInt.One / 3;
                    Behavior_NPC_HullRatioToRetreatAt = FInt.One / 3;
                    Behavior_Player_EveryXFireteamAsDefense = 4;
                    Behavior_NPC_EveryXFireteamAsDefense = 4;
                    Behavior_Player_OverkillRatio =  3;
                    Behavior_NPC_OverkillRatio = 3;
                    break;
            }
            #endregion

            if ( config.GetBoolValueForCustomFieldOrDefaultValue(UseCustomLogicForHumanAllies, true) )
            {
                Behavior_Player_HopsFromTerritoryToAttack = config.GetIntValueForCustomFieldOrDefaultValue( HopsFromTerritoryToAttack, true );
                Behavior_Player_HullRatioToRetreatAt = config.GetIntValueForCustomFieldOrDefaultValue( HullRatioToRetreatAt, true ) / (FInt.One * 100);
                Behavior_Player_EveryXFireteamAsDefense = config.GetIntValueForCustomFieldOrDefaultValue( EveryXFireteamsAsDefense, true );
                Behavior_Player_OverkillRatio = config.GetIntValueForCustomFieldOrDefaultValue( OverkillRatio, true );
            }
        }
    }
}
