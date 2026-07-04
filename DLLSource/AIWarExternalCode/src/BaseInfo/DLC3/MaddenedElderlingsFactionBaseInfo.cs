using Arcen.AIW2.Core;
using Arcen.Universal;
using System;


using System.Text;

namespace Arcen.AIW2.External
{
    public class MaddenedElderlingsFactionBaseInfo : ExternalFactionBaseInfoRoot
    {
        //serialized


        //not serialized
        public readonly DoubleBufferedList<SafeSquadWrapper> Elderlings = DoubleBufferedList<SafeSquadWrapper>.Create_WillNeverBeGCed( 30, "MaddenedElderlings-Elderlings" );

        public MaddenedElderlingsFactionBaseInfo()
        {
            Cleanup();
        }
        #region Serialization and Deserialization
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

        protected override void Cleanup()
        {
            Elderlings.Clear();
        }
        #region DoRefreshFromFactionSettings        
        protected override void DoRefreshFromFactionSettings()
        {
            //ConfigurationForFaction cfg = this.AttachedFaction.Config;
        }
        #endregion

        #region Xml Data
        public bool HaveLoadedData = false;
        private void LoadCustomDataIfNeeded()
        {
            if ( this.HaveLoadedData )
                return;
            this.HaveLoadedData = true;

        }
        #endregion

        #region DoFactionGeneralAggregationsPausedOrUnpaused
        protected override void DoFactionGeneralAggregationsPausedOrUnpaused()
        {
            this.LoadCustomDataIfNeeded();
        }
        #endregion


        #region DoPerSecondLogic_Stage2Aggregating_OnMainThreadAndPartOfSim_ClientAndHost
        public override void DoPerSecondLogic_Stage2Aggregating_OnMainThreadAndPartOfSim_ClientAndHost( ArcenClientOrHostSimContextCore Context )
        {
            int debugCode = 0;
            try
            {
                debugCode = 100;
                debugCode = 200;
                debugCode = 300;
                debugCode = 400;

                Elderlings.ClearConstructionListForStartingConstruction();

                foreach ( GameEntity_Squad entity in this.AttachedFaction.Squads( "Elderling" ) )
                {
                    Elderlings.AddToConstructionList( entity );
                }

            }
            catch ( Exception e )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "Hit exception debugCode " + debugCode + " in elderlings stage 2 " + e.ToString(), Verbosity.DoNotShow );
            }
        }
        #endregion end DoPerSecondLogic_Stage2Aggregating_OnMainThreadAndPartOfSim_ClientAndHost

        #region SetStartingFactionRelationships
        public override void SetStartingFactionRelationships()
        {
            base.SetStartingFactionRelationships();
            Faction faction = this.AttachedFaction;
            for ( int i = 0; i < World_AIW2.Instance.Factions.Count; i++ )
            {
                Faction otherFaction = World_AIW2.Instance.Factions[i];
                if ( faction == otherFaction )
                    continue;
                if ( otherFaction.Type == FactionType.NaturalObject )
                    continue;

                switch ( otherFaction.Type )
                {
                  case FactionType.Player:
                  case FactionType.AI:
                  case FactionType.SpecialFaction:
                    faction.MakeHostileTo( otherFaction );
                    otherFaction.MakeHostileTo( faction );
                    break;
                }
            }
        }
        #endregion


    }
}
