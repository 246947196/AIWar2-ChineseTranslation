using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Arcen.AIW2.Core;
using Arcen.Universal;

namespace Arcen.AIW2.External
{
    public class System_Transformation : ExternalData_CustomSystem
    {
        #region Required Crap
        public override string GetIdentifierForErrorMessages()
        {
            return "System_Transformation";
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
        
        public GameEntityTypeData Transform_Back { get; set; }
        public GameEntityTypeData Transform_Into { get; set; }

        protected override void OnParentSet()
        {
            //LOG.Msg("{0}.OnParentSet() id is {1}", this, Parent.TypeData.InternalName_Original);
        }
        
        protected override void OnEntitySet()
        {
            //LOG.Msg("{0}.OnEntitySet() entity is {1}", this, Entity);
            //base.OnEntitySet();
            //Transform_Back = Entity.TypeData;
        }
        
        protected override void OnTypeSet()
        {
            //LOG.Msg("{0}.OnTypeSet() type is {1}", this, Type.InternalName);
            
            var typeName = Type.OriginalXmlData.GetString("transform_into", null, true);
            if (typeName == null)
                return;
            
            var type = GameEntityTypeDataTable.Instance.GetRowByName(typeName, LookupSwapAllowed.No, false);
            if (type == null)
                return;
            
            Transform_Into = type;
            
            typeName = Type.OriginalXmlData.GetString("transform_back", null, true);
            if (typeName == null)
                return;
            
            type = GameEntityTypeDataTable.Instance.GetRowByName(typeName, LookupSwapAllowed.No, false);
            if (type == null)
                return;
            
            Transform_Back = type;
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
            LOG.Msg("{0}.OnActivate", this);
            
            var fac = Entity.GetFactionOrNull_Safe();
            fac.MetalStorage = -1000000;
            
            var con = Engine_AIW2.Instance.MainThreadContext_ClientOrHost.GetHostOnlyContext();
            if (con == null)
            {
                LOG.Msg("Client so returning.");
                return;
            }
            
            var squad = (GameEntity_Squad)Entity;
            if (squad == null)
            {
                LOG.Msg("Null squad so returning.");
                return;
            }
            
            var sel = squad.GetIsSelected();
            
            var new_squad = squad.TransformInto(con, Transform_Into, 1, true);
            if (new_squad == null)
            {
                LOG.Err("Error in {0}.OnActivate; TransformInto( {1} ) returned null.", this, Transform_Into.OrNull());
                return;
            }
            
            var new_sys = new_squad.GetSystemById(this.Parent.TypeData.InternalName_Original);
            if (new_sys == null)
            {
                LOG.Msg("Error in {0}.OnActivate; TransformInto( {1} ) did not have a system named '{2}'.", this, Transform_Into.OrNull(), this.Parent.TypeData.InternalName_Original);
                //return;
            }
            else
            {
                var cust = new_sys.CustomSystem as System_Transformation;
                if (cust == null)
                {
                    LOG.Msg("Error in {0}.OnActivate; TransformInto( {1} ) system named '{2}' does not seem to be a '{3}'.", 
                        this, Transform_Into.OrNull(), this.Parent.TypeData.InternalName_Original, "System_Transformation");
                }
                else
                {
                    cust._on = this._on;
                    cust.ActivationTime = this.ActivationTime;
                }
            }
            
            //new_squad.TransformsIntoAfterTime = Transform_Back.InternalName;
            //new_squad.SecondsTillTransformation = (short)this.Type.Duration;
            
            if (sel)
                new_squad.Select(true, "TransformInto Source was Selected");
        }

        protected override void OnDeactivate()
        {
            LOG.Msg("{0}.OnDeactivate", this);
            
            var con = Engine_AIW2.Instance.MainThreadContext_ClientOrHost.GetHostOnlyContext();
            if (con == null)
                return;
            
            var squad = (GameEntity_Squad)Entity;
            if (squad == null)
                return;
            
            var sel = squad.GetIsSelected();
            
            var new_squad = squad.TransformInto(con, Transform_Back, 1, true);
            if (new_squad == null)
            {
                LOG.Err("Error in {0}.OnDeactivate; TransformInto( {1} ) returned null.", this, Transform_Back.OrNull());
                return;
            }
            
            var new_sys = new_squad.GetSystemById(this.Parent.TypeData.InternalName_Original);
            if (new_sys == null)
            {
                LOG.Msg("Error in {0}.OnDeactivate; TransformInto( {1} ) did not have a system named '{2}'.", 
                    this, Transform_Back.OrNull(), this.Parent.TypeData.InternalName_Original);
                //return;
            }
            else
            {
                var cust = new_sys.CustomSystem as System_Transformation;
                if (cust == null)
                {
                    LOG.Msg("Error in {0}.OnDeactivate; TransformInto( {1} ) system named '{2}' does not seem to be a '{3}'.", 
                        this, Transform_Back.OrNull(), this.Parent.TypeData.InternalName_Original, "System_Transformation");
                }
                else
                {
                    cust._on = this._on;
                    cust.ActivationTime = this.ActivationTime;
                }
            }
            
            //new_squad.TransformsIntoAfterTime = Transform_Back.InternalName;
            //new_squad.SecondsTillTransformation = (short)this.Type.Duration;
            
            if (sel)
                new_squad.Select(true, "TransformInto Source was Selected");
        }

        public override void AppendVarValue(string key, TextStyle style, ArcenCharacterBufferBase buffer, object args)
        {
            if (key.Equals("TransformInto"))
            {
                //LOG.Msg("this.Transform_Into = {0}", this.Transform_Into?.InternalName??"null");
                
                var fac = this.Entity.GetFactionOrNull_Safe();// ?? World_AIW2.Instance.GetNaturalObjectFactionNeverNull());
                if (fac == null)
                    fac = World_AIW2.Instance.GetNeutralFaction();
                if ((Entity as GameEntity_Squad).IsFakeEntity)
                    fac = null;
                
                buffer
                    .AddShipIconInline(this.Transform_Into, fac)
                    .Add(this.Transform_Into.GetDisplayName(), fac?.FactionCenterColor.ColorHexBrighter);
                return;
            }

            // unknown
            {
                base.AppendVarValue(key, style, buffer, args);
            }
        }
        
        public override string ToString()
        {
            return this.Type?.InternalName ?? "System_Transformation";
        }
    }
}
