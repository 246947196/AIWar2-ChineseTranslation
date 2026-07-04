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
                    Buffer.Add( "璇锋殏鍋滄父鎴忎互鏌ョ湅闄勫姞淇℃伅" );
                    return;
                }
                if ( RelatedEntityTypeData.KillsToTriggerTransformation > 0 )
                {
                    debugCode = 140;
                    //for the scourge infused empire
                    if ( RelatedEntityTypeData.TransformAfterKills == null )
                        throw new Exception("Nothing is given to transform into");
                    Buffer.Add(" 姝よ埌闃熷凡鍑绘潃 ").Add( data.UnitsKilled, "a1ffa1" ).Add(" 涓崟浣嶏紱鍦ㄥ嚮鏉€ ").Add( RelatedEntityTypeData.KillsToTriggerTransformation, "ffa1a1" ).Add(" 涓悗灏嗗彉褰负 ").Add(RelatedEntityTypeData.TransformAfterKills.GetDisplayName()).Add("銆?);
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
                    Buffer.Add("姝よ姳鏈靛皢鍦?").Add(transformTime.ToString(), ArcenExternalUIUtilities.GetColorForNomadMoveTime(transformTime) ).Add(" 绉掑悗鍙樺舰銆?);
                    return;
                }
                debugCode = 300;
                Buffer.Add( "杩欐槸 " );
                if ( RelatedEntityOrNull.TypeData.IsMobile )
                    Buffer.Add( "鍗曚綅" );
                else
                    Buffer.Add( "寤虹瓚" );
                debugCode = 400;
                if ( data.IsOffToUpgrade )
                {
                    Buffer.Add( " 姝ｅ湪鍓嶅線鍐涙搴撳崌绾с€? );
                }
                else
                {
                    debugCode = 500;
                    if ( data.Experience >= data.ExperienceForNextLevel )
                    {
                        if ( RelatedEntityOrNull.TypeData.IsMobile )
                            Buffer.Add( " 宸叉湁璧勬牸鍦ㄥ啗姊板簱鍗囩骇銆? );
                        else
                            Buffer.Add( " 鍙敱寤洪€犺€呭崌绾с€? );
                    }
                    else
                        Buffer.Add( " 闇€瑕?" ).Add( (data.ExperienceForNextLevel - data.Experience).IntValue, "a1ffa1" ).Add( " 鏇村缁忛獙鍊兼墠鑳藉崌绾с€? );
                }
                debugCode = 600;
                if ( data.StoredMetal > FInt.Zero )
                    Buffer.Add( " 姝ゅ崟浣嶆嫢鏈?" ).Add( data.StoredMetal.IntValue, "10ff10" ).Add( " 鍌ㄥ瓨閲戝睘銆? );
                if ( data.MustEvolveBeforeJoiningFireteam && !(data.IsHybrid || data.IsEvolved) )
                    Buffer.Add( " 姝ゅ崟浣嶅繀椤昏繘鍖栧悗鎵嶈兘鍔犲叆鎴樻枟灏忛槦銆? );
                if ( data.NextMustBeAnUpgrade )
                    Buffer.Add( " 瀹冨繀椤诲厛鍗囩骇涓€涓缓绛戞墠鑳藉缓閫犳柊寤虹瓚銆? );
                debugCode = 700;
                if ( data.ScourgeTypeId != -1 )
                {
                    debugCode = 800;
                    ScourgeTypeData typedata = ScourgeTypeDataTable.Instance.GetRowById( data.ScourgeTypeId );
                    Buffer.Add( " " + typedata.ToString() + ". " );
                }
                if ( isVassal && RelatedEntityOrNull.TypeData.GetHasTag("ScourgeSummoner"))
                {
                    Buffer.Add("\n").Add("姝ゅ缓绛戞槸涓€涓彫鍞よ€咃紝鍚稿紩鎵€鏈夊弸鏂硅埌鑸?);
                }
                if ( RelatedEntityOrNull.TypeData.GetHasTag("ScourgeVassalArmory") && isVassal )
                {
                    ScourgeVassalFactionBaseInfo gdata = facOrNull.TryGetExternalBaseInfoAs<ScourgeVassalFactionBaseInfo>();
                    Buffer.Add("\n").Add("鍐涙搴撳彲浠ュ湪鏍囪 ").Add( gdata.Difficulty.RequiredLevelForArmoriesToEvolveWarriors, "a1ffa1" ).Add(" 绾ф椂鐢熶骇绉嶆棌鎴樺＋锛孖D " + data.ScourgeTypeId);
                    Buffer.Add("\n").Add("鍐涙搴撳彲浠ュ湪鏍囪 ").Add( gdata.Difficulty.RequiredLevelForArmoriesToHybridizeWarriors, "a1ffa1" ).Add(" 绾ф椂鐢熶骇娣峰悎鎴樺＋锛屽鏋滀綘宸茶В閿佺鎶€绛夌骇 2銆?);
                }
                debugCode = 900;
                if ( data.IsAssignedToMission )
                {
                    Buffer.Add( "\n姝ゅ崟浣嶈鍒嗛厤鍒颁换鍔★細" );
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
                        Buffer.Add( " 蹇呴』鍦ㄩ仴杩滄槦鐞?" + World_AIW2.Instance.GetPlanetByIndex( data.MustBuildNextOnFarFlungPlanetIdx ).Name + " 涓婂缓閫犱笅涓€涓€? );
                    if ( data.MetalIncomeLastSecond_ForUI > FInt.Zero )
                        Buffer.Add( " 涓婁竴绉掗噾灞炴敹鍏ワ細" ).Add( data.MetalIncomeLastSecond_ForUI.ReadableString, "10ffdd" ).Add( ".\n" );
                    if ( RelatedEntityOrNull.TypeData.GetHasTag( "ScourgeWarrior" ) )
                    {
                        Buffer.Add( " 鎴樻枟灏忛槦 ID " ).Add( RelatedEntityOrNull.FireteamId, "10ffdd" ).Add( " 鐘舵€佷负 " );
                        if ( team != null )
                        {
                            Buffer.Add( team.status.ToString(), "aaaaff" ).Add( "." );
                            if ( team.DefenseMode )
                                Buffer.Add( " 杩欐槸 " ).Add( "闃插尽鑸伴槦", "44dd44" ).Add( "銆? );
                            if ( team.Target != null )
                                Buffer.Add( " 鐩爣锛? ).Add( team.Target.ToStringWithPlanet(), "ffaaaa" ).Add( "銆? );
                            else if ( team.TargetPlanet != null )
                                Buffer.Add( " 鐩爣鏄熺悆锛? ).Add( team.TargetPlanet.Name, "ff2244" ).Add( "銆? );
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
