using Arcen.AIW2.Core;
using Arcen.Universal;
using System;

using System.Text;

namespace Arcen.AIW2.External
{
    public class AstroTrainDepotDescriptionAppender : GameEntityDescriptionAppenderBase
    {
        public override void AddToDescriptionBuffer( GameEntity_Squad RelatedEntityOrNull, GameEntityTypeData RelatedEntityTypeData, ArcenCharacterBufferBase Buffer )
        {
            if ( RelatedEntityOrNull == null )
                return;
            AstroTrainsPerDepotBaseInfo depotData = RelatedEntityOrNull.GetExternalBaseInfoAs<AstroTrainsPerDepotBaseInfo>();
            if ( depotData == null )
                return;
            if ( depotData.data == null )
                return;

            Buffer.Add( depotData.data.ToString() );
            if ( depotData.data.TrainsNeededBeforeFiring != -1 || depotData.data.FiresOnEveryTrain == false )
            {
                Buffer.Add( " So far " + depotData.TrainsThatArrivedSafely );
                if ( depotData.TrainsThatArrivedSafely == 1 )
                    Buffer.Add( " has " );
                else
                    Buffer.Add( " have " );
                Buffer.Add( "made it." );
            }
        }
    }
}
