using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Arcen.AIW2.Core;
using Arcen.Universal;
using UnityEngine;

namespace Arcen.AIW2.External
{
    public class DefendClickedTarget : ITargetedInputAction
    {
        public DefendClickedTarget()
        {
            //LOG.Line(); 
        }
        
        public object Actor { get; private set; }
        
        public bool CanBegin(object actor, out string reason)
        {
            reason = "Ok.";
            
            if (World_AIW2.Instance.LocalPlayerSelectedSquads_CanGiveOrders.Count > 0)
                return true;
            
            reason = "You have nothing selected to give orders to.";
            
            return false;
        }
        
        public void Begin(object actor)
        {
        }

        public void Cancel()
        {
        }

        public void UpdateCursor(ArcenPoint pointAtCursor)
        {
        }

        public void WriteStatusMessage( ArcenDoubleCharacterBuffer buffer )
        {
            // actually two newlines, for separation
            if (!buffer.Builder.IsNewLine())
                buffer.Add("\n\n");
            
            buffer.Add( "<color=#ffc178><b>Ordering ships</b></color>.\n" );
            buffer.Add( "<color=#999999>Click defend target.\n");
        }

        public bool Click( ArcenMouseEventType eventType, ArcenPoint pointAtCursor, GameEntity_Base entityAtCursor )
        {
            //if (entityAtCursor == null)
            //{
            //    LOG.Msg("Nothing under cursor?");
            //    return false;
            //}
            
            var playerfaction = World_AIW2.Instance.GetLocalPlayerFactionOrNull();
            if (playerfaction == null)
            {
                LOG.Msg("CustomOrder no playerFaction?");
                return false;
            }
            
            //var customOrderType = CustomOrderTypeTable.Instance.GetRowByNameOrNullIfNotFound( "DefendTarget" );
            var cmdtype = GameCommandTypeTable.Instance.GetRowByNameOrNullIfNotFound("CustomOrder");
            var cmd = cmdtype.GetOrCreateFromPool();
            cmd.RelatedString = "DefendTarget";

            foreach (var obj in World_AIW2.Instance.LocalPlayerSelectedSquads_CanGiveOrders)
            {
                var e = (GameEntity_Squad)obj;
                cmd.RelatedEntityIDs.Add( e.PrimaryKeyID );
            }
            
            if (cmd.RelatedEntityIDs.Count == 0)
            {
                LOG.Msg("CustomOrder no entities selected? Yet theres a count of {0}?", World_AIW2.Instance.LocalPlayerSelectedSquads_CanGiveOrders.Count);
                cmd.ReturnToPool();
                return true;
            }
            
            if (entityAtCursor != null)
            {
                cmd.RelatedBool = true;
                cmd.RelatedIntegers.Add( entityAtCursor.PrimaryKeyID );
                cmd.RelatedPoints.Add( pointAtCursor - entityAtCursor.WorldLocation );
            }
            else
            {
                cmd.RelatedBool = false;
                cmd.RelatedPoints.Add( pointAtCursor );
            }

            World_AIW2.Instance.QueueGameCommand( playerfaction, cmd, true );
            
            LOG.Msg("CustomOrder {0} for entity {1} targeting {2} queued", cmd.RelatedString, cmd.RelatedEntityIDs.First, cmd.RelatedIntegers.First);
            
            return true;
        }
    }
}
