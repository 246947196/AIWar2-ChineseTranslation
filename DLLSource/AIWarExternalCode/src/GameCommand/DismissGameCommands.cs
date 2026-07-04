using Arcen.AIW2.Core;
using Arcen.Universal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Arcen.AIW2.External
{
    public class GameCommand_Stop : BaseGameCommand
    {
        public override void Execute( GameCommand command, ArcenClientOrHostSimContextCore context )
        {
            foreach (var id in command.RelatedEntityIDs)
            {
                var e = World_AIW2.Instance.GetEntityByID_Squad(id);
                if (e == null || e.ToBeRemovedAtEndOfThisFrame)
                    continue;
                
                e.ClearFRDTargets();
                e.Orders.ClearOrders(ClearBehavior.AnythingIncludingAttackerMode, ClearDecollisionOnParent.YesClear_ButNotDecollisionMoveOrders, ClearSource.YesClearAnyOrders_IncludingFromHumans, "Ordered to Stop" );
                e.Orders.SetBehaviorDirectlyInSim( EntityBehaviorType.None );
                e.PreferredEntityTypeDataForTargeting = null;
            }
        }
    }
    
    class GameCommand_CustomOrder : BaseGameCommand
    {
        public override void Execute( GameCommand command, ArcenClientOrHostSimContextCore context )
        {
            try
            {
                if ( string.IsNullOrEmpty(command.RelatedString) )
                {
                    if ( ArcenNetworkAuthority.DesiredStatus != DesiredMultiplayerStatus.Client )
                        ArcenDebugging.ArcenDebugLogSingleLine( "No RelatedString passed to GameCommand_CustomOrder!", Verbosity.ShowAsError );
                    return;
                }
                
                if ( command.RelatedEntityIDs.Count == 0 )
                {
                    if ( ArcenNetworkAuthority.DesiredStatus != DesiredMultiplayerStatus.Client )
                        ArcenDebugging.ArcenDebugLogSingleLine( "No RelatedEntityIDs passed to GameCommand_CustomOrder! These should be the ships recieving the custom order.", Verbosity.ShowAsError );
                    return;
                }
                
                var customOrderType = CustomOrderTypeTable.Instance.GetRowByNameOrNullIfNotFound(command.RelatedString);
                if (customOrderType == null)
                {
                    LOG.Err("No CustomOrderType named '{0}' was found.", command.RelatedString);
                    return;
                }
                
                OrderSource orderFrom = OrderSource.Other;
                if ( command.FromActualInputEventOfPlayerID < 255 )
                    orderFrom = OrderSource.HumanPlayer;
                
                foreach (var id in command.RelatedEntityIDs)
                {
                    var e = World_AIW2.Instance.GetEntityByID_Squad(id);
                    if (e == null)
                        continue;
                    
                    var orders = e.Orders;
                    if ( !command.ToBeQueued )
                        orders.ClearOrders( ClearBehavior.DoNotClearBehaviors, ClearDecollisionOnParent.YesClear_AndAlsoClearDecollisionMoveOrders, orderFrom == OrderSource.HumanPlayer ? ClearSource.YesClearAnyOrders_IncludingFromHumans : ClearSource.DoNotClearHumanOrders_AndDoNotClearBehaviorsIfHumanOrdersPresent, "GameCommand_CustomOrder_NotQueued" );
                    else
                        orders.ClearOrders( ClearBehavior.DoNotClearBehaviors, ClearDecollisionOnParent.OnlyClearDecollisionOrdersAndNothingElse, ClearSource.DoNotClearHumanOrders_AndDoNotClearBehaviorsIfHumanOrdersPresent, "GameCommand_CustomOrder" );

                    var newOrder = EntityOrder.Create_Custom( orderFrom, e.CalculateShouldOrderBeIgnoredByFlagshipAsIfWasStationaryMode( command.RelatedMagnitude > 0 ), customOrderType );
                    if ( newOrder.TypeData == null )
                        continue;
                    
                    if (newOrder.CustomOrder.SetupFromCommand(ref newOrder, command) == false)
                    {
                        newOrder.CustomOrder.ReturnToPool();
                        return;
                    }

                    orders.QueueOrder( e, newOrder );
                }
            }
            catch ( Exception e )
            {
                if ( ArcenNetworkAuthority.DesiredStatus == DesiredMultiplayerStatus.Client )
                    return;
                    
                throw e;
            }
        }
    }
}
