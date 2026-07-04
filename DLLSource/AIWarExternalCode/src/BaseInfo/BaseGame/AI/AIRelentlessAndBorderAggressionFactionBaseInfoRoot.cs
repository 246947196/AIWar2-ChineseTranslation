using Arcen.AIW2.Core;
using Arcen.Universal;
using System;

using System.Text;

namespace Arcen.AIW2.External
{
    public class AIRelentlessAndBorderAggressionFactionBaseInfoRoot : AISubFactionBaseInfo
    {
        //serialized

        //non-serialized
        public static readonly DoubleBufferedDictionary<Planet, int> StrengthPerPlanet_TracingOnly = DoubleBufferedDictionary<Planet, int>.Create_WillNeverBeGCed( 300, "RelentAndBord-StrengthPerPlanet_TracingOnly" );

        //constants

        public AIRelentlessAndBorderAggressionFactionBaseInfoRoot()
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
            return this.ParentBaseInfo.SentinelInfo.AIDifficulty.Difficulty; //this one just pulls its difficulty from the parent
        }

        public override float CalculateYourPortionOfPredictedGameLoad_Where100IsANormalAI( ArcenCharacterBufferBase OptionalExplainCalculation )
        {
            //Chris says: skip telling us about this faction, because it's considered a part of the AI.  Tell us about the load of the AI there.
            return 0;
        }

        #region SubDoGeneralAggregationsPausedOrUnpaused
        protected override void SubDoGeneralAggregationsPausedOrUnpaused()
        {
        }
        #endregion

        #region SubDoPerSecondLogic_Stage2Aggregating_OnMainThreadAndPartOfSim_ClientAndHost
        protected override void SubDoPerSecondLogic_Stage2Aggregating_OnMainThreadAndPartOfSim_ClientAndHost( ArcenClientOrHostSimContextCore Context )
        {
            int debugCode = 0;
            try
            {
                debugCode = 100;
                bool tracing = this.tracing_shortTerm = Engine_AIW2.TraceAtAll && Engine_AIW2.TracingFlags.Has( ArcenTracingFlags.RelentlessWave );
                ArcenCharacterBuffer tracingBuffer = tracing ? ArcenCharacterBuffer.GetFromPoolOrCreate( "AIRelentless-tracing", 10f ) : null;

                debugCode = 300;
                int totalUnits = 0;
                int totalStrength = 0;
                if ( tracing )
                    StrengthPerPlanet_TracingOnly.ClearConstructionDictForStartingConstruction();
                debugCode = 400;
                foreach ( GameEntity_Squad entity in AttachedFaction.Squads() )
                {
                    totalUnits += 1 + entity.ExtraStackedSquadsInThis;
                    totalStrength += entity.GetStrengthOfSelfAndContents();
                    if ( !tracing )
                        break; //we don't need to count the total units except for tracing
                    if ( StrengthPerPlanet_TracingOnly.Construction[entity.Planet] == 0 )
                        StrengthPerPlanet_TracingOnly.Construction[entity.Planet] = entity.GetStrengthOfSelfAndContents();
                    else
                        StrengthPerPlanet_TracingOnly.Construction[entity.Planet] += entity.GetStrengthOfSelfAndContents();
                }
                debugCode = 900;
                if ( tracing )
                {
                    StrengthPerPlanet_TracingOnly.SwitchConstructionToDisplay();
                    if ( totalUnits > 0 )
                    {
                        tracingBuffer.Add( "Total strength of RelentlessWave: " + (totalStrength / 1000) + " totalUnits " + totalUnits + " \n" );
                        foreach ( KeyValuePair<Planet, int> pair in StrengthPerPlanet_TracingOnly.GetDisplayDict() )
                        {
                            tracingBuffer.Add( "\t" + pair.Key.Name + " -> " + (pair.Value / 1000) + " strength\n" );
                        }
                    }
                }

                debugCode = 1200;
                if ( tracing )
                {
                    if ( !tracingBuffer.GetIsEmpty() ) 
                        ArcenDebugging.ArcenDebugLogSingleLine( "RelentlessWaveLogic " + AttachedFaction.FactionIndex + " Sim Code. " + tracingBuffer.ToString(), Verbosity.DoNotShow );
                    tracingBuffer.ReturnToPool();
                    tracingBuffer = null;
                }
            }
            catch ( Exception e )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "Hit exception in RelentlessWave DoPerSecondLogic_Stage2Aggregating_OnMainThreadAndPartOfSim debugCode " + debugCode + " " + e.ToString(), Verbosity.ShowAsError );
            }
        }
        #endregion
    }
}
