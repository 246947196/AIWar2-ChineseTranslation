using Arcen.AIW2.Core;
using Arcen.Universal;
using System;

using System.Text;

namespace Arcen.AIW2.External
{
    public class AIHunterFactionBaseInfo : AISubFactionBaseInfo
    {
        //serialized

        //constants

        public AIHunterFactionBaseInfo()
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
            return this.ParentBaseInfo.HunterInfo.AIDifficulty.Difficulty; //hunters have their own difficulty
        }

        public override float CalculateYourPortionOfPredictedGameLoad_Where100IsANormalAI( ArcenCharacterBufferBase OptionalExplainCalculation )
        {
            //Chris says: these are part of the main faction, don't tell us about this here
            return 0;
        }

        #region SubDoGeneralAggregationsPausedOrUnpaused
        protected override void SubDoGeneralAggregationsPausedOrUnpaused()
        {
        }
        #endregion

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

        #region SubDoPerSecondLogic_Stage2Aggregating_OnMainThreadAndPartOfSim_ClientAndHost
        protected override void SubDoPerSecondLogic_Stage2Aggregating_OnMainThreadAndPartOfSim_ClientAndHost( ArcenClientOrHostSimContextCore Context )
        {
        }
        #endregion

        #region GetFireteamById
        public override Fireteam GetFireteamById( int id )
        {
            AIHunterCoreData hunterInfo = this.ParentBaseInfo.HunterInfo;
            return FireteamBaseUtility.GetFireteamById( hunterInfo.Teams, id );
        }
        #endregion

        #region GetHunterFleetStateForDisplay
        public void GetHunterFleetStateForDisplay( ArcenDoubleCharacterBuffer output )
        {
            Dictionary<Planet, int> strengthPerPlanet = Planet.GetTemporaryPlanetDictOfInts( "AIHunterFactionBaseInfo-GetHunterFleetStateForDisplay-strengthPerPlanet", 10f );
            if ( strengthPerPlanet == null ) //blocked for teardown/shutdown; bail
                return;

            int total = 0;
            foreach ( GameEntity_Squad entity in this.AttachedFaction.Squads() )
            {
                if ( !strengthPerPlanet.ContainsKey( entity.Planet ) )
                    strengthPerPlanet[entity.Planet] = 0;

                int strengthOfEntityAndContents = entity.GetStrengthOfSelfAndContents();
                strengthPerPlanet[entity.Planet] += strengthOfEntityAndContents;
                total += strengthOfEntityAndContents;
            }

            List<KeyValuePair<Planet, int>> strengthPerPlanetSorted = strengthPerPlanet.SortIntoList( delegate ( KeyValuePair<Planet, int> L, KeyValuePair<Planet, int> R )
            {
                return R.Value.CompareTo( L.Value );
            } );

            AIHunterCoreData hunterInfo = this.ParentBaseInfo.HunterInfo;
            if ( !hunterInfo.SubType.UseFireteams )
            {
                for ( int i = 0; i < strengthPerPlanetSorted.Count; i++ )
                {
                    KeyValuePair<Planet, int> pair = strengthPerPlanetSorted[i];
                    output.Add( pair.Key.Name, "a1ffa1" ).Add( ": " ).AddNumberMoreReadable( pair.Value, "ff0000" ).Add( "\n" );
                }

                Planet currentTargetPlanet = World_AIW2.Instance.GetPlanetByIndex( (Int16)hunterInfo.CurrentTargetPlanetIndex );
                if ( currentTargetPlanet == null )
                    output.Add( "Null Target Planet\n\n" );
                else
                    output.Add( "Total: " ).AddNumberMoreReadable( total, "ff0000" ).Add( " Target Planet " ).Add( currentTargetPlanet.Name, "a1ffa1" ).Add( "\n\n" );
            }
            else
            {
                output.Add( "\nState of Hunter Fireteams for  <" ).Add( this.AttachedFaction.FactionIndex ).Add( ">:\n" );
                int totalStrength = 0;
                foreach ( Fireteam team in Fireteam.LiveTeamsIn( hunterInfo.Teams ) )
                {
                    totalStrength += team.DeepInfo.TeamStrength;
                    if ( team.status != FireteamStatus.Disbanded )
                    {
                        team.DeepInfo.GetStringForDisplay( output );
                        output.Add( "\n" );
                    }
                }
                output.Add( "Total Hunter Strength: " ).Add( (totalStrength / 1000).ToString(), "ff0000" ).Add( ".\n" );
            }
            Planet.ReleaseTemporaryPlanetDictOfInts( strengthPerPlanet );
        }
        #endregion

        public override PlanetPathfinder GetNormalPathfinderThatMustBeReleased()
        {
            return this.ParentBaseInfo.HunterInfo.GetPathfinderThatMustBeReleased();
        }

        public override PlanetPathfinder GetConservativePathfinderThatMustBeReleasedOrNull()
        {
            return null;
        }
    }
}
