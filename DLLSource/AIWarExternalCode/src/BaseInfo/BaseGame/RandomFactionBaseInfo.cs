using Arcen.AIW2.Core;
using Arcen.Universal;
using System;

using System.Text;

namespace Arcen.AIW2.External
{
    public class RandomFactionBaseInfo : ExternalFactionBaseInfoRoot
    {
        //serialized

        //not serialized

        public RandomFactionBaseInfo()
        {
            Cleanup();
        }

        protected override void Cleanup()
        {
            
        }

        public override void SerializeFactionTo( SerMetaData MetaData, ArcenSerializationBuffer Buffer, SerializationCommandType SerializationCmdType )
        {
        }

        public override void DeserializeFactionIntoSelf( SerMetaData MetaData, ArcenDeserializationBuffer Buffer, SerializationCommandType SerializationCmdType )
        {
        }

        public override int GetDifficultyOrdinal_OrNegativeOneIfNotRelevant()
        {
            return -1; //not relevant here
        }

        public override float CalculateYourPortionOfPredictedGameLoad_Where100IsANormalAI( ArcenCharacterBufferBase OptionalExplainCalculation )
        {
            if ( OptionalExplainCalculation != null )
                OptionalExplainCalculation.Add( "Random factions may be lower than or higher than normal load, but we have to assume the worst.  150%" );
            return 150;
        }

        #region DoFactionGeneralAggregationsPausedOrUnpaused
        protected override void DoFactionGeneralAggregationsPausedOrUnpaused()
        {
        }
        #endregion

        #region DoRefreshFromFactionSettings        
        protected override void DoRefreshFromFactionSettings()
        {
            //ConfigurationForFaction cfg = this.AttachedFaction.Config;
            //Intensity = cfg.GetIntValueForCustomFieldOrDefaultValue( "Intensity", true );
        }
        #endregion        

        //this literally exists only for the WriteFactionSlotStatus; the faction itself will be replaced
        //in the BaseScenario
        public override void WriteFactionSlotStatus( ArcenCharacterBufferBase buffer )
        {
            if ( !AttachedFaction.SpecialFactionData.IsRandomFaction )
                return; //once the Random faction is swapped out don't show this anymore; this seems to be a race condition with the game starting
            string value = AttachedFaction.GetStringValueForCustomFieldOrDefaultValue( "RandomFactionType", true );
            buffer.Add( value ).Add( "  " );
            value = AttachedFaction.GetStringValueForCustomFieldOrDefaultValue( "Impact", true );
            if ( value != null )
                buffer.Add( value );
            //make this shorter in order to show it
            //value = FactionConfig.GetValueForCustomFieldOrDefaultValue( "Allegiance" );
            //if ( value != null )
            //    buffer.Add( value );
        }
    }
}
