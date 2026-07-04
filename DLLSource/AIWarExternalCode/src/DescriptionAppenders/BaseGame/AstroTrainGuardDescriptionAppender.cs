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
                Buffer.Add( "The train this guarded has died, so this guard is attritioning." );
                return;
            }
            else
            {
                debugCode = 400;
                Buffer.Add( "Guarding " + train.TypeData.GetDisplayName() + " on " + train.GetPlanetName_Safe() + ". " );
                if ( train.Planet != RelatedEntityOrNull.Planet )
                    Buffer.Add( "This unit is attritioning since its not on the same planet as its train. " );
            }
            } catch ( Exception e )
            {
                ArcenDebugging.ArcenDebugLogSingleLine("Hit exception in AstroTrainGuardDescriptionAppender debugCode " + debugCode + " " + e.ToString(), Verbosity.DoNotShow );
            }
        }
    }
}
