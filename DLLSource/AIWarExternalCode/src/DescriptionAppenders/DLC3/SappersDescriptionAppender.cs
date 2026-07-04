using Arcen.AIW2.Core;
using Arcen.Universal;
using System;

using System.Text;

namespace Arcen.AIW2.External
{
    public class SappersDescriptionAppender : GameEntityDescriptionAppenderBase
    {
        public override void AddToDescriptionBuffer( GameEntity_Squad RelatedEntityOrNull, GameEntityTypeData RelatedEntityTypeData, ArcenCharacterBufferBase Buffer )
        {
            bool debug = GameSettings.Current.GetBoolBySetting( "Debug_Tooltip" ) || (Engine_AIW2.TraceAtAll && Engine_AIW2.TracingFlags.Has( ArcenTracingFlags.Sappers ));
            if ( RelatedEntityTypeData == null )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "No type data?", Verbosity.DoNotShow );
                return;
            }

            SappersPerUnitBaseInfo data = RelatedEntityOrNull.TryGetExternalBaseInfoAs<SappersPerUnitBaseInfo>();
            if ( data == null )
            {
                Buffer.Add( "没有单位数据？" );
                return;
            }
            Faction faction = RelatedEntityOrNull.GetFactionOrNull_Safe();
            SappersFactionBaseInfo globaldata = faction.TryGetExternalBaseInfoAs<SappersFactionBaseInfo>();

            int intensity = faction.BaseInfo.GetDifficultyOrdinal_OrNegativeOneIfNotRelevant();
            SappersDifficulty diff = SappersDifficultyTable.Instance.GetRowByIntensity( globaldata.Intensity, faction );

            if ( data.IsBeachheadTurret )
                Buffer.Add( "此单位是作为滩头堡炮塔创建的，一旦该星球上的所有敌人被消灭，它将遭受损耗。" );

            if ( RelatedEntityTypeData.GetHasTag( "SapperBasicDefensiveStructure" ) )
            {
                int markupTime = SappersFactionBaseInfo.GetSecondsTillMarkup( RelatedEntityOrNull );
                if ( markupTime > -1 )
                {
                    string color = ArcenExternalUIUtilities.GetColorForNomadMoveTime( markupTime );
                    Buffer.Add( "此结构将在 " ).Add( markupTime.ToString(), color ).Add( " 秒后升级。" );
                }

            }
            if ( RelatedEntityTypeData.GetHasTag( "SapperHabitat" ) )
            {
                Buffer.Add( "此单位有 " ).Add( data.MetalStored, "a1ffa1" ).Add( " 金属可供工兵拾取。" );
            }
            if ( RelatedEntityTypeData.GetHasTag( "SapperBeachheadConstructor" ) )
            {
                if ( data.UnitToBuild != null && data.PlanetToBuildOn != null )
                    Buffer.Add( "此建造者将在 " ).Add( data.PlanetToBuildOn.Name, "a1ffa1" ).Add( " 建造 " ).Add( data.UnitToBuild.GetDisplayName(), "a1ffa1" ).Add( ".\n" );
            }
            if ( RelatedEntityTypeData.GetHasTag( "SapperBeachheader" ) )
            {
                if ( World_AIW2.Instance.GameSecond - data.TimeLastMadeConstructor < diff.BeachheadTurretInterval )
                    Buffer.Add( "此滩头堡将在 " ).Add( (diff.BeachheadTurretInterval - (World_AIW2.Instance.GameSecond - data.TimeLastMadeConstructor)), "ffa1a1" ).Add( " 秒后能够建造新的炮塔。" );
                else
                    Buffer.Add( "此滩头堡已准备好派遣炮塔协助其盟友。" );
                int markupTime = SappersFactionBaseInfo.GetSecondsTillMarkup( RelatedEntityOrNull );
                if ( markupTime > -1 )
                {
                    string color = ArcenExternalUIUtilities.GetColorForNomadMoveTime( markupTime );
                    Buffer.Add( "此滩头堡将在 " ).Add( markupTime.ToString(), color ).Add( " 秒后升级。" );
                }
            }
            if ( RelatedEntityTypeData.GetHasTag( "Sapper" ) )
            {
                if ( data.MetalStored > 0 )
                    Buffer.Add( "此工兵有 " ).Add( data.MetalStored, "a1ffa1" ).Add( " 金属可供花费。\n" );
                if ( data.BloodstoneStored > 0 )
                    Buffer.Add( "此工兵有 " ).Add( data.BloodstoneStored, "ffa143" ).Add( " 血石可供花费。\n" );
                if ( data.MoonstoneStored > 0 )
                    Buffer.Add( "此工兵有 " ).Add( data.MoonstoneStored, "43a1ff" ).Add( " 月石可供花费。\n" );
                if ( data.TagForUnitToBuildNext != "" )
                    Buffer.Add( "此工兵正在积攒资源购买 " ).Add( data.TagForUnitToBuildNext, "a1a1ff" ).Add( "\n" );
                GameEntity_Squad destSquad = data.DestinationForSappers.GetSquad();
                if ( destSquad != null )
                    Buffer.Add( "此工兵正前往 " ).Add( destSquad.Planet.Name, "a1ffa1" ).Add( " 的 " ).Add( destSquad.TypeData.GetDisplayName(), "a1ffa1" ).Add( " 收集资源。\n" );
                if ( data.UnitToBuild != null && data.PlanetToBuildOn != null )
                    Buffer.Add( "此工兵将在 " ).Add( data.PlanetToBuildOn.Name, "a1ffa1" ).Add( " 建造 " ).Add( data.UnitToBuild.GetDisplayName(), "a1ffa1" ).Add( ".\n" );

            }
            if ( RelatedEntityTypeData.GetHasTag( "SapperBaseCrystal" ) )
            {
                int flowerTime = (RelatedEntityOrNull.GameSecondEnteredThisPlanet + diff.TimeForCrystalToFlower) - World_AIW2.Instance.GameSecond;
                string color = ArcenExternalUIUtilities.GetColorForNomadMoveTime( flowerTime );
                Buffer.Add( "此水晶将在 " ).Add( (data.FloweringTime - World_AIW2.Instance.GameSecond).ToString(), color ).Add( " 秒后开花。" );
            }
            if ( RelatedEntityTypeData.GetHasTag( "SapperFloweredCrystal" ) )
            {
                Buffer.Add( "此开花水晶可被采集，获得 " ).Add( diff.ResourceFromHarvestingCrystal, "a1ffa1" ).Add( " 资源。" );
            }
            if ( RelatedEntityTypeData.GetHasTag( "SapperWatchtower" ) )
            {
                Buffer.Add( "此 " ).Add( RelatedEntityTypeData.GetDisplayName() ).Add( " 内部有：" );
                Buffer.Add( data.ShipsInside_ForUI );
                int strengthToShow = data.GetStrengthInside( RelatedEntityOrNull ) / 1000;
                if ( data.GetStrengthInside( RelatedEntityOrNull ) > 0 && strengthToShow == 0 )
                    strengthToShow = 1;
                if ( strengthToShow >= 1 )
                    Buffer.Add( "，约 " ).Add( ArcenExternalUIUtilities.GUI_StrengthTextColorAndIcon ).Add( strengthToShow, "a1ffa1" );
                Buffer.Add( "。瞭望塔最大 " ).Add( ArcenExternalUIUtilities.GUI_StrengthTextColorAndIcon ).Add( (globaldata.MaxWatchtowerStrength / 1000), "ff1a1a" ).Add( "。\n" );
                Buffer.Add( "它有 " ).Add( data.MetalStored, "a1ffa1" ).Add( " 金属可用于购买新舰船。\n" );
                Planet helpPlanet = data.PlanetWatchtowerWantsToHelp;
                if ( helpPlanet != null )
                    Buffer.Add( "我们检测到至少在 " + helpPlanet.Name + " 有敌人需要协助战斗。\n" );
                int markupTime = SappersFactionBaseInfo.GetSecondsTillMarkup( RelatedEntityOrNull );
                if ( markupTime > -1 )
                {
                    string color = ArcenExternalUIUtilities.GetColorForNomadMoveTime( markupTime );
                    Buffer.Add( "此瞭望塔将在 " ).Add( markupTime.ToString(), color ).Add( " 秒后升级。" );
                }
            }
        }
    }
}
