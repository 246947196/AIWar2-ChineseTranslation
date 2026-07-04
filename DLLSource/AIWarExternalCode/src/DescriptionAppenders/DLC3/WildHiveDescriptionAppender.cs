
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

            Buffer.Add( $"This Hive has been built around a {entityData.DisplayName}, and will convert back to it as a neutral entity if killed." );

            if ( WildHivesFactionBaseInfo.HasStoredSoldiers( RelatedEntityOrNull, RelatedEntityOrNull.GetFactionBaseInfoOrNullAs_Safe<WildHivesFactionBaseInfo>(), hiveInfo, out int storedSoldiers ) )
            {
                Buffer.Add( $" There are {storedSoldiers} clanlings stored within, ready to attack any that agitate hives or workers on this planet." );
            }
            if ( WildHivesFriendlyFactionBaseInfo.Instance.hivesOnFriendlyPlanets.DisplayContains( RelatedEntityOrNull ) )
            {
                int timeLeft = WildHivesFactionBaseInfo.GetHighestDifficulty().secondsBetweenFriendlySoldierSpawns - WildHivesFriendlyFactionBaseInfo.Instance.SecondsUntilNextSoldier( RelatedEntityOrNull );
                string minutes = (timeLeft / 60).ToString( "0" );
                string seconds = (timeLeft % 60).ToString( "0" );
                Buffer.Add( $" This hive considers you part of its ecosystem, and is producing friendly clanlings to defend you. Next spawn in: {minutes}:{seconds}" );
            }
        }
    }
}
