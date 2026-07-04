
using Arcen.AIW2.Core;
using Arcen.Universal;

namespace Arcen.AIW2.External
{
    public class WildHivesFriendlyFactionBaseInfo : ExternalFactionBaseInfoRoot, IExternalBaseInfo_Singleton
    {
        public WildHivesFriendlyFactionBaseInfo() => Cleanup();

        protected override void Cleanup()
        {
            soldiers.Clear();
            hivesOnFriendlyPlanets.Clear();
            attackedPlanets.Clear();
        }

        #region (De)Serialization
        public override void SerializeFactionTo( SerMetaData MetaData, ArcenSerializationBuffer Buffer, SerializationCommandType SerializationCmdType )
        {

        }

        public override void DeserializeFactionIntoSelf( SerMetaData MetaData, ArcenDeserializationBuffer Buffer, SerializationCommandType SerializationCmdType )
        {

        }
        #endregion

        public override int GetDifficultyOrdinal_OrNegativeOneIfNotRelevant()
        {
            return -1;
        }

        public override float CalculateYourPortionOfPredictedGameLoad_Where100IsANormalAI( ArcenCharacterBufferBase OptionalExplainCalculation )
        {
            //Chris says: these are part of the main faction, don't tell us about this
            return 0;
        }

        protected override void DoFactionGeneralAggregationsPausedOrUnpaused()
        {
            Instance = this;
        }

        protected override void DoRefreshFromFactionSettings()
        {

        }

        public string SoldierTag => WildHivesFactionBaseInfo.SoldierTag ?? "NeinzulClanling";

        public int SecondsUntilNextSoldier( GameEntity_Squad hive ) => hive.GetSecondsSinceCreation() % WildHivesFactionBaseInfo.GetHighestDifficulty().secondsBetweenFriendlySoldierSpawns;

        public static WildHivesFriendlyFactionBaseInfo Instance;

        public readonly DoubleBufferedList<SafeSquadWrapper> soldiers = DoubleBufferedList<SafeSquadWrapper>.Create_WillNeverBeGCed( 300, "WildHivesFriendly-soldiers" );

        public readonly DoubleBufferedConcurrentList<GameEntity_Squad> hivesOnFriendlyPlanets = DoubleBufferedConcurrentList<GameEntity_Squad>.Create_WillNeverBeGCed( 100, "WildHivesFriendly-hivesOnFriendlyPlanets" );
        public readonly DoubleBufferedList<Planet> attackedPlanets = DoubleBufferedList<Planet>.Create_WillNeverBeGCed( 30, "WildHivesFriendlyFactionBaseInfo-attackedPlanets" );

        public override void DoPerSecondLogic_Stage1Clearing_OnMainThreadAndPartOfSim_ClientAndHost( ArcenClientOrHostSimContextCore Context )
        {
            // This is accessed by multiple factions in Stage2.
            hivesOnFriendlyPlanets.ClearConstructionListForStartingConstruction();
        }
        public override void DoPerSecondLogic_Stage2Aggregating_OnMainThreadAndPartOfSim_ClientAndHost( ArcenClientOrHostSimContextCore Context )
        {
            AllegianceHelper.AllyThisFactionToHumans( AttachedFaction );

            soldiers.ClearConstructionListForStartingConstruction();

            foreach ( GameEntity_Squad soldier in AttachedFaction.Squads( SoldierTag ) )
            {
                soldiers.AddToConstructionList( soldier );
            }

            soldiers.SwitchConstructionToDisplay();
        }

        public override void DoPerSecondLogic_Stage2APostAllFactionAggregating_OnMainThreadAndPartOfSim_ClientAndHost( ArcenClientOrHostSimContextCore Context )
        {
            // Added to in regular Stage2 by multiple factions.
            hivesOnFriendlyPlanets.SwitchConstructionToDisplay();

            attackedPlanets.ClearConstructionListForStartingConstruction();

            // Now, for each planet, if there is at least 1 strength of non-hive hostile strength on the planet, consider it attacked.
            foreach ( KeyValuePair<GameEntity_Squad, bool> _kv in hivesOnFriendlyPlanets.GetDisplayList() )
            {
                GameEntity_Squad workingHive = _kv.Key;
                if ( ArcenNetworkAuthority.GetIsHostMode() && workingHive.Planet.IntelLevel >= PlanetIntelLevel.CurrentlyWatched )
                    if ( WildHivesFactionBaseInfo.AllWildHiveFactions.Count == 1 )
                        World_AIW2.Instance.QueueLogJournalEntryToSidebar( WildHivesFactionBaseInfo.JournalFriendly, string.Empty, workingHive.PlanetFaction.Faction, null, workingHive.Planet, OnClient.DoThisOnHostOnly_WillBeSentToClients );
                    else if ( WildHivesFactionBaseInfo.AllWildHiveFactions.Count > 1 )
                        World_AIW2.Instance.QueueLogJournalEntryToSidebar( WildHivesFactionBaseInfo.JournalFriendlyMultiHive, string.Empty, workingHive.PlanetFaction.Faction, null, workingHive.Planet, OnClient.DoThisOnHostOnly_WillBeSentToClients );

                foreach ( Faction workingFaction in World_AIW2.Instance.Factions )
                {
                    if ( workingFaction == AttachedFaction )
                        continue; // Skip self.

                    if ( workingFaction.GetIsFriendlyTowards( AttachedFaction ) )
                        continue; // Skip if friendly. Players are friendly so this covers that.

                    if ( workingFaction.SpecialFactionData.InternalName == "NeinzulWildHives" )
                        continue; // Skip if hive.

                    if ( workingHive.Planet.GetPlanetFactionForFaction( workingFaction ).DataByStance[FactionStance.Self].TotalStrength >= 1000 )
                    {
                        attackedPlanets.AddToConstructionListIfNotAlreadyIn( workingHive.Planet );
                        break;
                    }
                }
            }

            attackedPlanets.SwitchConstructionToDisplay();
        }

        public override void DoPerSecondLogic_Stage4AdvancedAllegianceCode_OnMainThreadAndPartOfSim_ClientAndHost( ArcenClientOrHostSimContextCore Context )
        {
            // Never be hostile to other Wild Hives.
            foreach ( Faction workingFaction in World_AIW2.Instance.Factions )
            {
                if ( workingFaction.SpecialFactionData.InternalName == "NeinzulWildHives" )
                {
                    AttachedFaction.MakeFriendlyTo( workingFaction );
                    workingFaction.MakeFriendlyTo( AttachedFaction );
                }
            }
        }
    }
}
