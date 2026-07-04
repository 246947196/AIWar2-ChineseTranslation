using Arcen.AIW2.Core;
using Arcen.Universal;
using System;

using System.Text;

namespace Arcen.AIW2.External
{
    public class ZenithArchitraveDescriptionAppender : GameEntityDescriptionAppenderBase
    {
        public override void AddToDescriptionBuffer( GameEntity_Squad RelatedEntityOrNull, GameEntityTypeData RelatedEntityTypeData, ArcenCharacterBufferBase Buffer )
        {
            bool debug = GameSettings.Current.GetBoolBySetting( "Debug_Tooltip" ) || (Engine_AIW2.TraceAtAll && Engine_AIW2.TracingFlags.Has( ArcenTracingFlags.ZenithArchitrave ));
            if ( RelatedEntityOrNull == null )
                return;
            Faction faction = RelatedEntityOrNull.GetFactionOrNull_Safe();
            if ( faction.SpecialFactionData.InternalName != "ZenithArchitrave" )
                return; //this is owned by the gladiator AI type
            ZenithArchitraveFactionBaseInfo globaldata = faction.TryGetExternalBaseInfoAs<ZenithArchitraveFactionBaseInfo>();
            if ( RelatedEntityOrNull.TypeData.IsMobile && RelatedEntityOrNull.TypeData.IsCombatant &&
                 !globaldata.IsInWarFooting )
            {
                Buffer.Add( "当拱顶石处于和平时，所有在生成器外的单位将遭受损耗。" );
                return;
            }
            if ( globaldata.IsInWarFooting )
            {
                Buffer.Add( "此拱顶石处于 " ).Add( "战争状态", "ff0000" ).Add( "。拱顶石处于战争状态的时间越长，它将变得越强大。\n" );
            }
            if ( RelatedEntityOrNull.TypeData.GetHasTag( "WarpingInZenithArchitraveSpawner" ) )
                return;
            ZenithArchitravePerUnitBaseInfo data = RelatedEntityOrNull.TryGetExternalBaseInfoAs<ZenithArchitravePerUnitBaseInfo>();
            if ( data == null )
                return;

            int intensity = faction.BaseInfo.GetDifficultyOrdinal_OrNegativeOneIfNotRelevant();
            ZenithArchitraveDifficulty diff = ZenithArchitraveDifficultyTable.Instance.GetRowByIntensity( intensity );
            byte markLevel = data.MarkLevelForShips;
            Balance_MarkLevel markByOrdinal = Balance_MarkLevelTable.Instance.RowsByOrdinal[markLevel];
            if ( data.ShipsInside.Count > 0 )
            {
                Buffer.Add( "此 " ).Add( RelatedEntityTypeData.GetDisplayName() ).Add( " 内部有 " );
                Buffer.Add( data.ShipsInside_ForUI );
            }
            if ( RelatedEntityTypeData.GetHasTag( "ZenithArchitraveSpawner" ) &&
                 RelatedEntityOrNull.CurrentMarkLevel < 7 && globaldata.GetSpawnerUpgradeTime( RelatedEntityOrNull ) != -1 ) //the upgrade time is -1 if we are quiesced
                Buffer.Add( "将在 " ).AddHoursAndMinutes( globaldata.GetSpawnerUpgradeTime( RelatedEntityOrNull ) - World_AIW2.Instance.GameSecond, "a1ffa1" ).Add( " 后升级。\n" );
            if ( globaldata.IsQuiesced )
                Buffer.Add( "此拱顶石将静默 " + (globaldata.QuiesceEndTime - World_AIW2.Instance.GameSecond) + " 秒。\n" );
            if ( debug )
            {
                if ( faction.HasObtainedSpireDebris )
                    Buffer.Add( "舰船标签：" ).Add( data.TagForShipsIncludingSpire, "a1ffa1" );
                else
                    Buffer.Add( "舰船标签：" ).Add( data.TagForShips, "a1ffa1" );
                int strength = globaldata.GetAllowedPeaceStrengthForSpawner( RelatedEntityOrNull, diff ) / 1000;
                Buffer.Add( " 和平时支持 " ).Add( strength, "a1ffa1" ).Add( " 战力。" );
                Buffer.Add( "当前金属：" + globaldata.MetalReserves ).Add( "。" );
                Buffer.Add( "先驱生成时间：" + globaldata.PioneerSpawnTime ).Add( "。" );
            }
        }
    }
}
