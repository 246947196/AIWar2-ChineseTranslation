using Arcen.AIW2.Core;
using Arcen.Universal;
using System;

using System.Text;

namespace Arcen.AIW2.External
{
    public class ScourgeGenericUnitDescriptionAppender : GameEntityDescriptionAppenderBase
    {
        public override void AddToDescriptionBuffer( GameEntity_Squad RelatedEntityOrNull, GameEntityTypeData RelatedEntityTypeData, ArcenCharacterBufferBase Buffer )
        {
            int debugCode = 0;
            try{
                bool debug = GameSettings.Current.GetBoolBySetting( "Debug_Tooltip" ) || (Engine_AIW2.TraceAtAll && Engine_AIW2.TracingFlags.Has( ArcenTracingFlags.Scourge ));
                if ( RelatedEntityOrNull == null || RelatedEntityOrNull.TypeData.GetHasTag( "WarpingInScourgeFortress" ) || RelatedEntityOrNull.TypeData.GetHasTag( "WarpingInScourgeArmory" ) || RelatedEntityOrNull.TypeData.GetHasTag( "WarpingInScourgeSpawner" ) ) //no bonus info about warping in units
                    return;
                debugCode = 100;
                ScourgePerUnitBaseInfo data = RelatedEntityOrNull.TryGetExternalBaseInfoAs<ScourgePerUnitBaseInfo>();
                if ( data == null )
                    return;
                Faction facOrNull = RelatedEntityOrNull.GetFactionOrNull_Safe();
                if ( facOrNull == null )
                {
                    Buffer.Add( "请暂停游戏以查看附加信息" );
                    return;
                }
                if ( RelatedEntityTypeData.KillsToTriggerTransformation > 0 )
                {
                    debugCode = 140;
                    //for the scourge infused empire
                    if ( RelatedEntityTypeData.TransformAfterKills == null )
                        throw new Exception("Nothing is given to transform into");
                    Buffer.Add(" 此舰队已击杀 ").Add( data.UnitsKilled, "a1ffa1" ).Add(" 个单位；在击杀 ").Add( RelatedEntityTypeData.KillsToTriggerTransformation, "ffa1a1" ).Add(" 个后将变形为 ").Add(RelatedEntityTypeData.TransformAfterKills.GetDisplayName()).Add("。");
                    return;
                }
                if ( facOrNull.Type == FactionType.Player)
                    return; //if this is owned by a player, it's a flagship so bail out after the KillsToTriggerTransformation code
                debugCode = 200;
                Fireteam team = null;
                bool isVassal = false;
                if ( facOrNull.SpecialFactionData.InternalName == "ScourgeVassal" )
                {
                    debugCode = 210;
                    ScourgeVassalFactionBaseInfo gdata = facOrNull.TryGetExternalBaseInfoAs<ScourgeVassalFactionBaseInfo>();
                    team = FireteamBaseUtility.GetFireteamById( gdata.Teams, RelatedEntityOrNull.FireteamId );
                    isVassal = true;
                }
                else
                {
                    debugCode = 220;
                    ScourgeFactionBaseInfo gdata = facOrNull.TryGetExternalBaseInfoAs<ScourgeFactionBaseInfo>();
                    team = FireteamBaseUtility.GetFireteamById( gdata.Teams, RelatedEntityOrNull.FireteamId );
                }
                debugCode = 230;
                if ( RelatedEntityTypeData.GetHasTag("ScourgeSeed"))
                    return;
                if ( RelatedEntityTypeData.GetHasTag("ScourgeFlower"))
                {

                    debugCode = 240;
                    ScourgeVassalFactionBaseInfo gdata = facOrNull.TryGetExternalBaseInfoAs<ScourgeVassalFactionBaseInfo>();
                    int transformTime = RelatedEntityOrNull.GameSecondCreated + gdata.Difficulty.FlowerGrowTime -  World_AIW2.Instance.GameSecond;
                    Buffer.Add("此花朵将�").Add(transformTime.ToString(), ArcenExternalUIUtilities.GetColorForNomadMoveTime(transformTime) ).Add(" 秒后变形。");
                    return;
                }
                debugCode = 300;
                Buffer.Add( "这是 " );
                if ( RelatedEntityOrNull.TypeData.IsMobile )
                    Buffer.Add( "单位" );
                else
                    Buffer.Add( "建筑" );
                debugCode = 400;
                if ( data.IsOffToUpgrade )
                {
                    Buffer.Add( " 正在前往军械库升级。" );
                }
                else
                {
                    debugCode = 500;
                    if ( data.Experience >= data.ExperienceForNextLevel )
                    {
                        if ( RelatedEntityOrNull.TypeData.IsMobile )
                            Buffer.Add( " 已有资格在军械库升级。" );
                        else
                            Buffer.Add( " 可由建造者升级。" );
                    }
                    else
                        Buffer.Add( " 需要 " ).Add( (data.ExperienceForNextLevel - data.Experience).IntValue, "a1ffa1" ).Add( " 更多经验值才能升级。" );
                }
                debugCode = 600;
                if ( data.StoredMetal > FInt.Zero )
                    Buffer.Add( " 此单位拥有 " ).Add( data.StoredMetal.IntValue, "10ff10" ).Add( " 储存金属。" );
                if ( data.MustEvolveBeforeJoiningFireteam && !(data.IsHybrid || data.IsEvolved) )
                    Buffer.Add( " 此单位必须进化后才能加入战斗小队。" );
                if ( data.NextMustBeAnUpgrade )
                    Buffer.Add( " 它必须先升级一个建筑才能建造新建筑。" );
                debugCode = 700;
                if ( data.ScourgeTypeId != -1 )
                {
                    debugCode = 800;
                    ScourgeTypeData typedata = ScourgeTypeDataTable.Instance.GetRowById( data.ScourgeTypeId );
                    Buffer.Add( " " + typedata.ToString() + ". " );
                }
                if ( isVassal && RelatedEntityOrNull.TypeData.GetHasTag("ScourgeSummoner"))
                {
                    Buffer.Add("\n").Add("此建筑是一个召唤者，吸引所有友方单位");
                }
                if ( RelatedEntityOrNull.TypeData.GetHasTag("ScourgeVassalArmory") && isVassal )
                {
                    ScourgeVassalFactionBaseInfo gdata = facOrNull.TryGetExternalBaseInfoAs<ScourgeVassalFactionBaseInfo>();
                    Buffer.Add("\n").Add("军械库可以在标记 ").Add( gdata.Difficulty.RequiredLevelForArmoriesToEvolveWarriors, "a1ffa1" ).Add(" 级时生产种族战士，ID " + data.ScourgeTypeId);
                    Buffer.Add("\n").Add("军械库可以在标记 ").Add( gdata.Difficulty.RequiredLevelForArmoriesToHybridizeWarriors, "a1ffa1" ).Add(" 级时生产混合战士，如果你已解锁科技等级 2。");
                }
                debugCode = 900;
                if ( data.IsAssignedToMission )
                {
                    Buffer.Add( "\n此单位被分配到任务：" );
                    if ( data.MyMission == null )
                        Buffer.Add(" (null)");
                    else
                        Buffer.Add(data.MyMission.ToStringForDisplay() );
                }
                debugCode = 1000;
                // if ( GameSettings.Current.GetBoolBySetting( "ShowFireteamHistory" ) && team != null && team.History.Count > 0 )
                // {
                //     Buffer.Add("\nFireteam History:\n ");
                //     for ( int i = team.History.Count - 1; i >= 0; i-- )
                //     {
                //         Buffer.Add("\t" + team.History[i] +"\n");
                //     }
                // }
                if ( debug )
                {
                    debugCode = 110;
                    if ( data.MustBuildNextOnFarFlungPlanetIdx != -1 )
                        Buffer.Add( " 必须在遥远星�" + World_AIW2.Instance.GetPlanetByIndex( data.MustBuildNextOnFarFlungPlanetIdx ).Name + " 上建造下一个。" );
                    if ( data.MetalIncomeLastSecond_ForUI > FInt.Zero )
                        Buffer.Add( " 上一秒金属收入：" ).Add( data.MetalIncomeLastSecond_ForUI.ReadableString, "10ffdd" ).Add( ".\n" );
                    if ( RelatedEntityOrNull.TypeData.GetHasTag( "ScourgeWarrior" ) )
                    {
                        Buffer.Add( " 战斗小队 ID " ).Add( RelatedEntityOrNull.FireteamId, "10ffdd" ).Add( " 状态为 " );
                        if ( team != null )
                        {
                            Buffer.Add( team.status.ToString(), "aaaaff" ).Add( "." );
                            if ( team.DefenseMode )
                                Buffer.Add( " 这是 " ).Add( "防御舰队", "44dd44" ).Add( "。" );
                            if ( team.Target != null )
                                Buffer.Add( " 目标：" ).Add( team.Target.ToStringWithPlanet(), "ffaaaa" ).Add( "。" );
                            else if ( team.TargetPlanet != null )
                                Buffer.Add( " 目标星球：" ).Add( team.TargetPlanet.Name, "ff2244" ).Add( "。" );
                        }
                    }

                }
            } catch ( Exception e)
            {
                ArcenDebugging.LogSingleLine("caught exception " + e + " debugCode " + debugCode, Verbosity.DoNotShow );
            }
            return;
        }
    }
}
