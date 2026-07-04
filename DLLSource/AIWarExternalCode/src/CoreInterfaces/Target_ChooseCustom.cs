using System;
using Arcen.AIW2.Core;
using Arcen.Universal;

namespace Arcen.AIW2.External
{
    public class ChooseCustomTarget : ITargetedInputAction
    {
        public object Actor { get; private set; }
        public CustomSystem System => (this as ITargetedInputAction).Actor as CustomSystem;
        public GameEntity_Squad Entity => System.Entity as GameEntity_Squad;
        
        bool ITargetedInputAction.CanBegin( object actor, out string reason )
        {
            reason = "Ok.";
            
            var sys = (actor as CustomSystem);
            if (sys == null)
            {
                reason = "No system!";
                return false;
            }
            
            if (sys.Type.TargetedInputAction == null)
            {
                reason = "System is not targeted!";
                return false;
            }
            
            if (sys.Type.TargetedInputAction.Handle.GetType() != this.GetType())
            {
                reason = "System uses a different type of targeting!";
                return false;
            }
            
            var playerfaction = World_AIW2.Instance.GetLocalPlayerFactionOrNull();
            if (playerfaction == null)
            {
                LOG.Msg("No local player faction!");
                return false;
            }
            
            return true;
        }

        void ITargetedInputAction.Begin(object actor)
        {
            this.Actor = actor;
        }
        
        void ITargetedInputAction.Cancel()
        {
            this.Actor = null;
        }

        bool ITargetedInputAction.Click( ArcenMouseEventType eventType, ArcenPoint pointAtCursor, GameEntity_Base entityAtCursor )
        {
            var cmdtype = GameCommandTypeTable.Instance.GetRowByNameOrNullIfNotFound("CustomOrder");
            
            var cmd = cmdtype.GetOrCreateFromPool();
            cmd.RelatedString = "ActivateTargetedCustomSystem";
            cmd.RelatedEntityIDs.Add(this.Entity.PrimaryKeyID);
            cmd.RelatedPoints.Add( pointAtCursor );
            cmd.RelatedString2 = System.Parent.TypeData.InternalName_Original;
            
            if (entityAtCursor != null)
            {
                cmd.RelatedBool = true;
                cmd.RelatedIntegers.Add( entityAtCursor.PrimaryKeyID );
            }

            World_AIW2.Instance.QueueGameCommand( this.Entity.GetFactionOrNull_Safe(), cmd, true );
            
            LOG.Msg("CustomOrder {0} for entity {1} targeting {2} queued", cmd.RelatedString, cmd.RelatedEntityIDs.First, cmd.RelatedIntegers.First);

            return true;
        }

        void ITargetedInputAction.UpdateCursor(ArcenPoint pointAtCursor)
        {
            
        }

        void ITargetedInputAction.WriteStatusMessage( ArcenDoubleCharacterBuffer buffer )
        {
            var map = TextVarMap.Get("StatusMessage_ChooseCustomTarget");
            if (map == null)
                throw new NullReferenceException("StatusMessage_ChooseCustomTarget");
            
            buffer.AddVarReplace(map, AppendVar);
        }

        private void AppendVar( string Name, TextStyle Style, ArcenCharacterBufferBase Buffer, object Args )
        {
            if (Name == "Name")
            {
                Buffer.Add( System.Type.GetDisplayName() );
                return;
            }
            
            if (Name == "Target")
            {
                Buffer.Add( "Target" );
                return;
            }
        }
    }
}
