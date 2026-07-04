using Arcen.AIW2.Core;
using Arcen.Universal;
using System;

using System.Text;

namespace Arcen.AIW2.External
{
    public abstract class AISubFactionBaseInfo : ExternalFactionBaseInfoRoot
    {
        public Faction ParentFaction { get; private set; }
        public AISentinelsFactionBaseInfo ParentBaseInfo { get; private set; }

        public AISubFactionBaseInfo()
        {
            Cleanup();
        }

        protected sealed override void Cleanup()
        {
            ParentFaction = null;
            ParentBaseInfo = null;

            this.SubCleanup();
        }

        protected abstract void SubCleanup();

        public sealed override void SerializeFactionTo( SerMetaData MetaData, ArcenSerializationBuffer Buffer, SerializationCommandType SerializationCmdType )
        {
            this.SubSerializeFactionTo( MetaData, Buffer, SerializationCmdType );
        }
        protected abstract void SubSerializeFactionTo( SerMetaData MetaData, ArcenSerializationBuffer Buffer, SerializationCommandType SerializationCmdType );

        public sealed override void DeserializeFactionIntoSelf( SerMetaData MetaData, ArcenDeserializationBuffer Buffer, SerializationCommandType SerializationCmdType )
        {
            this.SubDeserializeFactionIntoSelf( MetaData, Buffer, SerializationCmdType );
        }
        protected abstract void SubDeserializeFactionIntoSelf( SerMetaData MetaData, ArcenDeserializationBuffer Buffer, SerializationCommandType SerializationCmdType );

        #region DoFactionGeneralAggregationsPausedOrUnpaused
        protected sealed override void DoFactionGeneralAggregationsPausedOrUnpaused()
        {
            SubDoGeneralAggregationsPausedOrUnpaused();
        }
        protected abstract void SubDoGeneralAggregationsPausedOrUnpaused();
        #endregion

        #region DoRefreshFromFactionSettings        
        protected override void DoRefreshFromFactionSettings()
        {
            #region SubFaction Links
            if ( this.ParentFaction == null )
            {
                this.ParentFaction = this.AttachedFaction.GetParentFactionOrNull();
                if ( this.ParentFaction == null )
                {
                    ArcenDebugging.ArcenDebugLogSingleLine( "Missing parent AI for AI subfaction '" +
                        this.AttachedFaction.GetDisplayName() + "', which has a parent index set of " + this.AttachedFaction.FactionIndexOfMyParentIfIHaveOne, Verbosity.ShowAsError );
                    return;
                }

                this.ParentBaseInfo = this.ParentFaction.GetExternalBaseInfoAs<AISentinelsFactionBaseInfo>();
            }
            #endregion
        }
        #endregion

        public sealed override void DoPerSecondLogic_Stage2Aggregating_OnMainThreadAndPartOfSim_ClientAndHost( ArcenClientOrHostSimContextCore Context )
        {
            this.SubDoPerSecondLogic_Stage2Aggregating_OnMainThreadAndPartOfSim_ClientAndHost( Context );
        }
        protected abstract void SubDoPerSecondLogic_Stage2Aggregating_OnMainThreadAndPartOfSim_ClientAndHost( ArcenClientOrHostSimContextCore Context );

        public override bool GetShouldAttackNormallyExcludedTarget( GameEntity_Squad Target )
        {
            //to allow AIs to kill eachother's command stations and warp gates in civil war mode
            if ( Target == null )
                return false;
            if ( Target.TypeData.IsCommandStation )
                return true;
            if ( Target.TypeData.ProvidesAIWarpEntryPoint )
                return true;
            if ( Target.TypeData.GetHasTag( "NormalPlanetNastyPick" ) )
                return true;

            return false;
        }

        public override void WriteFactionIdentityString( ArcenCharacterBufferBase buffer )
        {
            if (ParentBaseInfo.SentinelInfo.WriteAITypeDisplayString(buffer))
                buffer.Add(" ");
            
            buffer.Add( AttachedFaction.GetDisplayNameInternal(true, false) );
        }
    }
}
