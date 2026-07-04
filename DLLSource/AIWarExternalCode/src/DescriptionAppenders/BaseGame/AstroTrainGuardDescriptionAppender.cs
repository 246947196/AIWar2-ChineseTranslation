using Arcen.AIW2.Core;
using Arcen.Universal;
using System;

using System.Text;

namespace Arcen.AIW2.External
{    
    public class AstroTrainGuardDescriptionAppender : GameEntityDescriptionAppenderBase
    {
        public override void AddToDescriptionBuffer( GameEntity_Squad RelatedEntityOrNull, GameEntityTypeData RelatedEntityTypeData, ArcenCharacterBufferBase Buffer )
        {
            if ( RelatedEntityOrNull == null )
                return;
            int debugCode = 0;
            try{
                debugCode = 100;
            AstroTrainsPerTrainGuardUnitBaseInfo guardData = RelatedEntityOrNull.TryGetExternalBaseInfoAs<AstroTrainsPerTrainGuardUnitBaseInfo>();
            if ( guardData == null )
                return;
            debugCode = 200;
            GameEntity_Squad train = guardData.TrainIGuard.GetSquad();
            if ( train == null || train.TypeData == null || train.Planet == null ||
                 train.GetIsHostileTowards_Safe( RelatedEntityOrNull.PlanetFaction.Faction ) )
            {
                debugCode = 300;
                Buffer.Add( "此护卫所保护的火车已摧毁，因此该护卫正在损耗。" );
                return;
            }
            else
            {
                debugCode = 400;
                Buffer.Add( "正在 " + train.GetPlanetName_Safe() + " 上护卫" + train.TypeData.GetDisplayName() + "。" );
                if ( train.Planet != RelatedEntityOrNull.Planet )
                    Buffer.Add( "此单位正在损耗，因为它与其所护卫的火车不在同一星球上。" );
            }
            } catch ( Exception e )
            {
                ArcenDebugging.ArcenDebugLogSingleLine("Hit exception in AstroTrainGuardDescriptionAppender debugCode " + debugCode + " " + e.ToString(), Verbosity.DoNotShow );
            }
        }
    }
}
