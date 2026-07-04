using Arcen.AIW2.Core;
using Arcen.Universal;
using System;


using System.Text;

namespace Arcen.AIW2.External
{
    public class AstroTrainDescriptionAppender : GameEntityDescriptionAppenderBase
    {
        public override void AddToDescriptionBuffer( GameEntity_Squad RelatedEntityOrNull, GameEntityTypeData RelatedEntityTypeData, ArcenCharacterBufferBase Buffer )
        {
            if ( RelatedEntityOrNull == null )
                return;

            AstroTrainsPerTrainBaseInfo trainInfo = RelatedEntityOrNull.TryGetExternalBaseInfoAs<AstroTrainsPerTrainBaseInfo>();
            if ( trainInfo == null )
                return;
            Planet planetOrNull = World_AIW2.Instance.GetPlanetByIndex( trainInfo.TargetDepotPlanetID );
            if ( planetOrNull == null )
                Buffer.Add( "这列火车当前正在闲置，没有前往任何仓库。" );
            else if ( planetOrNull.IntelLevel > PlanetIntelLevel.Unexplored )
                Buffer.Add( "这列火车的最终目的地是").Add( planetOrNull.Name, "a1a1ff").Add(" 上的仓库。" );
            else
                Buffer.Add( "这列火车的最终目的地是一颗未探索星球上的仓库。" );
            Planet nextDest = RelatedEntityOrNull.GetDestinationPlanet();
            if ( nextDest != null && nextDest.IntelLevel > PlanetIntelLevel.Unexplored )
                Buffer.Add(" 这列火车的下一站是 ").Add(nextDest.Name, "ffa1a1").Add("。");

            ConcurrentDictionary<GameEntityTypeData, int> guardsOfThisTrainByType = trainInfo.StoredGuardsInsideThisTrain;

            if ( guardsOfThisTrainByType.Count > 0 || trainInfo.GuardMetal > 0 )
            {
                Buffer.Add( "这列火车拥有 " ).Add( trainInfo.GuardMetal, "a1a1ff" ).Add( " 金属用于购买新护甲" );
                if ( guardsOfThisTrainByType.Count > 0 )
                {
                    Buffer.Add( "，以及当前内部存储的护卫：" );
                    Balance_MarkLevel markByOrdinal = Balance_MarkLevelTable.Instance.RowsByOrdinal[RelatedEntityOrNull.CurrentMarkLevel];
                    bool isFirst = true;
                    foreach ( KeyValuePair<GameEntityTypeData, int> kv in guardsOfThisTrainByType )
                    {
                        if ( isFirst )
                            isFirst = false;
                        else
                            Buffer.Add( ", " );
                        Buffer.Add( kv.Key.GetDisplayName(), "a1ffa1" ).Add( " x" ).Add( kv.Value.ToString(), markByOrdinal.ColorHex );
                    }
                }
                Buffer.Add( ". " );
            }
        }
    }
}
