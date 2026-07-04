using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Arcen.AIW2.Core;
using Arcen.Universal;

namespace Arcen.AIW2.External
{
    public class System_StatAdjust : ExternalData_CustomSystem, IGameEntitySpeedAdjuster, IGameEntityHullAdjuster, IGameEntityShieldAdjuster
    {
        #region Required Crap
        public override string GetIdentifierForErrorMessages()
        {
            return "System_StatAdjust";
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
        
        public int Adjust_MaxSpeed = -1;
        public int Adjust_MaxHull = -1;
        public int Adjust_MaxShield = -1;
        public int Adjust_Damage = -1;

        protected override void OnTypeSet()
        {
            Adjust_MaxSpeed = Type.OriginalXmlData.GetInt32("adjust_maxspeed", -1, false);
            Adjust_MaxHull = Type.OriginalXmlData.GetInt32("adjust_maxhull", -1, false);
            Adjust_MaxShield = Type.OriginalXmlData.GetInt32("adjust_maxshield", -1, false);
        }

        public int GetAdjustedBaseSpeed( GameEntity_Squad RelatedEntity, ref int BaseSpeed )
        {
            if (ActivationTime == -1)
                return 0;
            
            if (IsOff)
                return 0;
            
            int time = World_AIW2.Instance.GameSecond - ActivationTime;
            
            if (time > Type.Duration)
                return 0;
            
            BaseSpeed += Adjust_MaxSpeed;
            
            return Adjust_MaxSpeed;
        }
        
        public int GetAdjustedBaseHull( GameEntity_Squad RelatedEntity, ref int BaseHull )
        {
            if (ActivationTime == -1)
                return 0;
            
            if (IsOff)
                return 0;
            
            int time = World_AIW2.Instance.GameSecond - ActivationTime;
            
            if (time > Type.Duration)
                return 0;
            
            BaseHull += Adjust_MaxHull;
            
            return Adjust_MaxHull;
        }

        public int GetAdjustedBaseShields( GameEntity_Squad RelatedEntity, ref int BaseShields )
        {
            if (ActivationTime == -1)
                return 0;
            
            if (IsOff)
                return 0;
            
            int time = World_AIW2.Instance.GameSecond - ActivationTime;
            
            if (time > Type.Duration)
                return 0;
            
            BaseShields += Adjust_MaxShield;
            
            return Adjust_MaxShield;
        }
        
        public override void Update(ArcenClientOrHostSimContextCore Context, FInt EffectiveDeltaTime)
        {
            base.Update(Context, EffectiveDeltaTime);
        }

        public override void SerializeTo( SerMetaData MetaData, ArcenSerializationBuffer Buffer, SerializationCommandType SerializationCmdType )
        {
            base.SerializeTo(MetaData, Buffer, SerializationCmdType);
            
            //LOG.Msg("{0} writing ActivationTime={1}", this.GetType().Name, this.ActivationTime);
            //Buffer.AddInt32(MetaData, ReadStyle.PosExceptNeg1, this.ActivationTime, "System_StatAdjust.ActivationTime");
        }
        
        public override void DeserializeIntoSelf( SerMetaData MetaData, ArcenDeserializationBuffer Buffer, SerializationCommandType SerializationCmdType )
        {
            base.DeserializeIntoSelf(MetaData, Buffer, SerializationCmdType);
            
            //this.ActivationTime = Buffer.ReadInt32(MetaData, ReadStyle.PosExceptNeg1, "System_StatAdjust.ActivationTime");
            //LOG.Msg("{0} reading ActivationTime={1}", this.GetType().Name, this.ActivationTime);
        }

        protected override void OnActivate()
        {
            this.ActivationTime = World_AIW2.Instance.GameSecond;
        }

        protected override void OnDeactivate()
        {
            
        }

        public override void AppendVarValue(string key, TextStyle style, ArcenCharacterBufferBase buffer, object args)
        {
            if (key.Equals("Adjust_MaxSpeed"))
            {
                buffer.StartColor(TooltipColors.CustomSystem_Adjust_Speed).Add(this.Adjust_MaxSpeed).EndColor();
                return;
            }
            
            if (key.Equals("Adjust_MaxHull"))
            {
                buffer.StartColor(TooltipColors.CustomSystem_Adjust_MaxHull).Add(this.Adjust_MaxHull).EndColor();
                return;
            }
            
            if (key.Equals("Adjust_MaxShield"))
            {
                buffer.StartColor(TooltipColors.CustomSystem_Adjust_MaxShield).Add(this.Adjust_MaxShield).EndColor();
                return;
            }

            // unknown
            {
                base.AppendVarValue(key, style, buffer, args);
            }
        }
    }
}
