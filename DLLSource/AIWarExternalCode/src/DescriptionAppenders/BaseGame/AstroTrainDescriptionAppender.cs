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
                Buffer.Add( "杩欏垪鐏溅褰撳墠姝ｅ湪闂茬疆锛屾病鏈夊墠寰€浠讳綍浠撳簱銆? );
            else if ( planetOrNull.IntelLevel > PlanetIntelLevel.Unexplored )
                Buffer.Add( "杩欏垪鐏溅鐨勬渶缁堢洰鐨勫湴鏄?").Add( planetOrNull.Name, "a1a1ff").Add(" 涓婄殑浠撳簱銆? );
            else
                Buffer.Add( "杩欏垪鐏溅鐨勬渶缁堢洰鐨勫湴鏄竴棰楁湭鎺㈢储鏄熺悆涓婄殑浠撳簱銆? );
            Planet nextDest = RelatedEntityOrNull.GetDestinationPlanet();
            if ( nextDest != null && nextDest.IntelLevel > PlanetIntelLevel.Unexplored )
                Buffer.Add(" 杩欏垪鐏溅鐨勪笅涓€绔欐槸 ").Add(nextDest.Name, "ffa1a1").Add("銆?);

            ConcurrentDictionary<GameEntityTypeData, int> guardsOfThisTrainByType = trainInfo.StoredGuardsInsideThisTrain;

            if ( guardsOfThisTrainByType.Count > 0 || trainInfo.GuardMetal > 0 )
            {
                Buffer.Add( "杩欏垪鐏溅鎷ユ湁 " ).Add( trainInfo.GuardMetal, "a1a1ff" ).Add( " 閲戝睘鐢ㄤ簬璐拱鏂版姢鍗? );
                if ( guardsOfThisTrainByType.Count > 0 )
                {
                    Buffer.Add( ", 浠ュ強褰撳墠鍐呴儴瀛樺偍鐨勬姢鍗細" );
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
