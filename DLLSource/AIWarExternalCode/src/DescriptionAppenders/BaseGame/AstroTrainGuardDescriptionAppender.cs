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
                Buffer.Add( "姝ゆ姢鍗墍淇濇姢鐨勭伀杞﹀凡鎽ф瘉锛屽洜姝よ鎶ゅ崼姝ｅ湪鎹熻€椼€? );
                return;
            }
            else
            {
                debugCode = 400;
                Buffer.Add( "姝ｅ湪 " + train.GetPlanetName_Safe() + " 涓婃姢鍗?" + train.TypeData.GetDisplayName() + "銆? );
                if ( train.Planet != RelatedEntityOrNull.Planet )
                    Buffer.Add( "姝ゅ崟浣嶆鍦ㄦ崯鑰楋紝鍥犱负瀹冧笌鍏舵墍鎶ゅ崼鐨勭伀杞︿笉鍦ㄥ悓涓€鏄熺悆涓娿€? );
            }
            } catch ( Exception e )
            {
                ArcenDebugging.ArcenDebugLogSingleLine("Hit exception in AstroTrainGuardDescriptionAppender debugCode " + debugCode + " " + e.ToString(), Verbosity.DoNotShow );
            }
        }
    }
}
