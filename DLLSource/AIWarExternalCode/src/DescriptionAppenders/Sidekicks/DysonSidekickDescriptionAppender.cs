using Arcen.AIW2.Core;
using Arcen.Universal;
using System;

using System.Text;

namespace Arcen.AIW2.External
{
    public class DysonSidekickDescriptionAppender : GameEntityDescriptionAppenderBase
    {
        public override void AddToDescriptionBuffer( GameEntity_Squad RelatedEntityOrNull, GameEntityTypeData RelatedEntityTypeData, ArcenCharacterBufferBase Buffer )
        {
            try
            {
                bool debug = GameSettings.Current.GetBoolBySetting( "Debug_Tooltip" ) || (Engine_AIW2.TraceAtAll && Engine_AIW2.TracingFlags.Has( ArcenTracingFlags.Templar ));
                if ( RelatedEntityTypeData == null )
                {
                    ArcenDebugging.ArcenDebugLogSingleLine( "No type data?", Verbosity.DoNotShow );
                    return;
                }
                if ( RelatedEntityOrNull == null )
                    return;
                SafeSquadWrapper RelatedEntityWrapper = SafeSquadWrapper.Create( RelatedEntityOrNull );
                TooltipDetail detailLevel = EntityText.Detail;
                if (RelatedEntityTypeData.GetHasTag("ReaperGateway") )
                {
                    ReapersPerUnitBaseInfo rData = RelatedEntityOrNull.TryGetExternalBaseInfoAs<ReapersPerUnitBaseInfo>();
                    if (rData == null)
                    {
                        Buffer.Add("收割者网关的收割者单位数据为空");
                        return;
                    }
                    int time = rData.GatewayNextReinforcementTime - World_AIW2.Instance.GameSecond;
                    Buffer.Add("此网关将在 ").Add( time, "a1bb44" ).Add(" 秒后得到增援。").Add("\n");
                    time = rData.GatewayNextLarvaTime - World_AIW2.Instance.GameSecond;
                    Buffer.Add("此网关将在 ").Add( time, "a1bb44" ).Add(" 秒后生成一个幼虫。").Add("\n");
                    if (RelatedEntityOrNull.CurrentMarkLevel < 7)
                    {
                        time = rData.GatewayNextMarkupTime - World_AIW2.Instance.GameSecond;
                        Buffer.Add("此网关将在 ").Add( time, "a1bb44" ).Add(" 秒后升级。").Add("\n");
                    }
                    return;
                }
                if ( RelatedEntityTypeData.GetHasTag("ImmobileRavager")  || RelatedEntityTypeData.GetHasTag("ReaperChrysalis") )
                {
                    ReapersPerUnitBaseInfo rData = RelatedEntityOrNull.TryGetExternalBaseInfoAs<ReapersPerUnitBaseInfo>();
                    if ( rData != null )
                    {
                        if (RelatedEntityTypeData.GetHasTag("ImmobileRavager"))
                        {
                            Buffer.Add("此星球将在 ").Add( rData.SecondsTillRavage, "a1bb44" ).Add(" 秒后被蹂躏。").Add("\n");
                            Buffer.Add("此蹂躏者将在 ").Add( rData.SecondsTillTroopSpawn, "4455a1" ).Add(" 秒后产生新的一波部队。");
                        }
                        if (RelatedEntityTypeData.GetHasTag("ReaperChrysalis"))
                        {
                            int spawnTime = rData.ChrysalisHatchTime - World_AIW2.Instance.GameSecond;
                            if (spawnTime >= 0)
                            {
                                string color = ArcenExternalUIUtilities.GetColorForNomadMoveTime( spawnTime );
                                Buffer.Add("此卵将在 ").Add(spawnTime.ToString(), color).Add(" 秒后孵化，产生敌人。\n");
                            }
                            else
                                Buffer.Add("此卵即将孵化，产生敌人\n");
                            Buffer.Add("此卵剩余 ").Add( rData.CuendillarRemaining, "ff4444" ).Add(" 昆德拉铁。\n");

                        }
                    }

                    return;
                }
                DysonSidekickPerUnitBaseInfo data = RelatedEntityOrNull.TryGetExternalBaseInfoAs<DysonSidekickPerUnitBaseInfo>();
                if ( RelatedEntityTypeData.GetHasTag("AICuendillarDrill") )
                {
                    if ( data != null && data.TimeForNextTransport != -1 )
                        Buffer.Add("新的AI昆德拉铁运输船将在 ").Add( (data.TimeForNextTransport - World_AIW2.Instance.GameSecond), "0044ff" ).Add(" 秒后派出。");
                    return;
                }
                Faction faction = RelatedEntityOrNull.PlanetFaction.Faction;
                if (RelatedEntityTypeData.GetHasTag("DysonFlagship") )
                {
                    if ( World_AIW2.Instance.Setup.GetStringBySetting("DysonAutoDefend") != "Disabled" )
                    {
                        if ( faction.UnderPlayerControl() )
                        {
                            Buffer.Add("<size=90%>此旗舰当前由玩家控制，但如果未被控制，它将自动以 ").Add( World_AIW2.Instance.Setup.GetStringBySetting("DysonAutoDefend"), "a1ffa1").Add(" 模式防御。这可通过星系设置修改。</size> ");
                        }
                        else
                            Buffer.Add("<size=90%>此旗舰设置为 ").Add( World_AIW2.Instance.Setup.GetStringBySetting("DysonAutoDefend"), "a1ffa1").Add(" 并自动运行（只要您未控制该阵营）。这可通过星系设置修改。</size> ");
                    }
                }

                DysonSidekickFactionBaseInfo globaldata = faction.TryGetExternalBaseInfoAs<DysonSidekickFactionBaseInfo>();
                if ( RelatedEntityTypeData.GetHasTag("BoostsGuardianCap") ||
                     RelatedEntityTypeData.GetHasTag("BoostsDireGuardianCap") )
                {
                    
                    Dictionary<Planet, int> guardianDict = globaldata.GuardiansPerPlanet.GetDisplayDict();
                    bool printedIntro = false;
                    foreach ( KeyValuePair<Planet, int> kv in guardianDict )
                    {
                        if ( !printedIntro)
                        {
                            printedIntro = true;
                            Buffer.Add("您帝国所有守护者的统计：\n");
                        }
                        Buffer.Add("\t").Add(kv.Key.Name, "a1a1ff").Add(": ").Add( kv.Value, "a1ffa1" ).Add("\n");
                        continue;
                    }
                    return;
                }

                if ( data == null )
                {
                    //Buffer.Add(RelatedEntityOrNull.ToString() + " has no per unit");
                    return;
                }
                if ( RelatedEntityTypeData.KillsToTriggerTransformation > 0 )
                {
                    Buffer.Add(" 此舰队已击杀 ").Add( data.UnitsKilled, "a1ffa1" ).Add(" 个单位；在击杀 ").Add( RelatedEntityTypeData.KillsToTriggerTransformation, "ffa1a1" ).Add(" 个后将变形为 ").Add(RelatedEntityTypeData.TransformAfterKills.GetDisplayName()).Add("。" );
                }

                if ( RelatedEntityTypeData.GetHasTag("CuendillarAsteroid") )
                {
                    Buffer.Add("此小行星有 ").Add( data.CuendillarRemaining, "a1ffa1" ).Add(" 昆德拉铁可供开采。");
                    return;
                }
                if ( RelatedEntityTypeData.GetHasTag("CuendillarPlanetoid") )
                {
                    Buffer.Add("此矮行星有 ").Add( data.CuendillarRemaining, "a1ffa1" ).Add(" 昆德拉铁可供开采。");
                    return;
                }

                if ( globaldata == null )
                    return;

                if ( RelatedEntityTypeData.GetHasTag("DysonOverloader") )
                {
                    int secondsLeftForOverload = data.TimeTillPlanetOverloaded;
                    if ( secondsLeftForOverload == -1 )
                        Buffer.Add("此星球将在过载器完成后 ").Add( globaldata.Difficulty.PlanetOverloadTime, "ff0000" ).Add(" 秒后被摧毁。");
                    else
                    {
                        Buffer.Add("此星球将在 ").Add( secondsLeftForOverload, "ff00ff" ).Add(" 秒后被摧毁。");
                        Buffer.Add(" 这将产生 ").Add( globaldata.Difficulty.AIPForPlanetOverloading, "ff00" ).Add(" AIP 作为钻探进行时。" );
                    }
                    return;
                }
                if ( RelatedEntityTypeData.GetHasTag("DysonAsteroidDrill") && data != null )
                {
                    int secondsLeftForDrill = data.TimeTillPlanetDrilled;
                    if ( secondsLeftForDrill == -1 )
                        Buffer.Add("此小行星将在钻机完成后 ").Add( globaldata.CalculateDrillTime( RelatedEntityOrNull ), "ff0000" ).Add(" 秒后被钻探。");
                    else
                    {
                        Buffer.Add("此小行星将在 ").Add( secondsLeftForDrill, "ff00ff" ).Add(" 秒后被完全钻探。" );
                        if ( data.TimeForNextTransport != -1 )
                            Buffer.Add("新的昆德拉铁运输船将在 ").Add( (data.TimeForNextTransport - World_AIW2.Instance.GameSecond), "0044ff" ).Add(" 秒后派出。");
                    }
                    return;
                }
                if ( RelatedEntityTypeData.GetHasTag("AutoDefenseShip") && data != null )
                {
                    GameEntity_Squad HomeStronghold = data.HomeStronghold.GetSquad();
                    if ( HomeStronghold != null )
                    {
                        Buffer.Add("此单位仅可用于防御 ").Add(HomeStronghold.Planet.Name, "a1a1ff").Add( " 上的 " ).Add( HomeStronghold.TypeData.GetDisplayName(), "a1ffa1" ).Add( " 附近的星球。" );
                    }
                }
                if ( RelatedEntityTypeData.GetHasTag("DysonDrill") && data != null )
                {
                    int secondsLeftForDrill = data.TimeTillPlanetDrilled;
                    if ( secondsLeftForDrill == -1 )
                    {
                        int time = globaldata.CalculateDrillTime( RelatedEntityOrNull );
                        Buffer.Add( "此星球将在钻机完成后 " ).Add( time.ToString(), "55b223" ).Add( " 秒后被钻探，总共将产生 " );
                    }
                    else
                    {
                        Buffer.Add("此星球将在 ").Add(secondsLeftForDrill.ToString(), "ff00ff").Add(" 秒后被钻探。");
                    }
                    if ( data.TimeForNextTransport != -1 )
                        Buffer.Add("新的昆德拉铁运输船将在 ").Add( (data.TimeForNextTransport - World_AIW2.Instance.GameSecond).ToString(), "0044ff").Add(" 秒后派出。" );
                    if ( RelatedEntityTypeData.GetHasTag( "DysonPlanetaryDrill" ) || RelatedEntityTypeData.GetHasTag( "DysonOverloader" ) )
                    {
                        int aip = globaldata.Difficulty.AIPForPlanetDrilling;
                        if ( RelatedEntityTypeData.GetHasTag( "DysonOverloader" ) )
                            aip = globaldata.Difficulty.AIPForPlanetOverloading;
                        Buffer.Add( " 建造此结构将产生 " ).Add( globaldata.Difficulty.AIPForPlanetDrilling.ToString(), "ff0000" ).Add( " AIP 作为此结构运行时。" );
                    }
                    return;
                }
                if ( RelatedEntityTypeData.GetHasTag("DysonStronghold") && data != null )//&& detailLevel >= TooltipDetail.Full)
                {
                    if ( data.GuardianMetal > 0 && data.DireGuardianMetal > 0 )
                    {
                        Buffer.Add("<size=80%>我们有 ").Add( data.GuardianMetal, "999999" ).Add(" 守护者金属和 ").Add( data.DireGuardianMetal, "999999" ).Add(" 精英守护者金属。</size> ");  
                    }
                    else if ( data.GuardianMetal > 0 )
                        Buffer.Add("<size=80%>我们有 ").Add( data.GuardianMetal, "999999" ).Add(" 金属用于建造守护者。</size> ");
                    else if ( data.DireGuardianMetal > 0 )
                        Buffer.Add("<size=80%>我们有 ").Add( data.DireGuardianMetal, "999999" ).Add(" 金属用于建造精英守护者。</size> ");
                    if ( data.DireGuardianMetal > 0 || data.GuardianMetal > 0)
                        Buffer.Add("\n");
                    if ( globaldata.GuardiansPerStronghold.GetDisplayDict()[RelatedEntityWrapper] > 0 )
                        Buffer.Add("<size=80%>当前支持 ").Add( globaldata.GuardiansPerStronghold.GetDisplayDict()[RelatedEntityWrapper], "999999" ).Add(" / " ).Add( data.GuardianCap, "a1ffa1" ).Add(" 守护者防御此据点。</size> ");
                    if ( globaldata.DireGuardiansPerStronghold.GetDisplayDict()[RelatedEntityWrapper] > 0 )
                        Buffer.Add("<size=80%>当前支持 ").Add( globaldata.DireGuardiansPerStronghold.GetDisplayDict()[RelatedEntityWrapper], "999999" ).Add(" / " ).Add( data.DireGuardianCap, "a1ffa1" ).Add(" 精英守护者防御此据点。</size> ");
                    if ( globaldata.GuardiansPerStronghold.GetDisplayDict()[RelatedEntityWrapper] > 0 ||  globaldata.DireGuardiansPerStronghold.GetDisplayDict()[RelatedEntityWrapper] > 0 )
                        Buffer.Add("\n");
                }
                if ( RelatedEntityTypeData.GetHasTag("DysonSidekickSphere") && data != null )
                {
                    if ( data.TimeTillDysonSphereWin == -1 )
                        Buffer.Add("您的球体将在球体完成后 ").Add( globaldata.Difficulty.TimeForSphereWin, "ff00ff" ).Add(" 秒后上线。");
                    else
                        Buffer.Add("您的球体将在 ").Add( data.TimeTillDysonSphereWin, "ff00ff" ).Add(" 秒后上线。");
                    return;
                }
                if ( RelatedEntityTypeData.GetHasTag("DysonSidekickTransport")  )
                {
                    if ( data.CuendillarTransported == -1 )
                        Buffer.Add("未知昆德拉铁？");
                    else
                        Buffer.Add("此运输船正在运送 ").Add( data.CuendillarTransported, "0044ff" ).Add(" 昆德拉铁到附近的球体（或您的总部）。");
                }

            }
            catch ( Exception ) { ArcenDebugging.ArcenDebugLogSingleLine("Whoops? exception in DysonSidekickDescriptionAppender", Verbosity.DoNotShow );}

            return;
        }
    }
}
