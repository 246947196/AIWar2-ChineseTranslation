using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Arcen.AIW2.Core;
using Arcen.Universal;

namespace Arcen.AIW2.External
{
    public class System_Weapon : ExternalData_CustomSystem
    {
        #region Required Crap
        public override string GetIdentifierForErrorMessages()
        {
            return "System_Weapon";
        }
        
        public override void DoAnyBelatedCleanupWhenComingOutOfPool_ShouldBeVeryLittleToDo()
        {
        }

        public override void DoEarlyCleanupWhenGoingIntoQuarantine_ClearIncomingPointersButNotOugoingReferences()
        {
        }

        public override void DoMidCleanupWhenLeavingQuarantineBackIntoMainPool_ClearAsMuchAsPossibleIncludingOutgoingReferences()
        {
        }

        protected override void ActuallyPutSelfBackIntoAPool()
        {
            this.Type.Free(this);
        }
        #endregion

        protected override void OnParentSet()
        {
        }
        
        protected override void OnEntitySet()
        {
        }
        
        protected override void OnTypeSet()
        {
        }

        public override void Update(ArcenClientOrHostSimContextCore Context, FInt EffectiveDeltaTime)
        {
            base.Update(Context, EffectiveDeltaTime);
        }

        public override void SerializeTo( SerMetaData MetaData, ArcenSerializationBuffer Buffer, SerializationCommandType SerializationCmdType )
        {
            base.SerializeTo(MetaData, Buffer, SerializationCmdType);
        }
        
        public override void DeserializeIntoSelf( SerMetaData MetaData, ArcenDeserializationBuffer Buffer, SerializationCommandType SerializationCmdType )
        {
            base.DeserializeIntoSelf(MetaData, Buffer, SerializationCmdType);
        }

        protected override void OnActivate()
        {
        }

        protected override void OnDeactivate()
        {
        }

        public override void AppendVarValue(string key, TextStyle style, ArcenCharacterBufferBase buffer, object args)
        {
            // unknown
            {
                base.AppendVarValue(key, style, buffer, args);
            }
        }
        
        public override string ToString()
        {
            return this.Type?.InternalName ?? "System_Weapon";
        }
    }
}
