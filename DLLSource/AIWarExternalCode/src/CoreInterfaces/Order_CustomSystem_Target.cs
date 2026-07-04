using System;
using Arcen.AIW2.Core;
using Arcen.Universal;

namespace Arcen.AIW2.External
{
    public class Order_CustomSystem_Target : ExternalData_CustomOrder
    {
        public bool TargetIsEntity;
        public ArcenPoint TargetPos;
        public LazyLoadSquadWrapper TargetEntity;
        public ExternalData_CustomSystem System;
        
        #region Required Crap
        public override string GetIdentifierForErrorMessages()
        {
            return "Order_ActivateSystem_Targetted";
        }
        
        public override void SerializeTo( SerMetaData MetaData, ArcenSerializationBuffer Buffer, SerializationCommandType SerializationCmdType )
        {
            Buffer.AddBool(MetaData, TargetIsEntity, "Order_CustomSystem_Target.TargetIsEntity");
            Buffer.AddArcenPointFromCombatSpace(MetaData, TargetPos, "Order_CustomSystem_Target.TargetPos");
            Buffer.AddSquadPrimaryKeyID_PosDef0(MetaData, TargetEntity.PrimaryKeyID, "Order_CustomSystem_Target.TargetEntity");
            Buffer.AddString_Condensed(MetaData, System.Parent.TypeData.InternalName_Original, "Order_CustomSystem_Target.System");
        }
        
        public override void DeserializeIntoSelf( SerMetaData MetaData, ArcenDeserializationBuffer Buffer, SerializationCommandType SerializationCmdType )
        {
            TargetIsEntity = Buffer.ReadBool(MetaData, "Order_CustomSystem_Target.TargetIsEntity");
            TargetPos = Buffer.ReadArcenPointFromCombatSpace(MetaData, "Order_CustomSystem_Target.TargetPos");
            TargetEntity = LazyLoadSquadWrapper.Create( Buffer.ReadSquadPrimaryKeyID_PosDef0(MetaData, "Order_CustomSystem_Target.TargetEntity"), true, "Order_CustomSystem_Target" );
            System = (this.Entity as GameEntity_Squad).GetSystemById( Buffer.ReadString_Condensed(MetaData, "Order_CustomSystem_Target.System") ).CustomSystem as ExternalData_CustomSystem;
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
        
        public override string ReasonIfInvalid()
        {
            if (System.Status == CustomSystemStatus.Cooldown)
                return "Cooldown";
            
            return null;
        }
        
        public override bool GetShouldCopyToUnitsProducedByMe()
        {
            return false;
        }

        public override bool SetupFromCommand( ref EntityOrder order, GameCommand command )
        {
            TargetIsEntity = command.RelatedBool;
            
            if (command.RelatedEntityIDs.Count == 0)
            {
                LOG.Err("Order_CustomSystem_Target.SetupFromCommand needs 1x RelatedEntityIDs but has none.");
                return false;
            }

            var actor = World_AIW2.Instance.GetEntityByID_Squad( command.RelatedEntityIDs.First );
            if (actor == null)
            {
                LOG.Err("Order_CustomSystem_Target.SetupFromCommand RelatedEntityIDs[0] is '{0}' but found no entity with that id.", command.RelatedEntityIDs.First);
                return false;
            }
            
            if (string.IsNullOrEmpty(command.RelatedString2))
            {
                LOG.Err("Order_CustomSystem_Target.SetupFromCommand needs RelatedString2 but has none.");
                return false;
            }
            
            var system = actor.GetSystemById(command.RelatedString2);
            if (system == null)
            {
                LOG.Err("Order_CustomSystem_Target.SetupFromCommand RelatedString2 is '{0}' but found no system with that name.", command.RelatedString2);
                return false;
            }
            
            if (system.CustomSystem == null)
            {
                LOG.Err("Order_CustomSystem_Target.SetupFromCommand system '{0}' is not a custom system.", command.RelatedString2);
                return false;
            }
            
            this.System = system.CustomSystem as ExternalData_CustomSystem;
            
            if (TargetIsEntity)
            {
                if (command.RelatedIntegers.Count == 0)
                {
                    LOG.Err("Order_CustomSystem_Target.SetupFromCommand has TargetIsEntity (RelatedBool) as true but TargetEntity (RelatedIntegers[0]) is not set.");
                    return false;
                }
                
                var target = World_AIW2.Instance.GetEntityByID_Squad( command.RelatedIntegers.First );
                if (target == null)
                {
                    LOG.Err("Order_CustomSystem_Target.SetupFromCommand has TargetIsEntity (RelatedBool) as true but TargetEntity (RelatedIntegers[0]) is '{0}' and we found no entity with that id.", command.RelatedIntegers.First);
                    return false;
                }
                
                this.TargetEntity = LazyLoadSquadWrapper.Create(target);
            }
            else
            {
                if (command.RelatedPoints.Count == 0)
                {
                    LOG.Err("Order_CustomSystem_Target.SetupFromCommand has TargetIsEntity (RelatedBool) as false so needs a TargetPos (RelatedPoints[0]) but there aren't any.");
                    return false;
                }
                
                this.TargetPos = command.RelatedPoints.First;
            }
            
            order.RelatedPoint = Engine_AIW2.Instance.CombatCenter;

            return true;
        }

        public override void GetText( ArcenCharacterBufferBase buffer )
        {
            buffer.Add( "Fire " ).Add(System.Parent.TypeData.GetDisplayName()).Add(" at ");
                
            if ( TargetIsEntity )
            {
                buffer.Add(TargetEntity.GetSquad().TypeData.GetShortDisplayName());
            }
            else
            {
                buffer.Add(TargetPos);
            }
        }

        public override void Evaluate( ArcenClientOrHostSimContextCore context, ref EntityOrder order )
        {
            string reason;
            if (TargetIsEntity)
                System.Activate(TargetEntity.GetSquad(), out reason);
            else
                System.Activate(TargetPos, out reason);
        }
    }
}
