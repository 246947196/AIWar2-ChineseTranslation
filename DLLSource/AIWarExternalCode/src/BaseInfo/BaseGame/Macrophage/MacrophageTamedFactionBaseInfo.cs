using Arcen.AIW2.Core;
using Arcen.Universal;
using System;

using System.Text;

namespace Arcen.AIW2.External
{
    public class MacrophageTamedFactionBaseInfo : MacrophageFactionBaseInfoCore, IExternalBaseInfo_Singleton
    {
        //serialized

        //not serialized
        public override bool IsTamed => true;
        public override bool IsEnraged => false;
        public static MacrophageTamedFactionBaseInfo Instance; //there can only ever be one of this faction at a time

        public MacrophageTamedFactionBaseInfo()
        {
            Cleanup();
        }

        protected override void SubCleanup()
        {
            Instance = null;
        }

        protected override void SubSerializeFactionTo( SerMetaData MetaData, ArcenSerializationBuffer Buffer, SerializationCommandType SerializationCmdType )
        {
        }

        protected override void SubDeserializeFactionIntoSelf( SerMetaData MetaData, ArcenDeserializationBuffer Buffer, SerializationCommandType SerializationCmdType )
        {
        }

        public override int GetDifficultyOrdinal_OrNegativeOneIfNotRelevant()
        {
            return this.EffectiveIntensity; //the tamed are not effing around, apparently, and will be locked to 10 most of the time
        }

        public override float CalculateYourPortionOfPredictedGameLoad_Where100IsANormalAI( ArcenCharacterBufferBase OptionalExplainCalculation )
        {
            //Chris says: these are part of the main faction, don't tell us about this
            return 0;
        }

        #region SubDoGeneralAggregationsPausedOrUnpaused
        protected override void SubDoGeneralAggregationsPausedOrUnpaused()
        {
            Instance = this;
        }
        #endregion

        #region DoRefreshFromFactionSettings        
        protected override void DoRefreshFromFactionSettings()
        {
            //ConfigurationForFaction cfg = this.AttachedFaction.Config;
            //Intensity = cfg.GetIntValueForCustomFieldOrDefaultValue( "Intensity", true );
        }
        #endregion

        protected override void SubDoPerSecondLogic_Stage2Aggregating_OnMainThreadAndPartOfSim_ClientAndHost( ArcenClientOrHostSimContextCore Context )
        {
        }

        #region UpdateAllegiance
        protected override void UpdateAllegiance()
        {
            // Always be friendly to players.
            AllegianceHelper.AllyThisFactionToHumans( this.AttachedFaction );
        }
        #endregion
    }
}
