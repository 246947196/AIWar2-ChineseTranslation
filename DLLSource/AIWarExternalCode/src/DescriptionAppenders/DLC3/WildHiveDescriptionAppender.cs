
using Arcen.AIW2.Core;
using Arcen.Universal;

namespace Arcen.AIW2.External
{
    public class WildHiveDescriptionAppenderAppender : GameEntityDescriptionAppenderBase
    {
        public override void AddToDescriptionBuffer( GameEntity_Squad RelatedEntityOrNull, GameEntityTypeData RelatedEntityTypeData, ArcenCharacterBufferBase Buffer )
        {
            if ( RelatedEntityOrNull == null )
                return;

            WildHivesPerUnitBaseInfo hiveInfo = RelatedEntityOrNull.GetExternalBaseInfoAs<WildHivesPerUnitBaseInfo>();
            GameEntityTypeData entityData = GameEntityTypeDataTable.Instance.GetRowByNameOrNullIfNotFound( hiveInfo.BaseEntityName );
            if ( entityData == null )
                return; // Skip if not valid. We'll catch this error elsewhere in a less prone to spam zone.

            Buffer.Add( $"此蜂巢已围绕 {entityData.DisplayName} 建造，如果被摧毁将转换为中立实体。" );

            if ( WildHivesFactionBaseInfo.HasStoredSoldiers( RelatedEntityOrNull, RelatedEntityOrNull.GetFactionBaseInfoOrNullAs_Safe<WildHivesFactionBaseInfo>(), hiveInfo, out int storedSoldiers ) )
            {
                Buffer.Add( $" 内部储存了 {storedSoldiers} 个氏族成员，准备攻击任何在此星球上骚扰蜂巢或工蜂的目标。" );
            }
            if ( WildHivesFriendlyFactionBaseInfo.Instance.hivesOnFriendlyPlanets.DisplayContains( RelatedEntityOrNull ) )
            {
                int timeLeft = WildHivesFactionBaseInfo.GetHighestDifficulty().secondsBetweenFriendlySoldierSpawns - WildHivesFriendlyFactionBaseInfo.Instance.SecondsUntilNextSoldier( RelatedEntityOrNull );
                string minutes = (timeLeft / 60).ToString( "0" );
                string seconds = (timeLeft % 60).ToString( "0" );
                Buffer.Add( $" 此蜂巢认为您是其生态系统的一部分，正在生产友好的氏族成员来保护您。下次生成：{minutes}:{seconds}" );
            }
        }
    }
}
