using Arcen.AIW2.Core;
using Arcen.Universal;
using System;

using System.Text;

namespace Arcen.AIW2.External
{
    public class MacrophageEnragedFactionBaseInfo : MacrophageFactionBaseInfoCore, IExternalBaseInfo_Singleton
    {
        //serialized

        //not serialized
        public override bool IsTamed => false;
        public override bool IsEnraged => true;
        public static MacrophageEnragedFactionBaseInfo Instance; //there can only ever be one of this faction at a time

        public MacrophageEnragedFactionBaseInfo()
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
            return this.EffectiveIntensity; //this will generally be the highest value of the other macrophages
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

        // Always hate everything
        public override void SetStartingFactionRelationships()
        {
            AllegianceHelper.EnemyThisFactionToAll( AttachedFaction );
        }

        protected override void SubDoPerSecondLogic_Stage2Aggregating_OnMainThreadAndPartOfSim_ClientAndHost( ArcenClientOrHostSimContextCore Context )
        {
        }
    }
}
