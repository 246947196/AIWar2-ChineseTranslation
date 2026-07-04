using Arcen.AIW2.Core;
using Arcen.Universal;
using System;

using System.Text;

namespace Arcen.AIW2.External
{
    public class InstigatorFactionBaseInfo : ExternalFactionBaseInfoRoot, IExternalBaseInfo_Singleton
    {
        //serialized
        public int TimeForNextInstigatorBaseSpawn;
        public int AIFactionIndexForNextSpawn;

        //not serialized
        public static InstigatorFactionBaseInfo Instance; //there can only ever be one of this faction at a time

        public InstigatorFactionBaseInfo()
        {
            Cleanup();
        }

        protected override void Cleanup()
        {
            TimeForNextInstigatorBaseSpawn = -1;
            AIFactionIndexForNextSpawn = -1;

            Instance = null;

            HaveLoadedData = false; //force reload
        }

        #region Xml Info
        private bool HaveLoadedData;
        public int IntervalBetweenBaseSpawns = 0; //the one we use
        public int IntervalBetweenBaseSpawnsLow = 0;
        public int IntervalBetweenBaseSpawnsMed = 0;
        public int IntervalBetweenBaseSpawnsHigh = 0;
        public int TimeForInitialBaseSpawn = 0;

        private void LoadCustomDataIfNeeded()
        {
            if ( this.HaveLoadedData )
                return;
            this.HaveLoadedData = true;
            IntervalBetweenBaseSpawnsLow = ExternalConstants.Instance.GetCustomInt32_Slow( "custom_int_Instigators_IntervalBetweenBaseSpawnsLow" );
            IntervalBetweenBaseSpawnsMed = ExternalConstants.Instance.GetCustomInt32_Slow( "custom_int_Instigators_IntervalBetweenBaseSpawnsMed" );
            IntervalBetweenBaseSpawnsHigh = ExternalConstants.Instance.GetCustomInt32_Slow( "custom_int_Instigators_IntervalBetweenBaseSpawnsHigh" );
            TimeForInitialBaseSpawn = ExternalConstants.Instance.GetCustomInt32_Slow( "custom_int_Instigators_TimeForInitialBaseSpawn" );
        }
        #endregion

        #region Ser / Deser
        public override void SerializeFactionTo( SerMetaData MetaData, ArcenSerializationBuffer Buffer, SerializationCommandType SerializationCmdType )
        {
            Buffer.AddInt32( MetaData, ReadStyle.PosExceptNeg1, this.TimeForNextInstigatorBaseSpawn );
            Buffer.AddInt32( MetaData, ReadStyle.PosExceptNeg1, this.AIFactionIndexForNextSpawn );
        }

        public override void DeserializeFactionIntoSelf( SerMetaData MetaData, ArcenDeserializationBuffer Buffer, SerializationCommandType SerializationCmdType )
        {
            this.TimeForNextInstigatorBaseSpawn = Buffer.ReadInt32( MetaData, ReadStyle.PosExceptNeg1 );
            AIFactionIndexForNextSpawn = Buffer.ReadInt32( MetaData, ReadStyle.PosExceptNeg1 );
        }
        #endregion

        public override int GetDifficultyOrdinal_OrNegativeOneIfNotRelevant()
        {
            return -1;
        }

        public override float CalculateYourPortionOfPredictedGameLoad_Where100IsANormalAI( ArcenCharacterBufferBase OptionalExplainCalculation )
        {
            //Chris says: these are always here, don't tell us about this
            return 0;
        }

        #region DoFactionGeneralAggregationsPausedOrUnpaused
        protected override void DoFactionGeneralAggregationsPausedOrUnpaused()
        {
            Instance = this;
            LoadCustomDataIfNeeded();
        }
        #endregion

        #region DoRefreshFromFactionSettings
        protected override void DoRefreshFromFactionSettings()
        {
        }
        #endregion

        #region SetStartingFactionRelationships
        public override void SetStartingFactionRelationships()
        {
            //these are friendly with everyone since only the player should interact with them
            //... but ... factions that are always allied to the player should also be able to kill us
            base.SetStartingFactionRelationships();
            Faction instigators = this.AttachedFaction;
            for ( int i = 0; i < World_AIW2.Instance.Factions.Count; i++ )
            {
                Faction otherFaction = World_AIW2.Instance.Factions[i];
                if ( instigators == otherFaction )
                    continue;
                if ( otherFaction.Type == FactionType.NaturalObject )
                    continue;

                switch ( otherFaction.Type )
                {
                    case FactionType.Player:
                        instigators.MakeHostileTo( otherFaction );
                        otherFaction.MakeHostileTo( instigators );
                        break;
                    case FactionType.AI:
                        instigators.MakeFriendlyTo( otherFaction );
                        otherFaction.MakeFriendlyTo( instigators );
                        break;
                    case FactionType.SpecialFaction:
                        if (otherFaction.GetIsFriendlyToAnyPlayerFaction())
                        {
                            instigators.MakeHostileTo( otherFaction );
                            otherFaction.MakeHostileTo( instigators );
                        }
                        else
                        {
                            instigators.MakeFriendlyTo( otherFaction );
                            otherFaction.MakeFriendlyTo( instigators );
                        }
                        break;
                }
            }
        }
        #endregion

        #region GetInstigatorBases_Threadsafe
        public static void GetInstigatorBases_Threadsafe( List<SafeSquadWrapper> ListToFill )
        {
            ListToFill.Clear();
            if ( Instance == null )
                return;
            foreach ( GameEntity_Squad entity in Instance.AttachedFaction.Squads( "InstigatorBase" ) )
            {
                ListToFill.Add( entity );
            }
        }
        #endregion
    }
}
