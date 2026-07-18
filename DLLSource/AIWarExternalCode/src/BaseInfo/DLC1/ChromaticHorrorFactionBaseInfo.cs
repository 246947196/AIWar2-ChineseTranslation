using Arcen.AIW2.Core;
using Arcen.Universal;
using System;

using System.Text;

namespace Arcen.AIW2.External
{
    public class ChromaticHorrorFactionBaseInfo : LoneWandererFactionBaseInfo
    {
        //not serialized

        //constants
        public override int RespawnTime => 5; //ha, it's not staying gone for long.
        public override string LoneUnitTag => "ChromaticHorror";

        public ChromaticHorrorFactionBaseInfo()
        {
            Cleanup();
        }

        protected override void SubCleanup()
        {
        }

        protected override void SubSerializeFactionTo( SerMetaData MetaData, ArcenSerializationBuffer Buffer, SerializationCommandType SerializationCmdType )
        {
        }

        protected override void SubDeserializeFactionIntoSelf( SerMetaData MetaData, ArcenDeserializationBuffer Buffer, SerializationCommandType SerializationCmdType )
        {
        }

        public override int GetDifficultyOrdinal_OrNegativeOneIfNotRelevant()
        {
            return -1; //not relevant for ChromaticHorror golem
        }

        public override float CalculateYourPortionOfPredictedGameLoad_Where100IsANormalAI( ArcenCharacterBufferBase OptionalExplainCalculation )
        {
            if ( OptionalExplainCalculation != null )
                OptionalExplainCalculation.Add( "5 来自多彩恐惧的负载" );
            return 5;
        }

        protected override void DoFactionGeneralAggregationsPausedOrUnpaused()
        {

        }

        #region DoRefreshFromFactionSettings
        protected override void DoRefreshFromFactionSettings()
        {
        }
        #endregion

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

        protected override void SubDoPerSecondLogic_Stage2Aggregating_OnMainThreadAndPartOfSim_ClientAndHost( ArcenClientOrHostSimContextCore Context )
        {

        }

        //This is what we set the ChromaticHorror to every frame
        protected override void GivePerStanceStanceOrdersToFoundWanderers( GameEntity_Squad entity, ArcenClientOrHostSimContextCore Context )
        {
            entity.Orders.SetBehaviorDirectlyInSim( EntityBehaviorType.Attacker_Full ); //is fine, main sim thread
            entity.ShouldNotBeConsideredAsThreatToHumanTeam = true;
        }
    }
}