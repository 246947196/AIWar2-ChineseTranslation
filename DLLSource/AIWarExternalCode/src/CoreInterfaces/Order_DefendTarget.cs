using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Arcen.AIW2.Core;
using Arcen.Universal;

namespace Arcen.AIW2.External
{
    public class Order_DefendTarget : ExternalData_CustomOrder
    {
        #region Required Crap
        public override string GetIdentifierForErrorMessages()
        {
            return "Order_DefendTarget";
        }
        
        public override void SerializeTo( SerMetaData MetaData, ArcenSerializationBuffer Buffer, SerializationCommandType SerializationCmdType )
        {
            Buffer.AddSquadPrimaryKeyID_PosDef0(MetaData, this.TargetToDefend.PrimaryKeyID, "Order_DefendTarget.TargetToDefend");
        }
        
        public override void DeserializeIntoSelf( SerMetaData MetaData, ArcenDeserializationBuffer Buffer, SerializationCommandType SerializationCmdType )
        {
            int id = Buffer.ReadSquadPrimaryKeyID_PosDef0(MetaData, "Order_DefendTarget.TargetToDefend");
            this.TargetToDefend = LazyLoadSquadWrapper.Create(id, true, "Order_DefendTarget");
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

        public bool TargetIsEntity;
        public LazyLoadSquadWrapper TargetToDefend;
        public ArcenPoint PointToDefend;
        public ArcenPoint Offset;

        public override string ReasonIfInvalid()
        {
            //var squad = TargetToDefend.GetSquad();
            //if (squad == null)
                //return "No Target";
            
            return null;
        }
        
        public override bool GetShouldCopyToUnitsProducedByMe()
        {
            return false;
        }

        public override bool SetupFromCommand( ref EntityOrder order, GameCommand command )
        {
            TargetIsEntity = command.RelatedBool;
            if (TargetIsEntity)
            {
                if (command.RelatedIntegers.Count == 0)
                {
                    LOG.Err("Order_DefendTarget.SetupFromCommand has TargetIsEntity (RelatedBool) as true but TargetToDefend (RelatedIntegers[0]) is not set.");
                    return false;
                }
                
                var target = World_AIW2.Instance.GetEntityByID_Squad( command.RelatedIntegers.First );
                if (target == null)
                {
                    LOG.Err("Order_DefendTarget.SetupFromCommand has TargetIsEntity (RelatedBool) as true but TargetToDefend (RelatedIntegers[0]) is '{0}' and we found no entity with that id.", command.RelatedIntegers.First);
                    return false;
                }
                
                this.TargetToDefend = LazyLoadSquadWrapper.Create(target);
                
                if (command.RelatedPoints.Count > 0)
                {
                    this.Offset = command.RelatedPoints.First;
                }
            }
            else
            {
                if (command.RelatedPoints.Count == 0)
                {
                    LOG.Err("Order_DefendTarget.SetupFromCommand has TargetIsEntity (RelatedBool) as false so needs a PointToDefend (RelatedPoints[0]) but there aren't any.");
                    return false;
                }

                this.PointToDefend = command.RelatedPoints.First;

                if (command.RelatedPoints.Count > 1)
                {
                    ArcenPoint _rp_1 = ArcenPoint.ZeroZeroPoint;
                    int _rp_i = 0;
                    foreach ( var _rp_v in command.RelatedPoints )
                    {
                        if ( _rp_i == 1 ) { _rp_1 = _rp_v; break; }
                        _rp_i++;
                    }
                    this.Offset = _rp_1;
                }
            }
            
            if (this.TargetIsEntity)
            {
                order.RelatedSquad = this.TargetToDefend;
                order.RelatedPoint = this.Offset;
            }
            else
            {
                order.RelatedPoint = this.PointToDefend;
                order.RelatedSquad.Clear();
            }

            return true;
        }

        public override void GetText( ArcenCharacterBufferBase buffer )
        {
            var target = this.TargetToDefend.GetSquad();
            if (target != null)
            {
                buffer.Add("Defend ").Add(target.TypeData.GetShortDisplayName());
            }
            else
            {
                buffer.Add("Defend around ").Add(this.PointToDefend);
            }
            
        }

        public override void Evaluate( ArcenClientOrHostSimContextCore context, ref EntityOrder order )
        {
            var e = Entity as GameEntity_Squad;
            
            if (this.TargetToDefend.GetSquad() != null)
            {
                order.RelatedSquad = this.TargetToDefend;
                order.RelatedPoint = this.Offset;
            }
            else
            {
                order.RelatedSquad.Clear();
                order.RelatedPoint = this.PointToDefend + this.Offset;
            }
            
            e.Orders.ModifyQueuedOrder(order);
        }
    }
}
