using Arcen.AIW2.Core;
using Arcen.Universal;
using System;

using System.Text;

namespace Arcen.AIW2.External
{
    public class AIWardenFactionBaseInfo : AISubFactionBaseInfo
    {
        //serialized

        //non-serialized

        //constants

        public AIWardenFactionBaseInfo()
        {
            Cleanup();
        }

        protected override void SubCleanup()
        {
        }

        protected override void SubDeserializeFactionIntoSelf( SerMetaData MetaData, ArcenDeserializationBuffer Buffer, SerializationCommandType SerializationCmdType )
        {
        }

        protected override void SubSerializeFactionTo( SerMetaData MetaData, ArcenSerializationBuffer Buffer, SerializationCommandType SerializationCmdType )
        {
        }

        public override int GetDifficultyOrdinal_OrNegativeOneIfNotRelevant()
        {
            return this.ParentBaseInfo.WardenInfo.AIDifficulty.Difficulty; //wardens have their own difficulty
        }

        public override float CalculateYourPortionOfPredictedGameLoad_Where100IsANormalAI( ArcenCharacterBufferBase OptionalExplainCalculation )
        {
            //Chris says: these are part of the main faction, don't tell us about this here
            return 0;
        }

        #region SetStartingFactionRelationships
        public override void SetStartingFactionRelationships()
        {
            base.SetStartingFactionRelationships();
            Faction faction = AttachedFaction;
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
                        faction.MakeHostileTo( otherFaction );
                        otherFaction.MakeHostileTo( faction );
                        break;
                    case FactionType.AI:
                        faction.MakeFriendlyTo( otherFaction );
                        otherFaction.MakeFriendlyTo( faction );
                        break;
                    case FactionType.SpecialFaction:
                        if ( faction.SpecialFactionData.AlliedToAIByDefault &&
                             otherFaction.SpecialFactionData.AlliedToAIByDefault )
                        {
                            faction.MakeFriendlyTo( otherFaction );
                            otherFaction.MakeFriendlyTo( faction );
                        }
                        else
                        {
                            faction.MakeHostileTo( otherFaction );
                            otherFaction.MakeHostileTo( faction );
                        }
                        break;
                }
            }
        }
        #endregion

        #region SubDoGeneralAggregationsPausedOrUnpaused
        protected override void SubDoGeneralAggregationsPausedOrUnpaused()
        {
        }
        #endregion

        #region SubDoPerSecondLogic_Stage2Aggregating_OnMainThreadAndPartOfSim_ClientAndHost
        protected override void SubDoPerSecondLogic_Stage2Aggregating_OnMainThreadAndPartOfSim_ClientAndHost( ArcenClientOrHostSimContextCore Context )
        {
        }
        #endregion

        #region GetWardenFleetStateForDisplay
        public void GetWardenFleetStateForDisplay( ArcenDoubleCharacterBuffer output )
        {
            AIWardenCoreData factionExternal = this.AttachedFaction.GetAISentinelsCoreData().WardenInfo;
            if ( factionExternal != null && factionExternal.Teams.GetItemCount() > 0 )
            {
                output.Add( "\nState of Warden Fireteams for  <" ).Add( this.AttachedFaction.FactionIndex ).Add( ">:\n" );
                int totalStrength = 0;
                foreach ( Fireteam team in Fireteam.LiveTeamsIn( factionExternal.Teams ) )
                {
                    totalStrength += team.DeepInfo.TeamStrength;
                    if ( team.status != FireteamStatus.Disbanded )
                    {
                        team.DeepInfo.GetStringForDisplay( output );
                        output.Add( "\n" );
                    }
                }
                output.Add( "Total Warden Strength: " ).Add( (totalStrength / 1000).ToString(), "ff0000" ).Add( ".\n" );
            }
        }
        #endregion

        #region GetFireteamById
        public override Fireteam GetFireteamById( int id )
        {
            AIWardenCoreData factionExternal = this.AttachedFaction.GetAISentinelsCoreData().WardenInfo;
            return FireteamBaseUtility.GetFireteamById( factionExternal.Teams, id );
        }
        #endregion

        public override PlanetPathfinder GetNormalPathfinderThatMustBeReleased()
        {
            return this.ParentBaseInfo.WardenInfo.GetPathfinderThatMustBeReleased();
        }

        public override PlanetPathfinder GetConservativePathfinderThatMustBeReleasedOrNull()
        {
            return null;
        }
    }
}
