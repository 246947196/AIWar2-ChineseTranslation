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
                Buffer.Add( "When the ZA is at peace, all units outside of a Spawner will attrition." );
                return;
            }
            if ( globaldata.IsInWarFooting )
            {
                Buffer.Add( "This ZA is in " ).Add( "War Footing", "ff0000" ).Add( ". The ZA will get more powerful the longer it is in War Footing.\n" );
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
                Buffer.Add( "This " ).Add( RelatedEntityTypeData.GetDisplayName() ).Add( " has inside it " );
                Buffer.Add( data.ShipsInside_ForUI );
            }
            if ( RelatedEntityTypeData.GetHasTag( "ZenithArchitraveSpawner" ) &&
                 RelatedEntityOrNull.CurrentMarkLevel < 7 && globaldata.GetSpawnerUpgradeTime( RelatedEntityOrNull ) != -1 ) //the upgrade time is -1 if we are quiesced
                Buffer.Add( "Will upgrade in " ).AddHoursAndMinutes( globaldata.GetSpawnerUpgradeTime( RelatedEntityOrNull ) - World_AIW2.Instance.GameSecond, "a1ffa1" ).Add( ".\n" );
            if ( globaldata.IsQuiesced )
                Buffer.Add( "This Architrave is quiesced for " + (globaldata.QuiesceEndTime - World_AIW2.Instance.GameSecond) + " more seconds.\n" );
            if ( debug )
            {
                if ( faction.HasObtainedSpireDebris )
                    Buffer.Add( "Tag for ships: " ).Add( data.TagForShipsIncludingSpire, "a1ffa1" );
                else
                    Buffer.Add( "Tag for ships: " ).Add( data.TagForShips, "a1ffa1" );
                int strength = globaldata.GetAllowedPeaceStrengthForSpawner( RelatedEntityOrNull, diff ) / 1000;
                Buffer.Add( " Supports " ).Add( strength, "a1ffa1" ).Add( " strength in peace. " );
                Buffer.Add( "Current metal: " + globaldata.MetalReserves ).Add( ". " );
                Buffer.Add( "Pioneer Spawn time: " + globaldata.PioneerSpawnTime ).Add( ". " );
            }
        }
    }
}
