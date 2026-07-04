using Arcen.AIW2.Core;
using Arcen.Universal;
using System;

using System.Text;

namespace Arcen.AIW2.External
{
    public class AICrossPlanetAttackerBaseInfo : AISubFactionBaseInfo
    {
        //serialized
        public bool CPATimeInitialized;
        public int TimeForCPAToAttack; //when a CPA is created, it will marshall its strength before heading in

        //nonserialized
        public static readonly DoubleBufferedDictionary<Planet, int> StrengthPerPlanet_TracingOnly = DoubleBufferedDictionary<Planet, int>.Create_WillNeverBeGCed( 300, "CPA-StrengthPerPlanet_TracingOnly" );

        //constants
        public const int CPA_DELAY_TIME = 60;

        public AICrossPlanetAttackerBaseInfo()
        {
            Cleanup();
        }

        protected override void SubCleanup()
        {
            TimeForCPAToAttack = -1;
            CPATimeInitialized = false;

            StrengthPerPlanet_TracingOnly.Clear();
        }

        protected override void SubSerializeFactionTo( SerMetaData MetaData, ArcenSerializationBuffer Buffer, SerializationCommandType SerializationCmdType )
        {
            Buffer.AddBool( MetaData, CPATimeInitialized, "CPATimeInitialized" );
            Buffer.AddInt32( MetaData, ReadStyle.PosExceptNeg1, TimeForCPAToAttack, "TimeForCPAToAttack" );
        }

        protected override void SubDeserializeFactionIntoSelf( SerMetaData MetaData, ArcenDeserializationBuffer Buffer, SerializationCommandType SerializationCmdType )
        {
            CPATimeInitialized = Buffer.ReadBool( MetaData, "CPATimeInitialized" );
            TimeForCPAToAttack = Buffer.ReadInt32( MetaData, ReadStyle.PosExceptNeg1, "TimeForCPAToAttack" );
        }

        public override int GetDifficultyOrdinal_OrNegativeOneIfNotRelevant()
        {
            return this.ParentBaseInfo.SentinelInfo.AIDifficulty.Difficulty; //this one just pulls its difficulty from the parent
        }

        public override float CalculateYourPortionOfPredictedGameLoad_Where100IsANormalAI( ArcenCharacterBufferBase OptionalExplainCalculation )
        {
            //Chris says: these are part of the main faction, don't tell us about this
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
            bool tracing = this.tracing_shortTerm = Engine_AIW2.TraceAtAll && Engine_AIW2.TracingFlags.Has( ArcenTracingFlags.CPA );
            ArcenCharacterBuffer tracingBuffer = tracing ? ArcenCharacterBuffer.GetFromPoolOrCreate( "AICPA-Stage2ABase-tracing", 10f ) : null;

            int totalUnits = 0;
            int totalStrength = 0;
            if ( tracing )
                StrengthPerPlanet_TracingOnly.ClearConstructionDictForStartingConstruction();
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
            if ( totalUnits == 0 && (this.TimeForCPAToAttack > 0 || this.CPATimeInitialized) )
            {
                this.CPATimeInitialized = false;
                this.TimeForCPAToAttack = -1;
            }
            if ( totalUnits > 0 && !this.CPATimeInitialized )
            {
                if ( tracing ) tracingBuffer.Add( "Launching CPA into player planets in " + AICrossPlanetAttackerBaseInfo.CPA_DELAY_TIME + " seconds." );
                this.TimeForCPAToAttack = World_AIW2.Instance.GameSecond + AICrossPlanetAttackerBaseInfo.CPA_DELAY_TIME;
                this.CPATimeInitialized = true;
            }
            if ( tracing )
            {
                StrengthPerPlanet_TracingOnly.SwitchConstructionToDisplay();
                if ( this.TimeForCPAToAttack > World_AIW2.Instance.GameSecond )
                    tracingBuffer.Add( "CPA attacking in " + (this.TimeForCPAToAttack - World_AIW2.Instance.GameSecond) );
                if ( totalUnits > 0 )
                {
                    tracingBuffer.Add( "Total strength of CPA: " + (totalStrength / 1000) + " totalUnits " + totalUnits + " \n" );
                    foreach ( KeyValuePair<Planet, int> pair in StrengthPerPlanet_TracingOnly.GetDisplayDict() )
                    {
                        tracingBuffer.Add( "\t" + pair.Key.Name + " -> " + (pair.Value / 1000) + " strength\n" );
                    }
                }
            }

            if ( tracing )
            {
                if ( !tracingBuffer.GetIsEmpty() )
                    ArcenDebugging.ArcenDebugLogSingleLine( "CPALogic " + AttachedFaction.FactionIndex + " Sim Code. " + tracingBuffer.ToString(), Verbosity.DoNotShow );
                tracingBuffer.ReturnToPool();
                tracingBuffer = null;
            }
        }
        #endregion
    }
}
