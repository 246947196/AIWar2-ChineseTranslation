
using System;
using Arcen.AIW2.Core;
using Arcen.Universal;

namespace Arcen.AIW2.External
{
    public class NeinzulCustodiansFactionBaseInfo : ExternalFactionBaseInfoRoot
    {
        public bool IsEnabled => ConfigOption != "Disabled";
        public string EnclaveName = string.Empty;
        public int GameSecondOfLastInflux;

        public int SecondsSinceLastInflux => World_AIW2.Instance.GameSecond - GameSecondOfLastInflux;
        public int SecondsBetweenInflux => (AIAllied ? Difficulty.SecondsBetweenInfluxAI : PlayerAllied ? Difficulty.SecondsBetweenInfluxPlayer : Difficulty.SecondsBetweenInfluxNPC) + enclaves.Count * Difficulty.SecondsBetweenInfluxIncreasePerExistingEnclave;
        public bool ShouldHaveInflux => World_AIW2.Instance.GameSecond == Difficulty.SecondsBeforeFirstInflux || SecondsSinceLastInflux >= SecondsBetweenInflux;

        public readonly ArcenLessLinkedList<Fireteam> Teams = ArcenLessLinkedList<Fireteam>.Create_WillNeverBeGCed( "NeinzulCustodiansFactionBaseInfo-Teams" );

        public string ConfigOption;

        #region (De)Serialization
        public override void SerializeFactionTo( SerMetaData MetaData, ArcenSerializationBuffer Buffer, SerializationCommandType SerializationCmdType )
        {
            Buffer.AddString_Condensed( MetaData, EnclaveName, "Enclave Name" );
            Buffer.AddInt32( MetaData, ReadStyle.NonNeg, GameSecondOfLastInflux, "Game Second of Last Influx" );

            FireteamBaseUtility.SerializeFireteams( MetaData, Buffer, SerializationCmdType, Teams );
        }

        public override void DeserializeFactionIntoSelf( SerMetaData MetaData, ArcenDeserializationBuffer Buffer, SerializationCommandType SerializationCmdType )
        {
            EnclaveName = Buffer.ReadString_Condensed( MetaData, "Enclave Name" );
            GameSecondOfLastInflux = Buffer.ReadInt32( MetaData, ReadStyle.NonNeg, "Game Second of Last Influx" );

            FireteamBaseUtility.DeserializeFireteamsAndDiscardAnyExtraLeftovers( MetaData, Buffer, SerializationCmdType, Teams, "Neinzul Custodians" );
        }
        #endregion

        public NeinzulCustodiansFactionBaseInfo() => Cleanup();

        protected override void Cleanup()
        {
            EnclaveName = string.Empty;
            GameSecondOfLastInflux = 0;

            enclaves.Clear();
            clanlings.Clear();
            hives.Clear();
            hivesByPlanet.Clear();
            territory.Clear();

            ConfigOption = "Random";
        }

        public override int GetDifficultyOrdinal_OrNegativeOneIfNotRelevant()
        {
            return ParentInfo.Intensity;
        }

        protected override void DoFactionGeneralAggregationsPausedOrUnpaused()
        {

        }

        protected override void DoRefreshFromFactionSettings()
        {
            NeinzulCustodiansParentFactionBaseInfo parent = ParentInfo;
            //During deserialization fixup (FixDuringDeserForTemplate_AddAnyMissingFactions) a missing child faction can be created and refreshed before the parent's BaseInfo has been initialized, so the parent's static Instance isn't set yet.  Skip this pass; the standard post-deser init will refresh us again once the parent exists.
            if ( parent == null || parent.AttachedFaction == null )
                return;

            ConfigOption = parent.AttachedFaction.Config.GetStringValueForCustomFieldOrDefaultValue( AttachedFaction.SpecialFactionData.InternalName, true );

            GameEntityTypeData specificEnclaveType = GameEntityTypeDataTable.Instance.GetRowByNameOrNullIfNotFound( ConfigOption + "Class" );
            if ( specificEnclaveType != null )
                EnclaveName = specificEnclaveType.InternalName;
        }
        public override float CalculateYourPortionOfPredictedGameLoad_Where100IsANormalAI( ArcenCharacterBufferBase OptionalExplainCalculation )
        {
            //Chris says: these are part of the main faction, don't tell us about this
            return 0;
        }

        public NeinzulCustodiansParentFactionBaseInfo ParentInfo => NeinzulCustodiansParentFactionBaseInfo.Instance;
        public NeinzulCustodiansDifficulty Difficulty => ParentInfo.Difficulty;
        public byte FactionMarkLevel => (byte)Math.Min( 7, 1 + (World_AIW2.Instance.GameSecond / Difficulty.EntireFactionMarkUpEveryXSeconds) );
        public byte GetMarkLevelFor( GameEntity_Squad entity ) => (byte)Math.Min( 7, FactionMarkLevel + (entity.GetSecondsSinceCreation() / Difficulty.IndividualUnitMarkUpAfterAliveEveryXSeconds) );

        public bool PlayerAllied => Allegiance == "对玩家友好";
        public bool AIAllied => Allegiance == "对AI友好";
        public int EveryXFireteamAsDefense => PlayerAllied ? ParentInfo.Behavior_Player_EveryXFireteamAsDefense : ParentInfo.Behavior_NPC_EveryXFireteamAsDefense;
        public int AttackHops => PlayerAllied ? ParentInfo.Behavior_Player_HopsFromTerritoryToAttack : ParentInfo.Behavior_NPC_HopsFromTerritoryToAttack;
        public FInt HullRetreatRatio => PlayerAllied ? ParentInfo.Behavior_Player_HullRatioToRetreatAt : ParentInfo.Behavior_NPC_HullRatioToRetreatAt;
        public int OverkillRatio => PlayerAllied ? ParentInfo.Behavior_Player_OverkillRatio : ParentInfo.Behavior_NPC_OverkillRatio;

        public int MaxCustodians => (Difficulty.MaximumEnclaves_Base +
            ((World_AIW2.Instance.GameSecond / 60 / 60) * Difficulty.MaximumEnclaves_IncreasePerHour) +
            (FactionUtilityMethods.Instance.GetCurrentAIP() * Difficulty.MaximumEnclaves_IncreasePer100AIP)).GetNearestIntPreferringLower();

        public int MaxClanlingsPerCustodian => (Difficulty.MaximumClanlingsPerEnclave_Base +
            ((World_AIW2.Instance.GameSecond / 60 / 60) * Difficulty.MaximumClanlingsPerEnclave_IncreasePerHour) +
            (FactionUtilityMethods.Instance.GetCurrentAIP() * Difficulty.MaximumClanlingsPerEnclave_IncreasePer100AIP)).GetNearestIntPreferringLower();

        public override void UpdatePowerLevel()
        {
            FInt calculated = FInt.FromParts( 0, 010 ) * enclaves.GetDisplayList().Count;
            if ( calculated > FInt.One )
                AttachedFaction.OverallPowerLevel = FInt.One;
            else
                AttachedFaction.OverallPowerLevel = calculated;
        }

        #region Constants
        public readonly string EnclaveTag = "CustodianEnclave";
        public readonly string ClanlingTag = "NeinzulClanling";
        public readonly string HiveTag = "CustodianHive";
        #endregion

        public readonly DoubleBufferedList<SafeSquadWrapper> enclaves = DoubleBufferedList<SafeSquadWrapper>.Create_WillNeverBeGCed( 100, "NeinzulCustodians-enclaves" );
        public readonly DoubleBufferedList<SafeSquadWrapper> clanlings = DoubleBufferedList<SafeSquadWrapper>.Create_WillNeverBeGCed( 200, "NeinzulCustodians-clanlings" );
        public readonly DoubleBufferedList<SafeSquadWrapper> hives = DoubleBufferedList<SafeSquadWrapper>.Create_WillNeverBeGCed( 100, "NeinzulCustodians-hives" );
        public readonly DoubleBufferedDictionary<Planet, short> hivesByPlanet = DoubleBufferedDictionary<Planet, short>.Create_WillNeverBeGCed( 100, "NeinzulCustodians-hivesByPlanet" );

        public readonly DoubleBufferedList<Planet> territory = DoubleBufferedList<Planet>.Create_WillNeverBeGCed( 50, "NeinzulCustodians-territory" );

        public override void DoPerSecondLogic_Stage2Aggregating_OnMainThreadAndPartOfSim_ClientAndHost( ArcenClientOrHostSimContextCore Context )
        {
            enclaves.ClearConstructionListForStartingConstruction();
            clanlings.ClearConstructionListForStartingConstruction();
            hives.ClearConstructionListForStartingConstruction();
            hivesByPlanet.ClearConstructionDictForStartingConstruction();
            territory.ClearConstructionListForStartingConstruction();

            foreach ( GameEntity_Squad entity in AttachedFaction.Squads() )
            {
                if ( entity.TypeData.GetHasTag( EnclaveTag ) )
                    enclaves.AddToConstructionList( entity );
                else if ( entity.TypeData.GetHasTag( ClanlingTag ) )
                    clanlings.AddToConstructionList( entity );
                else if ( entity.TypeData.GetHasTag( HiveTag ) )
                {
                    hives.AddToConstructionList( entity );
                    territory.AddToConstructionListIfNotAlreadyIn( entity.Planet );
                    if ( hivesByPlanet.ConstructionContainsKey( entity.Planet ) )
                        hivesByPlanet.Construction[entity.Planet]++;
                    else
                        hivesByPlanet.SetToConstructionDict( entity.Planet, 1 );
                }

            }

            enclaves.SwitchConstructionToDisplay();
            clanlings.SwitchConstructionToDisplay();
            hives.SwitchConstructionToDisplay();
            hivesByPlanet.SwitchConstructionToDisplay();
            territory.SwitchConstructionToDisplay();
        }

        public override void DoPerSecondLogic_Stage3Main_OnMainThreadAndPartOfSim_ClientAndHost( ArcenClientOrHostSimContextCore Context )
        {
            UpdateAllegiance();
        }

        private void UpdateAllegiance()
        {
            switch ( Allegiance )
            {
                case "对AI友好":
                    AllegianceHelper.AllyThisFactionToAI( AttachedFaction );
                    break;
                case "对玩家友好":
                    AllegianceHelper.AllyThisFactionToHumans( AttachedFaction );
                    break;
                default:
                    AllegianceHelper.AllyThisFactionToMinorFactionTeam( AttachedFaction, Allegiance );
                    break;
            }
        }

        public override Fireteam GetFireteamById( int id )
        {
            return FireteamBaseUtility.GetFireteamById( Teams, id );
        }
    }
}
